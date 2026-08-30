"use client";

import React, { useState, useEffect } from "react";
import { FileText, Eye, Ban, Loader2, Search, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface SignedWaiver { waiverId: string; waiverTitle: string; signedAt: string; status: string; expiresAt?: string; }
interface ClientOption { id: string; name: string; }

export default function SignedWaiversPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [clients, setClients] = useState<ClientOption[]>([]);
  const [clientId, setClientId] = useState("");
  const [search, setSearch] = useState("");
  const [waivers, setWaivers] = useState<SignedWaiver[]>([]);
  const [loading, setLoading] = useState(false);
  const [revoking, setRevoking] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get("/api/v1/clients?limit=200").catch(() => ({ data: [] })).then((r) => {
      const list = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setClients(list.map((c: any) => ({ id: c.id, name: `${c.firstName ?? ""} ${c.lastName ?? ""}`.trim() || c.name || c.email || c.id })));
    });
  }, []);

  useEffect(() => {
    if (!clientId) { setWaivers([]); return; }
    setLoading(true);
    apiClient.get(`/api/v1/waivers/client/${clientId}`).catch(() => ({ data: [] })).then((r) => {
      setWaivers(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    }).finally(() => setLoading(false));
  }, [clientId]);

  const viewPdf = (waiverId: string) => {
    const base = (apiClient.defaults.baseURL ?? "").replace(/\/$/, "");
    window.open(`${base}/api/v1/waivers/${waiverId}/client/${clientId}/pdf`, "_blank", "noopener");
  };

  const revoke = async (waiverId: string) => {
    setRevoking(waiverId);
    try {
      await apiClient.post(`/api/v1/waivers/${waiverId}/client/${clientId}/revoke`);
      toastSuccess("Waiver signature revoked");
      setWaivers((w) => w.map((x) => x.waiverId === waiverId ? { ...x, status: "Revoked" } : x));
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Revoke failed"); }
    finally { setRevoking(null); }
  };

  const filteredClients = clients.filter((c) => !search || c.name.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Signed Waivers <FileText className="text-ai" size={22} /></h1>
        <p className="text-text-secondary mt-1">Look up a client's signed waivers, preview the signed document, or revoke a signature.</p>
      </header>

      <Card>
        <CardHeader><CardTitle className="text-base">Select Client</CardTitle></CardHeader>
        <CardContent className="space-y-3">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search clients…"
              className="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          </div>
          <select value={clientId} onChange={(e) => setClientId(e.target.value)}
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
            <option value="">Choose a client…</option>
            {filteredClients.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </CardContent>
      </Card>

      {clientId && (
        loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
          : waivers.length === 0 ? (
            <Card><CardContent className="text-center py-10">
              <FileText className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
              <p className="text-sm text-text-tertiary">No signed waivers for this client</p>
            </CardContent></Card>
          ) : (
            <div className="space-y-3">
              {waivers.map((w) => (
                <Card key={w.waiverId}>
                  <CardContent className="pt-4 pb-4 flex items-center gap-3">
                    <div className="w-9 h-9 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                      <FileText className="h-4 w-4 text-ai" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-semibold text-text-primary">{w.waiverTitle}</p>
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${w.status === "Revoked" ? "text-red-700 bg-red-50" : "text-green-700 bg-green-50"}`}>
                          {w.status === "Revoked" ? "Revoked" : "Signed"}
                        </span>
                      </div>
                      <div className="flex items-center gap-3 mt-0.5">
                        <span className="text-xs text-text-tertiary">Signed {new Date(w.signedAt).toLocaleDateString()}</span>
                        {w.expiresAt && <span className="text-xs text-warning-fg">Expires {new Date(w.expiresAt).toLocaleDateString()}</span>}
                      </div>
                    </div>
                    <div className="flex gap-1 flex-shrink-0">
                      <Button variant="outline" size="sm" leftIcon={<Eye size={11} />} onClick={() => viewPdf(w.waiverId)}>View</Button>
                      {w.status !== "Revoked" && (
                        <Button variant="outline" size="sm"
                          leftIcon={revoking === w.waiverId ? <Loader2 size={11} className="animate-spin" /> : <Ban size={11} className="text-danger-fg" />}
                          onClick={() => revoke(w.waiverId)} disabled={!!revoking}>Revoke</Button>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )
      )}
    </div>
  );
}
