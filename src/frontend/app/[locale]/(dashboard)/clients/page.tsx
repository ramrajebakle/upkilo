'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
    Search, Plus, MoreHorizontal, Users, Star, Heart,
    Gift, Filter, ChevronRight, Trash2, Mail, Phone,
    TrendingUp, Award, UserCheck, UserX, Calendar
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useClients, useDeleteClient, useDeleteClients, type Client } from '@/lib/query/clients';
import { PageHeader, StatsGrid, EmptyState, ErrorState, Pagination, SkeletonTable, Breadcrumb } from '@/components/ui';
import { ConfirmModal } from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';
import { CurrencyFormatter } from '@/components/ui/CurrencyFormatter';
import { 
    Download, 
    X, 
    CheckCircle, 
    AlertCircle, 
    ChevronDown 
} from 'lucide-react';

export default function ClientsPage() {
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState<string>('all');
    const { success, error: toastError } = useToast();

    // Pagination & Delete State
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [clientToDelete, setClientToDelete] = useState<Client | null>(null);
    const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

    const {
        data: clients = [],
        isPending: loading,
        isError,
        error,
        refetch,
        isFetching,
    } = useClients(searchQuery);

    const deleteClient = useDeleteClient();
    const deleteClients = useDeleteClients();

    const toggleSelectAll = () => {
        if (selectedIds.size === clients.length) {
            setSelectedIds(new Set());
        } else {
            setSelectedIds(new Set(clients.map(c => c.id)));
        }
    };

    const toggleSelectOne = (id: string) => {
        const next = new Set(selectedIds);
        if (next.has(id)) next.delete(id);
        else next.add(id);
        setSelectedIds(next);
    };

    const handleBulkDelete = async () => {
        const ids = [...selectedIds];
        if (ids.length === 0) return;
        try {
            const deleted = await deleteClients.mutateAsync(ids);
            success(`${deleted} ${deleted === 1 ? 'client' : 'clients'} deleted`);
            setSelectedIds(new Set());
        } catch (err) {
            // Partial success is reported as such: the old handler claimed the
            // whole operation failed even when some deletes had gone through.
            toastError(err instanceof Error ? err.message : 'Failed to delete clients');
            const deleted = (err as { deleted?: number })?.deleted ?? 0;
            if (deleted > 0) setSelectedIds(new Set());
        }
    };

    const filtered = clients.filter((c) => {
        const name = `${c.firstName} ${c.lastName}`.toLowerCase();
        const matchesSearch = name.includes(searchQuery.toLowerCase())
            || c.email.toLowerCase().includes(searchQuery.toLowerCase())
            || c.phone.includes(searchQuery);
        if (statusFilter === 'all') return matchesSearch;
        return matchesSearch && c.status.toLowerCase() === statusFilter;
    });

    // Stats
    const totalClients = clients.length;
    const activeClients = clients.filter(c => c.status === 'Active').length;
    const totalLoyalty = clients.reduce((sum, c) => sum + c.loyaltyPoints, 0);
    const avgSpend = clients.length > 0
        ? (clients.reduce((sum, c) => sum + c.totalSpend, 0) / clients.length).toFixed(0)
        : '0';

    // Pagination
    const totalPages = Math.ceil(filtered.length / itemsPerPage);
    const paginatedClients = filtered.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    );

    const handleDelete = async () => {
        if (!clientToDelete) return;
        try {
            await deleteClient.mutateAsync(clientToDelete.id);
            success('Client deleted');
            setIsDeleteModalOpen(false);
            if (paginatedClients.length === 1 && currentPage > 1) {
                setCurrentPage(currentPage - 1);
            }
        } catch (err) {
            toastError(err instanceof Error ? err.message : 'Failed to delete client');
        }
    };

    const confirmDelete = (client: Client) => {
        setClientToDelete(client);
        setIsDeleteModalOpen(true);
    };

    const formatCurrency = (value: number) =>
        new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 0 }).format(value);

    const formatDate = (dateString: string | null) => {
        if (!dateString) return 'Never';
        return new Date(dateString).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    };

    return (
        <div className="space-y-6">
            <Breadcrumb items={[{ label: 'Clients' }]} />
            <PageHeader
                title="Clients"
                description="Manage your customer base, loyalty programs, and referrals"
                icon={Users}
                iconGradient="from-primary-500 to-primary-600"
                iconShadow="shadow-primary-500/25"
                actions={
                    <Link
                        href="/clients/new"
                        className="inline-flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-primary-500/25 hover:shadow-xl transition-all hover:-translate-y-0.5 text-sm"
                    >
                        <Plus className="h-4 w-4" />
                        Add Client
                    </Link>
                }
            />

            <StatsGrid
                stats={[
                    { label: 'Total Clients', value: totalClients, icon: Users, color: 'blue' as const },
                    { label: 'Active', value: activeClients, icon: UserCheck, color: 'emerald' as const },
                    { label: 'Loyalty Points', value: totalLoyalty.toLocaleString(), icon: Star, color: 'amber' as const },
                    { label: 'Avg. Spend', value: formatCurrency(Number(avgSpend)), icon: TrendingUp, color: 'violet' as const },
                ]}
                loading={loading}
            />

            {/* Filters */}
            <div className="flex flex-col sm:flex-row gap-3 items-start sm:items-center">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                    <input
                        type="text"
                        placeholder="Search by name, email, or phone..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-sm focus:ring-2 focus:ring-primary-500/20 focus:border-primary-300 dark:focus:border-primary-500/50 text-slate-900 dark:text-white transition-all shadow-sm"
                    />
                </div>
                <div className="flex gap-2 flex-wrap">
                    {['all', 'active', 'inactive'].map((s) => (
                        <button
                            key={s}
                            onClick={() => setStatusFilter(s)}
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-semibold transition-all capitalize',
                                statusFilter === s
                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                    : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-800 hover:border-primary-300 dark:hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-300 shadow-sm'
                            )}
                        >
                            {s}
                        </button>
                    ))}
                </div>
            </div>

            {/* Bulk Actions Bar */}
            {selectedIds.size > 0 && (
                <div className="bg-primary-600 text-white px-6 py-3 rounded-xl flex items-center justify-between animate-in fade-in slide-in-from-bottom-2 duration-300">
                    <div className="flex items-center gap-3">
                        <span className="font-semibold">{selectedIds.size} selected</span>
                        <div className="flex gap-2 border-l border-white/20 ml-3 pl-3">
                            <button 
                                onClick={handleBulkDelete}
                                className="flex items-center gap-2 hover:bg-white/10 px-3 py-1.5 rounded-lg transition-colors text-sm"
                            >
                                <Trash2 className="h-4 w-4" />
                                Delete
                            </button>
                            <button className="flex items-center gap-2 hover:bg-white/10 px-3 py-1.5 rounded-lg transition-colors text-sm">
                                <Download className="h-4 w-4" />
                                Export
                            </button>
                        </div>
                    </div>
                    <button 
                        onClick={() => setSelectedIds(new Set())}
                        className="p-1 hover:bg-white/10 rounded-full"
                    >
                        <X className="h-5 w-5" />
                    </button>
                </div>
            )}

            {/* Table */}
            {isError ? (
                <ErrorState
                    title="Couldn't load clients"
                    error={error}
                    onRetry={() => refetch()}
                    isRetrying={isFetching}
                />
            ) : loading ? (
                <SkeletonTable rows={itemsPerPage} cols={7} />
            ) : (
                <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800 shadow-sm overflow-hidden">
                    <div className="overflow-x-auto">
                        <table className="w-full">
                            <thead>
                                <tr className="border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/50">
                                    <th className="px-6 py-3">
                                        <input
                                            type="checkbox"
                                            className="rounded border-slate-300 dark:border-slate-700 dark:bg-slate-800 text-primary-600 focus:ring-primary-500"
                                            checked={selectedIds.size === clients.length && clients.length > 0}
                                            onChange={toggleSelectAll}
                                        />
                                    </th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Client</th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Contact</th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Status</th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Loyalty</th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Spend</th>
                                    <th className="text-left text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Last Visit</th>
                                    <th className="text-right text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest px-6 py-3">Actions</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-50">
                                {filtered.length === 0 ? (
                                    <tr>
                                        <td colSpan={7} className="px-6 py-12">
                                            <EmptyState
                                                icon={UserX}
                                                title="No clients found"
                                                description="Start building your client base by adding your first customer."
                                                action={
                                                    <Link href="/clients/new" className="btn btn-primary">
                                                        <Plus className="h-4 w-4" />
                                                        Add Client
                                                    </Link>
                                                }
                                            />
                                        </td>
                                    </tr>
                                ) : (
                                    paginatedClients.map((client) => {
                                        const isActive = client.status === 'Active';
                                        return (
                                            <tr key={client.id} className={cn(
                                                "hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors group",
                                                selectedIds.has(client.id) && "bg-primary-50/30 dark:bg-primary-900/10"
                                            )}>
                                                <td className="px-6 py-4">
                                                    <input
                                                        type="checkbox"
                                                        className="rounded border-slate-300 dark:border-slate-700 dark:bg-slate-800 text-primary-600 focus:ring-primary-500"
                                                        checked={selectedIds.has(client.id)}
                                                        onChange={() => toggleSelectOne(client.id)}
                                                    />
                                                </td>
                                                <td className="px-6 py-4">
                                                    <div className="flex items-center gap-3">
                                                        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center text-white text-sm font-bold shadow-sm">
                                                            {client.firstName?.[0]}{client.lastName?.[0]}
                                                        </div>
                                                        <div>
                                                            <p className="font-bold text-slate-900 dark:text-white text-sm">{client.firstName} {client.lastName}</p>
                                                            {client.tags.length > 0 && (
                                                                <div className="flex gap-1 mt-0.5">
                                                                    {client.tags.slice(0, 2).map(tag => (
                                                                        <span key={tag} className="px-1.5 py-0.5 text-[10px] font-bold bg-primary-50 dark:bg-primary-900/40 text-primary-600 dark:text-primary-400 rounded-md border border-primary-100 dark:border-primary-800">{tag}</span>
                                                                    ))}
                                                                    {client.tags.length > 2 && (
                                                                        <span className="px-1.5 py-0.5 text-[10px] font-medium bg-muted text-foreground-secondary rounded-full">+{client.tags.length - 2}</span>
                                                                    )}
                                                                </div>
                                                            )}
                                                        </div>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <div className="text-sm font-medium text-slate-700 dark:text-slate-300">{client.email}</div>
                                                    <div className="text-xs text-foreground-secondary">{client.phone}</div>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <span className={cn(
                                                        'inline-flex px-2.5 py-1 rounded-full text-[10px] font-bold uppercase tracking-wider',
                                                        isActive ? 'bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400'
                                                    )}>
                                                        {client.status}
                                                    </span>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <div className="flex items-center gap-1.5">
                                                        <Star className="h-3.5 w-3.5 text-amber-400 fill-amber-400" />
                                                        <span className="font-bold text-sm text-slate-900 dark:text-white">{client.loyaltyPoints.toLocaleString()}</span>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4 text-sm font-bold text-slate-900 dark:text-white">
                                                    <CurrencyFormatter amount={client.totalSpend} />
                                                </td>
                                                <td className="px-6 py-4 text-sm text-slate-600 dark:text-slate-400">
                                                    {formatDate(client.lastVisitAt)}
                                                </td>
                                                <td className="px-6 py-4 text-right">
                                                    <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                                        <Link
                                                            href={`/clients/${client.id}`}
                                                            className="p-1.5 hover:bg-muted rounded-lg transition-colors text-foreground-muted hover:text-primary-600"
                                                            title="View"
                                                        >
                                                            <ChevronRight className="h-4 w-4" />
                                                        </Link>
                                                        <button className="p-1.5 hover:bg-blue-50 rounded-lg transition-colors" title="Email">
                                                            <Mail className="h-4 w-4 text-blue-500" />
                                                        </button>
                                                        <button className="p-1.5 hover:bg-emerald-50 rounded-lg transition-colors" title="Call">
                                                            <Phone className="h-4 w-4 text-success-fg" />
                                                        </button>
                                                        <button
                                                            onClick={() => confirmDelete(client)}
                                                            className="p-1.5 hover:bg-red-50 hover:text-red-500 rounded-lg transition-colors text-foreground-muted"
                                                            title="Delete"
                                                        >
                                                            <Trash2 className="h-4 w-4" />
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {!loading && !isError && totalPages > 1 && (
                <div className="mt-4">
                    <Pagination
                        currentPage={currentPage}
                        totalPages={totalPages}
                        onPageChange={setCurrentPage}
                        totalItems={filtered.length}
                    />
                </div>
            )}

            <ConfirmModal
                isOpen={isDeleteModalOpen}
                onClose={() => setIsDeleteModalOpen(false)}
                onConfirm={handleDelete}
                title="Delete Client"
                description={`Are you sure you want to delete ${clientToDelete?.firstName} ${clientToDelete?.lastName}? This action cannot be undone.`}
                confirmText="Delete"
                variant="danger"
                loading={deleteClient.isPending}
            />
        </div>
    );
}
