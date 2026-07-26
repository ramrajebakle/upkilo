"use client";

import React, { useEffect, useState } from "react";
import { 
  Settings, 
  Save, 
  RotateCcw,
  Globe,
  Mail,
  Lock,
  Zap,
  Layout,
  Database,
  RefreshCw
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";

export default function AdminSettingsPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();
  
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState<any>(null);

  useEffect(() => {
    if (isInitialized && user?.role !== 'superadmin') {
      router.push('/dashboard');
    }
  }, [user, isInitialized, router]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.superAdmin.getSettings();
      setSettings(res.data);
    } catch (error) {
      console.error("Failed to fetch settings:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user?.role === 'superadmin') {
      fetchData();
    }
  }, [user]);

  const handleSave = async () => {
    setSaving(true);
    try {
      await api.superAdmin.updateSettings(settings);
    } catch (error) {
      console.error("Failed to save settings:", error);
    } finally {
      setSaving(false);
    }
  };

  if (user?.role !== 'superadmin') return null;

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-2xl shadow-lg shadow-indigo-500/20">
              <Settings className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Global Platform Settings
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">Configure master defaults and system-wide integrations for Upkilo.</p>
        </div>
        <div className="flex items-center gap-3">
          <Button onClick={fetchData} variant="outline" size="sm">
            <RotateCcw className="h-4 w-4 mr-2" />
            Reset
          </Button>
          <Button onClick={handleSave} variant="primary" size="sm" disabled={saving}>
            {saving ? <RefreshCw className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
            Save Changes
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Navigation Sidebar for settings sections */}
        <div className="lg:col-span-1 space-y-2">
            <SettingsSectionButton active icon={Globe} label="General & Branding" />
            <SettingsSectionButton icon={Mail} label="Email & SMTP" />
            <SettingsSectionButton icon={Lock} label="Security & 2FA" />
            <SettingsSectionButton icon={Zap} label="Integrations & API" />
            <SettingsSectionButton icon={Layout} label="Default Tiers" />
            <SettingsSectionButton icon={Database} label="System Cleanups" />
        </div>

        {/* Content Area */}
        <div className="lg:col-span-2 space-y-8">
          <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-8 shadow-sm">
            <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-6" style={{ fontFamily: 'Outfit, sans-serif' }}>Platform Appearance</h2>
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-2">
                  <label className="text-sm font-bold text-slate-700 dark:text-slate-300">Platform Name</label>
                  <Input 
                    value={settings?.platformName || ""} 
                    onChange={e => setSettings({...settings, platformName: e.target.value})}
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-bold text-slate-700 dark:text-slate-300">Support Email</label>
                  <Input 
                    value={settings?.supportEmail || ""} 
                    onChange={e => setSettings({...settings, supportEmail: e.target.value})}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-8 shadow-sm">
            <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'Outfit, sans-serif' }}>System Status Controls</h2>
                <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">Global</Badge>
            </div>
            <div className="space-y-6">
               <SettingsToggle 
                 label="Maintenance Mode" 
                 description="Take the entire platform offline for maintenance. Super Admins can still log in."
                 enabled={settings?.maintenanceMode}
                 onToggle={() => setSettings({...settings, maintenanceMode: !settings.maintenanceMode})}
               />
               <SettingsToggle 
                 label="Enforce 2FA System-wide" 
                 description="Require all users (Admins & Staff) to set up 2FA before accessing their dashboards."
                 enabled={settings?.enforceTwoFactorGlobal}
                 onToggle={() => setSettings({...settings, enforceTwoFactorGlobal: !settings.enforceTwoFactorGlobal})}
               />
               <SettingsToggle 
                 label="Open Registrations" 
                 description="Allow new tenants to sign up via the public landing page."
                 enabled={settings?.allowNewRegistrations}
                 onToggle={() => setSettings({...settings, allowNewRegistrations: !settings.allowNewRegistrations})}
               />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function SettingsSectionButton({ icon: Icon, label, active = false }: { icon: any; label: string; active?: boolean }) {
    return (
        <button className={`w-full flex items-center gap-3 px-4 py-3 rounded-2xl transition-all ${active ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-600/20' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-white/5'}`}>
            <Icon className="h-5 w-5" />
            <span className="font-semibold text-sm">{label}</span>
        </button>
    );
}

function SettingsToggle({ label, description, enabled, onToggle }: { label: string; description: string; enabled: boolean; onToggle: () => void }) {
    return (
        <div className="flex items-start justify-between gap-4 p-4 rounded-2xl hover:bg-slate-50 dark:hover:bg-white/5 transition-colors">
            <div className="flex-1">
                <div className="font-bold text-slate-900 dark:text-white">{label}</div>
                <div className="text-xs text-slate-500">{description}</div>
            </div>
            <button 
                onClick={onToggle}
                className={`flex-shrink-0 w-12 h-6 rounded-full p-1 transition-colors ${enabled ? 'bg-indigo-600' : 'bg-slate-300 dark:bg-slate-700'}`}
            >
                <div className={`h-4 w-4 rounded-full bg-white transition-transform ${enabled ? 'translate-x-6' : 'translate-x-0'}`} />
            </button>
        </div>
    );
}
