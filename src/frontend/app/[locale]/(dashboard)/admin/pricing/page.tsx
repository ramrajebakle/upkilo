"use client";

import React, { useState, useEffect, useCallback } from "react";
import { DollarSign, Plus, Trash2, Edit2, Loader2, RefreshCw, Tag, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Plan { id: string; name: string; price: number; interval: string; trialDays?: number; isActive: boolean; }
interface Feature { id: string; key: string; name: string; description?: string; }
interface Discount { id: string; code: string; percentOff?: number; amountOff?: number; expiresAt?: string; isActive: boolean; }

type Tab = "plans" | "features" | "discounts";

export default function AdminPricingPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [tab, setTab] = useState<Tab>("plans");
  const [plans, setPlans] = useState<Plan[]>([]);
  const [features, setFeatures] = useState<Feature[]>([]);
  const [discounts, setDiscounts] = useState<Discount[]>([]);
  const [loading, setLoading] = useState(true);
  const [showPlanForm, setShowPlanForm] = useState(false);
  const [showDiscountForm, setShowDiscountForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [planForm, setPlanForm] = useState({ name: "", price: "", interval: "month", trialDays: "" });
  const [discountForm, setDiscountForm] = useState({ code: "", percentOff: "", amountOff: "", expiresAt: "" });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [planRes, featRes, discRes] = await Promise.all([
        apiClient.get("/api/admin/pricing/plans").catch(() => ({ data: [] })),
        apiClient.get("/api/admin/pricing/features").catch(() => ({ data: [] })),
        apiClient.get("/api/admin/pricing/discounts").catch(() => ({ data: [] })),
      ]);
      setPlans(Array.isArray(planRes.data) ? planRes.data : planRes.data?.data ?? []);
      setFeatures(Array.isArray(featRes.data) ? featRes.data : featRes.data?.data ?? []);
      setDiscounts(Array.isArray(discRes.data) ? discRes.data : discRes.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const savePlan = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/admin/pricing/plans", { ...planForm, price: parseFloat(planForm.price), trialDays: planForm.trialDays ? parseInt(planForm.trialDays) : null });
      toastSuccess("Plan created"); setShowPlanForm(false); setPlanForm({ name: "", price: "", interval: "month", trialDays: "" }); load();
    } catch { toastError("Failed to create plan"); }
    finally { setSaving(false); }
  };

  const deletePlan = async (id: string) => {
    try { await apiClient.delete(`/api/admin/pricing/plans/${id}`); toastSuccess("Plan deleted"); load(); }
    catch { toastError("Failed to delete plan"); }
  };

  const saveDiscount = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/admin/pricing/discounts", { ...discountForm, percentOff: discountForm.percentOff ? parseFloat(discountForm.percentOff) : null, amountOff: discountForm.amountOff ? parseFloat(discountForm.amountOff) : null });
      toastSuccess("Discount created"); setShowDiscountForm(false); setDiscountForm({ code: "", percentOff: "", amountOff: "", expiresAt: "" }); load();
    } catch { toastError("Failed to create discount"); }
    finally { setSaving(false); }
  };

  const syncStripe = async () => {
    try { await apiClient.post("/api/admin/pricing/sync-stripe"); toastSuccess("Synced with Stripe"); }
    catch { toastError("Stripe sync failed"); }
  };

  const TABS: { key: Tab; label: string; count: number }[] = [
    { key: "plans", label: "Plans", count: plans.length },
    { key: "features", label: "Features", count: features.length },
    { key: "discounts", label: "Discounts", count: discounts.length },
  ];

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Pricing Admin <DollarSign className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Manage subscription plans, features, and discount codes.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="outline" size="sm" onClick={syncStripe}>Sync Stripe</Button>
          {tab === "plans" && <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowPlanForm(true)}>New Plan</Button>}
          {tab === "discounts" && <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowDiscountForm(true)}>New Discount</Button>}
        </div>
      </header>

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {TABS.map((t) => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.key ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
            {t.label} <span className="text-text-tertiary">({t.count})</span>
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "plans" && (
            <>
              {showPlanForm && (
                <Card>
                  <CardHeader><CardTitle>New Plan</CardTitle></CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      {[
                        { key: "name", label: "Plan Name *", placeholder: "e.g. Pro" },
                        { key: "price", label: "Price *", placeholder: "e.g. 49", type: "number" },
                        { key: "trialDays", label: "Trial Days", placeholder: "e.g. 14", type: "number" },
                      ].map((f) => (
                        <div key={f.key}>
                          <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
                          <input type={f.type ?? "text"} value={(planForm as any)[f.key]} onChange={(e) => setPlanForm((p) => ({ ...p, [f.key]: e.target.value }))}
                            placeholder={f.placeholder} className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                        </div>
                      ))}
                      <div>
                        <label className="block text-xs font-medium text-text-secondary mb-1">Interval</label>
                        <select value={planForm.interval} onChange={(e) => setPlanForm((p) => ({ ...p, interval: e.target.value }))}
                          className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                          {["month", "year"].map((i) => <option key={i} value={i}>{i}</option>)}
                        </select>
                      </div>
                    </div>
                    <div className="flex justify-end gap-2">
                      <Button variant="outline" size="sm" onClick={() => setShowPlanForm(false)} disabled={saving}>Cancel</Button>
                      <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
                        onClick={savePlan} disabled={!planForm.name || !planForm.price || saving}>{saving ? "Saving…" : "Create Plan"}</Button>
                    </div>
                  </CardContent>
                </Card>
              )}
              <div className="space-y-3">
                {plans.map((p) => (
                  <Card key={p.id}>
                    <CardContent className="pt-4 pb-4 flex items-center justify-between">
                      <div>
                        <p className="font-semibold text-text-primary">{p.name}</p>
                        <p className="text-sm text-text-secondary">${p.price}/{p.interval}{p.trialDays ? ` · ${p.trialDays}-day trial` : ""}</p>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className={`text-xs px-2 py-0.5 rounded-full ${p.isActive ? "text-green-600 bg-green-50" : "text-gray-500 bg-gray-50"}`}>{p.isActive ? "Active" : "Inactive"}</span>
                        <Button variant="outline" size="sm" leftIcon={<Trash2 size={12} className="text-red-500" />} onClick={() => deletePlan(p.id)} />
                      </div>
                    </CardContent>
                  </Card>
                ))}
                {plans.length === 0 && <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No plans yet</p></CardContent></Card>}
              </div>
            </>
          )}

          {tab === "features" && (
            <div className="space-y-3">
              {features.map((f) => (
                <Card key={f.id}>
                  <CardContent className="pt-4 pb-4">
                    <p className="font-medium text-text-primary">{f.name}</p>
                    <p className="text-xs text-text-tertiary font-mono">{f.key}</p>
                    {f.description && <p className="text-xs text-text-secondary mt-1">{f.description}</p>}
                  </CardContent>
                </Card>
              ))}
              {features.length === 0 && <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No features defined</p></CardContent></Card>}
            </div>
          )}

          {tab === "discounts" && (
            <>
              {showDiscountForm && (
                <Card>
                  <CardHeader><CardTitle>New Discount</CardTitle></CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      {[
                        { key: "code", label: "Code *", placeholder: "e.g. LAUNCH50" },
                        { key: "percentOff", label: "% Off", placeholder: "e.g. 20", type: "number" },
                        { key: "amountOff", label: "$ Off", placeholder: "e.g. 10", type: "number" },
                        { key: "expiresAt", label: "Expires", type: "date" },
                      ].map((f) => (
                        <div key={f.key}>
                          <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
                          <input type={f.type ?? "text"} value={(discountForm as any)[f.key]} onChange={(e) => setDiscountForm((p) => ({ ...p, [f.key]: e.target.value }))}
                            placeholder={f.placeholder} className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                        </div>
                      ))}
                    </div>
                    <div className="flex justify-end gap-2">
                      <Button variant="outline" size="sm" onClick={() => setShowDiscountForm(false)} disabled={saving}>Cancel</Button>
                      <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
                        onClick={saveDiscount} disabled={!discountForm.code || saving}>{saving ? "Saving…" : "Create Discount"}</Button>
                    </div>
                  </CardContent>
                </Card>
              )}
              <div className="space-y-3">
                {discounts.map((d) => (
                  <Card key={d.id}>
                    <CardContent className="pt-4 pb-4 flex items-center justify-between">
                      <div>
                        <p className="font-semibold text-text-primary font-mono">{d.code}</p>
                        <p className="text-sm text-text-secondary">
                          {d.percentOff ? `${d.percentOff}% off` : `$${d.amountOff} off`}
                          {d.expiresAt ? ` · expires ${new Date(d.expiresAt).toLocaleDateString()}` : ""}
                        </p>
                      </div>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${d.isActive ? "text-green-600 bg-green-50" : "text-gray-500 bg-gray-50"}`}>{d.isActive ? "Active" : "Expired"}</span>
                    </CardContent>
                  </Card>
                ))}
                {discounts.length === 0 && <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No discounts yet</p></CardContent></Card>}
              </div>
            </>
          )}
        </>
      )}
    </div>
  );
}
