"use client";

import React, { useState, useEffect, useCallback } from "react";
import { CreditCard, ExternalLink, DollarSign, Loader2, RefreshCw, CheckCircle2, AlertTriangle, History } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface PayoutRecord {
  id: string;
  amount: number;
  status: "Pending" | "Processing" | "Paid" | "Failed";
  createdAt: string;
  paidAt?: string;
  staffName?: string;
}

const STATUS_COLOR: Record<string, string> = {
  Pending: "text-amber-600 bg-amber-50",
  Processing: "text-blue-600 bg-blue-50",
  Paid: "text-green-600 bg-green-50",
  Failed: "text-red-600 bg-red-50",
};

export default function StaffPayoutPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [payouts, setPayouts] = useState<PayoutRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [onboardingLoading, setOnboardingLoading] = useState(false);
  const [processingPayouts, setProcessingPayouts] = useState(false);
  const [isOnboarded, setIsOnboarded] = useState(false);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [histRes, allRes] = await Promise.all([
        apiClient.get("/api/v1/staffpayout/history").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/staffpayout/all-payouts").catch(() => ({ data: [] })),
      ]);
      const h: PayoutRecord[] = Array.isArray(histRes.data) ? histRes.data : histRes.data?.data ?? [];
      const a: PayoutRecord[] = Array.isArray(allRes.data) ? allRes.data : allRes.data?.data ?? [];
      const combined = [...h, ...a].filter((v, i, arr) => arr.findIndex((x) => x.id === v.id) === i);
      setPayouts(combined);
      setIsOnboarded(h.length > 0 || a.length > 0);
    } catch { toastError("Failed to load payout history"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const startOnboarding = async () => {
    setOnboardingLoading(true);
    try {
      const r = await apiClient.post("/api/v1/staffpayout/onboarding-url", {});
      const url = r.data?.url ?? r.data?.data?.url;
      if (url) window.open(url, "_blank");
      else toastError("No onboarding URL returned");
    } catch { toastError("Failed to start Stripe Connect onboarding"); }
    finally { setOnboardingLoading(false); }
  };

  const processPayouts = async () => {
    setProcessingPayouts(true);
    try {
      await apiClient.post("/api/v1/staffpayout/process-commissions", {});
      toastSuccess("Payouts queued for processing"); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to process payouts"); }
    finally { setProcessingPayouts(false); }
  };

  const totalPaid = payouts.filter((p) => p.status === "Paid").reduce((s, p) => s + p.amount, 0);
  const totalPending = payouts.filter((p) => p.status === "Pending").reduce((s, p) => s + p.amount, 0);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Staff Payouts <CreditCard className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Stripe Connect payouts for your team.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-xs font-medium text-text-secondary">Total paid</CardTitle>
            <CheckCircle2 className="h-4 w-4 text-success-fg" />
          </CardHeader>
          <CardContent><p className="text-2xl font-bold text-success-fg">${totalPaid.toFixed(2)}</p></CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-xs font-medium text-text-secondary">Pending</CardTitle>
            <AlertTriangle className="h-4 w-4 text-warning-fg" />
          </CardHeader>
          <CardContent><p className="text-2xl font-bold text-warning-fg">${totalPending.toFixed(2)}</p></CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-xs font-medium text-text-secondary">Total payouts</CardTitle>
            <History className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent><p className="text-2xl font-bold text-blue-500">{payouts.length}</p></CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><ExternalLink className="h-4 w-4" /> Stripe Connect Setup</CardTitle>
            <CardDescription>Connect Stripe to enable direct staff payouts</CardDescription></CardHeader>
          <CardContent className="space-y-3">
            <p className="text-sm text-text-secondary">
              Staff members receive their commissions directly via Stripe Express. Complete the onboarding to activate payouts.
            </p>
            <Button variant="primary" className="w-full"
              leftIcon={onboardingLoading ? <Loader2 size={14} className="animate-spin" /> : <ExternalLink size={14} />}
              onClick={startOnboarding} disabled={onboardingLoading}>
              {onboardingLoading ? "Loading…" : "Start Stripe Onboarding"}
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><DollarSign className="h-4 w-4" /> Process Commissions</CardTitle>
            <CardDescription>Batch-process all pending commission payouts</CardDescription></CardHeader>
          <CardContent className="space-y-3">
            <p className="text-sm text-text-secondary">
              {totalPending > 0 ? `$${totalPending.toFixed(2)} pending across ${payouts.filter((p) => p.status === "Pending").length} payout(s).` : "No pending payouts."}
            </p>
            <Button variant="primary" className="w-full bg-green-600 hover:bg-green-700 text-white"
              leftIcon={processingPayouts ? <Loader2 size={14} className="animate-spin" /> : <DollarSign size={14} />}
              onClick={processPayouts} disabled={processingPayouts || totalPending === 0}>
              {processingPayouts ? "Processing…" : "Process Pending Payouts"}
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><History className="h-4 w-4" /> Payout History</CardTitle>
          <CardDescription>{payouts.length} payouts</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : payouts.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <CreditCard className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No payouts yet</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Staff", "Amount", "Status", "Created", "Paid At"].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {payouts.map((p) => (
                    <tr key={p.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-3 font-medium text-text-primary">{p.staffName ?? "—"}</td>
                      <td className="py-3 px-3 font-bold text-success-fg">${p.amount.toFixed(2)}</td>
                      <td className="py-3 px-3"><span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLOR[p.status] ?? ""}`}>{p.status}</span></td>
                      <td className="py-3 px-3 text-xs text-text-secondary">{new Date(p.createdAt).toLocaleDateString()}</td>
                      <td className="py-3 px-3 text-xs text-text-secondary">{p.paidAt ? new Date(p.paidAt).toLocaleDateString() : "—"}</td>
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
