import NextAuth, { DefaultSession, NextAuthConfig } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";

declare module "next-auth" {
  interface Session {
    error?: "RefreshAccessTokenError";
    user: {
      role: "platform_owner" | "platform_admin" | "tenant_owner" | "team_member" | "customer";
      tenantId: string | null;
      accessToken?: string;
    } & DefaultSession["user"];
  }
}

// Shape of the data we persist inside the encrypted NextAuth session JWT.
// The refresh token lives here (server-side only) and is never exposed to the browser.
type AppToken = {
  role?: string;
  tenantId?: string | null;
  accessToken?: string;
  refreshToken?: string;
  accessTokenExpires?: number; // epoch ms
  error?: "RefreshAccessTokenError";
  /**
   * Set when the backend REFUSED the refresh token (401/403), as opposed to the request
   * merely failing. A refusal is final — the same token will be refused again — so the jwt
   * callback stops retrying. A transient failure leaves this unset and stays retryable, so a
   * backend blip does not sign everyone out.
   */
  refreshRejected?: boolean;
  sub?: string;
  [key: string]: unknown;
};

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

// The backend UserRole enum is { Owner, Admin, Manager, Staff, SuperAdmin }.
// The frontend middleware, login redirect, and route guards branch on the semantic
// vocabulary { platform_owner, platform_admin, tenant_owner, team_member, customer }.
// Without this mapping the two never match, so a real "Owner" login falls through to
// /dashboard instead of the tenant command center. Map at the auth boundary so the rest
// of the app keeps working with its existing vocabulary. Dev-mock roles are already
// semantic and pass through unchanged.
// The semantic vocabulary the middleware and route guards branch on. Anything
// not in this set is not a role as far as the frontend is concerned.
const SEMANTIC_ROLES = new Set([
  "platform_owner",
  "platform_admin",
  "tenant_owner",
  "team_member",
  "customer",
]);

function mapBackendRole(role: string | undefined | null): string {
  const raw = (role ?? "").toLowerCase();
  switch (raw) {
    case "superadmin":
      return "platform_owner";
    case "owner":
      return "tenant_owner";
    case "admin":
    case "manager":
    case "staff":
      return "team_member";
    default:
      // Dev-mock logins already speak the semantic vocabulary, so pass those
      // through — but only if they are genuinely one of it.
      //
      // The previous default returned the unrecognised string verbatim and fell
      // back to "tenant_owner", described as a safe default. Neither is safe:
      // tenant_owner is the highest tenant privilege, and an unknown value that
      // passes through unchanged matches none of the guards written against the
      // known roles. The platform guard was a blocklist, so a role outside it
      // was let through to /admin and /platform rather than kept out. Adding one
      // role to the backend enum — a receptionist, a contractor, a read-only
      // auditor — was enough to trigger that.
      //
      // team_member is the least-privileged role that still belongs to a tenant:
      // blocked from the platform back office, landing on /dashboard rather than
      // a client portal it has no account for.
      return SEMANTIC_ROLES.has(raw) ? raw : "team_member";
  }
}

// Decode a JWT's `exp` claim (seconds) → epoch ms. Runs server-side (jwt callback),
// so Buffer is available. No signature verification — we only read the expiry.
function jwtExpiryMs(jwt?: string): number {
  if (!jwt) return 0;
  try {
    const payload = JSON.parse(Buffer.from(jwt.split(".")[1], "base64").toString("utf8"));
    return typeof payload.exp === "number" ? payload.exp * 1000 : 0;
  } catch {
    return 0;
  }
}

