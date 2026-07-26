'use client';

import { useEffect, useState, useCallback } from 'react';
import {
    Calendar, Users, DollarSign, TrendingUp,
    ArrowUpRight, ArrowDownRight, Clock, Sparkles,
    ChevronRight, Zap, Target, Lightbulb, Settings2, Check, X
} from 'lucide-react';
import { HubConnectionState } from '@microsoft/signalr';
import { Link } from '@/navigation';
import { cn, formatCurrency, formatRelativeTime } from '@/lib/utils';
import api from '@/lib/api';
import { useSignalR } from '@/contexts/SignalRContext';
import { Skeleton, SkeletonCard, SkeletonBookingItem } from '@/components/ui';
import { useTranslations } from 'next-intl';
import { LiveBookingFeed } from '@/components/dashboard/LiveBookingFeed';
import { useAuthStore } from '@/store/authStore';

interface DashboardStats {
    todayBookings: number;
    todayRevenue: number;
    totalBookings: number;
    pendingBookings: number;
    completedBookings: number;
    bookingsThisMonth: number;
    revenueThisMonth: number;
    totalRevenue: number;
    totalClients: number;
    newClientsThisMonth?: number;
    lastUpdated: string;
    // Real trend data from API — undefined until backend provides it
    todayBookingsTrend?: number;
    todayRevenueTrend?: number;
    totalClientsTrend?: number;
    revenueThisMonthTrend?: number;
}

interface RecentBooking {
    id: string;
    clientName: string;
    clientInitials: string;
    serviceName: string;
    startTime: string;
    status: string;
    amount: number;
}

const KPI_STORAGE_KEY = 'upkilo_dashboard_kpis';
const ALL_KPI_IDS = ['todayBookings', 'todayRevenue', 'totalClients', 'revenueThisMonth', 'pendingBookings', 'completedBookings', 'newClients', 'totalRevenue'] as const;
type KpiId = typeof ALL_KPI_IDS[number];
const DEFAULT_KPIS: KpiId[] = ['todayBookings', 'todayRevenue', 'totalClients', 'revenueThisMonth'];

function loadSavedKpis(): KpiId[] {
    if (typeof window === 'undefined') return DEFAULT_KPIS;
    try {
        const saved = localStorage.getItem(KPI_STORAGE_KEY);
        if (saved) {
            const parsed = JSON.parse(saved) as KpiId[];
            if (Array.isArray(parsed) && parsed.length > 0) return parsed.slice(0, 4);
        }
    } catch {}
    return DEFAULT_KPIS;
}

