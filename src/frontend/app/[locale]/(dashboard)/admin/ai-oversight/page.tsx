'use client';

import React, { useEffect, useState } from 'react';
import {
    Bot,
    RefreshCw,
    DollarSign,
    Zap,
    CheckCircle2,
    AlertCircle,
    Clock,
    TrendingUp,
    ChevronDown,
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';

type Period = 7 | 14 | 30 | 90;

interface AiOverview {
    period: { days: number; since: string };
    summary: {
        totalRequests: number;
        totalTokens: number;
        totalCostUsd: number;
        successRate: number;
        avgLatencyMs: number;
        failedRequests: number;
    };
    byModel: { model: string; requests: number; tokens: number; cost: number; successRate: number }[];
    byFeature: { feature: string; requests: number; tokens: number; cost: number }[];
    topTenants: { tenantId: string; tenantName: string; requests: number; tokens: number; cost: number; failedCount: number }[];
    dailyTrend: { date: string; cost: number; requests: number; tokens: number }[];
}

function StatCard({ icon, label, value, sub, accent = 'indigo' }: {
    icon: React.ReactNode; label: string; value: string | number; sub?: string; accent?: string;
}) {
    return (
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl p-5 flex gap-4 items-start shadow-sm">
            <div className={`p-2.5 rounded-xl bg-${accent}-50 dark:bg-${accent}-500/10`}>{icon}</div>
            <div>
                <p className="text-xs text-slate-500 font-medium uppercase tracking-wide">{label}</p>
                <p className="text-2xl font-bold text-slate-900 dark:text-white mt-0.5">{value}</p>
                {sub && <p className="text-xs text-slate-400 mt-0.5">{sub}</p>}
            </div>
        </div>
    );
}

// Lightweight bar chart rendered with divs — no recharts dependency needed here
function MiniBarChart({ data, valueKey, labelKey }: { data: any[]; valueKey: string; labelKey: string }) {
    const max = Math.max(...data.map((d) => d[valueKey]), 0.001);
    return (
        <div className="space-y-2">
            {data.map((d, i) => (
                <div key={i} className="flex items-center gap-3">
                    <span className="w-28 text-xs text-slate-500 truncate text-right">{d[labelKey]}</span>
                    <div className="flex-1 h-5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                        <div
                            className="h-full bg-primary-500 rounded-full transition-all"
                            style={{ width: `${(d[valueKey] / max) * 100}%` }}
                        />
                    </div>
                    <span className="w-16 text-xs font-semibold text-slate-700 dark:text-slate-200 text-right">
                        {typeof d[valueKey] === 'number' && d[valueKey] < 1
                            ? `$${d[valueKey].toFixed(4)}`
                            : d[valueKey].toLocaleString()}
                    </span>
                </div>
            ))}
        </div>
    );
}

// Sparkline: daily trend as SVG polyline
function SparkTrend({ trend }: { trend: { date: string; cost: number }[] }) {
    if (trend.length < 2) return <p className="text-xs text-slate-400">No trend data</p>;
    const W = 320; const H = 60;
    const max = Math.max(...trend.map((d) => d.cost), 0.001);
    const points = trend.map((d, i) => {
        const x = (i / (trend.length - 1)) * W;
        const y = H - (d.cost / max) * (H - 8);
        return `${x},${y}`;
    }).join(' ');
    return (
        <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-14 text-primary-500">
            <polyline fill="none" stroke="currentColor" strokeWidth="2" points={points} />
            {trend.map((d, i) => {
                const x = (i / (trend.length - 1)) * W;
                const y = H - (d.cost / max) * (H - 8);
                return <circle key={i} cx={x} cy={y} r="3" fill="currentColor" />;
            })}
        </svg>
    );
}

export default function AiOversightPage() {
    const { user, isInitialized } = useAuthStore();
    const router = useRouter();
    const [period, setPeriod] = useState<Period>(30);
    const [data, setData] = useState<AiOverview | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (isInitialized && user?.role !== 'superadmin') router.push('/dashboard');
    }, [user, isInitialized, router]);

    const load = async () => {
        setLoading(true);
        try {
            const res = await api.superAdmin.aiOverview(period);
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

    return (
        <div className="max-w-7xl mx-auto space-y-8 pb-24">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2.5 bg-gradient-to-br from-primary-500 to-primary-600 rounded-2xl shadow-lg shadow-primary-500/20">
                            <Bot className="h-6 w-6 text-white" />
                        </div>
                        <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                            AI Oversight
                        </h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400">Platform-wide AI usage, cost tracking, and failure analysis.</p>
                </div>
                <div className="flex items-center gap-3">
                    {/* Period selector */}
                    <div className="relative">
                        <select
                            value={period}
                            onChange={(e) => setPeriod(Number(e.target.value) as Period)}
                            className="appearance-none pl-3 pr-8 py-2 text-sm border border-slate-200 dark:border-white/10 rounded-xl bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-primary-500"
                        >
                            <option value={7}>Last 7 days</option>
                            <option value={14}>Last 14 days</option>
                            <option value={30}>Last 30 days</option>
                            <option value={90}>Last 90 days</option>
                        </select>
                        <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 pointer-events-none" />
                    </div>
                    <Button onClick={load} variant="outline" size="sm">
                        <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
                        Refresh
                    </Button>
                </div>
            </div>

            {/* KPI Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
                <StatCard icon={<DollarSign className="text-green-500" size={20} />} label="Total AI Cost" value={`$${(s?.totalCostUsd ?? 0).toFixed(4)}`} sub={`Last ${period} days`} accent="green" />
                <StatCard icon={<Zap className="text-primary-500" size={20} />} label="Total Tokens" value={(s?.totalTokens ?? 0).toLocaleString()} sub={`${(s?.totalRequests ?? 0).toLocaleString()} requests`} accent="indigo" />
                <StatCard icon={<CheckCircle2 className="text-emerald-500" size={20} />} label="Success Rate" value={`${s?.successRate ?? 100}%`} sub={`${s?.failedRequests ?? 0} failures`} accent="emerald" />
                <StatCard icon={<Clock className="text-blue-500" size={20} />} label="Avg Latency" value={`${Math.round(s?.avgLatencyMs ?? 0)} ms`} sub="Per AI request" accent="blue" />
                <StatCard icon={<AlertCircle className="text-rose-500" size={20} />} label="Failed Requests" value={s?.failedRequests ?? 0} sub="Errors this period" accent="rose" />
                <StatCard icon={<TrendingUp className="text-primary-500" size={20} />} label="Avg Cost / Request" value={s && s.totalRequests > 0 ? `$${(s.totalCostUsd / s.totalRequests).toFixed(6)}` : '$0'} sub="Per AI call" accent="violet" />
            </div>

            {/* Daily Cost Trend */}
            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl p-6 shadow-sm">
                <h2 className="text-base font-bold text-slate-900 dark:text-white mb-4">Daily Cost Trend</h2>
                {data?.dailyTrend && data.dailyTrend.length > 0 ? (
                    <>
                        <SparkTrend trend={data.dailyTrend} />
                        <div className="flex justify-between text-xs text-slate-400 mt-1">
                            <span>{data.dailyTrend[0]?.date}</span>
                            <span>{data.dailyTrend[data.dailyTrend.length - 1]?.date}</span>
                        </div>
                    </>
                ) : (
                    <p className="text-sm text-slate-400">No usage data for this period.</p>
                )}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* By Model */}
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl p-6 shadow-sm">
                    <h2 className="text-base font-bold text-slate-900 dark:text-white mb-4">Cost by Model</h2>
                    {data?.byModel && data.byModel.length > 0 ? (
                        <MiniBarChart data={data.byModel} valueKey="cost" labelKey="model" />
                    ) : (
                        <p className="text-sm text-slate-400">No data.</p>
                    )}
                </div>

                {/* By Feature */}
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl p-6 shadow-sm">
                    <h2 className="text-base font-bold text-slate-900 dark:text-white mb-4">Cost by Feature</h2>
                    {data?.byFeature && data.byFeature.length > 0 ? (
                        <MiniBarChart data={data.byFeature} valueKey="cost" labelKey="feature" />
                    ) : (
                        <p className="text-sm text-slate-400">No data.</p>
                    )}
                </div>
            </div>

            {/* Top Tenants */}
            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl overflow-hidden shadow-sm">
                <div className="px-6 py-4 border-b border-slate-200 dark:border-white/5">
                    <h2 className="text-base font-bold text-slate-900 dark:text-white">Top Tenants by AI Spend</h2>
                </div>
                {!data?.topTenants || data.topTenants.length === 0 ? (
                    <div className="p-10 text-center text-slate-400">
                        <Bot size={32} className="mx-auto mb-3 opacity-30" />
                        <p className="font-medium">No AI usage recorded yet.</p>
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-slate-50 dark:bg-slate-800/50">
                            <tr>
                                <th className="px-5 py-3 text-left text-xs font-semibold text-slate-500 uppercase">Tenant</th>
                                <th className="px-5 py-3 text-right text-xs font-semibold text-slate-500 uppercase">Requests</th>
                                <th className="px-5 py-3 text-right text-xs font-semibold text-slate-500 uppercase">Tokens</th>
                                <th className="px-5 py-3 text-right text-xs font-semibold text-slate-500 uppercase">Cost</th>
                                <th className="px-5 py-3 text-right text-xs font-semibold text-slate-500 uppercase">Failures</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-white/5">
                            {data.topTenants.map((t) => (
                                <tr key={t.tenantId} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                                    <td className="px-5 py-3.5 font-medium text-slate-900 dark:text-white">{t.tenantName}</td>
                                    <td className="px-5 py-3.5 text-right text-slate-500">{t.requests.toLocaleString()}</td>
                                    <td className="px-5 py-3.5 text-right text-slate-500">{t.tokens.toLocaleString()}</td>
                                    <td className="px-5 py-3.5 text-right font-semibold text-green-600">${t.cost.toFixed(4)}</td>
                                    <td className="px-5 py-3.5 text-right">
                                        {t.failedCount > 0 ? (
                                            <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-600 dark:bg-rose-500/10 dark:text-rose-400">
                                                {t.failedCount}
                                            </span>
                                        ) : (
                                            <span className="text-slate-300 dark:text-slate-600">—</span>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}
