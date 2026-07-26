"use client";

import React, { useEffect, useState } from "react";
import { BillingBanner } from "@/components/billing/BillingBanner";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { CurrencyFormatter } from "@/components/ui/CurrencyFormatter";
import { useTenantCurrency } from "@/hooks/useTenantCurrency";
import { CreditCard, Download, ExternalLink, ArrowRight, Wallet } from "lucide-react";
import api from "@/lib/api";

interface Invoice {
  id: string;
  invoiceNumber: string;
  customerName: string;
  totalAmount: number;
  status: string;
  issuedAt: string | null;
  currency: string;
}

function formatWhen(iso: string | null) {
  if (!iso) return "—";
  const d = new Date(iso);
  const now = new Date();
  const sameDay = d.toDateString() === now.toDateString();
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);

  const time = d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
  if (sameDay) return `Today, ${time}`;
  if (d.toDateString() === yesterday.toDateString()) return `Yesterday, ${time}`;
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

export default function TenantRevenuePage() {
  const currency = useTenantCurrency();

  const [monthRevenue, setMonthRevenue] = useState<number | null>(null);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    (async () => {
      const [revenueRes, invoiceRes] = await Promise.allSettled([
        api.analytics.revenue("month"),
        api.billing.getInvoices({ page: 1, pageSize: 5 }),
      ]);

      if (!active) return;

      if (revenueRes.status === "fulfilled") {
        setMonthRevenue(Number(revenueRes.value.data?.totalRevenue) || 0);
      }

      if (invoiceRes.status === "fulfilled") {
        const raw = invoiceRes.value.data?.data ?? invoiceRes.value.data ?? [];
        setInvoices(
          (Array.isArray(raw) ? raw : []).map((i: any) => ({
            id: i.id || i.invoiceNumber,
            invoiceNumber: i.invoiceNumber || "—",
            customerName: i.customerName || "Unknown",
            totalAmount: Number(i.totalAmount) || 0,
            status: i.status || "Pending",
            issuedAt: i.issuedAt || i.issueDate || null,
            currency: i.currency || currency,
          }))
        );
      }

      setLoading(false);
    })();

    return () => {
      active = false;
    };
  }, [currency]);

  const outstanding = invoices.filter((i) => i.status !== "Paid");
  const outstandingTotal = outstanding.reduce((sum, i) => sum + i.totalAmount, 0);

  return (
    <div className="space-y-6 animate-fade-in max-w-5xl">
      <BillingBanner
        context="tenant"
        title="Your Revenue"
        subtitle="Manage your customer subscriptions and payouts"
        status="active"
        statusText="Stripe Connected"
      />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="bg-surface-0 md:col-span-2">
          <CardHeader className="pb-4">
            <CardTitle>Revenue this month</CardTitle>
            <CardDescription>Collected across all completed bookings</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-end justify-between">
              <div>
                <div className="text-5xl font-bold text-text-primary mb-2">
                  {monthRevenue === null ? (
                    "—"
                  ) : (
                    <CurrencyFormatter amount={monthRevenue} currency={currency} />
                  )}
                </div>
                <div className="text-sm text-text-secondary">
                  Updated just now
                </div>
              </div>
              <Button variant="primary" className="bg-tenant-600 hover:bg-tenant-700">
                Withdraw Now
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="bg-surface-0 border-t-[4px] border-t-tenant-400">
          <CardHeader className="pb-2">
            <CardDescription className="flex items-center gap-2">
              <Wallet size={16} className="text-tenant-600" />
              Outstanding Invoices
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-text-primary mb-2">
              {loading ? "—" : <CurrencyFormatter amount={outstandingTotal} currency={currency} />}
            </div>
            <div className="text-sm text-text-secondary">
              {loading
                ? "Loading…"
                : outstanding.length === 0
                ? "Nothing outstanding"
                : `Across ${outstanding.length} invoice${outstanding.length === 1 ? "" : "s"}`}
            </div>
            <Button
              variant="outline"
              size="sm"
              className="w-full mt-4"
              rightIcon={<ArrowRight size={14} />}
              disabled={outstanding.length === 0}
            >
              Send Reminders
            </Button>
          </CardContent>
        </Card>
      </div>

      <h2 className="text-xl font-bold text-text-primary pt-4">Recent Transactions</h2>

      <Card className="bg-surface-0">
        <div className="divide-y divide-surface-100">
          {loading && (
            <div className="p-6 text-sm text-text-tertiary text-center">
              Loading transactions…
            </div>
          )}

          {!loading && invoices.length === 0 && (
            <div className="p-8 text-center">
              <p className="text-text-secondary font-medium">No transactions yet</p>
              <p className="text-sm text-text-tertiary mt-1">
                Invoices appear here once you start taking payments.
              </p>
            </div>
          )}

          {invoices.map((tx) => (
            <div
              key={tx.id}
              className="p-4 flex items-center justify-between hover:bg-surface-50 transition-colors"
            >
              <div className="flex items-center gap-4">
                <div
                  className={`p-2 rounded-lg ${
                    tx.status === "Paid"
                      ? "bg-success-50 text-success-600"
                      : "bg-warning-50 text-warning-600"
                  }`}
                >
                  <CreditCard size={20} />
                </div>
                <div>
                  <div className="font-medium text-text-primary">{tx.customerName}</div>
                  <div className="text-sm text-text-tertiary">
                    {tx.invoiceNumber} &middot; {formatWhen(tx.issuedAt)}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="text-right">
                  <div className="font-semibold text-text-primary">
                    <CurrencyFormatter amount={tx.totalAmount} currency={tx.currency} />
                  </div>
                  <div
                    className={`text-xs font-medium ${
                      tx.status === "Paid" ? "text-success-600" : "text-warning-600"
                    }`}
                  >
                    {tx.status}
                  </div>
                </div>
                <button className="p-2 text-text-tertiary hover:text-text-primary transition-colors">
                  <Download size={18} />
                </button>
              </div>
            </div>
          ))}
        </div>
        <div className="p-4 border-t border-surface-100 bg-surface-50 text-center">
          <button className="text-sm font-medium text-tenant-600 hover:text-tenant-700 flex items-center justify-center gap-1 w-full">
            View all transactions <ExternalLink size={14} />
          </button>
        </div>
      </Card>
    </div>
  );
}
