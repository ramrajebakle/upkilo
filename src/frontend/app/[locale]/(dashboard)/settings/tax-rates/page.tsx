"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Percent, Plus, Pencil, Trash2, Loader2, RefreshCw, CheckCircle2, XCircle } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface TaxRate {
  id: string;
  name: string;
  rate: number;
  country?: string;
  region?: string;
  isDefault: boolean;
  isActive: boolean;
}

function TaxForm({
  initial, onSave, onCancel, saving,
}: {
  initial?: TaxRate | null;
  onSave: (d: Omit<TaxRate, "id">) => Promise<void>;
  onCancel: () => void;
  saving: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [rate, setRate] = useState(initial?.rate?.toString() ?? "");
  const [country, setCountry] = useState(initial?.country ?? "");
  const [region, setRegion] = useState(initial?.region ?? "");
  const [isDefault, setIsDefault] = useState(initial?.isDefault ?? false);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 bg-surface-50 rounded-xl border border-surface-200">
      {[
        { label: "Name *", value: name, onChange: setName, placeholder: "e.g. GST 18%" },
        { label: "Rate (%) *", value: rate, onChange: setRate, placeholder: "e.g. 18", type: "number" },
        { label: "Country", value: country, onChange: setCountry, placeholder: "e.g. India" },
        { label: "Region / State", value: region, onChange: setRegion, placeholder: "e.g. Maharashtra" },
      ].map((f) => (
        <div key={f.label}>
          <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
          <input
            type={f.type ?? "text"}
            value={f.value}
            onChange={(e) => f.onChange(e.target.value)}
            placeholder={f.placeholder}
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
          />
        </div>
      ))}
      <div className="sm:col-span-2 flex items-center gap-3">
        <label className="flex items-center gap-2 cursor-pointer text-sm">
          <input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} className="accent-ai-500 h-4 w-4 rounded" />
          <span className="text-text-primary">Set as default tax rate</span>
        </label>
      </div>
      <div className="sm:col-span-2 flex justify-end gap-2">
        <Button variant="outline" size="sm" onClick={onCancel} disabled={saving}>Cancel</Button>
        <Button
          variant="primary" size="sm"
          leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
          onClick={() => onSave({ name, rate: parseFloat(rate), country, region, isDefault, isActive: true })}
          disabled={!name.trim() || !rate || saving}
        >
          {saving ? "Saving…" : initial ? "Update" : "Create"}
        </Button>
      </div>
    </div>
  );
}

export default function TaxRatesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [rates, setRates] = useState<TaxRate[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<TaxRate | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/taxrates");
      const d = r.data?.data ?? r.data ?? [];
      setRates(Array.isArray(d) ? d : []);
    } catch { toastError("Failed to load tax rates"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const handleSave = async (data: Omit<TaxRate, "id">) => {
    setSaving(true);
    try {
      if (editing) {
        await apiClient.put(`/api/v1/taxrates/${editing.id}`, data);
        toastSuccess("Tax rate updated");
      } else {
        await apiClient.post("/api/v1/taxrates", data);
        toastSuccess("Tax rate created");
      }
      setShowForm(false); setEditing(null); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    try {
      await apiClient.delete(`/api/v1/taxrates/${id}`);
      toastSuccess("Deleted"); setRates((r) => r.filter((x) => x.id !== id));
    } catch { toastError("Failed to delete"); }
    finally { setDeletingId(null); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Tax Rates <Percent className="text-text-tertiary" size={20} /></h1>
          <p className="text-text-secondary mt-1">Manage tax rates applied to bookings and products.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => { setEditing(null); setShowForm(true); }}>Add Rate</Button>
        </div>
      </header>

      {(showForm || editing) && (
        <TaxForm initial={editing} onSave={handleSave} onCancel={() => { setShowForm(false); setEditing(null); }} saving={saving} />
      )}

      <Card>
        <CardHeader><CardTitle>Tax Rates</CardTitle><CardDescription>{rates.length} rates configured</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : rates.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <Percent className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No tax rates yet</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Name", "Rate", "Country", "Region", "Default", "Status", ""].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {rates.map((r) => (
                    <tr key={r.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-3 font-medium text-text-primary">{r.name}</td>
                      <td className="py-3 px-3 font-mono text-text-primary">{r.rate}%</td>
                      <td className="py-3 px-3 text-text-secondary">{r.country ?? "—"}</td>
                      <td className="py-3 px-3 text-text-secondary">{r.region ?? "—"}</td>
                      <td className="py-3 px-3">{r.isDefault ? <CheckCircle2 className="h-4 w-4 text-green-500" /> : <span className="text-text-tertiary">—</span>}</td>
                      <td className="py-3 px-3">
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${r.isActive ? "bg-green-50 text-green-600" : "bg-red-50 text-red-500"}`}>{r.isActive ? "Active" : "Inactive"}</span>
                      </td>
                      <td className="py-3 px-3">
                        <div className="flex gap-1">
                          <Button variant="outline" size="sm" leftIcon={<Pencil size={12} />} onClick={() => { setEditing(r); setShowForm(false); }}>Edit</Button>
                          <Button variant="outline" size="sm" className="text-red-500 border-red-200 hover:bg-red-50"
                            leftIcon={deletingId === r.id ? <Loader2 size={12} className="animate-spin" /> : <Trash2 size={12} />}
                            onClick={() => handleDelete(r.id)} disabled={deletingId === r.id}>Del</Button>
                        </div>
                      </td>
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
