"use client";

import React, { useState, useEffect } from "react";
import { FlaskConical, Play, Square, Database, Loader2, AlertTriangle, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface DemoStatus { isEnabled: boolean; seededAt?: string; clientCount?: number; bookingCount?: number; }

export default function DemoModePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [status, setStatus] = useState<DemoStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [toggling, setToggling] = useState(false);
  const [seeding, setSeeding] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/demo/status").catch(() => ({ data: null }));
      setStatus(r.data?.data ?? r.data ?? null);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const enable = async () => {
    setToggling(true);
    try { await apiClient.post("/api/v1/demo/enable"); toastSuccess("Demo mode enabled"); load(); }
    catch (e: any) { toastError(e?.response?.data?.error ?? "Failed"); }
    finally { setToggling(false); }
  };

  const disable = async () => {
    setToggling(true);
    try { await apiClient.post("/api/v1/demo/disable"); toastSuccess("Demo mode disabled"); load(); }
    catch (e: any) { toastError(e?.response?.data?.error ?? "Failed"); }
    finally { setToggling(false); }
  };

  const seed = async () => {
    setSeeding(true);
    try { await apiClient.post("/api/v1/demo/seed"); toastSuccess("Demo data seeded successfully"); load(); }
    catch (e: any) { toastError(e?.response?.data?.error ?? "Seeding failed"); }
    finally { setSeeding(false); }
  };

  return (
    <div className="max-w-xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Demo Mode <FlaskConical className="text-warning-fg" size={22} /></h1>
        <p className="text-text-secondary mt-1">Enable demo mode to showcase the platform with realistic sample data.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : status && (
          <>
            <div className={`flex items-center gap-3 p-4 rounded-xl border ${status.isEnabled ? "bg-amber-50 border-amber-200" : "bg-surface-50 border-surface-200"}`}>
              {status.isEnabled ? <AlertTriangle className="h-5 w-5 text-warning-fg flex-shrink-0" /> : <CheckCircle2 className="h-5 w-5 text-text-tertiary flex-shrink-0" />}
              <div>
                <p className={`text-sm font-semibold ${status.isEnabled ? "text-amber-800" : "text-text-primary"}`}>
                  Demo mode is {status.isEnabled ? "ENABLED" : "disabled"}
                </p>
                {status.isEnabled
                  ? <p className="text-xs text-warning-fg mt-0.5">Live data is hidden. Clients see demo content.</p>
                  : <p className="text-xs text-text-tertiary mt-0.5">Platform operates normally with your real data.</p>
                }
              </div>
              <Button variant={status.isEnabled ? "outline" : "primary"} size="sm" className="ml-auto"
                leftIcon={toggling ? <Loader2 size={12} className="animate-spin" /> : status.isEnabled ? <Square size={12} /> : <Play size={12} />}
                onClick={status.isEnabled ? disable : enable} disabled={toggling}>
                {toggling ? "…" : status.isEnabled ? "Disable" : "Enable"}
              </Button>
            </div>

            {status.isEnabled && status.seededAt && (
              <div className="grid grid-cols-3 gap-3">
                {[
                  { label: "Seeded", value: new Date(status.seededAt).toLocaleDateString() },
                  { label: "Demo Clients", value: status.clientCount?.toLocaleString() ?? "—" },
                  { label: "Demo Bookings", value: status.bookingCount?.toLocaleString() ?? "—" },
                ].map((m) => (
                  <Card key={m.label}><CardContent className="pt-3 pb-3 text-center">
                    <p className="text-lg font-bold text-text-primary">{m.value}</p>
                    <p className="text-xs text-text-tertiary mt-0.5">{m.label}</p>
                  </CardContent></Card>
                ))}
              </div>
            )}

            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><Database size={16} /> Seed Demo Data</CardTitle>
                <CardDescription>Populate the account with realistic clients, bookings, and history for demonstration purposes</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="p-3 rounded-lg bg-amber-50 border border-amber-200">
                  <p className="text-xs text-amber-800 font-medium">Seeding creates sample data and does not overwrite real records. Disable demo mode to hide sample data again.</p>
                </div>
                <Button variant="outline" leftIcon={seeding ? <Loader2 size={14} className="animate-spin" /> : <Database size={14} />}
                  onClick={seed} disabled={seeding}>{seeding ? "Seeding…" : "Seed Demo Data"}</Button>
              </CardContent>
            </Card>
          </>
        )}
    </div>
  );
}
