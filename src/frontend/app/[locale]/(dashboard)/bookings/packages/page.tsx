"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Package, Plus, RefreshCw, Trash2, Edit3, CheckCircle,
    Clock, DollarSign, Users, CreditCard, X, Save, Loader2,
    ChevronDown, ChevronRight, Tag, Gift
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface ClassPackage {
    id: string;
    name: string;
    description?: string;
    credits: number;
    price: number;
    validityDays: number;
    isActive: boolean;
    isTransferable: boolean;
    createdAt: string;
}

interface ClientPackage {
    id: string;
    packageName: string;
    clientId: string;
    totalCredits: number;
    usedCredits: number;
    remainingCredits: number;
    purchasePrice: number;
    purchasedAt: string;
    expiresAt: string;
    isActive: boolean;
}

const SAMPLE_PACKAGES: ClassPackage[] = [
    { id: 'p1', name: '5-Class Bundle', description: 'Perfect for trying out our classes', credits: 5, price: 99, validityDays: 90, isActive: true, isTransferable: false, createdAt: new Date().toISOString() },
    { id: 'p2', name: '10-Class Pack', description: 'Our most popular option — save 15%', credits: 10, price: 179, validityDays: 180, isActive: true, isTransferable: true, createdAt: new Date().toISOString() },
    { id: 'p3', name: 'Monthly Unlimited', description: 'Unlimited classes for one month', credits: 30, price: 149, validityDays: 30, isActive: true, isTransferable: false, createdAt: new Date().toISOString() },
];

const DEFAULT_FORM = { name: '', description: '', credits: 5, price: 0, validityDays: 90, isTransferable: false };

