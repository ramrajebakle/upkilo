'use client';

import { useState, useEffect } from 'react';
import {
    TrendingUp,
    TrendingDown,
    DollarSign,
    Users,
    Calendar,
    Download,
    ChevronDown,
    BarChart3,
    PieChart,
    ArrowUpRight,
    Clock,
    Target,
    Sparkles,
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { apiClient } from '@/lib/api';

interface ReportStats {
    totalRevenue: number;
    revenueChange: number;
    totalBookings: number;
    bookingsChange: number;
    newClients: number;
    clientsChange: number;
    avgBookingValue: number;
    avgValueChange: number;
}

interface ChartDataPoint { day: string; value: number; }
interface TopServiceItem { name: string; bookings: number; revenue: number; growth: number; }
interface PeakHourItem { time: string; bookings: number; percent: number; }
interface ChannelItem { channel: string; percent: number; color: string; }

const channelColors = ['bg-violet-500', 'bg-cyan-500', 'bg-amber-500', 'bg-emerald-500', 'bg-pink-500'];

export default function ReportsPage() {
    const [stats, setStats] = useState<ReportStats | null>(null);
    const [loading, setLoading] = useState(true);
    const [dateRange, setDateRange] = useState('30d');
    const [activeTab, setActiveTab] = useState<'overview' | 'revenue' | 'bookings' | 'clients'>('overview');
    const [revenueData, setRevenueData] = useState<ChartDataPoint[]>([]);
    const [topServices, setTopServices] = useState<TopServiceItem[]>([]);
    const [peakHours, setPeakHours] = useState<PeakHourItem[]>([]);
    const [retention, setRetention] = useState(0);
    const [channelData, setChannelData] = useState<ChannelItem[]>([]);

    useEffect(() => {
        const fetchReportStats = async () => {
            setLoading(true);
            try {
                const res = await apiClient.get(`/api/v1/analytics/dashboard?period=${dateRange}`);
                const d = res.data.data || res.data;
                setStats({
                    totalRevenue: d.totalRevenue ?? 0,
                    revenueChange: d.revenueChange ?? 0,
                    totalBookings: d.totalBookings ?? 0,
                    bookingsChange: d.bookingsChange ?? 0,
                    newClients: d.newClients ?? 0,
                    clientsChange: d.clientsChange ?? 0,
                    avgBookingValue: d.avgBookingValue ?? 0,
                    avgValueChange: d.avgValueChange ?? 0,
                });

                // Revenue chart data
                if (d.revenueByDay && Array.isArray(d.revenueByDay)) {
                    setRevenueData(d.revenueByDay.map((r: any) => ({ day: r.day || r.label, value: r.value || r.revenue || 0 })));
                } else {
                    const days = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun'];
                    const total = d.totalRevenue || 0;
                    setRevenueData(days.map(day => ({ day, value: Math.round(total / 7) })));
                }

                // Top services
                if (d.topServices && Array.isArray(d.topServices)) {
                    setTopServices(d.topServices.slice(0, 5).map((s: any) => ({
                        name: s.name || s.serviceName, bookings: s.bookings || s.count || 0,
                        revenue: s.revenue || 0, growth: s.growth || 0
                    })));
                } else {
                    setTopServices([]);
                }

                // Peak hours
                if (d.peakHours && Array.isArray(d.peakHours)) {
                    const maxBookings = Math.max(...d.peakHours.map((h: any) => h.bookings || 0), 1);
                    setPeakHours(d.peakHours.slice(0, 3).map((h: any) => ({
                        time: h.time || h.label, bookings: h.bookings || 0,
                        percent: Math.round(((h.bookings || 0) / maxBookings) * 100)
                    })));
                } else {
                    setPeakHours([{ time: '10 AM - 12 PM', bookings: 0, percent: 0 },
                        { time: '2 PM - 4 PM', bookings: 0, percent: 0 },
                        { time: '4 PM - 6 PM', bookings: 0, percent: 0 }]);
                }

                // Retention rate
                setRetention(d.retentionRate ?? d.clientRetention ?? 0);

                // Revenue by channel
                if (d.revenueByChannel && Array.isArray(d.revenueByChannel)) {
                    setChannelData(d.revenueByChannel.map((c: any, i: number) => ({
                        channel: c.channel || c.name, percent: c.percent || c.percentage || 0,
                        color: channelColors[i % channelColors.length]
                    })));
                } else {
                    setChannelData([{ channel: 'Online Booking', percent: 100, color: 'bg-violet-500' }]);
                }
            } catch (err) {
                console.error('Failed to fetch report stats:', err);
                setRevenueData(['Mon','Tue','Wed','Thu','Fri','Sat','Sun'].map(d => ({ day: d, value: 0 })));
                setPeakHours([{ time: '10 AM - 12 PM', bookings: 0, percent: 0 }]);
                setChannelData([{ channel: 'Online Booking', percent: 100, color: 'bg-violet-500' }]);
            } finally {
                setLoading(false);
            }
        };
        fetchReportStats();
    }, [dateRange]);

    const dateRanges = [
        { value: '7d', label: 'Last 7 days' },
        { value: '30d', label: 'Last 30 days' },
        { value: '90d', label: 'Last 90 days' },
        { value: 'year', label: 'This year' },
    ];

    const tabs = [
        { id: 'overview', label: 'Overview', icon: BarChart3 },
        { id: 'revenue', label: 'Revenue', icon: DollarSign },
        { id: 'bookings', label: 'Bookings', icon: Calendar },
        { id: 'clients', label: 'Clients', icon: Users },
    ];

    const maxValue = Math.max(...revenueData.map(d => d.value), 1);

    return (
        <div className="space-y-8 animate-fade-in px-1">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
                <div className="animate-fade-in-up">
                    <div className="flex items-center gap-4 mb-2">
                        <div className="p-3 bg-gradient-to-br from-violet-500 to-purple-600 rounded-2xl shadow-xl shadow-violet-500/25 transform transition-transform hover:scale-110">
                            <BarChart3 className="h-6 w-6 text-white" />
                        </div>
                        <div>
                            <h1
                                className="text-3xl lg:text-4xl font-black text-slate-900 dark:text-white tracking-tight"
                                style={{ fontFamily: 'var(--font-display)' }}
                            >
                                Intelligence Center
                            </h1>
                            <p className="text-slate-500 dark:text-slate-400 font-medium">Decode your business growth with precision analytics</p>
                        </div>
                    </div>
                </div>

                <div className="flex items-center gap-3 animate-fade-in" style={{ animationDelay: '100ms' }}>
                    {/* Date Range Selector */}
                    <div className="relative group">
                        <select
                            value={dateRange}
                            onChange={(e) => setDateRange(e.target.value)}
                            className="appearance-none bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl px-5 py-3 pr-12 text-sm font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-400 transition-all shadow-sm cursor-pointer uppercase tracking-widest text-[10px]"
                        >
                            {dateRanges.map((range) => (
                                <option key={range.value} value={range.value}>
                                    {range.label}
                                </option>
                            ))}
                        </select>
                        <ChevronDown className="absolute right-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 pointer-events-none group-hover:text-primary-500 transition-colors" />
                    </div>

                    <button className="btn btn-secondary px-6 py-3 rounded-2xl dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 font-bold uppercase tracking-widest text-[10px] shadow-sm hover:translate-y-[-1px] transition-all">
                        <Download className="h-4 w-4" />
                        Export
                    </button>
                </div>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {loading ? (
                    [...Array(4)].map((_, i) => (
                        <div key={i} className="stat-card border dark:border-slate-800">
                            <div className="h-5 w-20 skeleton dark:bg-slate-800 mb-2" />
                            <div className="h-8 w-32 skeleton dark:bg-slate-800 mb-2" />
                            <div className="h-4 w-16 skeleton dark:bg-slate-800" />
                        </div>
                    ))
                ) : (
                    [
                        {
                            label: 'Total Revenue',
                            value: formatCurrency(stats?.totalRevenue || 0),
                            change: stats?.revenueChange || 0,
                            icon: DollarSign,
                            gradient: 'from-emerald-500 to-teal-600',
                            bgGradient: 'from-emerald-500/5 to-teal-500/5 dark:from-emerald-500/10 dark:to-teal-500/5',
                            border: 'border-emerald-100/50 dark:border-emerald-900/20'
                        },
                        {
                            label: 'Total Bookings',
                            value: stats?.totalBookings || 0,
                            change: stats?.bookingsChange || 0,
                            icon: Calendar,
                            gradient: 'from-blue-500 to-cyan-600',
                            bgGradient: 'from-blue-500/5 to-cyan-500/5 dark:from-blue-500/10 dark:to-cyan-500/5',
                            border: 'border-blue-100/50 dark:border-blue-900/20'
                        },
                        {
                            label: 'New Clients',
                            value: stats?.newClients || 0,
                            change: stats?.clientsChange || 0,
                            icon: Users,
                            gradient: 'from-violet-500 to-purple-600',
                            bgGradient: 'from-violet-500/5 to-purple-500/5 dark:from-violet-500/10 dark:to-purple-500/5',
                            border: 'border-violet-100/50 dark:border-violet-900/20'
                        },
                        {
                            label: 'Avg. Order',
                            value: formatCurrency(stats?.avgBookingValue || 0),
                            change: stats?.avgValueChange || 0,
                            icon: Target,
                            gradient: 'from-amber-500 to-orange-600',
                            bgGradient: 'from-amber-500/5 to-orange-500/5 dark:from-amber-500/10 dark:to-orange-500/5',
                            border: 'border-amber-100/50 dark:border-amber-900/20'
                        },
                    ].map((stat, i) => (
                        <div
                            key={stat.label}
                            className={cn('stat-card animate-fade-in-up border dark:bg-slate-900 transition-all hover:scale-[1.02] hover:shadow-2xl', stat.bgGradient, stat.border)}
                            style={{ animationDelay: `${(i + 1) * 100}ms` }}
                        >
                            <div className="flex items-start justify-between mb-4">
                                <div className={cn(
                                    'p-3 rounded-2xl bg-gradient-to-br shadow-xl',
                                    stat.gradient
                                )}>
                                    <stat.icon className="h-5 w-5 text-white" />
                                </div>
                                <div className={cn(
                                    'flex items-center gap-1.5 text-[10px] font-black px-2.5 py-1 rounded-full shadow-sm border',
                                    stat.change >= 0 
                                        ? 'bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100/50 dark:border-emerald-400/20' 
                                        : 'bg-rose-50 dark:bg-rose-400/10 text-rose-600 dark:text-rose-400 border-rose-100/50 dark:border-rose-400/20'
                                )}>
                                    {stat.change >= 0 ? (
                                        <TrendingUp className="h-3 w-3" />
                                    ) : (
                                        <TrendingDown className="h-3 w-3" />
                                    )}
                                    {Math.abs(stat.change)}%
                                </div>
                            </div>
                            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500 mb-1.5">{stat.label}</p>
                            <p className="text-3xl font-black text-slate-900 dark:text-white tracking-tight">{stat.value}</p>
                        </div>
                    ))
                )}
            </div>

            {/* Tabs */}
            <div className="flex gap-2 p-1.5 bg-slate-100 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-800 rounded-2xl w-fit animate-fade-in-up shadow-inner" style={{ animationDelay: '300ms' }}>
                {tabs.map((tab) => (
                    <button
                        key={tab.id}
                        onClick={() => setActiveTab(tab.id as any)}
                        className={cn(
                            'flex items-center gap-2 px-5 py-2.5 rounded-xl text-[10px] font-bold uppercase tracking-widest transition-all',
                            activeTab === tab.id
                                ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-white shadow-lg'
                                : 'text-slate-500 dark:text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
                        )}
                    >
                        <tab.icon className="h-4 w-4" />
                        {tab.label}
                    </button>
                ))}
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Revenue Chart */}
                <div className="lg:col-span-2 card-elevated p-8 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-xl overflow-hidden relative" style={{ animationDelay: '400ms' }}>
                    <div className="absolute top-0 right-0 w-64 h-64 bg-primary-500/5 blur-3xl rounded-full -mr-32 -mt-32 pointer-events-none" />
                    <div className="flex items-center justify-between mb-8 relative">
                        <div>
                            <h3 className="text-xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                                Performance Velocity
                            </h3>
                            <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Daily revenue capitalization metrics</p>
                        </div>
                        <div className="flex items-center gap-3">
                            <span className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-widest text-slate-400">
                                <span className="w-2.5 h-2.5 rounded-full bg-primary-500 shadow-sm shadow-primary-500/50" />
                                Revenue
                            </span>
                        </div>
                    </div>

                    {/* Simple Bar Chart */}
                    <div className="flex items-end justify-between gap-4 h-60 relative px-2">
                        {revenueData.map((data, i) => (
                            <div
                                key={data.day}
                                className="flex-1 flex flex-col items-center gap-4 group"
                            >
                                <div className="w-full relative cursor-pointer">
                                    <div
                                        className="w-full bg-gradient-to-t from-primary-600 to-primary-400 dark:from-primary-700 dark:to-primary-500 rounded-2xl transition-all duration-700 group-hover:scale-x-110 shadow-lg"
                                        style={{ height: `${(data.value / maxValue) * 200}px`, animationDelay: `${(i + 1) * 50}ms` }}
                                    />
                                    {/* Tooltip */}
                                    <div className="absolute -top-12 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-all duration-300 bg-slate-900 dark:bg-white text-white dark:text-slate-900 text-[10px] font-black px-3 py-1.5 rounded-xl shadow-2xl whitespace-nowrap scale-90 group-hover:scale-100 z-10 border border-slate-700 dark:border-slate-100">
                                        {formatCurrency(data.value)}
                                    </div>
                                </div>
                                <span className="text-[10px] font-black uppercase tracking-tighter text-slate-400 dark:text-slate-500">{data.day}</span>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Top Services */}
                <div className="card-elevated p-8 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-xl" style={{ animationDelay: '450ms' }}>
                    <div className="flex items-center justify-between mb-8">
                        <div>
                            <h3 className="text-xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                                Premier Services
                            </h3>
                            <p className="text-xs font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500 mt-1">Growth Tier List</p>
                        </div>
                        <div className="p-2 bg-amber-50 dark:bg-amber-900/20 rounded-xl">
                            <Sparkles className="h-5 w-5 text-amber-500" />
                        </div>
                    </div>

                    <div className="space-y-6">
                        {topServices.map((service, i) => (
                            <div
                                key={service.name}
                                className="flex items-center gap-4 group"
                            >
                                <div className="w-10 h-10 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-100 dark:border-slate-700 flex items-center justify-center text-xs font-black text-slate-400 dark:text-slate-500 group-hover:bg-primary-50 dark:group-hover:bg-primary-900/20 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-all">
                                    0{i + 1}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className="font-bold text-slate-900 dark:text-white text-sm truncate group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors uppercase tracking-tight">{service.name}</p>
                                    <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 tracking-widest uppercase">{service.bookings} Reservations</p>
                                </div>
                                <div className="text-right">
                                    <p className="font-black text-slate-900 dark:text-white text-sm">{formatCurrency(service.revenue)}</p>
                                    <p className={cn(
                                        'text-[10px] font-black flex items-center justify-end gap-0.5 uppercase tracking-widest',
                                        service.growth >= 0 ? 'text-emerald-500' : 'text-rose-500'
                                    )}>
                                        {service.growth >= 0 ? '+' : ''}{service.growth}%
                                        <ArrowUpRight className={cn('h-3 w-3', service.growth < 0 && 'rotate-90')} />
                                    </p>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Additional Insights */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8 pb-4">
                {/* Peak Hours */}
                <div className="card-elevated p-8 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-xl" style={{ animationDelay: '500ms' }}>
                    <div className="flex items-center gap-4 mb-8">
                        <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-2xl">
                            <Clock className="h-6 w-6 text-blue-600 dark:text-blue-400" />
                        </div>
                        <div>
                            <h3 className="text-lg font-bold text-slate-900 dark:text-white tracking-tight">Peak Velocity</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-0.5">Booking Density</p>
                        </div>
                    </div>
                    <div className="space-y-5">
                        {peakHours.map((slot, i) => (
                            <div key={slot.time} className="space-y-2">
                                <div className="flex justify-between text-[11px] font-bold uppercase tracking-tight">
                                    <span className="text-slate-700 dark:text-slate-300">{slot.time}</span>
                                    <span className="text-slate-900 dark:text-white bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-md">{slot.bookings}</span>
                                </div>
                                <div className="h-2.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden shadow-inner">
                                    <div
                                        className="h-full bg-gradient-to-r from-blue-400 to-blue-600 rounded-full transition-all duration-1000 shadow-[0_0_12px_rgba(37,99,235,0.3)]"
                                        style={{ width: `${slot.percent}%`, animationDelay: `${i * 100}ms` }}
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Client Retention */}
                <div className="card-elevated p-8 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-xl" style={{ animationDelay: '550ms' }}>
                    <div className="flex items-center gap-4 mb-8">
                        <div className="p-3 bg-emerald-50 dark:bg-emerald-900/20 rounded-2xl">
                            <Users className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />
                        </div>
                        <div>
                            <h3 className="text-lg font-bold text-slate-900 dark:text-white tracking-tight">Loyalty Index</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-0.5">Retention Rate</p>
                        </div>
                    </div>
                    <div className="flex items-center justify-center py-2">
                        <div className="relative w-40 h-40">
                            {/* Circular progress */}
                            <svg className="w-full h-full transform -rotate-90 filter drop-shadow-xl">
                                <circle
                                    cx="80"
                                    cy="80"
                                    r="68"
                                    fill="none"
                                    stroke="currentColor"
                                    className="text-slate-100 dark:text-slate-800"
                                    strokeWidth="14"
                                />
                                <circle
                                    cx="80"
                                    cy="80"
                                    r="68"
                                    fill="none"
                                    stroke="url(#gradient-Reports)"
                                    strokeWidth="14"
                                    strokeLinecap="round"
                                    strokeDasharray={`${(retention / 100) * 427} ${427}`}
                                    className="transition-all duration-1000 ease-out"
                                />
                                <defs>
                                    <linearGradient id="gradient-Reports" x1="0%" y1="0%" x2="100%" y2="0%">
                                        <stop offset="0%" stopColor="#10b981" />
                                        <stop offset="100%" stopColor="#34d399" />
                                    </linearGradient>
                                </defs>
                            </svg>
                            <div className="absolute inset-0 flex flex-col items-center justify-center">
                                <span className="text-4xl font-black text-slate-900 dark:text-white tracking-tighter">{retention}%</span>
                                <span className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1">Returning</span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Revenue by Channel */}
                <div className="card-elevated p-8 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-xl" style={{ animationDelay: '600ms' }}>
                    <div className="flex items-center gap-4 mb-8">
                        <div className="p-3 bg-violet-50 dark:bg-violet-900/20 rounded-2xl">
                            <PieChart className="h-6 w-6 text-violet-600 dark:text-violet-400" />
                        </div>
                        <div>
                            <h3 className="text-lg font-bold text-slate-900 dark:text-white tracking-tight">Origin Analysis</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-0.5">Booking Sources</p>
                        </div>
                    </div>
                    <div className="space-y-4">
                        {channelData.map((item) => (
                            <div key={item.channel} className="group">
                                <div className="flex items-center justify-between mb-2">
                                    <div className="flex items-center gap-3">
                                        <div className={cn('w-3 h-3 rounded-full shadow-sm transition-transform group-hover:scale-125', item.color)} />
                                        <span className="text-[11px] font-bold text-slate-600 dark:text-slate-400 group-hover:text-slate-900 dark:group-hover:text-white transition-colors">{item.channel}</span>
                                    </div>
                                    <span className="text-xs font-black text-slate-900 dark:text-white">{item.percent}%</span>
                                </div>
                                <div className="h-1.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                                    <div 
                                        className={cn('h-full rounded-full transition-all duration-1000', item.color)}
                                        style={{ width: `${item.percent}%` }}
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
