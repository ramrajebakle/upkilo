'use client';

import React, { useEffect, useState } from 'react';
import {
    ShieldAlert,
    RefreshCw,
    AlertTriangle,
    AlertCircle,
    Info,
    Lock,
    LogIn,
    ChevronDown,
    CheckCircle,
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';

type Period = 1 | 7 | 14 | 30;

const SEVERITY_CONFIG: Record<string, { icon: React.ElementType; bg: string; text: string; border: string }> = {
    Critical: { icon: AlertCircle,   bg: 'bg-rose-50 dark:bg-rose-500/10',    text: 'text-rose-600 dark:text-rose-400',    border: 'border-rose-200 dark:border-rose-500/30' },
    High:     { icon: AlertTriangle, bg: 'bg-orange-50 dark:bg-orange-500/10', text: 'text-orange-600 dark:text-orange-400', border: 'border-orange-200 dark:border-orange-500/30' },
    Warning:  { icon: AlertTriangle, bg: 'bg-amber-50 dark:bg-amber-500/10',   text: 'text-amber-600 dark:text-amber-400',  border: 'border-amber-200 dark:border-amber-500/30' },
    Info:     { icon: Info,          bg: 'bg-blue-50 dark:bg-blue-500/10',     text: 'text-blue-600 dark:text-blue-400',    border: 'border-blue-200 dark:border-blue-500/30' },
};

interface SecurityOverview {
    period: { days: number; since: string };
    summary: {
        totalEvents: number;
        unresolvedCount: number;
        criticalCount: number;
        highCount: number;
        loginFailureRate: number;
        loginFailures: number;
        loginSuccesses: number;
    };
    bySeverity: { severity: string; count: number }[];
    unresolvedCritical: {
        id: string;
        severity: string;
        eventType: string;
        description?: string;
        tenantId?: string;
        ipAddress?: string;
        occurredAt: string;
    }[];
    targetedTenants: { tenantId: string; tenantName: string; failures: number }[];
}

function SeverityCard({ severity, count, isLoading }: { severity: string; count: number; isLoading: boolean }) {
    const cfg = SEVERITY_CONFIG[severity] ?? SEVERITY_CONFIG.Info;
    const Icon = cfg.icon;
    return (
        <div className={`rounded-2xl border p-5 flex gap-4 items-start shadow-sm ${cfg.bg} ${cfg.border}`}>
            <div className={`p-2.5 rounded-xl bg-white/60 dark:bg-black/20`}>
                <Icon className={`${cfg.text}`} size={20} />
            </div>
            <div>
                <p className={`text-xs font-semibold uppercase tracking-wide ${cfg.text}`}>{severity}</p>
                <p className={`text-3xl font-bold mt-0.5 ${cfg.text}`}>{isLoading ? '…' : count}</p>
                <p className="text-xs opacity-70 mt-0.5">events this period</p>
            </div>
        </div>
    );
}

function formatEventType(t: string) {
    return t.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
}

export default function SecurityOverviewPage() {
    const { user, isInitialized } = useAuthStore();
    const router = useRouter();
    const [period, setPeriod] = useState<Period>(7);
    const [data, setData] = useState<SecurityOverview | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (isInitialized && user?.role !== 'superadmin') router.push('/dashboard');
    }, [user, isInitialized, router]);

    const load = async () => {
        setLoading(true);
        try {
            const res = await api.superAdmin.securityOverview(period);
            setData(res.data);
        } catch (e) {
            console.error(e);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { if (user?.role === 'superadmin') load(); }, [user, period]);

    if (user?.role !== 'superadmin') return null;

    const s = data?.summary;
    const severityOrder = ['Critical', 'High', 'Warning', 'Info'];
    const bySeverityMap = Object.fromEntries((data?.bySeverity ?? []).map((x) => [x.severity, x.count]));

    return (
        <div className="max-w-7xl mx-auto space-y-8 pb-24">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2.5 bg-gradient-to-br from-rose-500 to-orange-500 rounded-2xl shadow-lg shadow-rose-500/20">
                            <ShieldAlert className="h-6 w-6 text-white" />
                        </div>
                        <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                            Security Overview
                        </h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400">Monitor authentication events, anomalies, and unresolved threats.</p>
                </div>
                <div className="flex items-center gap-3">
                    <div className="relative">
                        <select
                            value={period}
                            onChange={(e) => setPeriod(Number(e.target.value) as Period)}
                            className="appearance-none pl-3 pr-8 py-2 text-sm border border-slate-200 dark:border-white/10 rounded-xl bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-rose-500"
                        >
                            <option value={1}>Last 24 hours</option>
                            <option value={7}>Last 7 days</option>
                            <option value={14}>Last 14 days</option>
                            <option value={30}>Last 30 days</option>
                        </select>
                        <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted pointer-events-none" />
                    </div>
                    <Button onClick={load} variant="outline" size="sm">
                        <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
                        Refresh
                    </Button>
                </div>
            </div>

            {/* Severity cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {severityOrder.map((sev) => (
                    <SeverityCard key={sev} severity={sev} count={bySeverityMap[sev] ?? 0} isLoading={loading} />
                ))}
            </div>

            {/* Summary stats row */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                    { icon: <AlertCircle className="text-danger-fg" size={18} />, label: 'Unresolved', value: s?.unresolvedCount ?? 0, sub: 'Need attention' },
                    { icon: <LogIn className="text-blue-500" size={18} />, label: 'Login Successes', value: s?.loginSuccesses ?? 0, sub: 'This period' },
                    { icon: <Lock className="text-orange-500" size={18} />, label: 'Login Failures', value: s?.loginFailures ?? 0, sub: `${s?.loginFailureRate ?? 0}% failure rate` },
                    { icon: <Info className="text-foreground-secondary" size={18} />, label: 'Total Events', value: s?.totalEvents ?? 0, sub: `Last ${period}d` },
                ].map((c) => (
                    <div key={c.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl p-4 flex gap-3 items-start shadow-sm">
                        <div className="p-2 rounded-xl bg-slate-50 dark:bg-slate-800">{c.icon}</div>
                        <div>
                            <p className="text-xs text-foreground-secondary uppercase tracking-wide font-medium">{c.label}</p>
                            <p className="text-xl font-bold text-slate-900 dark:text-white">{loading ? '…' : c.value.toLocaleString()}</p>
                            <p className="text-xs text-foreground-muted mt-0.5">{c.sub}</p>
                        </div>
                    </div>
                ))}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Unresolved Critical / High events */}
                <div className="lg:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl overflow-hidden shadow-sm">
                    <div className="px-6 py-4 border-b border-slate-200 dark:border-white/5 flex items-center gap-2">
                        <AlertCircle className="text-danger-fg" size={16} />
                        <h2 className="text-base font-bold text-slate-900 dark:text-white">Unresolved Critical & High Events</h2>
                    </div>
                    {!data?.unresolvedCritical || data.unresolvedCritical.length === 0 ? (
                        <div className="p-10 text-center text-foreground-muted">
                            <CheckCircle size={32} className="mx-auto mb-3 text-emerald-400" />
                            <p className="font-medium text-emerald-600 dark:text-emerald-400">No unresolved critical events</p>
                            <p className="text-sm mt-1">Platform is operating normally.</p>
                        </div>
                    ) : (
                        <div className="divide-y divide-slate-100 dark:divide-white/5">
                            {data.unresolvedCritical.map((evt) => {
                                const cfg = SEVERITY_CONFIG[evt.severity] ?? SEVERITY_CONFIG.Info;
                                const Icon = cfg.icon;
                                return (
                                    <div key={evt.id} className="px-5 py-4 flex items-start gap-4 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                                        <div className={`mt-0.5 p-1.5 rounded-lg ${cfg.bg}`}>
                                            <Icon className={cfg.text} size={14} />
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2 flex-wrap">
                                                <span className="font-semibold text-sm text-slate-900 dark:text-white">{formatEventType(evt.eventType)}</span>
                                                <span className={`px-1.5 py-0.5 rounded text-xs font-semibold ${cfg.bg} ${cfg.text}`}>{evt.severity}</span>
                                            </div>
                                            {evt.description && <p className="text-xs text-foreground-secondary mt-0.5 truncate">{evt.description}</p>}
                                            <div className="flex items-center gap-3 mt-1 text-xs text-foreground-muted">
                                                {evt.ipAddress && <span>IP: {evt.ipAddress}</span>}
                                                <span>{new Date(evt.occurredAt).toLocaleString()}</span>
                                            </div>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>

                {/* Most targeted tenants */}
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl overflow-hidden shadow-sm">
                    <div className="px-6 py-4 border-b border-slate-200 dark:border-white/5 flex items-center gap-2">
                        <Lock className="text-orange-500" size={16} />
                        <h2 className="text-base font-bold text-slate-900 dark:text-white">Most Targeted Tenants</h2>
                    </div>
                    {!data?.targetedTenants || data.targetedTenants.length === 0 ? (
                        <div className="p-8 text-center text-foreground-muted">
                            <p className="text-sm">No login attack patterns detected.</p>
                        </div>
                    ) : (
                        <div className="divide-y divide-slate-100 dark:divide-white/5">
                            {data.targetedTenants.map((t, i) => (
                                <div key={t.tenantId} className="px-5 py-3.5 flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <span className="text-xs font-bold text-foreground-muted w-4">#{i + 1}</span>
                                        <span className="font-medium text-sm text-slate-900 dark:text-white">{t.tenantName}</span>
                                    </div>
                                    <span className="px-2 py-0.5 rounded-full text-xs font-bold bg-orange-100 text-orange-600 dark:bg-orange-500/10 dark:text-orange-400">
                                        {t.failures} failures
                                    </span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