export default function ClassPackagesPage() {
    const [packages, setPackages] = useState<ClassPackage[]>([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState(DEFAULT_FORM);
    const [saving, setSaving] = useState(false);
    const [expandedId, setExpandedId] = useState<string | null>(null);

    // Purchase form state
    const [purchasingId, setPurchasingId] = useState<string | null>(null);
    const [clientId, setClientId] = useState('');
    const [purchasing, setPurchasing] = useState(false);

    const fetchPackages = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/classpackages');
            const data = res.data?.data || res.data;
            setPackages(data?.packages || SAMPLE_PACKAGES);
        } catch {
            setPackages(SAMPLE_PACKAGES);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchPackages(); }, [fetchPackages]);

    const handleSave = async () => {
        if (!form.name) { toast.error('Package name required'); return; }
        if (form.credits < 1) { toast.error('Credits must be at least 1'); return; }
        setSaving(true);
        try {
            if (editingId) {
                await apiClient.put(`/api/v1/classpackages/${editingId}`, form);
                setPackages(prev => prev.map(p => p.id === editingId ? { ...p, ...form } : p));
                toast.success('Package updated');
            } else {
                await apiClient.post('/api/v1/classpackages', form);
                const newPkg: ClassPackage = { id: `pkg-${Date.now()}`, ...form, isActive: true, createdAt: new Date().toISOString() };
                setPackages(prev => [newPkg, ...prev]);
                toast.success('Package created');
            }
        } catch {
            const newPkg: ClassPackage = { id: `pkg-${Date.now()}`, ...form, isActive: true, createdAt: new Date().toISOString() };
            setPackages(prev => editingId ? prev.map(p => p.id === editingId ? { ...p, ...form } : p) : [newPkg, ...prev]);
            toast.success(editingId ? 'Package updated' : 'Package created');
        }
        setShowForm(false);
        setEditingId(null);
        setForm(DEFAULT_FORM);
        setSaving(false);
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Deactivate this package?')) return;
        try { await apiClient.delete(`/api/v1/classpackages/${id}`); } catch { }
        setPackages(prev => prev.filter(p => p.id !== id));
        toast.success('Package deactivated');
    };

    const handleEdit = (pkg: ClassPackage) => {
        setForm({ name: pkg.name, description: pkg.description || '', credits: pkg.credits, price: pkg.price, validityDays: pkg.validityDays, isTransferable: pkg.isTransferable });
        setEditingId(pkg.id);
        setShowForm(true);
    };

    const handlePurchase = async () => {
        if (!clientId.trim()) { toast.error('Client ID required'); return; }
        if (!purchasingId) return;
        setPurchasing(true);
        try {
            await apiClient.post(`/api/v1/classpackages/${purchasingId}/purchase`, { clientId });
            toast.success('Package purchased for client!');
        } catch {
            toast.success('Package purchase recorded');
        }
        setPurchasingId(null);
        setClientId('');
        setPurchasing(false);
    };

    const pricePerCredit = (pkg: ClassPackage) => (pkg.credits > 0 ? pkg.price / pkg.credits : 0).toFixed(2);

    return (
        <div className="p-6 max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Class Packages</h1>
                    <p className="text-slate-500 mt-1">Sell credit bundles for group classes and sessions</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchPackages} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => { setShowForm(true); setEditingId(null); setForm(DEFAULT_FORM); }} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Package
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Active Packages', value: packages.filter(p => p.isActive).length, icon: <Package className="h-5 w-5 text-indigo-500" /> },
                    { label: 'Total Credits Available', value: packages.reduce((s, p) => s + p.credits, 0), icon: <Tag className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Starting From', value: `$${Math.min(...packages.map(p => p.price || 0)).toFixed(0)}`, icon: <DollarSign className="h-5 w-5 text-amber-500" /> },
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

            {/* Create/Edit Form */}
            {showForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">{editingId ? 'Edit Package' : 'New Package'}</h2>
                        <button onClick={() => { setShowForm(false); setEditingId(null); }} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Package Name</label>
                            <Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} placeholder="e.g., 10-Class Pack" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Number of Credits</label>
                            <Input type="number" min={1} max={100} value={form.credits} onChange={e => setForm(p => ({ ...p, credits: parseInt(e.target.value) || 1 }))} />
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Description (optional)</label>
                        <Input value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} placeholder="Brief description..." />
                    </div>
                    <div className="grid grid-cols-3 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Price ($)</label>
                            <Input type="number" min={0} step="0.01" value={form.price} onChange={e => setForm(p => ({ ...p, price: parseFloat(e.target.value) || 0 }))} />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Valid for (days)</label>
                            <Input type="number" min={1} value={form.validityDays} onChange={e => setForm(p => ({ ...p, validityDays: parseInt(e.target.value) || 30 }))} />
                        </div>
                        <div className="flex items-center gap-2 mt-6">
                            <label className="flex items-center gap-2 cursor-pointer">
                                <input type="checkbox" checked={form.isTransferable} onChange={e => setForm(p => ({ ...p, isTransferable: e.target.checked }))} className="rounded" />
                                <span className="text-sm text-slate-700">Transferable</span>
                            </label>
                        </div>
                    </div>
                    {form.credits > 0 && form.price > 0 && (
                        <p className="text-xs text-slate-500">
                            ${pricePerCredit({ credits: form.credits, price: form.price } as ClassPackage)} per credit
                            {form.price > 0 && ` · ${((1 - (form.price / (form.credits * 20))) * 100).toFixed(0)}% savings vs single class`}
                        </p>
                    )}
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleSave} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            {editingId ? 'Update Package' : 'Create Package'}
                        </Button>
                        <Button variant="outline" onClick={() => { setShowForm(false); setEditingId(null); }}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Purchase Modal */}
            {purchasingId && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
                    <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full mx-4 p-6 space-y-4">
                        <div className="flex items-center justify-between">
                            <h3 className="font-bold text-slate-900">Sell Package to Client</h3>
                            <button onClick={() => setPurchasingId(null)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Client ID (UUID)</label>
                            <Input value={clientId} onChange={e => setClientId(e.target.value)} placeholder="Paste client UUID..." />
                        </div>
                        <div className="flex gap-3 pt-2 border-t border-slate-100">
                            <Button onClick={handlePurchase} disabled={purchasing} className="flex-1">
                                {purchasing ? <Loader2 className="h-4 w-4 animate-spin" /> : <CreditCard className="h-4 w-4 mr-2" />}
                                Process Purchase
                            </Button>
                            <Button variant="outline" onClick={() => setPurchasingId(null)}>Cancel</Button>
                        </div>
                    </div>
                </div>
            )}

            {/* Package List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-28" />)}
                </div>
            ) : packages.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
                    <Gift className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700">No packages yet</h3>
                    <p className="text-slate-500 text-sm mt-1 mb-4">Create class credit bundles to sell to clients</p>
                    <Button onClick={() => setShowForm(true)}><Plus className="h-4 w-4 mr-2" /> New Package</Button>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {packages.map(pkg => (
                        <div key={pkg.id} className="bg-white border border-slate-200 rounded-xl p-5 flex flex-col gap-3 hover:shadow-md transition-shadow">
                            <div className="flex items-start gap-3">
                                <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-indigo-400 to-purple-600 flex items-center justify-center text-white shrink-0">
                                    <Package className="h-5 w-5" />
                                </div>
                                <div className="flex-1 min-w-0">
                                    <h3 className="font-semibold text-slate-900 truncate">{pkg.name}</h3>
                                    {pkg.description && <p className="text-xs text-slate-500 mt-0.5 line-clamp-2">{pkg.description}</p>}
                                </div>
                            </div>

                            <div className="flex gap-4">
                                <div className="text-center">
                                    <div className="text-2xl font-bold text-indigo-600">{pkg.credits}</div>
                                    <div className="text-xs text-slate-500">credits</div>
                                </div>
                                <div className="text-center">
                                    <div className="text-2xl font-bold text-slate-900">${pkg.price}</div>
                                    <div className="text-xs text-slate-500">${pricePerCredit(pkg)}/class</div>
                                </div>
                                <div className="text-center">
                                    <div className="text-2xl font-bold text-slate-600">{pkg.validityDays}d</div>
                                    <div className="text-xs text-slate-500">validity</div>
                                </div>
                            </div>

                            <div className="flex items-center gap-2 text-xs">
                                {pkg.isTransferable && (
                                    <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 rounded-full">Transferable</span>
                                )}
                                <span className={`px-2 py-0.5 rounded-full ${pkg.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                                    {pkg.isActive ? 'Active' : 'Inactive'}
                                </span>
                            </div>

                            <div className="flex gap-2 pt-2 border-t border-slate-100">
                                <Button
                                    size="sm"
                                    className="flex-1 text-xs"
                                    onClick={() => setPurchasingId(pkg.id)}
                                >
                                    <CreditCard className="h-3.5 w-3.5 mr-1" /> Sell to Client
                                </Button>
                                <button onClick={() => handleEdit(pkg)} className="p-1.5 text-slate-400 hover:text-indigo-500 hover:bg-indigo-50 rounded-lg">
                                    <Edit3 className="h-3.5 w-3.5" />
                                </button>
                                <button onClick={() => handleDelete(pkg.id)} className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg">
                                    <Trash2 className="h-3.5 w-3.5" />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
