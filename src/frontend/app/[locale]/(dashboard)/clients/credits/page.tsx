"use client";

import React, { useState, useCallback } from "react";
import { Coins, Search, Plus, Loader2, RefreshCw, TrendingUp } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface CreditBalance {
  clientId: string;
  clientName?: string;
  email?: string;
  balance: number;
  lastUpdated?: string;
}

export default function ClientCreditsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [search, setSearch] = useState("");
  const [results, setResults] = useState<CreditBalance[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [adjustClientId, setAdjustClientId] = useState<string | null>(null);
  const [adjustAmount, setAdjustAmount] = useState("");
  const [adjustReason, setAdjustReason] = useState("");
  const [adjustSaving, setAdjustSaving] = useState(false);

  const doSearch = useCallback(async (q: string) => {
    if (!q.trim()) { setResults(null); return; }
    setLoading(true);
    try {
      const clientsRes = await apiClient.get("/api/v1/clients", { params: { search: q, limit: 10 } });
      const clients = clientsRes.data?.data ?? clientsRes.data ?? [];
      const creditsWithBalances = await Promise.all(
        (Array.isArray(clients) ? clients : []).map(async (c: any) => {
          try {
            const r = await apiClient.get(`/api/v1/credits/client/${c.id}`);
            const d = r.data?.data ?? r.data ?? {};
            return { clientId: c.id, clientName: `${c.firstName} ${c.lastName}`, email: c.email, balance: d.balance ?? 0, lastUpdated: d.lastUpdated };
          } catch {
            return { clientId: c.id, clientName: `${c.firstName} ${c.lastName}`, email: c.email, balance: 0 };
          }
        })
      );
      setResults(creditsWithBalances);
    } catch { toastError("Search failed"); }
    finally { setLoading(false); }
  }, []);

  const handleAdjust = async () => {
    if (!adjustClientId || !adjustAmount) return;
    setAdjustSaving(true);
    try {
      await apiClient.post(`/api/v1/credits`, { clientId: adjustClientId, amount: parseFloat(adjustAmount), reason: adjustReason });
      toastSuccess("Credits updated");
      setAdjustClientId(null); setAdjustAmount(""); setAdjustReason("");
      doSearch(search);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to adjust credits"); }
    finally { setAdjustSaving(false); }
  };

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Client Credits <Coins className="text-warning-fg" size={22} /></h1>
        <p className="text-text-secondary mt-1">View and adjust account credit balances.</p>
      </header>

      <Card>
        <CardHeader><CardTitle>Search Client</CardTitle><CardDescription>Look up a client to view or adjust their credit balance</CardDescription></CardHeader>
        <CardContent>
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
              <input type="text" placeholder="Client name or email…" value={search}
                onChange={(e) => { setSearch(e.target.value); if (e.target.value.length > 2) doSearch(e.target.value); else setResults(null); }}
                className="w-full pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <Button variant="primary" leftIcon={loading ? <Loader2 size={14} className="animate-spin" /> : <Search size={14} />}
              onClick={() => doSearch(search)} disabled={!search.trim() || loading}>Search</Button>
          </div>
        </CardContent>
      </Card>

      {results !== null && (
        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><Coins className="h-4 w-4" /> Credit Balances</CardTitle>
            <CardDescription>{results.length} clients found</CardDescription></CardHeader>
          <CardContent>
            {results.length === 0 ? (
              <div className="text-center py-8 text-text-tertiary"><p>No clients found</p></div>
            ) : (
              <div className="space-y-3">
                {results.map((c) => (
                  <div key={c.clientId} className="flex items-center justify-between p-4 rounded-xl border border-surface-200 bg-surface-50">
                    <div>
                      <p className="font-semibold text-text-primary">{c.clientName}</p>
                      <p className="text-xs text-text-tertiary">{c.email}</p>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-right">
                        <p className={`text-xl font-bold ${c.balance > 0 ? "text-success-fg" : "text-text-tertiary"}`}>${c.balance.toFixed(2)}</p>
                        <p className="text-xs text-text-tertiary">balance</p>
                      </div>
                      {adjustClientId === c.clientId ? (
                        <div className="flex items-center gap-2">
                          <input type="number" value={adjustAmount} onChange={(e) => setAdjustAmount(e.target.value)} placeholder="Amount"
                            className="w-24 px-2 py-1.5 text-sm rounded border border-surface-200 focus:outline-none focus:ring-2 focus:ring-ai-500" />
                          <input type="text" value={adjustReason} onChange={(e) => setAdjustReason(e.target.value)} placeholder="Reason"
                            className="w-32 px-2 py-1.5 text-sm rounded border border-surface-200 focus:outline-none focus:ring-2 focus:ring-ai-500" />
                          <Button size="sm" variant="primary" leftIcon={adjustSaving ? <Loader2 size={12} className="animate-spin" /> : undefined}
                            onClick={handleAdjust} disabled={!adjustAmount || adjustSaving}>Save</Button>
                          <Button size="sm" variant="outline" onClick={() => setAdjustClientId(null)}>✕</Button>
                        </div>
                      ) : (
                        <Button size="sm" variant="outline" leftIcon={<Plus size={12} />} onClick={() => setAdjustClientId(c.clientId)}>Adjust</Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
