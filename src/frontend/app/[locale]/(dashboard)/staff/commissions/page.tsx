"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    DollarSign, TrendingUp, RefreshCw, ChevronDown, ChevronUp,
    CheckCircle, Clock, Users, Award, Loader2, Download
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface TipRecord {
    id: string;
    bookingId: string;
    staffId: string;
    staffName?: string;
    amount: number;
    currency: string;
    isDistributed: boolean;
    distributedAt?: string;
    createdAt: string;
}

interface TipSummary {
    totalTips: number;
    totalAmount: number;
    pendingDistribution: number;
    pendingAmount: number;
    staffBreakdown: Array<{ staffId: string; staffName: string; tipCount: number; totalAmount: number; pendingAmount: number }>;
}

export default function CommissionsPage() {
    const [tips, setTips] = useState<TipRecord[]>([]);
    const [summary, setSummary] = useState<TipSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [distributing, setDistributing] = useState(false);
    const [processingCommissions, setProcessingCommissions] = useState(false);
    const [selectedTips, setSelectedTips] = useState<Set<string>>(new Set());
    const [expandedStaff, setExpandedStaff] = useState<string | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [summaryRes] = await Promise.all([
                apiClient.get('/api/v1/tips/summary'),
            ]);
            setSummary(summaryRes.data?.data || summaryRes.data);
        } catch {
            toast.error('Failed to load commission data');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleDistribute = async () => {
        const tipsToDistribute = Array.from(selectedTips);
        if (tipsToDistribute.length === 0) { toast.error('Select tips to distribute'); return; }
        setDistributing(true);
        try {
            await apiClient.post('/api/v1/tips/distribute', { tipIds: tipsToDistribute });
            toast.success(`${tipsToDistribute.length} tips distributed`);
            setSelectedTips(new Set());
            fetchData();
        } catch {
            toast.error('Failed to distribute tips');
        } finally {
            setDistributing(false);
        }
    };

    const handleProcessCommissions = async () => {
        setProcessingCommissions(true);
        try {
            await apiClient.post('/api/v1/staff/payouts/process-commissions');
            toast.success('Commission processing started');
            fetchData();
        } catch {
            toast.error('Failed to process commissions');
        } finally {
            setProcessingCommissions(false);
        }
    };

    if (loading) return (
        <div className="flex items-center justify-center h-64">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
    );

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">Tips & Commissions</h1>
                    <p className="text-foreground-secondary mt-1">Track and distribute staff tips and commissions</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchData} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button variant="outline" onClick={handleProcessCommissions} disabled={processingCommissions}>
                        {processingCommissions ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <TrendingUp className="h-4 w-4 mr-2" />}
                        Process Commissions
                    </Button>
                    {selectedTips.size > 0 && (
                        <Button onClick={handleDistribute} disabled={distributing}>
                            {distributing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <CheckCircle className="h-4 w-4 mr-2" />}
                            Distribute {selectedTips.size} Tips
                        </Button>
                    )}
                </div>
            </div>

            {/* Summary Stats */}
            {summary && (
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    {[
                        { label: 'Total Tips', value: summary.totalTips, icon: <DollarSign className="h-5 w-5 text-success-fg" /> },
                        { label: 'Total Amount', value: `$${summary.totalAmount?.toFixed(2) || '0.00'}`, icon: <TrendingUp className="h-5 w-5 text-blue-500" /> },
                        { label: 'Pending Distribution', value: summary.pendingDistribution, icon: <Clock className="h-5 w-5 text-warning-fg" /> },
                        { label: 'Pending Amount', value: `$${summary.pendingAmount?.toFixed(2) || '0.00'}`, icon: <Award className="h-5 w-5 text-primary" /> },
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
            )}

            {/* Staff Breakdown */}
            {summary?.staffBreakdown && summary.staffBreakdown.length > 0 && (
                <div className="bg-card border border-border rounded-xl overflow-hidden">
                    <div className="px-5 py-3 bg-muted border-b border-border-subtle">
                        <h2 className="font-semibold text-foreground">Staff Breakdown</h2>
                    </div>
                    {/* Header row */}
                    <div className="grid grid-cols-5 gap-4 px-5 py-2 text-xs font-semibold text-foreground-secondary uppercase tracking-wider border-b border-slate-50">
                        <div className="col-span-2">Staff</div>
                        <div className="text-right">Tips</div>
                        <div className="text-right">Total</div>
                        <div className="text-right">Pending</div>
                    </div>
                    {summary.staffBreakdown.map((staff, idx) => (
                        <div key={staff.staffId} className={idx < summary.staffBreakdown.length - 1 ? 'border-b border-slate-50' : ''}>
                            <div className="grid grid-cols-5 gap-4 px-5 py-3 items-center hover:bg-accent">
                                <div className="col-span-2 flex items-center gap-3">
                                    <div className="h-8 w-8 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
                                        {staff.staffName?.charAt(0) || 'S'}
                                    </div>
                                    <span className="font-medium text-foreground text-sm">{staff.staffName || 'Staff'}</span>
                                </div>
                                <div className="text-right text-sm text-foreground-secondary">{staff.tipCount}</div>
                                <div className="text-right text-sm font-medium text-foreground">${staff.totalAmount?.toFixed(2)}</div>
                                <div className={`text-right text-sm font-medium ${staff.pendingAmount > 0 ? 'text-warning-fg' : 'text-success-fg'}`}>
                                    ${staff.pendingAmount?.toFixed(2)}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Empty state */}
            {(!summary || !summary.staffBreakdown || summary.staffBreakdown.length === 0) && (
                <div className="text-center py-16 bg-card rounded-xl border border-border">
                    <DollarSign className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-foreground">No tip data yet</h3>
                    <p className="text-foreground-secondary text-sm mt-1">Tips will appear here after bookings are completed</p>
                </div>
            )}

            {/* Payout History */}
            <div className="bg-card border border-border rounded-xl p-5">
                <div className="flex items-center justify-between mb-4">
                    <h2 className="font-semibold text-foreground">Payout History</h2>
                    <Button variant="outline" size="sm" onClick={async () => {
                        try {
                            const res = await apiClient.get('/api/v1/staff/payouts/history');
                            toast.success(`Loaded ${(res.data?.data || []).length} payout records`);
                        } catch {
                            toast.error('Failed to load payout history');
                        }
                    }}>
                        <Download className="h-4 w-4 mr-2" /> Load History
                    </Button>
                </div>
                <p className="text-sm text-foreground-secondary">View and export historical commission payout records</p>
            </div>
        </div>
    );
}
