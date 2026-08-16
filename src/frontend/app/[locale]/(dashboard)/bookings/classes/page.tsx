"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Users, Plus, RefreshCw, UserPlus, XCircle, CheckCircle,
    Loader2, X, Save, Clock, DollarSign, Globe, Lock
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface ClassSession {
    id: string;
    groupName: string;
    maxParticipants: number;
    currentParticipants: number;
    status: 'Open' | 'Full' | 'Confirmed' | 'Completed' | 'Cancelled';
    totalPrice: number;
    isPublic: boolean;
    notes?: string;
    createdAt: string;
    spotsRemaining: number;
    isFull: boolean;
    participantCount: number;
}

interface Summary {
    total: number;
    open: number;
    full: number;
}

const STATUS_COLORS: Record<string, string> = {
    Open: 'bg-emerald-50 text-emerald-700',
    Full: 'bg-amber-50 text-amber-700',
    Confirmed: 'bg-blue-50 text-blue-700',
    Completed: 'bg-slate-100 text-slate-600',
    Cancelled: 'bg-red-50 text-red-600',
};

const DEFAULT_FORM = { groupName: '', maxParticipants: 10, pricePerParticipant: 0, isPublic: true, notes: '' };
const DEFAULT_ENROLL = { clientId: '', guestName: '', guestEmail: '', guestPhone: '' };

