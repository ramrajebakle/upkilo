/**
 * Currency catalogue for the frontend.
 *
 * Mirrors Upkilo.Core.Helpers.Currency on the server, which stays authoritative.
 *
 * This is a formatting catalogue, NOT a menu. A tenant does not choose their currency: it is
 * fixed by the country of the Stripe account they settle through, and the server derives it from
 * that account. The list exists so amounts can be rendered with the right symbol and the right
 * number of decimal places — a yen amount must not show as "¥5,000.00", and a price field for a
 * zero-decimal currency must not step by 0.01.
 *
 * Do not reintroduce a currency <select> from this list. Letting a tenant pick one lets them pick
 * one their Stripe account cannot settle, and mixed currencies within a tenant silently corrupt
 * every revenue total, which sums amounts without grouping by currency.
 */

export interface CurrencyOption {
  code: string;
  symbol: string;
  name: string;
  /** Minor units — 0 for JPY/KRW/VND, 3 for KWD/BHD. Drives input step and decimal display. */
  decimals: number;
}

// Module-local: no consumer outside this file, and deliberately not exported so a
// currency <select> cannot be built from it (see note above).
const CURRENCIES: CurrencyOption[] = [
  { code: 'AED', symbol: 'د.إ', name: 'UAE Dirham', decimals: 2 },
  { code: 'AUD', symbol: 'A$', name: 'Australian Dollar', decimals: 2 },
  { code: 'BHD', symbol: 'ب.د', name: 'Bahraini Dinar', decimals: 3 },
  { code: 'BRL', symbol: 'R$', name: 'Brazilian Real', decimals: 2 },
  { code: 'CAD', symbol: 'CA$', name: 'Canadian Dollar', decimals: 2 },
  { code: 'CHF', symbol: 'CHF', name: 'Swiss Franc', decimals: 2 },
  { code: 'DKK', symbol: 'kr', name: 'Danish Krone', decimals: 2 },
  { code: 'EGP', symbol: 'E£', name: 'Egyptian Pound', decimals: 2 },
  { code: 'EUR', symbol: '€', name: 'Euro', decimals: 2 },
  { code: 'GBP', symbol: '£', name: 'British Pound', decimals: 2 },
  { code: 'HKD', symbol: 'HK$', name: 'Hong Kong Dollar', decimals: 2 },
  { code: 'IDR', symbol: 'Rp', name: 'Indonesian Rupiah', decimals: 2 },
  { code: 'INR', symbol: '₹', name: 'Indian Rupee', decimals: 2 },
  { code: 'JPY', symbol: '¥', name: 'Japanese Yen', decimals: 0 },
  { code: 'KES', symbol: 'KSh', name: 'Kenyan Shilling', decimals: 2 },
  { code: 'KRW', symbol: '₩', name: 'South Korean Won', decimals: 0 },
  { code: 'KWD', symbol: 'د.ك', name: 'Kuwaiti Dinar', decimals: 3 },
  { code: 'MXN', symbol: 'MX$', name: 'Mexican Peso', decimals: 2 },
  { code: 'MYR', symbol: 'RM', name: 'Malaysian Ringgit', decimals: 2 },
  { code: 'NGN', symbol: '₦', name: 'Nigerian Naira', decimals: 2 },
  { code: 'NOK', symbol: 'kr', name: 'Norwegian Krone', decimals: 2 },
  { code: 'NZD', symbol: 'NZ$', name: 'New Zealand Dollar', decimals: 2 },
  { code: 'PHP', symbol: '₱', name: 'Philippine Peso', decimals: 2 },
  { code: 'PLN', symbol: 'zł', name: 'Polish Zloty', decimals: 2 },
  { code: 'QAR', symbol: 'ر.ق', name: 'Qatari Riyal', decimals: 2 },
  { code: 'SAR', symbol: '﷼', name: 'Saudi Riyal', decimals: 2 },
  { code: 'SEK', symbol: 'kr', name: 'Swedish Krona', decimals: 2 },
  { code: 'SGD', symbol: 'S$', name: 'Singapore Dollar', decimals: 2 },
  { code: 'THB', symbol: '฿', name: 'Thai Baht', decimals: 2 },
  { code: 'TRY', symbol: '₺', name: 'Turkish Lira', decimals: 2 },
  { code: 'USD', symbol: '$', name: 'US Dollar', decimals: 2 },
  { code: 'VND', symbol: '₫', name: 'Vietnamese Dong', decimals: 0 },
  { code: 'ZAR', symbol: 'R', name: 'South African Rand', decimals: 2 },
];

const BY_CODE: Record<string, CurrencyOption> = Object.fromEntries(
  CURRENCIES.map((c) => [c.code, c])
);

const DEFAULT_CURRENCY = 'USD';

/** Look up a currency, falling back to a synthesized 2-decimal entry. Never returns undefined. */
export function getCurrency(code?: string | null): CurrencyOption {
  const normalized = (code || DEFAULT_CURRENCY).trim().toUpperCase();
  return (
    BY_CODE[normalized] ?? {
      code: normalized,
      symbol: normalized,
      name: normalized,
      decimals: 2,
    }
  );
}

/** Display symbol for a currency code. Falls back to the code itself. */
export function currencySymbol(code?: string | null): string {
  return getCurrency(code).symbol;
}

/**
 * Step value for a price input, derived from the currency's minor units.
 * A yen field stepping by 0.01 invites amounts that cannot be charged.
 */
export function currencyStep(code?: string | null): string {
  const { decimals } = getCurrency(code);
  return decimals === 0 ? '1' : `0.${'0'.repeat(decimals - 1)}1`;
}
