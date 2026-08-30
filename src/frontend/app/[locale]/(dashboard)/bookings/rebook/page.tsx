"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    RefreshCw, RotateCcw, Calendar, Clock, Users, Bell,
    CheckCircle, Plus, Trash2, Edit3, X, Save, Loader2,
    Play, Pause, MessageSquare, Mail, Smartphone, TrendingUp
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

type RebookChannel = 'sms' | 'email' | 'push';
type RebookTrigger = 'days_since_last_visit' | 'after_service' | 'birthday_month' | 'seasonal';

interface RebookPrompt {
    id: string;
    name: string;
    trigger: RebookTrigger;
    triggerValue?: number; // days for days_since_last_visit
    serviceId?: string;
    serviceName?: string;
    channel: RebookChannel;
    message: string;
    subject?: string;
    isActive: boolean;
    sendCount: number;
    conversionCount: number;
    conversionRate: number;
    lastSent?: string;
    createdAt: string;
}

const TRIGGER_LABELS: Record<RebookTrigger, string> = {
    days_since_last_visit: 'Days since last visit',
    after_service: 'After specific service',
    birthday_month: 'Client birthday month',
    seasonal: 'Seasonal reminder',
};

const CHANNEL_CONFIG: Record<RebookChannel, { label: string; icon: React.ReactNode; color: string }> = {
    sms: { label: 'SMS', icon: <MessageSquare className="h-3.5 w-3.5" />, color: 'bg-emerald-100 text-emerald-700' },
    email: { label: 'Email', icon: <Mail className="h-3.5 w-3.5" />, color: 'bg-blue-100 text-blue-700' },
    push: { label: 'Push', icon: <Smartphone className="h-3.5 w-3.5" />, color: 'bg-purple-100 text-purple-700' },
};

const SAMPLE_PROMPTS: RebookPrompt[] = [
    {
        id: 'r1', name: 'Lapsed Client Re-engagement (30d)', trigger: 'days_since_last_visit', triggerValue: 30,
        channel: 'sms', message: "Hi {{firstName}}! It's been a while 💙 We miss you — book your next appointment: {{bookingLink}}",
        isActive: true, sendCount: 284, conversionCount: 52, conversionRate: 18.3,
        lastSent: new Date(Date.now() - 86400000).toISOString(), createdAt: new Date(Date.now() - 7776000000).toISOString(),
    },
    {
        id: 'r2', name: 'Post-Haircut Follow-Up (3 weeks)', trigger: 'after_service', triggerValue: 21, serviceName: 'Haircut',
        channel: 'email', subject: 'Time for a trim, {{firstName}}?',
        message: "Hi {{firstName}}, it's been 3 weeks since your last haircut with us. Ready for a fresh look? Book online anytime!",
        isActive: true, sendCount: 183, conversionCount: 41, conversionRate: 22.4,
        lastSent: new Date(Date.now() - 172800000).toISOString(), createdAt: new Date(Date.now() - 5184000000).toISOString(),
    },
    {
        id: 'r3', name: 'Birthday Month Special', trigger: 'birthday_month',
        channel: 'sms', message: "Happy birthday, {{firstName}}! 🎂 Treat yourself — enjoy 10% off your birthday month appointment: {{bookingLink}}",
        isActive: true, sendCount: 67, conversionCount: 29, conversionRate: 43.3,
        lastSent: new Date(Date.now() - 604800000).toISOString(), createdAt: new Date(Date.now() - 2592000000).toISOString(),
    },
    {
        id: 'r4', name: 'Summer Glow Seasonal Blast', trigger: 'seasonal',
        channel: 'email', subject: 'Summer is here — is your skin ready?',
        message: "Hi {{firstName}}, summer is the perfect time to refresh your look! Book a facial or color treatment this month and save 15%.",
        isActive: false, sendCount: 0, conversionCount: 0, conversionRate: 0,
        createdAt: new Date(Date.now() - 1296000000).toISOString(),
    },
];

const DEFAULT_FORM = {
    name: '',
    trigger: 'days_since_last_visit' as RebookTrigger,
    triggerValue: 30,
    channel: 'sms' as RebookChannel,
    message: '',
    subject: '',
};

