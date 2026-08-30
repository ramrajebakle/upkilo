'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
    Users,
    Filter,
    Search,
    Download,
    Star,
    DollarSign,
    Calendar,
    Tag,
    ChevronRight,
    ArrowRight
} from 'lucide-react';
import { cn, formatCurrency, formatDate } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

interface SegmentClient {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    status: string;
    totalSpent: number; // mapped from lifetimeValue
    lastVisit: string; // mapped from lastBookingAt
    loyaltyPoints: number;
    loyaltyTier: string;
    tags: string[];
}

export default function ClientSegmentsPage() {
    const [loading, setLoading] = useState(false);
    const [clients, setClients] = useState<SegmentClient[]>([]);
    const [totalcount, setTotalCount] = useState(0);
    const { error: toastError, success: toastSuccess } = useToast();

    // Filters
    const [filters, setFilters] = useState({
        minSpend: '',
        daysSinceVisit: '', // "30", "60", "90"
        visitOperator: 'gt', // "gt" (more than X days ago) or "lt" (less than X days ago)
        loyaltyTier: '',
        tag: ''
    });

    const handleSearch = async () => {
        setLoading(true);
        try {
            const payload: any = {};

            if (filters.minSpend) payload.minSpend = parseFloat(filters.minSpend);

            if (filters.daysSinceVisit) {
                const days = parseInt(filters.daysSinceVisit);
                if (filters.visitOperator === 'gt') {
                    payload.maxDaysSinceLastVisit = days; // Logic inverse: > 30 days ago means LastVisit date < Now-30
                } else {
                    payload.minDaysSinceLastVisit = days; // < 30 days ago means LastVisit date > Now-30
                }
            }

            if (filters.loyaltyTier) payload.loyaltyTier = filters.loyaltyTier;
            if (filters.tag) payload.tags = [filters.tag];

            const response = await api.clients.segment(payload);

            // Map backend response to UI
            const mappedClients = response.data.data.map((c: any) => ({
                id: c.id,
                firstName: c.firstName,
                lastName: c.lastName,
                email: c.email,
                phone: c.phone,
                status: c.status || 'Active',
                totalSpent: c.lifetimeValue || 0,
                lastVisit: c.lastBookingAt,
                loyaltyPoints: c.loyaltyPoints || 0,
                loyaltyTier: c.loyaltyTier || 'Bronze',
                tags: c.tags || []
            }));

            setClients(mappedClients);
            setTotalCount(response.data.total);

            if (mappedClients.length === 0) {
                toastSuccess('No clients found matching these criteria');
            }
        } catch (error) {
            console.error(error);
            toastError('Failed to run segmentation');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-7xl mx-auto">
            {/* Header */}
            <div className="flex items-center justify-between mb-8 animate-fade-in-up">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>
                        Client Segments
                    </h1>
                    <p className="text-slate-500 dark:text-slate-400">Filter and segment your client base specifically for marketing campaigns.</p>
                </div>
                <div className="flex gap-3">
                    <button className="btn btn-secondary dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-700">
                        <Download className="h-4 w-4" />
                        Export
                    </button>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                {/* Filters Sidebar */}
                <div className="lg:col-span-1 space-y-6 animate-fade-in">
                    <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-6 shadow-sm border border-slate-200">
                        <div className="flex items-center gap-2 mb-6">
                            <Filter className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                            <h2 className="font-semibold text-slate-900 dark:text-white">Filters</h2>
                        </div>

                        <div className="space-y-6">
                            {/* Spend */}
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Lifetime Spend</label>
                                <div className="relative">
                                    <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                    <input
                                        type="number"
                                        placeholder="Min. Amount"
                                        className="input pl-10 dark:bg-slate-800 dark:border-slate-700 dark:text-white dark:placeholder-slate-500 focus:ring-2 focus:ring-primary-500"
                                        value={filters.minSpend}
                                        onChange={(e) => setFilters({ ...filters, minSpend: e.target.value })}
                                    />
                                </div>
                            </div>

                            {/* Visit */}
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Last Visit</label>
                                <div className="flex gap-2 mb-2">
                                    <select
                                        className="input text-sm p-2 dark:bg-slate-800 dark:border-slate-700 dark:text-white"
                                        value={filters.visitOperator}
                                        onChange={(e) => setFilters({ ...filters, visitOperator: e.target.value })}
                                    >
                                        <option value="gt">More than</option>
                                        <option value="lt">Less than</option>
                                    </select>
                                </div>
                                <div className="relative">
                                    <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                    <select
                                        className="input pl-10 dark:bg-slate-800 dark:border-slate-700 dark:text-white"
                                        value={filters.daysSinceVisit}
                                        onChange={(e) => setFilters({ ...filters, daysSinceVisit: e.target.value })}
                                    >
                                        <option value="">Any time</option>
                                        <option value="30">30 Days Ago</option>
                                        <option value="60">60 Days Ago</option>
                                        <option value="90">90 Days Ago</option>
                                        <option value="180">6 Months Ago</option>
                                        <option value="365">1 Year Ago</option>
                                    </select>
                                </div>
                            </div>

                            {/* Loyalty */}
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Loyalty Tier</label>
                                <div className="relative">
                                    <Star className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                    <select
                                        className="input pl-10 dark:bg-slate-800 dark:border-slate-700 dark:text-white"
                                        value={filters.loyaltyTier}
                                        onChange={(e) => setFilters({ ...filters, loyaltyTier: e.target.value })}
                                    >
                                        <option value="">Any Tier</option>
                                        <option value="Bronze">Bronze</option>
                                        <option value="Silver">Silver</option>
                                        <option value="Gold">Gold</option>
                                        <option value="Platinum">Platinum</option>
                                    </select>
                                </div>
                            </div>

                            {/* Tag */}
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Has Tag</label>
                                <div className="relative">
                                    <Tag className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                    <input
                                        type="text"
                                        placeholder="Enter tag..."
                                        className="input pl-10 dark:bg-slate-800 dark:border-slate-700 dark:text-white dark:placeholder-slate-500 focus:ring-2 focus:ring-primary-500"
                                        value={filters.tag}
                                        onChange={(e) => setFilters({ ...filters, tag: e.target.value })}
                                    />
                                </div>
                            </div>

                            <button
                                onClick={handleSearch}
                                disabled={loading}
                                className="btn btn-primary w-full justify-center"
                            >
                                {loading ? 'Running...' : 'Run Segmentation'}
                            </button>
                        </div>
                    </div>
                </div>

                {/* Results */}
                <div className="lg:col-span-3 space-y-6 animate-fade-in" style={{ animationDelay: '0.1s' }}>
                    <div className="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
                        <div className="p-6 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
                            <div className="flex items-center gap-3">
                                <Users className="h-5 w-5 text-foreground-muted" />
                                <h3 className="font-semibold text-slate-900 dark:text-white">
                                    {clients.length > 0 ? `${clients.length} Matches Found` : 'Results'}
                                </h3>
                            </div>
                            {totalcount > clients.length && (
                                <span className="text-xs text-slate-500 dark:text-slate-400">Showing top 100 of {totalcount}</span>
                            )}
                        </div>

                        {clients.length === 0 ? (
                            <div className="p-12 text-center">
                                <Search className="h-12 w-12 text-slate-300 mx-auto mb-4" />
                                <h3 className="text-lg font-medium text-slate-900 dark:text-white">No segments run yet</h3>
                                <p className="text-slate-500 dark:text-slate-400 mt-1">Adjust filters on the left and click Run to see clients.</p>
                            </div>
                        ) : (
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead>
                                        <tr className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-100 dark:border-slate-800">
                                            <th className="text-left py-3 px-6 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase">Client</th>
                                            <th className="text-left py-3 px-6 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase">Loyalty</th>
                                            <th className="text-left py-3 px-6 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase">Spend</th>
                                            <th className="text-left py-3 px-6 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase">Last Visit</th>
                                            <th className="text-right py-3 px-6 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase">Action</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                                        {clients.map((client) => (
                                            <tr key={client.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors group">
                                                <td className="py-4 px-6">
                                                    <div>
                                                        <div className="font-medium text-slate-900 dark:text-white">{client.firstName} {client.lastName}</div>
                                                        <div className="text-sm text-slate-500 dark:text-slate-400">{client.email}</div>
                                                    </div>
                                                </td>
                                                <td className="py-4 px-6">
                                                    <div className="flex flex-col">
                                                        <span className={cn(
                                                            "w-fit px-2 py-0.5 rounded-full text-xs font-medium mb-1",
                                                            client.loyaltyTier === 'Platinum' ? 'bg-slate-800 text-white dark:bg-slate-700' :
                                                                client.loyaltyTier === 'Gold' ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400' :
                                                                    client.loyaltyTier === 'Silver' ? 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300' :
                                                                        'bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400'
                                                        )}>
                                                            {client.loyaltyTier}
                                                        </span>
                                                        <span className="text-xs text-slate-500 dark:text-slate-400">{client.loyaltyPoints} pts</span>
                                                    </div>
                                                </td>
                                                <td className="py-4 px-6">
                                                    <div className="font-medium text-slate-900 dark:text-white">{formatCurrency(client.totalSpent)}</div>
                                                </td>
                                                <td className="py-4 px-6">
                                                    <div className="text-sm text-slate-600 dark:text-slate-300">
                                                        {client.lastVisit ? formatDate(client.lastVisit) : 'Never'}
                                                    </div>
                                                </td>
                                                <td className="py-4 px-6 text-right">
                                                    <Link
                                                        href={`/clients/${client.id}`}
                                                        className="inline-flex items-center justify-center p-2 text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 rounded-lg transition-colors opacity-0 group-hover:opacity-100"
                                                    >
                                                        <ChevronRight className="h-4 w-4" />
                                                    </Link>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
