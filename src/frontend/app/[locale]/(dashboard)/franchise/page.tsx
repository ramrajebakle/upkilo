"use client";

import React, { useState, useEffect } from "react";
import { Building2, Share2, TrendingUp, Loader2, RefreshCw, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { useTenantCurrency } from '@/hooks/useTenantCurrency';

interface FranchiseDashboard {
  totalLocations: number; totalRevenue: number; totalBookings: number; avgOccupancy: number;
  locations: Array<{ id: string; name: string; revenue: number; bookings: number; occupancy: number; city?: string; }>;
  topPerformer?: string; bottomPerformer?: string;
}

export default function FranchisePage() {
  const currency = useTenantCurrency();
  const { success: toastSuccess, error: toastError } = useToast();
  const [data, setData] = useState<FranchiseDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [pushing, setPushing] = useState(false);
  const [days, setDays] = useState(30);
  const [tab, setTab] = useState<"overview" | "push">("overview");
  const [selectedServices, setSelectedServices] = useState<string[]>([]);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get(`/api/v1/franchise/dashboard?days=${days}`).catch(() => ({ data: null }));
      setData(r.data?.data ?? r.data ?? null);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [days]);

  const pushServices = async () => {
    if (selectedServices.length === 0) { toastError("Select at least one service"); return; }
    setPushing(true);
    try {
      await apiClient.post("/api/v1/franchise/push-services", { serviceIds: selectedServices, pushToAll: true });
      toastSuccess("Services pushed to all locations"); setSelectedServices([]);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Push failed"); }
    finally { setPushing(false); }
  };

  // Tenant-currency aware: this used to hardcode ₹ regardless of the tenant's currency.
  const fmt = (n: number) =>
    new Intl.NumberFormat(undefined, { style: 'currency', currency, notation: n >= 1000 ? 'compact' : 'standard', maximumFractionDigits: 1 }).format(n);

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Franchise Management <Building2 className="text-ai-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Monitor all franchise locations and push updates from head office.</p>
        </div>
        <div className="flex items-center gap-3">
          <select value={days} onChange={(e) => setDays(Number(e.target.value))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none">
            {[7, 30, 60, 90].map((d) => <option key={d} value={d}>Last {d} days</option>)}
          </select>
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : data ? (
        <>
          <div className="grid grid-cols-4 gap-4">
            {[
              { label: "Locations", value: data.totalLocations, icon: <Building2 size={16} className="text-blue-500" /> },
              { label: "Total Revenue", value: fmt(data.totalRevenue), icon: <TrendingUp size={16} className="text-green-500" /> },
              { label: "Total Bookings", value: data.totalBookings.toLocaleString(), icon: <CheckCircle2 size={16} className="text-ai-500" /> },
              { label: "Avg Occupancy", value: `${(data.avgOccupancy ?? 0).toFixed(1)}%`, icon: <Share2 size={16} className="text-primary-500" /> },
            ].map((m) => (
              <Card key={m.label}><CardContent className="pt-4 pb-4">
                <div className="flex items-center gap-2 mb-1">{m.icon}<p className="text-xs text-text-tertiary font-medium">{m.label}</p></div>
                <p className="text-xl font-bold text-text-primary">{m.value}</p>
              </CardContent></Card>
            ))}
          </div>

          <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
            {[{ k: "overview" as const, l: "Locations" }, { k: "push" as const, l: "Push Services" }].map((t) => (
              <button key={t.k} onClick={() => setTab(t.k)}
                className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.k ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>{t.l}</button>
            ))}
          </div>

          {tab === "overview" && (
            <Card>
              <CardHeader><CardTitle>Location Performance</CardTitle></CardHeader>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Location", "City", "Revenue", "Bookings", "Occupancy"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {(data.locations ?? []).map((loc) => (
                      <tr key={loc.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-sm font-medium text-text-primary">{loc.name}
                          {loc.name === data.topPerformer && <span className="ml-2 text-xs text-green-600 font-semibold">★ Top</span>}
                        </td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{loc.city ?? "—"}</td>
                        <td className="py-3 px-4 text-sm font-semibold text-text-primary">{fmt(loc.revenue)}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{loc.bookings}</td>
                        <td className="py-3 px-4">
                          <div className="flex items-center gap-2">
                            <div className="flex-1 bg-surface-200 rounded-full h-1.5 max-w-[80px]">
                              <div className="bg-ai-500 h-1.5 rounded-full" style={{ width: `${Math.min(loc.occupancy, 100)}%` }} />
                            </div>
                            <span className="text-xs text-text-secondary">{(loc.occupancy ?? 0).toFixed(0)}%</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                    {(data.locations ?? []).length === 0 && (
                      <tr><td colSpan={5} className="text-center py-8 text-text-tertiary text-xs">No location data available</td></tr>
                    )}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}

          {tab === "push" && (
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><Share2 size={16} /> Push Services to All Locations</CardTitle>
                <CardDescription>Sync service catalog changes from head office to all franchise locations</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="p-4 rounded-xl bg-blue-50 border border-blue-200">
                  <p className="text-sm text-blue-800 font-medium">Push to all {data.totalLocations} locations</p>
                  <p className="text-xs text-blue-600 mt-0.5">Enter service IDs to push, or leave empty to sync all services.</p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Service IDs (optional)</label>
                  <textarea value={selectedServices.join("\n")} onChange={(e) => setSelectedServices(e.target.value.split("\n").filter(Boolean))} rows={4}
                    placeholder="One service ID per line (leave empty to push all)"
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none font-mono" />
                </div>
                <div className="flex justify-end">
                  <Button variant="primary" leftIcon={pushing ? <Loader2 size={14} className="animate-spin" /> : <Share2 size={14} />}
                    onClick={pushServices} disabled={pushing}>{pushing ? "Pushing…" : "Push Services"}</Button>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      ) : (
        <Card><CardContent className="text-center py-12">
          <Building2 className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
          <p className="text-sm text-text-tertiary">No franchise data available. Ensure the franchise module is enabled.</p>
        </CardContent></Card>
      )}
    </div>
  );
}
