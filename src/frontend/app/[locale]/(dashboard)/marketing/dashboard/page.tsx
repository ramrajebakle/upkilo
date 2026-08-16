'use client';

import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import {
    Send,
    Mail,
    MessageSquare,
    TrendingUp,
    Users,
    DollarSign,
    MousePointerClick,
    Eye,
    UserX,
    Gift,
    Zap,
    RefreshCw,
    ArrowRight,
    AlertCircle,
    CheckCircle,
    BarChart3,
} from 'lucide-react';
import Link from 'next/link';
import { apiClient } from '@/lib/api';

interface BroadcastStats {
    campaignsByStatus: { Status: string; Count: number }[];
    totalSent: number;
    totalDelivered: number;
    totalOpened: number;
    totalClicked: number;
    totalUnsubscribed: number;
    totalRevenue: number;
    openRate: number;
    clickRate: number;
    deliveryRate: number;
}

interface WinBackStats {
    lapsedDays: number;
    lapsedClientCount: number;
    avgLifetimeValue: number;
    estimatedRecoveryRevenue: number;
    message: string;
}

interface Segment {
    id: string;
    name: string;
    description: string;
    count: number;
}

interface Campaign {
    id: string;
    name: string;
    channel: string;
    status: string;
    sentAt?: string;
    sentCount: number;
    opened: number;
    clicked: number;
    revenue: number;
}

const STATUS_COLOR: Record<string, string> = {
    sent:      'bg-green-100 text-green-700',
    sending:   'bg-blue-100 text-blue-700',
    scheduled: 'bg-yellow-100 text-yellow-700',
    draft:     'bg-slate-100 text-slate-600',
    cancelled: 'bg-red-100 text-red-600',
};

function StatCard({ icon, label, value, sub, color = 'indigo' }: {
    icon: React.ReactNode;
    label: string;
    value: string | number;
    sub?: string;
    color?: string;
}) {
    return (
        <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            className="bg-white rounded-2xl border border-surface-200 p-5 flex gap-4 items-start shadow-sm"
        >
            <div className={`p-2.5 rounded-xl bg-${color}-50`}>{icon}</div>
            <div>
                <p className="text-xs text-text-tertiary font-medium uppercase tracking-wide">{label}</p>
                <p className="text-2xl font-bold text-text-primary mt-0.5">{value}</p>
                {sub && <p className="text-xs text-text-tertiary mt-0.5">{sub}</p>}
            </div>
        </motion.div>
    );
}

