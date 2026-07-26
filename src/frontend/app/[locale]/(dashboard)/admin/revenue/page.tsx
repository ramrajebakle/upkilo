"use client";

import React, { useState, useEffect } from "react";
import { TrendingUp, DollarSign, Users, BarChart3, Loader2, RefreshCw, ArrowUpRight, ArrowDownRight } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

interface RevenueSummary { mrr: number; arr: number; mrrGrowth: number; activeSubscriptions: number; churnRate: number; ltv: number; newSubscriptionsThisMonth: number; churned: number; }
interface MonthlyData { month: string; mrr: number; newMrr: number; churnedMrr: number; netNewMrr: number; }

export default function AdminRevenuePage() {
  const [summary, setSummary] = useState<RevenueSummary | null>(null);
  const [monthly, setMonthly] = useState<MonthlyData[]>([]);
  const [loading, setLoading] = useState(true);
  const [period, setPeriod] = useState(12);

  const load = async () => {
    setLoading(true);
    try {
      const [sumRes, mthRes] = await Promise.all([
        apiClient.get(`/api/v1/admin/revenue/summary?months=${period}`).catch(() => ({ data: null })),
        apiClient.get(`/api/v1/admin/revenue/monthly?months=${period}`).catch(() => ({ data: [] })),
      ]);
      setSummary(sumRes.data?.data ?? sumRes.data ?? null);
      setMonthly(Array.isArray(mthRes.data) ? mthRes.data : mthRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [period]);

  const fmt = (n: number) => n >= 1000 ? `$${(n / 1000).toFixed(1)}k` : `$${n}`;

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Platform Revenue <TrendingUp className="text-green-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">MRR, ARR, churn, and growth analytics across all tenants.</p>
        </div>
        <div className="flex items-center gap-2">
          <select value={period} onChange={(e) => setPeriod(parseInt(e.target.value))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
            {[3, 6, 12, 24].map((m) => <option key={m} value={m}>Last {m} months</option>)}
          </select>
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>
      </header>

      {loading ? <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {summary && (
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: "MRR", value: fmt(summary.mrr), sub: `${summary.mrrGrowth >= 0 ? "+" : ""}${summary.mrrGrowth?.toFixed(1)}% MoM`, positive: summary.mrrGrowth >= 0, icon: <DollarSign className="h-5 w-5 text-green-400" /> },
                { label: "ARR", value: fmt(summary.arr), sub: "Annualized", positive: true, icon: <TrendingUp className="h-5 w-5 text-ai-400" /> },
                { label: "Active Subscriptions", value: summary.activeSubscriptions.toString(), sub: `${summary.newSubscriptionsThisMonth} new this month`, positive: true, icon: <Users className="h-5 w-5 text-blue-400" /> },
                { label: "Churn Rate", value: `${summary.churnRate?.toFixed(1)}%`, sub: `${summary.churned} churned`, positive: summary.churnRate < 5, icon: <BarChart3 className="h-5 w-5 text-text-tertiary" /> },
              ].map((s) => (
                <Card key={s.label}>
                  <CardContent className="pt-5">
                    <div className="flex items-center justify-between mb-2">{s.icon}
                      <span className={`flex items-center gap-0.5 text-xs font-medium ${s.positive ? "text-green-600" : "text-red-500"}`}>
                        {s.positive ? <ArrowUpRight size={12} /> : <ArrowDownRight size={12} />}{s.sub}
                      </span>
                    </div>
                    <p className="text-xs text-text-secondary">{s.label}</p>
                    <p className="text-2xl font-bold mt-0.5 text-text-primary">{s.value}</p>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <Card className="lg:col-span-1">
              <CardHeader><CardTitle>Key Metrics</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                {summary ? [
                  { label: "Avg LTV", value: fmt(summary.ltv) },
                  { label: "New Subs (month)", value: summary.newSubscriptionsThisMonth },
                  { label: "Churned (month)", value: summary.churned },
                ].map((m) => (
                  <div key={m.label} className="flex items-center justify-between py-1.5 border-b border-surface-100">
                    <span className="text-xs text-text-secondary">{m.label}</span>
                    <span className="text-sm font-semibold text-text-primary">{m.value}</span>
                  </div>
                )) : <p className="text-sm text-text-tertiary">No data</p>}
              </CardContent>
            </Card>

            <Card className="lg:col-span-2">
              <CardHeader><CardTitle>Monthly MRR Breakdown</CardTitle><CardDescription>New, churned, and net-new MRR per month</CardDescription></CardHeader>
              <CardContent>
                {monthly.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No monthly data available</p> : (
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Month", "MRR", "New MRR", "Churned MRR", "Net New"].map((h) => (
                        <th key={h} className="text-left py-2 px-2 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {monthly.slice(-12).map((m, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-2 px-2 text-xs font-medium text-text-primary">{m.month}</td>
                          <td className="py-2 px-2 text-xs font-semibold text-text-primary">{fmt(m.mrr ?? 0)}</td>
                          <td className="py-2 px-2 text-xs text-green-600">+{fmt(m.newMrr ?? 0)}</td>
                          <td className="py-2 px-2 text-xs text-red-500">-{fmt(m.churnedMrr ?? 0)}</td>
                          <td className={`py-2 px-2 text-xs font-semibold ${(m.netNewMrr ?? 0) >= 0 ? "text-green-600" : "text-red-500"}`}>
                            {(m.netNewMrr ?? 0) >= 0 ? "+" : ""}{fmt(m.netNewMrr ?? 0)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
