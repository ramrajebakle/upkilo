'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Plus,
    Search,
    Filter,
    MoreVertical,
    Mail,
    MessageSquare,
    Megaphone,
    TrendingUp,
    Users,
    Clock,
    CheckCircle,
    XCircle,
    Play,
    Pause,
    ChevronRight,
    Zap,
    Target,
    BarChart3,
    Send,
    Bot,
    DollarSign,
    MousePointerClick,
    Eye,
} from 'lucide-react';
import api from '@/lib/api';
import { useAuthStore } from '@/store/authStore';
import { cn } from '@/lib/utils';
import { SkeletonCard } from '@/components/ui';

interface Campaign {
    id: string;
    name: string;
    type: 'email' | 'sms' | 'push';
    status: 'active' | 'scheduled' | 'completed' | 'draft' | 'sending' | 'sent' | 'cancelled';
    audience: number;
    sent: number;
    opened: number;
    clicked: number;
    scheduledFor?: string;
    createdAt: string;
}

interface AutoResponder {
    id: string;
    name: string;
    triggerEvent: string;
    subject: string | null;
    isActive: boolean;
    delayMinutes: number;
}

interface PerformanceAggregate {
    campaignCount: number;
    totalSent: number;
    totalOpened: number;
    totalClicked: number;
    totalRevenue: number;
    openRate: number;
    clickRate: number;
}

