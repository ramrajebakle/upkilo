"use client";

import React, { useState, useEffect } from "react";
import { AlertTriangle, CheckCircle2, Loader2, RefreshCw, BarChart3 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Escalation { id: string; module: string; severity: "critical" | "high" | "medium" | "low"; description: string; status: "open" | "resolved"; tenantId?: string; tenantName?: string; createdAt: string; resolvedAt?: string; }
interface EscalationStats { open: number; critical: number; avgResolutionHours: number; resolvedToday: number; }

const SEVERITY_MAP = {
  critical: "text-red-700 bg-red-50 border-red-200",
  high: "text-orange-700 bg-orange-50 border-orange-200",
  medium: "text-amber-700 bg-amber-50 border-amber-200",
  low: "text-blue-700 bg-blue-50 border-blue-200",
};

export default function EscalationsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [escalations, setEscalations] = useState<Escalation[]>([]);
  const [stats, setStats] = useState<EscalationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [resolving, setResolving] = useState<string | null>(null);
  const [moduleFilter, setModuleFilter] = useState("");
  const [severityFilter, setSeverityFilter] = useState("");
  const [resolution, setResolution] = useState<Record<string, string>>({});

  const load = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (moduleFilter) params.set("module", moduleFilter);
      if (severityFilter) params.set("severity", severityFilter);
      const [escRes, statsRes] = await Promise.all([
        apiClient.get(`/api/v1/escalations?${params}`).catch(() => ({ data: [] })),
        apiClient.get("/api/v1/escalations/stats").catch(() => ({ data: null })),
      ]);
      setEscalations(Array.isArray(escRes.data) ? escRes.data : escRes.data?.data ?? []);
      setStats(statsRes.data?.data ?? statsRes.data ?? null);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [moduleFilter, severityFilter]);

  const resolve = async (id: string) => {
    setResolving(id);
    try {
      await apiClient.post(`/api/v1/escalations/${id}/resolve`, { resolution: resolution[id] ?? "Resolved by admin" });
      toastSuccess("Escalation resolved"); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to resolve"); }
    finally { setResolving(null); }
  };

  const modules = Array.from(new Set(escalations.map((e) => e.module))).filter(Boolean);

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Escalations <AlertTriangle className="text-red-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Monitor and resolve critical issues flagged across all tenant accounts.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {stats && (
        <div className="grid grid-cols-4 gap-4">
          {[
            { label: "Open", value: stats.open, cls: "text-red-600" },
            { label: "Critical", value: stats.critical, cls: "text-orange-600" },
            { label: "Avg Resolution", value: `${stats.avgResolutionHours?.toFixed(1)}h`, cls: "text-blue-600" },
            { label: "Resolved Today", value: stats.resolvedToday, cls: "text-green-600" },
          ].map((m) => (
            <Card key={m.label}><CardContent className="pt-4 pb-4">
              <p className="text-xs text-text-tertiary font-medium">{m.label}</p>
              <p className={`text-2xl font-bold mt-1 ${m.cls}`}>{m.value}</p>
            </CardContent></Card>
          ))}
        </div>
      )}

      <div className="flex gap-3">
        <select value={moduleFilter} onChange={(e) => setModuleFilter(e.target.value)}
          className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
          <option value="">All Modules</option>
          {modules.map((m) => <option key={m} value={m}>{m}</option>)}
        </select>
        <select value={severityFilter} onChange={(e) => setSeverityFilter(e.target.value)}
          className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
          <option value="">All Severities</option>
          {["critical", "high", "medium", "low"].map((s) => <option key={s} value={s} className="capitalize">{s.charAt(0).toUpperCase() + s.slice(1)}</option>)}
        </select>
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : escalations.length === 0 ? (
          <Card><CardContent className="text-center py-12">
            <CheckCircle2 className="h-10 w-10 mx-auto mb-3 text-green-400" />
            <p className="text-sm text-text-tertiary font-medium">No escalations found</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {escalations.map((e) => (
              <Card key={e.id}>
                <CardContent className="pt-4 pb-4">
                  <div className="flex items-start gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap mb-1">
                        <span className={`text-xs font-semibold px-2 py-0.5 rounded-full border ${SEVERITY_MAP[e.severity] ?? SEVERITY_MAP.low}`}>{e.severity.toUpperCase()}</span>
                        <span className="text-xs font-medium text-text-tertiary bg-surface-100 px-2 py-0.5 rounded">{e.module}</span>
                        {e.tenantName && <span className="text-xs text-text-tertiary">Tenant: {e.tenantName}</span>}
                        <span className={`text-xs px-2 py-0.5 rounded-full ${e.status === "resolved" ? "text-green-600 bg-green-50" : "text-amber-600 bg-amber-50"}`}>{e.status}</span>
                      </div>
                      <p className="text-sm text-text-primary">{e.description}</p>
                      <p className="text-xs text-text-tertiary mt-1">{new Date(e.createdAt).toLocaleString()}</p>
                      {e.status === "open" && (
                        <input value={resolution[e.id] ?? ""} onChange={(ev) => setResolution((r) => ({ ...r, [e.id]: ev.target.value }))}
                          placeholder="Resolution notes (optional)…"
                          className="mt-2 w-full px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                      )}
                    </div>
                    {e.status === "open" && (
                      <Button variant="primary" size="sm" leftIcon={resolving === e.id ? <Loader2 size={11} className="animate-spin" /> : <CheckCircle2 size={11} />}
                        onClick={() => resolve(e.id)} disabled={!!resolving}>Resolve</Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
