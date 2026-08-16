"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Shield, CheckCircle, AlertCircle, XCircle, Loader2, Key, Globe, Lock, Users, Settings } from 'lucide-react';
import api from '@/lib/api';

interface SsoConfigData {
  sso: {
    id: string;
    provider: string;
    protocol: string;
    entityId?: string;
    metadataUrl?: string;
    signInUrl?: string;
    hasCertificate: boolean;
    clientId?: string;
    hasClientSecret: boolean;
    attributeMapping?: string;
    isEnabled: boolean;
    enforceForAllUsers: boolean;
    createdAt: string;
    updatedAt: string;
  } | null;
  saml: {
    id: string;
    isEnabled: boolean;
    entityId?: string;
    idpMetadataUrl?: string;
    hasCertificate: boolean;
    signOnUrl?: string;
    logoutUrl?: string;
    attributeMapping?: string;
    allowPasswordLogin: boolean;
    autoCreateUsers: boolean;
    defaultRoleId?: string;
  } | null;
}

interface Provider {
  id: string;
  name: string;
  protocols: string[];
  icon: string;
}

export default function SsoSettingsPage() {
  const [config, setConfig] = useState<SsoConfigData | null>(null);
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<{ status: string; issues: string[] } | null>(null);

  const [form, setForm] = useState({
    provider: '',
    protocol: 'SAML',
    entityId: '',
    metadataUrl: '',
    signInUrl: '',
    certificate: '',
    clientId: '',
    clientSecret: '',
    logoutUrl: '',
    allowPasswordLogin: true,
    autoCreateUsers: false,
    enforceForAllUsers: false,
    isEnabled: false,
  });

  const loadConfig = useCallback(async () => {
    try {
      setLoading(true);
      const [configRes, providersRes] = await Promise.all([
        api.sso.getConfig(),
        api.sso.getProviders()
      ]);

      const data = configRes.data as SsoConfigData;
      setConfig(data);
      setProviders(providersRes.data?.data || []);

      if (data?.sso) {
        setForm({
          provider: data.sso.provider || '',
          protocol: data.sso.protocol || 'SAML',
          entityId: data.sso.entityId || '',
          metadataUrl: data.sso.metadataUrl || '',
          signInUrl: data.sso.signInUrl || '',
          certificate: '',
          clientId: data.sso.clientId || '',
          clientSecret: '',
          logoutUrl: data.saml?.logoutUrl || '',
          allowPasswordLogin: data.saml?.allowPasswordLogin ?? true,
          autoCreateUsers: data.saml?.autoCreateUsers ?? false,
          enforceForAllUsers: data.sso.enforceForAllUsers || false,
          isEnabled: data.sso.isEnabled || false,
        });
      }
    } catch (err) {
      console.error('Failed to load SSO config:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadConfig(); }, [loadConfig]);

  const handleSave = async () => {
    if (!form.provider) return alert('Please select an SSO provider.');
    setSaving(true);
    try {
      await api.sso.updateConfig(form);
      await loadConfig();
    } catch (err) {
      console.error('Failed to save SSO config:', err);
      alert('Failed to save SSO configuration.');
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async () => {
    setTesting(true);
    setTestResult(null);
    try {
      const res = await api.sso.testConnection();
      setTestResult(res.data);
    } catch (err: any) {
      setTestResult({ status: 'error', issues: [err.message || 'Connection test failed'] });
    } finally {
      setTesting(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Remove SSO configuration? Users will need to use password login.')) return;
    try {
      await api.sso.deleteConfig();
      setConfig(null);
      setForm({
        provider: '', protocol: 'SAML', entityId: '', metadataUrl: '', signInUrl: '',
        certificate: '', clientId: '', clientSecret: '', logoutUrl: '',
        allowPasswordLogin: true, autoCreateUsers: false, enforceForAllUsers: false, isEnabled: false,
      });
    } catch (err) {
      console.error('Failed to delete SSO config:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="w-8 h-8 border-4 border-primary-500 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900">Single Sign-On (SSO)</h1>
          <p className="text-slate-500 mt-1">Configure enterprise SAML or OIDC authentication for your team</p>
        </div>
        <div className="flex items-center gap-2">
          {config?.sso && (
            <>
              <button onClick={handleTest} disabled={testing}
                className="inline-flex items-center gap-2 px-4 py-2 border border-slate-300 rounded-lg text-slate-700 hover:bg-slate-50 disabled:opacity-50">
                {testing ? <Loader2 className="w-4 h-4 animate-spin" /> : <Shield className="w-4 h-4" />}
                Test Connection
              </button>
              <button onClick={handleDelete} className="px-4 py-2 border border-red-200 text-red-600 rounded-lg hover:bg-red-50">
                Remove
              </button>
            </>
          )}
        </div>
      </div>

      {/* Test Result */}
      {testResult && (
        <div className={`rounded-xl border p-4 ${testResult.status === 'healthy' ? 'bg-emerald-50 border-emerald-200' : 'bg-red-50 border-red-200'}`}>
          <div className="flex items-center gap-2">
            {testResult.status === 'healthy' ? (
              <CheckCircle className="w-5 h-5 text-emerald-600" />
            ) : (
              <AlertCircle className="w-5 h-5 text-red-600" />
            )}
            <span className={`font-medium ${testResult.status === 'healthy' ? 'text-emerald-700' : 'text-red-700'}`}>
              {testResult.status === 'healthy' ? 'Connection is healthy' : 'Issues detected'}
            </span>
          </div>
          {testResult.issues.length > 0 && (
            <ul className="mt-2 space-y-1">
              {testResult.issues.map((issue, i) => (
                <li key={i} className="text-sm text-red-600 flex items-center gap-1">
                  <XCircle className="w-3 h-3" /> {issue}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {/* Provider Selection */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <h2 className="text-lg font-semibold text-slate-900 mb-4 flex items-center gap-2">
          <Globe className="w-5 h-5 text-primary-500" /> Identity Provider
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          {providers.map((p) => (
            <button key={p.id} onClick={() => setForm({ ...form, provider: p.id })}
              className={`p-4 border-2 rounded-xl text-left transition-all ${form.provider === p.id ? 'border-primary-500 bg-primary-50' : 'border-slate-200 hover:border-slate-300'}`}>
              <p className="font-semibold text-slate-900">{p.name}</p>
              <p className="text-xs text-slate-500 mt-1">{p.protocols.join(' / ')}</p>
            </button>
          ))}
        </div>
      </div>

      {/* Configuration */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <h2 className="text-lg font-semibold text-slate-900 mb-4 flex items-center gap-2">
          <Settings className="w-5 h-5 text-primary-500" /> Configuration
        </h2>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Protocol</label>
              <select value={form.protocol} onChange={e => setForm({ ...form, protocol: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none">
                <option value="SAML">SAML 2.0</option>
                <option value="OIDC">OpenID Connect</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Entity ID / Issuer</label>
              <input type="text" value={form.entityId} onChange={e => setForm({ ...form, entityId: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" placeholder="https://idp.example.com" />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">IdP Metadata URL</label>
            <input type="url" value={form.metadataUrl} onChange={e => setForm({ ...form, metadataUrl: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" placeholder="https://idp.example.com/metadata" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Sign-In URL</label>
              <input type="url" value={form.signInUrl} onChange={e => setForm({ ...form, signInUrl: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" placeholder="https://idp.example.com/sso" />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Logout URL</label>
              <input type="url" value={form.logoutUrl} onChange={e => setForm({ ...form, logoutUrl: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" placeholder="https://idp.example.com/slo" />
            </div>
          </div>

          {form.protocol === 'SAML' && (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">X.509 Certificate {config?.sso?.hasCertificate && <span className="text-emerald-500">(configured)</span>}</label>
              <textarea value={form.certificate} onChange={e => setForm({ ...form, certificate: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none font-mono text-xs" rows={4} placeholder="-----BEGIN CERTIFICATE-----&#10;...&#10;-----END CERTIFICATE-----" />
            </div>
          )}

          {form.protocol === 'OIDC' && (
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Client ID</label>
                <input type="text" value={form.clientId} onChange={e => setForm({ ...form, clientId: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Client Secret {config?.sso?.hasClientSecret && <span className="text-emerald-500">(configured)</span>}</label>
                <input type="password" value={form.clientSecret} onChange={e => setForm({ ...form, clientSecret: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Options */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <h2 className="text-lg font-semibold text-slate-900 mb-4 flex items-center gap-2">
          <Lock className="w-5 h-5 text-primary-500" /> Options
        </h2>
        <div className="space-y-4">
          {[
            { key: 'isEnabled', label: 'Enable SSO', desc: 'Allow users to sign in via SSO' },
            { key: 'enforceForAllUsers', label: 'Enforce for all users', desc: 'Require SSO — disables password login for non-admin users' },
            { key: 'allowPasswordLogin', label: 'Allow password login', desc: 'Users can still sign in with email/password' },
            { key: 'autoCreateUsers', label: 'Auto-create users', desc: 'Automatically create accounts for new SSO users' },
          ].map(opt => (
            <label key={opt.key} className="flex items-center justify-between p-3 rounded-lg hover:bg-slate-50 cursor-pointer">
              <div>
                <p className="font-medium text-slate-900">{opt.label}</p>
                <p className="text-sm text-slate-500">{opt.desc}</p>
              </div>
              <input type="checkbox" checked={(form as any)[opt.key]} onChange={e => setForm({ ...form, [opt.key]: e.target.checked })}
                className="w-5 h-5 text-primary-500 rounded border-slate-300 focus:ring-primary-500" />
            </label>
          ))}
        </div>
      </div>

      {/* Save Button */}
      <div className="flex justify-end">
        <button onClick={handleSave} disabled={saving}
          className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-primary-500/25 hover:shadow-primary-500/40 transition-all disabled:opacity-50">
          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Key className="w-4 h-4" />}
          Save Configuration
        </button>
      </div>
    </div>
  );
}
