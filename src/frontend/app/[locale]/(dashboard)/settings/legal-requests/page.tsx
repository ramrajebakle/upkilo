"use client";

import React, { useState, useEffect } from "react";
import { Shield, FileText, Plus, Loader2, CheckCircle2, Clock, AlertCircle } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface GovernmentRequest { id: string; requestType: string; issuingAuthority: string; status: "pending" | "under_review" | "complied" | "challenged" | "rejected"; receivedAt: string; dueBy?: string; description?: string; }
interface TransparencyReport { period: string; totalRequests: number; complied: number; challenged: number; rejected: number; byType: Record<string, number>; }

const STATUS_MAP = {
  pending: { cls: "text-amber-700 bg-amber-50", label: "Pending" },
  under_review: { cls: "text-blue-700 bg-blue-50", label: "Under Review" },
  complied: { cls: "text-green-700 bg-green-50", label: "Complied" },
  challenged: { cls: "text-purple-700 bg-purple-50", label: "Challenged" },
  rejected: { cls: "text-red-700 bg-red-50", label: "Rejected" },
};

const REQUEST_TYPES = ["Search Warrant", "Subpoena", "Court Order", "National Security Letter", "Emergency Disclosure", "Voluntary Disclosure", "DPDP Data Request"];

export default function LegalRequestsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [requests, setRequests] = useState<GovernmentRequest[]>([]);
  const [report, setReport] = useState<TransparencyReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [tab, setTab] = useState<"requests" | "transparency" | "new">("requests");
  const [form, setForm] = useState({ requestType: "Subpoena", issuingAuthority: "", description: "", dueBy: "" });

  const load = async () => {
    setLoading(true);
    try {
      const [rRes, tRes] = await Promise.all([
        apiClient.get("/api/v1/legal/government-requests").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/legal/government-requests/transparency-report").catch(() => ({ data: null })),
      ]);
      setRequests(Array.isArray(rRes.data) ? rRes.data : rRes.data?.data ?? []);
      setReport(tRes.data?.data ?? tRes.data ?? null);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const submit = async () => {
    if (!form.issuingAuthority.trim()) return;
    setSubmitting(true);
    try {
      await apiClient.post("/api/v1/legal/government-requests", form);
      toastSuccess("Request logged"); setTab("requests"); setForm({ requestType: "Subpoena", issuingAuthority: "", description: "", dueBy: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Submit failed"); }
    finally { setSubmitting(false); }
  };

  const updateStatus = async (id: string, status: string) => {
    try {
      await apiClient.patch(`/api/v1/legal/government-requests/${id}/status`, { status });
      toastSuccess("Status updated"); load();
    } catch { toastError("Update failed"); }
  };

  const TABS = [{ k: "requests" as const, l: "Requests" }, { k: "transparency" as const, l: "Transparency Report" }, { k: "new" as const, l: "Log New Request" }];

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Government & Legal Requests <Shield className="text-red-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Track law enforcement requests, court orders, and government data disclosures.</p>
      </header>

      <div className="p-4 rounded-xl bg-blue-50 border border-blue-200">
        <p className="text-sm text-blue-800 font-medium">Confidentiality Notice</p>
        <p className="text-xs text-blue-600 mt-0.5">All entries are access-restricted and logged. Consult legal counsel before responding to any government request.</p>
      </div>

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl">
        {TABS.map((t) => (
          <button key={t.k} onClick={() => setTab(t.k)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.k ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>{t.l}</button>
        ))}
      </div>

      {loading && tab !== "new" ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "requests" && (
            requests.length === 0 ? (
              <Card><CardContent className="text-center py-10">
                <CheckCircle2 className="h-10 w-10 mx-auto mb-3 text-green-400" />
                <p className="text-sm text-text-tertiary">No government requests on record</p>
              </CardContent></Card>
            ) : (
              <div className="space-y-3">
                {requests.map((r) => {
                  const s = STATUS_MAP[r.status] ?? STATUS_MAP.pending;
                  return (
                    <Card key={r.id}>
                      <CardContent className="pt-4 pb-4">
                        <div className="flex items-start justify-between gap-4">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${s.cls}`}>{s.label}</span>
                              <span className="text-xs text-text-tertiary bg-surface-100 px-2 py-0.5 rounded">{r.requestType}</span>
                            </div>
                            <p className="text-sm font-semibold text-text-primary">{r.issuingAuthority}</p>
                            {r.description && <p className="text-xs text-text-secondary mt-0.5">{r.description}</p>}
                            <div className="flex items-center gap-3 mt-1">
                              <span className="text-xs text-text-tertiary">Received {new Date(r.receivedAt).toLocaleDateString()}</span>
                              {r.dueBy && <span className="text-xs text-red-600 font-medium">Due {new Date(r.dueBy).toLocaleDateString()}</span>}
                            </div>
                          </div>
                          {r.status === "pending" && (
                            <div className="flex gap-1 flex-shrink-0">
                              <Button variant="outline" size="sm" onClick={() => updateStatus(r.id, "under_review")}>Review</Button>
                              <Button variant="primary" size="sm" onClick={() => updateStatus(r.id, "complied")}>Comply</Button>
                            </div>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            )
          )}

          {tab === "transparency" && report && (
            <div className="space-y-4">
              <div className="grid grid-cols-4 gap-3">
                {[
                  { label: "Total", value: report.totalRequests },
                  { label: "Complied", value: report.complied, cls: "text-green-600" },
                  { label: "Challenged", value: report.challenged, cls: "text-purple-600" },
                  { label: "Rejected", value: report.rejected, cls: "text-red-600" },
                ].map((m) => (
                  <Card key={m.label}><CardContent className="pt-3 pb-3 text-center">
                    <p className={`text-2xl font-bold ${m.cls ?? "text-text-primary"}`}>{m.value}</p>
                    <p className="text-xs text-text-tertiary mt-0.5">{m.label}</p>
                  </CardContent></Card>
                ))}
              </div>
              {report.period && <p className="text-xs text-text-tertiary">Reporting period: {report.period}</p>}
              {report.byType && Object.keys(report.byType).length > 0 && (
                <Card>
                  <CardHeader><CardTitle className="text-base">Breakdown by Type</CardTitle></CardHeader>
                  <CardContent className="space-y-2">
                    {Object.entries(report.byType).map(([type, count]) => (
                      <div key={type} className="flex items-center justify-between">
                        <span className="text-sm text-text-secondary">{type}</span>
                        <span className="text-sm font-semibold text-text-primary">{count}</span>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              )}
            </div>
          )}

          {tab === "new" && (
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><Plus size={15} /> Log New Request</CardTitle>
                <CardDescription>Record a government or law enforcement request for your compliance register</CardDescription></CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-1">Request Type</label>
                    <select value={form.requestType} onChange={(e) => setForm((p) => ({ ...p, requestType: e.target.value }))}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                      {REQUEST_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-1">Issuing Authority *</label>
                    <input value={form.issuingAuthority} onChange={(e) => setForm((p) => ({ ...p, issuingAuthority: e.target.value }))} placeholder="e.g. Delhi High Court"
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-text-primary mb-1">Response Due By</label>
                    <input type="date" value={form.dueBy} onChange={(e) => setForm((p) => ({ ...p, dueBy: e.target.value }))}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-text-primary mb-1">Description / Reference</label>
                    <textarea value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} rows={3}
                      placeholder="Case number, scope of request, or internal notes…"
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
                  </div>
                </div>
                <div className="flex justify-end">
                  <Button variant="primary" leftIcon={submitting ? <Loader2 size={14} className="animate-spin" /> : <FileText size={14} />}
                    onClick={submit} disabled={!form.issuingAuthority.trim() || submitting}>{submitting ? "Logging…" : "Log Request"}</Button>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
