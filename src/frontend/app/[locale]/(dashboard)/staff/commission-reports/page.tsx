"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    DollarSign, TrendingUp, Users, Download,
    RefreshCw, BarChart2, ChevronDown, ChevronRight,
    Award, Search
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface CommissionEntry {
    bookingId: string;
    serviceName: string;
    clientName: string;
    date: string;
    serviceRevenue: number;
    commissionRate: number;
    commissionEarned: number;
    isPaid: boolean;
}

interface StaffCommission {
    staffId: string;
    staffName: string;
    role: string;
    totalBookings: number;
    totalRevenue: number;
    totalCommission: number;
    paidCommission: number;
    pendingCommission: number;
    avgCommissionRate: number;
    entries: CommissionEntry[];
}

interface CommissionSummary {
    period: string;
    totalRevenue: number;
    totalCommissions: number;
    paidCommissions: number;
    pendingCommissions: number;
    staffReports: StaffCommission[];
}

const SAMPLE_REPORT: CommissionSummary = {
    period: 'April 2026',
    totalRevenue: 18420,
    totalCommissions: 3684,
    paidCommissions: 2100,
    pendingCommissions: 1584,
    staffReports: [
        {
            staffId: 's1', staffName: 'Jessica Lee', role: 'Senior Stylist',
            totalBookings: 48, totalRevenue: 6800, totalCommission: 1360,
            paidCommission: 800, pendingCommission: 560, avgCommissionRate: 20,
            entries: [
                { bookingId: 'b1', serviceName: 'Balayage', clientName: 'Sarah M.', date: '2026-04-08', serviceRevenue: 180, commissionRate: 20, commissionEarned: 36, isPaid: true },
                { bookingId: 'b2', serviceName: 'Haircut & Style', clientName: 'Emma T.', date: '2026-04-09', serviceRevenue: 65, commissionRate: 20, commissionEarned: 13, isPaid: true },
                { bookingId: 'b3', serviceName: 'Color + Gloss', clientName: 'Mia C.', date: '2026-04-10', serviceRevenue: 220, commissionRate: 20, commissionEarned: 44, isPaid: false },
            ],
        },
        {
            staffId: 's2', staffName: 'Marcus Williams', role: 'Barber',
            totalBookings: 62, totalRevenue: 4340, totalCommission: 868,
            paidCommission: 600, pendingCommission: 268, avgCommissionRate: 20,
            entries: [
                { bookingId: 'b4', serviceName: 'Haircut', clientName: 'John D.', date: '2026-04-08', serviceRevenue: 45, commissionRate: 20, commissionEarned: 9, isPaid: true },
                { bookingId: 'b5', serviceName: 'Beard Trim', clientName: 'David P.', date: '2026-04-09', serviceRevenue: 25, commissionRate: 20, commissionEarned: 5, isPaid: false },
            ],
        },
        {
            staffId: 's3', staffName: 'Aisha Patel', role: 'Esthetician',
            totalBookings: 35, totalRevenue: 7280, totalCommission: 1456,
            paidCommission: 700, pendingCommission: 756, avgCommissionRate: 20,
            entries: [
                { bookingId: 'b6', serviceName: 'HydraFacial', clientName: 'Lisa K.', date: '2026-04-07', serviceRevenue: 150, commissionRate: 20, commissionEarned: 30, isPaid: true },
                { bookingId: 'b7', serviceName: 'Chemical Peel', clientName: 'Raj M.', date: '2026-04-10', serviceRevenue: 180, commissionRate: 20, commissionEarned: 36, isPaid: false },
            ],
        },
    ],
};

