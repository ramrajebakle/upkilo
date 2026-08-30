"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  ShieldCheck, ShieldAlert, Lock, AlertTriangle, CheckCircle2,
  RefreshCw, Loader2, Eye, Globe, Users, Activity,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface SecurityOverview {
  period: { days: number; since: string };
  summary: {
    totalEvents: number; unresolvedCount: number; criticalCount: number;
    highCount: number; loginFailureRate: number; loginFailures: number; loginSuccesses: number;
  };
  bySeverity: { severity: string; count: number }[];
  unresolvedCritical: { id: string; severity: string; eventType: string; description: string; tenantId?: string; ipAddress?: string; occurredAt: string }[];
  targetedTenants: { tenantId: string; tenantName: string; failures: number }[];
}

const SEV_CFG: Record<string, { color: string; bg: string }> = {
  Critical: { color: "text-red-600", bg: "bg-red-50" },
  High: { color: "text-orange-600", bg: "bg-orange-50" },
  Medium: { color: "text-amber-600", bg: "bg-amber-50" },
  Low: { color: "text-blue-600", bg: "bg-blue-50" },
  Info: { color: "text-foreground-secondary", bg: "bg-muted" },
};

export default function PlatformSecurityPage() {
  const [data, setData] = useState<SecurityOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(7);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get(`/api/v1/super-admin/security/overview?days=${days}`).catch(() => ({ data: null }));
      setData(r.data?.data ?? r.data ?? null);
    } finally { setLoading(false); }
  }, [days]);

  useEffect(() => { load(); }, [load]);

  const stats = data?.summary;

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            Platform Security <ShieldCheck className="text-success-fg" size={22} />
          </h1>
          <p className="text-text-secondary mt-1">Real-time security events and threat monitoring across all tenants.</p>
        </div>
        <div className="flex items-center gap-2">
          <select value={days} onChange={(e) => setDays(Number(e.target.value))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
            {[1, 7, 14, 30].map((d) => <option key={d} value={d}>Last {d} day{d !== 1 ? "s" : ""}</option>)}
          </select>
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : !data ? (
        <Card><CardContent className="text-center py-10 text-text-tertiary"><ShieldAlert className="h-10 w-10 mx-auto mb-3 opacity-30" /><p>No security data available</p></CardContent></Card>
      ) : (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {[
              { label: "Total Events", value: stats!.totalEvents, icon: Activity, color: "text-blue-500" },
              { label: "Unresolved", value: stats!.unresolvedCount, icon: AlertTriangle, color: "text-warning-fg" },
              { label: "Critical", value: stats!.criticalCount, icon: ShieldAlert, color: "text-danger-fg" },
              { label: "Login Failure Rate", value: `${stats!.loginFailureRate}%`, icon: Lock, color: stats!.loginFailureRate > 10 ? "text-danger-fg" : "text-success-fg" },
            ].map((s) => (
              <Card key={s.label}>
                <CardContent className="pt-5 flex items-start gap-3">
                  <s.icon className={cn("h-5 w-5 mt-0.5 flex-shrink-0", s.color)} />
                  <div>
                    <p className="text-xs text-text-secondary">{s.label}</p>
                    <p className={cn("text-xl font-bold mt-0.5", s.color)}>{s.value}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader><CardTitle>Events by Severity</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                {data.bySeverity.map((s) => {
                  const cfg = SEV_CFG[s.severity] ?? SEV_CFG.Info;
                  return (
                    <div key={s.severity} className="flex items-center justify-between p-3 rounded-lg border border-surface-100">
                      <span className={cn("text-sm font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{s.severity}</span>
                      <span className="text-sm font-bold text-text-primary">{s.count}</span>
                    </div>
                  );
                })}
                {data.bySeverity.length === 0 && <p className="text-sm text-text-tertiary text-center py-4">No events in this period</p>}
              </CardContent>
            </Card>

            <Card>
              <CardHeader><CardTitle>Most Targeted Tenants</CardTitle><CardDescription>By failed login attempts</CardDescription></CardHeader>
              <CardContent className="space-y-2">
                {data.targetedTenants.map((t) => (
                  <div key={t.tenantId} className="flex items-center justify-between p-3 rounded-lg border border-surface-100">
                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-text-tertiary" />
                      <span className="text-sm text-text-primary">{t.tenantName}</span>
                    </div>
                    <span className="text-xs font-medium text-red-600 bg-red-50 px-2 py-0.5 rounded-full">{t.failures} failures</span>
                  </div>
                ))}
                {data.targetedTenants.length === 0 && <p className="text-sm text-text-tertiary text-center py-4">No targeted tenants</p>}
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><ShieldAlert className="h-4 w-4 text-danger-fg" /> Unresolved Critical / High Events</CardTitle></CardHeader>
            <CardContent>
              {data.unresolvedCritical.length === 0 ? (
                <div className="text-center py-8 text-text-tertiary">
                  <CheckCircle2 className="h-8 w-8 text-success-fg mx-auto mb-2" />
                  <p className="font-medium text-success-fg">No unresolved critical or high events</p>
                </div>
              ) : (
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Severity", "Event Type", "Description", "Tenant", "IP", "Time"].map((h) => (
                      <th key={h} className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {data.unresolvedCritical.map((e) => {
                      const cfg = SEV_CFG[e.severity] ?? SEV_CFG.Info;
                      return (
                        <tr key={e.id} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-2.5 px-3"><span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{e.severity}</span></td>
                          <td className="py-2.5 px-3 text-xs text-text-secondary font-mono">{e.eventType}</td>
                          <td className="py-2.5 px-3 text-xs text-text-primary max-w-xs truncate">{e.description}</td>
                          <td className="py-2.5 px-3 text-xs text-text-tertiary">{e.tenantId ?? "—"}</td>
                          <td className="py-2.5 px-3 text-xs text-text-tertiary font-mono">{e.ipAddress ?? "—"}</td>
                          <td className="py-2.5 px-3 text-xs text-text-tertiary">{new Date(e.occurredAt).toLocaleDateString()}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
