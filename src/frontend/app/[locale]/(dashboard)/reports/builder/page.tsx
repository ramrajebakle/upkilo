"use client";

import React, { useState, useEffect, useCallback } from "react";
import { BarChart3, Plus, Play, Trash2, Loader2, RefreshCw, Download } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface ReportDef { id: string; name: string; description?: string; metrics: string[]; dimensions: string[]; filters?: string; createdAt: string; }
interface ReportResult { columns: string[]; rows: (string | number)[][]; }

const METRICS = ["Revenue", "Bookings", "NewClients", "RetainedClients", "CancellationRate", "NoShowRate", "AverageBookingValue", "StaffUtilization", "ServiceRevenue"];
const DIMENSIONS = ["Day", "Week", "Month", "Service", "Staff", "Location", "ClientType"];

export default function ReportBuilderPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [defs, setDefs] = useState<ReportDef[]>([]);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<ReportResult | null>(null);
  const [activeReportId, setActiveReportId] = useState<string | null>(null);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", metrics: [] as string[], dimensions: [] as string[] });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/reports/definitions").catch(() => ({ data: [] }));
      setDefs(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const toggleArr = <T,>(arr: T[], val: T): T[] => arr.includes(val) ? arr.filter((x) => x !== val) : [...arr, val];

  const saveReport = async () => {
    if (!form.name || form.metrics.length === 0) return;
    setSaving(true);
    try {
      await apiClient.post("/api/v1/reports/definitions", form);
      toastSuccess("Report saved"); setShowNew(false); setForm({ name: "", description: "", metrics: [], dimensions: [] }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const runReport = async (id: string) => {
    setRunning(id); setActiveReportId(id); setResult(null);
    try {
      const r = await apiClient.post(`/api/v1/reports/definitions/${id}/run`);
      setResult(r.data?.data ?? r.data ?? null);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Report run failed"); }
    finally { setRunning(null); }
  };

  const deleteReport = async (id: string) => {
    try { await apiClient.delete(`/api/v1/reports/definitions/${id}`); toastSuccess("Report deleted"); setDefs((d) => d.filter((x) => x.id !== id)); }
    catch { toastError("Delete failed"); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Report Builder <BarChart3 className="text-ai-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Create and save custom reports from any combination of metrics and dimensions.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowNew(true)}>New Report</Button>
        </div>
      </header>

      {showNew && (
        <Card>
          <CardHeader><CardTitle>New Custom Report</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Report Name *</label>
                <input value={form.name} onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Description</label>
                <input value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-2">Metrics * (select at least one)</label>
                <div className="flex flex-wrap gap-1.5 p-3 rounded-lg border border-surface-200 bg-surface-50 min-h-[80px]">
                  {METRICS.map((m) => (
                    <button key={m} onClick={() => setForm((p) => ({ ...p, metrics: toggleArr(p.metrics, m) }))}
                      className={`text-xs px-2 py-1 rounded-full border transition-colors ${form.metrics.includes(m) ? "bg-ai-500 text-white border-ai-500" : "border-surface-300 text-text-secondary hover:border-ai-300"}`}>{m}</button>
                  ))}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-2">Group By (Dimensions)</label>
                <div className="flex flex-wrap gap-1.5 p-3 rounded-lg border border-surface-200 bg-surface-50 min-h-[80px]">
                  {DIMENSIONS.map((d) => (
                    <button key={d} onClick={() => setForm((p) => ({ ...p, dimensions: toggleArr(p.dimensions, d) }))}
                      className={`text-xs px-2 py-1 rounded-full border transition-colors ${form.dimensions.includes(d) ? "bg-green-500 text-white border-green-500" : "border-surface-300 text-text-secondary hover:border-green-300"}`}>{d}</button>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowNew(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
                onClick={saveReport} disabled={!form.name || form.metrics.length === 0 || saving}>{saving ? "Saving…" : "Save Report"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="space-y-2">
          <h2 className="text-sm font-semibold text-text-primary">Saved Reports ({defs.length})</h2>
          {loading ? <div className="flex justify-center py-6"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : defs.length === 0 ? <Card><CardContent className="text-center py-8 text-text-tertiary"><p className="text-sm">No saved reports yet</p></CardContent></Card>
            : defs.map((d) => (
              <Card key={d.id} className={activeReportId === d.id ? "border-ai-300" : ""}>
                <CardContent className="pt-3 pb-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-text-primary truncate">{d.name}</p>
                      {d.description && <p className="text-xs text-text-tertiary truncate">{d.description}</p>}
                      <p className="text-xs text-text-tertiary mt-0.5">{d.metrics.length} metrics · {new Date(d.createdAt).toLocaleDateString()}</p>
                    </div>
                    <div className="flex gap-1 flex-shrink-0">
                      <Button variant="primary" size="sm" leftIcon={running === d.id ? <Loader2 size={10} className="animate-spin" /> : <Play size={10} />}
                        onClick={() => runReport(d.id)} disabled={!!running}>Run</Button>
                      <Button variant="outline" size="sm" leftIcon={<Trash2 size={10} className="text-red-500" />}
                        onClick={() => deleteReport(d.id)} />
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
        </div>

        <div className="lg:col-span-2">
          {running ? <div className="flex items-center justify-center py-16 gap-3 text-text-tertiary"><Loader2 className="h-5 w-5 animate-spin" /><span className="text-sm">Running report…</span></div>
            : result ? (
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-base">Results ({result.rows.length} rows)</CardTitle>
                    <Button variant="outline" size="sm" leftIcon={<Download size={12} />}
                      onClick={() => {
                        const csv = [result.columns.join(","), ...result.rows.map((r) => r.join(","))].join("\n");
                        const a = document.createElement("a"); a.href = URL.createObjectURL(new Blob([csv], { type: "text/csv" })); a.download = "report.csv"; a.click();
                      }}>Export CSV</Button>
                  </div>
                </CardHeader>
                <CardContent className="p-0 overflow-auto max-h-[500px]">
                  <table className="w-full text-sm">
                    <thead className="sticky top-0 bg-white"><tr className="border-b border-surface-200">
                      {result.columns.map((c) => <th key={c} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase whitespace-nowrap">{c}</th>)}
                    </tr></thead>
                    <tbody>
                      {result.rows.map((row, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          {row.map((cell, j) => <td key={j} className="py-2 px-3 text-xs text-text-secondary whitespace-nowrap">{cell}</td>)}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </CardContent>
              </Card>
            ) : (
              <Card><CardContent className="text-center py-16 text-text-tertiary">
                <BarChart3 className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">Select a report and click Run</p>
                <p className="text-sm mt-1">Or create a new report with the button above.</p>
              </CardContent></Card>
            )}
        </div>
      </div>
    </div>
  );
}
