"use client";

import React, { useState, useEffect } from "react";
import { Clock, Plus, Trash2, Save, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface WorkingHours { dayOfWeek: number; startTime: string; endTime: string; isWorking: boolean; }
interface ScheduleException { id: string; date: string; reason?: string; type: "day-off" | "special-hours"; startTime?: string; endTime?: string; }

const DAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export default function StaffSchedulePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [staffId, setStaffId] = useState("");
  const [staffList, setStaffList] = useState<{ id: string; name: string }[]>([]);
  const [hours, setHours] = useState<WorkingHours[]>(
    DAYS.map((_, i) => ({ dayOfWeek: i, startTime: "09:00", endTime: "17:00", isWorking: i > 0 && i < 6 }))
  );
  const [exceptions, setExceptions] = useState<ScheduleException[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [excForm, setExcForm] = useState<{ date: string; reason: string; type: "day-off" | "special-hours"; startTime: string; endTime: string }>({ date: "", reason: "", type: "day-off", startTime: "", endTime: "" });

  useEffect(() => {
    apiClient.get("/api/v1/staff").catch(() => ({ data: [] })).then((r) => {
      const list = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setStaffList(list.map((s: any) => ({ id: s.id, name: `${s.firstName ?? ""} ${s.lastName ?? ""}`.trim() || s.name || s.id })));
    });
  }, []);

  useEffect(() => {
    if (!staffId) return;
    setLoading(true);
    Promise.all([
      apiClient.get(`/api/v1/schedule/staff/${staffId}`).catch(() => ({ data: null })),
      apiClient.get(`/api/v1/schedule/staff/${staffId}/exceptions/list`).catch(() => ({ data: [] })),
    ]).then(([hRes, eRes]) => {
      const h = hRes.data?.data ?? hRes.data;
      if (Array.isArray(h) && h.length > 0) setHours(h);
      setExceptions(Array.isArray(eRes.data) ? eRes.data : eRes.data?.data ?? []);
    }).finally(() => setLoading(false));
  }, [staffId]);

  const saveHours = async () => {
    if (!staffId) return;
    setSaving(true);
    try {
      await apiClient.put(`/api/v1/schedule/staff/${staffId}/working-hours`, hours);
      toastSuccess("Working hours saved");
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const addException = async () => {
    if (!staffId || !excForm.date) return;
    try {
      await apiClient.post(`/api/v1/schedule/staff/${staffId}/exceptions`, excForm);
      toastSuccess("Exception added");
      setExcForm({ date: "", reason: "", type: "day-off", startTime: "", endTime: "" });
      const r = await apiClient.get(`/api/v1/schedule/staff/${staffId}/exceptions/list`).catch(() => ({ data: [] }));
      setExceptions(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Add failed"); }
  };

  const removeException = async (id: string) => {
    if (!staffId) return;
    try {
      await apiClient.delete(`/api/v1/schedule/staff/${staffId}/exceptions/${id}`);
      setExceptions((e) => e.filter((x) => x.id !== id));
    } catch { toastError("Remove failed"); }
  };

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Staff Schedule <Clock className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Set working hours and schedule exceptions (days off, special hours) per staff member.</p>
      </header>

      <div className="flex items-center gap-3">
        <label className="text-sm font-medium text-text-primary whitespace-nowrap">Select Staff:</label>
        <select value={staffId} onChange={(e) => setStaffId(e.target.value)}
          className="flex-1 max-w-xs px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
          <option value="">Choose staff member…</option>
          {staffList.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        {staffId && <Button variant="outline" size="sm" leftIcon={<RefreshCw size={12} />} onClick={() => { setStaffId(""); setStaffId(staffId); }} disabled={loading} />}
      </div>

      {!staffId ? (
        <Card><CardContent className="text-center py-10">
          <Clock className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
          <p className="text-sm text-text-tertiary">Select a staff member to manage their schedule</p>
        </CardContent></Card>
      ) : loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div> : (
        <>
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div><CardTitle>Working Hours</CardTitle><CardDescription>Set regular weekly availability</CardDescription></div>
                <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={12} className="animate-spin" /> : <Save size={12} />}
                  onClick={saveHours} disabled={saving}>{saving ? "Saving…" : "Save Hours"}</Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-2">
              {hours.map((h, i) => (
                <div key={i} className="flex items-center gap-3 py-2 border-b border-surface-100 last:border-0">
                  <div onClick={() => setHours((prev) => prev.map((x, j) => j === i ? { ...x, isWorking: !x.isWorking } : x))}
                    className={`w-9 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors ${h.isWorking ? "bg-ai-500" : "bg-surface-300"}`}>
                    <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${h.isWorking ? "translate-x-4" : "translate-x-0.5"}`} />
                  </div>
                  <span className={`text-sm w-24 flex-shrink-0 ${h.isWorking ? "text-text-primary font-medium" : "text-text-tertiary"}`}>{DAYS[h.dayOfWeek]}</span>
                  {h.isWorking ? (
                    <div className="flex items-center gap-2">
                      <input type="time" value={h.startTime} onChange={(e) => setHours((prev) => prev.map((x, j) => j === i ? { ...x, startTime: e.target.value } : x))}
                        className="px-2 py-1 text-xs rounded border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500" />
                      <span className="text-text-tertiary text-xs">to</span>
                      <input type="time" value={h.endTime} onChange={(e) => setHours((prev) => prev.map((x, j) => j === i ? { ...x, endTime: e.target.value } : x))}
                        className="px-2 py-1 text-xs rounded border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500" />
                    </div>
                  ) : <span className="text-xs text-text-tertiary">Day off</span>}
                </div>
              ))}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Schedule Exceptions</CardTitle><CardDescription>One-off days off, holidays, or special hours</CardDescription></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-3 p-3 bg-surface-50 rounded-lg border border-surface-200">
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Date *</label>
                  <input type="date" value={excForm.date} onChange={(e) => setExcForm((p) => ({ ...p, date: e.target.value }))}
                    className="w-full px-2 py-1.5 text-sm rounded border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Type</label>
                  <select value={excForm.type} onChange={(e) => setExcForm((p) => ({ ...p, type: e.target.value as any }))}
                    className="w-full px-2 py-1.5 text-sm rounded border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                    <option value="day-off">Day Off</option>
                    <option value="special-hours">Special Hours</option>
                  </select>
                </div>
                {excForm.type === "special-hours" && (
                  <>
                    <div>
                      <label className="block text-xs font-medium text-text-primary mb-1">Start</label>
                      <input type="time" value={excForm.startTime} onChange={(e) => setExcForm((p) => ({ ...p, startTime: e.target.value }))}
                        className="w-full px-2 py-1.5 text-sm rounded border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-text-primary mb-1">End</label>
                      <input type="time" value={excForm.endTime} onChange={(e) => setExcForm((p) => ({ ...p, endTime: e.target.value }))}
                        className="w-full px-2 py-1.5 text-sm rounded border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                    </div>
                  </>
                )}
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-text-primary mb-1">Reason (optional)</label>
                  <input value={excForm.reason} onChange={(e) => setExcForm((p) => ({ ...p, reason: e.target.value }))} placeholder="e.g. Public holiday, Vacation"
                    className="w-full px-2 py-1.5 text-sm rounded border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
                <div className="col-span-2 flex justify-end">
                  <Button variant="primary" size="sm" leftIcon={<Plus size={12} />} onClick={addException} disabled={!excForm.date}>Add Exception</Button>
                </div>
              </div>

              {exceptions.length > 0 && (
                <div className="space-y-2">
                  {exceptions.map((ex) => (
                    <div key={ex.id} className="flex items-center justify-between py-2 border-b border-surface-100 last:border-0">
                      <div>
                        <span className="text-sm font-medium text-text-primary">{new Date(ex.date).toLocaleDateString()}</span>
                        <span className={`ml-2 text-xs px-2 py-0.5 rounded-full ${ex.type === "day-off" ? "bg-red-50 text-red-600" : "bg-blue-50 text-blue-600"}`}>{ex.type === "day-off" ? "Day Off" : "Special Hours"}</span>
                        {ex.reason && <span className="ml-2 text-xs text-text-tertiary">{ex.reason}</span>}
                        {ex.type === "special-hours" && ex.startTime && <span className="ml-2 text-xs text-text-secondary">{ex.startTime}–{ex.endTime}</span>}
                      </div>
                      <Button variant="outline" size="sm" leftIcon={<Trash2 size={11} className="text-red-500" />} onClick={() => removeException(ex.id)} />
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
