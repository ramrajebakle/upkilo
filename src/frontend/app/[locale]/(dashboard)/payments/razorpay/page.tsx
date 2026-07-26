"use client";

import React, { useState, useEffect } from "react";
import { CreditCard, CheckCircle2, AlertCircle, Loader2, RefreshCw, ExternalLink, Save } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface RazorpayConfig { isEnabled: boolean; keyId?: string; webhookConfigured: boolean; testMode: boolean; currency: string; }
interface RazorpayOrder { id: string; amount: number; currency: string; status: string; clientName?: string; createdAt: string; }

export default function RazorpayPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [config, setConfig] = useState<RazorpayConfig | null>(null);
  const [orders, setOrders] = useState<RazorpayOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ keyId: "", keySecret: "", testMode: true, currency: "INR" });
  const [tab, setTab] = useState<"setup" | "orders">("setup");

  const load = async () => {
    setLoading(true);
    try {
      const [cfgRes, ordRes] = await Promise.all([
        apiClient.get("/api/v1/payments/razorpay/config").catch(() => ({ data: null })),
        apiClient.get("/api/v1/payments/razorpay/orders").catch(() => ({ data: [] })),
      ]);
      const cfg = cfgRes.data?.data ?? cfgRes.data ?? null;
      setConfig(cfg);
      if (cfg) setForm((p) => ({ ...p, keyId: cfg.keyId ?? "", testMode: cfg.testMode, currency: cfg.currency ?? "INR" }));
      setOrders(Array.isArray(ordRes.data) ? ordRes.data : ordRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    try {
      await apiClient.put("/api/v1/payments/razorpay/config", form);
      toastSuccess("Razorpay configuration saved"); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const Toggle = ({ label, desc, field }: { label: string; desc: string; field: "testMode" }) => (
    <div className="flex items-start justify-between gap-4">
      <div><p className="text-sm font-medium text-text-primary">{label}</p><p className="text-xs text-text-tertiary mt-0.5">{desc}</p></div>
      <div onClick={() => setForm((p) => ({ ...p, [field]: !p[field] }))}
        className={`w-10 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors mt-0.5 ${form[field] ? "bg-ai-500" : "bg-surface-300"}`}>
        <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${form[field] ? "translate-x-5" : "translate-x-0.5"}`} />
      </div>
    </div>
  );

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Razorpay Payments <CreditCard className="text-blue-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Accept INR and international payments via Razorpay (India-optimized).</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {config && (
        <div className={`flex items-center gap-3 p-4 rounded-xl border ${config.isEnabled ? "bg-green-50 border-green-200" : "bg-amber-50 border-amber-200"}`}>
          {config.isEnabled ? <CheckCircle2 className="h-5 w-5 text-green-600 flex-shrink-0" /> : <AlertCircle className="h-5 w-5 text-amber-600 flex-shrink-0" />}
          <div>
            <p className={`text-sm font-semibold ${config.isEnabled ? "text-green-800" : "text-amber-800"}`}>
              {config.isEnabled ? "Razorpay is Active" : "Razorpay Not Configured"}
            </p>
            <p className={`text-xs ${config.isEnabled ? "text-green-600" : "text-amber-600"}`}>
              {config.isEnabled ? `${config.testMode ? "Test" : "Live"} mode · ${config.currency} · Webhook ${config.webhookConfigured ? "✓" : "not set"}` : "Add your API keys below to enable Razorpay."}
            </p>
          </div>
          {config.isEnabled && (
            <a href="https://dashboard.razorpay.com" target="_blank" rel="noopener noreferrer" className="ml-auto">
              <Button variant="outline" size="sm" leftIcon={<ExternalLink size={12} />}>Dashboard</Button>
            </a>
          )}
        </div>
      )}

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {[{ k: "setup" as const, l: "Setup" }, { k: "orders" as const, l: `Orders (${orders.length})` }].map((t) => (
          <button key={t.k} onClick={() => setTab(t.k)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.k ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>{t.l}</button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "setup" && (
            <Card>
              <CardHeader><CardTitle>API Keys & Settings</CardTitle><CardDescription>Get your keys from the Razorpay dashboard under Settings → API Keys</CardDescription></CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Key ID</label>
                  <input value={form.keyId} onChange={(e) => setForm((p) => ({ ...p, keyId: e.target.value }))} placeholder="rzp_test_xxxxxxxxxxxx"
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Key Secret</label>
                  <input type="password" value={form.keySecret} onChange={(e) => setForm((p) => ({ ...p, keySecret: e.target.value }))} placeholder="Leave blank to keep existing"
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Currency</label>
                  <select value={form.currency} onChange={(e) => setForm((p) => ({ ...p, currency: e.target.value }))}
                    className="w-32 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                    {["INR", "USD", "EUR", "GBP", "AED", "SGD"].map((c) => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>
                <Toggle label="Test Mode" desc="Use Razorpay test keys (disable for production payments)" field="testMode" />
                <div className="flex justify-end">
                  <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                    onClick={save} disabled={saving}>{saving ? "Saving…" : "Save Configuration"}</Button>
                </div>
              </CardContent>
            </Card>
          )}

          {tab === "orders" && (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Order ID", "Client", "Amount", "Status", "Created"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {orders.map((o) => (
                      <tr key={o.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4 text-xs font-mono text-text-primary truncate max-w-[120px]">{o.id}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{o.clientName ?? "—"}</td>
                        <td className="py-3 px-4 text-xs font-semibold text-text-primary">{o.currency} {(o.amount / 100).toFixed(2)}</td>
                        <td className="py-3 px-4">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${o.status === "paid" ? "text-green-600 bg-green-50" : o.status === "created" ? "text-blue-500 bg-blue-50" : "text-gray-500 bg-gray-50"}`}>{o.status}</span>
                        </td>
                        <td className="py-3 px-4 text-xs text-text-tertiary">{new Date(o.createdAt).toLocaleDateString()}</td>
                      </tr>
                    ))}
                    {orders.length === 0 && <tr><td colSpan={5} className="text-center py-10 text-text-tertiary text-xs">No Razorpay orders yet</td></tr>}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
