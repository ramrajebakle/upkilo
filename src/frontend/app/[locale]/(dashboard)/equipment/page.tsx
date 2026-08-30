"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Wrench, Plus, AlertTriangle, CheckCircle2, Loader2, RefreshCw, Calendar, DollarSign } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Equipment {
  id: string;
  name: string;
  description?: string;
  serialNumber?: string;
  location?: string;
  purchaseDate?: string;
  purchasePrice?: number;
  status: "Active" | "Maintenance" | "Retired";
  nextMaintenanceDue?: string;
}

const STATUS_CFG = {
  Active: { color: "text-green-600", bg: "bg-green-50" },
  Maintenance: { color: "text-amber-600", bg: "bg-amber-50" },
  Retired: { color: "text-foreground-secondary", bg: "bg-muted" },
};

export default function EquipmentPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", serialNumber: "", location: "", purchasePrice: "", purchaseDate: "" });
  const [maintenanceDue, setMaintenanceDue] = useState<Equipment[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [eqRes, dueRes] = await Promise.all([
        apiClient.get("/api/v1/equipment").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/equipment/maintenance-due").catch(() => ({ data: [] })),
      ]);
      setEquipment(Array.isArray(eqRes.data) ? eqRes.data : eqRes.data?.data ?? []);
      setMaintenanceDue(Array.isArray(dueRes.data) ? dueRes.data : dueRes.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/equipment", { ...form, purchasePrice: form.purchasePrice ? parseFloat(form.purchasePrice) : null });
      toastSuccess("Equipment added"); setShowForm(false); setForm({ name: "", description: "", serialNumber: "", location: "", purchasePrice: "", purchaseDate: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  const logMaintenance = async (id: string) => {
    try {
      await apiClient.post(`/api/v1/equipment/${id}/maintenance`, { notes: "Maintenance completed", performedAt: new Date().toISOString() });
      toastSuccess("Maintenance logged"); load();
    } catch { toastError("Failed to log maintenance"); }
  };

  const statusFilter = ["All", "Active", "Maintenance", "Retired"];
  const [filter, setFilter] = useState("All");
  const filtered = equipment.filter((e) => filter === "All" || e.status === filter);

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Equipment <Wrench className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Track equipment, maintenance schedules, and asset values.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add Equipment</Button>
        </div>
      </header>

      {maintenanceDue.length > 0 && (
        <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-xl">
          <AlertTriangle className="h-5 w-5 text-warning-fg mt-0.5 flex-shrink-0" />
          <div>
            <p className="text-sm font-medium text-amber-800">{maintenanceDue.length} item{maintenanceDue.length !== 1 ? "s" : ""} due for maintenance</p>
            <p className="text-xs text-warning-fg">{maintenanceDue.map((e) => e.name).join(", ")}</p>
          </div>
        </div>
      )}

      {showForm && (
        <Card>
          <CardHeader><CardTitle>Add Equipment</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[
                { key: "name", label: "Equipment Name *", placeholder: "e.g. Massage Table" },
                { key: "serialNumber", label: "Serial Number", placeholder: "Optional" },
                { key: "location", label: "Location", placeholder: "e.g. Room 2" },
                { key: "purchasePrice", label: "Purchase Price", placeholder: "e.g. 1200", type: "number" },
                { key: "purchaseDate", label: "Purchase Date", type: "date" },
              ].map((f) => (
                <div key={f.key}>
                  <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
                  <input type={f.type ?? "text"} value={(form as any)[f.key]} onChange={(e) => setForm((p) => ({ ...p, [f.key]: e.target.value }))}
                    placeholder={f.placeholder} className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
              ))}
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Description</label>
                <input value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} placeholder="Optional notes"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={saving}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
                onClick={handleSave} disabled={!form.name.trim() || saving}>{saving ? "Saving…" : "Add Equipment"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-sm">
        {statusFilter.map((f) => (
          <button key={f} onClick={() => setFilter(f)}
            className={cn("flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors",
              filter === f ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary")}>
            {f}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Wrench className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No equipment found</p>
          </CardContent></Card>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {filtered.map((e) => {
              const cfg = STATUS_CFG[e.status] ?? STATUS_CFG.Active;
              const isDue = maintenanceDue.some((m) => m.id === e.id);
              return (
                <Card key={e.id} className={isDue ? "border-amber-200" : ""}>
                  <CardContent className="pt-5">
                    <div className="flex items-start justify-between mb-2">
                      <p className="font-semibold text-text-primary">{e.name}</p>
                      <span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{e.status}</span>
                    </div>
                    {e.description && <p className="text-xs text-text-secondary mb-2">{e.description}</p>}
                    <div className="space-y-1.5 mb-3">
                      {e.location && <p className="text-xs text-text-tertiary">📍 {e.location}</p>}
                      {e.serialNumber && <p className="text-xs text-text-tertiary font-mono">S/N: {e.serialNumber}</p>}
                      {e.purchasePrice && (
                        <p className="text-xs text-text-tertiary flex items-center gap-1">
                          <DollarSign className="h-3 w-3" />${e.purchasePrice.toLocaleString()}
                        </p>
                      )}
                      {e.nextMaintenanceDue && (
                        <p className={cn("text-xs flex items-center gap-1", isDue ? "text-warning-fg font-medium" : "text-text-tertiary")}>
                          <Calendar className="h-3 w-3" />Next: {new Date(e.nextMaintenanceDue).toLocaleDateString()}
                          {isDue && <AlertTriangle className="h-3 w-3" />}
                        </p>
                      )}
                    </div>
                    <Button variant="outline" size="sm" className="w-full text-xs" leftIcon={<CheckCircle2 size={12} />}
                      onClick={() => logMaintenance(e.id)}>Log Maintenance</Button>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
    </div>
  );
}
