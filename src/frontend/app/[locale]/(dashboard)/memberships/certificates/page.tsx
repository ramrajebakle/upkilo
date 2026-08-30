"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Award, Plus, Search, Loader2, Copy, RefreshCw, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Certificate { id: string; code: string; value: number; recipientName?: string; recipientEmail?: string; balance: number; expiresAt?: string; status: "Active" | "Redeemed" | "Expired"; issuedAt: string; }

const STATUS_CFG: Record<string, string> = {
  Active: "text-green-600 bg-green-50",
  Redeemed: "text-foreground-secondary bg-muted",
  Expired: "text-red-500 bg-red-50",
};

export default function GiftCertificatesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [certs, setCerts] = useState<Certificate[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ value: "50", recipientName: "", recipientEmail: "", message: "" });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/giftcertificates").catch(() => ({ data: [] }));
      setCerts(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleCreate = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/giftcertificates", { ...form, value: parseFloat(form.value) });
      toastSuccess("Gift certificate created"); setShowForm(false); setForm({ value: "50", recipientName: "", recipientEmail: "", message: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to create certificate"); }
    finally { setSaving(false); }
  };

  const filtered = certs.filter((c) => !search ||
    c.code?.toLowerCase().includes(search.toLowerCase()) ||
    c.recipientName?.toLowerCase().includes(search.toLowerCase()) ||
    c.recipientEmail?.toLowerCase().includes(search.toLowerCase())
  );

  const totalValue = certs.filter((c) => c.status === "Active").reduce((sum, c) => sum + (c.balance ?? 0), 0);

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Gift Certificates <Award className="text-warning-fg" size={22} /></h1>
          <p className="text-text-secondary mt-1">Issue and manage monetary gift certificates (distinct from Gift Cards).</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>New Certificate</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Issued", value: certs.length, color: "text-text-primary" },
          { label: "Active", value: certs.filter((c) => c.status === "Active").length, color: "text-success-fg" },
          { label: "Outstanding Value", value: `$${totalValue.toLocaleString()}`, color: "text-ai" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>New Gift Certificate</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Value ($) *</label>
                <input type="number" min="1" value={form.value} onChange={(e) => setForm((p) => ({ ...p, value: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Recipient Name</label>
                <input value={form.recipientName} onChange={(e) => setForm((p) => ({ ...p, recipientName: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Recipient Email</label>
                <input type="email" value={form.recipientEmail} onChange={(e) => setForm((p) => ({ ...p, recipientEmail: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Personal Message</label>
              <textarea value={form.message} onChange={(e) => setForm((p) => ({ ...p, message: e.target.value }))} rows={2}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Award size={14} />}
                onClick={handleCreate} disabled={!form.value || saving}>{saving ? "Creating…" : "Create Certificate"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by code, recipient name, or email…"
          className="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Award className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No gift certificates found</p>
          </CardContent></Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Code", "Recipient", "Value", "Balance", "Status", "Issued"].map((h) => (
                    <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {filtered.map((c) => (
                    <tr key={c.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-1">
                          <span className="text-xs font-mono font-semibold text-text-primary">{c.code}</span>
                          <button onClick={() => { navigator.clipboard.writeText(c.code); toastSuccess("Code copied"); }}
                            className="text-text-tertiary hover:text-text-primary"><Copy size={10} /></button>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <p className="text-xs font-medium text-text-primary">{c.recipientName ?? "—"}</p>
                        {c.recipientEmail && <p className="text-xs text-text-tertiary">{c.recipientEmail}</p>}
                      </td>
                      <td className="py-3 px-4 text-xs font-semibold text-text-primary">${c.value}</td>
                      <td className="py-3 px-4 text-xs font-semibold text-success-fg">${c.balance}</td>
                      <td className="py-3 px-4">
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_CFG[c.status] ?? STATUS_CFG.Active}`}>{c.status}</span>
                      </td>
                      <td className="py-3 px-4 text-xs text-text-tertiary">{new Date(c.issuedAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
        )}
    </div>
  );
}
