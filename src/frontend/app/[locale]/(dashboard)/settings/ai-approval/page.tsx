"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Brain, CheckCircle, XCircle, Clock, RefreshCw, Trash2,
    ChevronDown, ChevronRight, Edit3, Save, X, Loader2,
    AlertTriangle, Shield, CreditCard, Activity, Link as LinkIcon
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';

interface EscalationItem {
    id: string;
    module: 'AI' | 'Billing' | 'Security' | 'Workflow';
    reason: string;
    severity: 'Critical' | 'High' | 'Medium' | 'Low';
    isResolved: boolean;
    createdAt: string;
    metadata: any;
    resolutionNotes?: string;
    actionTaken?: string;
}

const SEVERITY_COLORS = {
    Critical: 'text-red-700 bg-red-50 border-red-200',
    High: 'text-amber-700 bg-amber-50 border-amber-200',
    Medium: 'text-blue-700 bg-blue-50 border-blue-200',
    Low: 'text-foreground bg-muted border-border'
};

const MODULE_ICONS = {
    AI: <Brain className="h-4 w-4" />,
    Billing: <CreditCard className="h-4 w-4" />,
    Security: <Shield className="h-4 w-4" />,
    Workflow: <Activity className="h-4 w-4" />
};

import { FeatureGate } from '@/components/ui/FeatureGate';

export default function SystemEscalationsPage() {
    return (
        <FeatureGate 
            featureName="AiFeatures" 
            title="AI Features"
            description="Upgrade your plan to unlock AI features, system escalations, and automated decision making."
        >
            <SystemEscalationsContent />
        </FeatureGate>
    );
}

