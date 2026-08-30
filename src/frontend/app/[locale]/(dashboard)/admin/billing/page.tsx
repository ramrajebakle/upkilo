"use client";

import React, { useState, useEffect } from "react";
import { CreditCard, DollarSign, RefreshCw, CheckCircle2, Loader2, TrendingUp } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface AdminInvoice { id: string; tenantId: string; tenantName?: string; amount: number; currency: string; status: "pending" | "paid" | "overdue" | "cancelled"; dueDate?: string; paidAt?: string; }
interface RevenueSummary { mrr: number; mrrTrend: number; arr: number; growth: number; }


// Platform-level billing is denominated in the platform's own currency; per-invoice rows use
// the currency stored on the invoice. Both previously printed a hardcoded ₹.
const PLATFORM_CURRENCY = "USD";

function money(amount: number, currency?: string | null) {
  const code = currency || PLATFORM_CURRENCY;
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency: code, maximumFractionDigits: 0 }).format(amount);
  } catch {
    return `${code} ${Math.round(amount).toLocaleString()}`;
  }
}

function compact(amount: number) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency: PLATFORM_CURRENCY, notation: "compact", maximumFractionDigits: 1 }).format(amount);
  } catch {
    return `${PLATFORM_CURRENCY} ${Math.round(amount).toLocaleString()}`;
  }
}

