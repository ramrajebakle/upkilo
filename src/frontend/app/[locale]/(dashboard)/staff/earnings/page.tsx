"use client";

import React, { useState, useEffect, useCallback } from "react";
import { DollarSign, TrendingUp, Clock, Award, Loader2, RefreshCw, Calendar } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface EarningsEntry {
  date: string;
  serviceRevenue: number;
  commission: number;
  tips: number;
  totalHours?: number;
  bookingCount?: number;
}

interface EarningsSummary {
  totalCommission: number;
  totalTips: number;
  totalRevenue: number;
  totalHours: number;
  avgPerHour: number;
}

export default function StaffEarningsPage() {
  const { error: toastError } = useToast();
  const [entries, setEntries] = useState<EarningsEntry[]>([]);
  const [summary, setSummary] = useState<EarningsSummary>({ totalCommission: 0, totalTips: 0, totalRevenue: 0, totalHours: 0, avgPerHour: 0 });
  const [loading, setLoading] = useState(true);
  const [startDate, setStartDate] = useState(() => { const d = new Date(); d.setDate(1); return d.toISOString().split("T")[0]; });
  const [endDate] = useState(() => new Date().toISOString().split("T")[0]);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/attendance/my-timesheet", { params: { startDate, endDate } }).catch(() => ({ data: [] }));
      const commRes = await apiClient.get("/api/v1/staff/commissions", { params: { startDate, endDate } }).catch(() => ({ data: [] }));
      const tipsRes = await apiClient.get("/api/v1/tips/my-tips", { params: { startDate } }).catch(() => ({ data: [] }));

      const ts: EarningsEntry[] = (Array.isArray(r.data) ? r.data : r.data?.data ?? []).map((e: any) => ({
        date: e.clockInTime ?? e.date,
        serviceRevenue: e.serviceRevenue ?? 0,
        commission: e.commission ?? 0,
        tips: e.tips ?? 0,
        totalHours: e.totalMinutes ? e.totalMinutes / 60 : e.totalHours,
        bookingCount: e.bookingCount,
      }));
      setEntries(ts);

      const totalCommission = (Array.isArray(commRes.data) ? commRes.data : commRes.data?.data ?? []).reduce((s: number, c: any) => s + (c.amount ?? 0), 0);
      const totalTips = (Array.isArray(tipsRes.data) ? tipsRes.data : tipsRes.data?.data ?? []).reduce((s: number, t: any) => s + (t.amount ?? 0), 0);
      const totalHours = ts.reduce((s, e) => s + (e.totalHours ?? 0), 0);
      setSummary({
        totalCommission,
        totalTips,
        totalRevenue: totalCommission + totalTips,
        totalHours,
        avgPerHour: totalHours > 0 ? (totalCommission + totalTips) / totalHours : 0,
      });
    } catch { toastError("Failed to load earnings"); }
    finally { setLoading(false); }
  }, [startDate, endDate]);

  useEffect(() => { fetch(); }, [fetch]);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">My Earnings <DollarSign className="text-green-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Your commissions, tips, and hours worked.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Commissions", value: `$${summary.totalCommission.toFixed(2)}`, icon: Award, color: "text-primary-500" },
          { label: "Tips", value: `$${summary.totalTips.toFixed(2)}`, icon: DollarSign, color: "text-red-400" },
          { label: "Total earned", value: `$${summary.totalRevenue.toFixed(2)}`, icon: TrendingUp, color: "text-green-500" },
          { label: "Hours worked", value: `${summary.totalHours.toFixed(1)}h`, icon: Clock, color: "text-blue-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-xs font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent><p className={`text-2xl font-bold ${s.color}`}>{s.value}</p></CardContent>
          </Card>
        ))}
      </div>

      <div className="flex items-center gap-3">
        <Calendar className="h-4 w-4 text-text-tertiary" />
        <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        <span className="text-text-tertiary text-sm">to {new Date(endDate).toLocaleDateString([], { month: "short", day: "numeric" })}</span>
      </div>

      <Card>
        <CardHeader><CardTitle>Earnings Breakdown</CardTitle><CardDescription>Daily summary</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : entries.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <DollarSign className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No earnings data for this period</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Date", "Hours", "Bookings", "Commission", "Tips", "Total"].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {entries.map((e, i) => (
                    <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-3 text-text-secondary text-xs">{new Date(e.date).toLocaleDateString([], { month: "short", day: "numeric" })}</td>
                      <td className="py-3 px-3 text-text-primary">{e.totalHours ? `${e.totalHours.toFixed(1)}h` : "—"}</td>
                      <td className="py-3 px-3 text-text-secondary">{e.bookingCount ?? "—"}</td>
                      <td className="py-3 px-3 text-primary-600 font-medium">${e.commission.toFixed(2)}</td>
                      <td className="py-3 px-3 text-red-500 font-medium">${e.tips.toFixed(2)}</td>
                      <td className="py-3 px-3 text-green-600 font-bold">${(e.commission + e.tips).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </CardContent>
      </Card>
    </div>
  );
}