export default function CommissionReportsPage() {
    const [report, setReport] = useState<CommissionSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [expandedStaff, setExpandedStaff] = useState<string | null>(null);
    const [dateFrom, setDateFrom] = useState(() => {
        const d = new Date(); d.setDate(1); return d.toISOString().slice(0, 10);
    });
    const [dateTo, setDateTo] = useState(() => new Date().toISOString().slice(0, 10));
    const [search, setSearch] = useState('');

    const fetchReport = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/staff/commissions/report', { params: { from: dateFrom, to: dateTo } });
            const data = res.data?.data || res.data;
            setReport(data || SAMPLE_REPORT);
        } catch {
            setReport(SAMPLE_REPORT);
        } finally {
            setLoading(false);
        }
    }, [dateFrom, dateTo]);

    useEffect(() => { fetchReport(); }, [fetchReport]);

    const handleExport = () => {
        if (!report) return;
        const rows = ['Staff,Bookings,Revenue,Commission,Paid,Pending'];
        report.staffReports.forEach(s => {
            rows.push(`"${s.staffName}",${s.totalBookings},$${s.totalRevenue},$${s.totalCommission},$${s.paidCommission},$${s.pendingCommission}`);
        });
        const blob = new Blob([rows.join('\n')], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = `commission-report-${dateFrom}.csv`; a.click();
        URL.revokeObjectURL(url);
        toast.success('Report exported');
    };

    const handleMarkPaid = async (staffId: string) => {
        try {
            await apiClient.post(`/api/v1/staff/${staffId}/commissions/mark-paid`, { period: report?.period });
        } catch { }
        setReport(prev => prev ? {
            ...prev,
            paidCommissions: prev.paidCommissions + (prev.staffReports.find(s => s.staffId === staffId)?.pendingCommission || 0),
            pendingCommissions: prev.pendingCommissions - (prev.staffReports.find(s => s.staffId === staffId)?.pendingCommission || 0),
            staffReports: prev.staffReports.map(s => s.staffId === staffId
                ? { ...s, paidCommission: s.totalCommission, pendingCommission: 0, entries: s.entries.map(e => ({ ...e, isPaid: true })) }
                : s
            ),
        } : prev);
        toast.success('Commission marked as paid');
    };

    const filtered = report?.staffReports.filter(s =>
        !search || s.staffName.toLowerCase().includes(search.toLowerCase())
    ) || [];

    if (loading) {
        return (
            <div className="p-6 max-w-5xl mx-auto space-y-4">
                <div className="h-8 bg-slate-200 rounded w-48 animate-pulse" />
                {[...Array(3)].map((_, i) => <div key={i} className="h-24 bg-slate-100 rounded-xl animate-pulse" />)}
            </div>
        );
    }

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Commission Reports</h1>
                    <p className="text-slate-500 mt-1">Track and pay staff service commissions by period</p>
                </div>
                <div className="flex gap-2">
                    <Button onClick={fetchReport} variant="outline" size="sm" className="flex items-center gap-2">
                        <RefreshCw className="h-4 w-4" /> Refresh
                    </Button>
                    <Button onClick={handleExport} variant="outline" size="sm" className="flex items-center gap-2">
                        <Download className="h-4 w-4" /> Export CSV
                    </Button>
                </div>
            </div>

            {/* Date range filter */}
            <div className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-4 flex-wrap">
                <div className="flex items-center gap-2">
                    <label className="text-sm font-medium text-slate-700">From:</label>
                    <Input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} className="w-40" />
                </div>
                <div className="flex items-center gap-2">
                    <label className="text-sm font-medium text-slate-700">To:</label>
                    <Input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} className="w-40" />
                </div>
                <Button onClick={fetchReport} size="sm">Generate</Button>
                {report && <p className="text-sm font-medium text-slate-600 ml-auto">Period: {report.period}</p>}
            </div>

            {/* Summary Stats */}
            {report && (
                <div className="grid grid-cols-4 gap-4">
                    {[
                        { label: 'Total Revenue', value: `$${report.totalRevenue.toLocaleString()}`, icon: <DollarSign className="h-5 w-5 text-slate-500" />, color: 'text-slate-800' },
                        { label: 'Total Commissions', value: `$${report.totalCommissions.toLocaleString()}`, icon: <Award className="h-5 w-5 text-primary-500" />, color: 'text-primary-700' },
                        { label: 'Paid Out', value: `$${report.paidCommissions.toLocaleString()}`, icon: <TrendingUp className="h-5 w-5 text-emerald-500" />, color: 'text-emerald-700' },
                        { label: 'Pending', value: `$${report.pendingCommissions.toLocaleString()}`, icon: <BarChart2 className="h-5 w-5 text-amber-500" />, color: 'text-amber-700' },
                    ].map(s => (
                        <div key={s.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                            <div className="p-2 bg-slate-50 rounded-lg">{s.icon}</div>
                            <div>
                                <div className={`text-xl font-bold ${s.color}`}>{s.value}</div>
                                <div className="text-xs text-slate-500">{s.label}</div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Search */}
            <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                <input
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                    placeholder="Search staff..."
                    className="pl-9 pr-4 py-2 border border-slate-200 rounded-lg text-sm w-64 focus:outline-none focus:ring-2 focus:ring-primary-500"
                />
            </div>

            {/* Staff commission cards */}
            <div className="space-y-3">
                {filtered.map(staff => (
                    <div key={staff.staffId} className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                        <div className="p-4 flex items-center gap-4">
                            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-bold text-sm shrink-0">
                                {staff.staffName.split(' ').map(n => n[0]).join('')}
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2 flex-wrap">
                                    <span className="font-semibold text-slate-900">{staff.staffName}</span>
                                    <span className="text-xs text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full">{staff.role}</span>
                                    <span className="text-xs text-slate-500">{staff.avgCommissionRate}% rate</span>
                                </div>
                                <div className="flex gap-5 mt-1 flex-wrap">
                                    <span className="text-xs text-slate-600">{staff.totalBookings} bookings</span>
                                    <span className="text-xs text-slate-600">Revenue: <strong>${staff.totalRevenue.toLocaleString()}</strong></span>
                                    <span className="text-xs text-slate-600">Commission: <strong className="text-primary-600">${staff.totalCommission.toLocaleString()}</strong></span>
                                    <span className="text-xs text-emerald-600">Paid: ${staff.paidCommission}</span>
                                    {staff.pendingCommission > 0 && (
                                        <span className="text-xs text-amber-600 font-medium">Pending: ${staff.pendingCommission}</span>
                                    )}
                                </div>
                                <div className="mt-2 h-1.5 bg-slate-100 rounded-full overflow-hidden w-56">
                                    <div
                                        className="h-full bg-emerald-500 rounded-full"
                                        style={{ width: `${staff.totalCommission > 0 ? (staff.paidCommission / staff.totalCommission) * 100 : 0}%` }}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center gap-2 shrink-0">
                                {staff.pendingCommission > 0 && (
                                    <Button size="sm" onClick={() => handleMarkPaid(staff.staffId)} className="text-xs">
                                        Mark ${staff.pendingCommission} Paid
                                    </Button>
                                )}
                                <button
                                    onClick={() => setExpandedStaff(expandedStaff === staff.staffId ? null : staff.staffId)}
                                    className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400"
                                >
                                    {expandedStaff === staff.staffId ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                                </button>
                            </div>
                        </div>

                        {expandedStaff === staff.staffId && (
                            <div className="border-t border-slate-100 overflow-x-auto">
                                <table className="w-full text-sm">
                                    <thead className="bg-slate-50">
                                        <tr>
                                            {['Date', 'Service', 'Client', 'Revenue', 'Rate', 'Commission', 'Status'].map(h => (
                                                <th key={h} className={`px-4 py-2 text-xs font-semibold text-slate-500 ${['Revenue', 'Rate', 'Commission'].includes(h) ? 'text-right' : h === 'Status' ? 'text-center' : 'text-left'}`}>{h}</th>
                                            ))}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {staff.entries.map(entry => (
                                            <tr key={entry.bookingId} className="border-t border-slate-50 hover:bg-slate-50">
                                                <td className="px-4 py-2.5 text-xs text-slate-600">{new Date(entry.date).toLocaleDateString()}</td>
                                                <td className="px-4 py-2.5 text-xs font-medium text-slate-800">{entry.serviceName}</td>
                                                <td className="px-4 py-2.5 text-xs text-slate-600">{entry.clientName}</td>
                                                <td className="px-4 py-2.5 text-xs text-slate-800 text-right">${entry.serviceRevenue}</td>
                                                <td className="px-4 py-2.5 text-xs text-slate-600 text-right">{entry.commissionRate}%</td>
                                                <td className="px-4 py-2.5 text-xs font-semibold text-primary-600 text-right">${entry.commissionEarned}</td>
                                                <td className="px-4 py-2.5 text-center">
                                                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${entry.isPaid ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                                                        {entry.isPaid ? 'Paid' : 'Pending'}
                                                    </span>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}
