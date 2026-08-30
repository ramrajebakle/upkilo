"use client";

import React, { useState, useEffect } from "react";
import { CreditCard, CheckCircle2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { currencySymbol } from "@/lib/currency";

// This page previously called GET/PUT /api/v1/payments/razorpay/config and
// GET /api/v1/payments/razorpay/orders for a per-tenant Setup/Orders form — neither
// endpoint exists on the backend, so it 404'd silently (both calls were wrapped in
// `.catch(() => ...)`) and always rendered "Razorpay Not Configured" regardless of
// reality. Razorpay runs on a single platform-wide Upkilo account for now (see
// PublicBookingController.CreateRazorpayOrder / RazorpayService), so there is nothing for
// a tenant to configure here. This reads the existing, real payment-history endpoint
// instead and shows what actually happened.
interface RazorpayPayment {
  id: string;
  bookingId: string | null;
  clientName: string | null;
  amount: number;
  currency: string;
  status: string;
  createdAt: string;
}

export default function RazorpayPage() {
  const [payments, setPayments] = useState<RazorpayPayment[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get("/api/v1/payments/history?pageSize=100");
      const items = res.data?.data ?? [];
      setPayments(items.filter((p: any) => p.paymentMethod === "razorpay"));
    } catch {
      setPayments([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Razorpay Payments <CreditCard className="text-blue-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Accept INR payments via Razorpay (India-optimized).</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      <div className="flex items-center gap-3 p-4 rounded-xl border bg-green-50 border-green-200">
        <CheckCircle2 className="h-5 w-5 text-success-fg flex-shrink-0" />
        <div>
          <p className="text-sm font-semibold text-green-800">Razorpay is active</p>
          <p className="text-xs text-success-fg">
            Payments are processed through Upkilo&apos;s platform Razorpay account — nothing to set up here.
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Recent Razorpay payments</CardTitle>
          <CardDescription>Bookings paid via Razorpay checkout, newest first.</CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {loading ? (
            <div className="flex justify-center py-10 text-text-tertiary text-xs">Loading…</div>
          ) : (
            <table className="w-full text-sm">
              <thead><tr className="border-b border-surface-200">
                {["Client", "Amount", "Status", "Date"].map((h) => (
                  <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {payments.map((p) => (
                  <tr key={p.id} className="border-b border-surface-100 hover:bg-surface-50">
                    <td className="py-3 px-4 text-xs text-text-secondary">{p.clientName ?? "—"}</td>
                    <td className="py-3 px-4 text-xs font-semibold text-text-primary">{currencySymbol(p.currency)}{p.amount}</td>
                    <td className="py-3 px-4">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${p.status === "Succeeded" ? "text-green-600 bg-green-50" : p.status === "Pending" ? "text-blue-500 bg-blue-50" : "text-foreground-secondary bg-muted"}`}>{p.status}</span>
                    </td>
                    <td className="py-3 px-4 text-xs text-text-tertiary">{new Date(p.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
                {payments.length === 0 && <tr><td colSpan={4} className="text-center py-10 text-text-tertiary text-xs">No Razorpay payments yet</td></tr>}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
