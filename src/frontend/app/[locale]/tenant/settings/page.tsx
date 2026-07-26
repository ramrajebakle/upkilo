"use client";

import React, { useState, useEffect } from "react";
import { Settings, Globe, Bell, Shield, Save, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface TenantSettings {
  businessName: string;
  contactEmail: string;
  contactPhone?: string;
  timezone: string;
  currency: string;
  bookingLeadTimeHours: number;
  allowOnlineBooking: boolean;
  requireDeposit: boolean;
  sendReminders: boolean;
  reminderHoursBefore: number;
}

const DEFAULT: TenantSettings = {
  businessName: "",
  contactEmail: "",
  contactPhone: "",
  timezone: "UTC",
  currency: "USD",
  bookingLeadTimeHours: 2,
  allowOnlineBooking: true,
  requireDeposit: false,
  sendReminders: true,
  reminderHoursBefore: 24,
};

export default function TenantSettingsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [settings, setSettings] = useState<TenantSettings>(DEFAULT);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    apiClient.get("/api/v1/settings/business").then((r) => {
      setSettings({ ...DEFAULT, ...(r.data?.data ?? r.data ?? {}) });
    }).catch(() => {}).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const setStr = (k: keyof TenantSettings, v: string) => setSettings((s) => ({ ...s, [k]: v }));
  const setNum = (k: keyof TenantSettings, v: string) => setSettings((s) => ({ ...s, [k]: parseInt(v) || 0 }));
  const setBool = (k: keyof TenantSettings) => setSettings((s) => ({ ...s, [k]: !s[k] }));

  const save = async () => {
    setSaving(true);
    try {
      await apiClient.put("/api/v1/settings/business", settings);
      toastSuccess("Settings saved");
    } catch { toastError("Failed to save settings"); }
    finally { setSaving(false); }
  };

  const Toggle = ({ label, desc, field }: { label: string; desc: string; field: keyof TenantSettings }) => (
    <div className="flex items-start justify-between gap-4">
      <div>
        <p className="text-sm font-medium text-text-primary">{label}</p>
        <p className="text-xs text-text-tertiary mt-0.5">{desc}</p>
      </div>
      <div onClick={() => setBool(field)}
        className={`w-10 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors mt-0.5 ${settings[field] ? "bg-ai-500" : "bg-surface-300"}`}>
        <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${settings[field] ? "translate-x-5" : "translate-x-0.5"}`} />
      </div>
    </div>
  );

  if (loading) return <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>;

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Business Settings <Settings className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Configure your business profile and booking behaviour.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load}>Refresh</Button>
      </header>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Globe className="h-4 w-4" /> Business Info</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          {[
            { key: "businessName" as const, label: "Business Name" },
            { key: "contactEmail" as const, label: "Contact Email" },
            { key: "contactPhone" as const, label: "Phone" },
          ].map((f) => (
            <div key={f.key}>
              <label className="block text-sm font-medium text-text-primary mb-1">{f.label}</label>
              <input type="text" value={settings[f.key] as string ?? ""} onChange={(e) => setStr(f.key, e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
          ))}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Timezone</label>
              <select value={settings.timezone} onChange={(e) => setStr("timezone", e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {["UTC", "America/New_York", "America/Los_Angeles", "Europe/London", "Asia/Kolkata", "Australia/Sydney"].map((tz) => (
                  <option key={tz} value={tz}>{tz}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Currency</label>
              <select value={settings.currency} onChange={(e) => setStr("currency", e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {["USD", "EUR", "GBP", "INR", "AUD", "CAD"].map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Bell className="h-4 w-4" /> Bookings & Reminders</CardTitle></CardHeader>
        <CardContent className="space-y-5">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Min. lead time (hours)</label>
              <input type="number" min={0} value={settings.bookingLeadTimeHours} onChange={(e) => setNum("bookingLeadTimeHours", e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Reminder (hours before)</label>
              <input type="number" min={1} value={settings.reminderHoursBefore} onChange={(e) => setNum("reminderHoursBefore", e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
          </div>
          <Toggle label="Allow online booking" desc="Clients can book via your public booking page" field="allowOnlineBooking" />
          <Toggle label="Require deposit" desc="Clients must pay a deposit to confirm bookings" field="requireDeposit" />
          <Toggle label="Send reminders" desc="Automatically send appointment reminders" field="sendReminders" />
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
          onClick={save} disabled={saving}>{saving ? "Saving…" : "Save Settings"}</Button>
      </div>
    </div>
  );
}
