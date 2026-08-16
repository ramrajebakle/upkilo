"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  ShoppingBag, Plus, Search, RefreshCw, Loader2,
  CheckCircle2, Clock, XCircle, DollarSign, Package,
  User, ChevronRight, Filter, Eye,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface OrderItem {
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

interface Order {
  id: string;
  clientId?: string;
  clientName?: string;
  status: "Draft" | "Paid" | "Pending" | "Cancelled" | "Refunded";
  totalAmount: number;
  issueDate: string;
  items: OrderItem[];
}

interface OrderStats {
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  avgOrderValue: number;
}

const STATUS_CONFIG: Record<string, { label: string; color: string; bg: string; icon: React.ComponentType<{ className?: string }> }> = {
  Draft:     { label: "Draft",     color: "text-gray-500",   bg: "bg-gray-50",   icon: Clock },
  Paid:      { label: "Paid",      color: "text-green-600",  bg: "bg-green-50",  icon: CheckCircle2 },
  Pending:   { label: "Pending",   color: "text-amber-600",  bg: "bg-amber-50",  icon: Clock },
  Cancelled: { label: "Cancelled", color: "text-red-600",    bg: "bg-red-50",    icon: XCircle },
  Refunded:  { label: "Refunded",  color: "text-purple-600", bg: "bg-purple-50", icon: RefreshCw },
};

function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? STATUS_CONFIG.Draft;
  return (
    <span className={cn("inline-flex items-center gap-1 text-xs font-medium px-2.5 py-0.5 rounded-full", cfg.color, cfg.bg)}>
      <cfg.icon className="h-3 w-3" />
      {cfg.label}
    </span>
  );
}

