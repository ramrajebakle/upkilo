"use client";

import React, { useState, useEffect } from "react";
import { Globe, CheckCircle2, AlertCircle, Loader2, Save, ExternalLink } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface SocialProvider { provider: string; isEnabled: boolean; clientId?: string; }

export default function SocialLoginPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [providers, setProviders] = useState<SocialProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<string | null>(null);
  const [forms, setForms] = useState<Record<string, { clientId: string; clientSecret: string }>>({});

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/auth/social/providers").catch(() => ({ data: [] }));
      const list: SocialProvider[] = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setProviders(list);
      const initial: Record<string, { clientId: string; clientSecret: string }> = {};
      list.forEach((p) => { initial[p.provider] = { clientId: p.clientId ?? "", clientSecret: "" }; });
      setForms(initial);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async (provider: string) => {
    setSaving(provider);
    try {
      await apiClient.put(`/api/v1/auth/social/providers/${provider}`, forms[provider]);
      toastSuccess(`${provider} login configured`); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(null); }
  };

  const DEFAULT_PROVIDERS = [
    { provider: "Google", icon: "G", color: "text-red-500 bg-red-50", docsUrl: "https://console.cloud.google.com/", docsLabel: "Google Cloud Console" },
    { provider: "Apple", icon: "", color: "text-gray-900 bg-gray-100", docsUrl: "https://developer.apple.com/", docsLabel: "Apple Developer" },
    { provider: "Facebook", icon: "f", color: "text-blue-600 bg-blue-50", docsUrl: "https://developers.facebook.com/", docsLabel: "Meta Developers" },
  ];

  const mergedProviders = DEFAULT_PROVIDERS.map((dp) => ({
    ...dp,
    ...(providers.find((p) => p.provider === dp.provider) ?? { isEnabled: false }),
  }));

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Social Login <Globe className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Allow clients and staff to sign in with Google, Apple, or Facebook.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <div className="space-y-4">
          {mergedProviders.map((p) => (
            <Card key={p.provider}>
              <CardHeader>
                <CardTitle className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-lg ${p.color} flex items-center justify-center font-bold text-sm flex-shrink-0`}>{p.icon}</div>
                  <span>{p.provider}</span>
                  {(p as any).isEnabled ? <CheckCircle2 className="h-4 w-4 text-green-500" /> : <AlertCircle className="h-4 w-4 text-gray-400" />}
                  <a href={p.docsUrl} target="_blank" rel="noopener noreferrer" className="ml-auto">
                    <Button variant="outline" size="sm" leftIcon={<ExternalLink size={11} />}>{p.docsLabel}</Button>
                  </a>
                </CardTitle>
                <CardDescription>Configure OAuth 2.0 credentials from the {p.provider} developer console</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Client ID</label>
                  <input value={forms[p.provider]?.clientId ?? ""} onChange={(e) => setForms((f) => ({ ...f, [p.provider]: { ...f[p.provider], clientId: e.target.value } }))}
                    placeholder={`${p.provider} OAuth Client ID`}
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-primary mb-1">Client Secret</label>
                  <input type="password" value={forms[p.provider]?.clientSecret ?? ""} onChange={(e) => setForms((f) => ({ ...f, [p.provider]: { ...f[p.provider], clientSecret: e.target.value } }))}
                    placeholder="Leave blank to keep existing"
                    className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
                </div>
                <div className="flex justify-between items-center">
                  <p className="text-xs text-text-tertiary">Callback URL: <code className="bg-surface-100 px-1 rounded text-xs">{typeof window !== "undefined" ? `${window.location.origin}/api/auth/callback/${p.provider.toLowerCase()}` : ""}</code></p>
                  <Button variant="primary" size="sm" leftIcon={saving === p.provider ? <Loader2 size={12} className="animate-spin" /> : <Save size={12} />}
                    onClick={() => save(p.provider)} disabled={!!saving}>{saving === p.provider ? "Saving…" : "Save"}</Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
