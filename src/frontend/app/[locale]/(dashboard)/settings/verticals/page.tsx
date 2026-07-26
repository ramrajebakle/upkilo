"use client";

import React, { useState } from "react";
import { Layers, Heart, Activity, Plus, Loader2, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface TreatmentPlan { id: string; name: string; description?: string; duration?: number; }
interface FitnessSession { clientId: string; date: string; type: string; duration: number; notes?: string; }

export default function VerticalsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [tab, setTab] = useState<"medical" | "fitness">("medical");
  const [plans, setPlans] = useState<TreatmentPlan[]>([]);
  const [loadingPlans, setLoadingPlans] = useState(false);
  const [logging, setLogging] = useState(false);
  const [preAuthForm, setPreAuthForm] = useState({ clientId: "", insuranceProvider: "", serviceCode: "", amount: "" });
  const [fitForm, setFitForm] = useState({ clientId: "", type: "Strength", duration: 60, notes: "" });

  const loadPlans = async () => {
    setLoadingPlans(true);
    try {
      const r = await apiClient.get("/api/v1/verticals/medical/treatment-plans").catch(() => ({ data: [] }));
      setPlans(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoadingPlans(false); }
  };

  const submitPreAuth = async () => {
    try {
      await apiClient.post("/api/v1/verticals/medical/insurance-preauth", {
        ...preAuthForm, amount: Number(preAuthForm.amount),
      });
      toastSuccess("Pre-authorization submitted"); setPreAuthForm({ clientId: "", insuranceProvider: "", serviceCode: "", amount: "" });
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Submission failed"); }
  };

  const logFitness = async () => {
    setLogging(true);
    try {
      await apiClient.post("/api/v1/verticals/fitness/session-log", { ...fitForm, date: new Date().toISOString() });
      toastSuccess("Fitness session logged"); setFitForm({ clientId: "", type: "Strength", duration: 60, notes: "" });
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Log failed"); }
    finally { setLogging(false); }
  };

  const TABS = [{ k: "medical" as const, l: "Medical / Spa", icon: <Heart size={14} /> }, { k: "fitness" as const, l: "Fitness / Gym", icon: <Activity size={14} /> }];
  const FIT_TYPES = ["Strength", "Cardio", "HIIT", "Yoga", "Pilates", "CrossFit", "Swimming", "Cycling", "Boxing", "Rehab"];

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Industry Features <Layers className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Specialized tools for medical spas, clinics, fitness studios, and gyms.</p>
      </header>

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {TABS.map((t) => (
          <button key={t.k} onClick={() => { setTab(t.k); if (t.k === "medical" && plans.length === 0) loadPlans(); }}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors flex items-center justify-center gap-1.5 ${tab === t.k ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
            {t.icon}{t.l}
          </button>
        ))}
      </div>

      {tab === "medical" && (
        <div className="space-y-4">
          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><Heart size={16} className="text-red-500" /> Treatment Plan Templates</CardTitle>
              <CardDescription>Pre-built treatment protocols for common medical/spa services</CardDescription></CardHeader>
            <CardContent>
              {loadingPlans ? <div className="flex justify-center py-4"><Loader2 className="h-4 w-4 animate-spin text-text-tertiary" /></div>
                : plans.length === 0 ? (
                  <div className="text-center py-6">
                    <p className="text-sm text-text-tertiary mb-3">No treatment plan templates loaded</p>
                    <Button variant="outline" size="sm" onClick={loadPlans}>Load Templates</Button>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {plans.map((p) => (
                      <div key={p.id} className="flex items-center gap-3 py-2 border-b border-surface-100 last:border-0">
                        <CheckCircle2 className="h-4 w-4 text-green-500 flex-shrink-0" />
                        <div className="flex-1">
                          <p className="text-sm font-medium text-text-primary">{p.name}</p>
                          {p.description && <p className="text-xs text-text-tertiary">{p.description}</p>}
                        </div>
                        {p.duration && <span className="text-xs text-text-tertiary">{p.duration} min</span>}
                      </div>
                    ))}
                  </div>
                )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Insurance Pre-Authorization</CardTitle>
              <CardDescription>Submit pre-auth requests for insurance-covered services</CardDescription></CardHeader>
            <CardContent className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                {[
                  { label: "Client ID", key: "clientId", ph: "Client UUID" },
                  { label: "Insurance Provider", key: "insuranceProvider", ph: "e.g. UnitedHealth" },
                  { label: "Service Code (CPT/ICD)", key: "serviceCode", ph: "e.g. 99213" },
                  { label: "Estimated Amount", key: "amount", ph: "0.00", type: "number" },
                ].map((f) => (
                  <div key={f.key}>
                    <label className="block text-xs font-medium text-text-primary mb-1">{f.label}</label>
                    <input type={f.type ?? "text"} value={(preAuthForm as any)[f.key]} placeholder={f.ph}
                      onChange={(e) => setPreAuthForm((p) => ({ ...p, [f.key]: e.target.value }))}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  </div>
                ))}
              </div>
              <div className="flex justify-end">
                <Button variant="primary" size="sm" leftIcon={<Plus size={12} />} onClick={submitPreAuth}
                  disabled={!preAuthForm.clientId || !preAuthForm.insuranceProvider}>Submit Pre-Auth</Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {tab === "fitness" && (
        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><Activity size={16} className="text-green-500" /> Log Fitness Session</CardTitle>
            <CardDescription>Record training sessions for client progress tracking</CardDescription></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Client ID</label>
                <input value={fitForm.clientId} onChange={(e) => setFitForm((p) => ({ ...p, clientId: e.target.value }))} placeholder="Client UUID"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Session Type</label>
                <select value={fitForm.type} onChange={(e) => setFitForm((p) => ({ ...p, type: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  {FIT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Duration (minutes)</label>
                <input type="number" value={fitForm.duration} onChange={(e) => setFitForm((p) => ({ ...p, duration: Number(e.target.value) }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Notes</label>
                <input value={fitForm.notes} onChange={(e) => setFitForm((p) => ({ ...p, notes: e.target.value }))} placeholder="Optional session notes"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end">
              <Button variant="primary" leftIcon={logging ? <Loader2 size={14} className="animate-spin" /> : <Activity size={14} />}
                onClick={logFitness} disabled={!fitForm.clientId || logging}>{logging ? "Logging…" : "Log Session"}</Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
