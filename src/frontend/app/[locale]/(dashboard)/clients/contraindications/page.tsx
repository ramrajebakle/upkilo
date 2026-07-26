"use client";

import React, { useState, useEffect, useCallback } from "react";
import { AlertTriangle, Plus, Search, Loader2, Trash2, RefreshCw, User } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Alert { id: string; clientId: string; clientName: string; condition: string; severity: "Low" | "Medium" | "High" | "Critical"; notes?: string; createdAt: string; }

const SEV_CFG: Record<string, string> = {
  Critical: "text-red-600 bg-red-50 border-red-200",
  High: "text-orange-600 bg-orange-50 border-orange-200",
  Medium: "text-amber-600 bg-amber-50 border-amber-200",
  Low: "text-gray-600 bg-gray-50 border-gray-200",
};

export default function ContraindicationsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ clientId: "", condition: "", severity: "Medium", notes: "" });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/contraindications/clients-with-alerts").catch(() => ({ data: [] }));
      setAlerts(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async () => {
    if (!form.clientId || !form.condition) return;
    setSaving(true);
    try {
      await apiClient.post(`/api/v1/contraindications/client/${form.clientId}`, {
        condition: form.condition, severity: form.severity, notes: form.notes,
      });
      toastSuccess("Medical alert added"); setShowForm(false); setForm({ clientId: "", condition: "", severity: "Medium", notes: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to add alert"); }
    finally { setSaving(false); }
  };

  const filtered = alerts.filter((a) =>
    !search || a.clientName?.toLowerCase().includes(search.toLowerCase()) || a.condition?.toLowerCase().includes(search.toLowerCase())
  );

  const criticalCount = alerts.filter((a) => a.severity === "Critical").length;
  const highCount = alerts.filter((a) => a.severity === "High").length;

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Medical Alerts <AlertTriangle className="text-red-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Contraindications and medical alerts for client safety.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add Alert</Button>
        </div>
      </header>

      {(criticalCount > 0 || highCount > 0) && (
        <div className="flex items-start gap-3 p-4 rounded-xl bg-red-50 border border-red-200">
          <AlertTriangle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-semibold text-red-800">Active medical alerts require attention</p>
            <p className="text-xs text-red-600 mt-0.5">{criticalCount} critical, {highCount} high-severity alerts on file.</p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Alerts", value: alerts.length, color: "text-text-primary" },
          { label: "Critical", value: criticalCount, color: "text-red-600" },
          { label: "High", value: highCount, color: "text-orange-600" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Medical Alert</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Client ID *</label>
                <input value={form.clientId} onChange={(e) => setForm((p) => ({ ...p, clientId: e.target.value }))} placeholder="Client UUID"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Severity</label>
                <select value={form.severity} onChange={(e) => setForm((p) => ({ ...p, severity: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  {["Low", "Medium", "High", "Critical"].map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Condition / Contraindication *</label>
              <input value={form.condition} onChange={(e) => setForm((p) => ({ ...p, condition: e.target.value }))} placeholder="e.g. Latex allergy, Pacemaker, Blood thinner medication"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Notes</label>
              <textarea value={form.notes} onChange={(e) => setForm((p) => ({ ...p, notes: e.target.value }))} rows={3} placeholder="Additional clinical notes…"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <AlertTriangle size={14} />}
                onClick={handleAdd} disabled={!form.clientId || !form.condition || saving}>{saving ? "Saving…" : "Add Alert"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by client name or condition…"
          className="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <AlertTriangle className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">{search ? "No alerts match your search" : "No medical alerts on file"}</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((a) => (
              <Card key={a.id} className={cn("border", SEV_CFG[a.severity] ?? SEV_CFG.Low)}>
                <CardContent className="pt-4 pb-4 flex items-start justify-between gap-3">
                  <div className="flex items-start gap-3">
                    <div className="w-9 h-9 rounded-lg bg-white/70 border border-current/20 flex items-center justify-center flex-shrink-0">
                      <User className="h-4 w-4 opacity-60" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2 mb-0.5">
                        <p className="font-semibold text-sm">{a.clientName ?? "Unknown client"}</p>
                        <span className={cn("text-xs font-bold px-2 py-0.5 rounded-full border", SEV_CFG[a.severity])}>{a.severity}</span>
                      </div>
                      <p className="text-sm font-medium">{a.condition}</p>
                      {a.notes && <p className="text-xs mt-0.5 opacity-70">{a.notes}</p>}
                      <p className="text-xs opacity-50 mt-1">Added {new Date(a.createdAt).toLocaleDateString()}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