function SystemEscalationsContent() {
    const router = useRouter();
    const [items, setItems] = useState<EscalationItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [stats, setStats] = useState({ total: 0, pending: 0, critical: 0 });
    const [filter, setFilter] = useState<'all' | 'AI' | 'Billing' | 'Security'>('all');
    const [expandedId, setExpandedId] = useState<string | null>(null);
    const [noteInput, setNoteInput] = useState('');
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    const fetchEscalations = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/escalations', {
                params: filter !== 'all' ? { module: filter } : {}
            });
            const data = res.data;
            setItems(data.items || []);
            setStats({
                total: data.total || 0,
                pending: data.pending || 0,
                critical: (data.items as EscalationItem[]).filter(i => i.severity === 'Critical').length
            });
        } catch (error) {
            console.error('Failed to fetch escalations', error);
            toast.error('Failed to load escalations');
        } finally {
            setLoading(false);
        }
    }, [filter]);

    useEffect(() => { fetchEscalations(); }, [fetchEscalations]);

    const handleResolve = async (id: string, approved: boolean) => {
        setActionLoading(id);
        try {
            await apiClient.post(`/api/v1/escalations/${id}/resolve`, {
                approved,
                notes: noteInput || undefined
            });
            toast.success(approved ? 'Escalation Approved' : 'Escalation Rejected/Resolved');
            setItems(prev => prev.filter(i => i.id !== id));
            setNoteInput('');
        } catch (error) {
            toast.error('Failed to resolve escalation');
        } finally {
            setActionLoading(null);
        }
    };

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">System Escalations</h1>
                    <p className="text-foreground-secondary mt-1">Unified Command Center for high-confidence autonomous oversight</p>
                </div>
                <button onClick={fetchEscalations} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                    <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
            </div>

            {/* Quick Stats */}
            <div className="grid grid-cols-3 gap-4">
                <div className="bg-card border border-border rounded-xl p-4">
                    <div className="text-foreground-secondary text-xs font-medium uppercase mb-1">Pending Review</div>
                    <div className="text-2xl font-bold text-foreground">{stats.pending}</div>
                </div>
                <div className="bg-card border border-border rounded-xl p-4 border-l-4 border-l-red-500">
                    <div className="text-danger-fg text-xs font-medium uppercase mb-1">Critical Risks</div>
                    <div className="text-2xl font-bold text-red-700">{stats.critical}</div>
                </div>
                <div className="bg-card border border-border rounded-xl p-4">
                    <div className="text-foreground-secondary text-xs font-medium uppercase mb-1">System Health</div>
                    <div className="text-2xl font-bold text-success-fg">{stats.pending === 0 ? 'Optimal' : 'Stable'}</div>
                </div>
            </div>

            {/* Filter Tabs */}
            <div className="flex gap-2 p-1 bg-muted rounded-xl w-fit">
                {['all', 'AI', 'Billing', 'Security'].map(f => (
                    <button
                        key={f}
                        onClick={() => setFilter(f as any)}
                        className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-all ${filter === f ? 'bg-card text-foreground shadow-sm' : 'text-foreground-secondary hover:text-foreground'}`}
                    >
                        {f.charAt(0).toUpperCase() + f.slice(1)}
                    </button>
                ))}
            </div>

            {/* Escalation List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-card border border-border rounded-xl p-6 h-24 animate-pulse" />)}
                </div>
            ) : items.length === 0 ? (
                <div className="text-center py-20 bg-card border border-dashed border-border-strong rounded-2xl">
                    <CheckCircle className="h-12 w-12 text-success-fg mx-auto mb-4" />
                    <h3 className="text-lg font-semibold text-foreground">Clear Skies</h3>
                    <p className="text-foreground-secondary text-sm mt-1">No pending system escalations require your attention.</p>
                </div>
            ) : (
                <div className="space-y-3">
                    {items.map(item => (
                        <div key={item.id} className={`bg-card border rounded-2xl transition-all ${expandedId === item.id ? 'ring-2 ring-primary-100 border-primary/25' : 'border-border hover:border-border-strong'}`}>
                            <div className="p-5 flex items-start gap-4">
                                <div className={`p-2.5 rounded-xl ${SEVERITY_COLORS[item.severity].split(' ')[1]}`}>
                                    {MODULE_ICONS[item.module]}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap mb-1">
                                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase border ${SEVERITY_COLORS[item.severity]}`}>
                                            {item.severity}
                                        </span>
                                        <span className="text-xs font-semibold text-foreground-muted px-1.5 border-l border-border">
                                            {item.module}
                                        </span>
                                        <span className="text-[10px] text-foreground-muted ml-auto">
                                            {new Date(item.createdAt).toLocaleString()}
                                        </span>
                                    </div>
                                    <h3 className="text-sm font-semibold text-foreground">{item.reason}</h3>
                                </div>
                                <button 
                                    onClick={() => setExpandedId(expandedId === item.id ? null : item.id)}
                                    className="p-2 hover:bg-accent rounded-lg text-foreground-muted"
                                >
                                    {expandedId === item.id ? <ChevronDown className="h-5 w-5" /> : <ChevronRight className="h-5 w-5" />}
                                </button>
                            </div>

                            {expandedId === item.id && (
                                <div className="p-5 pt-0 border-t border-slate-50 bg-muted/50 rounded-b-2xl">
                                    <div className="mt-4 space-y-4">
                                        {/* Module Specific Metadata */}
                                        {item.module === 'AI' && item.metadata && (
                                            <div className="bg-card border border-border rounded-xl p-4 shadow-sm">
                                                <div className="text-[10px] uppercase font-bold text-foreground-muted mb-2">AI Suggestion (Score: {item.metadata.Score}%)</div>
                                                <div className="text-xs text-foreground-secondary font-mono bg-muted p-3 rounded-lg border border-border-subtle max-h-40 overflow-y-auto">
                                                    {item.metadata.Content}
                                                </div>
                                            </div>
                                        )}

                                        {item.module === 'Billing' && (
                                            <div className="bg-brand-subtle border border-primary/25 rounded-xl p-4 flex items-center justify-between">
                                                <div className="flex items-center gap-3">
                                                    <CreditCard className="h-5 w-5 text-primary" />
                                                    <div>
                                                        <div className="text-sm font-semibold text-primary-900">Credit Restoration Required</div>
                                                        <div className="text-xs text-primary">Add top-up or upgrade plan to resume services.</div>
                                                    </div>
                                                </div>
                                                <Button 
                                                    size="sm" 
                                                    className="bg-primary-600 hover:bg-primary-700"
                                                    onClick={() => router.push('/settings/billing')}
                                                >
                                                    <LinkIcon className="h-3.5 w-3.5 mr-1.5" /> Fix Billing
                                                </Button>
                                            </div>
                                        )}

                                        {/* Resolution Actions */}
                                        <div className="space-y-3">
                                            <Input 
                                                placeholder="Internal resolution notes..."
                                                value={noteInput}
                                                onChange={e => setNoteInput(e.target.value)}
                                                className="bg-card border-border"
                                            />
                                            <div className="flex gap-2">
                                                <Button 
                                                    onClick={() => handleResolve(item.id, true)}
                                                    disabled={!!actionLoading}
                                                    className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white"
                                                >
                                                    {actionLoading === item.id ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Approve / Acknowledge'}
                                                </Button>
                                                <Button 
                                                    onClick={() => handleResolve(item.id, false)}
                                                    disabled={!!actionLoading}
                                                    variant="outline"
                                                    className="flex-1 text-danger-fg border-red-200 hover:bg-red-50"
                                                >
                                                    Reject / Resolve
                                                </Button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
