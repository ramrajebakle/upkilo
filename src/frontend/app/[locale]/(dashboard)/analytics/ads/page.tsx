'use client';

import { useState, useEffect } from 'react';
import {
    Megaphone,
    TrendingUp,
    TrendingDown,
    DollarSign,
    Eye,
    MousePointer,
    Users,
    RefreshCw,
    ExternalLink,
    BarChart3,
    Play,
    Pause,
    ChevronDown,
    ArrowLeft,
    Loader2,
    Target,
    Zap,
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { apiClient as api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import Link from 'next/link';
import {
    AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid,
    Tooltip, ResponsiveContainer, Legend
} from 'recharts';

// ─── Types ────────────────────────────────────────────────────────────────────

interface AdCampaign {
    id: string;
    name: string;
    platform: 'google' | 'meta' | 'tiktok' | 'linkedin';
    status: 'active' | 'paused' | 'ended';
    budget: number;
    spend: number;
    impressions: number;
    clicks: number;
    conversions: number;
    ctr: number;
    cpc: number;
    roas: number;
    startDate: string;
    endDate?: string;
}

const PLATFORM_CONFIG = {
    google: { name: 'Google Ads', color: '#4285F4', icon: '🔵' },
    meta: { name: 'Meta Ads', color: '#1877F2', icon: '🔷' },
    tiktok: { name: 'TikTok Ads', color: '#000000', icon: '⬛' },
    linkedin: { name: 'LinkedIn Ads', color: '#0A66C2', icon: '🔹' },
};

const DATE_RANGES = [
    { label: 'Last 7 days', value: '7d' },
    { label: 'Last 30 days', value: '30d' },
    { label: 'Last 90 days', value: '90d' },
    { label: 'This year', value: 'ytd' },
];
// ─── Platform badge ───────────────────────────────────────────────────────────

function PlatformBadge({ platform }: { platform: string }) {
    const cfg = PLATFORM_CONFIG[platform as keyof typeof PLATFORM_CONFIG] ?? { name: platform, color: '#888', icon: '📢' };
    return (
        <span className="inline-flex items-center gap-1 text-xs font-medium text-foreground-secondary">
            <span>{cfg.icon}</span>
            {cfg.name}
        </span>
    );
}

// ─── Metric card ──────────────────────────────────────────────────────────────

function MetricCard({
    label, value, sub, icon: Icon, trend, color = 'violet'
}: {
    label: string; value: string; sub?: string;
    icon: React.ElementType; trend?: number; color?: string;
}) {
    const colorMap: Record<string, string> = {
        violet: 'bg-brand-subtle text-primary',
        emerald: 'bg-emerald-50 text-emerald-600',
        blue: 'bg-blue-50 text-blue-500',
        amber: 'bg-amber-50 text-amber-600',
        red: 'bg-red-50 text-red-500',
    };

    return (
        <div className="bg-card rounded-xl border border-border-subtle p-5 shadow-sm">
            <div className="flex items-center justify-between mb-3">
                <div className={cn('p-2 rounded-lg', colorMap[color] ?? colorMap.violet)}>
                    <Icon className="w-4 h-4" />
                </div>
                {trend !== undefined && (
                    <span className={cn('text-xs font-medium flex items-center gap-0.5',
                        trend >= 0 ? 'text-success-fg' : 'text-danger-fg')}>
                        {trend >= 0 ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
                        {Math.abs(trend)}%
                    </span>
                )}
            </div>
            <p className="text-2xl font-bold text-foreground">{value}</p>
            <p className="text-xs text-foreground-secondary mt-0.5">{label}</p>
            {sub && <p className="text-xs text-foreground-muted mt-0.5">{sub}</p>}
        </div>
    );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function AdPerformancePage() {
    const { error: toastError } = useToast();

    const [campaigns, setCampaigns] = useState<AdCampaign[]>([]);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState<string | null>(null);
    const [dateRange, setDateRange] = useState('30d');
    const [platformFilter, setPlatformFilter] = useState<string | null>(null);
    const [chartData, setChartData] = useState<any[]>([]);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const [campaignsRes, trendRes] = await Promise.all([
                    api.get('/api/v1/adperformance/campaigns', { params: { period: dateRange } }),
                    api.get('/api/v1/adperformance/trend', { params: { period: dateRange } }).catch(() => ({ data: { data: [] } })),
                ]);

                const campaignData = campaignsRes.data?.data || campaignsRes.data || [];
                setCampaigns(campaignData.map((c: any) => ({
                    id: c.id,
                    name: c.name,
                    platform: c.platform || 'google',
                    status: c.status || 'active',
                    budget: c.budget || 0,
                    spend: c.spend || c.totalSpend || 0,
                    impressions: c.impressions || 0,
                    clicks: c.clicks || 0,
                    conversions: c.conversions || 0,
                    ctr: c.ctr || (c.impressions > 0 ? (c.clicks / c.impressions * 100) : 0),
                    cpc: c.cpc || (c.clicks > 0 ? (c.spend / c.clicks) : 0),
                    roas: c.roas || 0,
                    startDate: c.startDate || c.createdAt || '',
                    endDate: c.endDate || undefined,
                })));

                const trendData = trendRes.data?.data || trendRes.data || [];
                setChartData(trendData.map((d: any) => ({
                    date: d.date ? new Date(d.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) : d.label || '',
                    spend: d.spend || 0,
                    conversions: d.conversions || 0,
                    clicks: d.clicks || 0,
                })));
            } catch (err) {
                console.error('Failed to load ad performance:', err);
                setCampaigns([]);
                setChartData([]);
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [dateRange]);

    const handleSync = async (platform: string) => {
        setSyncing(platform);
        try {
            await api.post(`/api/v1/adperformance/sync/${platform}`);
        } catch {
            // ignore — just optimistic UX
        } finally {
            setSyncing(null);
        }
    };

    const handleToggle = async (campaign: AdCampaign) => {
        const newStatus = campaign.status === 'active' ? 'paused' : 'active';
        try {
            await api.put(`/api/v1/adperformance/campaign/${campaign.id}/status?status=${newStatus}`);
            setCampaigns(prev => prev.map(c => c.id === campaign.id ? { ...c, status: newStatus } : c));
        } catch {
            toastError('Failed to update campaign status');
        }
    };

    const filtered = platformFilter
        ? campaigns.filter(c => c.platform === platformFilter)
        : campaigns;

    // Aggregates
    const totalSpend = filtered.reduce((s, c) => s + c.spend, 0);
    const totalImpressions = filtered.reduce((s, c) => s + c.impressions, 0);
    const totalClicks = filtered.reduce((s, c) => s + c.clicks, 0);
    const totalConversions = filtered.reduce((s, c) => s + c.conversions, 0);
    const avgRoas = filtered.length > 0 ? filtered.reduce((s, c) => s + c.roas, 0) / filtered.length : 0;
    const overallCtr = totalImpressions > 0 ? (totalClicks / totalImpressions * 100) : 0;

    return (
        <div className="min-h-screen bg-muted">
            {/* Header */}
            <div className="bg-card border-b border-border-subtle px-6 py-5 sticky top-0 z-10 shadow-sm">
                <div className="max-w-7xl mx-auto flex items-center justify-between gap-4 flex-wrap">
                    <div className="flex items-center gap-3">
                        <Link href="/analytics" className="p-2 hover:bg-accent rounded-lg transition-colors text-foreground-secondary">
                            <ArrowLeft className="w-4 h-4" />
                        </Link>
                        <div>
                            <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                                <Megaphone className="w-5 h-5 text-primary" />
                                Ad Performance
                            </h1>
                            <p className="text-sm text-foreground-secondary">
                                Across {Object.keys(PLATFORM_CONFIG).length} platforms · {campaigns.length} campaigns
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        {/* Date range */}
                        <div className="relative">
                            <select
                                value={dateRange}
                                onChange={e => setDateRange(e.target.value)}
                                className="appearance-none pl-3 pr-8 py-2 border border-border rounded-lg text-sm bg-card focus:outline-none focus:ring-2 focus:ring-primary-400"
                            >
                                {DATE_RANGES.map(r => (
                                    <option key={r.value} value={r.value}>{r.label}</option>
                                ))}
                            </select>
                            <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 w-4 h-4 text-foreground-muted pointer-events-none" />
                        </div>

                        {/* Platform sync buttons */}
                        {Object.entries(PLATFORM_CONFIG).map(([key, cfg]) => (
                            <button
                                key={key}
                                onClick={() => handleSync(key)}
                                disabled={syncing === key}
                                className="flex items-center gap-1.5 px-3 py-2 border border-border text-foreground-secondary rounded-lg text-xs hover:bg-accent transition-colors disabled:opacity-40"
                            >
                                {syncing === key ? (
                                    <Loader2 className="w-3 h-3 animate-spin" />
                                ) : (
                                    <RefreshCw className="w-3 h-3" />
                                )}
                                {cfg.icon} Sync
                            </button>
                        ))}
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
                {/* KPI cards */}
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
                    <MetricCard label="Total Spend" value={formatCurrency(totalSpend)} trend={8.4} icon={DollarSign} color="violet" />
                    <MetricCard label="Impressions" value={totalImpressions.toLocaleString()} trend={12.1} icon={Eye} color="blue" />
                    <MetricCard label="Clicks" value={totalClicks.toLocaleString()} trend={5.7} icon={MousePointer} color="emerald" />
                    <MetricCard label="Conversions" value={totalConversions.toLocaleString()} trend={-2.3} icon={Target} color="amber" />
                    <MetricCard label="Avg ROAS" value={`${avgRoas.toFixed(1)}×`} trend={3.2} icon={TrendingUp} color="emerald" />
                    <MetricCard label="CTR" value={`${overallCtr.toFixed(2)}%`} icon={Zap} color="blue" />
                </div>

                {/* Chart */}
                <div className="bg-card rounded-xl border border-border-subtle shadow-sm p-5">
                    <h2 className="font-semibold text-foreground mb-4">Performance Trend</h2>
                    <ResponsiveContainer width="100%" height={240}>
                        <AreaChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
                            <defs>
                                <linearGradient id="spendGrad" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="#8B5CF6" stopOpacity={0.2} />
                                    <stop offset="95%" stopColor="#8B5CF6" stopOpacity={0} />
                                </linearGradient>
                                <linearGradient id="clicksGrad" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="#10B981" stopOpacity={0.2} />
                                    <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
                                </linearGradient>
                            </defs>
                            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                            <XAxis dataKey="date" tick={{ fontSize: 11 }} tickLine={false} />
                            <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                            <Tooltip contentStyle={{ borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 12 }} />
                            <Legend />
                            <Area type="monotone" dataKey="spend" name="Spend ($)" stroke="#8B5CF6" fill="url(#spendGrad)" strokeWidth={2} dot={false} />
                            <Area type="monotone" dataKey="clicks" name="Clicks" stroke="#10B981" fill="url(#clicksGrad)" strokeWidth={2} dot={false} />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Campaigns table */}
                    <div className="lg:col-span-2">
                        {/* Platform filter tabs */}
                        <div className="flex gap-2 mb-4 flex-wrap">
                            <button
                                onClick={() => setPlatformFilter(null)}
                                className={cn(
                                    'px-3 py-1.5 rounded-full text-sm font-medium border transition-all',
                                    !platformFilter ? 'bg-primary-600 text-white border-primary-600' : 'bg-card text-foreground-secondary border-border'
                                )}
                            >
                                All Platforms
                            </button>
                            {Object.entries(PLATFORM_CONFIG).map(([key, cfg]) => (
                                <button
                                    key={key}
                                    onClick={() => setPlatformFilter(key === platformFilter ? null : key)}
                                    className={cn(
                                        'px-3 py-1.5 rounded-full text-sm font-medium border transition-all',
                                        platformFilter === key
                                            ? 'bg-primary-600 text-white border-primary-600'
                                            : 'bg-card text-foreground-secondary border-border'
                                    )}
                                >
                                    {cfg.icon} {cfg.name}
                                </button>
                            ))}
                        </div>

                        <div className="bg-card rounded-xl border border-border-subtle shadow-sm overflow-hidden">
                            <div className="overflow-x-auto">
                                <table className="w-full text-sm">
                                    <thead>
                                        <tr className="border-b border-gray-50 text-xs text-foreground-muted font-medium">
                                            <th className="text-left px-4 py-3">Campaign</th>
                                            <th className="text-right px-4 py-3">Spend</th>
                                            <th className="text-right px-4 py-3 hidden md:table-cell">Clicks</th>
                                            <th className="text-right px-4 py-3 hidden lg:table-cell">Conv.</th>
                                            <th className="text-right px-4 py-3">ROAS</th>
                                            <th className="text-center px-4 py-3">Status</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-50">
                                        {loading ? (
                                            [...Array(4)].map((_, i) => (
                                                <tr key={i}>
                                                    <td colSpan={6} className="px-4 py-4">
                                                        <div className="h-4 bg-muted rounded animate-pulse" />
                                                    </td>
                                                </tr>
                                            ))
                                        ) : filtered.map(campaign => (
                                            <tr key={campaign.id} className="hover:bg-accent transition-colors">
                                                <td className="px-4 py-3">
                                                    <div>
                                                        <p className="font-medium text-foreground text-sm">{campaign.name}</p>
                                                        <PlatformBadge platform={campaign.platform} />
                                                    </div>
                                                </td>
                                                <td className="px-4 py-3 text-right">
                                                    <p className="font-medium text-foreground">{formatCurrency(campaign.spend)}</p>
                                                    <p className="text-xs text-foreground-muted">of {formatCurrency(campaign.budget)}</p>
                                                </td>
                                                <td className="px-4 py-3 text-right hidden md:table-cell text-foreground">
                                                    {campaign.clicks.toLocaleString()}
                                                    <span className="block text-xs text-foreground-muted">
                                                        {campaign.ctr.toFixed(1)}% CTR
                                                    </span>
                                                </td>
                                                <td className="px-4 py-3 text-right hidden lg:table-cell text-foreground">
                                                    {campaign.conversions}
                                                </td>
                                                <td className="px-4 py-3 text-right">
                                                    <span className={cn(
                                                        'font-bold',
                                                        campaign.roas >= 4 ? 'text-success-fg' :
                                                        campaign.roas >= 2 ? 'text-warning-fg' : 'text-danger-fg'
                                                    )}>
                                                        {campaign.roas.toFixed(1)}×
                                                    </span>
                                                </td>
                                                <td className="px-4 py-3 text-center">
                                                    <button
                                                        onClick={() => handleToggle(campaign)}
                                                        className={cn(
                                                            'inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium transition-all',
                                                            campaign.status === 'active'
                                                                ? 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                                                                : campaign.status === 'paused'
                                                                ? 'bg-amber-50 text-amber-700 hover:bg-amber-100'
                                                                : 'bg-muted text-foreground-secondary'
                                                        )}
                                                    >
                                                        {campaign.status === 'active' ? (
                                                            <><Play className="w-3 h-3" /> Active</>
                                                        ) : campaign.status === 'paused' ? (
                                                            <><Pause className="w-3 h-3" /> Paused</>
                                                        ) : (
                                                            'Ended'
                                                        )}
                                                    </button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>

                    {/* Platform breakdown sidebar */}
                    <div className="space-y-4">
                        <h3 className="font-semibold text-foreground">By Platform</h3>

                        {Object.entries(PLATFORM_CONFIG).map(([key, cfg]) => {
                            const platformCampaigns = campaigns.filter(c => c.platform === key);
                            const spend = platformCampaigns.reduce((s, c) => s + c.spend, 0);
                            const conv = platformCampaigns.reduce((s, c) => s + c.conversions, 0);
                            const roas = platformCampaigns.length > 0
                                ? platformCampaigns.reduce((s, c) => s + c.roas, 0) / platformCampaigns.length : 0;

                            return (
                                <div key={key} className="bg-card rounded-xl border border-border-subtle shadow-sm p-4">
                                    <div className="flex items-center justify-between mb-3">
                                        <div className="flex items-center gap-2">
                                            <span className="text-xl">{cfg.icon}</span>
                                            <span className="font-medium text-foreground text-sm">{cfg.name}</span>
                                        </div>
                                        <span className="text-xs text-foreground-muted">{platformCampaigns.length} campaigns</span>
                                    </div>
                                    <div className="grid grid-cols-3 gap-2 text-sm">
                                        <div>
                                            <p className="text-foreground-muted text-xs">Spend</p>
                                            <p className="font-bold text-foreground">{formatCurrency(spend)}</p>
                                        </div>
                                        <div>
                                            <p className="text-foreground-muted text-xs">Conv.</p>
                                            <p className="font-bold text-foreground">{conv}</p>
                                        </div>
                                        <div>
                                            <p className="text-foreground-muted text-xs">ROAS</p>
                                            <p className={cn('font-bold', roas >= 3 ? 'text-success-fg' : roas >= 1.5 ? 'text-warning-fg' : 'text-danger-fg')}>
                                                {roas > 0 ? `${roas.toFixed(1)}×` : '—'}
                                            </p>
                                        </div>
                                    </div>
                                    {/* Budget bar */}
                                    {platformCampaigns.length > 0 && (
                                        <div className="mt-3">
                                            <div className="flex justify-between text-xs text-foreground-muted mb-1">
                                                <span>Budget utilization</span>
                                                <span>{Math.round(spend / platformCampaigns.reduce((s, c) => s + c.budget, 0) * 100)}%</span>
                                            </div>
                                            <div className="h-1.5 bg-muted rounded-full">
                                                <div
                                                    className="h-1.5 rounded-full transition-all"
                                                    style={{
                                                        width: `${Math.min(100, spend / platformCampaigns.reduce((s, c) => s + c.budget, 0) * 100)}%`,
                                                        backgroundColor: cfg.color
                                                    }}
                                                />
                                            </div>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
}
