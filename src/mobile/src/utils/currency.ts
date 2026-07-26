/**
 * Currency formatting for the mobile apps.
 *
 * Consolidates two ad-hoc copies of this logic (RevenueScreen, ConsumerConfirmScreen) and
 * replaces the hardcoded `$${amount.toFixed(2)}` used across the invoice, payment, report and
 * service screens — which showed dollars to every tenant regardless of the currency they bill in,
 * and always with two decimal places.
 *
 * Nothing here throws. React Native's JS engine does not guarantee full Intl support on every
 * platform/version, and money is rendered inside component bodies where a throw unmounts the
 * screen. Every entry point falls back to a plain, always-correct string.
 */

import { useEffect, useState } from 'react';
import { apiClient } from '../api/apiClient';

/** Currencies with no minor unit — rendering these with decimals is wrong, not just ugly. */
const ZERO_DECIMAL = new Set([
  'BIF', 'CLP', 'DJF', 'GNF', 'JPY', 'KMF', 'KRW', 'MGA',
  'PYG', 'RWF', 'UGX', 'VND', 'VUV', 'XAF', 'XOF', 'XPF',
]);

/** Currencies with three minor units. */
const THREE_DECIMAL = new Set(['BHD', 'JOD', 'KWD', 'OMR', 'TND']);

const DEFAULT_CURRENCY = 'USD';

function normalizeCurrency(currency?: string | null): string {
  const code = (currency || DEFAULT_CURRENCY).trim().toUpperCase();
  return code || DEFAULT_CURRENCY;
}

/** Decimal places for a currency. Unknown codes default to 2. */
function currencyDecimals(currency?: string | null): number {
  const code = normalizeCurrency(currency);
  if (ZERO_DECIMAL.has(code)) return 0;
  if (THREE_DECIMAL.has(code)) return 3;
  return 2;
}

/**
 * Format an amount with its currency, e.g. "¥5,000" or "$39.00".
 * Falls back to "CODE 39.00" when Intl is unavailable or the code is unrecognised.
 */
export function money(amount: number | null | undefined, currency?: string | null): string {
  const code = normalizeCurrency(currency);
  const value = typeof amount === 'number' && isFinite(amount) ? amount : 0;

  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: code }).format(value);
  } catch {
    return `${code} ${value.toFixed(currencyDecimals(code))}`;
  }
}

// ── Tenant currency ──────────────────────────────────────────────────────
//
// Mirrors the web app's useTenantCurrency hook. Records that carry their own currency (services,
// invoices) should use that; this is the fallback for aggregate figures — revenue totals, report
// summaries — which belong to the tenant rather than to any one record.

let cachedTenantCurrency: string | null = null;
let inFlight: Promise<string> | null = null;

/**
 * The tenant's configured currency. Cached for the session — it does not change while the app is
 * open and several screens read it. Resolves to USD if the call fails, never rejects.
 */
async function loadTenantCurrency(): Promise<string> {
  if (cachedTenantCurrency) return cachedTenantCurrency;
  if (!inFlight) {
    inFlight = apiClient
      .get('/api/v1/settings/business')
      .then((res: any) => {
        cachedTenantCurrency = normalizeCurrency(res?.data?.currency);
        return cachedTenantCurrency;
      })
      .catch(() => DEFAULT_CURRENCY)
      .finally(() => {
        inFlight = null;
      });
  }
  return inFlight;
}

/** React hook returning the tenant's currency, defaulting to USD until loaded. */
export function useTenantCurrency(): string {
  const [currency, setCurrency] = useState<string>(cachedTenantCurrency || DEFAULT_CURRENCY);

  useEffect(() => {
    let active = true;
    loadTenantCurrency().then((c) => {
      if (active) setCurrency(c);
    });
    return () => {
      active = false;
    };
  }, []);

  return currency;
}

/** Display symbol for a currency, e.g. "¥". Falls back to the code itself. */
export function currencySymbol(currency?: string | null): string {
  const code = normalizeCurrency(currency);
  try {
    return (
      new Intl.NumberFormat(undefined, { style: 'currency', currency: code })
        .formatToParts(0)
        .find((p) => p.type === 'currency')?.value ?? code
    );
  } catch {
    return code;
  }
}
