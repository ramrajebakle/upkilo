"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { TrendingDown, X } from "lucide-react";

interface AnnualSavingsData {
  eligible: boolean;
  showBanner: boolean;
  planName: string;
  monthlyAmount: number;
  annualAmount: number;
  savingsAmount: number;
  savingsPercent: number;
  currency: string;
  monthsOnCurrentPlan: number;
}

const DISMISS_KEY = "annual_banner_dismissed_until";

export function AnnualUpgradeBanner() {
  const [data, setData] = useState<AnnualSavingsData | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    const dismissedUntil = localStorage.getItem(DISMISS_KEY);
    if (dismissedUntil && Date.now() < parseInt(dismissedUntil, 10)) {
      setDismissed(true);
      return;
    }

    fetch("/api/v1/billing/annual-savings", { credentials: "include" })
      .then((r) => (r.ok ? r.json() : null))
      .then((d) => setData(d ?? null))
      .catch(() => null);
  }, []);

  const handleDismiss = () => {
    // Suppress for 30 days after dismissal
    localStorage.setItem(DISMISS_KEY, String(Date.now() + 30 * 24 * 60 * 60 * 1000));
    setDismissed(true);
  };

  if (dismissed || !data?.eligible || !data?.showBanner) return null;

  return (
    <div className="relative flex items-center justify-between gap-4 rounded-xl bg-gradient-to-r from-emerald-600 to-teal-600 p-4 pr-10 text-white shadow-md mb-6">
      <TrendingDown className="h-6 w-6 flex-shrink-0 text-emerald-200" />
      <div className="flex-1 min-w-0">
        <p className="font-semibold text-sm sm:text-base">
          Save {data.savingsPercent}% on {data.planName} — switch to annual billing
        </p>
        <p className="text-emerald-100 text-xs sm:text-sm mt-0.5">
          You&apos;re paying ${data.monthlyAmount}/mo. Annual is ${data.annualAmount}/mo — that&apos;s{" "}
          <strong>${Math.round(data.savingsAmount)}</strong> saved per year.
        </p>
      </div>
      <Link
        href="/settings/billing?upgrade=annual"
        className="flex-shrink-0 rounded-lg bg-white text-emerald-700 font-semibold text-xs sm:text-sm px-4 py-2 hover:bg-emerald-50 transition-colors"
      >
        Switch Now
      </Link>
      <button
        onClick={handleDismiss}
        aria-label="Dismiss"
        className="absolute right-2 top-2 rounded-full p-1 text-emerald-200 hover:text-white"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}
