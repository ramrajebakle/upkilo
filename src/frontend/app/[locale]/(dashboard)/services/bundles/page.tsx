"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Package, Plus, Loader2, RefreshCw, DollarSign, Layers } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface ServiceBundle {
  id: string;
  name: string;
  description?: string;
  price: number;
  discountPercent?: number;
  serviceIds: string[];
  serviceNames?: string[];
  isActive: boolean;
}

interface Service { id: string; name: string; price: number; }

export default function ServiceBundlesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [bundles, setBundles] = useState<ServiceBundle[]>([]);
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", price: "", discountPercent: "", serviceIds: [] as string[] });

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [bundleRes, svcRes] = await Promise.all([
        apiClient.get("/api/v1/services/bundles").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/services").catch(() => ({ data: [] })),
      ]);
      setBundles(Array.isArray(bundleRes.data) ? bundleRes.data : bundleRes.data?.data ?? []);
      setServices(Array.isArray(svcRes.data) ? svcRes.data : svcRes.data?.data ?? []);
    } catch { toastError("Failed to load"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const toggleService = (id: string) =>
    setForm((f) => ({ ...f, serviceIds: f.serviceIds.includes(id) ? f.serviceIds.filter((x) => x !== id) : [...f.serviceIds, id] }));

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/services/bundles", { ...form, price: parseFloat(form.price), discountPercent: form.discountPercent ? parseFloat(form.discountPercent) : null });
      toastSuccess("Bundle created"); setShowForm(false); setForm({ name: "", description: "", price: "", discountPercent: "", serviceIds: [] }); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Service Bundles <Layers className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Group services together at a bundled price.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>New Bundle</Button>
        </div>
      </header>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>Create Bundle</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[
                { key: "name", label: "Bundle Name *", placeholder: "e.g. Bridal Package" },
                { key: "price", label: "Bundle Price *", placeholder: "e.g. 299", type: "number" },
                { key: "discountPercent", label: "Discount %", placeholder: "e.g. 15", type: "number" },
                { key: "description", label: "Description", placeholder: "What's included?" },
              ].map((f) => (
                <div key={f.key}>
                  <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
                  <input type={f.type ?? "text"} value={(form as any)[f.key]} onChange={(e) => setForm((prev) => ({ ...prev, [f.key]: e.target.value }))}
                    placeholder={f.placeholder} className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
              ))}
            </div>
            <div>
              <p className="text-sm font-medium text-text-primary mb-2">Included Services</p>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                {services.map((s) => (
                  <label key={s.id} className="flex items-center gap-2 cursor-pointer text-sm p-2 rounded-lg border border-surface-200 hover:bg-surface-50">
                    <input type="checkbox" checked={form.serviceIds.includes(s.id)} onChange={() => toggleService(s.id)} className="accent-ai-500 h-4 w-4 rounded" />
                    <span className="text-text-primary flex-1 min-w-0 truncate">{s.name}</span>
                    <span className="text-xs text-text-tertiary">${s.price}</span>
                  </label>
                ))}
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={saving}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
                onClick={handleSave} disabled={!form.name.trim() || !form.price || form.serviceIds.length === 0 || saving}>
                {saving ? "Saving…" : "Create Bundle"}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : bundles.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Layers className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No service bundles yet</p>
            <Button variant="primary" className="mt-4" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Create first bundle</Button>
          </CardContent></Card>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {bundles.map((b) => (
              <Card key={b.id}>
                <CardContent className="pt-5">
                  <div className="flex items-start justify-between mb-2">
                    <p className="font-semibold text-text-primary">{b.name}</p>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${b.isActive ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"}`}>{b.isActive ? "Active" : "Inactive"}</span>
                  </div>
                  {b.description && <p className="text-sm text-text-secondary mb-3">{b.description}</p>}
                  <div className="flex items-center gap-2 mb-3">
                    <DollarSign className="h-4 w-4 text-success-fg" />
                    <span className="text-lg font-bold text-success-fg">${b.price}</span>
                    {b.discountPercent && <span className="text-xs text-amber-600 bg-amber-50 px-2 py-0.5 rounded-full">{b.discountPercent}% off</span>}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {(b.serviceNames ?? b.serviceIds).map((s, i) => (
                      <span key={i} className="text-xs bg-surface-100 text-text-secondary px-2 py-0.5 rounded-full">{s}</span>
                    ))}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
