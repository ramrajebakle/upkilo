"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Building2, Plus, Search, Users, Globe, Settings, ExternalLink,
    RefreshCw, CheckCircle, XCircle, Pause, Play, ChevronRight, Loader2
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface SubTenant {
    id: string;
    name: string;
    slug: string;
    email?: string;
    status: number; // 0=Active, 1=Suspended, 2=Cancelled
    subscriptionTier: number;
    createdAt: string;
}

const statusLabel: Record<number, string> = { 0: 'Active', 1: 'Suspended', 2: 'Cancelled' };
const statusColor: Record<number, string> = {
    0: 'bg-emerald-100 text-emerald-700',
    1: 'bg-amber-100 text-amber-700',
    2: 'bg-red-100 text-red-600',
};
const tierLabel: Record<number, string> = { 0: 'Free', 1: 'Starter', 2: 'Growth', 3: 'Enterprise' };

export default function AgencyDashboardPage() {
    const [subtenants, setSubtenants] = useState<SubTenant[]>([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [showCreate, setShowCreate] = useState(false);
    const [creating, setCreating] = useState(false);
    const [form, setForm] = useState({ name: '', slug: '', email: '' });
    const [actioningId, setActioningId] = useState<string | null>(null);

    const fetchSubtenants = useCallback(async () => {
        try {
            setLoading(true);
            const res = await apiClient.get('/api/v1/agency/subtenants');
            setSubtenants(res.data?.data || []);
        } catch {
            toast.error('Failed to load sub-accounts');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchSubtenants(); }, [fetchSubtenants]);

    const handleCreate = async () => {
        if (!form.name || !form.slug || !form.email) { toast.error('All fields required'); return; }
        setCreating(true);
        try {
            const res = await apiClient.post('/api/v1/agency/subtenants', form);
            setSubtenants(prev => [res.data, ...prev]);
            setShowCreate(false);
            setForm({ name: '', slug: '', email: '' });
            toast.success('Sub-account created');
        } catch (e: any) {
            toast.error(e?.response?.data || 'Failed to create sub-account');
        } finally {
            setCreating(false);
        }
    };

    const handleToggleStatus = async (tenant: SubTenant) => {
        const newStatus = tenant.status === 0 ? 1 : 0; // toggle Active/Suspended
        setActioningId(tenant.id);
        try {
            await apiClient.put(`/api/v1/agency/subtenants/${tenant.id}/status`, { status: newStatus });
            setSubtenants(prev => prev.map(t => t.id === tenant.id ? { ...t, status: newStatus } : t));
            toast.success(`Account ${newStatus === 0 ? 'activated' : 'suspended'}`);
        } catch {
            toast.error('Failed to update status');
        } finally {
            setActioningId(null);
        }
    };

    const handleImpersonate = async (tenant: SubTenant) => {
        setActioningId(tenant.id);
        try {
            const res = await apiClient.post(`/api/v1/agency/subtenants/${tenant.id}/impersonate`);
            toast.success(`Switching to ${tenant.name}...`);
            // Store the target tenant ID for context switch
            localStorage.setItem('impersonateTenantId', res.data.switchToTenantId);
            window.location.href = '/dashboard';
        } catch {
            toast.error('Failed to impersonate account');
        } finally {
            setActioningId(null);
        }
    };

    const filtered = subtenants.filter(t =>
        !search || t.name.toLowerCase().includes(search.toLowerCase()) || t.slug.toLowerCase().includes(search.toLowerCase())
    );

    const stats = {
        total: subtenants.length,
        active: subtenants.filter(t => t.status === 0).length,
        suspended: subtenants.filter(t => t.status === 1).length,
    };

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Agency Dashboard</h1>
                    <p className="text-slate-500 mt-1">Manage white-label sub-accounts for your clients</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchSubtenants} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowCreate(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Sub-Account
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Total Accounts', value: stats.total, icon: <Building2 className="h-5 w-5 text-indigo-500" /> },
                    { label: 'Active', value: stats.active, icon: <CheckCircle className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Suspended', value: stats.suspended, icon: <Pause className="h-5 w-5 text-amber-500" /> },
                ].map(s => (
                    <div key={s.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-slate-50 rounded-lg">{s.icon}</div>
                        <div>
                            <div className="text-2xl font-bold text-slate-900">{s.value}</div>
                            <div className="text-xs text-slate-500">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Create Form */}
            {showCreate && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">Create Sub-Account</h2>
                        <button onClick={() => setShowCreate(false)} className="text-slate-400 hover:text-slate-600">✕</button>
                    </div>
                    <div className="grid grid-cols-3 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Business Name</label>
                            <Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} placeholder="Client Business Name" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">URL Slug</label>
                            <Input value={form.slug} onChange={e => setForm(p => ({ ...p, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-') }))} placeholder="client-business" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Admin Email</label>
                            <Input type="email" value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))} placeholder="admin@client.com" />
                        </div>
                    </div>
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleCreate} disabled={creating}>
                            {creating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
                            Create Account
                        </Button>
                        <Button variant="outline" onClick={() => setShowCreate(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Search */}
            <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search accounts..." className="pl-9" />
            </div>

            {/* Sub-accounts list */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(4)].map((_, i) => (
                        <div key={i} className="bg-white border border-slate-200 rounded-xl p-4 animate-pulse h-20" />
                    ))}
                </div>
            ) : filtered.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
                    <Building2 className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700">No sub-accounts yet</h3>
                    <p className="text-slate-500 text-sm mt-1 mb-4">Create your first white-label account for a client</p>
                    <Button onClick={() => setShowCreate(true)}><Plus className="h-4 w-4 mr-2" /> New Sub-Account</Button>
                </div>
            ) : (
                <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                    {/* Header row */}
                    <div className="grid grid-cols-12 gap-4 px-5 py-3 bg-slate-50 border-b border-slate-100 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        <div className="col-span-4">Account</div>
                        <div className="col-span-2">Slug</div>
                        <div className="col-span-2">Plan</div>
                        <div className="col-span-1">Status</div>
                        <div className="col-span-1">Created</div>
                        <div className="col-span-2 text-right">Actions</div>
                    </div>

                    {filtered.map((tenant, idx) => (
                        <div key={tenant.id} className={`grid grid-cols-12 gap-4 px-5 py-4 items-center hover:bg-slate-50 transition-colors ${idx < filtered.length - 1 ? 'border-b border-slate-50' : ''}`}>
                            <div className="col-span-4 flex items-center gap-3">
                                <div className="h-9 w-9 rounded-lg bg-gradient-to-br from-indigo-400 to-purple-600 flex items-center justify-center text-white font-bold text-sm shrink-0">
                                    {tenant.name.charAt(0).toUpperCase()}
                                </div>
                                <div>
                                    <p className="font-medium text-slate-900">{tenant.name}</p>
                                    {tenant.email && <p className="text-xs text-slate-400">{tenant.email}</p>}
                                </div>
                            </div>
                            <div className="col-span-2">
                                <span className="flex items-center gap-1 text-sm text-slate-600">
                                    <Globe className="h-3.5 w-3.5 text-slate-400" />
                                    {tenant.slug}
                                </span>
                            </div>
                            <div className="col-span-2">
                                <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600">
                                    {tierLabel[tenant.subscriptionTier] || 'Starter'}
                                </span>
                            </div>
                            <div className="col-span-1">
                                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColor[tenant.status] || 'bg-slate-100 text-slate-600'}`}>
                                    {statusLabel[tenant.status] || 'Unknown'}
                                </span>
                            </div>
                            <div className="col-span-1 text-xs text-slate-400">
                                {new Date(tenant.createdAt).toLocaleDateString()}
                            </div>
                            <div className="col-span-2 flex items-center justify-end gap-1">
                                <button
                                    onClick={() => handleImpersonate(tenant)}
                                    disabled={actioningId === tenant.id || tenant.status !== 0}
                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors disabled:opacity-40"
                                    title="Enter account"
                                >
                                    {actioningId === tenant.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <ExternalLink className="h-3.5 w-3.5" />}
                                </button>
                                <button
                                    onClick={() => handleToggleStatus(tenant)}
                                    disabled={actioningId === tenant.id || tenant.status === 2}
                                    className={`p-1.5 rounded-lg transition-colors disabled:opacity-40 ${tenant.status === 0 ? 'text-slate-400 hover:text-amber-600 hover:bg-amber-50' : 'text-slate-400 hover:text-emerald-600 hover:bg-emerald-50'}`}
                                    title={tenant.status === 0 ? 'Suspend' : 'Activate'}
                                >
                                    {tenant.status === 0 ? <Pause className="h-3.5 w-3.5" /> : <Play className="h-3.5 w-3.5" />}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
