"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Flag, Plus, RefreshCw, ToggleLeft, ToggleRight, Loader2,
    Zap, Shield, X, Save, Percent, ChevronDown, ChevronUp
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface FeatureFlag {
    name: string;
    description: string;
    isEnabled: boolean;
    rolloutPercentage: number;
    enabledForTenant: boolean;
}

const FLAG_DESCRIPTIONS: Record<string, string> = {
    ai_chatbot: 'AI-powered chatbot for client interactions',
    ad_platform_linkedin: 'LinkedIn Ads integration for campaigns',
    landing_page_builder: 'Visual landing page editor',
    referral_program: 'Client referral tracking & rewards',
    partner_program: 'Partner/Agency white-label program',
    gdpr_export: 'GDPR data export & right-to-be-forgotten',
    outbox_processing: 'Reliable event delivery via outbox pattern',
    circuit_breaker: 'Circuit breaker for external API resilience',
};

export default function FeatureFlagsPage() {
    const [flags, setFlags] = useState<FeatureFlag[]>([]);
    const [loading, setLoading] = useState(true);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [togglingFlag, setTogglingFlag] = useState<string | null>(null);
    const [editingRollout, setEditingRollout] = useState<string | null>(null);
    const [rolloutValue, setRolloutValue] = useState<number>(100);
    const [form, setForm] = useState({ name: '', description: '', defaultEnabled: false, rolloutPercentage: 100 });
    const [saving, setSaving] = useState(false);

    const fetchFlags = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/featureflags');
            const data = res.data?.data || res.data;
            setFlags(data?.flags || data || []);
        } catch {
            toast.error('Failed to load feature flags');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchFlags(); }, [fetchFlags]);

    const handleToggle = async (flag: FeatureFlag) => {
        setTogglingFlag(flag.name);
        try {
            await apiClient.post(`/api/v1/featureflags/${flag.name}/override`, {
                enabled: !flag.enabledForTenant
            });
            setFlags(prev => prev.map(f =>
                f.name === flag.name ? { ...f, enabledForTenant: !f.enabledForTenant } : f
            ));
            toast.success(`${flag.name} ${!flag.enabledForTenant ? 'enabled' : 'disabled'} for your tenant`);
        } catch {
            toast.error('Failed to update flag');
        } finally {
            setTogglingFlag(null);
        }
    };

    const handleSetRollout = async (flagName: string) => {
        try {
            await apiClient.put(`/api/v1/featureflags/${flagName}/rollout`, { percentage: rolloutValue });
            setFlags(prev => prev.map(f =>
                f.name === flagName ? { ...f, rolloutPercentage: rolloutValue } : f
            ));
            setEditingRollout(null);
            toast.success(`Rollout for ${flagName} set to ${rolloutValue}%`);
        } catch {
            toast.error('Failed to update rollout');
        }
    };

    const handleCreate = async () => {
        if (!form.name) { toast.error('Flag name is required'); return; }
        setSaving(true);
        try {
            await apiClient.post('/api/v1/featureflags', {
                name: form.name,
                description: form.description,
                defaultEnabled: form.defaultEnabled,
                rolloutPercentage: form.rolloutPercentage,
            });
            toast.success('Feature flag created');
            setShowCreateForm(false);
            setForm({ name: '', description: '', defaultEnabled: false, rolloutPercentage: 100 });
            fetchFlags();
        } catch {
            toast.error('Failed to create flag');
        } finally {
            setSaving(false);
        }
    };

    const activeCount = flags.filter(f => f.enabledForTenant).length;

    return (
        <div className="p-6 max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Feature Flags</h1>
                    <p className="text-slate-500 mt-1">Control feature rollouts, kill switches, and gradual deployments</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchFlags} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowCreateForm(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Flag
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Total Flags', value: flags.length, icon: <Flag className="h-5 w-5 text-indigo-500" /> },
                    { label: 'Enabled for Tenant', value: activeCount, icon: <Zap className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Disabled', value: flags.length - activeCount, icon: <Shield className="h-5 w-5 text-slate-400" /> },
                ].map(s => (
                    <div key={s.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-slate-50 rounded-lg">{s.icon}</div>
                        <div>
                            <div className="text-xl font-bold text-slate-900">{s.value}</div>
                            <div className="text-xs text-slate-500">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Create Form */}
            {showCreateForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">New Feature Flag</h2>
                        <button onClick={() => setShowCreateForm(false)} className="text-slate-400 hover:text-slate-600">
                            <X className="h-4 w-4" />
                        </button>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Flag Name (snake_case)</label>
                            <Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} placeholder="my_new_feature" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Rollout %</label>
                            <input
                                type="number"
                                min={0}
                                max={100}
                                value={form.rolloutPercentage}
                                onChange={e => setForm(p => ({ ...p, rolloutPercentage: parseInt(e.target.value) || 0 }))}
                                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                            />
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                        <Input value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} placeholder="What does this flag control?" />
                    </div>
                    <label className="flex items-center gap-2 cursor-pointer">
                        <input
                            type="checkbox"
                            checked={form.defaultEnabled}
                            onChange={e => setForm(p => ({ ...p, defaultEnabled: e.target.checked }))}
                            className="rounded"
                        />
                        <span className="text-sm text-slate-700">Enabled by default</span>
                    </label>
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleCreate} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            Create Flag
                        </Button>
                        <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Flags List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(5)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-4 animate-pulse h-20" />)}
                </div>
            ) : (
                <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                    <div className="px-5 py-3 bg-slate-50 border-b border-slate-100 grid grid-cols-12 gap-4 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        <div className="col-span-4">Flag</div>
                        <div className="col-span-4">Description</div>
                        <div className="col-span-2 text-center">Rollout</div>
                        <div className="col-span-2 text-center">Tenant Override</div>
                    </div>
                    {flags.length === 0 ? (
                        <div className="text-center py-12 text-slate-400">
                            <Flag className="h-8 w-8 mx-auto mb-2 opacity-30" />
                            <p className="text-sm">No feature flags configured</p>
                        </div>
                    ) : flags.map((flag, idx) => (
                        <div key={flag.name} className={`grid grid-cols-12 gap-4 px-5 py-3 items-center ${idx < flags.length - 1 ? 'border-b border-slate-50' : ''} hover:bg-slate-50`}>
                            <div className="col-span-4">
                                <div className="flex items-center gap-2">
                                    <div className={`w-2 h-2 rounded-full ${flag.enabledForTenant ? 'bg-emerald-500' : 'bg-slate-300'}`} />
                                    <span className="font-mono text-sm font-medium text-slate-900">{flag.name}</span>
                                </div>
                            </div>
                            <div className="col-span-4 text-sm text-slate-500 truncate">
                                {FLAG_DESCRIPTIONS[flag.name] || flag.description || '—'}
                            </div>
                            <div className="col-span-2 text-center">
                                {editingRollout === flag.name ? (
                                    <div className="flex items-center gap-1">
                                        <input
                                            type="number"
                                            min={0}
                                            max={100}
                                            value={rolloutValue}
                                            onChange={e => setRolloutValue(parseInt(e.target.value) || 0)}
                                            className="w-16 border border-slate-200 rounded px-1.5 py-0.5 text-xs text-center"
                                        />
                                        <button onClick={() => handleSetRollout(flag.name)} className="text-emerald-600 hover:text-emerald-700">
                                            <Save className="h-3 w-3" />
                                        </button>
                                        <button onClick={() => setEditingRollout(null)} className="text-slate-400">
                                            <X className="h-3 w-3" />
                                        </button>
                                    </div>
                                ) : (
                                    <button
                                        onClick={() => { setEditingRollout(flag.name); setRolloutValue(flag.rolloutPercentage); }}
                                        className="flex items-center gap-1 mx-auto text-sm text-slate-600 hover:text-indigo-600"
                                    >
                                        <Percent className="h-3 w-3" />
                                        {flag.rolloutPercentage}%
                                    </button>
                                )}
                            </div>
                            <div className="col-span-2 flex justify-center">
                                <button
                                    onClick={() => handleToggle(flag)}
                                    disabled={togglingFlag === flag.name}
                                    className="p-1"
                                    title={flag.enabledForTenant ? 'Disable for this tenant' : 'Enable for this tenant'}
                                >
                                    {togglingFlag === flag.name
                                        ? <Loader2 className="h-5 w-5 animate-spin text-slate-400" />
                                        : flag.enabledForTenant
                                            ? <ToggleRight className="h-6 w-6 text-emerald-500" />
                                            : <ToggleLeft className="h-6 w-6 text-slate-300" />}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800">
                <strong>Note:</strong> Tenant overrides take precedence over global flag settings. Changes take effect immediately without deployment.
            </div>
        </div>
    );
}
