"use client";

import React, { useState, useEffect, useCallback } from "react";
import { UserCog, Search, Loader2, LogIn, AlertTriangle, Building2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { useRouter } from "next/navigation";

interface Tenant { id: string; name: string; slug: string; plan?: string; status?: string; ownerEmail?: string; }

export default function ImpersonatePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const router = useRouter();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [impersonating, setImpersonating] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/super-admin/tenants?limit=200").catch(() => ({ data: [] }));
      const items = r.data?.items ?? r.data?.data ?? r.data ?? [];
      setTenants(Array.isArray(items) ? items : []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const impersonate = async (tenant: Tenant) => {
    setImpersonating(tenant.id);
    try {
      const r = await apiClient.post(`/api/v1/super-admin/tenants/${tenant.id}/impersonate`);
      const token = r.data?.token ?? r.data?.data?.token;
      if (token) {
        localStorage.setItem("impersonationToken", token);
        localStorage.setItem("impersonatingTenant", JSON.stringify(tenant));
        toastSuccess(`Now impersonating ${tenant.name}`);
        router.push("/dashboard");
      } else {
        toastError("No impersonation token returned");
      }
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Impersonation failed"); }
    finally { setImpersonating(null); }
  };

  const filtered = tenants.filter((t) => !search ||
    t.name?.toLowerCase().includes(search.toLowerCase()) ||
    t.slug?.toLowerCase().includes(search.toLowerCase()) ||
    t.ownerEmail?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Tenant Impersonation <UserCog className="text-amber-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Temporarily access a tenant's dashboard for support or debugging.</p>
      </header>

      <div className="flex items-start gap-3 p-4 rounded-xl bg-amber-50 border border-amber-200">
        <AlertTriangle className="h-5 w-5 text-amber-600 flex-shrink-0 mt-0.5" />
        <div>
          <p className="text-sm font-semibold text-amber-800">Use with caution</p>
          <p className="text-xs text-amber-600 mt-0.5">Impersonation sessions are logged in the audit trail. Only use for legitimate support purposes.</p>
        </div>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by tenant name, slug, or owner email…"
          className="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : (
          <div className="space-y-2">
            {filtered.map((t) => (
              <Card key={t.id}>
                <CardContent className="pt-4 pb-4 flex items-center gap-4">
                  <div className="w-10 h-10 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                    <Building2 className="h-4 w-4 text-text-tertiary" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-semibold text-text-primary">{t.name}</p>
                    <div className="flex items-center gap-3 mt-0.5">
                      <span className="text-xs text-text-tertiary font-mono">{t.slug}</span>
                      {t.plan && <span className="text-xs text-ai-600 bg-ai-50 px-1.5 py-0.5 rounded-full">{t.plan}</span>}
                      {t.ownerEmail && <span className="text-xs text-text-tertiary">{t.ownerEmail}</span>}
                      {t.status && <span className={`text-xs font-medium px-1.5 py-0.5 rounded-full ${t.status === "Active" ? "text-green-600 bg-green-50" : "text-gray-500 bg-gray-50"}`}>{t.status}</span>}
                    </div>
                  </div>
                  <Button variant="outline" size="sm"
                    leftIcon={impersonating === t.id ? <Loader2 size={12} className="animate-spin" /> : <LogIn size={12} className="text-amber-600" />}
                    onClick={() => impersonate(t)} disabled={!!impersonating}>
                    {impersonating === t.id ? "Logging in…" : "Impersonate"}
                  </Button>
                </CardContent>
              </Card>
            ))}
            {filtered.length === 0 && (
              <Card><CardContent className="text-center py-10 text-text-tertiary"><Building2 className="h-8 w-8 mx-auto mb-2 opacity-20" /><p className="text-sm">No tenants found</p></CardContent></Card>
            )}
          </div>
        )}
    </div>
  );
}
