"use client";

import React, { useState, useEffect } from "react";
import { Zap, Loader2, Save, Info } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface AdvancedFeature { key: string; label: string; description: string; enabled: boolean; category: string; }

export default function AdvancedFeaturesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [features, setFeatures] = useState<AdvancedFeature[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirty, setDirty] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/advancedfeatures").catch(() => ({ data: [] }));
      setFeatures(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const toggle = (key: string) => {
    setFeatures((prev) => prev.map((f) => f.key === key ? { ...f, enabled: !f.enabled } : f));
    setDirty(true);
  };

  const save = async () => {
    setSaving(true);
    try {
      const updates: Record<string, boolean> = {};
      features.forEach((f) => { updates[f.key] = f.enabled; });
      await apiClient.put("/api/v1/advancedfeatures", updates);
      toastSuccess("Advanced features saved"); setDirty(false);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const categories = Array.from(new Set(features.map((f) => f.category ?? "General"))).filter(Boolean);

  const ToggleRow = ({ feature }: { feature: AdvancedFeature }) => (
    <div className="flex items-start justify-between gap-4 py-3 border-b border-surface-100 last:border-0">
      <div className="flex-1">
        <p className="text-sm font-medium text-text-primary">{feature.label}</p>
        <p className="text-xs text-text-tertiary mt-0.5">{feature.description}</p>
      </div>
      <div onClick={() => toggle(feature.key)}
        className={`w-10 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors mt-0.5 ${feature.enabled ? "bg-ai-500" : "bg-surface-300"}`}>
        <div className={`absolute top-0.5 w-4 h-4 bg-control-thumb rounded-full shadow transition-transform ${feature.enabled ? "translate-x-5" : "translate-x-0.5"}`} />
      </div>
    </div>
  );

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Advanced Features <Zap className="text-ai" size={22} /></h1>
          <p className="text-text-secondary mt-1">Enable or disable advanced platform capabilities. Changes apply immediately.</p>
        </div>
        {dirty && (
          <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
            onClick={save} disabled={saving}>{saving ? "Saving…" : "Save Changes"}</Button>
        )}
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : features.length === 0 ? (
          <Card><CardContent className="flex items-center gap-3 py-8">
            <Info className="h-5 w-5 text-text-tertiary" />
            <p className="text-sm text-text-tertiary">No advanced features available for your plan.</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-4">
            {categories.map((cat) => (
              <Card key={cat}>
                <CardHeader><CardTitle className="text-base">{cat}</CardTitle></CardHeader>
                <CardContent className="pt-0">
                  {features.filter((f) => (f.category ?? "General") === cat).map((f) => (
                    <ToggleRow key={f.key} feature={f} />
                  ))}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