export default function AdminBillingPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [invoices, setInvoices] = useState<AdminInvoice[]>([]);
  const [summary, setSummary] = useState<RevenueSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState<string | null>(null);
  const [tab, setTab] = useState<"invoices" | "revenue">("invoices");

  const load = async () => {
    setLoading(true);
    try {
      const [invRes, mrrRes, trendRes] = await Promise.all([
        apiClient.get("/api/v1/admin/billing/invoices").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/admin/revenue/mrr").catch(() => ({ data: null })),
        apiClient.get("/api/v1/admin/revenue/growth").catch(() => ({ data: null })),
      ]);
      setInvoices(Array.isArray(invRes.data) ? invRes.data : invRes.data?.data ?? []);
      const mrr = mrrRes.data?.data ?? mrrRes.data;
      const growth = trendRes.data?.data ?? trendRes.data;
      if (mrr) setSummary({ mrr: mrr.value ?? mrr.mrr ?? 0, mrrTrend: mrr.trend ?? 0, arr: (mrr.value ?? mrr.mrr ?? 0) * 12, growth: growth?.rate ?? growth?.growthRate ?? 0 });
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const markPaid = async (id: string) => {
    setProcessing(id);
    try { await apiClient.post(`/api/v1/admin/billing/invoices/${id}/mark-paid`); toastSuccess("Invoice marked as paid"); load(); }
    catch { toastError("Failed to update"); }
    finally { setProcessing(null); }
  };

  const refund = async (id: string) => {
    setProcessing(`refund-${id}`);
    try { await apiClient.post(`/api/v1/admin/billing/invoices/${id}/refund`); toastSuccess("Refund issued"); load(); }
    catch (e: any) { toastError(e?.response?.data?.error ?? "Refund failed"); }
    finally { setProcessing(null); }
  };

  const statusCls = (s: string) => ({
    paid: "text-green-700 bg-green-50",
    pending: "text-amber-700 bg-amber-50",
    overdue: "text-red-700 bg-red-50",
    cancelled: "text-foreground-secondary bg-muted",
  })[s] ?? "text-foreground-secondary bg-muted";

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Admin Billing <CreditCard className="text-ai" size={22} /></h1>
          <p className="text-text-secondary mt-1">Manage tenant invoices, refunds, and platform revenue metrics.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {summary && (
        <div className="grid grid-cols-4 gap-4">
          {[
            { label: "MRR", value: compact(summary.mrr), cls: "text-success-fg" },
            { label: "ARR", value: compact(summary.arr), cls: "text-blue-600" },
            { label: "MRR Trend", value: `${summary.mrrTrend >= 0 ? "+" : ""}${summary.mrrTrend?.toFixed(1)}%`, cls: summary.mrrTrend >= 0 ? "text-success-fg" : "text-danger-fg" },
            { label: "Growth", value: `${summary.growth >= 0 ? "+" : ""}${summary.growth?.toFixed(1)}%`, cls: summary.growth >= 0 ? "text-success-fg" : "text-danger-fg" },
          ].map((m) => (
            <Card key={m.label}><CardContent className="pt-4 pb-4">
              <p className="text-xs text-text-tertiary font-medium">{m.label}</p>
              <p className={`text-2xl font-bold mt-1 ${m.cls}`}>{m.value}</p>
            </CardContent></Card>
          ))}
        </div>
      )}

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {[{ k: "invoices" as const, l: "Invoices" }, { k: "revenue" as const, l: "Revenue Trend" }].map((t) => (
          <button key={t.k} onClick={() => setTab(t.k)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.k ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>{t.l}</button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "invoices" && (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Tenant", "Amount", "Status", "Due Date", "Paid At", "Actions"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {invoices.map((inv) => (
                      <tr key={inv.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-sm font-medium text-text-primary">{inv.tenantName ?? inv.tenantId?.slice(0, 8)}</td>
                        <td className="py-3 px-4 text-sm font-semibold text-text-primary">{money(inv.amount, inv.currency)}</td>
                        <td className="py-3 px-4"><span className={`text-xs font-medium px-2 py-0.5 rounded-full ${statusCls(inv.status)}`}>{inv.status}</span></td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{inv.dueDate ? new Date(inv.dueDate).toLocaleDateString() : "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{inv.paidAt ? new Date(inv.paidAt).toLocaleDateString() : "—"}</td>
                        <td className="py-3 px-4">
                          <div className="flex gap-1">
                            {inv.status === "pending" || inv.status === "overdue" ? (
                              <Button variant="primary" size="sm"
                                leftIcon={processing === inv.id ? <Loader2 size={10} className="animate-spin" /> : <CheckCircle2 size={10} />}
                                onClick={() => markPaid(inv.id)} disabled={!!processing}>Mark Paid</Button>
                            ) : null}
                            {inv.status === "paid" ? (
                              <Button variant="outline" size="sm"
                                leftIcon={processing === `refund-${inv.id}` ? <Loader2 size={10} className="animate-spin" /> : <RefreshCw size={10} />}
                                onClick={() => refund(inv.id)} disabled={!!processing}>Refund</Button>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    ))}
                    {invoices.length === 0 && <tr><td colSpan={6} className="text-center py-10 text-text-tertiary text-xs">No invoices found</td></tr>}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}

          {tab === "revenue" && (
            <RevenueTab />
          )}
        </>
      )}
    </div>
  );
}

function RevenueTab() {
  const [trend, setTrend] = useState<{ period: string; mrr: number }[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient.get("/api/v1/admin/revenue/trend").catch(() => ({ data: [] })).then((r) => {
      setTrend(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    }).finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>;

  return (
    <Card>
      <CardHeader><CardTitle className="flex items-center gap-2"><TrendingUp size={16} /> MRR Trend</CardTitle></CardHeader>
      <CardContent className="p-0">
        <table className="w-full text-sm">
          <thead><tr className="border-b border-surface-200">
            {["Period", "MRR"].map((h) => <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>)}
          </tr></thead>
          <tbody>
            {trend.map((r) => (
              <tr key={r.period} className="border-b border-surface-100 hover:bg-surface-50">
                <td className="py-3 px-4 text-sm text-text-secondary">{r.period}</td>
                <td className="py-3 px-4 text-sm font-semibold text-text-primary">{money(r.mrr, PLATFORM_CURRENCY)}</td>
              </tr>
            ))}
            {trend.length === 0 && <tr><td colSpan={2} className="text-center py-8 text-text-tertiary text-xs">No trend data</td></tr>}
          </tbody>
        </table>
      </CardContent>
    </Card>
  );
}
