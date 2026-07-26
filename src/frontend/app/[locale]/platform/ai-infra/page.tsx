"use client";

import React, { useState, useEffect } from "react";
import { Sparkles, Cpu, Zap, Clock, TrendingUp, AlertCircle, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

interface ModelStat { model: string; requests: number; usage: number; avgLatencyMs: number; errorRate: number; costUsd: number; status: string; }
interface AISummary { totalRequests24h: number; avgLatencyMs: number; totalCostUsd24h: number; errorRate: number; }

export default function AIInfraPage() {
  const [summary, setSummary] = useState<AISummary | null>(null);
  const [models, setModels] = useState<ModelStat[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const [sumRes, modRes] = await Promise.all([
        apiClient.get("/api/v1/super-admin/ai-infrastructure/summary").catch(() => ({ data: null })),
        apiClient.get("/api/v1/super-admin/ai-infrastructure/models").catch(() => ({ data: [] })),
      ]);
      setSummary(sumRes.data?.data ?? sumRes.data ?? null);
      setModels(Array.isArray(modRes.data) ? modRes.data : modRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">AI Infrastructure <Sparkles className="text-ai-500" size={24} /></h1>
          <p className="text-text-secondary mt-1">Model usage, latency, and cost across all tenants.</p>
        </div>
        <Button variant="outline" leftIcon={loading ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {loading ? <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {summary && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: "Total AI Requests (24h)", value: summary.totalRequests24h?.toLocaleString() ?? "—", icon: Zap, color: "text-yellow-500" },
                { label: "Avg Latency", value: summary.avgLatencyMs ? `${(summary.avgLatencyMs / 1000).toFixed(1)}s` : "—", icon: Clock, color: "text-blue-500" },
                { label: "Cost (24h)", value: summary.totalCostUsd24h ? `$${summary.totalCostUsd24h.toFixed(2)}` : "—", icon: TrendingUp, color: "text-green-500" },
                { label: "Error Rate", value: summary.errorRate != null ? `${summary.errorRate.toFixed(2)}%` : "—", icon: AlertCircle, color: summary.errorRate > 1 ? "text-red-500" : "text-green-500" },
              ].map((s) => (
                <Card key={s.label}>
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-text-secondary">{s.label}</CardTitle>
                    <s.icon className={`h-4 w-4 ${s.color}`} />
                  </CardHeader>
                  <CardContent><p className="text-2xl font-bold text-text-primary">{s.value}</p></CardContent>
                </Card>
              ))}
            </div>
          )}

          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><Cpu className="h-4 w-4" /> Model Performance</CardTitle>
              <CardDescription>Per-model usage, latency, and cost breakdown</CardDescription></CardHeader>
            <CardContent>
              {models.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No model data available</p> : (
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Model", "Requests", "Usage %", "Avg Latency", "Error Rate", "Cost (USD)", "Status"].map((h) => (
                      <th key={h} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {models.map((m, i) => (
                      <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-2 px-3 text-xs font-mono font-medium text-text-primary">{m.model}</td>
                        <td className="py-2 px-3 text-xs text-text-secondary">{m.requests?.toLocaleString()}</td>
                        <td className="py-2 px-3">
                          <div className="flex items-center gap-1.5">
                            <div className="w-12 h-1.5 bg-surface-200 rounded-full overflow-hidden"><div className="h-full bg-ai-500 rounded-full" style={{ width: `${m.usage}%` }} /></div>
                            <span className="text-xs text-text-tertiary">{m.usage}%</span>
                          </div>
                        </td>
                        <td className="py-2 px-3 text-xs text-text-secondary">{m.avgLatencyMs ? `${(m.avgLatencyMs / 1000).toFixed(2)}s` : "—"}</td>
                        <td className="py-2 px-3 text-xs text-text-secondary">{m.errorRate != null ? `${m.errorRate.toFixed(2)}%` : "—"}</td>
                        <td className="py-2 px-3 text-xs font-medium text-green-600">${m.costUsd?.toFixed(4)}</td>
                        <td className="py-2 px-3">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${m.status === "Healthy" ? "text-green-600 bg-green-50" : "text-amber-600 bg-amber-50"}`}>{m.status ?? "—"}</span>
                        </td>
                      </tr>
                    ))}
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
