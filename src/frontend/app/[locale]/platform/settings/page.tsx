"use client";

import React, { useState, useEffect } from "react";
import { Settings, Globe, Shield, Save, Loader2, RefreshCw, ToggleLeft } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface PlatformSettings {
  platformName: string;
  supportEmail: string;
  enforceTwoFactorGlobal: boolean;
  maintenanceMode: boolean;
  allowNewRegistrations: boolean;
  defaultTenantTier: string;
  apiRateLimit: number;
  smtpConfigured: boolean;
  stripeConnected: boolean;
}

const DEFAULT: PlatformSettings = {
  platformName: "Upkilo",
  supportEmail: "support@upkilo.com",
  enforceTwoFactorGlobal: false,
  maintenanceMode: false,
  allowNewRegistrations: true,
  defaultTenantTier: "Starter",
  apiRateLimit: 1000,
  smtpConfigured: false,
  stripeConnected: false,
};

export default function PlatformSettingsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [settings, setSettings] = useState<PlatformSettings>(DEFAULT);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    apiClient.get("/api/v1/super-admin/settings")
      .then((r) => setSettings({ ...DEFAULT, ...(r.data?.data ?? r.data ?? {}) }))
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const setStr = (k: keyof PlatformSettings, v: string) => setSettings((s) => ({ ...s, [k]: v }));
  const setNum = (k: keyof PlatformSettings, v: string) => setSettings((s) => ({ ...s, [k]: parseInt(v) || 0 }));
  const setBool = (k: keyof PlatformSettings, v: boolean) => setSettings((s) => ({ ...s, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiClient.put("/api/v1/super-admin/settings", settings);
      toastSuccess("Platform settings saved");
    } catch { toastError("Failed to save settings"); }
    finally { setSaving(false); }
  };

  const Toggle = ({ label, desc, field, danger }: { label: string; desc: string; field: keyof PlatformSettings; danger?: boolean }) => (
    <div className="flex items-start justify-between gap-4">
      <div>
        <p className={`text-sm font-medium ${danger ? "text-red-600" : "text-text-primary"}`}>{label}</p>
        <p className="text-xs text-text-tertiary mt-0.5">{desc}</p>
      </div>
      <div onClick={() => setBool(field, !(settings[field] as boolean))}
        className={`w-10 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors mt-0.5 ${settings[field] ? (danger ? "bg-red-500" : "bg-ai-500") : "bg-surface-300"}`}>
        <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${settings[field] ? "translate-x-5" : "translate-x-0.5"}`} />
      </div>
    </div>
  );

  if (loading) return <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>;

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Platform Settings <Settings className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Global configuration for the Upkilo SaaS platform.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load}>Refresh</Button>
      </header>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Globe className="h-4 w-4" /> General</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          {[
            { key: "platformName" as const, label: "Platform Name" },
            { key: "supportEmail" as const, label: "Support Email" },
            { key: "defaultTenantTier" as const, label: "Default Tenant Tier" },
          ].map((f) => (
            <div key={f.key}>
              <label className="block text-sm font-medium text-text-primary mb-1">{f.label}</label>
              <input type="text" value={settings[f.key] as string} onChange={(e) => setStr(f.key, e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
          ))}
          <div>
            <label className="block text-sm font-medium text-text-primary mb-1">API Rate Limit (req/min)</label>
            <input type="number" value={settings.apiRateLimit} onChange={(e) => setNum("apiRateLimit", e.target.value)}
              className="w-40 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Shield className="h-4 w-4" /> Security & Access</CardTitle></CardHeader>
        <CardContent className="space-y-5">
          <Toggle label="Enforce 2FA globally" desc="All users on all tenants must have 2FA enabled" field="enforceTwoFactorGlobal" />
          <Toggle label="Allow new registrations" desc="New tenants can sign up without invitation" field="allowNewRegistrations" />
          <Toggle label="Maintenance Mode" desc="Block all tenant logins — platform-wide maintenance" field="maintenanceMode" danger />
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Infrastructure Status</CardTitle><CardDescription>Read-only connection status</CardDescription></CardHeader>
        <CardContent className="space-y-3">
          {[
            { label: "SMTP / Email", field: "smtpConfigured" as const },
            { label: "Stripe Payments", field: "stripeConnected" as const },
          ].map((item) => (
            <div key={item.field} className="flex items-center justify-between p-3 rounded-lg border border-surface-100 bg-surface-50">
              <span className="text-sm text-text-primary">{item.label}</span>
              <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${settings[item.field] ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"}`}>
                {settings[item.field] ? "Connected" : "Not configured"}
              </span>
            </div>
          ))}
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
          onClick={save} disabled={saving}>
          {saving ? "Saving…" : "Save Settings"}
        </Button>
      </div>
    </div>
  );
}
