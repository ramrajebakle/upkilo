'use client';

import { cn, formatCurrency } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import { useEffect, useState } from 'react';
import { DollarSign, Calendar, Users, Target, BarChart3, Download, PieChart, Clock, Zap, Eye, TrendingUp } from 'lucide-react';
import { PageHeader } from '@/components/ui/PageHeader';
import { StatsGrid } from '@/components/ui/StatsGrid';
import {
    RevenueAreaChart,
    BookingsBarChart,
    ServiceDonutChart,
    PeakHoursChart,
    StaffPerformanceChart,
    RetentionLineChart,
} from '@/components/charts';

export default function AnalyticsPage() {
    const [dateRange, setDateRange] = useState('30days');
    const [loading, setLoading] = useState(true);
    const [dashboardMetrics, setDashboardMetrics] = useState<any>(null);
    const [revenueData, setRevenueData] = useState<any[]>([]);
    const [bookingsData, setBookingsData] = useState<any>(null);
    const [serviceStats, setServiceStats] = useState<any[]>([]);
    const [staffStats, setStaffStats] = useState<any[]>([]);
    const [bookingsTrend, setBookingsTrend] = useState<any[]>([]);

    useEffect(() => {
        const fetchAnalytics = async () => {
            setLoading(true);
            try {
                const period = dateRange === '7days' ? '7d' : dateRange === '90days' ? '90d' : dateRange === 'year' ? '365d' : '30d';

                const [dashRes, revRes, bookRes, servRes, staffRes] = await Promise.all([
                    apiClient.get('/api/v1/analytics/dashboard'),
                    apiClient.get(`/api/v1/analytics/revenue?period=${period}`),
                    apiClient.get(`/api/v1/analytics/bookings?period=${period}`),
                    apiClient.get(`/api/v1/analytics/services?period=${period}`),
                    apiClient.get(`/api/v1/analytics/staff?period=${period}`)
                ]);

                setDashboardMetrics(dashRes.data);

                // Revenue area chart data
                const rawRev = revRes.data?.data ?? [];
                setRevenueData(rawRev.map((d: any) => ({
                    label: d.date?.split('-')[2] ?? d.label ?? '',
                    revenue: d.revenue ?? 0,
                    expenses: d.expenses ?? 0,
                })));

                setBookingsData(bookRes.data);

                // Bookings trend
                const rawBook = bookRes.data?.trend ?? [];
                setBookingsTrend(rawBook.map((d: any) => ({
                    label: d.date?.split('-')[2] ?? d.label ?? '',
                    bookings: d.count ?? d.bookings ?? 0,
                    cancelled: d.cancelled ?? 0,
                })));

                const services = servRes.data?.topServices ?? [];
                setServiceStats(services.map((s: any) => ({
                    name: s.name,
                    value: s.bookings,
                })));

                setStaffStats(staffRes.data?.topPerformers ?? []);
            } catch (err) {
                console.error('Failed to fetch analytics:', err);
            } finally {
                setLoading(false);
            }
        };
        fetchAnalytics();
    }, [dateRange]);

    const stats = [
        {
            label: 'Total Revenue',
            value: formatCurrency(dashboardMetrics?.todayRevenue || 0),
            trend: `${Math.abs(dashboardMetrics?.revenueChange || 0)}%`,
            trendUp: (dashboardMetrics?.revenueChange || 0) >= 0,
            icon: DollarSign,
            color: 'emerald' as const,
        },
        {
            label: 'Total Bookings',
            value: dashboardMetrics?.todayBookings || 0,
            trend: `${Math.abs(dashboardMetrics?.bookingsChange || 0)}%`,
            trendUp: (dashboardMetrics?.bookingsChange || 0) >= 0,
            icon: Calendar,
            color: 'blue' as const,
        },
        {
            label: 'Active Clients',
            value: dashboardMetrics?.activeClients || 0,
            icon: Users,
            color: 'violet' as const,
        },
        {
            label: 'Avg. Booking Value',
            value: formatCurrency(bookingsData?.averageValue || 0),
            icon: Target,
            color: 'amber' as const,
        },
    ];

    const peakHours: any[] = bookingsData?.peakHours ?? [];

    // Retention data from API, fallback to dashboard metrics if available
    const [retentionData, setRetentionData] = useState<any[]>([]);

    useEffect(() => {
        const fetchRetention = async () => {
            try {
                const period = dateRange === '7days' ? '7d' : dateRange === '90days' ? '90d' : dateRange === 'year' ? '365d' : '30d';
                const res = await apiClient.get(`/api/v1/analytics/retention?period=${period}`);
                const data = res.data?.data ?? res.data ?? [];
                if (Array.isArray(data) && data.length > 0) {
                    setRetentionData(data.map((d: any) => ({
                        label: d.month || d.label || d.date?.split('-').slice(1).join('/') || '',
                        rate: d.rate ?? d.retentionRate ?? 0,
                    })));
                }
            } catch {
                // Retention endpoint may not exist — leave empty, chart will show nothing
            }
        };
        fetchRetention();
    }, [dateRange]);

    const RANGES = [
        { key: '7days', label: '7 Days' },
        { key: '30days', label: '30 Days' },
        { key: '90days', label: '90 Days' },
        { key: 'year', label: 'Year' },
    ];

    return (
        <div className="space-y-6">
            <PageHeader
                icon={BarChart3}
                iconGradient="from-primary-500 to-primary-600"
                iconShadow="shadow-primary-500/25"
                title="Analytics"
                description="Track your business performance and insights"
                actions={
                    <>
                        <div className="flex gap-2">
                            {RANGES.map((range) => (
                                <button
                                    key={range.key}
                                    onClick={() => setDateRange(range.key)}
                                    className={cn(
                                        'px-3 py-1.5 rounded-lg text-sm font-medium transition-all',
                                        dateRange === range.key
                                            ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                            : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700 hover:border-primary-300'
                                    )}
                                >
                                    {range.label}
                                </button>
                            ))}
                        </div>
                        <button className="btn btn-secondary">
                            <Download className="h-4 w-4" />
                            Export
                        </button>
                    </>
                }
            />

            {/* KPI cards */}
            <StatsGrid stats={stats} loading={loading} columns={4} />

            {/* Revenue + Bookings charts */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Revenue area chart */}
                <div className="lg:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 shadow-sm animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                    <div className="flex items-center justify-between mb-6">
                        <div>
                            <h2 className="text-lg font-bold text-slate-900 dark:text-white">Revenue Over Time</h2>
                            <p className="text-sm text-slate-500 dark:text-slate-400">Daily revenue breakdown and booking volume</p>
                        </div>
                        <div className="flex gap-2">
                            <button className="px-3 py-1 text-xs font-medium rounded-lg bg-primary-50 dark:bg-primary-500/10 text-primary-600 dark:text-primary-400 border border-primary-100 dark:border-primary-500/20">
                                Revenue
                            </button>
                            <button className="px-3 py-1 text-xs font-medium rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800">
                                Bookings
                            </button>
                        </div>
                    </div>
                    <RevenueAreaChart data={revenueData} height={260} />
                </div>

                {/* Service donut */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                By Service
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Booking distribution</p>
                        </div>
                        <PieChart className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <ServiceDonutChart data={serviceStats} height={160} />
                </div>
            </div>

            {/* Bookings bar + Peak hours + Staff */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Bookings bar */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                Bookings
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Daily booking count</p>
                        </div>
                        <Calendar className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <BookingsBarChart data={bookingsTrend} height={200} showCancelled />
                </div>

                {/* Peak hours */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                Peak Hours
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Busiest times of day</p>
                        </div>
                        <Clock className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <PeakHoursChart data={peakHours} height={180} />
                </div>

                {/* Staff performance */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '250ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                Top Performers
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Staff by revenue</p>
                        </div>
                        <Zap className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <StaffPerformanceChart data={staffStats} />
                </div>
            </div>

            {/* Retention + Quick Insights */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Retention line chart */}
                <div className="lg:col-span-2 card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '250ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                Client Retention
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Monthly retention rate vs 75% target</p>
                        </div>
                        <TrendingUp className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <RetentionLineChart data={retentionData} targetRate={75} />
                </div>

                {/* Quick insights */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                    <div className="flex items-center justify-between mb-5">
                        <div>
                            <h3 className="text-base font-semibold text-foreground">
                                Quick Insights
                            </h3>
                            <p className="text-xs text-foreground-secondary mt-0.5">Key metrics snapshot</p>
                        </div>
                        <Eye className="h-4 w-4 text-foreground-muted" />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                        {[
                            { label: 'Retention Rate', value: `${dashboardMetrics?.retentionRate || 78}%`, gradient: 'from-emerald-500 to-emerald-600', text: 'text-success-fg' },
                            { label: 'No-show Rate', value: `${dashboardMetrics?.noShowRate || 4.2}%`, gradient: 'from-rose-500 to-red-600', text: 'text-danger-fg' },
                            { label: 'Avg. Booking', value: formatCurrency(bookingsData?.averageValue || 0), gradient: 'from-blue-500 to-primary-600', text: 'text-blue-600' },
                            { label: 'Active Clients', value: (dashboardMetrics?.activeClients || 0).toLocaleString(), gradient: 'from-primary-500 to-primary-600', text: 'text-primary-600' },
                        ].map((metric, i) => (
                            <div
                                key={metric.label}
                                className="bg-muted rounded-xl p-3 text-center animate-fade-in border border-border-subtle hover:border-border transition-colors"
                                style={{ animationDelay: `${800 + i * 80}ms` }}
                            >
                                <p className={`text-xl font-bold ${metric.text}`}>
                                    {metric.value}
                                </p>
                                <p className="text-[10px] text-foreground-secondary mt-1 leading-tight">{metric.label}</p>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
