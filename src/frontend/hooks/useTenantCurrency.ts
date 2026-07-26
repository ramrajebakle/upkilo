import { useEffect, useState } from 'react';
import api from '@/lib/api';

// Module-level cache — the tenant's currency does not change during a session, and
// several pages read it, so fetch it once rather than per-mount.
let cached: string | null = null;
let inFlight: Promise<string> | null = null;

async function loadCurrency(): Promise<string> {
  if (cached) return cached;
  if (!inFlight) {
    inFlight = api.settings
      .getBusiness()
      .then((res) => {
        cached = res.data?.currency || 'USD';
        return cached!;
      })
      .catch(() => 'USD')
      .finally(() => {
        inFlight = null;
      });
  }
  return inFlight;
}

/**
 * Returns the tenant's configured currency code (e.g. "USD", "INR").
 * Falls back to USD until loaded or if the settings call fails.
 */
export function useTenantCurrency(): string {
  const [currency, setCurrency] = useState<string>(cached || 'USD');

  useEffect(() => {
    let active = true;
    loadCurrency().then((c) => {
      if (active) setCurrency(c);
    });
    return () => {
      active = false;
    };
  }, []);

  return currency;
}