export default function DashboardPage() {
    const { user } = useAuthStore();
    const t = useTranslations('Dashboard');
    const nt = useTranslations('Navigation');
    const [stats, setStats] = useState<DashboardStats | null>(null);
    const [recentBookings, setRecentBookings] = useState<RecentBooking[]>([]);
    const [loading, setLoading] = useState(true);
    const [greeting, setGreeting] = useState('');
    const { connection } = useSignalR();
    const [dateRange, setDateRange] = useState<'today' | '7d' | '30d' | 'month'>('today');
    const [selectedKpis, setSelectedKpis] = useState<KpiId[]>(DEFAULT_KPIS);
    const [kpiPanelOpen, setKpiPanelOpen] = useState(false);
    const [kpiDraft, setKpiDraft] = useState<KpiId[]>(DEFAULT_KPIS);

    // Load saved KPI selection from localStorage after mount
    useEffect(() => {
        setSelectedKpis(loadSavedKpis());
    }, []);

    const openKpiPanel = useCallback(() => {
        setKpiDraft(selectedKpis);
        setKpiPanelOpen(true);
    }, [selectedKpis]);

    const saveKpiSelection = useCallback(() => {
        if (kpiDraft.length === 0) return;
        const toSave = kpiDraft.slice(0, 4);
        setSelectedKpis(toSave);
        localStorage.setItem(KPI_STORAGE_KEY, JSON.stringify(toSave));
        setKpiPanelOpen(false);
    }, [kpiDraft]);

    const toggleKpiDraft = useCallback((id: KpiId) => {
        setKpiDraft(prev =>
            prev.includes(id)
                ? prev.filter(k => k !== id)
                : prev.length < 4 ? [...prev, id] : prev
        );
    }, []);

    useEffect(() => {
        if (!connection) return;

        const handleStatsUpdate = (updatedStats: any) => {
            setStats(prev => prev ? { ...prev, ...updatedStats } : updatedStats);
        };

        connection.on('DashboardStatsUpdated', handleStatsUpdate);

        return () => {
            connection.off('DashboardStatsUpdated', handleStatsUpdate);
        };
    }, [connection]);

    useEffect(() => {
        const hour = new Date().getHours();
        if (hour < 12) setGreeting('Good morning');
        else if (hour < 17) setGreeting('Good afternoon');
        else setGreeting('Good evening');

        const fetchData = async () => {
            setLoading(true);
            try {
                const [statsRes, bookingsRes] = await Promise.all([
                    api.dashboard.stats(),
                    api.dashboard.recentBookings()
                ]);
                setStats(statsRes.data);
                setRecentBookings(bookingsRes.data);
            } catch (error) {
                console.error('Failed to fetch dashboard data', error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    if (loading) {
        return (
            <div className="space-y-8 pb-8">
                {/* Skeleton Header */}
                <div className="h-48 w-full rounded-2xl bg-slate-100 dark:bg-slate-800/50 animate-pulse relative overflow-hidden">
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/20 dark:via-white/5 to-transparent -translate-x-full animate-shimmer" />
                </div>

                {/* Skeleton Stats Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
                    <SkeletonCard />
                    <SkeletonCard />
                    <SkeletonCard />
                    <SkeletonCard />
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <div className="lg:col-span-2 card-elevated overflow-hidden">
                        <div className="p-6 border-b border-slate-100 dark:border-slate-800">
                            <Skeleton className="h-6 w-48 mb-2" />
                            <Skeleton className="h-4 w-32" />
                        </div>
                        <div className="divide-y divide-slate-100 dark:divide-slate-800">
                            <SkeletonBookingItem />
                            <SkeletonBookingItem />
                            <SkeletonBookingItem />
                            <SkeletonBookingItem />
                        </div>
                    </div>
                    <div className="space-y-6">
                        <Skeleton className="h-48 w-full rounded-2xl" />
                        <Skeleton className="h-48 w-full rounded-2xl" />
                    </div>
                </div>
            </div>
        );
    }

    // Trend helpers — only show badge when API provides real data
    const trendBadge = (trend?: number) => {
        if (trend === undefined || trend === null) return null;
        return { value: `${trend > 0 ? '+' : ''}${trend.toFixed(1)}%`, up: trend >= 0 };
    };

    const kpiCatalog: Record<KpiId, { title: string; href: string; value: string | number; icon: React.ElementType; trend: ReturnType<typeof trendBadge>; gradient: string; bgGradient: string }> = {
        todayBookings: {
            title: "Today's Bookings",
            href: '/bookings?date=today',
            value: stats?.todayBookings ?? 0,
            icon: Calendar,
            trend: trendBadge(stats?.todayBookingsTrend),
            gradient: 'from-blue-500 to-cyan-400',
            bgGradient: 'from-blue-500/10 to-cyan-400/5',
        },
        todayRevenue: {
            title: "Today's Revenue",
            href: '/payments?date=today',
            value: formatCurrency(stats?.todayRevenue ?? 0),
            icon: DollarSign,
            trend: trendBadge(stats?.todayRevenueTrend),
            gradient: 'from-emerald-500 to-teal-400',
            bgGradient: 'from-emerald-500/10 to-teal-400/5',
        },
        totalClients: {
            title: nt('clients'),
            href: '/clients',
            value: stats?.totalClients ?? 0,
            icon: Users,
            trend: stats?.newClientsThisMonth != null
                ? { value: `+${stats.newClientsThisMonth} new`, up: true }
                : null,
            gradient: 'from-violet-500 to-purple-400',
            bgGradient: 'from-violet-500/10 to-purple-400/5',
        },
        revenueThisMonth: {
            title: 'Monthly Revenue',
            href: '/analytics?period=month',
            value: formatCurrency(stats?.revenueThisMonth ?? 0),
            icon: TrendingUp,
            trend: trendBadge(stats?.revenueThisMonthTrend),
            gradient: 'from-amber-500 to-orange-400',
            bgGradient: 'from-amber-500/10 to-orange-400/5',
        },
        pendingBookings: {
            title: 'Pending Bookings',
            href: '/bookings?status=pending',
            value: stats?.pendingBookings ?? 0,
            icon: Clock,
            trend: null,
            gradient: 'from-yellow-500 to-amber-400',
            bgGradient: 'from-yellow-500/10 to-amber-400/5',
        },
        completedBookings: {
            title: 'Completed Today',
            href: '/bookings?status=completed',
            value: stats?.completedBookings ?? 0,
            icon: Target,
            trend: null,
            gradient: 'from-green-500 to-emerald-400',
            bgGradient: 'from-green-500/10 to-emerald-400/5',
        },
        newClients: {
            title: 'New Clients',
            href: '/clients?filter=new',
            value: stats?.newClientsThisMonth ?? 0,
            icon: Users,
            trend: null,
            gradient: 'from-pink-500 to-rose-400',
            bgGradient: 'from-pink-500/10 to-rose-400/5',
        },
        totalRevenue: {
            title: 'Total Revenue',
            href: '/payments',
            value: formatCurrency(stats?.totalRevenue ?? 0),
            icon: DollarSign,
            trend: null,
            gradient: 'from-indigo-500 to-blue-400',
            bgGradient: 'from-indigo-500/10 to-blue-400/5',
        },
    };

    const KPI_LABELS: Record<KpiId, string> = {
        todayBookings: "Today's Bookings",
        todayRevenue: "Today's Revenue",
        totalClients: 'Total Clients',
        revenueThisMonth: 'Monthly Revenue',
        pendingBookings: 'Pending Bookings',
        completedBookings: 'Completed Today',
        newClients: 'New Clients',
        totalRevenue: 'Total Revenue',
    };

    const statCards = selectedKpis.map(id => kpiCatalog[id]).filter(Boolean);

    const quickActions = [
        { href: '/bookings/new', icon: Calendar, label: 'New Booking', color: 'primary' },
        { href: '/clients', icon: Users, label: 'Add Client', color: 'violet' },
        { href: '/services', icon: Zap, label: 'New Service', color: 'emerald' },
        { href: '/dashboard/reports', icon: Target, label: 'View Reports', color: 'amber' },
    ];

    return (
        <div className="space-y-8 pb-8">
            {/* Hero Header with Gradient Mesh Background */}
            <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-8 grain">
                {/* Gradient Mesh Overlay */}
                <div className="absolute inset-0 opacity-40">
                    <div className="absolute top-0 -left-10 w-72 h-72 bg-primary-500 rounded-full mix-blend-multiply filter blur-3xl animate-float" />
                    <div className="absolute top-0 -right-10 w-72 h-72 bg-emerald-500 rounded-full mix-blend-multiply filter blur-3xl animate-float" style={{ animationDelay: '1s' }} />
                    <div className="absolute -bottom-10 left-1/2 w-72 h-72 bg-violet-500 rounded-full mix-blend-multiply filter blur-3xl animate-float" style={{ animationDelay: '2s' }} />
                </div>

                <div className="relative z-10">
                    <div className="flex items-start justify-between">
                        <div className="animate-fade-in-up">
                            <p className="text-primary-400 font-medium mb-1 flex items-center gap-2">
                                <Sparkles className="w-4 h-4" />
                                {greeting}
                            </p>
                            <h1 className="text-3xl md:text-4xl font-bold text-white mb-2">
                                Welcome back,{' '}
                                {user?.firstName ? (
                                    <span className="gradient-text">{user.firstName}</span>
                                ) : (
                                    <span className="inline-block w-28 h-8 bg-white/20 rounded-lg animate-pulse align-middle" aria-hidden="true" />
                                )}
                            </h1>
                            <p className="text-slate-400 max-w-lg">
                                Here's what's happening with your business today. You have {stats?.todayBookings} bookings scheduled.
                            </p>
                        </div>

                        {/* Date range selector */}
                        <div className="flex flex-col items-end gap-3">
                            <div
                                className="flex items-center gap-1 glass-dark rounded-xl p-1"
                                role="group"
                                aria-label="Dashboard date range"
                            >
                                {([ ['today', 'Today'], ['7d', '7 Days'], ['30d', '30 Days'], ['month', 'This Month'] ] as const).map(([val, label]) => (
                                    <button
                                        key={val}
                                        onClick={() => setDateRange(val)}
                                        className={cn(
                                            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
                                            dateRange === val
                                                ? 'bg-white text-slate-900 shadow-sm'
                                                : 'text-slate-400 hover:text-white'
                                        )}
                                        aria-pressed={dateRange === val}
                                    >
                                        {label}
                                    </button>
                                ))}
                            </div>

                        {/* Quick stat badge — only show when SignalR is connected */}
                        {connection?.state === HubConnectionState.Connected && (
                            <div className="hidden md:flex items-center gap-3 glass-dark rounded-xl px-4 py-3 animate-fade-in" style={{ animationDelay: '200ms' }}>
                                <div className="flex items-center gap-2">
                                    <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" aria-hidden="true" />
                                    <span className="text-emerald-400 text-sm font-medium">Live</span>
                                </div>
                                <div className="h-4 w-px bg-slate-600" aria-hidden="true" />
                                <span className="text-white font-semibold">{stats?.todayBookings} Active</span>
                            </div>
                        )}
                        </div>{/* end date range + live badge column */}
                    </div>
                </div>
            </div>

            {/* Stats Grid — L9: customizable KPI selection */}
            <div className="flex items-center justify-between mb-1">
                <h2 className="sr-only">Key Performance Indicators</h2>
                <button
                    onClick={openKpiPanel}
                    className="ms-auto flex items-center gap-1.5 text-xs text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors py-1 px-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800"
                    aria-label="Customize dashboard KPI cards"
                    aria-expanded={kpiPanelOpen}
                    aria-controls="kpi-customizer"
                >
                    <Settings2 className="h-3.5 w-3.5" aria-hidden="true" />
                    Customize
                </button>
            </div>

            {/* KPI Customization Panel */}
            {kpiPanelOpen && (
                <div
                    id="kpi-customizer"
                    role="dialog"
                    aria-label="Choose KPI cards to display"
                    aria-modal="true"
                    className="card-elevated p-5 animate-scale-in"
                >
                    <div className="flex items-center justify-between mb-4">
                        <div>
                            <h3 className="font-semibold text-slate-900 dark:text-white text-sm">Choose your KPI cards</h3>
                            <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Select up to 4 metrics to show on your dashboard</p>
                        </div>
                        <button
                            onClick={() => setKpiPanelOpen(false)}
                            className="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
                            aria-label="Close KPI customizer"
                        >
                            <X className="h-4 w-4 text-slate-400" aria-hidden="true" />
                        </button>
                    </div>
                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mb-4">
                        {ALL_KPI_IDS.map(id => {
                            const selected = kpiDraft.includes(id);
                            const disabled = !selected && kpiDraft.length >= 4;
                            return (
                                <button
                                    key={id}
                                    onClick={() => toggleKpiDraft(id)}
                                    disabled={disabled}
                                    aria-pressed={selected}
                                    className={cn(
                                        'flex items-center gap-2 px-3 py-2.5 rounded-xl border text-sm font-medium transition-all text-start',
                                        selected
                                            ? 'border-violet-500 bg-violet-50 dark:bg-violet-500/10 text-violet-700 dark:text-violet-300'
                                            : 'border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:border-slate-300 dark:hover:border-slate-600',
                                        disabled && 'opacity-40 cursor-not-allowed'
                                    )}
                                >
                                    <div className={cn(
                                        'w-4 h-4 rounded flex items-center justify-center shrink-0 border transition-colors',
                                        selected ? 'bg-violet-500 border-violet-500' : 'border-slate-300 dark:border-slate-600'
                                    )}>
                                        {selected && <Check className="h-2.5 w-2.5 text-white" aria-hidden="true" />}
                                    </div>
                                    <span className="truncate">{KPI_LABELS[id]}</span>
                                </button>
                            );
                        })}
                    </div>
                    <div className="flex items-center justify-between">
                        <p className="text-xs text-slate-400">{kpiDraft.length}/4 selected</p>
                        <div className="flex gap-2">
                            <button
                                onClick={() => setKpiPanelOpen(false)}
                                className="btn btn-secondary text-xs px-3 py-1.5"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={saveKpiSelection}
                                disabled={kpiDraft.length === 0}
                                className="btn btn-primary text-xs px-3 py-1.5"
                            >
                                Apply
                            </button>
                        </div>
                    </div>
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
                {statCards.map((stat, i) => (
                    <Link
                        key={i}
                        href={stat.href}
                        className={cn(
                            'stat-card group animate-fade-in-up dark:bg-slate-900 dark:border-slate-800',
                            `bg-gradient-to-br ${stat.bgGradient}`
                        )}
                        style={{ animationDelay: `${i * 50}ms` }}
                        aria-label={`${stat.title}: ${stat.value}`}
                    >
                        <div className="flex items-start justify-between mb-4">
                            <div className={cn(
                                'p-2.5 rounded-xl bg-gradient-to-br',
                                stat.gradient,
                                'shadow-lg group-hover:scale-110 transition-transform duration-300'
                            )}>
                                <stat.icon className="h-5 w-5 text-white" aria-hidden="true" />
                            </div>
                            {stat.trend && (
                                <div className={cn(
                                    'flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium',
                                    stat.trend.up
                                        ? 'bg-emerald-50 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400'
                                        : 'bg-red-50 text-red-600 dark:bg-red-500/10 dark:text-red-400'
                                )} aria-label={`Trend: ${stat.trend.value}`}>
                                    {stat.trend.up
                                        ? <ArrowUpRight className="h-3 w-3" aria-hidden="true" />
                                        : <ArrowDownRight className="h-3 w-3" aria-hidden="true" />
                                    }
                                    {stat.trend.value}
                                </div>
                            )}
                        </div>

                        <p className="text-sm text-slate-500 dark:text-slate-400 mb-1">{stat.title}</p>
                        <p className="stat-value text-slate-900 dark:text-white">{stat.value}</p>
                    </Link>
                ))}
            </div>

            {/* Onboarding re-engagement banner — shown only when account has no bookings yet */}
            {stats?.totalBookings === 0 && (
                <div
                    role="status"
                    className="flex items-center gap-4 bg-gradient-to-r from-violet-50 to-indigo-50 dark:from-violet-900/20 dark:to-indigo-900/20 border border-violet-200 dark:border-violet-800 rounded-2xl p-5 animate-fade-in-up"
                    style={{ animationDelay: '350ms' }}
                >
                    <div className="w-10 h-10 rounded-xl bg-violet-100 dark:bg-violet-800/40 flex items-center justify-center shrink-0">
                        <Sparkles className="h-5 w-5 text-violet-500" aria-hidden="true" />
                    </div>
                    <div className="flex-1 min-w-0">
                        <p className="font-semibold text-slate-900 dark:text-white text-sm">Complete your setup to get started</p>
                        <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Add your services, hours, and first booking in a few quick steps.</p>
                    </div>
                    <Link
                        href="/onboarding"
                        className="shrink-0 inline-flex items-center gap-1.5 px-4 py-2 bg-violet-600 hover:bg-violet-700 text-white text-sm font-medium rounded-xl transition-colors"
                    >
                        Continue setup
                        <ChevronRight className="h-4 w-4" aria-hidden="true" />
                    </Link>
                </div>
            )}

            {/* Main Content Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Upcoming Bookings - Takes 2 columns */}
                <div className="lg:col-span-2 card-elevated overflow-hidden animate-fade-in-up dark:bg-slate-900 dark:border-slate-800" style={{ animationDelay: '400ms' }}>
                    <div className="p-6 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="p-2 bg-primary-50 dark:bg-primary-500/10 rounded-lg" aria-hidden="true">
                                <Clock className="h-5 w-5 text-primary-500" />
                            </div>
                            <div>
                                <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
                                    Upcoming Bookings
                                </h2>
                                <p className="text-sm text-slate-500 dark:text-slate-400">Next 24 hours</p>
                            </div>
                        </div>
                        <Link
                            href="/bookings"
                            className="flex items-center gap-1 text-primary-500 hover:text-primary-600 text-sm font-medium group"
                        >
                            View all
                            <ChevronRight className="h-4 w-4 group-hover:translate-x-0.5 transition-transform" />
                        </Link>
                    </div>

                    {/* Empty state for new accounts */}
                    {recentBookings.length === 0 && !loading && (
                        <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
                            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-100 to-violet-200 dark:from-violet-900/30 dark:to-violet-800/30 flex items-center justify-center mb-4">
                                <Calendar className="h-8 w-8 text-violet-500" aria-hidden="true" />
                            </div>
                            <h3 className="text-base font-semibold text-slate-900 dark:text-white mb-1">No bookings yet</h3>
                            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-xs mb-5">
                                Your upcoming appointments will appear here. Create your first booking to get started.
                            </p>
                            <Link
                                href="/bookings/new"
                                className="inline-flex items-center gap-2 px-4 py-2 bg-violet-600 hover:bg-violet-700 text-white text-sm font-medium rounded-xl transition-colors"
                            >
                                <Calendar className="h-4 w-4" aria-hidden="true" />
                                Create first booking
                            </Link>
                        </div>
                    )}

                    <div className="divide-y divide-slate-100 dark:divide-slate-800">
                        {recentBookings.map((booking, index) => (
                            <Link
                                key={booking.id}
                                href={`/bookings/${booking.id}`}
                                className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors group animate-fade-in"
                                style={{ animationDelay: `${200 + index * 40}ms` }}
                                aria-label={`Booking: ${booking.clientName} — ${booking.serviceName}, ${formatRelativeTime(booking.startTime)}`}
                            >
                                <div className="flex items-center gap-4">
                                    {/* Avatar */}
                                    <div className="relative flex-shrink-0">
                                        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold text-sm shadow-lg shadow-primary-500/20" aria-hidden="true">
                                            {booking.clientInitials}
                                        </div>
                                        <div
                                            className={cn(
                                                'absolute -bottom-1 -right-1 w-4 h-4 rounded-full border-2 border-white dark:border-slate-900',
                                                booking.status === 'confirmed' ? 'bg-emerald-400' : 'bg-amber-400'
                                            )}
                                            aria-label={`Status: ${booking.status}`}
                                            role="img"
                                        />
                                    </div>

                                    {/* Info */}
                                    <div className="flex-1 min-w-0">
                                        <p className="font-medium text-slate-900 dark:text-white truncate">{booking.clientName}</p>
                                        <p className="text-sm text-slate-500 dark:text-slate-400 truncate">{booking.serviceName}</p>
                                    </div>

                                    {/* Time & Amount */}
                                    <div className="text-right">
                                        <p className="text-sm font-medium text-slate-900 dark:text-white">
                                            {formatRelativeTime(booking.startTime)}
                                        </p>
                                        <p className="text-sm font-semibold text-primary-600">
                                            {formatCurrency(booking.amount)}
                                        </p>
                                    </div>

                                    {/* Arrow */}
                                    <ChevronRight className="h-5 w-5 text-slate-300 group-hover:text-primary-500 group-hover:translate-x-1 transition-all" aria-hidden="true" />
                                </div>
                            </Link>
                        ))}
                    </div>

                    {/* Footer CTA */}
                    <div className="p-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-100 dark:border-slate-800">
                        <Link
                            href="/bookings/new"
                            className="w-full btn btn-primary justify-center"
                        >
                            <Calendar className="h-4 w-4" />
                            Create New Booking
                        </Link>
                    </div>
                </div>

                {/* Right Column */}
                <div className="space-y-6">
                    {/* Quick Actions */}
                    <div className="card-elevated p-6 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800" style={{ animationDelay: '450ms' }}>
                        <h2 className="text-lg font-semibold text-slate-900 dark:text-white mb-4 flex items-center gap-2">
                            <Zap className="h-5 w-5 text-amber-500" aria-hidden="true" />
                            Quick Actions
                        </h2>
                        <div className="grid grid-cols-2 gap-3">
                            {quickActions.map((action, index) => (
                                <Link
                                    key={action.href}
                                    href={action.href}
                                    className={cn(
                                        'group flex flex-col items-center justify-center p-4 rounded-xl border border-slate-200 dark:border-slate-800',
                                        'hover:border-primary-300 hover:shadow-lg hover:shadow-primary-500/10 dark:hover:border-primary-500/50',
                                        'transition-all duration-300 hover:-translate-y-1',
                                        'animate-fade-in'
                                    )}
                                    style={{ animationDelay: `${550 + index * 50}ms` }}
                                >
                                    <div className="p-3 rounded-xl bg-slate-100 dark:bg-slate-800 group-hover:bg-primary-50 dark:group-hover:bg-primary-500/10 transition-colors mb-2">
                                        <action.icon className="h-5 w-5 text-slate-600 dark:text-slate-400 group-hover:text-primary-500 transition-colors" />
                                    </div>
                                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                                        {action.label}
                                    </span>
                                </Link>
                            ))}
                        </div>
                    </div>

                    {/* Live Activity Feed */}
                    <LiveBookingFeed />

                    {/* Booking Status Summary — real data from API */}
                    <div className="card-elevated p-6 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800" style={{ animationDelay: '300ms' }}>
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center gap-2">
                                <Lightbulb className="h-5 w-5 text-violet-500" aria-hidden="true" />
                                Today's Status
                            </h2>
                            <Link href="/bookings?date=today" className="text-xs text-primary-500 hover:text-primary-600 font-medium">
                                View all
                            </Link>
                        </div>

                        <div className="space-y-3">
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-slate-600 dark:text-slate-400">Total bookings</span>
                                <span className="font-semibold text-slate-900 dark:text-white">{stats?.todayBookings ?? '—'}</span>
                            </div>
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-slate-600 dark:text-slate-400">Pending confirmation</span>
                                <span className="font-semibold text-amber-600">{stats?.pendingBookings ?? '—'}</span>
                            </div>
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-slate-600 dark:text-slate-400">Completed</span>
                                <span className="font-semibold text-emerald-600">{stats?.completedBookings ?? '—'}</span>
                            </div>
                            <div className="h-px bg-slate-100 dark:bg-slate-800 my-2" aria-hidden="true" />
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-slate-600 dark:text-slate-400">This month</span>
                                <span className="font-semibold text-slate-900 dark:text-white">{stats?.bookingsThisMonth ?? '—'}</span>
                            </div>
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-slate-600 dark:text-slate-400">Total clients</span>
                                <span className="font-semibold text-violet-600">{stats?.totalClients ?? '—'}</span>
                            </div>
                        </div>
                    </div>
                    {/* AI Insight Card — M16/M21: proactive insight with distinct loading vs empty states */}
                    <AiInsightCard todayBookings={stats?.todayBookings} todayRevenue={stats?.todayRevenue} />
                </div>
            </div>
        </div>
    );
}

function AiInsightCard({ todayBookings, todayRevenue }: { todayBookings?: number; todayRevenue?: number }) {
    const [insight, setInsight] = useState<string | null>(null);
    const [insightLoading, setInsightLoading] = useState(true);

    useEffect(() => {
        const timer = setTimeout(() => {
            if (todayBookings === undefined) { setInsightLoading(false); return; }
            // Generate a contextual insight from available stats
            if (todayBookings === 0) {
                setInsight('No bookings scheduled yet today. Consider sending a reminder campaign to re-engage recent clients.');
            } else if (todayBookings >= 5) {
                setInsight(`Busy day ahead with ${todayBookings} bookings. Make sure staff schedules are confirmed and reminders are sent.`);
            } else {
                setInsight(`You have ${todayBookings} booking${todayBookings !== 1 ? 's' : ''} today. A light day — good time to follow up with pending quotes.`);
            }
            setInsightLoading(false);
        }, 1200);
        return () => clearTimeout(timer);
    }, [todayBookings]);

    return (
        <div className="card-elevated p-5 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800" style={{ animationDelay: '400ms' }}>
            <div className="flex items-center gap-2 mb-3">
                <div className="p-1.5 rounded-lg bg-violet-100 dark:bg-violet-900/40">
                    <Sparkles className="h-4 w-4 text-violet-500" aria-hidden="true" />
                </div>
                <span className="text-sm font-semibold text-slate-900 dark:text-white">AI Daily Insight</span>
            </div>

            {insightLoading ? (
                // Loading state — pulsing skeleton lines
                <div className="space-y-2" aria-busy="true" aria-label="Loading AI insight">
                    <div className="h-3 bg-slate-100 dark:bg-slate-800 rounded animate-pulse w-full" />
                    <div className="h-3 bg-slate-100 dark:bg-slate-800 rounded animate-pulse w-4/5" />
                    <div className="h-3 bg-slate-100 dark:bg-slate-800 rounded animate-pulse w-3/5" />
                </div>
            ) : insight ? (
                // Loaded with insight
                <p className="text-sm text-slate-600 dark:text-slate-400 leading-relaxed">{insight}</p>
            ) : (
                // Empty state — no data yet
                <div className="text-center py-2">
                    <p className="text-sm text-slate-400 dark:text-slate-500">Insights will appear once you have bookings.</p>
                </div>
            )}
        </div>
    );
}
