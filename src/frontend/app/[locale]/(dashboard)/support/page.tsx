"use client";

import React, { useState, useEffect, useCallback } from "react";
import { LifeBuoy, Plus, Clock, CheckCircle2, AlertCircle, Loader2, RefreshCw, MessageSquare } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface SupportTicket {
  id: string;
  subject: string;
  description: string;
  status: "Open" | "InProgress" | "Resolved" | "Closed";
  priority: "Low" | "Medium" | "High" | "Critical";
  createdAt: string;
  updatedAt?: string;
}

const STATUS_CFG: Record<string, { color: string; bg: string; icon: React.ReactNode }> = {
  Open: { color: "text-blue-600", bg: "bg-blue-50", icon: <Clock className="h-3 w-3" /> },
  InProgress: { color: "text-amber-600", bg: "bg-amber-50", icon: <Loader2 className="h-3 w-3" /> },
  Resolved: { color: "text-green-600", bg: "bg-green-50", icon: <CheckCircle2 className="h-3 w-3" /> },
  Closed: { color: "text-gray-500", bg: "bg-gray-50", icon: <CheckCircle2 className="h-3 w-3" /> },
};

const PRIORITY_CFG: Record<string, string> = {
  Low: "text-gray-500 bg-gray-50",
  Medium: "text-blue-500 bg-blue-50",
  High: "text-amber-600 bg-amber-50",
  Critical: "text-red-600 bg-red-50",
};

export default function SupportTicketsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ subject: "", description: "", priority: "Medium" });
  const [statusFilter, setStatusFilter] = useState("All");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/support-tickets").catch(() => ({ data: [] }));
      setTickets(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/support-tickets", form);
      toastSuccess("Support ticket submitted"); setShowForm(false); setForm({ subject: "", description: "", priority: "Medium" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to submit ticket"); }
    finally { setSaving(false); }
  };

  const filtered = tickets.filter((t) => statusFilter === "All" || t.status === statusFilter);
  const stats = { open: tickets.filter((t) => t.status === "Open").length, inProgress: tickets.filter((t) => t.status === "InProgress").length };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Support Tickets <LifeBuoy className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Submit and track support requests.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>New Ticket</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Open", value: stats.open, color: "text-blue-500" },
          { label: "In Progress", value: stats.inProgress, color: "text-amber-500" },
          { label: "Total", value: tickets.length, color: "text-text-primary" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Support Ticket</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Subject *</label>
              <input value={form.subject} onChange={(e) => setForm((p) => ({ ...p, subject: e.target.value }))} placeholder="Brief description of the issue"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Description *</label>
              <textarea value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} rows={4} placeholder="Detailed description…"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Priority</label>
              <select value={form.priority} onChange={(e) => setForm((p) => ({ ...p, priority: e.target.value }))}
                className="w-40 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {["Low", "Medium", "High", "Critical"].map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={saving}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <MessageSquare size={14} />}
                onClick={handleSubmit} disabled={!form.subject || !form.description || saving}>{saving ? "Submitting…" : "Submit Ticket"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-sm">
        {["All", "Open", "InProgress", "Resolved", "Closed"].map((f) => (
          <button key={f} onClick={() => setStatusFilter(f)}
            className={cn("flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors",
              statusFilter === f ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary")}>
            {f === "InProgress" ? "In Progress" : f}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <LifeBuoy className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">{statusFilter === "All" ? "No tickets yet" : `No ${statusFilter.toLowerCase()} tickets`}</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((t) => {
              const st = STATUS_CFG[t.status] ?? STATUS_CFG.Open;
              return (
                <Card key={t.id}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className={cn("inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full", st.color, st.bg)}>
                            {st.icon}{t.status}
                          </span>
                          <span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", PRIORITY_CFG[t.priority] ?? PRIORITY_CFG.Medium)}>{t.priority}</span>
                          <span className="text-xs text-text-tertiary">{new Date(t.createdAt).toLocaleDateString()}</span>
                        </div>
                        <p className="font-semibold text-text-primary">{t.subject}</p>
                        <p className="text-sm text-text-secondary mt-1 line-clamp-2">{t.description}</p>
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
