"use client";

import React, { useEffect, useState } from "react";
import { BillingBanner } from "@/components/billing/BillingBanner";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { TrendingUp, Wallet, ArrowUpRight, ArrowDownRight, Users, Loader2 } from "lucide-react";
import { apiClient } from "@/lib/api";

interface TrendPoint { year: number; month: number; revenue: number; }

interface RevenueState {
  totalRevenue: number;
  activeSubscriptions: number;
  currency: string;
  trend: TrendPoint[];
  tiers: Array<{ tier: string; count: number }>;
}

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

function money(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    }).format(amount);
  } catch {
    return `${currency} ${Math.round(amount).toLocaleString()}`;
  }
}

export default function PlatformRevenuePage() {
  const [data, setData] = useState<RevenueState | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    // Every figure here is read from the platform billing endpoints. This page previously
    // displayed hardcoded amounts (₹48.2L MRR, ₹5.78Cr ARR, a ₹2.1L "forecast") that were not
    // derived from any data and were denominated in a currency the platform may not bill in.
    (async () => {
      const [billingRes, trendRes, tierRes] = await Promise.allSettled([
        apiClient.get("/api/v1/super-admin/billing"),
        apiClient.get("/api/v1/super-admin/analytics/revenue-trend"),
        apiClient.get("/api/v1/super-admin/analytics/tier-distribution"),
      ]);

      if (!active) return;

      const billing = billingRes.status === "fulfilled" ? billingRes.value.data ?? {} : {};
      const trendBody = trendRes.status === "fulfilled" ? trendRes.value.data : null;
      const tierBody = tierRes.status === "fulfilled" ? tierRes.value.data : null;

      const asArray = (v: unknown): any[] =>
        Array.isArray(v) ? v : Array.isArray((v as any)?.data) ? (v as any).data : [];

      setData({
        totalRevenue: Number(billing.totalRevenue) || 0,
        activeSubscriptions: Number(billing.activeSubscriptions) || 0,
        currency: billing.currency ?? "USD",
        trend: asArray(trendBody),
        tiers: asArray(tierBody).map((t: any) => ({
          tier: t.tier ?? t.name ?? "Unknown",
          count: Number(t.count ?? t.tenants) || 0,
        })),
      });
      setLoading(false);
    })();

    return () => { active = false; };
  }, []);

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <Loader2 className="h-6 w-6 animate-spin text-text-tertiary" />
      </div>
    );
  }

  const d = data!;
  const latest = d.trend.length ? d.trend[d.trend.length - 1] : null;
  const previous = d.trend.length > 1 ? d.trend[d.trend.length - 2] : null;
  const monthRevenue = latest ? Number(latest.revenue) || 0 : 0;
  const prevRevenue = previous ? Number(previous.revenue) || 0 : 0;
  const change = prevRevenue > 0 ? ((monthRevenue - prevRevenue) / prevRevenue) * 100 : null;
  const peak = d.trend.reduce((m, p) => Math.max(m, Number(p.revenue) || 0), 0) || 1;

  return (
    <div className="space-y-6 animate-fade-in">
      <BillingBanner
        context="platform"
        title="Platform Revenue"
        subtitle="Upkilo master billing and tenant subscription income"
        status="active"
        statusText="Stripe Connected"
      />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="bg-surface-0">
          <CardHeader className="pb-2">
            <CardDescription className="flex items-center gap-2">
              <Wallet size={16} className="text-text-tertiary" />
              Revenue this month
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-bold text-text-primary mb-2 tabular-nums">
              {money(monthRevenue, d.currency)}
            </div>
            <div className="flex items-center gap-2 text-sm">
              {change === null ? (
                <span className="text-text-tertiary">No prior month to compare</span>
              ) : (
                <>
                  <span
                    className={`flex items-center font-medium ${
                      change >= 0 ? "text-success-500" : "text-danger-500"
                    }`}
                  >
                    {change >= 0 ? <ArrowUpRight size={16} /> : <ArrowDownRight size={16} />}
                    {change >= 0 ? "+" : ""}
                    {change.toFixed(1)}%
                  </span>
                  <span className="text-text-tertiary">vs last month</span>
                </>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="bg-surface-0">
          <CardHeader className="pb-2">
            <CardDescription className="flex items-center gap-2">
              <TrendingUp size={16} className="text-text-tertiary" />
              Collected all time
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-bold text-text-primary mb-2 tabular-nums">
              {money(d.totalRevenue, d.currency)}
            </div>
            <span className="text-sm text-text-tertiary">Across all tenants</span>
          </CardContent>
        </Card>

        <Card className="bg-surface-0">
          <CardHeader className="pb-2">
            <CardDescription className="flex items-center gap-2">
              <Users size={16} className="text-text-tertiary" />
              Active subscriptions
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-bold text-text-primary mb-2 tabular-nums">
              {d.activeSubscriptions.toLocaleString()}
            </div>
            <span className="text-sm text-text-tertiary">Currently billing</span>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <Card className="bg-surface-0 h-full">
            <CardHeader>
              <CardTitle>Revenue trend</CardTitle>
              <CardDescription>Succeeded payments, last six months</CardDescription>
            </CardHeader>
            <CardContent className="border-t border-surface-100 pt-6">
              {d.trend.length === 0 ? (
                <p className="text-text-tertiary text-sm py-10 text-center">
                  No payments recorded in the last six months.
                </p>
              ) : (
                <div className="flex items-end gap-3 h-56">
                  {d.trend.map((p) => {
                    const v = Number(p.revenue) || 0;
                    return (
                      <div key={`${p.year}-${p.month}`} className="flex-1 flex flex-col items-center gap-2 min-w-0">
                        <span className="text-xs text-text-secondary tabular-nums">
                          {money(v, d.currency)}
                        </span>
                        <div
                          className="w-full bg-primary-500 rounded-t"
                          style={{ height: `${Math.max(4, (v / peak) * 160)}px` }}
                        />
                        <span className="text-xs text-text-tertiary">
                          {MONTHS[Math.min(11, Math.max(0, p.month - 1))]}
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        <div>
          <Card className="bg-surface-0 h-full">
            <CardHeader>
              <CardTitle>Tenants by plan</CardTitle>
              <CardDescription>Where subscribers sit today</CardDescription>
            </CardHeader>
            <CardContent className="border-t border-surface-100 pt-5">
              {d.tiers.length === 0 ? (
                <p className="text-text-tertiary text-sm">No subscription data available.</p>
              ) : (
                <ul className="space-y-3">
                  {d.tiers.map((t) => (
                    <li key={t.tier} className="flex items-center justify-between">
                      <span className="text-sm text-text-primary">{t.tier}</span>
                      <span className="text-sm font-semibold text-text-primary tabular-nums">
                        {t.count.toLocaleString()}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
