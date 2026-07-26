"use client";

import { useFormatter } from "next-intl";
import { formatCurrency } from "@/lib/utils";

interface CurrencyFormatterProps {
  amount: number;
  currency?: string;
  className?: string;
}

/**
 * A reusable component to format currency consistently across the app.
 * Uses next-intl's useFormatter which is configured in i18n.ts
 *
 * The format call is guarded: next-intl delegates to Intl.NumberFormat, which throws a
 * RangeError on an unrecognised currency code. Thrown from a component body that error unmounts
 * the surrounding tree, so an invalid code stored against one tenant blanked the page. On
 * failure we fall back to the shared non-throwing formatter.
 */
export function CurrencyFormatter({
  amount,
  currency = "USD",
  className = "",
}: CurrencyFormatterProps) {
  const format = useFormatter();

  let text: string;
  try {
    text = format.number(amount, { style: "currency", currency });
  } catch {
    text = formatCurrency(amount, currency);
  }

  return <span className={className}>{text}</span>;
}

export default CurrencyFormatter;
