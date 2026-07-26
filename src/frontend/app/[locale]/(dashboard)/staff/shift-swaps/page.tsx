"use client";

import React, { useState, useEffect, useCallback } from "react";
import { ArrowLeftRight, Clock, CheckCircle2, XCircle, Loader2, RefreshCw, AlertCircle } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Shift { id: string; staffName?: string; date: string; startTime: string; endTime: string; }
interface SwapRequest {
  id: string;
  requestingStaffName?: string;
  targetStaffName?: string;
  requestingShiftDate?: string;
  reason?: string;
  status: "Pending" | "Accepted" | "Approved" | "Rejected";
  createdAt: string;
}

const STATUS_CFG: Record<string, { color: string; bg: string; icon: React.ReactNode }> = {
  Pending: { color: "text-amber-600", bg: "bg-amber-50", icon: <Clock className="h-3.5 w-3.5" /> },
  Accepted: { color: "text-blue-600", bg: "bg-blue-50", icon: <CheckCircle2 className="h-3.5 w-3.5" /> },
  Approved: { color: "text-green-600", bg: "bg-green-50", icon: <CheckCircle2 className="h-3.5 w-3.5" /> },
  Rejected: { color: "text-red-600", bg: "bg-red-50", icon: <XCircle className="h-3.5 w-3.5" /> },
};

export default function ShiftSwapsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [swaps, setSwaps] = useState<SwapRequest[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [selectedShift, setSelectedShift] = useState("");
  const [reason, setReason] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [actioning, setActioning] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("All");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [swapRes, shiftRes] = await Promise.all([
        apiClient.get("/api/v1/staff/shifts/swaps").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/staff/shifts").catch(() => ({ data: [] })),
      ]);
      setSwaps(Array.isArray(swapRes.data) ? swapRes.data : swapRes.data?.data ?? []);
      setShifts(Array.isArray(shiftRes.data) ? shiftRes.data : shiftRes.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const handleRequest = async () => {
    if (!selectedShift) return;
    setSubmitting(true);
    try {
      await apiClient.post("/api/v1/staff/shifts/swap-request", { shiftId: selectedShift, reason });
      toastSuccess("Swap request submitted");
      setShowForm(false); setSelectedShift(""); setReason(""); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to submit request"); }
    finally { setSubmitting(false); }
  };

  const handleAction = async (swapId: string, action: "accept" | "approve" | "reject") => {
    setActioning(swapId);
    try {
      await apiClient.post(`/api/v1/staff/shifts/swap-${action}/${swapId}`);
      toastSuccess(`Swap ${action}d`);
      fetch();
    } catch { toastError(`Failed to ${action} swap`); }
    finally { setActioning(null); }
  };

  const filtered = swaps.filter((s) => statusFilter === "All" || s.status === statusFilter);

  const counts = {
    Pending: swaps.filter((s) => s.status === "Pending").length,
    Accepted: swaps.filter((s) => s.status === "Accepted").length,
    Approved: swaps.filter((s) => s.status === "Approved").length,
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Shift Swaps <ArrowLeftRight className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Manage staff shift swap requests and approvals.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<ArrowLeftRight size={14} />} onClick={() => setShowForm(true)}>Request Swap</Button>
        </div>
      </header>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Pending Review", value: counts.Pending, color: "text-amber-500" },
          { label: "Accepted (awaiting approval)", value: counts.Accepted, color: "text-blue-500" },
          { label: "Approved", value: counts.Approved, color: "text-green-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardContent className="pt-5">
              <p className="text-xs text-text-secondary">{s.label}</p>
              <p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Request form */}
      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Swap Request</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Select your shift to swap *</label>
                <select value={selectedShift} onChange={(e) => setSelectedShift(e.target.value)}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  <option value="">-- Choose shift --</option>
                  {shifts.map((s) => (
                    <option key={s.id} value={s.id}>{s.date} {s.startTime}–{s.endTime}{s.staffName ? ` (${s.staffName})` : ""}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Reason (optional)</label>
                <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="e.g. Personal appointment"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex items-start gap-2 p-3 bg-amber-50 rounded-lg border border-amber-200">
              <AlertCircle className="h-4 w-4 text-amber-600 mt-0.5 flex-shrink-0" />
              <p className="text-xs text-amber-700">Your request will be visible to other staff who can accept it. An admin must then approve the swap.</p>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={submitting}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={submitting ? <Loader2 size={14} className="animate-spin" /> : <ArrowLeftRight size={14} />}
                onClick={handleRequest} disabled={!selectedShift || submitting}>
                {submitting ? "Submitting…" : "Submit Request"}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Filter tabs */}
      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-sm">
        {["All", "Pending", "Accepted", "Approved", "Rejected"].map((f) => (
          <button key={f} onClick={() => setStatusFilter(f)}
            className={cn("flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors",
              statusFilter === f ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary")}>
            {f}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <ArrowLeftRight className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No swap requests found</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((s) => {
              const cfg = STATUS_CFG[s.status] ?? STATUS_CFG.Pending;
              return (
                <Card key={s.id}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className={cn("inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>
                            {cfg.icon}{s.status}
                          </span>
                          <span className="text-xs text-text-tertiary">{new Date(s.createdAt).toLocaleDateString()}</span>
                        </div>
                        <p className="text-sm font-medium text-text-primary">
                          {s.requestingStaffName ?? "Staff member"}
                          {s.targetStaffName ? ` ↔ ${s.targetStaffName}` : " (open swap)"}
                        </p>
                        {s.requestingShiftDate && <p className="text-xs text-text-secondary">Shift: {s.requestingShiftDate}</p>}
                        {s.reason && <p className="text-xs text-text-tertiary mt-0.5">Reason: {s.reason}</p>}
                      </div>
                      <div className="flex gap-1.5 flex-shrink-0">
                        {s.status === "Pending" && (
                          <Button variant="outline" size="sm"
                            leftIcon={actioning === s.id ? <Loader2 size={12} className="animate-spin" /> : <CheckCircle2 size={12} />}
                            onClick={() => handleAction(s.id, "accept")} disabled={!!actioning}>
                            Accept
                          </Button>
                        )}
                        {s.status === "Accepted" && (
                          <Button variant="primary" size="sm"
                            leftIcon={actioning === s.id ? <Loader2 size={12} className="animate-spin" /> : <CheckCircle2 size={12} />}
                            onClick={() => handleAction(s.id, "approve")} disabled={!!actioning}>
                            Approve
                          </Button>
                        )}
                        {(s.status === "Pending" || s.status === "Accepted") && (
                          <Button variant="outline" size="sm"
                            leftIcon={<XCircle size={12} className="text-red-500" />}
                            onClick={() => handleAction(s.id, "reject")} disabled={!!actioning}>
                            Reject
                          </Button>
                        )}
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
