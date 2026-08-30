"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Users, RefreshCw, Merge, Trash2, ChevronDown, ChevronUp,
    CheckCircle, AlertTriangle, Loader2, ArrowLeft, UserCheck
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface DuplicateClient {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    createdAt: string;
    totalBookings: number;
    lifetimeValue: number;
}

interface DuplicateGroup {
    groupId: string;
    clients: DuplicateClient[];
    reason: string;
}

export default function DuplicateClientsPage() {
    const router = useRouter();
    const [groups, setGroups] = useState<DuplicateGroup[]>([]);
    const [loading, setLoading] = useState(true);
    const [expandedGroup, setExpandedGroup] = useState<string | null>(null);
    const [selectedPrimary, setSelectedPrimary] = useState<Record<string, string>>({});
    const [mergingGroup, setMergingGroup] = useState<string | null>(null);
    const [resolvedGroups, setResolvedGroups] = useState<Set<string>>(new Set());

    const fetchDuplicates = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/clients/duplicates', { params: { threshold: 60 } });
            const data = res.data?.data || res.data;
            setGroups(data?.duplicateGroups || []);
        } catch {
            toast.error('Failed to detect duplicates');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchDuplicates(); }, [fetchDuplicates]);

    const handleMerge = async (group: DuplicateGroup) => {
        const primaryId = selectedPrimary[group.groupId];
        if (!primaryId) { toast.error('Select a primary client to keep'); return; }

        setMergingGroup(group.groupId);
        const sourceIds = group.clients.filter(c => c.id !== primaryId).map(c => c.id);

        try {
            await apiClient.post(`/api/v1/clients/${primaryId}/merge`, { sourceClientIds: sourceIds });
            toast.success(`Merged ${sourceIds.length} duplicate(s) into primary client`);
            setResolvedGroups(prev => new Set([...prev, group.groupId]));
            setGroups(prev => prev.filter(g => g.groupId !== group.groupId));
        } catch {
            toast.error('Failed to merge clients');
        } finally {
            setMergingGroup(null);
        }
    };

    const pendingCount = groups.filter(g => !resolvedGroups.has(g.groupId)).length;

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center gap-4 flex-wrap">
                <button onClick={() => router.back()} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                    <ArrowLeft className="h-4 w-4" />
                </button>
                <div className="flex-1">
                    <h1 className="text-2xl font-bold text-foreground">Duplicate Client Detection</h1>
                    <p className="text-foreground-secondary mt-1">Find and merge duplicate client records to keep your database clean</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchDuplicates} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Duplicate Groups', value: groups.length, icon: <AlertTriangle className="h-5 w-5 text-warning-fg" /> },
                    { label: 'Affected Clients', value: groups.reduce((acc, g) => acc + g.clients.length, 0), icon: <Users className="h-5 w-5 text-primary" /> },
                    { label: 'Resolved', value: resolvedGroups.size, icon: <CheckCircle className="h-5 w-5 text-success-fg" /> },
                ].map(s => (
                    <div key={s.label} className="bg-card border border-border rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-muted rounded-lg">{s.icon}</div>
                        <div>
                            <div className="text-xl font-bold text-foreground">{s.value}</div>
                            <div className="text-xs text-foreground-secondary">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Groups */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-card border border-border rounded-xl p-5 animate-pulse h-32" />)}
                </div>
            ) : groups.length === 0 ? (
                <div className="text-center py-16 bg-card rounded-xl border border-border">
                    <UserCheck className="h-12 w-12 text-emerald-400 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-foreground">No duplicates found!</h3>
                    <p className="text-foreground-secondary text-sm mt-1">Your client database looks clean</p>
                </div>
            ) : (
                <div className="space-y-4">
                    {groups.map(group => (
                        <div key={group.groupId} className="bg-card border border-border rounded-xl overflow-hidden">
                            {/* Group header */}
                            <div
                                className="flex items-center gap-4 p-4 cursor-pointer hover:bg-accent"
                                onClick={() => setExpandedGroup(expandedGroup === group.groupId ? null : group.groupId)}
                            >
                                <div className="h-8 w-8 bg-amber-100 rounded-full flex items-center justify-center shrink-0">
                                    <AlertTriangle className="h-4 w-4 text-warning-fg" />
                                </div>
                                <div className="flex-1">
                                    <p className="font-semibold text-foreground">
                                        {group.clients.map(c => `${c.firstName || ''} ${c.lastName || ''}`.trim() || c.email).join(' & ')}
                                    </p>
                                    <p className="text-xs text-foreground-secondary mt-0.5">{group.clients.length} potential duplicates · {group.reason}</p>
                                </div>
                                <div className="flex items-center gap-2 shrink-0">
                                    <span className="text-xs text-foreground-muted">Group #{group.groupId}</span>
                                    {expandedGroup === group.groupId ? <ChevronUp className="h-4 w-4 text-foreground-muted" /> : <ChevronDown className="h-4 w-4 text-foreground-muted" />}
                                </div>
                            </div>

                            {/* Expanded — client comparison */}
                            {expandedGroup === group.groupId && (
                                <div className="border-t border-border-subtle p-4 space-y-3 bg-muted">
                                    <p className="text-xs font-semibold text-foreground-secondary uppercase tracking-wider">Select primary record to keep:</p>
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                        {group.clients.map(client => {
                                            const isPrimary = selectedPrimary[group.groupId] === client.id;
                                            return (
                                                <div
                                                    key={client.id}
                                                    onClick={() => setSelectedPrimary(prev => ({ ...prev, [group.groupId]: client.id }))}
                                                    className={`p-4 rounded-xl border-2 cursor-pointer transition-colors ${isPrimary ? 'border-primary-500 bg-brand-subtle' : 'border-border bg-card hover:border-primary/25'}`}
                                                >
                                                    <div className="flex items-start justify-between mb-2">
                                                        <div className="flex items-center gap-2">
                                                            <div className={`h-8 w-8 rounded-full flex items-center justify-center text-white text-xs font-bold ${isPrimary ? 'bg-primary-600' : 'bg-slate-400'}`}>
                                                                {(client.firstName?.[0] || client.email?.[0] || 'C').toUpperCase()}
                                                            </div>
                                                            <div>
                                                                <p className="font-semibold text-foreground text-sm">
                                                                    {`${client.firstName || ''} ${client.lastName || ''}`.trim() || 'Unknown'}
                                                                </p>
                                                                {isPrimary && <span className="text-xs text-primary font-medium">Primary (keep)</span>}
                                                            </div>
                                                        </div>
                                                        {isPrimary && <CheckCircle className="h-4 w-4 text-primary shrink-0" />}
                                                    </div>
                                                    <div className="space-y-1 text-xs text-foreground-secondary">
                                                        {client.email && <p>✉ {client.email}</p>}
                                                        {client.phone && <p>📱 {client.phone}</p>}
                                                        <div className="flex gap-3 pt-1 text-foreground-secondary">
                                                            <span>{client.totalBookings} bookings</span>
                                                            <span>${client.lifetimeValue?.toFixed(0) || 0} spent</span>
                                                            <span>Since {new Date(client.createdAt).toLocaleDateString()}</span>
                                                        </div>
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>

                                    <div className="flex gap-3 pt-2">
                                        <Button
                                            onClick={() => handleMerge(group)}
                                            disabled={!selectedPrimary[group.groupId] || mergingGroup === group.groupId}
                                            className="flex items-center gap-2"
                                        >
                                            {mergingGroup === group.groupId
                                                ? <Loader2 className="h-4 w-4 animate-spin" />
                                                : <Merge className="h-4 w-4" />}
                                            {mergingGroup === group.groupId ? 'Merging...' : 'Merge — Keep Primary'}
                                        </Button>
                                        <Button
                                            variant="outline"
                                            onClick={() => setExpandedGroup(null)}
                                        >
                                            Skip for now
                                        </Button>
                                    </div>
                                    <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded px-3 py-2">
                                        Merging will reassign all bookings, payments, and notes from duplicates to the primary record, then soft-delete the duplicates. This cannot be undone easily.
                                    </p>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
