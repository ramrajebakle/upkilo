"use client";

import React, { useState, useEffect } from "react";
import { CreditCard, TrendingUp, DollarSign, Loader2, RefreshCw, CheckCircle2, AlertCircle } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface FinanceStatus { staffPayoutsEnabled: boolean; bnplEnabled: boolean; advanceEnabled: boolean; walletBalance: number; pendingPayouts: number; totalAdvancedThisMonth: number; }
interface StaffPayout { staffId: string; staffName: string; amount: number; status: string; scheduledDate: string; }

type Tab = "overview" | "payouts" | "advance";

export default function EmbeddedFinancePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [tab, setTab] = useState<Tab>("overview");
  const [status, setStatus] = useState<FinanceStatus | null>(null);
  const [payouts, setPayouts] = useState<StaffPayout[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      apiClient.get("/api/v1/embedded-finance/status").catch(() => ({ data: null })),
      apiClient.get("/api/v1/embedded-finance/payouts").catch(() => ({ data: [] })),
    ]).then(([s, p]) => {
      setStatus(s.data?.data ?? s.data ?? null);
      setPayouts(Array.isArray(p.data) ? p.data : p.data?.data ?? []);
    }).finally(() => setLoading(false));
  }, []);

  const triggerPayout = async () => {
    try {
      await apiClient.post("/api/v1/embedded-finance/payouts/run");
      toastSuccess("Payout run initiated");
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Payout failed"); }
  };

  const TABS = [
    { key: "overview" as const, label: "Overview" },
    { key: "payouts" as const, label: `Staff Payouts (${payouts.length})` },
    { key: "advance" as const, label: "Revenue Advance" },
  ];

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Embedded Finance <CreditCard className="text-ai" size={22} /></h1>
          <p className="text-text-secondary mt-1">Staff payouts, BNPL, and revenue advance financing.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={() => location.reload()} disabled={loading}>Refresh</Button>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {status && (
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: "Wallet Balance", value: `$${(status.walletBalance ?? 0).toLocaleString()}`, color: "text-success-fg", icon: <DollarSign className="h-5 w-5 text-green-400" /> },
                { label: "Pending Payouts", value: `$${(status.pendingPayouts ?? 0).toLocaleString()}`, color: "text-warning-fg", icon: <CreditCard className="h-5 w-5 text-amber-400" /> },
                { label: "Advances This Month", value: `$${(status.totalAdvancedThisMonth ?? 0).toLocaleString()}`, color: "text-text-primary", icon: <TrendingUp className="h-5 w-5 text-text-tertiary" /> },
                { label: "Payout Staff", value: payouts.length.toString(), color: "text-text-primary", icon: <CheckCircle2 className="h-5 w-5 text-text-tertiary" /> },
              ].map((s) => (
                <Card key={s.label}><CardContent className="pt-5 flex items-center gap-3">{s.icon}<div><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-xl font-bold mt-0.5 ${s.color}`}>{s.value}</p></div></CardContent></Card>
              ))}
            </div>
          )}

          <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-md">
            {TABS.map((t) => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.key ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
                {t.label}
              </button>
            ))}
          </div>

          {tab === "overview" && status && (
            <div className="space-y-4">
              <Card>
                <CardHeader><CardTitle>Feature Status</CardTitle></CardHeader>
                <CardContent className="space-y-3">
                  {[
                    { label: "Staff Automatic Payouts", enabled: status.staffPayoutsEnabled, desc: "Automatically pay staff via Stripe Connect on schedule" },
                    { label: "Buy Now Pay Later (BNPL)", enabled: status.bnplEnabled, desc: "Allow clients to split payments over time" },
                    { label: "Revenue Advance", enabled: status.advanceEnabled, desc: "Access early working capital based on your revenue" },
                  ].map((f) => (
                    <div key={f.label} className="flex items-start gap-3 p-3 rounded-xl bg-surface-50 border border-surface-200">
                      {f.enabled ? <CheckCircle2 className="h-4 w-4 text-success-fg mt-0.5 flex-shrink-0" /> : <AlertCircle className="h-4 w-4 text-text-tertiary mt-0.5 flex-shrink-0" />}
                      <div>
                        <p className="text-sm font-medium text-text-primary">{f.label}</p>
                        <p className="text-xs text-text-tertiary">{f.desc}</p>
                      </div>
                      <span className={`ml-auto text-xs font-medium px-2 py-0.5 rounded-full flex-shrink-0 ${f.enabled ? "text-green-600 bg-green-50" : "text-foreground-secondary bg-muted"}`}>
                        {f.enabled ? "Active" : "Inactive"}
                      </span>
                    </div>
                  ))}
                </CardContent>
              </Card>
            </div>
          )}

          {tab === "payouts" && (
            <div className="space-y-4">
              <div className="flex justify-end">
                <Button variant="primary" leftIcon={<DollarSign size={14} />} onClick={triggerPayout}>Run Payout Now</Button>
              </div>
              <Card>
                <CardContent className="p-0">
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Staff", "Amount", "Status", "Scheduled Date"].map((h) => (
                        <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {payouts.map((p, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-3 px-4 text-xs font-medium text-text-primary">{p.staffName}</td>
                          <td className="py-3 px-4 text-xs font-semibold text-success-fg">${p.amount?.toLocaleString()}</td>
                          <td className="py-3 px-4">
                            <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${p.status === "Completed" ? "text-green-600 bg-green-50" : p.status === "Pending" ? "text-amber-600 bg-amber-50" : "text-foreground-secondary bg-muted"}`}>{p.status}</span>
                          </td>
                          <td className="py-3 px-4 text-xs text-text-tertiary">{new Date(p.scheduledDate).toLocaleDateString()}</td>
                        </tr>
                      ))}
                      {payouts.length === 0 && <tr><td colSpan={4} className="text-center py-10 text-text-tertiary text-xs">No payouts scheduled</td></tr>}
                    </tbody>
                  </table>
                </CardContent>
              </Card>
            </div>
          )}

          {tab === "advance" && (
            <Card>
              <CardHeader><CardTitle>Revenue Advance</CardTitle><CardDescription>Access working capital based on your future revenue</CardDescription></CardHeader>
              <CardContent className="space-y-4">
                <div className="p-4 rounded-xl bg-ai-subtle border border-ai/25">
                  <p className="text-sm font-medium text-ai-800">Based on your revenue history, you may qualify for an advance</p>
                  <p className="text-xs text-ai mt-1">Contact our finance team or use the API endpoint to apply: <code className="bg-card px-1 py-0.5 rounded text-xs">POST /api/v1/embedded-finance/advance/apply</code></p>
                </div>
                <Button variant="primary" leftIcon={<TrendingUp size={14} />}
                  onClick={async () => {
                    try { await apiClient.post("/api/v1/embedded-finance/advance/apply"); toastSuccess("Advance application submitted"); }
                    catch (e: any) { toastError(e?.response?.data?.error ?? "Application failed"); }
                  }}>Apply for Revenue Advance</Button>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