export default function CampaignsPage() {
    const { user } = useAuthStore();
    const [campaigns, setCampaigns] = useState<Campaign[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState<string>('all');
    const [activeTab, setActiveTab] = useState<'campaigns' | 'auto-responders'>('campaigns');
    const [autoResponders, setAutoResponders] = useState<AutoResponder[]>([]);
    const [aggregate, setAggregate] = useState<PerformanceAggregate | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const [campaignsRes, aggregateRes, respondersRes] = await Promise.all([
                    api.campaigns.list({ pageSize: 100 }),
                    api.campaigns.performanceAggregate().catch(() => ({ data: null })),
                    api.campaigns.autoResponders().catch(() => ({ data: { data: [] } })),
                ]);

                const backendCampaigns = campaignsRes.data.data || [];

                // Fetch analytics for each campaign
                const campaignsWithStats = await Promise.all(
                    backendCampaigns.map(async (c: any) => {
                        try {
                            const statsRes = await api.campaigns.analytics(c.id);
                            const stats = statsRes.data;
                            return {
                                ...c,
                                audience: stats.sentCount,
                                sent: stats.sentCount,
                                opened: stats.openedCount,
                                clicked: stats.clickedCount
                            };
                        } catch {
                            return { ...c, audience: 0, sent: 0, opened: 0, clicked: 0 };
                        }
                    })
                );

                setCampaigns(campaignsWithStats);
                if (aggregateRes.data) setAggregate(aggregateRes.data);
                setAutoResponders(respondersRes.data.data || []);
            } catch (error) {
                console.error('Failed to fetch campaigns', error);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, []);

    const filteredCampaigns = campaigns.filter(campaign => {
        const matchesSearch = campaign.name.toLowerCase().includes(searchQuery.toLowerCase());
        const matchesStatus = statusFilter === 'all' || campaign.status === statusFilter;
        return matchesSearch && matchesStatus;
    });

    // Stats
    const activeCampaigns = campaigns.filter(c => c.status === 'active').length;
    const totalSent = campaigns.reduce((sum, c) => sum + c.sent, 0);
    const totalOpened = campaigns.reduce((sum, c) => sum + c.opened, 0);
    const avgOpenRate = totalSent > 0 ? Math.round((totalOpened / totalSent) * 100) : 0;

    const getTypeIcon = (type: string) => {
        switch (type) {
            case 'email': return Mail;
            case 'sms': return MessageSquare;
            default: return Megaphone;
        }
    };

    const getStatusStyles = (status: string) => {
        switch (status) {
            case 'active': return { bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500' };
            case 'scheduled': return { bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500' };
            case 'completed': return { bg: 'bg-slate-100', text: 'text-slate-600', dot: 'bg-slate-400' };
            case 'draft': return { bg: 'bg-amber-50', text: 'text-amber-700', dot: 'bg-amber-500' };
            default: return { bg: 'bg-slate-100', text: 'text-slate-600', dot: 'bg-slate-400' };
        }
    };

    const getTypeColor = (type: string) => {
        switch (type) {
            case 'email': return 'from-blue-500 to-cyan-600';
            case 'sms': return 'from-emerald-500 to-teal-600';
            default: return 'from-violet-500 to-purple-600';
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    };

    const formatCurrency = (value: number) =>
        new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 0 }).format(value);

    const triggerLabels: Record<string, string> = {
        'client.created': 'New Client',
        'booking.confirmed': 'Booking Confirmed',
        'booking.completed': 'Service Completed',
        'client.birthday': 'Birthday',
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div className="animate-fade-in-up">
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2 bg-gradient-to-br from-rose-500 to-pink-600 rounded-xl shadow-lg shadow-rose-500/25">
                            <Megaphone className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl lg:text-3xl font-bold text-slate-900"
                            style={{ fontFamily: 'Outfit, sans-serif' }}
                        >
                            Marketing Campaigns
                        </h1>
                    </div>
                    <p className="text-slate-500">Create and manage email, SMS, and push campaigns</p>
                </div>
                <Link
                    href="/campaigns/new"
                    className="btn btn-primary shadow-lg shadow-primary-500/25 animate-fade-in"
                    style={{ animationDelay: '100ms' }}
                >
                    <Plus className="h-5 w-5" />
                    Create Campaign
                </Link>
            </div>

            {/* Performance Aggregate Banner */}
            {aggregate && (
                <div className="bg-gradient-to-br from-slate-900 to-slate-800 rounded-2xl p-6 text-white">
                    <div className="flex items-center gap-2 mb-4">
                        <BarChart3 className="h-5 w-5 text-cyan-400" />
                        <h3 className="font-semibold">Campaign Performance (Last 30 Days)</h3>
                    </div>
                    <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                        <div>
                            <p className="text-slate-400 text-xs uppercase tracking-wide">Campaigns</p>
                            <p className="text-2xl font-bold mt-1">{aggregate.campaignCount}</p>
                        </div>
                        <div>
                            <p className="text-slate-400 text-xs uppercase tracking-wide flex items-center gap-1"><Send className="h-3 w-3" /> Sent</p>
                            <p className="text-2xl font-bold mt-1">{aggregate.totalSent.toLocaleString()}</p>
                        </div>
                        <div>
                            <p className="text-slate-400 text-xs uppercase tracking-wide flex items-center gap-1"><Eye className="h-3 w-3" /> Open Rate</p>
                            <p className="text-2xl font-bold mt-1 text-emerald-400">{aggregate.openRate}%</p>
                        </div>
                        <div>
                            <p className="text-slate-400 text-xs uppercase tracking-wide flex items-center gap-1"><MousePointerClick className="h-3 w-3" /> Click Rate</p>
                            <p className="text-2xl font-bold mt-1 text-cyan-400">{aggregate.clickRate}%</p>
                        </div>
                        <div>
                            <p className="text-slate-400 text-xs uppercase tracking-wide flex items-center gap-1"><DollarSign className="h-3 w-3" /> Revenue</p>
                            <p className="text-2xl font-bold mt-1 text-amber-400">{formatCurrency(aggregate.totalRevenue)}</p>
                        </div>
                    </div>
                </div>
            )}

            {/* Tabs */}
            <div className="flex gap-1 bg-slate-100 p-1 rounded-xl w-fit">
                <button
                    onClick={() => setActiveTab('campaigns')}
                    className={cn(
                        'px-5 py-2 rounded-lg text-sm font-medium transition-all',
                        activeTab === 'campaigns'
                            ? 'bg-white text-slate-900 shadow-sm'
                            : 'text-slate-500 hover:text-slate-700'
                    )}
                >
                    <div className="flex items-center gap-2">
                        <Megaphone className="h-4 w-4" />
                        Campaigns
                    </div>
                </button>
                <button
                    onClick={() => setActiveTab('auto-responders')}
                    className={cn(
                        'px-5 py-2 rounded-lg text-sm font-medium transition-all',
                        activeTab === 'auto-responders'
                            ? 'bg-white text-slate-900 shadow-sm'
                            : 'text-slate-500 hover:text-slate-700'
                    )}
                >
                    <div className="flex items-center gap-2">
                        <Bot className="h-4 w-4" />
                        Auto-Responders ({autoResponders.length})
                    </div>
                </button>
            </div>

            {/* Auto-Responders Tab */}
            {activeTab === 'auto-responders' && (
                <div className="space-y-4">
                    {autoResponders.length === 0 ? (
                        <div className="card-elevated py-16 text-center">
                            <Bot className="h-12 w-12 text-slate-300 mx-auto mb-4" />
                            <h3 className="text-lg font-semibold text-slate-900 mb-2">No auto-responders yet</h3>
                            <p className="text-slate-500 mb-4">Set up automated messages for events like new clients or completed bookings</p>
                        </div>
                    ) : (
                        autoResponders.map((ar) => (
                            <div key={ar.id} className="card-elevated p-5 flex items-center justify-between gap-4">
                                <div className="flex items-center gap-4">
                                    <div className={cn(
                                        'w-10 h-10 rounded-xl flex items-center justify-center',
                                        ar.isActive
                                            ? 'bg-gradient-to-br from-emerald-400 to-emerald-600 text-white'
                                            : 'bg-slate-100 text-slate-400'
                                    )}>
                                        <Bot className="h-5 w-5" />
                                    </div>
                                    <div>
                                        <p className="font-semibold text-slate-900">{ar.name}</p>
                                        <p className="text-sm text-slate-500">
                                            Trigger: <span className="font-medium text-slate-700">{triggerLabels[ar.triggerEvent] || ar.triggerEvent}</span>
                                            {ar.delayMinutes > 0 && (
                                                <span className="ml-2">• Delay: {ar.delayMinutes}min</span>
                                            )}
                                        </p>
                                    </div>
                                </div>
                                <span className={cn(
                                    'px-3 py-1 rounded-full text-xs font-semibold',
                                    ar.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
                                )}>
                                    {ar.isActive ? 'Active' : 'Paused'}
                                </span>
                            </div>
                        ))
                    )}
                </div>
            )}

            {/* Campaigns Tab */}
            {activeTab === 'campaigns' && (<>

            {/* Stats Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                    { label: 'Active Campaigns', value: activeCampaigns, icon: Zap, color: 'emerald' },
                    { label: 'Total Sent', value: totalSent.toLocaleString(), icon: Send, color: 'blue' },
                    { label: 'Avg. Open Rate', value: `${avgOpenRate}%`, icon: Target, color: 'violet' },
                    { label: 'Click Rate', value: aggregate ? `${aggregate.clickRate}%` : '0%', icon: TrendingUp, color: 'amber' },
                ].map((stat, i) => (
                    <div
                        key={stat.label}
                        className="stat-card animate-fade-in-up"
                        style={{ animationDelay: `${(i + 1) * 100}ms` }}
                    >
                        <div className="flex items-center gap-3">
                            <div className={cn(
                                'p-2.5 rounded-xl',
                                stat.color === 'emerald' && 'bg-emerald-100 text-emerald-600',
                                stat.color === 'blue' && 'bg-blue-100 text-blue-600',
                                stat.color === 'violet' && 'bg-violet-100 text-violet-600',
                                stat.color === 'amber' && 'bg-amber-100 text-amber-600',
                            )}>
                                <stat.icon className="h-5 w-5" />
                            </div>
                            <div>
                                <p className="stat-value text-xl">{stat.value}</p>
                                <p className="text-sm text-slate-500">{stat.label}</p>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Filters */}
            <div className="flex flex-col sm:flex-row gap-4 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                <div className="relative flex-1">
                    <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <input
                        type="text"
                        placeholder="Search campaigns..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="input pl-11"
                    />
                </div>
                <div className="flex gap-2 flex-wrap">
                    {['all', 'active', 'scheduled', 'completed', 'draft'].map((status) => (
                        <button
                            key={status}
                            onClick={() => setStatusFilter(status)}
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-medium transition-all capitalize',
                                statusFilter === status
                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                    : 'bg-white text-slate-600 border border-slate-200 hover:border-primary-300'
                            )}
                        >
                            {status}
                        </button>
                    ))}
                </div>
            </div>

            {/* Campaigns List */}
            <div className="space-y-4">
                {loading ? (
                    Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="card-elevated p-6 animate-pulse">
                            <div className="flex items-start gap-4">
                                <div className="p-3 rounded-xl bg-slate-100 w-12 h-12" />
                                <div className="flex-1 space-y-3">
                                    <div className="flex items-center justify-between">
                                        <div className="h-5 w-48 bg-slate-200 rounded" />
                                        <div className="h-8 w-8 bg-slate-100 rounded-lg" />
                                    </div>
                                    <div className="h-4 w-64 bg-slate-100 rounded" />
                                    <div className="pt-4 border-t border-slate-100">
                                        <div className="grid grid-cols-4 gap-4">
                                            {[1, 2, 3, 4].map(j => (
                                                <div key={j} className="space-y-1">
                                                    <div className="h-3 w-12 bg-slate-50 rounded" />
                                                    <div className="h-4 w-16 bg-slate-100 rounded" />
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))
                ) : filteredCampaigns.length === 0 ? (
                    <div className="card-elevated py-16 text-center animate-fade-in">
                        <Megaphone className="h-12 w-12 text-slate-300 mx-auto mb-4" />
                        <h3 className="text-lg font-semibold text-slate-900 mb-2">No campaigns found</h3>
                        <p className="text-slate-500 mb-6">Create your first campaign to engage with customers</p>
                        <Link href="/campaigns/new" className="btn btn-primary">
                            <Plus className="h-4 w-4" />
                            Create Campaign
                        </Link>
                    </div>
                ) : (
                    filteredCampaigns.map((campaign, index) => {
                        const TypeIcon = getTypeIcon(campaign.type);
                        const statusStyles = getStatusStyles(campaign.status);
                        const openRate = campaign.sent > 0 ? Math.round((campaign.opened / campaign.sent) * 100) : 0;
                        const clickRate = campaign.opened > 0 ? Math.round((campaign.clicked / campaign.opened) * 100) : 0;

                        return (
                            <div
                                key={campaign.id}
                                className="card-elevated overflow-hidden group cursor-pointer animate-fade-in-up"
                                style={{ animationDelay: `${400 + index * 100}ms` }}
                            >
                                <div className="p-6">
                                    <div className="flex items-start gap-4">
                                        {/* Type Icon */}
                                        <div className={cn(
                                            'p-3 rounded-xl bg-gradient-to-br shadow-lg',
                                            getTypeColor(campaign.type)
                                        )}>
                                            <TypeIcon className="h-6 w-6 text-white" />
                                        </div>

                                        <div className="flex-1 min-w-0">
                                            {/* Header Row */}
                                            <div className="flex items-start justify-between gap-4 mb-3">
                                                <div>
                                                    <div className="flex items-center gap-2 mb-1">
                                                        <h3 className="font-semibold text-slate-900 text-lg">{campaign.name}</h3>
                                                        <span className={cn(
                                                            'flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium',
                                                            statusStyles.bg, statusStyles.text
                                                        )}>
                                                            <span className={cn('w-1.5 h-1.5 rounded-full', statusStyles.dot)} />
                                                            {campaign.status.charAt(0).toUpperCase() + campaign.status.slice(1)}
                                                        </span>
                                                    </div>
                                                    <div className="flex items-center gap-3 text-sm text-slate-500">
                                                        <span className="capitalize">{campaign.type}</span>
                                                        <span>•</span>
                                                        <span>{campaign.audience.toLocaleString()} recipients</span>
                                                        {campaign.scheduledFor && (
                                                            <>
                                                                <span>•</span>
                                                                <span className="flex items-center gap-1">
                                                                    <Clock className="h-3.5 w-3.5" />
                                                                    Scheduled for {formatDate(campaign.scheduledFor)}
                                                                </span>
                                                            </>
                                                        )}
                                                    </div>
                                                </div>

                                                <div className="flex items-center gap-2">
                                                    {campaign.status === 'active' && (
                                                        <button className="p-2 rounded-lg text-amber-600 bg-amber-50 hover:bg-amber-100 transition-colors">
                                                            <Pause className="h-4 w-4" />
                                                        </button>
                                                    )}
                                                    {campaign.status === 'draft' && (
                                                        <button className="btn btn-primary text-sm py-1.5">
                                                            <Play className="h-4 w-4" />
                                                            Launch
                                                        </button>
                                                    )}
                                                    <button className="p-2 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors">
                                                        <MoreVertical className="h-4 w-4" />
                                                    </button>
                                                </div>
                                            </div>

                                            {/* Metrics */}
                                            {campaign.status !== 'draft' && (
                                                <div className="grid grid-cols-4 gap-4 pt-4 border-t border-slate-100">
                                                    <div>
                                                        <p className="text-xs text-slate-500 mb-1">Sent</p>
                                                        <p className="font-semibold text-slate-900">{campaign.sent.toLocaleString()}</p>
                                                    </div>
                                                    <div>
                                                        <p className="text-xs text-slate-500 mb-1">Opened</p>
                                                        <p className="font-semibold text-slate-900">{campaign.opened.toLocaleString()}</p>
                                                    </div>
                                                    <div>
                                                        <p className="text-xs text-slate-500 mb-1">Open Rate</p>
                                                        <p className="font-semibold text-emerald-600">{openRate}%</p>
                                                    </div>
                                                    <div>
                                                        <p className="text-xs text-slate-500 mb-1">Click Rate</p>
                                                        <p className="font-semibold text-blue-600">{clickRate}%</p>
                                                    </div>
                                                </div>
                                            )}
                                        </div>

                                        {/* Arrow */}
                                        <ChevronRight className="h-5 w-5 text-slate-300 group-hover:text-primary-500 group-hover:translate-x-1 transition-all self-center" />
                                    </div>
                                </div>

                                {/* Progress bar for active campaigns */}
                                {campaign.status === 'active' && campaign.sent > 0 && (
                                    <div className="h-1 bg-slate-100">
                                        <div
                                            className="h-full bg-gradient-to-r from-emerald-400 to-emerald-600 transition-all duration-500"
                                            style={{ width: `${openRate}%` }}
                                        />
                                    </div>
                                )}
                            </div>
                        );
                    })
                )}
            </div>
            </>)}
        </div>
    );
}
