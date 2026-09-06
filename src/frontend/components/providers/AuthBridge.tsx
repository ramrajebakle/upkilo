"use client";

import { useEffect, useRef } from "react";
import { useSession, signOut } from "next-auth/react";
import { apiClient } from "@/lib/api";

/**
 * Bridges the NextAuth session's short-lived access token into the axios client
 * (and localStorage, which the request interceptor reads) so that the ~220 files
 * using `apiClient` send `Authorization: Bearer <token>` on every request.
 *
 * The refresh token is deliberately NOT mirrored here — it stays inside the
 * encrypted NextAuth session JWT (server-side). When the access token expires,
 * NextAuth's jwt() callback refreshes it, and the next session read pushes the
 * new token through this effect.
 */
export function AuthBridge() {
  const { data: session, status } = useSession();

  // Signing out is a one-way trip, so it must fire at most once per mount.
  //
  // Without this guard it looped until the browser ran out of sockets:
  //   POST /api/auth/signout net::ERR_INSUFFICIENT_RESOURCES
  //   Uncaught (in promise) TypeError: Failed to fetch
  //
  // useSession hands back a NEW session object on every poll and revalidation, and this effect
  // depends on `session`, so it re-runs constantly. While the refresh error is set — and it stays
  // set precisely when the network is failing, which is when signOut itself starts failing too —
  // every one of those re-runs fired another signOut. The failure was self-sustaining: the worse
  // the network got, the more requests it made.
  const signOutStarted = useRef(false);

  useEffect(() => {
    if (typeof window === "undefined") return;

    // A failed refresh means the session can no longer be renewed — force re-login.
    if (session?.error === "RefreshAccessTokenError") {
      localStorage.removeItem("token");
      localStorage.removeItem("tenantId");
      delete apiClient.defaults.headers.common["Authorization"];

      if (!signOutStarted.current) {
        signOutStarted.current = true;
        // Redirect explicitly rather than trusting callbackUrl: if the signOut request itself
        // fails, the user would otherwise sit on a dead page with a session that can never
        // recover, which is exactly the state that fed the loop.
        void signOut({ callbackUrl: "/en/login" }).catch(() => {
          window.location.href = "/en/login";
        });
      }
      return;
    }

    const token = session?.user?.accessToken;
    const tenantId = session?.user?.tenantId;

    if (status === "authenticated" && token) {
      localStorage.setItem("token", token);
      apiClient.defaults.headers.common["Authorization"] = `Bearer ${token}`;
      if (tenantId) {
        localStorage.setItem("tenantId", tenantId);
        apiClient.defaults.headers.common["X-Tenant-Id"] = tenantId;
      } else {
        localStorage.removeItem("tenantId");
        delete apiClient.defaults.headers.common["X-Tenant-Id"];
      }
    } else if (status === "unauthenticated") {
      localStorage.removeItem("token");
      localStorage.removeItem("tenantId");
      delete apiClient.defaults.headers.common["Authorization"];
      delete apiClient.defaults.headers.common["X-Tenant-Id"];
    }
  }, [session, status]);

  return null;
}
