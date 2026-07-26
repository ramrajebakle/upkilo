"use client";

import React, { useState, useEffect } from "react";
import { Tag, Plus, Trash2, Copy, CheckCircle2, Loader2, RefreshCw, Search } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { useTenantCurrency } from '@/hooks/useTenantCurrency';

interface PromoCode { id: string; code: string; discountType: "percentage" | "fixed"; discountValue: number; maxUses?: number; usedCount: number; expiresAt?: string; isActive: boolean; }


// Formats in the tenant's own currency; the previous hardcoded ₹ was wrong for any
// tenant not billing in rupees.
function money(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount);
  } catch {
    return `${currency} ${Math.round(amount).toLocaleString()}`;
  }
}

export default function PromosPage() {
  const currency = useTenantCurrency();
  const { success: toastSuccess, error: toastError } = useToast();
  const [promos, setPromos] = useState<PromoCode[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [showNew, setShowNew] = useState(false);
  const [copied, setCopied] = useState<string | null>(null);
  const [form, setForm] = useState({ code: "", discountType: "percentage" as const, discountValue: 10, maxUses: "", expiresAt: "" });
  const [validateCode, setValidateCode] = useState("");
  const [validating, setValidating] = useState(false);
  const [validResult, setValidResult] = useState<{ valid: boolean; message?: string } | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/coupons").catch(() => ({ data: [] }));
      setPromos(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const create = async () => {
    if (!form.code.trim()) return;
    setCreating(true);
    try {
      await apiClient.post("/api/v1/coupons", {
        ...form, code: form.code.toUpperCase().trim(),
        maxUses: form.maxUses ? Number(form.maxUses) : null,
        expiresAt: form.expiresAt || null,
      });
      toastSuccess("Promo code created"); setShowNew(false); setForm({ code: "", discountType: "percentage", discountValue: 10, maxUses: "", expiresAt: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Create failed"); }
    finally { setCreating(false); }
  };

  const remove = async (id: string) => {
    setDeleting(id);
    try { await apiClient.delete(`/api/v1/coupons/${id}`); toastSuccess("Code deleted"); setPromos((p) => p.filter((x) => x.id !== id)); }
    catch { toastError("Delete failed"); }
    finally { setDeleting(null); }
  };

  const validate = async () => {
    if (!validateCode.trim()) return;
    setValidating(true); setValidResult(null);
    try {
      const r = await apiClient.post("/api/v1/coupons/validate", { code: validateCode });
      setValidResult({ valid: true, message: r.data?.data?.message ?? "Valid code" });
    } catch (e: any) { setValidResult({ valid: false, message: e?.response?.data?.error ?? "Invalid code" }); }
    finally { setValidating(false); }
  };

  const copyCode = (code: string) => {
    navigator.clipboard.writeText(code).then(() => { setCopied(code); setTimeout(() => setCopied(null), 1500); });
  };

  const filtered = promos.filter((p) => !search || p.code.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Promo Codes <Tag className="text-ai-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Create discount codes for clients. Share via campaigns or the booking page.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowNew(true)}>New Code</Button>
        </div>
      </header>

      {showNew && (
        <Card>
          <CardHeader><CardTitle>Create Promo Code</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Code *</label>
                <input value={form.code} onChange={(e) => setForm((p) => ({ ...p, code: e.target.value.toUpperCase() }))} placeholder="SAVE20"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono uppercase" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Discount Type</label>
                <select value={form.discountType} onChange={(e) => setForm((p) => ({ ...p, discountType: e.target.value as any }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  <option value="percentage">Percentage (%)</option>
                  <option value="fixed">Fixed Amount ({currency})</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Discount Value</label>
                <input type="number" value={form.discountValue} onChange={(e) => setForm((p) => ({ ...p, discountValue: Number(e.target.value) }))} min="1"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Max Uses (blank = unlimited)</label>
                <input type="number" value={form.maxUses} onChange={(e) => setForm((p) => ({ ...p, maxUses: e.target.value }))} placeholder="Unlimited"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Expiry Date (optional)</label>
                <input type="date" value={form.expiresAt} onChange={(e) => setForm((p) => ({ ...p, expiresAt: e.target.value }))}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowNew(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={creating ? <Loader2 size={13} className="animate-spin" /> : <Plus size={13} />}
                onClick={create} disabled={!form.code.trim() || creating}>{creating ? "Creating…" : "Create Code"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-3 gap-4">
        <div className="col-span-2">
          <div className="relative mb-3">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search codes…"
              className="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          </div>

          {loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : filtered.length === 0 ? (
              <Card><CardContent className="text-center py-10">
                <Tag className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
                <p className="text-sm text-text-tertiary">{search ? "No codes match" : "No promo codes yet"}</p>
              </CardContent></Card>
            ) : (
              <div className="space-y-2">
                {filtered.map((p) => (
                  <Card key={p.id}>
                    <CardContent className="pt-3 pb-3 flex items-center gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <code className="text-sm font-bold text-text-primary font-mono">{p.code}</code>
                          <button onClick={() => copyCode(p.code)} className="text-text-tertiary hover:text-ai-500">
                            {copied === p.code ? <CheckCircle2 size={12} className="text-green-500" /> : <Copy size={12} />}
                          </button>
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${p.isActive ? "text-green-700 bg-green-50" : "text-gray-500 bg-gray-100"}`}>{p.isActive ? "Active" : "Inactive"}</span>
                        </div>
                        <div className="flex items-center gap-3 mt-0.5">
                          <span className="text-xs text-text-secondary">{p.discountType === "percentage" ? `${p.discountValue}% off` : `${money(p.discountValue, currency)} off`}</span>
                          <span className="text-xs text-text-tertiary">{p.usedCount}{p.maxUses ? `/${p.maxUses}` : ""} uses</span>
                          {p.expiresAt && <span className="text-xs text-text-tertiary">Expires {new Date(p.expiresAt).toLocaleDateString()}</span>}
                        </div>
                      </div>
                      <Button variant="outline" size="sm"
                        leftIcon={deleting === p.id ? <Loader2 size={11} className="animate-spin" /> : <Trash2 size={11} className="text-red-500" />}
                        onClick={() => remove(p.id)} disabled={!!deleting} />
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
        </div>

        <Card>
          <CardHeader><CardTitle className="text-sm">Validate a Code</CardTitle><CardDescription className="text-xs">Test if a code is valid before sending to a client</CardDescription></CardHeader>
          <CardContent className="space-y-3">
            <input value={validateCode} onChange={(e) => setValidateCode(e.target.value.toUpperCase())} placeholder="Enter code…"
              className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono"
              onKeyDown={(e) => e.key === "Enter" && validate()} />
            <Button variant="outline" size="sm" className="w-full" leftIcon={validating ? <Loader2 size={12} className="animate-spin" /> : undefined}
              onClick={validate} disabled={!validateCode.trim() || validating}>Validate</Button>
            {validResult && (
              <div className={`flex items-center gap-2 p-2 rounded-lg text-xs ${validResult.valid ? "bg-green-50 text-green-700" : "bg-red-50 text-red-700"}`}>
                {validResult.valid ? <CheckCircle2 size={12} /> : <Tag size={12} />}
                {validResult.message}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
