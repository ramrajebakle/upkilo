"use client";

import React, { useState, useEffect } from "react";
import { TrendingUp, Users, DollarSign, Loader2, RefreshCw, Award } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useTenantCurrency } from '@/hooks/useTenantCurrency';

interface StaffPerf { staffId: string; staffName: string; bookings: number; revenue: number; avgRating?: number; utilizationPct?: number; cancellationRate?: number; }
interface CommissionRow { staffId: string; staffName: string; totalCommission: number; bookings: number; avgPerBooking: number; }


// Formats in the tenant's own currency; the previous hardcoded ₹ was wrong for any
// tenant not billing in rupees.
function money(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount);
  } catch {
    return `${currency} ${Math.round(amount).toLocaleString()}`;
  }
}

export default function StaffPerformancePage() {
  const currency = useTenantCurrency();
  const [tab, setTab] = useState<"performance" | "commissions">("performance");
  const [perf, setPerf] = useState<StaffPerf[]>([]);
  const [commissions, setCommissions] = useState<CommissionRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [dateRange, setDateRange] = useState({ start: "", end: "" });

  const load = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (dateRange.start) params.set("start", dateRange.start);
      if (dateRange.end) params.set("end", dateRange.end);
      const [pRes, cRes] = await Promise.all([
        apiClient.get(`/api/v1/performance/staff?${params}`).catch(() => ({ data: [] })),
        apiClient.get(`/api/v1/performance/commissions?${params}`).catch(() => ({ data: [] })),
      ]);
      setPerf(Array.isArray(pRes.data) ? pRes.data : pRes.data?.data ?? []);
      setCommissions(Array.isArray(cRes.data) ? cRes.data : cRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const topPerformer = perf.reduce<StaffPerf | null>((best, s) => !best || s.revenue > best.revenue ? s : best, null);

  return (
    <div className="space-y-6 animate-fade-in">
      {/* flex-wrap + gap: at 390px the title and the control cluster together measured 671px
          against a 390px viewport, so the page scrolled sideways (ux-guidelines #69, High).
          Without wrapping, `justify-between` has nowhere to put the overflow. */}
      <header className="flex flex-wrap items-end justify-between gap-4 border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Staff Performance <TrendingUp className="text-ai" size={22} /></h1>
          <p className="text-text-secondary mt-1">Track individual and team performance metrics across all staff.</p>
        </div>
        {/* The outer <header> wraps, but this control cluster did not, so it still pushed the
            page to 446px at a 390px viewport. Both levels have to wrap for the row to reflow. */}
        <div className="flex flex-wrap items-center gap-3">
          <input type="date" value={dateRange.start} onChange={(e) => setDateRange((p) => ({ ...p, start: e.target.value }))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          <span className="text-text-tertiary text-sm">→</span>
          <input type="date" value={dateRange.end} onChange={(e) => setDateRange((p) => ({ ...p, end: e.target.value }))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Apply</Button>
        </div>
      </header>

      {topPerformer && (
        <div className="flex items-center gap-4 p-4 rounded-xl bg-gradient-to-r from-amber-50 to-yellow-50 border border-amber-200">
          <Award className="h-8 w-8 text-warning-fg flex-shrink-0" />
          <div>
            <p className="text-xs text-warning-fg font-medium uppercase tracking-wide">Top Performer</p>
            <p className="text-lg font-bold text-text-primary">{topPerformer.staffName}</p>
            <p className="text-sm text-amber-700">{money(topPerformer.revenue, currency)} revenue · {topPerformer.bookings} bookings</p>
          </div>
        </div>
      )}

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {[{ k: "performance" as const, l: "Performance" }, { k: "commissions" as const, l: "Commissions" }].map((t) => (
          <button key={t.k} onClick={() => setTab(t.k)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.k ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>{t.l}</button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "performance" && (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Staff Member", "Bookings", "Revenue", "Avg Rating", "Utilization", "Cancellation"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {perf.map((s, i) => (
                      <tr key={s.staffId} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 flex items-center gap-2">
                          <div className="w-7 h-7 rounded-full bg-ai-subtle text-ai text-xs font-bold flex items-center justify-center flex-shrink-0">#{i + 1}</div>
                          <span className="text-sm font-medium text-text-primary">{s.staffName}</span>
                        </td>
                        <td className="py-3 px-4 text-sm text-text-secondary">{s.bookings}</td>
                        <td className="py-3 px-4 text-sm font-semibold text-text-primary">{money(s.revenue, currency)}</td>
                        <td className="py-3 px-4 text-sm">
                          {s.avgRating != null ? (
                            <span className="text-warning-fg font-medium">★ {s.avgRating.toFixed(1)}</span>
                          ) : "—"}
                        </td>
                        <td className="py-3 px-4">
                          {s.utilizationPct != null ? (
                            <div className="flex items-center gap-2">
                              <div className="w-16 bg-surface-200 rounded-full h-1.5">
                                <div className="bg-ai-500 h-1.5 rounded-full" style={{ width: `${Math.min(s.utilizationPct, 100)}%` }} />
                              </div>
                              <span className="text-xs text-text-secondary">{s.utilizationPct.toFixed(0)}%</span>
                            </div>
                          ) : "—"}
                        </td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{s.cancellationRate != null ? `${s.cancellationRate.toFixed(1)}%` : "—"}</td>
                      </tr>
                    ))}
                    {perf.length === 0 && <tr><td colSpan={6} className="text-center py-10 text-text-tertiary text-xs">No performance data for selected period</td></tr>}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}

          {tab === "commissions" && (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Staff Member", "Total Commission", "Bookings", "Avg / Booking"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {commissions.map((c) => (
                      <tr key={c.staffId} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-sm font-medium text-text-primary">{c.staffName}</td>
                        <td className="py-3 px-4 text-sm font-semibold text-success-fg">{money(c.totalCommission, currency)}</td>
                        <td className="py-3 px-4 text-sm text-text-secondary">{c.bookings}</td>
                        <td className="py-3 px-4 text-sm text-text-secondary">{c.avgPerBooking != null ? money(c.avgPerBooking, currency) : "—"}</td>
                      </tr>
                    ))}
                    {commissions.length === 0 && <tr><td colSpan={4} className="text-center py-10 text-text-tertiary text-xs">No commission data for selected period</td></tr>}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
