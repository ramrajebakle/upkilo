"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Clock, Users, RefreshCw, Bell, CheckCircle, XCircle,
    Calendar, Loader2, ArrowRight, Mail, Phone, Filter
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface WaitlistEntry {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    status: 'Pending' | 'Waiting' | 'Notified' | 'Converted' | 'Booked' | 'Expired' | 'Cancelled';
    preferredDate: string;
    preferredTimeRange?: string;
    priority: number;
    notes?: string;
    createdAt: string;
    service?: { id: string; name: string };
}

const STATUS_COLORS: Record<string, string> = {
    Pending: 'bg-blue-50 text-blue-700',
    Waiting: 'bg-amber-50 text-amber-700',
    Notified: 'bg-brand-subtle text-primary',
    Converted: 'bg-emerald-50 text-emerald-700',
    Booked: 'bg-emerald-50 text-emerald-700',
    Expired: 'bg-muted text-foreground-secondary',
    Cancelled: 'bg-red-50 text-red-600',
};

type FilterStatus = 'all' | 'active' | 'notified' | 'converted';

export default function WaitlistPage() {
    const [entries, setEntries] = useState<WaitlistEntry[]>([]);
    const [summary, setSummary] = useState<{ total: number; pending: number; notified: number; converted: number; expired: number } | null>(null);
    const [loading, setLoading] = useState(true);
    const [filter, setFilter] = useState<FilterStatus>('all');
    const [notifyingId, setNotifyingId] = useState<string | null>(null);
    const [convertingId, setConvertingId] = useState<string | null>(null);
    const [removingId, setRemovingId] = useState<string | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [entriesRes, summaryRes] = await Promise.all([
                apiClient.get('/api/v1/waitlistentries'),
                apiClient.get('/api/v1/waitlistentries/summary'),
            ]);
            setEntries(entriesRes.data?.data || entriesRes.data || []);
            setSummary(summaryRes.data?.data || summaryRes.data);
        } catch {
            toast.error('Failed to load waitlist');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleNotify = async (entry: WaitlistEntry) => {
        setNotifyingId(entry.id);
        try {
            await apiClient.post(`/api/v1/waitlistentries/${entry.id}/notify`);
            setEntries(prev => prev.map(e => e.id === entry.id ? { ...e, status: 'Notified' } : e));
            toast.success(`${entry.firstName} notified about availability`);
        } catch {
            toast.error('Failed to send notification');
        } finally {
            setNotifyingId(null);
        }
    };

    const handleConvert = async (entry: WaitlistEntry) => {
        setConvertingId(entry.id);
        try {
            await apiClient.post(`/api/v1/waitlistentries/${entry.id}/convert`);
            setEntries(prev => prev.map(e => e.id === entry.id ? { ...e, status: 'Converted' } : e));
            toast.success(`${entry.firstName} converted to booking`);
        } catch {
            toast.error('Failed to convert to booking');
        } finally {
            setConvertingId(null);
        }
    };

    const handleRemove = async (entry: WaitlistEntry) => {
        if (!confirm(`Remove ${entry.firstName} from waitlist?`)) return;
        setRemovingId(entry.id);
        try {
            await apiClient.delete(`/api/v1/waitlistentries/${entry.id}`);
            setEntries(prev => prev.filter(e => e.id !== entry.id));
            toast.success('Removed from waitlist');
        } catch {
            toast.error('Failed to remove entry');
        } finally {
            setRemovingId(null);
        }
    };

    const filteredEntries = entries.filter(e => {
        if (filter === 'active') return e.status === 'Pending' || e.status === 'Waiting';
        if (filter === 'notified') return e.status === 'Notified';
        if (filter === 'converted') return e.status === 'Converted' || e.status === 'Booked';
        return true;
    });

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">Waitlist</h1>
                    <p className="text-foreground-secondary mt-1">Manage clients waiting for appointment slots</p>
                </div>
                <button onClick={fetchData} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                    <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
            </div>

            {/* Summary Cards */}
            {summary && (
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    {[
                        { label: 'Total', value: summary.total, icon: <Users className="h-5 w-5 text-foreground-secondary" />, color: 'text-foreground' },
                        { label: 'Waiting', value: summary.pending, icon: <Clock className="h-5 w-5 text-warning-fg" />, color: 'text-amber-700' },
                        { label: 'Notified', value: summary.notified, icon: <Bell className="h-5 w-5 text-primary" />, color: 'text-primary' },
                        { label: 'Converted', value: summary.converted, icon: <CheckCircle className="h-5 w-5 text-success-fg" />, color: 'text-emerald-700' },
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
            )}

            {/* Filters */}
            <div className="flex gap-1 bg-muted p-1 rounded-xl w-fit">
                {(['all', 'active', 'notified', 'converted'] as FilterStatus[]).map(f => (
                    <button
                        key={f}
                        onClick={() => setFilter(f)}
                        className={`px-4 py-1.5 rounded-lg text-sm font-medium capitalize transition-colors ${filter === f ? 'bg-card text-foreground shadow-sm' : 'text-foreground-secondary hover:text-foreground'}`}
                    >
                        {f}
                    </button>
                ))}
            </div>

            {/* Waitlist Table */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(4)].map((_, i) => <div key={i} className="bg-card border border-border rounded-xl p-4 animate-pulse h-20" />)}
                </div>
            ) : filteredEntries.length === 0 ? (
                <div className="text-center py-16 bg-card rounded-xl border border-border">
                    <Clock className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-foreground">No waitlist entries</h3>
                    <p className="text-foreground-secondary text-sm mt-1">Clients will appear here when they join the waitlist</p>
                </div>
            ) : (
                <div className="bg-card border border-border rounded-xl overflow-hidden">
                    <div className="grid grid-cols-12 gap-3 px-5 py-2 text-xs font-semibold text-foreground-secondary uppercase tracking-wider bg-muted border-b border-border-subtle">
                        <div className="col-span-3">Client</div>
                        <div className="col-span-2">Contact</div>
                        <div className="col-span-2">Preferred Date</div>
                        <div className="col-span-1">Priority</div>
                        <div className="col-span-2">Status</div>
                        <div className="col-span-2 text-right">Actions</div>
                    </div>
                    {filteredEntries.map((entry, idx) => (
                        <div key={entry.id} className={`grid grid-cols-12 gap-3 px-5 py-3 items-center ${idx < filteredEntries.length - 1 ? 'border-b border-slate-50' : ''} hover:bg-accent`}>
                            <div className="col-span-3">
                                <div className="flex items-center gap-2">
                                    <div className="h-8 w-8 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
                                        {entry.firstName?.[0] || entry.email?.[0]?.toUpperCase() || 'W'}
                                    </div>
                                    <div>
                                        <p className="text-sm font-medium text-foreground">{entry.firstName} {entry.lastName}</p>
                                        {entry.service && <p className="text-xs text-foreground-muted">{entry.service.name}</p>}
                                    </div>
                                </div>
                            </div>
                            <div className="col-span-2 text-xs text-foreground-secondary space-y-0.5">
                                <div className="flex items-center gap-1"><Mail className="h-3 w-3 text-foreground-muted" />{entry.email}</div>
                                {entry.phone && <div className="flex items-center gap-1"><Phone className="h-3 w-3 text-foreground-muted" />{entry.phone}</div>}
                            </div>
                            <div className="col-span-2 text-sm text-foreground-secondary">
                                <div>{new Date(entry.preferredDate).toLocaleDateString()}</div>
                                {entry.preferredTimeRange && <div className="text-xs text-foreground-muted">{entry.preferredTimeRange}</div>}
                            </div>
                            <div className="col-span-1">
                                <span className="text-sm font-semibold text-foreground">#{entry.priority || 1}</span>
                            </div>
                            <div className="col-span-2">
                                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[entry.status] || 'bg-muted text-foreground-secondary'}`}>
                                    {entry.status}
                                </span>
                            </div>
                            <div className="col-span-2 flex items-center gap-1 justify-end">
                                {(entry.status === 'Pending' || entry.status === 'Waiting') && (
                                    <button
                                        onClick={() => handleNotify(entry)}
                                        disabled={notifyingId === entry.id}
                                        className="p-1.5 rounded-lg text-primary hover:bg-brand-subtle hover:text-primary"
                                        title="Notify client"
                                    >
                                        {notifyingId === entry.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Bell className="h-3.5 w-3.5" />}
                                    </button>
                                )}
                                {entry.status === 'Notified' && (
                                    <button
                                        onClick={() => handleConvert(entry)}
                                        disabled={convertingId === entry.id}
                                        className="p-1.5 rounded-lg text-success-fg hover:bg-emerald-50 hover:text-emerald-700"
                                        title="Convert to booking"
                                    >
                                        {convertingId === entry.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <ArrowRight className="h-3.5 w-3.5" />}
                                    </button>
                                )}
                                <button
                                    onClick={() => handleRemove(entry)}
                                    disabled={removingId === entry.id}
                                    className="p-1.5 rounded-lg text-red-400 hover:bg-red-50 hover:text-red-600"
                                    title="Remove from waitlist"
                                >
                                    {removingId === entry.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <XCircle className="h-3.5 w-3.5" />}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
