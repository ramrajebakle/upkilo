'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Search,
    Plus,
    Filter,
    Calendar,
    Clock,
    MoreVertical,
    ChevronRight,
    User,
    CheckCircle,
    XCircle,
    AlertCircle,
    Eye,
    Edit,
    CalendarDays,
    DollarSign,
    TrendingUp,
} from 'lucide-react';
import { cn, formatDate, formatTime, formatCurrency } from '@/lib/utils';

import api from '@/lib/api';
import { SkeletonCard } from '@/components/ui';
import { BulkActionsBar } from '@/components/ui/BulkActionsBar';
import { Trash2, Ban, CheckSquare } from 'lucide-react';
import { toast } from 'sonner';

interface Booking {
    id: string;
    clientName: string;
    clientEmail: string;
    clientInitials: string;
    serviceName: string;
    staffName: string;
    startTime: string;
    endTime: string;
    status: 'confirmed' | 'pending' | 'completed' | 'cancelled' | 'no_show';
    price: number;
}

export default function BookingsPage() {
    const [bookings, setBookings] = useState<Booking[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState('all');
    const [dateFilter, setDateFilter] = useState('today');
    const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

    useEffect(() => {
        const fetchBookings = async () => {
            setLoading(true);
            try {
                const now = new Date();
                let from: Date | undefined;
                let to: Date | undefined;
                if (dateFilter === 'today') {
                    from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
                    to = new Date(from); to.setDate(to.getDate() + 1);
                } else if (dateFilter === 'week') {
                    from = new Date(now); from.setDate(now.getDate() - now.getDay());
                    from.setHours(0, 0, 0, 0);
                    to = new Date(from); to.setDate(to.getDate() + 7);
                } else if (dateFilter === 'month') {
                    from = new Date(now.getFullYear(), now.getMonth(), 1);
                    to = new Date(now.getFullYear(), now.getMonth() + 1, 1);
                }
                const response = await api.bookings.list({
                    limit: 100,
                    status: statusFilter === 'all' ? undefined : statusFilter,
                    ...(from && { startDate: from.toISOString() }),
                    ...(to && { endDate: to.toISOString() }),
                } as any);
                setBookings(response.data.data ?? []);
            } catch (error) {
                console.error('Failed to fetch bookings', error);
                setBookings([]);
            } finally {
                setLoading(false);
            }
        };
        fetchBookings();
    }, [statusFilter, dateFilter]);

    const filteredBookings = bookings.filter(booking => {
        const matchesSearch = booking.clientName.toLowerCase().includes(searchQuery.toLowerCase()) ||
            booking.serviceName.toLowerCase().includes(searchQuery.toLowerCase());
        const matchesStatus = statusFilter === 'all' || booking.status === statusFilter;
        return matchesSearch && matchesStatus;
    });

    // Stats
    const todayBookings = bookings.length;
    const confirmedCount = bookings.filter(b => b.status === 'confirmed').length;
    const pendingCount = bookings.filter(b => b.status === 'pending').length;
    const todayRevenue = bookings.filter(b => b.status !== 'cancelled').reduce((sum, b) => sum + b.price, 0);

    const toggleSelect = (id: string) => {
        setSelectedIds(prev => {
            const next = new Set(prev);
            if (next.has(id)) { next.delete(id); } else { next.add(id); }
            return next;
        });
    };

    const handleBulkCancel = async () => {
        try {
            await Promise.all([...selectedIds].map(id =>
                api.bookings.update(id, { status: 'cancelled' } as any).catch(() => null)
            ));
            setBookings(prev => prev.map(b => selectedIds.has(b.id) ? { ...b, status: 'cancelled' as const } : b));
            setSelectedIds(new Set());
            toast.success(`${selectedIds.size} bookings cancelled`);
        } catch { toast.error('Failed to cancel bookings'); }
    };

    const handleBulkDelete = async () => {
        if (!confirm(`Delete ${selectedIds.size} booking(s)? This cannot be undone.`)) return;
        try {
            await Promise.all([...selectedIds].map(id =>
                api.bookings.cancel(id).catch(() => null)
            ));
            setBookings(prev => prev.filter(b => !selectedIds.has(b.id)));
            setSelectedIds(new Set());
            toast.success(`${selectedIds.size} bookings deleted`);
        } catch { toast.error('Failed to delete bookings'); }
    };

    const getStatusStyles = (status: string) => {
        switch (status) {
            case 'confirmed': return { bg: 'bg-emerald-50 dark:bg-emerald-900/20', text: 'text-emerald-700 dark:text-emerald-400', icon: CheckCircle, dot: 'bg-emerald-500' };
            case 'pending': return { bg: 'bg-amber-50 dark:bg-amber-900/20', text: 'text-amber-700 dark:text-amber-400', icon: Clock, dot: 'bg-amber-500' };
            case 'completed': return { bg: 'bg-blue-50 dark:bg-blue-900/20', text: 'text-blue-700 dark:text-blue-400', icon: CheckCircle, dot: 'bg-blue-500' };
            case 'cancelled': return { bg: 'bg-red-50 dark:bg-red-900/20', text: 'text-red-700 dark:text-red-400', icon: XCircle, dot: 'bg-red-500' };
            case 'no_show': return { bg: 'bg-slate-100 dark:bg-slate-800', text: 'text-slate-600 dark:text-slate-400', icon: AlertCircle, dot: 'bg-slate-400' };
            default: return { bg: 'bg-slate-100 dark:bg-slate-800', text: 'text-slate-600 dark:text-slate-400', icon: Clock, dot: 'bg-slate-400' };
        }
    };

    const formatBookingTime = (start: string, end: string) => {
        const startDate = new Date(start);
        const endDate = new Date(end);
        return `${startDate.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })} - ${endDate.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })}`;
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div className="animate-fade-in-up">
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-xl shadow-lg shadow-blue-500/25">
                            <CalendarDays className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl lg:text-3xl font-bold text-slate-900 dark:text-white"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Bookings
                        </h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400">Manage appointments and scheduling</p>
                </div>
                <Link
                    href="/bookings/new"
                    className="btn btn-primary shadow-lg shadow-primary-500/25 animate-fade-in"
                    style={{ animationDelay: '100ms' }}
                >
                    <Plus className="h-5 w-5" />
                    New Booking
                </Link>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                    { label: 'Total', value: todayBookings, icon: CalendarDays, gradient: 'from-blue-500 to-indigo-600', shadow: 'shadow-blue-500/25' },
                    { label: 'Confirmed', value: confirmedCount, icon: CheckCircle, gradient: 'from-emerald-500 to-emerald-700', shadow: 'shadow-emerald-500/25' },
                    { label: 'Pending', value: pendingCount, icon: Clock, gradient: 'from-amber-500 to-orange-600', shadow: 'shadow-amber-500/25' },
                    { label: 'Expected Revenue', value: formatCurrency(todayRevenue), icon: DollarSign, gradient: 'from-violet-500 to-violet-700', shadow: 'shadow-violet-500/25' },
                ].map((stat, i) => {
                    const Icon = stat.icon;
                    return (
                        <div
                            key={stat.label}
                            className="card-elevated p-5 animate-fade-in-up hover:-translate-y-0.5 transition-transform"
                            style={{ animationDelay: `${(i + 1) * 80}ms` }}
                        >
                            <div className="flex items-start justify-between">
                                <div className="min-w-0">
                                    <p className="text-sm font-medium text-slate-500 dark:text-slate-400 mb-1 truncate">{stat.label}</p>
                                    <p className="text-2xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                                        {stat.value}
                                    </p>
                                </div>
                                <div className={cn('p-2.5 rounded-xl bg-gradient-to-br shadow-lg', stat.gradient, stat.shadow)}>
                                    <Icon className="h-5 w-5 text-white" />
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Filters */}
            <div className="flex flex-col lg:flex-row gap-4 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                <div className="relative flex-1">
                    <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 dark:text-slate-500" />
                    <input
                        type="text"
                        placeholder="Search by client or service..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="input pl-11 dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:placeholder-slate-500 shadow-sm"
                    />
                </div>
                <div className="flex gap-2 flex-wrap">
                    {['today', 'week', 'month'].map((date) => (
                        <button
                            key={date}
                            onClick={() => setDateFilter(date)}
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-semibold transition-all capitalize',
                                dateFilter === date
                                    ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-lg'
                                    : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-600 shadow-sm'
                            )}
                        >
                            {date === 'today' ? 'Today' : date === 'week' ? 'This Week' : 'This Month'}
                        </button>
                    ))}
                </div>
                <div className="flex gap-2 flex-wrap">
                    {['all', 'confirmed', 'pending', 'completed'].map((status) => (
                        <button
                            key={status}
                            onClick={() => setStatusFilter(status)}
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-semibold transition-all capitalize',
                                statusFilter === status
                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                    : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-800 hover:border-primary-300 dark:hover:border-primary-500 shadow-sm'
                            )}
                        >
                            {status}
                        </button>
                    ))}
                </div>
            </div>

            {/* Bookings List */}
            {loading ? (
                <div className="space-y-4">
                    {[...Array(3)].map((_, i) => (
                        <div key={i} className="card-elevated p-6 animate-pulse">
                            <div className="flex items-center gap-4">
                                <div className="hidden sm:flex flex-col items-center justify-center w-20 h-14 bg-slate-50 rounded-xl" />
                                <div className="w-12 h-12 rounded-xl bg-slate-100" />
                                <div className="flex-1 space-y-2">
                                    <div className="h-5 w-48 bg-slate-200 rounded" />
                                    <div className="h-4 w-32 bg-slate-100 rounded" />
                                </div>
                                <div className="text-right hidden sm:block space-y-2">
                                    <div className="h-6 w-20 bg-slate-200 rounded ml-auto" />
                                    <div className="h-4 w-32 bg-slate-50 rounded ml-auto" />
                                </div>
                                <div className="flex items-center gap-1">
                                    <div className="w-8 h-8 rounded-lg bg-slate-50" />
                                    <div className="w-8 h-8 rounded-lg bg-slate-50" />
                                    <div className="w-8 h-8 rounded-lg bg-slate-50" />
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="space-y-4">
                    {filteredBookings.map((booking, index) => {
                        const statusStyles = getStatusStyles(booking.status);
                        const StatusIcon = statusStyles.icon;

                        return (
                            <div
                                key={booking.id}
                                className={cn("card-elevated group overflow-hidden animate-fade-in-up transition-all", selectedIds.has(booking.id) && "ring-2 ring-indigo-500 ring-offset-1")}
                                style={{ animationDelay: `${400 + index * 100}ms` }}
                            >
                                <div className="p-5">
                                    <div className="flex items-center gap-4">
                                        {/* Checkbox */}
                                        <input
                                            type="checkbox"
                                            checked={selectedIds.has(booking.id)}
                                            onChange={() => toggleSelect(booking.id)}
                                            className="h-4 w-4 rounded border-slate-300 dark:border-slate-700 dark:bg-slate-800 text-indigo-600 focus:ring-indigo-500 cursor-pointer shrink-0"
                                            onClick={e => e.stopPropagation()}
                                        />
                                        {/* Time Block */}
                                        <div className="hidden sm:flex flex-col items-center justify-center w-20 py-2 bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-800/50 dark:to-slate-900/50 rounded-xl border border-slate-200 dark:border-slate-700 shadow-inner">
                                            <span className="text-lg font-bold text-slate-900 dark:text-white leading-tight">
                                                {new Date(booking.startTime).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true }).replace(/\s?(AM|PM)/i, '')}
                                            </span>
                                            <span className="text-[10px] uppercase tracking-wider font-bold text-slate-500 dark:text-slate-500">
                                                {new Date(booking.startTime).toLocaleTimeString('en-US', { hour: 'numeric', hour12: true }).match(/AM|PM/i)?.[0]} · {Math.round((new Date(booking.endTime).getTime() - new Date(booking.startTime).getTime()) / 60000)}m
                                            </span>
                                        </div>

                                        {/* Client Avatar */}
                                        <div className={cn(
                                            'w-12 h-12 rounded-xl flex items-center justify-center text-white font-semibold shadow-lg bg-gradient-to-br',
                                            booking.status === 'confirmed' ? 'from-emerald-400 to-emerald-600' :
                                                booking.status === 'pending' ? 'from-amber-400 to-amber-600' :
                                                    booking.status === 'completed' ? 'from-blue-400 to-blue-600' :
                                                        'from-slate-400 to-slate-600'
                                        )}>
                                            {booking.clientInitials}
                                        </div>

                                        {/* Details */}
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-2 mb-1.5">
                                                <h3 className="font-bold text-slate-900 dark:text-white">{booking.clientName}</h3>
                                                <span className={cn(
                                                    'flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider',
                                                    statusStyles.bg, statusStyles.text, 'border border-current/10'
                                                )}>
                                                    <StatusIcon className="h-3 w-3" />
                                                    {booking.status}
                                                </span>
                                            </div>
                                            <div className="flex items-center gap-4 text-xs font-medium text-slate-500 dark:text-slate-400">
                                                <span className="font-bold text-slate-800 dark:text-slate-300">{booking.serviceName}</span>
                                                <span className="flex items-center gap-1.5">
                                                    <User className="h-3.5 w-3.5 text-slate-400" />
                                                    {booking.staffName}
                                                </span>
                                                <span className="flex items-center gap-1.5 sm:hidden">
                                                    <Clock className="h-3.5 w-3.5 text-slate-400" />
                                                    {formatBookingTime(booking.startTime, booking.endTime)}
                                                </span>
                                            </div>
                                        </div>

                                        {/* Price & Actions */}
                                        <div className="flex items-center gap-4">
                                            <div className="text-right hidden sm:block">
                                                <p className="text-lg font-bold text-slate-900 dark:text-white leading-none mb-1">{formatCurrency(booking.price)}</p>
                                                <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-tighter">{formatBookingTime(booking.startTime, booking.endTime)}</p>
                                            </div>

                                            <div className="flex items-center gap-1">
                                                <Link
                                                    href={`/bookings/${booking.id}`}
                                                    className="p-2 hover:bg-primary-50 dark:hover:bg-primary-500/10 hover:text-primary-600 dark:hover:text-primary-400 rounded-lg transition-colors text-slate-400 dark:text-slate-500"
                                                    title="View details"
                                                >
                                                    <Eye className="h-4 w-4" />
                                                </Link>
                                                <Link
                                                    href={`/bookings/${booking.id}/edit`}
                                                    className="p-2 hover:bg-primary-50 dark:hover:bg-primary-500/10 hover:text-primary-600 dark:hover:text-primary-400 rounded-lg transition-colors text-slate-400 dark:text-slate-500"
                                                    title="Edit booking"
                                                >
                                                    <Edit className="h-4 w-4" />
                                                </Link>
                                                <button
                                                    className="p-2 hover:bg-slate-100 rounded-lg transition-colors"
                                                    title="More actions"
                                                >
                                                    <MoreVertical className="h-4 w-4 text-slate-400" />
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                {/* Status bar at bottom */}
                                <div className={cn('h-1', statusStyles.dot)} />
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Bulk Actions Bar */}
            <BulkActionsBar
                selectedCount={selectedIds.size}
                totalCount={filteredBookings.length}
                isAllSelected={selectedIds.size === filteredBookings.length && filteredBookings.length > 0}
                onSelectAll={() => setSelectedIds(new Set(filteredBookings.map(b => b.id)))}
                onClearSelection={() => setSelectedIds(new Set())}
                actions={[
                    {
                        label: 'Cancel Selected',
                        icon: <Ban className="h-3.5 w-3.5" />,
                        onClick: handleBulkCancel,
                    },
                    {
                        label: 'Delete Selected',
                        icon: <Trash2 className="h-3.5 w-3.5" />,
                        onClick: handleBulkDelete,
                        destructive: true,
                    },
                ]}
            />

            {/* Empty State */}
            {!loading && filteredBookings.length === 0 && (
                <div className="card-elevated py-20 text-center animate-fade-in dark:bg-slate-900 dark:border-slate-800">
                    <div className="w-20 h-20 bg-slate-50 dark:bg-slate-800 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-inner border border-slate-100 dark:border-slate-700">
                        <CalendarDays className="h-10 w-10 text-slate-300 dark:text-slate-600" />
                    </div>
                    <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-2">No bookings found</h3>
                    <p className="text-slate-500 dark:text-slate-400 mb-8 max-w-sm mx-auto">Adjust your filters to see more results or start fresh with a new appointment.</p>
                    <Link href="/bookings/new" className="btn btn-primary shadow-xl shadow-primary-500/20">
                        <Plus className="h-5 w-5" />
                        New Booking
                    </Link>
                </div>
            )}
        </div>
    );
}
