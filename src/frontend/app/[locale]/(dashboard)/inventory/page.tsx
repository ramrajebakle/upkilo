'use client';

import { cn, formatCurrency } from '@/lib/utils';
import api from '@/lib/api';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
    Package, Plus, Search, AlertTriangle, TrendingDown,
    MoreHorizontal, Edit2, Trash2, DollarSign, Boxes, PackageX,
    Bell, Layers, X
} from 'lucide-react';
import { PageHeader, StatsGrid, EmptyState, Pagination, SkeletonTable } from '@/components/ui';
import { ConfirmModal } from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';

interface InventoryItem {
    id: string;
    name: string;
    sku: string;
    category: string;
    quantityOnHand: number;
    reorderLevel: number;
    costPrice: number;
    salePrice: number | null;
    isRetail: boolean;
    isLowStock: boolean;
}

export default function InventoryPage() {
    const [items, setItems] = useState<InventoryItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [filterStatus, setFilterStatus] = useState<'all' | 'low' | 'out'>('all');
    const { success, error: toastError } = useToast();

    // Pagination & Delete State
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [itemToDelete, setItemToDelete] = useState<InventoryItem | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    // Bulk Adjust State
    const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
    const [isBulkAdjustOpen, setIsBulkAdjustOpen] = useState(false);
    const [bulkAdjustValue, setBulkAdjustValue] = useState(0);
    const [bulkAdjustNotes, setBulkAdjustNotes] = useState('');
    const [isBulkProcessing, setIsBulkProcessing] = useState(false);

    // Inventory Value
    const [inventoryValue, setInventoryValue] = useState<any>(null);

    useEffect(() => {
        const fetchInventory = async () => {
            setLoading(true);
            try {
                const [itemsRes, valueRes] = await Promise.all([
                    api.inventory.list(),
                    api.inventory.value(),
                ]);
                setItems(itemsRes.data.data || []);
                setInventoryValue(valueRes.data);
            } catch (err) {
                console.error('Failed to fetch inventory:', err);
            } finally {
                setLoading(false);
            }
        };
        fetchInventory();
    }, []);

    const filtered = items.filter((item) => {
        const matchesSearch = item.name.toLowerCase().includes(searchQuery.toLowerCase())
            || (item.sku || '').toLowerCase().includes(searchQuery.toLowerCase());
        if (filterStatus === 'low') return matchesSearch && item.quantityOnHand <= item.reorderLevel && item.quantityOnHand > 0;
        if (filterStatus === 'out') return matchesSearch && item.quantityOnHand === 0;
        return matchesSearch;
    });

    const lowStockCount = items.filter((i) => i.quantityOnHand <= i.reorderLevel && i.quantityOnHand > 0).length;
    const outOfStockCount = items.filter((i) => i.quantityOnHand === 0).length;

    const stats = [
        { label: 'Total Products', value: items.length, icon: Package, color: 'blue' as const },
        { label: 'Inventory Value', value: formatCurrency(inventoryValue?.totalCostValue || 0), icon: DollarSign, color: 'emerald' as const },
        { label: 'Low Stock', value: lowStockCount, icon: TrendingDown, color: 'amber' as const },
        { label: 'Out of Stock', value: outOfStockCount, icon: AlertTriangle, color: 'rose' as const },
    ];

    // Pagination
    const totalPages = Math.ceil(filtered.length / itemsPerPage);
    const paginatedItems = filtered.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    );

    const handleDelete = async () => {
        if (!itemToDelete) return;
        setIsDeleting(true);
        try {
            await api.inventory.delete(itemToDelete.id);
            setItems(current => current.filter(i => i.id !== itemToDelete.id));
            success('Inventory item deleted successfully');
            setIsDeleteModalOpen(false);
        } catch (err) {
            console.error('Failed to delete item:', err);
            toastError('Failed to delete inventory item');
        } finally {
            setIsDeleting(false);
        }
    };

    const confirmDelete = (item: InventoryItem) => {
        setItemToDelete(item);
        setIsDeleteModalOpen(true);
    };

    const toggleSelect = (id: string) => {
        setSelectedItems(prev => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id); else next.add(id);
            return next;
        });
    };

    const toggleSelectAll = () => {
        if (selectedItems.size === paginatedItems.length) {
            setSelectedItems(new Set());
        } else {
            setSelectedItems(new Set(paginatedItems.map(i => i.id)));
        }
    };

    const handleBulkAdjust = async () => {
        if (selectedItems.size === 0) return;
        setIsBulkProcessing(true);
        try {
            const adjustments = Array.from(selectedItems).map(itemId => ({
                itemId,
                quantityChange: bulkAdjustValue,
                type: bulkAdjustValue >= 0 ? 0 : 3, // StockIn or Adjustment
                notes: bulkAdjustNotes || 'Bulk adjustment',
            }));
            const res = await api.inventory.bulkAdjust({ adjustments });
            success(`Adjusted stock for ${res.data.adjustedCount} items`);
            // Refresh data
            const itemsRes = await api.inventory.list();
            setItems(itemsRes.data.data || []);
            setIsBulkAdjustOpen(false);
            setSelectedItems(new Set());
            setBulkAdjustValue(0);
            setBulkAdjustNotes('');
        } catch (err) {
            console.error('Bulk adjust failed:', err);
            toastError('Failed to adjust stock');
        } finally {
            setIsBulkProcessing(false);
        }
    };

    const handleSendAlerts = async () => {
        const lowStockIds = items
            .filter(i => i.quantityOnHand <= i.reorderLevel)
            .map(i => i.id);
        if (lowStockIds.length === 0) {
            toastError('No low-stock items to alert');
            return;
        }
        try {
            const res = await api.inventory.sendAlerts(lowStockIds);
            success(res.data.message || `Alerts sent for ${res.data.alertedCount} items`);
        } catch (err) {
            console.error('Send alerts failed:', err);
            toastError('Failed to send alerts');
        }
    };

    return (
        <div className="space-y-8 animate-fade-in">
            <PageHeader
                title="Inventory"
                description="Track product stock levels, manage reorders, and send automated alerts"
                icon={Boxes}
                iconGradient="from-orange-500 to-amber-600"
                iconShadow="shadow-orange-500/25"
                actions={
                    <div className="flex gap-3">
                        <button
                            onClick={handleSendAlerts}
                            className="inline-flex items-center gap-2 px-5 py-2.5 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800/50 rounded-2xl font-bold uppercase tracking-widest text-[10px] hover:bg-amber-100 dark:hover:bg-amber-900/40 transition-all shadow-sm"
                        >
                            <Bell className="h-4 w-4" />
                            Send alerts
                        </button>
                        <Link
                            href="/inventory/new"
                            className="inline-flex items-center gap-2 px-6 py-2.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-2xl font-bold uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/25 hover:shadow-2xl transition-all hover:-translate-y-0.5 active:scale-95"
                        >
                            <Plus className="h-4 w-4" />
                            Add Product
                        </Link>
                    </div>
                }
            />

            <StatsGrid stats={stats} loading={loading} />

            {/* Filters + Bulk Actions */}
            <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between px-1">
                <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center flex-1 w-full">
                    <div className="relative flex-1 max-w-md w-full group">
                        <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400 dark:text-slate-500 group-focus-within:text-primary-500 transition-colors" />
                        <input
                            type="text"
                            placeholder="Search products or SKU..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-12 pr-4 py-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl text-sm font-medium dark:text-white placeholder:text-slate-400 dark:placeholder:text-slate-600 focus:ring-4 focus:ring-primary-500/10 focus:border-primary-400 dark:focus:border-primary-500/50 transition-all shadow-sm"
                        />
                    </div>
                    <div className="flex bg-slate-100 dark:bg-slate-800 p-1 rounded-2xl border border-slate-200 dark:border-slate-700 shadow-inner">
                        {[
                            { key: 'all' as const, label: 'All' },
                            { key: 'low' as const, label: 'Low Stock' },
                            { key: 'out' as const, label: 'Out of Stock' },
                        ].map((f) => (
                            <button
                                key={f.key}
                                onClick={() => setFilterStatus(f.key)}
                                className={cn(
                                    'px-5 py-2 rounded-xl text-[10px] font-bold uppercase tracking-widest transition-all',
                                    filterStatus === f.key
                                        ? 'bg-white dark:bg-slate-700 text-primary-600 dark:text-white shadow-md'
                                        : 'text-slate-500 dark:text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
                                )}
                            >
                                {f.label}
                            </button>
                        ))}
                    </div>
                </div>
                {selectedItems.size > 0 && (
                    <button
                        onClick={() => setIsBulkAdjustOpen(true)}
                        className="inline-flex items-center gap-2 px-6 py-2.5 bg-primary-600 text-white rounded-2xl text-[10px] font-bold uppercase tracking-widest shadow-xl shadow-primary-600/25 hover:bg-primary-700 transition-all transform animate-in slide-in-from-right-4"
                    >
                        <Layers className="h-4 w-4" />
                        Bulk Adjust ({selectedItems.size})
                    </button>
                )}
            </div>

            {/* Bulk Adjust Modal */}
            {isBulkAdjustOpen && (
                <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-md z-50 flex items-center justify-center p-4">
                    <div className="bg-white dark:bg-slate-900 rounded-3xl shadow-2xl max-w-md w-full p-8 border border-slate-200 dark:border-slate-800 animate-in zoom-in-95">
                        <div className="flex items-center justify-between mb-8">
                            <div>
                                <h3 className="text-xl font-bold text-slate-900 dark:text-white tracking-tight">Stock Adjustment</h3>
                                <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1 uppercase tracking-widest">
                                    Updating <span className="text-primary-600 dark:text-primary-400 font-bold">{selectedItems.size}</span> items
                                </p>
                            </div>
                            <button onClick={() => setIsBulkAdjustOpen(false)} className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-all">
                                <X className="h-5 w-5 text-slate-400" />
                            </button>
                        </div>
                        <div className="space-y-6">
                            <div className="space-y-2">
                                <label className="text-[10px] font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400 ml-1">Quantity Change</label>
                                <input
                                    type="number"
                                    value={bulkAdjustValue}
                                    onChange={(e) => setBulkAdjustValue(parseInt(e.target.value) || 0)}
                                    className="w-full px-5 py-3.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl text-sm font-bold dark:text-white focus:ring-4 focus:ring-primary-500/10 focus:border-primary-400 transition-all shadow-inner"
                                    placeholder="e.g. +10 or -5"
                                />
                                <p className="text-[10px] font-medium text-slate-400 dark:text-slate-500 ml-1">Use positive for stock in, negative for stock out</p>
                            </div>
                            <div className="space-y-2">
                                <label className="text-[10px] font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400 ml-1">Notes</label>
                                <input
                                    type="text"
                                    value={bulkAdjustNotes}
                                    onChange={(e) => setBulkAdjustNotes(e.target.value)}
                                    className="w-full px-5 py-3.5 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl text-sm font-medium dark:text-white focus:ring-4 focus:ring-primary-500/10 focus:border-primary-400 transition-all shadow-inner"
                                    placeholder="Reason for adjustment..."
                                />
                            </div>
                        </div>
                        <div className="flex gap-4 mt-10">
                            <button
                                onClick={() => setIsBulkAdjustOpen(false)}
                                className="flex-1 px-6 py-3.5 border border-slate-200 dark:border-slate-800 rounded-2xl text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800 transition-all"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleBulkAdjust}
                                disabled={isBulkProcessing || bulkAdjustValue === 0}
                                className="flex-1 px-6 py-3.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-2xl text-xs font-bold uppercase tracking-widest shadow-xl shadow-primary-500/25 disabled:opacity-50 hover:shadow-2xl transition-all active:scale-95"
                            >
                                {isBulkProcessing ? 'Processing...' : 'Apply Change'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Table */}
            {loading ? (
                <SkeletonTable rows={itemsPerPage} cols={7} />
            ) : (
                <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-xl overflow-hidden transition-all">
                    <div className="overflow-x-auto">
                        <table className="w-full">
                            <thead>
                                <tr className="border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/50">
                                    <th className="w-12 px-6 py-4">
                                        <input
                                            type="checkbox"
                                            checked={selectedItems.size === paginatedItems.length && paginatedItems.length > 0}
                                            onChange={toggleSelectAll}
                                            className="rounded-md border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-primary-600 focus:ring-primary-500/20 transition-all pointer-events-auto cursor-pointer"
                                        />
                                    </th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Product Details</th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">SKU Code</th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Current Stock</th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Unit Cost</th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Retail Price</th>
                                    <th className="text-left text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Risk Status</th>
                                    <th className="text-right text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest px-6 py-4">Action</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-100 dark:divide-slate-800/50">
                                {filtered.length === 0 ? (
                                    <tr>
                                        <td colSpan={8} className="px-6 py-20">
                                            <EmptyState
                                                icon={PackageX}
                                                title="No products found"
                                                description="Your inventory is currently empty. Scale your business by adding your first product."
                                                action={
                                                    <Link href="/inventory/new" className="btn btn-primary px-8 rounded-2xl">
                                                        <Plus className="h-5 w-5" />
                                                        Add First Product
                                                    </Link>
                                                }
                                            />
                                        </td>
                                    </tr>
                                ) : (
                                    paginatedItems.map((item) => {
                                        const isLow = item.quantityOnHand <= item.reorderLevel && item.quantityOnHand > 0;
                                        const isOut = item.quantityOnHand === 0;
                                        return (
                                            <tr key={item.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-800/20 transition-all group">
                                                <td className="px-6 py-4">
                                                    <input
                                                        type="checkbox"
                                                        checked={selectedItems.has(item.id)}
                                                        onChange={() => toggleSelect(item.id)}
                                                        className="rounded-md border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-primary-600 focus:ring-primary-500/20 transition-all pointer-events-auto cursor-pointer"
                                                    />
                                                </td>
                                                <td className="px-6 py-4">
                                                    <div className="font-bold text-slate-900 dark:text-white text-sm group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">{item.name}</div>
                                                    <div className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-0.5">{item.category}</div>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <span className="text-xs font-bold text-slate-500 dark:text-slate-500 bg-slate-50 dark:bg-slate-800 px-2 py-1 rounded-md border border-slate-100 dark:border-slate-700 font-mono tracking-tight">{item.sku || 'N/A'}</span>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <div className="flex items-baseline gap-1.5">
                                                        <span className={cn(
                                                            'font-black text-sm',
                                                            isOut ? 'text-rose-600 dark:text-rose-400' : isLow ? 'text-amber-600 dark:text-amber-400' : 'text-slate-900 dark:text-white'
                                                        )}>
                                                            {item.quantityOnHand}
                                                        </span>
                                                        <span className="text-[10px] font-bold text-slate-400 dark:text-slate-600">/ {item.reorderLevel} min</span>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <span className="text-xs font-bold text-slate-600 dark:text-slate-400">{formatCurrency(item.costPrice)}</span>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <span className="text-xs font-bold text-slate-900 dark:text-white">{item.salePrice ? formatCurrency(item.salePrice) : '—'}</span>
                                                </td>
                                                <td className="px-6 py-4">
                                                    <span className={cn(
                                                        'inline-flex px-3 py-1 rounded-lg text-[10px] font-bold uppercase tracking-widest shadow-sm ring-1 ring-inset',
                                                        isOut ? 'bg-rose-50 dark:bg-rose-900/10 text-rose-700 dark:text-rose-400 ring-rose-200/50 dark:ring-rose-800/50' :
                                                        isLow ? 'bg-amber-50 dark:bg-amber-900/10 text-amber-700 dark:text-amber-400 ring-amber-200/50 dark:ring-amber-800/50' :
                                                        'bg-emerald-50 dark:bg-emerald-900/10 text-emerald-700 dark:text-emerald-400 ring-emerald-200/50 dark:ring-emerald-800/50'
                                                    )}>
                                                        {isOut ? 'Sold Out' : isLow ? 'Critical Low' : 'In Stock'}
                                                    </span>
                                                </td>
                                                <td className="px-6 py-4 text-right">
                                                    <div className="flex gap-2 justify-end opacity-0 group-hover:opacity-100 transition-all transform translate-x-2 group-hover:translate-x-0">
                                                        <Link
                                                            href={`/inventory/${item.id}`}
                                                            className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-all text-slate-400 dark:text-slate-500 hover:text-primary-600 dark:hover:text-primary-400 active:scale-90 border border-transparent hover:border-slate-200 dark:hover:border-slate-700"
                                                        >
                                                            <Edit2 className="h-4 w-4" />
                                                        </Link>
                                                        <button
                                                            onClick={() => confirmDelete(item)}
                                                            className="p-2 hover:bg-rose-50 dark:hover:bg-rose-900/40 text-slate-400 dark:text-slate-500 hover:text-rose-600 dark:hover:text-rose-400 rounded-xl transition-all active:scale-90 border border-transparent hover:border-rose-100 dark:hover:border-rose-900/50"
                                                        >
                                                            <Trash2 className="h-4 w-4" />
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })
                                )
                                }
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {!loading && totalPages > 1 && (
                <div className="mt-6">
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
                title="Delete Product"
                description={`This will permanently remove ${itemToDelete?.name} from your inventory catalog. This action cannot be reversed.`}
                confirmText="Delete Product"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
