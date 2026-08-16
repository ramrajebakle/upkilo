'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Plus,
    Search,
    Filter,
    MoreVertical,
    Mail,
    Phone,
    Calendar,
    Star,
    Clock,
    TrendingUp,
    Users,
    Award,
    ChevronRight,
    Trophy,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { PageHeader, StatsGrid, EmptyState, Pagination, SkeletonCard, SkeletonTable } from '@/components/ui';
import { ConfirmModal } from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';
import { Trash2 } from 'lucide-react';

interface StaffMember {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    role: string;
    avatar?: string;
    status: 'active' | 'away' | 'offline';
    rating: number;
    bookingsToday: number;
    bookingsTotal: number;
    specialties: string[];
    joinedAt: string;
}

export default function StaffPage() {
    const [staff, setStaff] = useState<StaffMember[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const { success, error: toastError } = useToast();

    // Pagination & Delete State
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 8;
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [staffToDelete, setStaffToDelete] = useState<StaffMember | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        const fetchStaff = async () => {
            setLoading(true);
            try {
                const res = await api.staff.list();
                const data = res.data.map((s: any) => ({
                    id: s.id,
                    firstName: s.firstName,
                    lastName: s.lastName,
                    email: s.email,
                    phone: s.phone,
                    role: s.role,
                    status: s.employmentStatus === 'Active' ? 'active' : 'offline',
                    rating: s.averageRating || 0,
                    bookingsToday: s.bookingsToday || 0,
                    bookingsTotal: s.totalBookings || 0,
                    specialties: s.specialties || [],
                    joinedAt: s.employmentStartDate
                }));
                setStaff(data);
            } catch (error) {
                console.error(error);
                toastError('Failed to load staff list');
            } finally {
                setLoading(false);
            }
        };
        fetchStaff();
    }, []);

    const filteredStaff = staff.filter((member) =>
        `${member.firstName} ${member.lastName}`.toLowerCase().includes(searchQuery.toLowerCase()) ||
        member.role.toLowerCase().includes(searchQuery.toLowerCase())
    );

    const getStatusColor = (status: string) => {
        switch (status) {
            case 'active': return 'bg-emerald-400';
            case 'away': return 'bg-amber-400';
            default: return 'bg-slate-300';
        }
    };

    const getGradient = (index: number) => {
        const gradients = [
            'from-violet-500 to-purple-600',
            'from-cyan-500 to-blue-600',
            'from-rose-500 to-pink-600',
            'from-amber-500 to-orange-600',
        ];
        return gradients[index % gradients.length];
    };

    // Stats
    const totalStaff = staff.length;
    const activeStaff = staff.filter(s => s.status === 'active').length;
    const todayBookings = staff.reduce((sum, s) => sum + s.bookingsToday, 0);
    const avgRating = staff.length > 0 ? (staff.reduce((sum, s) => sum + s.rating, 0) / staff.length).toFixed(1) : "0";

    // Pagination
    const totalPages = Math.ceil(filteredStaff.length / itemsPerPage);
    const paginatedStaff = filteredStaff.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    );

    const handleDelete = async () => {
        if (!staffToDelete) return;
        setIsDeleting(true);
        try {
            await api.staff.delete(staffToDelete.id);
            setStaff(current => current.filter(s => s.id !== staffToDelete.id));
            success('Staff member deleted successfully');
            setIsDeleteModalOpen(false);
            if (paginatedStaff.length === 1 && currentPage > 1) {
                setCurrentPage(currentPage - 1);
            }
        } catch (err) {
            console.error('Failed to delete staff:', err);
            toastError('Failed to delete staff member');
        } finally {
            setIsDeleting(false);
        }
    };

    const confirmDelete = (e: React.MouseEvent, member: StaffMember) => {
        e.preventDefault();
        e.stopPropagation();
        setStaffToDelete(member);
        setIsDeleteModalOpen(true);
    };

    return (
        <div className="space-y-6">
            <PageHeader 
                title="Staff Management" 
                description="Manage your team members and their schedules"
                icon={Users}
                iconGradient="from-blue-500 to-primary-600"
                iconShadow="shadow-blue-500/25"
                actions={
                    <Link
                        href="/staff/new"
                        className="btn btn-primary shadow-lg shadow-primary-500/25"
                    >
                        <Plus className="h-5 w-5" />
                        Add Staff Member
                    </Link>
                }
            />

            <StatsGrid 
                stats={[
                    { label: 'Total Staff', value: totalStaff, icon: Users, color: 'blue' },
                    { label: 'Active Now', value: activeStaff, icon: Clock, color: 'emerald' },
                    { label: "Today's Bookings", value: todayBookings, icon: Calendar, color: 'violet' },
                    { label: 'Avg. Rating', value: avgRating, icon: Star, color: 'amber' },
                ]}
                loading={loading}
            />

            {/* Performance Ranking */}
            {!loading && staff.length > 0 && (
                <div className="card-elevated p-6 animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-sm" style={{ animationDelay: '150ms' }}>
                    <div className="flex items-center gap-2 mb-5">
                        <div className="p-1.5 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
                            <Trophy className="h-5 w-5 text-amber-500" />
                        </div>
                        <h3 className="font-bold text-slate-900 dark:text-white uppercase tracking-widest text-xs">Top Performers</h3>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        {[...staff]
                            .sort((a, b) => b.bookingsTotal - a.bookingsTotal)
                            .slice(0, 3)
                            .map((member, i) => {
                                const medalColors = [
                                    'from-amber-400 to-yellow-500',
                                    'from-slate-300 to-slate-400',
                                    'from-amber-600 to-amber-700',
                                ];
                                return (
                                    <div key={member.id} className="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800 shadow-inner">
                                        <div className={cn(
                                            'w-10 h-10 rounded-xl flex items-center justify-center text-white font-bold text-xs bg-gradient-to-br shadow-md',
                                            medalColors[i] || 'from-slate-200 to-slate-300'
                                        )}>
                                            #{i + 1}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className="font-bold text-slate-900 dark:text-white text-sm truncate">{member.firstName} {member.lastName}</p>
                                            <p className="text-[10px] font-bold text-slate-500 dark:text-slate-500 uppercase tracking-tighter">{member.bookingsTotal} bookings • ⭐ {member.rating}</p>
                                        </div>
                                    </div>
                                );
                            })}
                    </div>
                </div>
            )}

            {/* Search and Filters */}
            <div className="flex flex-col sm:flex-row gap-4 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                <div className="relative flex-1">
                    <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 dark:text-slate-500" />
                    <input
                        type="text"
                        placeholder="Search staff by name or role..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="input pl-11 dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:placeholder-slate-500 shadow-sm"
                    />
                </div>
                <div className="flex gap-2">
                    <button className="btn btn-secondary dark:bg-slate-900 dark:border-slate-800 dark:text-slate-400">
                        <Filter className="h-4 w-4" />
                        Filters
                    </button>
                    <div className="flex border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
                        <button
                            onClick={() => setViewMode('grid')}
                            className={cn(
                                'px-4 py-2 transition-all',
                                viewMode === 'grid' ? 'bg-primary-500 text-white shadow-inner' : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'
                            )}
                        >
                            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 16 16">
                                <rect x="1" y="1" width="6" height="6" rx="1" />
                                <rect x="9" y="1" width="6" height="6" rx="1" />
                                <rect x="1" y="9" width="6" height="6" rx="1" />
                                <rect x="9" y="9" width="6" height="6" rx="1" />
                            </svg>
                        </button>
                        <button
                            onClick={() => setViewMode('list')}
                            className={cn(
                                'px-4 py-2 transition-all',
                                viewMode === 'list' ? 'bg-primary-500 text-white shadow-inner' : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'
                            )}
                        >
                            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 16 16">
                                <rect x="1" y="1" width="14" height="3" rx="1" />
                                <rect x="1" y="6" width="14" height="3" rx="1" />
                                <rect x="1" y="11" width="14" height="3" rx="1" />
                            </svg>
                        </button>
                    </div>
                </div>
            </div>

            {/* Staff content */}
            {loading ? (
                viewMode === 'grid' ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                        {Array.from({ length: 4 }).map((_, i) => (
                            <SkeletonCard key={i} />
                        ))}
                    </div>
                ) : (
                    <SkeletonTable rows={itemsPerPage} cols={7} />
                )
            ) : viewMode === 'grid' ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {paginatedStaff.map((member, index) => (
                        <Link
                            key={member.id}
                            href={`/staff/${member.id}`}
                            className="card-elevated hover-premium group cursor-pointer overflow-hidden animate-fade-in-up dark:bg-slate-900 dark:border-slate-800 shadow-lg"
                            style={{ animationDelay: `${300 + index * 100}ms` }}
                        >
                            {/* Top gradient bar */}
                            <div className={cn('h-1.5 bg-gradient-to-r', getGradient(index))} />

                            <div className="p-6">
                                <div className="flex flex-col items-center text-center">
                                    {/* Avatar */}
                                    <div className="relative mb-4">
                                        <div className={cn(
                                            'w-20 h-20 rounded-2xl bg-gradient-to-br flex items-center justify-center text-white text-2xl font-bold shadow-lg group-hover:scale-105 transition-transform duration-300 ring-4 ring-white dark:ring-slate-800',
                                            getGradient(index)
                                        )}>
                                            {member.firstName[0]}{member.lastName[0]}
                                        </div>
                                        <div className={cn(
                                            'absolute -bottom-1 -right-1 w-6 h-6 rounded-full border-4 border-white dark:border-slate-900 shadow-sm',
                                            getStatusColor(member.status)
                                        )} />
                                    </div>

                                    {/* Name & Role */}
                                    <h3 className="font-bold text-slate-900 dark:text-white mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                                        {member.firstName} {member.lastName}
                                    </h3>
                                    <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mb-4">{member.role}</p>

                                    {/* Rating */}
                                    <div className="flex items-center gap-1.5 mb-5 bg-slate-50 dark:bg-slate-800 px-3 py-1 rounded-full border border-slate-100 dark:border-slate-700">
                                        <Star className="h-3.5 w-3.5 text-amber-400 fill-amber-400" />
                                        <span className="font-bold text-slate-900 dark:text-white text-sm">{member.rating}</span>
                                        <span className="text-[10px] font-medium text-slate-400 dark:text-slate-500">({member.bookingsTotal})</span>
                                    </div>

                                    {/* Specialties */}
                                    <div className="flex flex-wrap justify-center gap-1.5 mb-5">
                                        {member.specialties.slice(0, 2).map((spec) => (
                                            <span
                                                key={spec}
                                                className="px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-tighter bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 rounded-md border border-primary-100 dark:border-primary-800/50"
                                            >
                                                {spec}
                                            </span>
                                        ))}
                                        {member.specialties.length > 2 && (
                                            <span className="px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-tighter bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 rounded-md border border-slate-200 dark:border-slate-700">
                                                +{member.specialties.length - 2}
                                            </span>
                                        )}
                                    </div>

                                    {/* Today's bookings badge */}
                                    <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400">
                                        <Calendar className="h-3.5 w-3.5 text-primary-500" />
                                        <span>
                                            <span className="text-slate-900 dark:text-white">{member.bookingsToday}</span> bookings today
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div className="absolute top-4 right-4 opacity-0 group-hover:opacity-100 transition-all transform translate-x-2 group-hover:translate-x-0 flex gap-2">
                                <button
                                    onClick={(e) => confirmDelete(e, member)}
                                    className="p-2 hover:bg-red-50 dark:hover:bg-red-900/40 bg-white dark:bg-slate-800 shadow-md rounded-xl transition-all text-red-500 hover:text-red-600 active:scale-90 border border-slate-100 dark:border-slate-700"
                                >
                                    <Trash2 className="h-4 w-4" />
                                </button>
                                <div className="p-2 hover:bg-primary-50 dark:hover:bg-primary-900/40 bg-white dark:bg-slate-800 shadow-md rounded-xl transition-all text-slate-700 dark:text-slate-300 border border-slate-100 dark:border-slate-700">
                                    <ChevronRight className="h-4 w-4" />
                                </div>
                            </div>
                        </Link>
                    ))}
                </div>
            ) : (
                /* List View */
                <div className="card-elevated overflow-hidden animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                    <div className="table-container">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>Staff Member</th>
                                    <th>Role</th>
                                    <th>Status</th>
                                    <th>Rating</th>
                                    <th>Today</th>
                                    <th>Total</th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                {paginatedStaff.map((member, index) => (
                                    <tr key={member.id} className="animate-fade-in" style={{ animationDelay: `${index * 50}ms` }}>
                                        <td>
                                            <div className="flex items-center gap-3">
                                                <div className={cn(
                                                    'w-10 h-10 rounded-xl bg-gradient-to-br flex items-center justify-center text-white text-sm font-bold shadow-sm',
                                                    getGradient(index)
                                                )}>
                                                    {member.firstName[0]}{member.lastName[0]}
                                                </div>
                                                <div>
                                                    <p className="font-bold text-slate-900 dark:text-white text-sm">{member.firstName} {member.lastName}</p>
                                                    <p className="text-[10px] font-medium text-slate-500 dark:text-slate-500 uppercase tracking-widest">{member.email}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="text-slate-600 dark:text-slate-400 font-medium text-sm">{member.role}</td>
                                        <td>
                                            <span className={cn(
                                                'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-wider',
                                                member.status === 'active' && 'bg-emerald-50 dark:bg-emerald-900/20 text-emerald-700 dark:text-emerald-400',
                                                member.status === 'away' && 'bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400',
                                                member.status === 'offline' && 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400',
                                            )}>
                                                <span className={cn('w-1.5 h-1.5 rounded-full', getStatusColor(member.status))} />
                                                {member.status}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="flex items-center gap-1.5">
                                                <Star className="h-3.5 w-3.5 text-amber-400 fill-amber-400" />
                                                <span className="font-bold text-slate-900 dark:text-white text-sm">{member.rating}</span>
                                            </div>
                                        </td>
                                        <td className="font-bold text-slate-900 dark:text-white">{member.bookingsToday}</td>
                                        <td className="text-slate-600 dark:text-slate-400 text-sm font-medium">{member.bookingsTotal}</td>
                                        <td>
                                            <div className="flex gap-2 justify-end">
                                                <Link 
                                                    href={`/staff/${member.id}`}
                                                    className="p-2 hover:bg-slate-100 rounded-lg transition-colors"
                                                >
                                                    <ChevronRight className="h-4 w-4 text-slate-400 font-bold" />
                                                </Link>
                                                <button 
                                                    onClick={(e) => confirmDelete(e, member)}
                                                    className="p-2 hover:bg-red-50 text-slate-400 hover:text-red-500 rounded-lg transition-colors"
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {!loading && totalPages > 1 && (
                <div className="mt-4">
                    <Pagination
                        currentPage={currentPage}
                        totalPages={totalPages}
                        onPageChange={setCurrentPage}
                        totalItems={filteredStaff.length}
                    />
                </div>
            )}

            {/* Empty State */}
            {!loading && filteredStaff.length === 0 && (
                <EmptyState
                    icon={Users}
                    title="No staff members found"
                    description="Try adjusting your search or add a new team member."
                    action={
                        <Link href="/staff/new" className="btn btn-primary">
                            <Plus className="h-4 w-4" />
                            Add Staff Member
                        </Link>
                    }
                />
            )}

            <ConfirmModal
                isOpen={isDeleteModalOpen}
                onClose={() => setIsDeleteModalOpen(false)}
                onConfirm={handleDelete}
                title="Delete Staff Member"
                description={`Are you sure you want to delete ${staffToDelete?.firstName} ${staffToDelete?.lastName}? This action cannot be undone.`}
                confirmText="Delete"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
