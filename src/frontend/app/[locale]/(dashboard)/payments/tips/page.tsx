"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Heart, DollarSign, Users, TrendingUp, Loader2, RefreshCw, Calendar } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Tip {
  id: string;
  amount: number;
  bookingId?: string;
  staffId?: string;
  staffName?: string;
  clientName?: string;
  createdAt: string;
  paymentMethod?: string;
}

interface TipStats {
  totalTips: number;
  totalAmount: number;
  averageTip: number;
  topStaffName?: string;
}

export default function TipsPage() {
  const { error: toastError } = useToast();
  const [tips, setTips] = useState<Tip[]>([]);
  const [stats, setStats] = useState<TipStats>({ totalTips: 0, totalAmount: 0, averageTip: 0 });
  const [loading, setLoading] = useState(true);
  const [staffFilter, setStaffFilter] = useState("all");
  const [startDate, setStartDate] = useState(() => {
    const d = new Date(); d.setDate(d.getDate() - 30); return d.toISOString().split("T")[0];
  });

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string> = { startDate };
      if (staffFilter !== "all") params.staffId = staffFilter;
      const r = await apiClient.get("/api/v1/tips", { params }).catch(() => ({ data: [] }));
      const d: Tip[] = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setTips(d);
      const total = d.reduce((s, t) => s + t.amount, 0);
      const topMap: Record<string, number> = {};
      d.forEach((t) => { if (t.staffName) topMap[t.staffName] = (topMap[t.staffName] ?? 0) + t.amount; });
      const topEntry = Object.entries(topMap).sort((a, b) => b[1] - a[1])[0];
      setStats({ totalTips: d.length, totalAmount: total, averageTip: d.length ? total / d.length : 0, topStaffName: topEntry?.[0] });
    } catch { toastError("Failed to load tips"); }
    finally { setLoading(false); }
  }, [startDate, staffFilter]);

  useEffect(() => { fetch(); }, [fetch]);

  const uniqueStaff = [...new Set(tips.map((t) => t.staffName).filter(Boolean))] as string[];

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Tips <Heart className="text-red-400" size={22} /></h1>
          <p className="text-text-secondary mt-1">Gratuity received by your staff.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Total tips", value: stats.totalTips, icon: Heart, color: "text-red-400" },
          { label: "Total amount", value: `$${stats.totalAmount.toFixed(2)}`, icon: DollarSign, color: "text-green-500" },
          { label: "Average tip", value: `$${stats.averageTip.toFixed(2)}`, icon: TrendingUp, color: "text-blue-500" },
          { label: "Top earner", value: stats.topStaffName ?? "—", icon: Users, color: "text-purple-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-xs font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent><p className={`text-xl font-bold ${s.color}`}>{s.value}</p></CardContent>
          </Card>
        ))}
      </div>

      <div className="flex flex-wrap gap-3 items-center">
        <Calendar className="h-4 w-4 text-text-tertiary" />
        <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        <select value={staffFilter} onChange={(e) => setStaffFilter(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
          <option value="all">All staff</option>
          {uniqueStaff.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Heart className="h-4 w-4 text-red-400" /> Tip History</CardTitle>
          <CardDescription>{tips.length} tips in period</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : tips.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <Heart className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No tips recorded in this period</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Date", "Staff", "Client", "Amount", "Method"].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {tips.map((t) => (
                    <tr key={t.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-3 text-text-secondary text-xs">{new Date(t.createdAt).toLocaleDateString([], { month: "short", day: "numeric" })}</td>
                      <td className="py-3 px-3 font-medium text-text-primary">{t.staffName ?? "—"}</td>
                      <td className="py-3 px-3 text-text-secondary">{t.clientName ?? "—"}</td>
                      <td className="py-3 px-3 font-bold text-green-600">${t.amount.toFixed(2)}</td>
                      <td className="py-3 px-3 text-text-tertiary text-xs">{t.paymentMethod ?? "—"}</td>
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
