'use client';

import { useState, useEffect, ReactNode } from 'react';

export function ClientSideProvider({ children }: { children: ReactNode }) {
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        setMounted(true);

        // ── PWA service worker ──────────────────────────────────────────────
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker
                .register('/sw.js', { scope: '/' })
                .then((reg) => {
                    reg.update().catch(() => {});
                })
                .catch(() => {});
        }

        // ── Timezone cookie (Essential) ────────────────────────────────────
        // This cookie is ESSENTIAL (listed in cookie-policy): the server uses it
        // for SSR timezone-aware rendering (e.g., appointment times). It contains
        // no personal identifier — only an IANA timezone string. It does not
        // require consent under the IT Act / ePrivacy Directive strictly-necessary
        // exemption, but is disclosed in our Cookie Policy as an essential cookie.
        //
        // We write it ONLY if it is absent or stale. We do NOT reload the page
        // on first set — the reload only occurs on a subsequent load where the
        // existing cookie differs, meaning the user has changed timezone.
        if (typeof Intl !== 'undefined') {
            try {
                const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
                if (!tz) return;

                const match = document.cookie.match(/(?:^|;\s*)timezone=([^;]+)/);
                const storedTz = match ? decodeURIComponent(match[1]) : null;

                if (storedTz !== tz) {
                    // Write the updated timezone cookie (1-year expiry, SameSite=Lax)
                    const expires = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toUTCString();
                    document.cookie = `timezone=${encodeURIComponent(tz)}; expires=${expires}; path=/; SameSite=Lax; Secure`;

                    // Only reload if the cookie was previously set to a *different* value.
                    // On first visit storedTz is null — no reload needed; SSR will pick up
                    // the timezone on the next navigation naturally.
                    if (storedTz !== null) {
                        window.location.reload();
                    }
                }
            } catch {
                // Intl may be unavailable in very old browsers — non-fatal
            }
        }
    }, []);

    if (!mounted) return null;

    return <>{children}</>;
}
