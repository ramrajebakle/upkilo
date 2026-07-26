"use client";

import React, { useState, useEffect } from "react";
import { Download, Plus, Loader2, RefreshCw, CheckCircle2, Clock, AlertCircle, FileText } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface ExportJob { id: string; type: string; status: "pending" | "processing" | "completed" | "failed"; createdAt: string; completedAt?: string; fileSize?: number; downloadUrl?: string; }

const EXPORT_TYPES = [
  { value: "clients", label: "Clients" },
  { value: "bookings", label: "Bookings" },
  { value: "payments", label: "Payments & Invoices" },
  { value: "staff", label: "Staff" },
  { value: "services", label: "Services" },
  { value: "inventory", label: "Inventory" },
  { value: "loyalty", label: "Loyalty Points" },
  { value: "audit-logs", label: "Audit Logs" },
];

const FORMAT_OPTIONS = ["csv", "xlsx", "json"];

export default function DataExportsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [exports, setExports] = useState<ExportJob[]>([]);
  const [loading, setLoading] = useState(true);
  const [requesting, setRequesting] = useState(false);
  const [form, setForm] = useState({ type: "clients", format: "csv", dateFrom: "", dateTo: "" });

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/dataexports").catch(() => ({ data: [] }));
      setExports(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const requestExport = async () => {
    setRequesting(true);
    try {
      await apiClient.post("/api/v1/dataexports/trigger", form);
      toastSuccess("Export job queued — check back shortly"); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Export request failed"); }
    finally { setRequesting(false); }
  };

  const download = async (job: ExportJob) => {
    try {
      const r = await apiClient.get(`/api/v1/dataexports/${job.id}/download`, { responseType: "blob" });
      const url = URL.createObjectURL(r.data);
      const a = document.createElement("a"); a.href = url; a.download = `export-${job.type}-${job.id}.${form.format}`; a.click();
      URL.revokeObjectURL(url);
    } catch { toastError("Download failed"); }
  };

  const pollStatus = async (id: string) => {
    try {
      const r = await apiClient.get(`/api/v1/dataexports/${id}/status`);
      const updated = r.data?.data ?? r.data;
      setExports((prev) => prev.map((e) => e.id === id ? { ...e, ...updated } : e));
    } catch { /* ignore */ }
  };

  const StatusBadge = ({ status }: { status: ExportJob["status"] }) => {
    const map = {
      pending: { cls: "text-amber-700 bg-amber-50", icon: <Clock size={10} />, label: "Pending" },
      processing: { cls: "text-blue-700 bg-blue-50", icon: <Loader2 size={10} className="animate-spin" />, label: "Processing" },
      completed: { cls: "text-green-700 bg-green-50", icon: <CheckCircle2 size={10} />, label: "Completed" },
      failed: { cls: "text-red-700 bg-red-50", icon: <AlertCircle size={10} />, label: "Failed" },
    };
    const s = map[status] ?? map.pending;
    return (
      <span className={`inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full ${s.cls}`}>
        {s.icon}{s.label}
      </span>
    );
  };

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Data Exports <Download className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Export your business data in CSV, Excel, or JSON format.</p>
      </header>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Plus size={16} /> New Export</CardTitle>
          <CardDescription>Choose the data type and date range for your export</CardDescription></CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Data Type</label>
              <select value={form.type} onChange={(e) => setForm((p) => ({ ...p, type: e.target.value }))}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {EXPORT_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Format</label>
              <select value={form.format} onChange={(e) => setForm((p) => ({ ...p, format: e.target.value }))}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {FORMAT_OPTIONS.map((f) => <option key={f} value={f}>{f.toUpperCase()}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">From Date (optional)</label>
              <input type="date" value={form.dateFrom} onChange={(e) => setForm((p) => ({ ...p, dateFrom: e.target.value }))}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">To Date (optional)</label>
              <input type="date" value={form.dateTo} onChange={(e) => setForm((p) => ({ ...p, dateTo: e.target.value }))}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
          </div>
          <div className="flex justify-end">
            <Button variant="primary" leftIcon={requesting ? <Loader2 size={14} className="animate-spin" /> : <Download size={14} />}
              onClick={requestExport} disabled={requesting}>{requesting ? "Queuing…" : "Request Export"}</Button>
          </div>
        </CardContent>
      </Card>

      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold text-text-primary">Export History ({exports.length})</h2>
          <Button variant="outline" size="sm" leftIcon={<RefreshCw size={12} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>

        {loading ? <div className="flex justify-center py-6"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
          : exports.length === 0 ? (
            <Card><CardContent className="text-center py-10">
              <FileText className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-30" />
              <p className="text-sm text-text-tertiary">No exports yet. Request one above.</p>
            </CardContent></Card>
          ) : (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Type", "Status", "Requested", "Completed", "Size", ""].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {exports.map((e) => (
                      <tr key={e.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-sm font-medium text-text-primary capitalize">{e.type.replace("-", " ")}</td>
                        <td className="py-3 px-4"><StatusBadge status={e.status} /></td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{new Date(e.createdAt).toLocaleString()}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{e.completedAt ? new Date(e.completedAt).toLocaleString() : "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{e.fileSize ? `${(e.fileSize / 1024).toFixed(1)} KB` : "—"}</td>
                        <td className="py-3 px-4 text-right">
                          {e.status === "processing" || e.status === "pending" ? (
                            <Button variant="outline" size="sm" leftIcon={<RefreshCw size={11} />} onClick={() => pollStatus(e.id)}>Check</Button>
                          ) : e.status === "completed" ? (
                            <Button variant="primary" size="sm" leftIcon={<Download size={11} />} onClick={() => download(e)}>Download</Button>
                          ) : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}
      </div>
    </div>
  );
}
