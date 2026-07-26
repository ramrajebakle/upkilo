'use client';

import { useState, useEffect } from 'react';
import { type StoredConsent, getStoredConsent } from '@/components/CookieConsent';

/**
 * Returns the current consent state and re-renders when the user changes it.
 * Use this hook to gate analytics initialisation and other optional features.
 *
 * Example:
 *   const { analytics, marketing } = useConsent();
 *   if (analytics) initAnalytics();
 */
export function useConsent(): Partial<StoredConsent> {
    const [consent, setConsent] = useState<Partial<StoredConsent>>(() => getStoredConsent() ?? {});

    useEffect(() => {
        // Sync on mount in case consent was given in a previous session
        setConsent(getStoredConsent() ?? {});

        // Re-sync whenever the CookieConsent component fires 'consent-updated'
        const handler = () => setConsent(getStoredConsent() ?? {});
        window.addEventListener('consent-updated', handler);
        return () => window.removeEventListener('consent-updated', handler);
    }, []);

    return consent;
}

/**
 * Returns true only when the user has explicitly accepted the given category.
 * Returns false during SSR and before the banner has been interacted with.
 */
export function useConsentCategory(category: keyof Omit<StoredConsent, 'version' | 'timestamp'>): boolean {
    const consent = useConsent();
    return consent[category] === true;
}
