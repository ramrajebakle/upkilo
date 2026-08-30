"use client";

import React, { useState, useEffect, useCallback } from "react";
import { TrendingUp, Plus, ToggleLeft, ToggleRight, Trash2, Loader2, RefreshCw, Zap, Clock, Users } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface DynamicPricingRule {
  id: string;
  name: string;
  trigger: "TimeOfDay" | "DemandLevel" | "CapacityThreshold" | "DaysInAdvance" | "WalkIn";
  adjustmentType: "Percent" | "Fixed";
  adjustmentValue: number;
  conditions?: Record<string, any>;
  isActive: boolean;
  priority: number;
}

const TRIGGER_LABELS: Record<string, { label: string; icon: React.ReactNode }> = {
  TimeOfDay: { label: "Time of Day", icon: <Clock className="h-3.5 w-3.5" /> },
  DemandLevel: { label: "Demand Level", icon: <TrendingUp className="h-3.5 w-3.5" /> },
  CapacityThreshold: { label: "Capacity", icon: <Users className="h-3.5 w-3.5" /> },
  DaysInAdvance: { label: "Days in Advance", icon: <Clock className="h-3.5 w-3.5" /> },
  WalkIn: { label: "Walk-In", icon: <Zap className="h-3.5 w-3.5" /> },
};

export default function DynamicPricingRulesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [rules, setRules] = useState<DynamicPricingRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [toggling, setToggling] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", trigger: "DemandLevel", adjustmentType: "Percent", adjustmentValue: "", priority: "1" });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/dynamicpricing/rules").catch(() => ({ data: [] }));
      setRules(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/dynamicpricing/rules", {
        ...form, adjustmentValue: parseFloat(form.adjustmentValue), priority: parseInt(form.priority) || 1,
      });
      toastSuccess("Rule created"); setShowForm(false); setForm({ name: "", trigger: "DemandLevel", adjustmentType: "Percent", adjustmentValue: "", priority: "1" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save rule"); }
    finally { setSaving(false); }
  };

  const toggleRule = async (id: string) => {
    setToggling(id);
    try {
      await apiClient.put(`/api/v1/dynamicpricing/rules/${id}/toggle`);
      setRules((prev) => prev.map((r) => r.id === id ? { ...r, isActive: !r.isActive } : r));
    } catch { toastError("Failed to toggle rule"); }
    finally { setToggling(null); }
  };

  const deleteRule = async (id: string) => {
    try {
      await apiClient.delete(`/api/v1/dynamicpricing/rules/${id}`);
      toastSuccess("Rule deleted"); setRules((prev) => prev.filter((r) => r.id !== id));
    } catch { toastError("Failed to delete rule"); }
  };

  const activeCount = rules.filter((r) => r.isActive).length;

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Dynamic Pricing Rules <TrendingUp className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Automatically adjust prices based on demand, time, and capacity.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>New Rule</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        <Card><CardContent className="pt-5"><p className="text-xs text-text-secondary">Total Rules</p><p className="text-2xl font-bold text-text-primary mt-1">{rules.length}</p></CardContent></Card>
        <Card><CardContent className="pt-5"><p className="text-xs text-text-secondary">Active</p><p className="text-2xl font-bold text-success-fg mt-1">{activeCount}</p></CardContent></Card>
        <Card><CardContent className="pt-5"><p className="text-xs text-text-secondary">Inactive</p><p className="text-2xl font-bold text-foreground-muted mt-1">{rules.length - activeCount}</p></CardContent></Card>
      </div>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Pricing Rule</CardTitle><CardDescription>Rules are evaluated in priority order (lower = higher priority)</CardDescription></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Rule Name *</label>
                <input value={form.name} onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))} placeholder="e.g. Peak Hour Surge"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Trigger</label>
                <select value={form.trigger} onChange={(e) => setForm((p) => ({ ...p, trigger: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  {Object.entries(TRIGGER_LABELS).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Adjustment Type</label>
                <select value={form.adjustmentType} onChange={(e) => setForm((p) => ({ ...p, adjustmentType: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  <option value="Percent">Percent (%)</option>
                  <option value="Fixed">Fixed ($)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Adjustment Value * <span className="text-text-tertiary">(negative = discount)</span></label>
                <input type="number" value={form.adjustmentValue} onChange={(e) => setForm((p) => ({ ...p, adjustmentValue: e.target.value }))} placeholder="e.g. 20"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Priority (1 = highest)</label>
                <input type="number" min="1" value={form.priority} onChange={(e) => setForm((p) => ({ ...p, priority: e.target.value }))}
                  className="w-28 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={saving}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
                onClick={handleSave} disabled={!form.name || !form.adjustmentValue || saving}>{saving ? "Saving…" : "Create Rule"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : rules.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <TrendingUp className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No dynamic pricing rules yet</p>
            <p className="text-sm mt-1">Rules automatically adjust prices based on demand, time, and capacity.</p>
            <Button variant="primary" className="mt-4" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Create first rule</Button>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {[...rules].sort((a, b) => a.priority - b.priority).map((r) => {
              const trig = TRIGGER_LABELS[r.trigger] ?? { label: r.trigger, icon: <Zap className="h-3.5 w-3.5" /> };
              const isPositive = r.adjustmentValue >= 0;
              return (
                <Card key={r.id} className={r.isActive ? "" : "opacity-60"}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex items-center gap-3">
                        <span className="text-xs font-bold text-text-tertiary w-6 text-center">#{r.priority}</span>
                        <div>
                          <p className="font-semibold text-text-primary">{r.name}</p>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span className="inline-flex items-center gap-1 text-xs text-text-secondary bg-surface-100 px-2 py-0.5 rounded-full">
                              {trig.icon}{trig.label}
                            </span>
                            <span className={cn("text-xs font-medium px-2 py-0.5 rounded-full",
                              isPositive ? "text-orange-600 bg-orange-50" : "text-green-600 bg-green-50")}>
                              {isPositive ? "+" : ""}{r.adjustmentValue}{r.adjustmentType === "Percent" ? "%" : "$"}
                            </span>
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <button onClick={() => toggleRule(r.id)} disabled={toggling === r.id} className="text-text-tertiary hover:text-ai transition-colors">
                          {toggling === r.id ? <Loader2 className="h-5 w-5 animate-spin" /> : r.isActive ? <ToggleRight className="h-6 w-6 text-success-fg" /> : <ToggleLeft className="h-6 w-6" />}
                        </button>
                        <Button variant="outline" size="sm" leftIcon={<Trash2 size={12} className="text-danger-fg" />} onClick={() => deleteRule(r.id)} />
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
    </div>
  );
}
