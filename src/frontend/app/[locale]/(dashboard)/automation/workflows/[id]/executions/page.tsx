"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import {
    ChevronLeft, RefreshCw, CheckCircle, XCircle, Clock, AlertCircle,
    ChevronDown, ChevronRight, List, Activity
} from 'lucide-react';
import { toast } from 'sonner';

interface Execution {
    id: string;
    workflowId: string;
    status: string;
    currentStepIndex: number;
    startedAt: string;
    completedAt?: string;
    errorMessage?: string;
    durationMs?: number;
}

interface ExecutionLog {
    stepIndex: number;
    stepType: string;
    actionType: string;
    status: string;
    durationMs?: number;
    executedAt: string;
    message?: string;
    errorDetails?: string;
}

const statusIcon: Record<string, React.ReactNode> = {
    completed: <CheckCircle className="h-4 w-4 text-success-fg" />,
    failed: <XCircle className="h-4 w-4 text-danger-fg" />,
    running: <Activity className="h-4 w-4 text-blue-500 animate-pulse" />,
    pending: <Clock className="h-4 w-4 text-foreground-muted" />,
};

const statusColor: Record<string, string> = {
    completed: 'bg-emerald-100 text-emerald-700',
    failed: 'bg-red-100 text-red-600',
    running: 'bg-blue-100 text-blue-700',
    pending: 'bg-muted text-foreground-secondary',
};

const stepStatusColor: Record<string, string> = {
    completed: 'bg-emerald-500',
    failed: 'bg-red-500',
    skipped: 'bg-slate-300',
};