export default function MarketingDashboardPage() {
    const [broadcastStats, setBroadcastStats] = useState<BroadcastStats | null>(null);
    const [winBack, setWinBack]               = useState<WinBackStats | null>(null);
    const [segments, setSegments]             = useState<Segment[]>([]);
    const [recent, setRecent]                 = useState<Campaign[]>([]);
    const [loading, setLoading]               = useState(true);

    const load = async () => {
        setLoading(true);
        try {
            const [statsRes, segRes, winRes, campRes] = await Promise.allSettled([
                apiClient.get('/api/v1/broadcast/stats'),
                apiClient.get('/api/v1/broadcast/segments'),
                apiClient.get('/api/v1/proactive-messaging/win-back/stats'),
                apiClient.get('/api/v1/broadcast/campaigns?pageSize=5'),
            ]);

            if (statsRes.status === 'fulfilled') setBroadcastStats(statsRes.value.data?.data ?? statsRes.value.data);
            if (segRes.status   === 'fulfilled') setSegments(segRes.value.data?.data?.segments ?? []);
            if (winRes.status   === 'fulfilled') setWinBack(winRes.value.data?.data);
            if (campRes.status  === 'fulfilled') setRecent(campRes.value.data?.data?.data ?? []);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, []);

    const s = broadcastStats;

    return (
        <div className="max-w-7xl mx-auto space-y-8 pb-24">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
                        <BarChart3 className="text-primary-500" size={28} />
                        Marketing Dashboard
                    </h1>
                    <p className="text-text-secondary mt-1">Your complete marketing health at a glance.</p>
                </div>
                <div className="flex gap-3">
                    <button onClick={load} className="p-2 rounded-xl border border-surface-200 text-text-tertiary hover:text-text-primary hover:bg-surface-100 transition-colors">
                        <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
                    </button>
                    <Link href="/campaigns/broadcast" className="btn btn-primary flex items-center gap-2">
                        <Send size={15} />
                        New Campaign
                    </Link>
                </div>
            </div>

            {/* Win-Back Opportunity Banner */}
            {winBack && winBack.lapsedClientCount > 0 && (
                <motion.div
                    initial={{ opacity: 0, scale: 0.98 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200 rounded-2xl p-5 flex items-center justify-between gap-4"
                >
                    <div className="flex items-center gap-3">
                        <div className="p-2.5 bg-amber-100 rounded-xl">
                            <UserX className="text-amber-600" size={20} />
                        </div>
                        <div>
                            <p className="font-semibold text-amber-900">Win-Back Opportunity</p>
                            <p className="text-sm text-amber-700 mt-0.5">{winBack.message}</p>
                        </div>
                    </div>
                    <Link
                        href="/campaigns/broadcast?segment=win_back"
                        className="shrink-0 flex items-center gap-1.5 text-sm font-semibold text-amber-700 bg-amber-100 hover:bg-amber-200 px-4 py-2 rounded-xl transition-colors"
                    >
                        Launch Win-Back <ArrowRight size={14} />
                    </Link>
                </motion.div>
            )}

            {/* Key Metrics */}
            <section>
                <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wide mb-4">Campaign Performance</h2>
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                    <StatCard icon={<Send className="text-primary-500" size={20} />}   label="Total Sent"      value={s?.totalSent?.toLocaleString() ?? '—'} sub={`${s?.deliveryRate ?? 0}% delivery rate`} color="indigo" />
                    <StatCard icon={<Eye className="text-blue-500" size={20} />}      label="Emails Opened"  value={s?.totalOpened?.toLocaleString() ?? '—'} sub={`${s?.openRate ?? 0}% open rate`} color="blue" />
                    <StatCard icon={<MousePointerClick className="text-primary-500" size={20} />} label="Clicks" value={s?.totalClicked?.toLocaleString() ?? '—'} sub={`${s?.clickRate ?? 0}% click rate`} color="purple" />
                    <StatCard icon={<DollarSign className="text-green-500" size={20} />} label="Revenue Attributed" value={`$${(s?.totalRevenue ?? 0).toLocaleString()}`} sub="From tracked campaigns" color="green" />
                </div>
            </section>

            {/* Smart Audience Segments */}
            <section>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wide">Smart Audience Segments</h2>
                    <Link href="/campaigns/broadcast" className="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1">
                        Create Campaign <ArrowRight size={13} />
                    </Link>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                    {segments.map((seg) => (
                        <Link
                            key={seg.id}
                            href={`/campaigns/broadcast?segment=${seg.id}`}
                            className="group bg-white border border-surface-200 rounded-2xl p-4 hover:border-primary-300 hover:shadow-md transition-all"
                        >
                            <div className="flex items-start justify-between">
                                <div>
                                    <p className="font-semibold text-text-primary text-sm">{seg.name}</p>
                                    <p className="text-xs text-text-tertiary mt-1 leading-relaxed">{seg.description}</p>
                                </div>
                                <span className="shrink-0 ml-3 text-lg font-bold text-primary-600">{seg.count.toLocaleString()}</span>
                            </div>
                            <div className="mt-3 flex items-center gap-1 text-xs text-primary-600 opacity-0 group-hover:opacity-100 transition-opacity font-medium">
                                Target this segment <ArrowRight size={11} />
                            </div>
                        </Link>
                    ))}
                </div>
            </section>

            {/* Recent Campaigns */}
            <section>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wide">Recent Campaigns</h2>
                    <Link href="/campaigns/broadcast" className="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1">
                        View all <ArrowRight size={13} />
                    </Link>
                </div>
                <div className="bg-white border border-surface-200 rounded-2xl overflow-hidden">
                    {recent.length === 0 ? (
                        <div className="p-10 text-center text-text-tertiary">
                            <Send size={32} className="mx-auto mb-3 opacity-30" />
                            <p className="font-medium">No campaigns yet</p>
                            <p className="text-sm mt-1">Create your first broadcast to reach your clients.</p>
                        </div>
                    ) : (
                        <table className="w-full text-sm">
                            <thead className="bg-surface-50 border-b border-surface-200">
                                <tr>
                                    <th className="px-5 py-3 text-left text-xs font-semibold text-text-tertiary uppercase">Campaign</th>
                                    <th className="px-5 py-3 text-left text-xs font-semibold text-text-tertiary uppercase">Channel</th>
                                    <th className="px-5 py-3 text-right text-xs font-semibold text-text-tertiary uppercase">Sent</th>
                                    <th className="px-5 py-3 text-right text-xs font-semibold text-text-tertiary uppercase">Opened</th>
                                    <th className="px-5 py-3 text-right text-xs font-semibold text-text-tertiary uppercase">Clicked</th>
                                    <th className="px-5 py-3 text-right text-xs font-semibold text-text-tertiary uppercase">Revenue</th>
                                    <th className="px-5 py-3 text-left text-xs font-semibold text-text-tertiary uppercase">Status</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-surface-100">
                                {recent.map((c) => (
                                    <tr key={c.id} className="hover:bg-surface-50 transition-colors">
                                        <td className="px-5 py-3.5 font-medium text-text-primary">{c.name}</td>
                                        <td className="px-5 py-3.5">
                                            <span className="flex items-center gap-1.5 text-text-secondary">
                                                {c.channel === 'email' ? <Mail size={13} /> : <MessageSquare size={13} />}
                                                {c.channel === 'email' ? 'Email' : 'SMS'}
                                            </span>
                                        </td>
                                        <td className="px-5 py-3.5 text-right text-text-secondary">{c.sentCount.toLocaleString()}</td>
                                        <td className="px-5 py-3.5 text-right text-text-secondary">{c.opened.toLocaleString()}</td>
                                        <td className="px-5 py-3.5 text-right text-text-secondary">{c.clicked.toLocaleString()}</td>
                                        <td className="px-5 py-3.5 text-right font-semibold text-green-600">${c.revenue.toFixed(0)}</td>
                                        <td className="px-5 py-3.5">
                                            <span className={`px-2 py-1 rounded-full text-xs font-semibold ${STATUS_COLOR[c.status] ?? STATUS_COLOR.draft}`}>
                                                {c.status}
                                            </span>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            </section>

            {/* Quick Actions */}
            <section>
                <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wide mb-4">Quick Actions</h2>
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                    {[
                        { href: '/campaigns/broadcast',           icon: <Send size={18} />,          label: 'Email Broadcast',       desc: 'Send to your audience',     color: 'indigo' },
                        { href: '/marketing/sms-templates',       icon: <MessageSquare size={18} />, label: 'SMS Templates',         desc: 'Reusable SMS messages',     color: 'blue' },
                        { href: '/marketing/automation',          icon: <Zap size={18} />,           label: 'AI Automation',         desc: 'Autonomous campaigns',      color: 'purple' },
                        { href: '/marketing/landing-pages',       icon: <TrendingUp size={18} />,    label: 'Landing Pages',         desc: 'Convert more visitors',     color: 'green' },
                    ].map((a) => (
                        <Link
                            key={a.href}
                            href={a.href}
                            className={`group bg-white border border-surface-200 rounded-2xl p-4 hover:border-${a.color}-300 hover:shadow-md transition-all`}
                        >
                            <div className={`p-2 rounded-xl bg-${a.color}-50 w-fit mb-3`}>
                                <span className={`text-${a.color}-500`}>{a.icon}</span>
                            </div>
                            <p className="font-semibold text-text-primary text-sm">{a.label}</p>
                            <p className="text-xs text-text-tertiary mt-0.5">{a.desc}</p>
                        </Link>
                    ))}
                </div>
            </section>
        </div>
    );
}
