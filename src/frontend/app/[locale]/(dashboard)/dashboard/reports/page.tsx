'use client';

import { useState, useEffect } from 'react';
import {
    TrendingUp,
    TrendingDown,
    Calendar,
    Users,
    DollarSign,
    BarChart3,
    Download,
    RefreshCw,
    Loader2
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { analyticsApi, RevenueAnalytics, BookingAnalytics, ClientAnalytics, ServiceAnalytics, StaffAnalytics } from '@/lib/api.analytics';
import { toast } from 'sonner';

interface ReportCard {
    title: string;
    value: string | number;
    change?: number;
    changeLabel?: string;
    icon: React.ReactNode;
}

const reportTypes = [
    { id: 'revenue', name: 'Revenue Report', description: 'Revenue breakdown by period' },
    { id: 'bookings', name: 'Bookings Report', description: 'Booking analytics and trends' },
    { id: 'clients', name: 'Clients Report', description: 'Client acquisition and retention' },
    { id: 'staff', name: 'Staff Performance', description: 'Staff productivity and revenue' },
    { id: 'services', name: 'Services Report', description: 'Service popularity and revenue' },
];

export default function ReportsPage() {
    const [period, setPeriod] = useState('30d');
    const [selectedReport, setSelectedReport] = useState('revenue');
    const [loading, setLoading] = useState(true);

    // Data states
    const [kpis, setKpis] = useState<ReportCard[]>([]);
    const [tableData, setTableData] = useState<any[]>([]); // Dynamic table data
    const [chartData, setChartData] = useState<any[]>([]);

    useEffect(() => {
        fetchData();
    }, [period, selectedReport]);

    const fetchData = async () => {
        try {
            setLoading(true);

            // Parallel fetch for KPIs (always show revenue/bookings summary if possible, or context specific)
            // For now, let's just fetch the specific report data

            switch (selectedReport) {
                case 'revenue':
                    const rev = await analyticsApi.getRevenue(period);
                    updateRevenueView(rev.data);
                    break;
                case 'bookings':
                    const bookings = await analyticsApi.getBookings(period);
                    updateBookingsView(bookings.data);
                    break;
                case 'clients':
                    const clients = await analyticsApi.getClients(period);
                    updateClientsView(clients.data);
                    break;
                case 'staff':
                    const staff = await analyticsApi.getStaff(period);
                    updateStaffView(staff.data);
                    break;
                case 'services':
                    const services = await analyticsApi.getServices(period);
                    updateServicesView(services.data);
                    break;
            }
        } catch (error) {
            console.error('Failed to fetch analytics', error);
            toast.error('Failed to load report data');
        } finally {
            setLoading(false);
        }
    };

    const updateRevenueView = (data: RevenueAnalytics) => {
        setKpis([
            {
                title: 'Total Revenue',
                value: formatCurrency(data.totalRevenue),
                change: data.growthRate,
                changeLabel: 'vs previous period',
                icon: <DollarSign className="h-6 w-6" />
            },
            {
                title: 'Avg. Daily Revenue',
                value: formatCurrency(data.averageDaily),
                icon: <BarChart3 className="h-6 w-6" />
            }
        ]);
        setChartData(data.data);
        // Table could be daily breakdown
        setTableData(data.data.map(d => ({ col1: d.date, col2: formatCurrency(d.revenue) })).reverse());
    };

    const updateBookingsView = (data: BookingAnalytics) => {
        setKpis([
            {
                title: 'Total Bookings',
                value: data.totalBookings,
                icon: <Calendar className="h-6 w-6" />
            },
            {
                title: 'Completion Rate',
                value: `${Math.round(data.completionRate)}%`,
                icon: <TrendingUp className="h-6 w-6" />
            },
            {
                title: 'Avg. Value',
                value: formatCurrency(data.averageValue),
                icon: <DollarSign className="h-6 w-6" />
            }
        ]);
        setTableData(data.peakHours.map(p => ({ col1: p.hour, col2: `${p.bookings} bookings` })));
    };

    const updateClientsView = (data: ClientAnalytics) => {
        setKpis([
            {
                title: 'Total Clients',
                value: data.totalClients,
                icon: <Users className="h-6 w-6" />
            },
            {
                title: 'New Clients',
                value: data.newClients,
                icon: <Users className="h-6 w-6" />
            },
            {
                title: 'Returning',
                value: data.returningClients,
                icon: <RefreshCw className="h-6 w-6" />
            },
            {
                title: 'Avg. LTV',
                value: formatCurrency(data.averageLifetimeValue),
                icon: <DollarSign className="h-6 w-6" />
            }
        ]);
        setTableData([]); // Clients list requires separate API or privacy consideration
    };

    const updateStaffView = (data: StaffAnalytics) => {
        setKpis([
            // Could aggregate total bookings/revenue
        ]);
        setTableData(data.topPerformers.map(s => ({
            col1: s.name,
            col2: `${s.bookings} bookings`,
            col3: formatCurrency(s.revenue)
        })));
    };

    const updateServicesView = (data: ServiceAnalytics) => {
        setTableData(data.topServices.map(s => ({
            col1: s.name,
            col2: `${s.bookings} bookings`,
            col3: formatCurrency(s.revenue)
        })));
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Reports & Analytics</h1>
                    <p className="text-gray-500 mt-1">Track your business performance</p>
                </div>
                <div className="flex items-center gap-3">
                    <select
                        value={period}
                        onChange={(e) => setPeriod(e.target.value)}
                        className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                    >
                        <option value="7d">Last 7 days</option>
                        <option value="30d">Last 30 days</option>
                        <option value="90d">Last 90 days</option>
                    </select>
                    <button onClick={fetchData} className="p-2 border border-gray-300 rounded-lg hover:bg-gray-50">
                        <RefreshCw className="h-5 w-5 text-gray-500" />
                    </button>
                    <button className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600">
                        <Download className="h-4 w-4" />
                        Export
                    </button>
                </div>
            </div>

            {loading ? (
                <div className="flex justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin text-primary-500" />
                </div>
            ) : (
                <>
                    {/* KPI Cards */}
                    {kpis.length > 0 && (
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                            {kpis.map((kpi, index) => (
                                <div
                                    key={index}
                                    className="bg-white rounded-xl shadow-sm border border-gray-200 p-6"
                                >
                                    <div className="flex items-center justify-between mb-4">
                                        <div className="p-2 bg-primary-50 rounded-lg text-primary-500">
                                            {kpi.icon}
                                        </div>
                                        {kpi.change !== undefined && (
                                            <div
                                                className={cn(
                                                    'flex items-center gap-1 text-sm font-medium',
                                                    kpi.change >= 0 ? 'text-green-600' : 'text-red-600'
                                                )}
                                            >
                                                {kpi.change >= 0 ? (
                                                    <TrendingUp className="h-4 w-4" />
                                                ) : (
                                                    <TrendingDown className="h-4 w-4" />
                                                )}
                                                {Math.abs(Math.round(kpi.change))}%
                                            </div>
                                        )}
                                    </div>
                                    <p className="text-sm text-gray-500 mb-1">{kpi.title}</p>
                                    <p className="text-2xl font-bold text-gray-900">{kpi.value}</p>
                                    {kpi.changeLabel && (
                                        <p className="text-xs text-gray-400 mt-1">{kpi.changeLabel}</p>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}

                    {/* Report Types */}
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
                        <h2 className="text-lg font-semibold text-gray-900 mb-4">Available Reports</h2>
                        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                            {reportTypes.map((report) => (
                                <button
                                    key={report.id}
                                    onClick={() => setSelectedReport(report.id)}
                                    className={cn(
                                        'p-4 rounded-lg border-2 text-left transition-all',
                                        selectedReport === report.id
                                            ? 'border-primary-500 bg-primary-50'
                                            : 'border-gray-200 hover:border-gray-300'
                                    )}
                                >
                                    <h3 className="font-medium text-gray-900">{report.name}</h3>
                                    <p className="text-sm text-gray-500 mt-1">{report.description}</p>
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Dynamic Table */}
                    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
                        <h2 className="text-lg font-semibold text-gray-900 mb-4">
                            {reportTypes.find((r) => r.id === selectedReport)?.name} Data
                        </h2>
                        {tableData.length > 0 ? (
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead>
                                        <tr className="text-left text-sm text-gray-500 border-b border-gray-200">
                                            <th className="pb-3 font-medium">Metric / Name</th>
                                            <th className="pb-3 font-medium">Value / Count</th>
                                            {tableData[0].col3 && <th className="pb-3 font-medium">Revenue / Additional</th>}
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-100">
                                        {tableData.map((row, i) => (
                                            <tr key={i} className="text-sm">
                                                <td className="py-3 font-medium text-gray-900">{row.col1}</td>
                                                <td className="py-3 text-gray-600">{row.col2}</td>
                                                {row.col3 && <td className="py-3 text-gray-600">{row.col3}</td>}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <p className="text-center py-8 text-gray-500">No data available for this report.</p>
                        )}
                    </div>
                </>
            )}
        </div>
    );
}