export default function ClassSchedulingPage() {
    const [classes, setClasses] = useState<ClassSession[]>([]);
    const [summary, setSummary] = useState<Summary | null>(null);
    const [loading, setLoading] = useState(true);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [form, setForm] = useState(DEFAULT_FORM);
    const [saving, setSaving] = useState(false);
    const [enrollingId, setEnrollingId] = useState<string | null>(null);
    const [enrollForm, setEnrollForm] = useState(DEFAULT_ENROLL);
    const [enrollingLoading, setEnrollingLoading] = useState(false);

    const fetchClasses = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/classscheduling');
            const data = res.data?.data || res.data;
            setClasses(data?.classes || []);
            setSummary({ total: data?.total || 0, open: data?.open || 0, full: data?.full || 0 });
        } catch {
            toast.error('Failed to load classes');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchClasses(); }, [fetchClasses]);

    const handleCreate = async () => {
        if (!form.groupName) { toast.error('Class name required'); return; }
        setSaving(true);
        try {
            await apiClient.post('/api/v1/classscheduling', {
                groupName: form.groupName,
                maxParticipants: form.maxParticipants,
                pricePerParticipant: form.pricePerParticipant,
                isPublic: form.isPublic,
                notes: form.notes || undefined,
            });
            toast.success('Class created');
            setShowCreateForm(false);
            setForm(DEFAULT_FORM);
            fetchClasses();
        } catch {
            toast.error('Failed to create class');
        } finally {
            setSaving(false);
        }
    };

    const handleEnroll = async () => {
        if (!enrollingId) return;
        if (!enrollForm.clientId && !enrollForm.guestEmail) {
            toast.error('Provide client ID or guest email');
            return;
        }
        setEnrollingLoading(true);
        try {
            const res = await apiClient.post(`/api/v1/classscheduling/${enrollingId}/enroll`, {
                clientId: enrollForm.clientId || undefined,
                guestName: enrollForm.guestName || undefined,
                guestEmail: enrollForm.guestEmail || undefined,
                guestPhone: enrollForm.guestPhone || undefined,
            });
            const data = res.data?.data || res.data;
            toast.success(`Enrolled! ${data?.spotsRemaining || 0} spots remaining`);
            setEnrollingId(null);
            setEnrollForm(DEFAULT_ENROLL);
            fetchClasses();
        } catch {
            toast.error('Failed to enroll');
        } finally {
            setEnrollingLoading(false);
        }
    };

    const handleCancel = async (id: string) => {
        if (!confirm('Cancel this class?')) return;
        try {
            await apiClient.delete(`/api/v1/classscheduling/${id}`);
            setClasses(prev => prev.map(c => c.id === id ? { ...c, status: 'Cancelled' } : c));
            toast.success('Class cancelled');
        } catch {
            toast.error('Failed to cancel class');
        }
    };

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Class Scheduling</h1>
                    <p className="text-slate-500 mt-1">Manage group sessions, classes, and capacity</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchClasses} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowCreateForm(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Class
                    </Button>
                </div>
            </div>

            {/* Stats */}
            {summary && (
                <div className="grid grid-cols-3 gap-4">
                    {[
                        { label: 'Total Classes', value: summary.total, icon: <Users className="h-5 w-5 text-primary-500" /> },
                        { label: 'Open', value: summary.open, icon: <CheckCircle className="h-5 w-5 text-emerald-500" /> },
                        { label: 'Full', value: summary.full, icon: <Users className="h-5 w-5 text-amber-500" /> },
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
            )}

            {/* Enroll Modal */}
            {enrollingId && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
                    <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full mx-4 p-6 space-y-4">
                        <div className="flex items-center justify-between">
                            <h3 className="font-bold text-slate-900">Enroll in Class</h3>
                            <button onClick={() => { setEnrollingId(null); setEnrollForm(DEFAULT_ENROLL); }} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                        </div>
                        <div className="space-y-3">
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-1">Client ID (if existing client)</label>
                                <Input value={enrollForm.clientId} onChange={e => setEnrollForm(p => ({ ...p, clientId: e.target.value }))} placeholder="UUID of existing client" />
                            </div>
                            <div className="text-center text-xs text-slate-400">— or guest —</div>
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">Guest Name</label>
                                    <Input value={enrollForm.guestName} onChange={e => setEnrollForm(p => ({ ...p, guestName: e.target.value }))} placeholder="Full name" />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">Guest Email</label>
                                    <Input type="email" value={enrollForm.guestEmail} onChange={e => setEnrollForm(p => ({ ...p, guestEmail: e.target.value }))} placeholder="email@example.com" />
                                </div>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-1">Phone (optional)</label>
                                <Input value={enrollForm.guestPhone} onChange={e => setEnrollForm(p => ({ ...p, guestPhone: e.target.value }))} placeholder="+1 (555) 000-0000" />
                            </div>
                        </div>
                        <div className="flex gap-3 pt-2 border-t border-slate-100">
                            <Button onClick={handleEnroll} disabled={enrollingLoading} className="flex-1">
                                {enrollingLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4 mr-2" />}
                                Enroll
                            </Button>
                            <Button variant="outline" onClick={() => { setEnrollingId(null); setEnrollForm(DEFAULT_ENROLL); }}>Cancel</Button>
                        </div>
                    </div>
                </div>
            )}

            {/* Create Form */}
            {showCreateForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">New Class Session</h2>
                        <button onClick={() => setShowCreateForm(false)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Class Name</label>
                            <Input value={form.groupName} onChange={e => setForm(p => ({ ...p, groupName: e.target.value }))} placeholder="e.g., Morning Yoga — Beginner" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Max Participants</label>
                            <Input type="number" min={2} max={200} value={form.maxParticipants} onChange={e => setForm(p => ({ ...p, maxParticipants: parseInt(e.target.value) || 2 }))} />
                        </div>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Price per Participant ($)</label>
                            <Input type="number" min={0} step="0.01" value={form.pricePerParticipant} onChange={e => setForm(p => ({ ...p, pricePerParticipant: parseFloat(e.target.value) || 0 }))} />
                        </div>
                        <div className="flex items-center gap-3 mt-6">
                            <label className="flex items-center gap-2 cursor-pointer">
                                <input type="checkbox" checked={form.isPublic} onChange={e => setForm(p => ({ ...p, isPublic: e.target.checked }))} className="rounded" />
                                <span className="text-sm text-slate-700">Public (visible on booking page)</span>
                            </label>
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Notes</label>
                        <textarea value={form.notes} onChange={e => setForm(p => ({ ...p, notes: e.target.value }))} className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm h-20 resize-none" placeholder="Equipment needed, prerequisites, etc." />
                    </div>
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleCreate} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            Create Class
                        </Button>
                        <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Classes List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-28" />)}
                </div>
            ) : classes.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
                    <Users className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700">No classes scheduled</h3>
                    <p className="text-slate-500 text-sm mt-1 mb-4">Create your first group class or session</p>
                    <Button onClick={() => setShowCreateForm(true)}><Plus className="h-4 w-4 mr-2" /> New Class</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {classes.map(cls => (
                        <div key={cls.id} className="bg-white border border-slate-200 rounded-xl p-4">
                            <div className="flex items-start gap-4">
                                <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white shrink-0">
                                    <Users className="h-5 w-5" />
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap">
                                        <span className="font-semibold text-slate-900">{cls.groupName}</span>
                                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[cls.status] || 'bg-slate-100 text-slate-600'}`}>
                                            {cls.status}
                                        </span>
                                        {cls.isPublic ? <Globe className="h-3.5 w-3.5 text-slate-400" /> : <Lock className="h-3.5 w-3.5 text-slate-400" />}
                                    </div>
                                    <div className="flex gap-4 text-xs text-slate-500 mt-1">
                                        <span className="flex items-center gap-1">
                                            <Users className="h-3 w-3" />
                                            {cls.currentParticipants}/{cls.maxParticipants}
                                            {cls.isFull && <span className="text-amber-600 font-medium ml-1">FULL</span>}
                                        </span>
                                        {cls.totalPrice > 0 && (
                                            <span className="flex items-center gap-1">
                                                <DollarSign className="h-3 w-3" />
                                                ${(cls.totalPrice / cls.maxParticipants).toFixed(2)}/person
                                            </span>
                                        )}
                                        {cls.notes && <span className="truncate max-w-48">📝 {cls.notes}</span>}
                                    </div>

                                    {/* Capacity bar */}
                                    <div className="mt-2 h-1.5 bg-slate-100 rounded-full overflow-hidden">
                                        <div
                                            className={`h-full rounded-full transition-all ${cls.isFull ? 'bg-amber-500' : 'bg-emerald-500'}`}
                                            style={{ width: `${(cls.currentParticipants / cls.maxParticipants) * 100}%` }}
                                        />
                                    </div>
                                </div>
                                <div className="flex items-center gap-1.5 shrink-0">
                                    {cls.status === 'Open' && (
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            onClick={() => { setEnrollingId(cls.id); setEnrollForm(DEFAULT_ENROLL); }}
                                            className="text-xs"
                                        >
                                            <UserPlus className="h-3.5 w-3.5 mr-1" /> Enroll
                                        </Button>
                                    )}
                                    {cls.status !== 'Cancelled' && cls.status !== 'Completed' && (
                                        <button
                                            onClick={() => handleCancel(cls.id)}
                                            className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg"
                                            title="Cancel class"
                                        >
                                            <XCircle className="h-4 w-4" />
                                        </button>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
