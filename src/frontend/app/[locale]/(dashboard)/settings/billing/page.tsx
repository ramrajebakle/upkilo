"use client";

import React from "react";
import { BillingSettings } from "@/components/settings/BillingSettings";
import { AnnualUpgradeBanner } from "@/components/billing/AnnualUpgradeBanner";

export default function BillingPage() {
  return (
    <div className="space-y-6 max-w-5xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Billing & Subscription</h1>
        <p className="text-muted-foreground">Manage your plan, payment methods, and invoice history.</p>
      </div>

      <AnnualUpgradeBanner />
      <BillingSettings />
    </div>
  );
}

