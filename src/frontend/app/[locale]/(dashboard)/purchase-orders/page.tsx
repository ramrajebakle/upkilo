"use client";

import React, { useState, useEffect, useCallback } from "react";
import { ClipboardList, Plus, Send, CheckCircle2, XCircle, PackageCheck, Loader2, RefreshCw, ChevronRight } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface PurchaseOrder {
  id: string;
  supplierName?: string;
  supplierId?: string;
  status: "Draft" | "Submitted" | "Received" | "Cancelled";
  totalAmount: number;
  createdAt: string;
  expectedDelivery?: string;
  items?: { productName: string; quantity: number; unitCost: number }[];
}

const STATUS_CONFIG: Record<string, { color: string; bg: string; icon: React.ComponentType<{ className?: string }> }> = {
  Draft:     { color: "text-foreground-secondary",   bg: "bg-muted",   icon: ClipboardList },
  Submitted: { color: "text-blue-600",   bg: "bg-blue-50",   icon: Send },
  Received:  { color: "text-green-600",  bg: "bg-green-50",  icon: PackageCheck },
  Cancelled: { color: "text-red-600",    bg: "bg-red-50",    icon: XCircle },
};

export default function PurchaseOrdersPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<PurchaseOrder | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("all");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/purchase-orders").catch(() => ({ data: [] }));
      const d: PurchaseOrder[] = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setOrders(d);
    } catch { toastError("Failed to load purchase orders"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const doAction = async (id: string, action: "submit" | "receive" | "cancel") => {
    setActionLoading(id + action);
    try {
      await apiClient.post(`/api/purchase-orders/${id}/${action}`, {});
      toastSuccess(`Order ${action}ted`);
      fetch(); if (selected?.id === id) setSelected(null);
    } catch (e: any) { toastError(e?.response?.data?.error ?? `Failed to ${action}`); }
    finally { setActionLoading(null); }
  };

  const stats = {
    draft: orders.filter((o) => o.status === "Draft").length,
    submitted: orders.filter((o) => o.status === "Submitted").length,
    received: orders.filter((o) => o.status === "Received").length,
    totalValue: orders.filter((o) => o.status !== "Cancelled").reduce((s, o) => s + o.totalAmount, 0),
  };

  const filtered = orders.filter((o) => statusFilter === "all" || o.status === statusFilter);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Purchase Orders <ClipboardList className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Track and manage supplier purchase orders.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
          <Button variant="primary" leftIcon={<Plus size={14} />}>New Order</Button>
        </div>
      </header>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Draft", value: stats.draft, color: "text-foreground-secondary" },
          { label: "Submitted", value: stats.submitted, color: "text-blue-600" },
          { label: "Received", value: stats.received, color: "text-success-fg" },
          { label: "Total value", value: `$${stats.totalValue.toFixed(0)}`, color: "text-text-primary" },
        ].map((s) => (
          <Card key={s.label}>
            <CardContent className="pt-5">
              <p className="text-xs text-text-secondary font-medium mb-1">{s.label}</p>
              <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="flex gap-2">
        {["all", "Draft", "Submitted", "Received", "Cancelled"].map((f) => (
          <button key={f} onClick={() => setStatusFilter(f)}
            className={cn("px-3 py-1.5 rounded-lg text-sm font-medium transition-colors",
              statusFilter === f ? "bg-ai-500 text-white" : "bg-surface-100 text-text-secondary hover:bg-surface-200")}>
            {f === "all" ? "All" : f}
          </button>
        ))}
      </div>

      <div className={cn("grid gap-6", selected ? "lg:grid-cols-3" : "grid-cols-1")}>
        <Card className={selected ? "lg:col-span-2" : undefined}>
          <CardHeader><CardTitle>Orders</CardTitle><CardDescription>{filtered.length} orders</CardDescription></CardHeader>
          <CardContent>
            {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
              : filtered.length === 0 ? (
                <div className="text-center py-10 text-text-tertiary">
                  <ClipboardList className="h-10 w-10 mx-auto mb-3 opacity-20" />
                  <p className="font-medium">No purchase orders found</p>
                </div>
              ) : (
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["PO #", "Supplier", "Date", "Total", "Status", ""].map((h) => (
                      <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {filtered.map((o) => {
                      const cfg = STATUS_CONFIG[o.status] ?? STATUS_CONFIG.Draft;
                      return (
                        <tr key={o.id} onClick={() => setSelected(selected?.id === o.id ? null : o)}
                          className={cn("border-b border-surface-100 hover:bg-surface-50 transition-colors cursor-pointer", selected?.id === o.id && "bg-ai-subtle")}>
                          <td className="py-3 px-3 font-mono text-xs text-text-primary">#{o.id.slice(-8).toUpperCase()}</td>
                          <td className="py-3 px-3 font-medium text-text-primary">{o.supplierName ?? "—"}</td>
                          <td className="py-3 px-3 text-xs text-text-secondary">{new Date(o.createdAt).toLocaleDateString([], { month: "short", day: "numeric" })}</td>
                          <td className="py-3 px-3 font-semibold text-text-primary">${o.totalAmount.toFixed(2)}</td>
                          <td className="py-3 px-3"><span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{o.status}</span></td>
                          <td className="py-3 px-3"><ChevronRight className="h-4 w-4 text-text-tertiary" /></td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
          </CardContent>
        </Card>

        {selected && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between text-sm">
                <span>PO #{selected.id.slice(-8).toUpperCase()}</span>
                <button onClick={() => setSelected(null)} className="text-text-tertiary hover:text-text-primary">×</button>
              </CardTitle>
              <CardDescription>{selected.supplierName ?? "No supplier"}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2 text-sm">
                {[
                  { label: "Status", value: selected.status },
                  { label: "Total", value: `$${selected.totalAmount.toFixed(2)}` },
                  { label: "Created", value: new Date(selected.createdAt).toLocaleDateString() },
                  { label: "Expected", value: selected.expectedDelivery ? new Date(selected.expectedDelivery).toLocaleDateString() : "—" },
                ].map(({ label, value }) => (
                  <div key={label} className="flex justify-between">
                    <span className="text-text-secondary">{label}</span>
                    <span className="font-medium text-text-primary">{value}</span>
                  </div>
                ))}
              </div>
              {selected.items && selected.items.length > 0 && (
                <div className="border-t border-surface-200 pt-3 space-y-1.5">
                  <p className="text-xs font-semibold text-text-tertiary uppercase mb-2">Items</p>
                  {selected.items.map((item, i) => (
                    <div key={i} className="flex justify-between text-xs">
                      <span className="text-text-primary">{item.productName} × {item.quantity}</span>
                      <span className="text-text-secondary">${(item.quantity * item.unitCost).toFixed(2)}</span>
                    </div>
                  ))}
                </div>
              )}
              <div className="border-t border-surface-200 pt-3 flex flex-col gap-2">
                {selected.status === "Draft" && (
                  <Button variant="primary" size="sm" className="w-full"
                    leftIcon={actionLoading === selected.id + "submit" ? <Loader2 size={13} className="animate-spin" /> : <Send size={13} />}
                    onClick={() => doAction(selected.id, "submit")} disabled={!!actionLoading}>Submit Order</Button>
                )}
                {selected.status === "Submitted" && (
                  <Button variant="primary" size="sm" className="w-full bg-green-600 hover:bg-green-700 text-white"
                    leftIcon={actionLoading === selected.id + "receive" ? <Loader2 size={13} className="animate-spin" /> : <PackageCheck size={13} />}
                    onClick={() => doAction(selected.id, "receive")} disabled={!!actionLoading}>Mark Received</Button>
                )}
                {(selected.status === "Draft" || selected.status === "Submitted") && (
                  <Button variant="outline" size="sm" className="w-full text-danger-fg border-red-200 hover:bg-red-50"
                    leftIcon={actionLoading === selected.id + "cancel" ? <Loader2 size={13} className="animate-spin" /> : <XCircle size={13} />}
                    onClick={() => doAction(selected.id, "cancel")} disabled={!!actionLoading}>Cancel</Button>
                )}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
