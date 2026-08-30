"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Shield, Plus, Calendar, Trash2, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface ScheduleBlock { id: string; staffId?: string; staffName?: string; startTime: string; endTime: string; reason?: string; isRecurring: boolean; }

export default function ScheduleBlocksPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [blocks, setBlocks] = useState<ScheduleBlock[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ staffId: "", startTime: "", endTime: "", reason: "", isRecurring: false });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/schedule-blocks").catch(() => ({ data: [] }));
      setBlocks(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/schedule-blocks", form);
      toastSuccess("Schedule block added"); setShowForm(false); setForm({ staffId: "", startTime: "", endTime: "", reason: "", isRecurring: false }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to add block"); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    setDeleting(id);
    try {
      await apiClient.delete(`/api/schedule-blocks/${id}`);
      toastSuccess("Block removed"); setBlocks((b) => b.filter((x) => x.id !== id));
    } catch { toastError("Failed to remove block"); }
    finally { setDeleting(null); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Schedule Blocks <Shield className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Block time periods to prevent bookings (holidays, maintenance, closures).</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add Block</Button>
        </div>
      </header>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Schedule Block</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Start Time *</label>
                <input type="datetime-local" value={form.startTime} onChange={(e) => setForm((p) => ({ ...p, startTime: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">End Time *</label>
                <input type="datetime-local" value={form.endTime} onChange={(e) => setForm((p) => ({ ...p, endTime: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Reason</label>
              <input value={form.reason} onChange={(e) => setForm((p) => ({ ...p, reason: e.target.value }))} placeholder="e.g. Public holiday, Staff training, Maintenance"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="recurring" checked={form.isRecurring} onChange={(e) => setForm((p) => ({ ...p, isRecurring: e.target.checked }))} className="rounded" />
              <label htmlFor="recurring" className="text-sm text-text-primary cursor-pointer">Recurring weekly</label>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
                onClick={handleAdd} disabled={!form.startTime || !form.endTime || saving}>{saving ? "Saving…" : "Add Block"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : blocks.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Calendar className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No schedule blocks configured</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {blocks.map((b) => (
              <Card key={b.id}>
                <CardContent className="pt-4 pb-4 flex items-center justify-between gap-3">
                  <div className="flex items-start gap-3">
                    <div className="w-9 h-9 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                      <Shield className="h-4 w-4 text-text-tertiary" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-semibold text-text-primary">{b.reason || "Blocked time"}</p>
                        {b.isRecurring && <span className="text-xs bg-ai-subtle text-ai px-2 py-0.5 rounded-full">Recurring</span>}
                        {b.staffName && <span className="text-xs text-text-tertiary">{b.staffName}</span>}
                      </div>
                      <p className="text-xs text-text-secondary mt-0.5">
                        {new Date(b.startTime).toLocaleString()} → {new Date(b.endTime).toLocaleString()}
                      </p>
                    </div>
                  </div>
                  <Button variant="outline" size="sm" leftIcon={deleting === b.id ? <Loader2 size={12} className="animate-spin" /> : <Trash2 size={12} className="text-danger-fg" />}
                    onClick={() => handleDelete(b.id)} disabled={!!deleting}>Remove</Button>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
