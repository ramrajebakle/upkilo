"use client";

import { useEffect } from "react";
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

  useEffect(() => {
    if (typeof window === "undefined") return;

    // A failed refresh means the session can no longer be renewed — force re-login.
    if (session?.error === "RefreshAccessTokenError") {
      localStorage.removeItem("token");
      localStorage.removeItem("tenantId");
      delete apiClient.defaults.headers.common["Authorization"];
      void signOut({ callbackUrl: "/en/login" });
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
