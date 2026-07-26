"use client";

import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
    FileText, Plus, Edit, Trash2, RefreshCw, Eye, CheckCircle,
    Clock, Users, Shield, X, Save, Loader2, ToggleLeft, ToggleRight,
    AlertCircle
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';
import DOMPurify from 'isomorphic-dompurify';

interface DigitalWaiver {
    id: string;
    title: string;
    content: string;
    isRequired: boolean;
    isActive: boolean;
    expiryDays?: number;
    version: number;
    createdAt: string;
    signatureCount?: number;
}

const DEFAULT_CONTENT = `<h2>Consent & Liability Waiver</h2>
<p>I, the undersigned, acknowledge and agree to the following:</p>
<ul>
  <li>I am voluntarily participating in the service(s) provided.</li>
  <li>I have disclosed all relevant medical conditions.</li>
  <li>I release the business from liability for any injuries.</li>
</ul>
<p>By signing below, I confirm I have read and understood this waiver.</p>`;

export default function WaiversPage() {
    const [waivers, setWaivers] = useState<DigitalWaiver[]>([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [editingWaiver, setEditingWaiver] = useState<DigitalWaiver | null>(null);
    const [previewWaiver, setPreviewWaiver] = useState<DigitalWaiver | null>(null);
    const [form, setForm] = useState({ title: '', content: DEFAULT_CONTENT, isRequired: true, expiryDays: '' });
    const [saving, setSaving] = useState(false);
    const [togglingId, setTogglingId] = useState<string | null>(null);

    const fetchWaivers = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/waivers');
            const data = res.data?.data || res.data;
            setWaivers(Array.isArray(data) ? data : data?.waivers || []);
        } catch {
            toast.error('Failed to load waivers');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchWaivers(); }, [fetchWaivers]);

    const openCreateForm = () => {
        setEditingWaiver(null);
        setForm({ title: '', content: DEFAULT_CONTENT, isRequired: true, expiryDays: '' });
        setShowForm(true);
    };

    const openEditForm = (waiver: DigitalWaiver) => {
        setEditingWaiver(waiver);
        setForm({
            title: waiver.title,
            content: waiver.content,
            isRequired: waiver.isRequired,
            expiryDays: waiver.expiryDays?.toString() || '',
        });
        setShowForm(true);
    };

    const handleSave = async () => {
        if (!form.title || !form.content) { toast.error('Title and content are required'); return; }
        setSaving(true);
        try {
            const payload = {
                title: form.title,
                content: form.content,
                isRequired: form.isRequired,
                expiryDays: form.expiryDays ? parseInt(form.expiryDays) : null,
            };

            if (editingWaiver) {
                await apiClient.put(`/api/v1/waivers/${editingWaiver.id}`, payload);
                toast.success('Waiver updated');
            } else {
                await apiClient.post('/api/v1/waivers', payload);
                toast.success('Waiver created');
            }
            setShowForm(false);
            fetchWaivers();
        } catch {
            toast.error('Failed to save waiver');
        } finally {
            setSaving(false);
        }
    };

    const handleToggle = async (waiver: DigitalWaiver) => {
        setTogglingId(waiver.id);
        try {
            await apiClient.put(`/api/v1/waivers/${waiver.id}`, { ...waiver, isActive: !waiver.isActive });
            setWaivers(prev => prev.map(w => w.id === waiver.id ? { ...w, isActive: !w.isActive } : w));
            toast.success(waiver.isActive ? 'Waiver deactivated' : 'Waiver activated');
        } catch {
            toast.error('Failed to update waiver');
        } finally {
            setTogglingId(null);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this waiver? Client signatures will be preserved.')) return;
        try {
            await apiClient.delete(`/api/v1/waivers/${id}`);
            setWaivers(prev => prev.filter(w => w.id !== id));
            toast.success('Waiver deleted');
        } catch {
            toast.error('Failed to delete waiver');
        }
    };

    const totalSigs = waivers.reduce((acc, w) => acc + (w.signatureCount || 0), 0);

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Digital Waivers</h1>
                    <p className="text-slate-500 mt-1">Create and manage consent forms clients sign before appointments</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchWaivers} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={openCreateForm} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Waiver
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Total Waivers', value: waivers.length, icon: <FileText className="h-5 w-5 text-indigo-500" /> },
                    { label: 'Active', value: waivers.filter(w => w.isActive).length, icon: <CheckCircle className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Total Signatures', value: totalSigs, icon: <Users className="h-5 w-5 text-blue-500" /> },
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

            {/* Preview Modal */}
            {previewWaiver && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm" onClick={() => setPreviewWaiver(null)}>
                    <div className="bg-white rounded-2xl shadow-2xl max-w-2xl w-full mx-4 max-h-[80vh] flex flex-col" onClick={e => e.stopPropagation()}>
                        <div className="flex items-center justify-between p-5 border-b border-slate-100">
                            <h3 className="font-bold text-slate-900">{previewWaiver.title}</h3>
                            <button onClick={() => setPreviewWaiver(null)} className="text-slate-400 hover:text-slate-600">
                                <X className="h-5 w-5" />
                            </button>
                        </div>
                        <div
                            className="flex-1 overflow-y-auto p-5 prose prose-sm max-w-none"
                            dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(previewWaiver.content) }}
                        />
                        <div className="p-4 border-t border-slate-100 text-xs text-slate-400">
                            Version {previewWaiver.version} · {previewWaiver.isRequired ? 'Required' : 'Optional'}
                            {previewWaiver.expiryDays ? ` · Expires after ${previewWaiver.expiryDays} days` : ''}
                        </div>
                    </div>
                </div>
            )}

            {/* Create/Edit Form */}
            {showForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-5">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">{editingWaiver ? 'Edit Waiver' : 'New Digital Waiver'}</h2>
                        <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Waiver Title</label>
                            <Input value={form.title} onChange={e => setForm(p => ({ ...p, title: e.target.value }))} placeholder="e.g., Massage Therapy Consent" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Expiry (days, optional)</label>
                            <Input type="number" value={form.expiryDays} onChange={e => setForm(p => ({ ...p, expiryDays: e.target.value }))} placeholder="e.g., 365 (leave blank = never)" />
                        </div>
                    </div>
                    <label className="flex items-center gap-2 cursor-pointer">
                        <input type="checkbox" checked={form.isRequired} onChange={e => setForm(p => ({ ...p, isRequired: e.target.checked }))} className="rounded" />
                        <span className="text-sm text-slate-700">Required before appointment</span>
                    </label>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Waiver Content (HTML)</label>
                        <textarea
                            value={form.content}
                            onChange={e => setForm(p => ({ ...p, content: e.target.value }))}
                            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm h-48 resize-none font-mono"
                            placeholder="<h2>Consent Form</h2>..."
                        />
                        <p className="text-xs text-slate-400 mt-1">HTML is rendered when clients view and sign the waiver</p>
                    </div>
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleSave} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            {saving ? 'Saving...' : editingWaiver ? 'Update Waiver' : 'Create Waiver'}
                        </Button>
                        <Button variant="outline" onClick={() => setShowForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Waivers List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-24" />)}
                </div>
            ) : waivers.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
                    <FileText className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700">No waivers yet</h3>
                    <p className="text-slate-500 text-sm mt-1 mb-4">Create your first digital waiver for clients to sign</p>
                    <Button onClick={openCreateForm}><Plus className="h-4 w-4 mr-2" /> New Waiver</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {waivers.map(waiver => (
                        <div key={waiver.id} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-4 hover:shadow-sm transition-shadow">
                            <div className={`p-2.5 rounded-xl ${waiver.isActive ? 'bg-indigo-50' : 'bg-slate-100'}`}>
                                <Shield className={`h-5 w-5 ${waiver.isActive ? 'text-indigo-600' : 'text-slate-400'}`} />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2">
                                    <span className="font-semibold text-slate-900">{waiver.title}</span>
                                    <span className="text-xs text-slate-400">v{waiver.version}</span>
                                    {waiver.isRequired && (
                                        <span className="px-1.5 py-0.5 bg-red-50 text-red-600 text-xs rounded-full font-medium">Required</span>
                                    )}
                                    {!waiver.isActive && (
                                        <span className="px-1.5 py-0.5 bg-slate-100 text-slate-500 text-xs rounded-full">Inactive</span>
                                    )}
                                </div>
                                <div className="text-xs text-slate-500 mt-0.5 flex gap-3">
                                    {waiver.signatureCount !== undefined && <span>✍ {waiver.signatureCount} signatures</span>}
                                    {waiver.expiryDays && <span>⏱ Expires after {waiver.expiryDays} days</span>}
                                    <span>Created {new Date(waiver.createdAt).toLocaleDateString()}</span>
                                </div>
                            </div>
                            <div className="flex items-center gap-1 shrink-0">
                                <button
                                    onClick={() => setPreviewWaiver(waiver)}
                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg"
                                    title="Preview"
                                >
                                    <Eye className="h-4 w-4" />
                                </button>
                                <button
                                    onClick={() => openEditForm(waiver)}
                                    className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg"
                                    title="Edit"
                                >
                                    <Edit className="h-4 w-4" />
                                </button>
                                <button onClick={() => handleToggle(waiver)} disabled={togglingId === waiver.id}>
                                    {togglingId === waiver.id
                                        ? <Loader2 className="h-5 w-5 animate-spin text-slate-400" />
                                        : waiver.isActive
                                            ? <ToggleRight className="h-6 w-6 text-emerald-500" />
                                            : <ToggleLeft className="h-6 w-6 text-slate-300" />}
                                </button>
                                <button
                                    onClick={() => handleDelete(waiver.id)}
                                    className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg"
                                    title="Delete"
                                >
                                    <Trash2 className="h-4 w-4" />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 text-sm text-blue-800">
                <strong>How it works:</strong> Waivers are automatically sent to clients via email before their appointment. Clients sign digitally and the signature is stored with their booking record.
            </div>
        </div>
    );
}
