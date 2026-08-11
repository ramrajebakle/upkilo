'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
    AlertTriangle, RefreshCw, CheckCircle, Calendar, Clock,
    Loader2, UserX, CalendarClock, XCircle, ChevronDown,
    User, Zap, Shield, BarChart3, ArrowRight, Download,
    Filter, ChevronUp, Sparkles
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { apiClient as api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import Link from 'next/link';

// ─── Types ────────────────────────────────────────────────────────────────────

interface ConflictBooking {
    id: string;
    clientName: string;
    serviceName: string;
    staffName: string;
    staffId: string;
    startTime: string;
    endTime: string;
}

interface Conflict {
    id: string;
    type: 'staff_double_booking' | 'client_double_booking' | 'resource_conflict' | 'blocked_time';
    severity: 'critical' | 'high' | 'medium' | 'low';
    bookingA: ConflictBooking;
    bookingB: ConflictBooking;
    overlapMinutes: number;
    suggestedResolution?: {
        action: 'reschedule' | 'cancel' | 'reassign_staff';
        reason: string;
        targetBookingId: string;
        newStartTime?: string;
    };
}

interface Resolution {
    bookingId: string;
    type: 'reschedule' | 'cancel' | 'reassign_staff';
    newStartTime?: string;
    newStaffId?: string;
}

const CONFLICT_TYPE_CONFIG = {
    staff_double_booking: { label: 'Staff Double-Booking', icon: UserX, color: 'red' },
    client_double_booking: { label: 'Client Double-Booking', icon: User, color: 'orange' },
    resource_conflict: { label: 'Resource Conflict', icon: AlertTriangle, color: 'amber' },
    blocked_time: { label: 'Blocked Time Overlap', icon: Shield, color: 'gray' },
};

const SEVERITY_CONFIG = {
    critical: { label: 'Critical', bg: 'bg-red-50', border: 'border-red-200', text: 'text-red-700', badge: 'bg-red-100 text-red-700' },
    high: { label: 'High', bg: 'bg-orange-50', border: 'border-orange-200', text: 'text-orange-700', badge: 'bg-orange-100 text-orange-700' },
    medium: { label: 'Medium', bg: 'bg-amber-50', border: 'border-amber-200', text: 'text-amber-700', badge: 'bg-amber-100 text-amber-700' },
    low: { label: 'Low', bg: 'bg-gray-50', border: 'border-gray-200', text: 'text-gray-600', badge: 'bg-gray-100 text-gray-600' },
};

export default function ConflictsPage() {
    const { success: toastSuccess, error: toastError } = useToast();

    const [conflicts, setConflicts] = useState<Conflict[]>([]);
    const [loading, setLoading] = useState(true);
    const [totalScanned, setTotalScanned] = useState(0);
    const [resolvingId, setResolvingId] = useState<string | null>(null);
    const [resolutionForm, setResolutionForm] = useState<Resolution | null>(null);
    const [resolvedIds, setResolvedIds] = useState<Set<string>>(new Set());
    const [expandedConflict, setExpandedConflict] = useState<string | null>(null);
    const [severityFilter, setSeverityFilter] = useState<string | null>(null);
    const [typeFilter, setTypeFilter] = useState<string | null>(null);
    const [bulkResolving, setBulkResolving] = useState(false);

    const [dateFrom, setDateFrom] = useState(() => {
        const d = new Date();
        d.setDate(d.getDate() - 7);
        return d.toISOString().slice(0, 10);
    });
    const [dateTo, setDateTo] = useState(() => {
        const d = new Date();
        d.setDate(d.getDate() + 30);
        return d.toISOString().slice(0, 10);
    });

    const fetchConflicts = useCallback(async () => {
        setLoading(true);
        try {
            const res = await api.get('/api/v1/bookings/conflicts', {
                params: { from: dateFrom, to: dateTo }
            });
            const data = res.data?.data ?? res.data;
            const raw: Conflict[] = (data?.conflicts ?? []).map((c: any, i: number) => ({
                id: c.id ?? `conflict-${i}`,
                type: c.type ?? 'staff_double_booking',
                severity: c.severity ?? (c.overlapMinutes > 30 ? 'critical' : c.overlapMinutes > 15 ? 'high' : 'medium'),
                bookingA: c.bookingA,
                bookingB: c.bookingB,
                overlapMinutes: c.overlapMinutes ?? 0,
                suggestedResolution: c.suggestedResolution ?? buildSuggestion(c),
            }));
            // Sort by severity
            const order = { critical: 0, high: 1, medium: 2, low: 3 };
            raw.sort((a, b) => order[a.severity] - order[b.severity]);
            setConflicts(raw);
            setTotalScanned(data?.scannedBookings ?? 0);
        } catch {
            toastError('Failed to detect conflicts');
        } finally {
            setLoading(false);
        }
    }, [dateFrom, dateTo]);

    useEffect(() => { fetchConflicts(); }, [fetchConflicts]);

    // Build AI-style suggested resolution from conflict data
    function buildSuggestion(c: any) {
        if (!c.bookingA || !c.bookingB) return undefined;
        // Prefer rescheduling the later booking
        const laterBooking = new Date(c.bookingA.startTime) > new Date(c.bookingB.startTime)
            ? c.bookingA : c.bookingB;
        const newStart = new Date(laterBooking.endTime);
        newStart.setMinutes(newStart.getMinutes() + 15);
        return {
            action: 'reschedule',
            reason: 'Move the later booking to immediately after the earlier one ends.',
            targetBookingId: laterBooking.id,
            newStartTime: newStart.toISOString(),
        };
    }

    const handleResolve = async () => {
        if (!resolutionForm) return;
        setResolvingId(resolutionForm.bookingId);
        try {
            await api.post(`/api/v1/bookings/${resolutionForm.bookingId}/resolve-conflict`, {
                resolution: resolutionForm.type,
                newStartTime: resolutionForm.newStartTime || undefined,
                newStaffId: resolutionForm.newStaffId || undefined,
            });
            toastSuccess('Conflict resolved');
            setResolvedIds(prev => new Set([...prev, resolutionForm.bookingId]));
            setResolutionForm(null);
            fetchConflicts();
        } catch {
            toastError('Failed to resolve conflict');
        } finally {
            setResolvingId(null);
        }
    };

    const applyAISuggestion = async (conflict: Conflict) => {
        const s = conflict.suggestedResolution;
        if (!s) return;
        setResolvingId(s.targetBookingId);
        try {
            await api.post(`/api/v1/bookings/${s.targetBookingId}/resolve-conflict`, {
                resolution: s.action,
                newStartTime: s.newStartTime,
            });
            toastSuccess('AI suggestion applied');
            setResolvedIds(prev => new Set([...prev, s.targetBookingId]));
            fetchConflicts();
        } catch {
            toastError('Failed to apply suggestion');
        } finally {
            setResolvingId(null);
        }
    };

    const bulkAutoResolve = async () => {
        const pending = filteredConflicts.filter(c => c.severity !== 'critical' && c.suggestedResolution);
        if (pending.length === 0) {
            toastError('No non-critical conflicts with auto-suggestions to resolve');
            return;
        }
        if (!confirm(`Auto-resolve ${pending.length} non-critical conflict(s)?`)) return;
        setBulkResolving(true);
        let resolved = 0;
        for (const c of pending) {
            const s = c.suggestedResolution!;
            try {
                await api.post(`/api/v1/bookings/${s.targetBookingId}/resolve-conflict`, {
                    resolution: s.action,
                    newStartTime: s.newStartTime,
                });
                setResolvedIds(prev => new Set([...prev, s.targetBookingId]));
                resolved++;
            } catch { /* continue */ }
        }
        setBulkResolving(false);
        toastSuccess(`Auto-resolved ${resolved} conflict(s)`);
        fetchConflicts();
    };

    const exportReport = () => {
        const rows = [['Type', 'Severity', 'Overlap Min', 'Client A', 'Staff', 'Start A', 'Client B', 'Start B']];
        filteredConflicts.forEach(c => {
            rows.push([
                c.type, c.severity, String(c.overlapMinutes),
                c.bookingA.clientName, c.bookingA.staffName,
                new Date(c.bookingA.startTime).toLocaleString(),
                c.bookingB.clientName,
                new Date(c.bookingB.startTime).toLocaleString()
            ]);
        });
        const csv = rows.map(r => r.map(v => `"${v}"`).join(',')).join('\n');
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = 'conflicts-report.csv'; a.click();
        URL.revokeObjectURL(url);
    };

    const pendingConflicts = conflicts.filter(c =>
        !resolvedIds.has(c.bookingA.id) && !resolvedIds.has(c.bookingB.id)
    );

    const filteredConflicts = pendingConflicts.filter(c => {
        if (severityFilter && c.severity !== severityFilter) return false;
        if (typeFilter && c.type !== typeFilter) return false;
        return true;
    });

    const criticalCount = pendingConflicts.filter(c => c.severity === 'critical').length;

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <div className="bg-white border-b border-gray-100 px-6 py-5 sticky top-0 z-10 shadow-sm">
                <div className="max-w-5xl mx-auto flex items-center justify-between gap-4 flex-wrap">
                    <div className="flex items-center gap-3">
                        <Link href="/bookings" className="p-2 hover:bg-gray-100 rounded-lg transition-colors text-gray-500">
                            <ArrowRight className="w-4 h-4 rotate-180" />
                        </Link>
                        <div>
                            <h1 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                                <AlertTriangle className="w-5 h-5 text-red-500" />
                                Booking Conflicts
                            </h1>
                            <p className="text-sm text-gray-500">
                                {totalScanned} bookings scanned · {pendingConflicts.length} conflicts detected
                                {criticalCount > 0 && (
                                    <span className="ml-2 text-red-600 font-medium">⚠ {criticalCount} critical</span>
                                )}
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <button
                            onClick={bulkAutoResolve}
                            disabled={bulkResolving || filteredConflicts.filter(c => c.severity !== 'critical').length === 0}
                            className="flex items-center gap-2 px-3 py-2 bg-primary-600 text-white rounded-lg text-sm font-medium hover:bg-primary-700 disabled:opacity-40 transition-colors"
                        >
                            {bulkResolving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
                            Auto-Resolve
                        </button>
                        <button onClick={exportReport} className="flex items-center gap-2 px-3 py-2 border border-gray-200 text-gray-600 rounded-lg text-sm hover:bg-gray-50 transition-colors">
                            <Download className="w-4 h-4" />
                            Export
                        </button>
                        <button
                            onClick={fetchConflicts}
                            className="p-2 hover:bg-gray-100 rounded-lg transition-colors text-gray-500"
                        >
                            <RefreshCw className={cn('w-4 h-4', loading && 'animate-spin')} />
                        </button>
                    </div>
                </div>
            </div>

            <div className="max-w-5xl mx-auto px-6 py-6 space-y-5">
                {/* Scan controls */}
                <div className="bg-white border border-gray-100 rounded-xl p-4 flex items-center gap-4 flex-wrap shadow-sm">
                    <div className="flex items-center gap-2">
                        <label className="text-sm font-medium text-gray-700">From</label>
                        <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)}
                            className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400" />
                    </div>
                    <div className="flex items-center gap-2">
                        <label className="text-sm font-medium text-gray-700">To</label>
                        <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)}
                            className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400" />
                    </div>
                    <button onClick={fetchConflicts}
                        className="px-4 py-1.5 bg-gray-900 text-white rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors">
                        Scan
                    </button>

                    {/* Filters */}
                    <div className="ml-auto flex gap-2">
                        <select
                            value={severityFilter ?? ''}
                            onChange={e => setSeverityFilter(e.target.value || null)}
                            className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm bg-white focus:outline-none"
                        >
                            <option value="">All Severities</option>
                            {Object.entries(SEVERITY_CONFIG).map(([k, v]) => (
                                <option key={k} value={k}>{v.label}</option>
                            ))}
                        </select>
                        <select
                            value={typeFilter ?? ''}
                            onChange={e => setTypeFilter(e.target.value || null)}
                            className="px-3 py-1.5 border border-gray-200 rounded-lg text-sm bg-white focus:outline-none"
                        >
                            <option value="">All Types</option>
                            {Object.entries(CONFLICT_TYPE_CONFIG).map(([k, v]) => (
                                <option key={k} value={k}>{v.label}</option>
                            ))}
                        </select>
                    </div>
                </div>

                {/* Stats */}
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                    {[
                        { label: 'Total', value: conflicts.length, color: 'text-gray-900' },
                        { label: 'Critical', value: criticalCount, color: 'text-red-700' },
                        { label: 'Pending', value: pendingConflicts.length, color: 'text-amber-700' },
                        { label: 'Resolved', value: resolvedIds.size, color: 'text-emerald-700' },
                    ].map(s => (
                        <div key={s.label} className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm text-center">
                            <p className={cn('text-2xl font-bold', s.color)}>{s.value}</p>
                            <p className="text-xs text-gray-500 mt-0.5">{s.label}</p>
                        </div>
                    ))}
                </div>

                {/* Resolution panel */}
                {resolutionForm && (
                    <div className="bg-primary-50 border border-primary-200 rounded-xl p-5 space-y-4 shadow-sm">
                        <div className="flex items-center justify-between">
                            <h3 className="font-semibold text-gray-900 flex items-center gap-2">
                                <Zap className="w-4 h-4 text-primary-600" />
                                Resolve Booking #{resolutionForm.bookingId.slice(0, 8)}
                            </h3>
                            <button onClick={() => setResolutionForm(null)}>
                                <XCircle className="h-4 w-4 text-gray-400 hover:text-gray-600" />
                            </button>
                        </div>

                        <div className="flex gap-2 flex-wrap">
                            {[
                                { value: 'reschedule', label: 'Reschedule', icon: CalendarClock },
                                { value: 'cancel', label: 'Cancel Booking', icon: XCircle },
                                { value: 'reassign_staff', label: 'Reassign Staff', icon: User },
                            ].map(opt => {
                                const Icon = opt.icon;
                                return (
                                    <button
                                        key={opt.value}
                                        onClick={() => setResolutionForm(p => p ? { ...p, type: opt.value as any } : p)}
                                        className={cn(
                                            'flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium border transition-all',
                                            resolutionForm.type === opt.value
                                                ? 'bg-primary-600 text-white border-primary-600'
                                                : 'bg-white border-gray-200 text-gray-600 hover:bg-gray-50'
                                        )}
                                    >
                                        <Icon className="w-3.5 h-3.5" /> {opt.label}
                                    </button>
                                );
                            })}
                        </div>

                        {resolutionForm.type === 'reschedule' && (
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">New start time</label>
                                <input
                                    type="datetime-local"
                                    value={resolutionForm.newStartTime ?? ''}
                                    onChange={e => setResolutionForm(p => p ? { ...p, newStartTime: e.target.value } : p)}
                                    className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400"
                                />
                            </div>
                        )}

                        {resolutionForm.type === 'reassign_staff' && (
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">New staff member ID</label>
                                <input
                                    value={resolutionForm.newStaffId ?? ''}
                                    onChange={e => setResolutionForm(p => p ? { ...p, newStaffId: e.target.value } : p)}
                                    placeholder="Staff UUID"
                                    className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 w-64"
                                />
                            </div>
                        )}

                        <button
                            onClick={handleResolve}
                            disabled={!!resolvingId}
                            className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg text-sm font-bold hover:bg-primary-700 disabled:opacity-50 transition-colors"
                        >
                            {resolvingId ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle className="w-4 h-4" />}
                            Apply Resolution
                        </button>
                    </div>
                )}

                {/* Conflicts list */}
                {loading ? (
                    <div className="space-y-3">
                        {[...Array(3)].map((_, i) => (
                            <div key={i} className="bg-white rounded-xl border border-gray-100 p-5 animate-pulse h-32" />
                        ))}
                    </div>
                ) : filteredConflicts.length === 0 ? (
                    <div className="text-center py-16 bg-white rounded-xl border border-gray-100 shadow-sm">
                        <CheckCircle className="w-12 h-12 text-emerald-400 mx-auto mb-3" />
                        <h3 className="text-lg font-semibold text-gray-700">No conflicts detected!</h3>
                        <p className="text-gray-500 text-sm mt-1">
                            {severityFilter || typeFilter ? 'Try clearing filters' : 'All bookings are free of scheduling conflicts'}
                        </p>
                    </div>
                ) : (
                    <div className="space-y-3">
                        {filteredConflicts.map((conflict) => {
                            const expanded = expandedConflict === conflict.id;
                            const sev = SEVERITY_CONFIG[conflict.severity];
                            const typeConfig = CONFLICT_TYPE_CONFIG[conflict.type];
                            const TypeIcon = typeConfig?.icon ?? AlertTriangle;

                            return (
                                <div
                                    key={conflict.id}
                                    className={cn('bg-white rounded-xl border shadow-sm overflow-hidden', sev.border)}
                                >
                                    {/* Conflict header */}
                                    <div
                                        className={cn('flex items-center gap-3 px-4 py-3 cursor-pointer', sev.bg)}
                                        onClick={() => setExpandedConflict(expanded ? null : conflict.id)}
                                    >
                                        <TypeIcon className={cn('w-4 h-4', sev.text)} />
                                        <div className="flex-1">
                                            <span className={cn('text-sm font-semibold', sev.text)}>
                                                {typeConfig?.label} — {conflict.overlapMinutes}min overlap
                                            </span>
                                            <span className="text-xs text-gray-500 ml-3">
                                                {conflict.bookingA.staffName} · {new Date(conflict.bookingA.startTime).toLocaleDateString()}
                                            </span>
                                        </div>
                                        <span className={cn('px-2 py-0.5 rounded-full text-xs font-bold', sev.badge)}>
                                            {sev.label}
                                        </span>
                                        {expanded ? <ChevronUp className="w-4 h-4 text-gray-400" /> : <ChevronDown className="w-4 h-4 text-gray-400" />}
                                    </div>

                                    {/* Conflict details */}
                                    {expanded && (
                                        <div className="p-4">
                                            {/* Two booking cards */}
                                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-4">
                                                {[conflict.bookingA, conflict.bookingB].map((booking, bi) => (
                                                    <div
                                                        key={bi}
                                                        className={cn(
                                                            'p-3 rounded-xl border text-sm',
                                                            bi === 0 ? 'border-primary-200 bg-primary-50' : 'border-blue-200 bg-blue-50'
                                                        )}
                                                    >
                                                        <p className="font-semibold text-gray-900 mb-1">{booking.clientName}</p>
                                                        <p className="text-gray-600">{booking.serviceName}</p>
                                                        <div className="text-gray-500 mt-2 space-y-0.5 text-xs">
                                                            <div className="flex items-center gap-1">
                                                                <User className="w-3 h-3" /> {booking.staffName}
                                                            </div>
                                                            <div className="flex items-center gap-1">
                                                                <Clock className="w-3 h-3" />
                                                                {new Date(booking.startTime).toLocaleString()} → {new Date(booking.endTime).toLocaleTimeString()}
                                                            </div>
                                                        </div>
                                                        <button
                                                            onClick={() => setResolutionForm({ bookingId: booking.id, type: 'reschedule' })}
                                                            className="mt-3 flex items-center gap-1 text-xs font-medium text-primary-700 hover:text-primary-900"
                                                        >
                                                            <Zap className="w-3 h-3" />
                                                            Resolve this
                                                        </button>
                                                    </div>
                                                ))}
                                            </div>

                                            {/* AI suggestion */}
                                            {conflict.suggestedResolution && (
                                                <div className="flex items-start justify-between gap-3 p-3 bg-primary-50 border border-primary-200 rounded-lg text-sm">
                                                    <div className="flex items-start gap-2">
                                                        <Sparkles className="w-4 h-4 text-primary-600 mt-0.5 flex-shrink-0" />
                                                        <div>
                                                            <p className="font-medium text-primary-800">AI Suggestion</p>
                                                            <p className="text-primary-600 text-xs mt-0.5">
                                                                {conflict.suggestedResolution.action === 'reschedule' ? 'Reschedule' :
                                                                 conflict.suggestedResolution.action === 'cancel' ? 'Cancel' : 'Reassign staff'}
                                                                {' — '}
                                                                {conflict.suggestedResolution.reason}
                                                            </p>
                                                        </div>
                                                    </div>
                                                    <button
                                                        onClick={() => applyAISuggestion(conflict)}
                                                        disabled={resolvingId === conflict.suggestedResolution.targetBookingId}
                                                        className="flex-shrink-0 flex items-center gap-1 px-3 py-1.5 bg-primary-600 text-white rounded-lg text-xs font-medium hover:bg-primary-700 disabled:opacity-50 transition-colors"
                                                    >
                                                        {resolvingId === conflict.suggestedResolution.targetBookingId ? (
                                                            <Loader2 className="w-3 h-3 animate-spin" />
                                                        ) : (
                                                            <Zap className="w-3 h-3" />
                                                        )}
                                                        Apply
                                                    </button>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>
        </div>
    );
}
