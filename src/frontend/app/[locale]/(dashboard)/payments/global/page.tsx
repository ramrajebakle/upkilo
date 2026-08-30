"use client";

import React, { useEffect, useState } from "react";
import { Globe, CreditCard, Loader2, CheckCircle2, Info } from "lucide-react";
import { apiClient } from "@/lib/api";
import { useTenantCurrency } from "@/hooks/useTenantCurrency";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface PaymentMethod { id: string; name: string; type: string; currencies: string[]; available: boolean; }
interface ComplianceInfo { countryCode: string; requirements: string[]; restrictedAmounts?: string; taxNotes?: string; }

const COUNTRIES = [
  { code: "IN", name: "India" }, { code: "US", name: "United States" }, { code: "GB", name: "United Kingdom" },
  { code: "AE", name: "UAE" }, { code: "AU", name: "Australia" }, { code: "SG", name: "Singapore" },
  { code: "CA", name: "Canada" }, { code: "DE", name: "Germany" }, { code: "FR", name: "France" },
];

export default function GlobalPaymentsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [countryCode, setCountryCode] = useState("IN");
  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [compliance, setCompliance] = useState<ComplianceInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [initiating, setInitiating] = useState(false);
  // Defaulted to a hardcoded "INR", which charged the wrong currency for any tenant not
  // billing in rupees. The tenant's own configured currency is the only correct default here
  // (useTenantCurrency falls back to USD until it loads, or if the settings call fails).
  const tenantCurrency = useTenantCurrency();
  const [payForm, setPayForm] = useState({ amount: "", currency: "", methodId: "", description: "" });

  // Seed the field once the tenant currency resolves, without clobbering a typed override.
  useEffect(() => {
    setPayForm((p) => (p.currency ? p : { ...p, currency: tenantCurrency }));
  }, [tenantCurrency]);

  const fetchMethods = async (code: string) => {
    setLoading(true);
    try {
      const [mRes, cRes] = await Promise.all([
        apiClient.get(`/api/v1/global-payments/methods?countryCode=${code}`).catch(() => ({ data: [] })),
        apiClient.get(`/api/v1/global-payments/compliance/${code}`).catch(() => ({ data: null })),
      ]);
      setMethods(Array.isArray(mRes.data) ? mRes.data : mRes.data?.data ?? []);
      setCompliance(cRes.data?.data ?? cRes.data ?? null);
    } finally { setLoading(false); }
  };

  const handleCountryChange = (code: string) => { setCountryCode(code); fetchMethods(code); };

  const initiate = async () => {
    if (!payForm.amount || !payForm.methodId) return;
    setInitiating(true);
    try {
      await apiClient.post("/api/v1/global-payments/initiate", { ...payForm, countryCode, amount: Number(payForm.amount) });
      toastSuccess("Payment initiated"); setPayForm({ amount: "", currency: tenantCurrency, methodId: "", description: "" });
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Initiation failed"); }
    finally { setInitiating(false); }
  };

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Global Payments <Globe className="text-ai" size={22} /></h1>
        <p className="text-text-secondary mt-1">Accept payments with region-optimized methods and compliance checks.</p>
      </header>

      <Card>
        <CardHeader><CardTitle>Select Country</CardTitle><CardDescription>Payment methods and compliance information update per region</CardDescription></CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {COUNTRIES.map((c) => (
              <button key={c.code} onClick={() => handleCountryChange(c.code)}
                className={`px-3 py-1.5 text-xs font-medium rounded-lg border transition-colors ${countryCode === c.code ? "bg-ai-500 text-white border-ai-500" : "border-surface-200 text-text-secondary hover:border-ai/25 hover:text-text-primary"}`}>
                {c.name}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div> : (
        <>
          {methods.length > 0 && (
            <div>
              <h2 className="text-sm font-semibold text-text-primary mb-3">Available Payment Methods</h2>
              <div className="grid grid-cols-2 gap-3">
                {methods.map((m) => (
                  <Card key={m.id} className={m.available ? "" : "opacity-50"}>
                    <CardContent className="pt-3 pb-3 flex items-center gap-3">
                      <div className="w-8 h-8 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                        <CreditCard className="h-4 w-4 text-ai" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-text-primary">{m.name}</p>
                        <p className="text-xs text-text-tertiary">{m.type} · {(m.currencies ?? []).join(", ")}</p>
                      </div>
                      {m.available ? <CheckCircle2 className="h-4 w-4 text-success-fg flex-shrink-0" /> : null}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          )}

          {compliance && (
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><Info size={15} /> Compliance Requirements</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                {(compliance.requirements ?? []).map((req, i) => (
                  <div key={i} className="flex items-start gap-2">
                    <CheckCircle2 className="h-4 w-4 text-success-fg mt-0.5 flex-shrink-0" />
                    <p className="text-sm text-text-secondary">{req}</p>
                  </div>
                ))}
                {compliance.restrictedAmounts && <p className="text-xs text-amber-700 bg-amber-50 px-3 py-2 rounded-lg mt-2">{compliance.restrictedAmounts}</p>}
                {compliance.taxNotes && <p className="text-xs text-blue-700 bg-blue-50 px-3 py-2 rounded-lg">{compliance.taxNotes}</p>}
              </CardContent>
            </Card>
          )}

          {methods.length > 0 && (
            <Card>
              <CardHeader><CardTitle>Initiate Payment</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-text-primary mb-1">Amount</label>
                    <input type="number" value={payForm.amount} onChange={(e) => setPayForm((p) => ({ ...p, amount: e.target.value }))} placeholder="0.00"
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-text-primary mb-1">Currency</label>
                    <input value={payForm.currency} onChange={(e) => setPayForm((p) => ({ ...p, currency: e.target.value }))} placeholder={tenantCurrency}
                      className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  </div>
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Payment Method</label>
                  <select value={payForm.methodId} onChange={(e) => setPayForm((p) => ({ ...p, methodId: e.target.value }))}
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                    <option value="">Select method…</option>
                    {methods.filter((m) => m.available).map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Description</label>
                  <input value={payForm.description} onChange={(e) => setPayForm((p) => ({ ...p, description: e.target.value }))} placeholder="Payment description"
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
                <div className="flex justify-end">
                  <Button variant="primary" leftIcon={initiating ? <Loader2 size={14} className="animate-spin" /> : <CreditCard size={14} />}
                    onClick={initiate} disabled={!payForm.amount || !payForm.methodId || initiating}>
                    {initiating ? "Processing…" : "Initiate Payment"}
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