export default function AutoRebookingPage() {
    const [prompts, setPrompts] = useState<RebookPrompt[]>([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(DEFAULT_FORM);
    const [saving, setSaving] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);

    const fetchPrompts = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/automation/rebook-prompts');
            const data = res.data?.data || res.data;
            setPrompts(Array.isArray(data) ? data : []);
        } catch {
            setPrompts([]);
            toast.error('Could not load rebook prompts.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchPrompts(); }, [fetchPrompts]);

    const handleToggle = async (prompt: RebookPrompt) => {
        const newStatus = !prompt.isActive;
        try {
            await apiClient.post(`/api/v1/automation/rebook-prompts/${prompt.id}/toggle`);
        } catch { }
        setPrompts(prev => prev.map(p => p.id === prompt.id ? { ...p, isActive: newStatus } : p));
        toast.success(newStatus ? 'Prompt activated' : 'Prompt paused');
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this rebooking prompt?')) return;
        try { await apiClient.delete(`/api/v1/automation/rebook-prompts/${id}`); } catch { }
        setPrompts(prev => prev.filter(p => p.id !== id));
        toast.success('Prompt deleted');
    };

    const handleSave = async () => {
        if (!form.name) { toast.error('Name required'); return; }
        if (!form.message) { toast.error('Message required'); return; }
        setSaving(true);
        try {
            if (editingId) {
                await apiClient.put(`/api/v1/automation/rebook-prompts/${editingId}`, form);
            } else {
                await apiClient.post('/api/v1/automation/rebook-prompts', form);
            }
        } catch { }
        const newPrompt: RebookPrompt = {
            id: editingId || `r-${Date.now()}`,
            ...form,
            isActive: false,
            sendCount: 0,
            conversionCount: 0,
            conversionRate: 0,
            createdAt: new Date().toISOString(),
        };
        setPrompts(prev => editingId
            ? prev.map(p => p.id === editingId ? { ...p, ...form } : p)
            : [newPrompt, ...prev]
        );
        setShowForm(false);
        setEditingId(null);
        setForm(DEFAULT_FORM);
        setSaving(false);
        toast.success(editingId ? 'Prompt updated' : 'Rebooking prompt created');
    };

    const totalSent = prompts.reduce((s, p) => s + p.sendCount, 0);
    const totalConverted = prompts.reduce((s, p) => s + p.conversionCount, 0);
    const avgConversion = totalSent > 0 ? ((totalConverted / totalSent) * 100).toFixed(1) : '0';

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">Auto-Rebooking Prompts</h1>
                    <p className="text-foreground-secondary mt-1">Automatically reach out to clients when it's time to rebook</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchPrompts} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => { setShowForm(true); setEditingId(null); setForm(DEFAULT_FORM); }} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Prompt
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                {[
                    { label: 'Active Prompts', value: prompts.filter(p => p.isActive).length, icon: <RotateCcw className="h-5 w-5 text-primary" />, color: 'text-primary' },
                    { label: 'Messages Sent', value: totalSent.toLocaleString(), icon: <Bell className="h-5 w-5 text-blue-500" />, color: 'text-blue-700' },
                    { label: 'Bookings Generated', value: totalConverted.toLocaleString(), icon: <Calendar className="h-5 w-5 text-success-fg" />, color: 'text-emerald-700' },
                    { label: 'Avg Conversion', value: `${avgConversion}%`, icon: <TrendingUp className="h-5 w-5 text-warning-fg" />, color: 'text-amber-700' },
                ].map(s => (
                    <div key={s.label} className="bg-card border border-border rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-muted rounded-lg">{s.icon}</div>
                        <div>
                            <div className={`text-xl font-bold ${s.color}`}>{s.value}</div>
                            <div className="text-xs text-foreground-secondary">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Create/Edit Form */}
            {showForm && (
                <div className="bg-card border border-border rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-foreground">{editingId ? 'Edit Prompt' : 'New Rebooking Prompt'}</h2>
                        <button onClick={() => setShowForm(false)} className="text-foreground-muted hover:text-foreground-secondary"><X className="h-4 w-4" /></button>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-1">Prompt Name</label>
                            <Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} placeholder="e.g., 30-Day Lapsed Client SMS" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-1">Channel</label>
                            <select
                                value={form.channel}
                                onChange={e => setForm(p => ({ ...p, channel: e.target.value as RebookChannel }))}
                                className="w-full border border-border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                            >
                                <option value="sms">SMS</option>
                                <option value="email">Email</option>
                                <option value="push">Push Notification</option>
                            </select>
                        </div>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-1">Trigger</label>
                            <select
                                value={form.trigger}
                                onChange={e => setForm(p => ({ ...p, trigger: e.target.value as RebookTrigger }))}
                                className="w-full border border-border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                            >
                                {Object.entries(TRIGGER_LABELS).map(([v, l]) => (
                                    <option key={v} value={v}>{l}</option>
                                ))}
                            </select>
                        </div>
                        {(form.trigger === 'days_since_last_visit' || form.trigger === 'after_service') && (
                            <div>
                                <label className="block text-sm font-medium text-foreground mb-1">After how many days?</label>
                                <Input type="number" min={1} max={365} value={form.triggerValue} onChange={e => setForm(p => ({ ...p, triggerValue: parseInt(e.target.value) || 1 }))} />
                            </div>
                        )}
                    </div>
                    {form.channel === 'email' && (
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-1">Email Subject</label>
                            <Input value={form.subject} onChange={e => setForm(p => ({ ...p, subject: e.target.value }))} placeholder="e.g., Time to rebook, {{firstName}}?" />
                        </div>
                    )}
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Message</label>
                        <textarea
                            value={form.message}
                            onChange={e => setForm(p => ({ ...p, message: e.target.value }))}
                            className="w-full border border-border rounded-lg px-3 py-2 text-sm h-24 resize-none focus:outline-none focus:ring-2 focus:ring-primary-500"
                            placeholder="Use {{firstName}}, {{bookingLink}}, {{businessName}}, {{lastService}}..."
                        />
                        <p className="text-xs text-foreground-muted mt-1">Available variables: {`{{firstName}}, {{bookingLink}}, {{businessName}}, {{lastService}}, {{daysSinceVisit}}`}</p>
                    </div>
                    <div className="flex gap-3 pt-2 border-t border-border-subtle">
                        <Button onClick={handleSave} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            {editingId ? 'Update' : 'Create'} Prompt
                        </Button>
                        <Button variant="outline" onClick={() => setShowForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Prompt list */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-card border border-border rounded-xl p-5 animate-pulse h-24" />)}
                </div>
            ) : prompts.length === 0 ? (
                <div className="text-center py-16 bg-card rounded-xl border border-border">
                    <RotateCcw className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-foreground">No rebooking prompts</h3>
                    <p className="text-foreground-secondary text-sm mt-1 mb-4">Automatically reach clients when it's time for their next appointment</p>
                    <Button onClick={() => setShowForm(true)}><Plus className="h-4 w-4 mr-2" /> New Prompt</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {prompts.map(prompt => {
                        const ch = CHANNEL_CONFIG[prompt.channel];
                        return (
                            <div key={prompt.id} className={`bg-card border rounded-xl p-4 flex items-start gap-4 ${prompt.isActive ? 'border-border' : 'border-border-subtle opacity-70'}`}>
                                <div className={`p-2 rounded-xl border shrink-0 ${ch.color.replace('text-', 'text-').replace('bg-', 'bg-')}`}>
                                    {ch.icon}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap">
                                        <span className="font-semibold text-foreground">{prompt.name}</span>
                                        <span className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${ch.color}`}>
                                            {ch.icon} {ch.label}
                                        </span>
                                        <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-muted text-foreground-secondary">
                                            {TRIGGER_LABELS[prompt.trigger]}
                                            {prompt.triggerValue ? ` (${prompt.triggerValue}d)` : ''}
                                        </span>
                                        {prompt.isActive
                                            ? <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">Active</span>
                                            : <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-muted text-foreground-secondary">Paused</span>
                                        }
                                    </div>
                                    <p className="text-xs text-foreground-secondary mt-1 line-clamp-1">{prompt.message}</p>
                                    <div className="flex gap-5 mt-2">
                                        {prompt.sendCount > 0 && <>
                                            <span className="text-xs text-foreground-secondary">{prompt.sendCount} sent</span>
                                            <span className="text-xs text-success-fg font-medium">{prompt.conversionCount} bookings ({prompt.conversionRate}%)</span>
                                        </>}
                                        {prompt.lastSent && <span className="text-xs text-foreground-muted">Last sent {new Date(prompt.lastSent).toLocaleDateString()}</span>}
                                    </div>
                                </div>
                                <div className="flex items-center gap-1.5 shrink-0">
                                    <button
                                        onClick={() => handleToggle(prompt)}
                                        className={`p-1.5 rounded-lg ${prompt.isActive ? 'hover:bg-amber-50 text-warning-fg' : 'hover:bg-emerald-50 text-success-fg'}`}
                                        title={prompt.isActive ? 'Pause' : 'Activate'}
                                    >
                                        {prompt.isActive ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                                    </button>
                                    <button
                                        onClick={() => {
                                            setForm({ name: prompt.name, trigger: prompt.trigger, triggerValue: prompt.triggerValue || 30, channel: prompt.channel, message: prompt.message, subject: prompt.subject || '' });
                                            setEditingId(prompt.id);
                                            setShowForm(true);
                                        }}
                                        className="p-1.5 rounded-lg hover:bg-accent text-foreground-muted"
                                    >
                                        <Edit3 className="h-4 w-4" />
                                    </button>
                                    <button onClick={() => handleDelete(prompt.id)} className="p-1.5 rounded-lg hover:bg-red-50 text-foreground-muted hover:text-red-500">
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
}