export default function OrdersPage() {
  const { error: toastError } = useToast();
  const [orders, setOrders] = useState<Order[]>([]);
  const [stats, setStats] = useState<OrderStats>({ totalOrders: 0, totalRevenue: 0, pendingOrders: 0, avgOrderValue: 0 });
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [selected, setSelected] = useState<Order | null>(null);

  const fetchOrders = useCallback(async () => {
    setLoading(true);
    try {
      const res = await apiClient.get("/api/v1/orders").catch(() => ({ data: [] }));
      const data: Order[] = Array.isArray(res.data) ? res.data : res.data?.data ?? [];
      setOrders(data);

      const paid = data.filter((o) => o.status === "Paid");
      const pending = data.filter((o) => o.status === "Pending");
      const totalRevenue = paid.reduce((sum, o) => sum + (o.totalAmount ?? 0), 0);
      setStats({
        totalOrders: data.length,
        totalRevenue,
        pendingOrders: pending.length,
        avgOrderValue: paid.length > 0 ? totalRevenue / paid.length : 0,
      });
    } catch {
      toastError("Failed to load orders");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  const filtered = orders.filter((o) => {
    const matchesStatus = statusFilter === "all" || o.status === statusFilter;
    const matchesSearch =
      !search ||
      o.clientName?.toLowerCase().includes(search.toLowerCase()) ||
      o.id.toLowerCase().includes(search.toLowerCase());
    return matchesStatus && matchesSearch;
  });

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            Orders
            <ShoppingBag className="text-text-tertiary" size={22} />
          </h1>
          <p className="text-text-secondary mt-1">
            Retail and product orders from your store.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={15} />} onClick={fetchOrders} disabled={loading}>
            Refresh
          </Button>
          <Button variant="primary" leftIcon={<Plus size={15} />}>
            New Order
          </Button>
        </div>
      </header>

      {/* Stats */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Total orders", value: stats.totalOrders, icon: ShoppingBag, color: "text-blue-500" },
          { label: "Revenue (paid)", value: `$${stats.totalRevenue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, icon: DollarSign, color: "text-green-500" },
          { label: "Pending", value: stats.pendingOrders, icon: Clock, color: "text-amber-500" },
          { label: "Avg order value", value: `$${stats.avgOrderValue.toFixed(2)}`, icon: Package, color: "text-primary-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-xs font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent>
              <p className={`text-xl font-bold ${s.color}`}>{s.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 items-center">
        <Filter className="h-4 w-4 text-text-tertiary" />
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
          <input
            type="text"
            placeholder="Search by client or order ID…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 w-64"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
        >
          <option value="all">All statuses</option>
          {Object.keys(STATUS_CONFIG).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>

      {/* Orders table + detail panel */}
      <div className={cn("grid gap-6", selected ? "lg:grid-cols-3" : "grid-cols-1")}>
        <Card className={selected ? "lg:col-span-2" : undefined}>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ShoppingBag className="h-4 w-4" /> Order List
            </CardTitle>
            <CardDescription>{filtered.length} orders</CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex items-center justify-center py-12">
                <Loader2 className="h-6 w-6 animate-spin text-text-tertiary" />
              </div>
            ) : filtered.length === 0 ? (
              <div className="text-center py-12 text-text-tertiary">
                <ShoppingBag className="h-10 w-10 mx-auto mb-3 opacity-30" />
                <p className="font-medium">No orders found</p>
                <p className="text-sm mt-1">
                  {orders.length === 0 ? "Orders from your store will appear here" : "Try adjusting your filters"}
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-surface-200">
                      <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Order</th>
                      <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Client</th>
                      <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Date</th>
                      <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Total</th>
                      <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Status</th>
                      <th className="py-3 px-4" />
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.map((o) => (
                      <tr
                        key={o.id}
                        className={cn(
                          "border-b border-surface-100 hover:bg-surface-50 transition-colors cursor-pointer",
                          selected?.id === o.id && "bg-ai-50 dark:bg-ai-950/20"
                        )}
                        onClick={() => setSelected(selected?.id === o.id ? null : o)}
                      >
                        <td className="py-3 px-4 font-mono text-xs text-text-primary">
                          #{o.id.slice(-8).toUpperCase()}
                        </td>
                        <td className="py-3 px-4">
                          <div className="flex items-center gap-2">
                            <User className="h-3.5 w-3.5 text-text-tertiary" />
                            <span className="text-text-primary">{o.clientName ?? "Walk-in"}</span>
                          </div>
                        </td>
                        <td className="py-3 px-4 text-text-secondary text-xs">
                          {new Date(o.issueDate).toLocaleDateString([], { month: "short", day: "numeric", year: "numeric" })}
                        </td>
                        <td className="py-3 px-4 font-semibold text-text-primary">
                          ${(o.totalAmount ?? 0).toFixed(2)}
                        </td>
                        <td className="py-3 px-4">
                          <StatusBadge status={o.status} />
                        </td>
                        <td className="py-3 px-4">
                          <Eye className="h-4 w-4 text-text-tertiary" />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Order detail panel */}
        {selected && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span className="text-sm">Order #{selected.id.slice(-8).toUpperCase()}</span>
                <button
                  onClick={() => setSelected(null)}
                  className="text-text-tertiary hover:text-text-primary"
                >
                  ×
                </button>
              </CardTitle>
              <CardDescription>
                {new Date(selected.issueDate).toLocaleDateString([], { weekday: "long", month: "long", day: "numeric", year: "numeric" })}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-text-secondary">Status</span>
                <StatusBadge status={selected.status} />
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-text-secondary">Client</span>
                <span className="text-sm font-medium text-text-primary">{selected.clientName ?? "Walk-in"}</span>
              </div>

              {selected.items && selected.items.length > 0 && (
                <div className="border-t border-surface-200 pt-4">
                  <p className="text-xs font-semibold text-text-tertiary uppercase mb-2">Items</p>
                  <div className="space-y-2">
                    {selected.items.map((item, i) => (
                      <div key={i} className="flex justify-between items-start text-sm">
                        <div className="flex-1 min-w-0">
                          <p className="text-text-primary font-medium truncate">{item.description}</p>
                          <p className="text-text-tertiary text-xs">Qty {item.quantity} × ${item.unitPrice.toFixed(2)}</p>
                        </div>
                        <span className="font-medium text-text-primary ms-4">${item.total.toFixed(2)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="border-t border-surface-200 pt-4 flex justify-between">
                <span className="font-semibold text-text-primary">Total</span>
                <span className="text-xl font-bold text-text-primary">${(selected.totalAmount ?? 0).toFixed(2)}</span>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