export default function WorkflowExecutionsPage() {
    const router = useRouter();
    const params = useParams();
    const id = params?.id as string;

    const [executions, setExecutions] = useState<Execution[]>([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [loading, setLoading] = useState(true);
    const [statusFilter, setStatusFilter] = useState('');
    const [expandedId, setExpandedId] = useState<string | null>(null);
    const [logs, setLogs] = useState<Record<string, ExecutionLog[]>>({});
    const [loadingLogs, setLoadingLogs] = useState<string | null>(null);
    const [workflowName, setWorkflowName] = useState('Workflow');

    const fetchExecutions = useCallback(async () => {
        try {
            setLoading(true);
            const params = new URLSearchParams({ page: String(page), pageSize: '20' });
            if (statusFilter) params.append('status', statusFilter);
            const res = await apiClient.get(`/api/v1/workflows/${id}/executions?${params}`);
            const data = res.data;
            setExecutions(data.data || []);
            setTotal(data.total || 0);
        } catch {
            toast.error('Failed to load executions');
        } finally {
            setLoading(false);
        }
    }, [id, page, statusFilter]);

    useEffect(() => {
        const loadName = async () => {
            try {
                const res = await apiClient.get(`/api/v1/workflows/${id}`);
                setWorkflowName((res.data?.data || res.data)?.name || 'Workflow');
            } catch {}
        };
        if (id) { loadName(); fetchExecutions(); }
    }, [id, fetchExecutions]);

    const toggleExpand = async (executionId: string) => {
        if (expandedId === executionId) { setExpandedId(null); return; }
        setExpandedId(executionId);
        if (!logs[executionId]) {
            setLoadingLogs(executionId);
            try {
                const res = await apiClient.get(`/api/v1/workflows/${id}/executions/${executionId}/logs`);
                setLogs(prev => ({ ...prev, [executionId]: res.data?.data || [] }));
            } catch {
                toast.error('Failed to load execution logs');
            } finally {
                setLoadingLogs(null);
            }
        }
    };

    const stats = {
        total: executions.length,
        completed: executions.filter(e => e.status === 'completed').length,
        failed: executions.filter(e => e.status === 'failed').length,
        avgDuration: executions.filter(e => e.durationMs).length > 0
            ? Math.round(executions.filter(e => e.durationMs).reduce((s, e) => s + (e.durationMs || 0), 0) / executions.filter(e => e.durationMs).length)
            : 0,
    };

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <button onClick={() => router.back()} className="p-2 hover:bg-accent rounded-lg">
                        <ChevronLeft className="h-5 w-5 text-foreground-secondary" />
                    </button>
                    <div>
                        <h1 className="text-2xl font-bold text-foreground">Execution Logs</h1>
                        <p className="text-foreground-secondary text-sm mt-0.5">{workflowName}</p>
                    </div>
                </div>
                <button onClick={fetchExecutions} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
                    <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                {[
                    { label: 'Total Runs', value: total, icon: <List className="h-4 w-4 text-foreground-secondary" /> },
                    { label: 'Completed', value: stats.completed, icon: <CheckCircle className="h-4 w-4 text-success-fg" /> },
                    { label: 'Failed', value: stats.failed, icon: <XCircle className="h-4 w-4 text-danger-fg" /> },
                    { label: 'Avg Duration', value: stats.avgDuration > 0 ? `${stats.avgDuration}ms` : '—', icon: <Clock className="h-4 w-4 text-blue-500" /> },
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

            {/* Filters */}
            <div className="flex gap-2">
                {['', 'completed', 'failed', 'running'].map(s => (
                    <button
                        key={s}
                        onClick={() => { setStatusFilter(s); setPage(1); }}
                        className={`px-3 py-1.5 rounded-lg text-sm font-medium capitalize transition-colors ${statusFilter === s ? 'bg-primary-600 text-white' : 'bg-card border border-border text-foreground-secondary hover:bg-accent'}`}
                    >
                        {s || 'All'}
                    </button>
                ))}
            </div>

            {/* Executions List */}
            {loading ? (
                <div className="space-y-2">
                    {[...Array(5)].map((_, i) => (
                        <div key={i} className="bg-card border border-border rounded-xl p-4 animate-pulse h-16" />
                    ))}
                </div>
            ) : executions.length === 0 ? (
                <div className="text-center py-16 bg-card rounded-xl border border-border">
                    <Activity className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-foreground">No executions yet</h3>
                    <p className="text-foreground-secondary text-sm mt-1">Run or trigger this workflow to see execution history</p>
                </div>
            ) : (
                <div className="bg-card border border-border rounded-xl overflow-hidden">
                    {executions.map((exec, idx) => (
                        <div key={exec.id} className={idx < executions.length - 1 ? 'border-b border-border-subtle' : ''}>
                            {/* Execution row */}
                            <div
                                className="flex items-center gap-4 px-5 py-4 hover:bg-accent cursor-pointer transition-colors"
                                onClick={() => toggleExpand(exec.id)}
                            >
                                <div className="shrink-0">{statusIcon[exec.status] || <Clock className="h-4 w-4 text-foreground-muted" />}</div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2">
                                        <span className="font-mono text-xs text-foreground-muted">{exec.id.slice(0, 8)}...</span>
                                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColor[exec.status] || 'bg-muted text-foreground-secondary'}`}>
                                            {exec.status}
                                        </span>
                                        {exec.durationMs && (
                                            <span className="text-xs text-foreground-muted">{exec.durationMs}ms</span>
                                        )}
                                    </div>
                                    {exec.errorMessage && (
                                        <p className="text-xs text-danger-fg mt-0.5 truncate">{exec.errorMessage}</p>
                                    )}
                                </div>
                                <div className="text-right shrink-0">
                                    <div className="text-xs text-foreground-secondary">{new Date(exec.startedAt).toLocaleString()}</div>
                                    {exec.completedAt && (
                                        <div className="text-xs text-foreground-muted">
                                            ended {new Date(exec.completedAt).toLocaleTimeString()}
                                        </div>
                                    )}
                                </div>
                                <div className="shrink-0">
                                    {expandedId === exec.id ? <ChevronDown className="h-4 w-4 text-foreground-muted" /> : <ChevronRight className="h-4 w-4 text-foreground-muted" />}
                                </div>
                            </div>

                            {/* Expanded step logs */}
                            {expandedId === exec.id && (
                                <div className="bg-muted border-t border-border-subtle px-5 py-4">
                                    {loadingLogs === exec.id ? (
                                        <div className="flex items-center gap-2 text-sm text-foreground-secondary">
                                            <RefreshCw className="h-4 w-4 animate-spin" /> Loading step logs...
                                        </div>
                                    ) : (logs[exec.id] || []).length === 0 ? (
                                        <p className="text-sm text-foreground-muted">No step logs available</p>
                                    ) : (
                                        <div className="space-y-2">
                                            <h4 className="text-xs font-semibold text-foreground-secondary uppercase tracking-wider mb-3">Step-by-Step Execution</h4>
                                            {(logs[exec.id] || []).map((log, li) => (
                                                <div key={li} className="flex items-start gap-3 bg-card rounded-lg p-3 border border-border-subtle">
                                                    <div className="shrink-0 mt-0.5">
                                                        <div className={`h-5 w-5 rounded-full flex items-center justify-center text-white text-[10px] font-bold ${stepStatusColor[log.status] || 'bg-slate-300'}`}>
                                                            {log.stepIndex + 1}
                                                        </div>
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <div className="flex items-center gap-2 mb-0.5">
                                                            <span className="text-sm font-medium text-foreground">
                                                                {log.actionType || log.stepType}
                                                            </span>
                                                            <span className={`px-1.5 py-0.5 rounded text-[10px] font-medium ${stepStatusColor[log.status] ? statusColor[log.status] || 'bg-muted text-foreground-secondary' : 'bg-muted text-foreground-secondary'}`}>
                                                                {log.status}
                                                            </span>
                                                            {log.durationMs && <span className="text-xs text-foreground-muted">{log.durationMs}ms</span>}
                                                        </div>
                                                        {log.message && <p className="text-xs text-foreground-secondary">{log.message}</p>}
                                                        {log.errorDetails && (
                                                            <div className="mt-1 p-2 bg-red-50 rounded text-xs text-red-600 font-mono">{log.errorDetails}</div>
                                                        )}
                                                    </div>
                                                    <div className="shrink-0 text-xs text-foreground-muted">
                                                        {new Date(log.executedAt).toLocaleTimeString()}
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {total > 20 && (
                <div className="flex items-center justify-between text-sm text-foreground-secondary">
                    <span>Showing {(page - 1) * 20 + 1}–{Math.min(page * 20, total)} of {total}</span>
                    <div className="flex gap-2">
                        <Button variant="outline" size="sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>Previous</Button>
                        <Button variant="outline" size="sm" onClick={() => setPage(p => p + 1)} disabled={page * 20 >= total}>Next</Button>
                    </div>
                </div>
            )}
        </div>
    );
}