// Exchange the refresh token for a fresh access token via the backend.
// The backend accepts the refresh token in the request body (or cookie).
async function refreshAccessToken(token: AppToken): Promise<AppToken> {
  try {
    if (!token.refreshToken) {
      // Nothing to exchange, and nothing will change on a later attempt.
      return { ...token, error: "RefreshAccessTokenError", refreshRejected: true };
    }
    const res = await fetch(`${API_URL}/api/v1/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: token.refreshToken }),
    });

    if (!res.ok) {
      // 401/403 means the backend has judged this refresh token invalid, expired or revoked.
      // Repeating the identical request cannot change that answer, so it is marked permanent
      // and never retried. Production logged six of these in 1.4 seconds from one browser:
      //   SECURITY: AUTH_FAILURE [401] on /api/v1/auth/refresh by User=anonymous
      // Every session read retried a request that had already been definitively refused.
      //
      // Any other status (5xx, gateway, network) is left retryable — a backend blip should
      // not sign a user out.
      const rejected = res.status === 401 || res.status === 403;
      return { ...token, error: "RefreshAccessTokenError", refreshRejected: rejected };
    }

    const data = await res.json();
    if (!data?.token) throw new Error("refresh returned no token");
    return {
      ...token,
      accessToken: data.token,
      refreshToken: data.refreshToken ?? token.refreshToken,
      accessTokenExpires: jwtExpiryMs(data.token),
      error: undefined,
      refreshRejected: undefined,
    };
  } catch {
    // Network or parse failure — transient by assumption, so retryable. Fail closed on the
    // session either way, so nothing is authorised with a token we could not renew.
    return { ...token, error: "RefreshAccessTokenError" };
  }
}

export const authConfig: NextAuthConfig = {
  // Required in NextAuth v5 beta when running behind a proxy or on non-standard hosts (including localhost dev).
  trustHost: true,
  providers: [
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
        // Dev-only quick-login: username field used by mock buttons
        username: { label: "Username", type: "text" },
      },
      async authorize(credentials) {
        // ── Dev-only mock login (removed in production via NODE_ENV guard) ──
        if (process.env.NODE_ENV === "development") {
          if (credentials?.username === "platform" && credentials?.password === "password") {
            return {
              id: "dev-platform-1",
              name: "Platform Admin",
              email: "admin@upkilo.com",
              role: "platform_owner",
              tenantId: null,
              accessToken: "dev-mock-token",
            } as any;
          }
          if (credentials?.username === "tenant" && credentials?.password === "password") {
            return {
              id: "dev-tenant-1",
              name: "Tenant Owner",
              email: "owner@devnest.io",
              role: "tenant_owner",
              tenantId: "t-1",
              accessToken: "dev-mock-token",
            } as any;
          }
        }

        // ── Real credential validation via backend API ──
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const res = await fetch(`${API_URL}/api/v1/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          });

          if (!res.ok) return null;

          const data = await res.json();
          // The backend returns { token, refreshToken, user } for a completed login.
          // A 2FA-gated account returns { twoFactorRequired: true } with no token — the
          // main login page does not implement the 2FA step, so treat it as unauthenticated here.
          const accessToken = data?.token;
          if (!accessToken) return null;

          return {
            id: data.user?.id ?? data.userId ?? data.id,
            name: `${data.user?.firstName ?? data.firstName ?? ""} ${data.user?.lastName ?? data.lastName ?? ""}`.trim(),
            email: data.user?.email ?? data.email,
            role: mapBackendRole(data.user?.role ?? data.role),
            tenantId: data.user?.tenantId ?? data.tenantId ?? null,
            accessToken,
            refreshToken: data.refreshToken,
            accessTokenExpires: jwtExpiryMs(accessToken),
          } as any;
        } catch {
          // Network or parse failure — fail closed (never authenticate on error)
          return null;
        }
      },
    }),
  ],

  callbacks: {
    async jwt({ token, user }) {
      // Initial sign in — persist the backend tokens into the encrypted session JWT.
      if (user) {
        token.role = (user as any).role;
        token.tenantId = (user as any).tenantId;
        token.accessToken = (user as any).accessToken;
        token.refreshToken = (user as any).refreshToken;
        token.accessTokenExpires = (user as any).accessTokenExpires;
        token.sub = (user as any).id;
        return token;
      }

      // Dev mock token has no real expiry — keep the session alive.
      if (token.accessToken === "dev-mock-token") return token;

      // Access token still valid (60s safety margin) — reuse it.
      const exp = token.accessTokenExpires as number | undefined;
      if (exp && Date.now() < exp - 60_000) {
        return token;
      }

      // The backend has already refused this refresh token. Retrying cannot succeed, and this
      // callback runs on EVERY session read — so without this guard each read fired another
      // request that was certain to 401, which is what produced the burst in the server log.
      // The error stays on the token, so the client still sees it and signs out once.
      if ((token as AppToken).refreshRejected) return token;

      // Expired (or no expiry recorded) — attempt a refresh.
      return (await refreshAccessToken(token as AppToken)) as typeof token;
    },
    session({ session, token }) {
      if (session.user) {
        session.user.role = token.role as any;
        session.user.tenantId = (token.tenantId ?? null) as any;
        // Expose ONLY the short-lived access token to the browser — never the refresh token.
        session.user.accessToken = token.accessToken as string | undefined;
        session.user.id = token.sub as string;
      }
      session.error = token.error as "RefreshAccessTokenError" | undefined;
      return session;
    },
  },

  pages: {
    // Middleware extracts locale from pathname for locale-aware redirect —
    // this default only fires when middleware hasn't already redirected.
    signIn: "/en/login",
    error: "/en/login",
  },

  session: {
    strategy: "jwt",
    maxAge: 30 * 24 * 60 * 60, // 30 days
  },
};

export const { handlers, auth, signIn, signOut } = NextAuth(authConfig);
