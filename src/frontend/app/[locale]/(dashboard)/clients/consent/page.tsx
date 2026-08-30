"use client";

import React, { useState, useEffect, useCallback } from "react";
import { FileText, Search, CheckCircle2, XCircle, Loader2, RefreshCw, Clock } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface ConsentRecord { id: string; clientId: string; clientName: string; consentType: string; givenAt?: string; revokedAt?: string; status: "Given" | "Revoked" | "Pending"; ipAddress?: string; }

const STATUS_CFG: Record<string, { color: string; bg: string; icon: React.ReactNode }> = {
  Given: { color: "text-green-600", bg: "bg-green-50", icon: <CheckCircle2 className="h-3 w-3" /> },
  Revoked: { color: "text-danger-fg", bg: "bg-red-50", icon: <XCircle className="h-3 w-3" /> },
  Pending: { color: "text-amber-600", bg: "bg-amber-50", icon: <Clock className="h-3 w-3" /> },
};

export default function ConsentPage() {
  const [records, setRecords] = useState<ConsentRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/consent").catch(() => ({ data: [] }));
      setRecords(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = records.filter((r) => {
    const matchSearch = !search || r.clientName?.toLowerCase().includes(search.toLowerCase()) || r.consentType?.toLowerCase().includes(search.toLowerCase());
    const matchStatus = statusFilter === "All" || r.status === statusFilter;
    return matchSearch && matchStatus;
  });

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Client Consent Records <FileText className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Audit trail of client consent for data processing, marketing, and medical treatments.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Records", value: records.length, color: "text-text-primary" },
          { label: "Consented", value: records.filter((r) => r.status === "Given").length, color: "text-success-fg" },
          { label: "Revoked", value: records.filter((r) => r.status === "Revoked").length, color: "text-danger-fg" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by client or consent type…"
            className="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        </div>
        <div className="flex gap-1 p-1 bg-surface-100 rounded-xl">
          {["All", "Given", "Revoked", "Pending"].map((f) => (
            <button key={f} onClick={() => setStatusFilter(f)}
              className={cn("px-3 py-1.5 text-xs font-medium rounded-lg transition-colors", statusFilter === f ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary")}>
              {f}
            </button>
          ))}
        </div>
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : (
          <Card>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Client", "Consent Type", "Status", "Given At", "IP Address"].map((h) => (
                    <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {filtered.map((r) => {
                    const cfg = STATUS_CFG[r.status] ?? STATUS_CFG.Pending;
                    return (
                      <tr key={r.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-xs font-medium text-text-primary">{r.clientName}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{r.consentType}</td>
                        <td className="py-3 px-4">
                          <span className={cn("inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>
                            {cfg.icon}{r.status}
                          </span>
                        </td>
                        <td className="py-3 px-4 text-xs text-text-tertiary">{r.givenAt ? new Date(r.givenAt).toLocaleDateString() : "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-tertiary font-mono">{r.ipAddress ?? "—"}</td>
                      </tr>
                    );
                  })}
                  {filtered.length === 0 && <tr><td colSpan={5} className="text-center py-10 text-text-tertiary text-xs">No consent records found</td></tr>}
                </tbody>
              </table>
            </CardContent>
          </Card>
        )}
    </div>
  );
}
