'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import {
    ArrowLeft,
    Package,
    Tag,
    Hash,
    Layers,
    Boxes,
    AlertCircle,
    DollarSign,
    Truck,
    Save,
    Sparkles,
    History,
    ArrowUpDown,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const inventorySchema = z.object({
    name: z.string().min(2, 'Product name must be at least 2 characters'),
    sku: z.string().optional(),
    category: z.string().min(1, 'Please select a category'),
    currentStock: z.number().min(0, 'Stock cannot be negative'),
    reorderLevel: z.number().min(0, 'Reorder level cannot be negative'),
    costPrice: z.number().min(0, 'Cost price cannot be negative'),
    retailPrice: z.number().min(0, 'Retail price cannot be negative'),
    supplier: z.string().optional(),
    description: z.string().optional(),
});

type InventoryFormData = z.infer<typeof inventorySchema>;

export default function EditInventoryPage() {
    const router = useRouter();
    const params = useParams();
    const id = params.id as string;
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        reset,
        watch,
    } = useForm<InventoryFormData>({
        resolver: zodResolver(inventorySchema),
    });

    const currentStock = watch('currentStock');
    const reorderLevel = watch('reorderLevel');
    const retailPrice = watch('retailPrice');

    useEffect(() => {
        const fetchItem = async () => {
            if (!id) return;
            setFetching(true);
            try {
                const res = await api.inventory.get(id);
                reset(res.data);
            } catch (error) {
                console.error('Failed to fetch inventory item', error);
                toastError('Failed to load product details');
                router.push('/inventory');
            } finally {
                setFetching(false);
            }
        };
        fetchItem();
    }, [id, router, toastError, reset]);

    const onSubmit = async (data: InventoryFormData) => {
        setLoading(true);
        try {
            await api.inventory.update(id, data);
            toastSuccess('Product updated successfully');
            router.push('/inventory?updated=true');
        } catch (error) {
            console.error('Failed to update inventory item', error);
            toastError('Failed to update product');
        } finally {
            setLoading(false);
        }
    };

    if (fetching) {
        return (
            <div className="max-w-4xl mx-auto animate-pulse space-y-8">
                <div className="h-20 bg-slate-100 rounded-2xl w-full" />
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <div className="lg:col-span-2 h-96 bg-slate-50 rounded-2xl" />
                    <div className="h-96 bg-slate-50 rounded-2xl" />
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/inventory"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl shadow-lg shadow-primary-500/25">
                            <Package className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Edit Product
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Modify product details and stock settings</p>
                </div>
                <button className="btn btn-secondary flex items-center gap-2">
                    <History className="h-4 w-4" />
                    View History
                </button>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Main Info */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Tag className="h-5 w-5 text-primary-500" />
                                Product Details
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Product Name <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        {...register('name')}
                                        type="text"
                                        className={cn("input", errors.name && "border-red-500")}
                                        placeholder="e.g. Lavender Massage Oil"
                                    />
                                    {errors.name && <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>}
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            SKU / Barcode
                                        </label>
                                        <div className="relative">
                                            <Hash className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                            <input
                                                {...register('sku')}
                                                type="text"
                                                className="input pl-11"
                                                placeholder="LAV-OIL-001"
                                            />
                                        </div>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Category <span className="text-red-500">*</span>
                                        </label>
                                        <div className="relative">
                                            <Layers className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                            <select
                                                {...register('category')}
                                                className={cn("input pl-11 appearance-none", errors.category && "border-red-500")}
                                            >
                                                <option value="">Select Category</option>
                                                <option value="Skin Care">Skin Care</option>
                                                <option value="Hair Care">Hair Care</option>
                                                <option value="Equipment">Equipment</option>
                                                <option value="Supplies">Supplies</option>
                                            </select>
                                        </div>
                                        {errors.category && <p className="text-xs text-red-500 mt-1">{errors.category.message}</p>}
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Description
                                    </label>
                                    <textarea
                                        {...register('description')}
                                        className="input min-h-[100px] py-3"
                                        placeholder="Describe the product..."
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <DollarSign className="h-5 w-5 text-emerald-500" />
                                Pricing
                            </h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Cost Price ($)
                                    </label>
                                    <input
                                        {...register('costPrice', { valueAsNumber: true })}
                                        type="number"
                                        step="0.01"
                                        className={cn("input", errors.costPrice && "border-red-500")}
                                        placeholder="0.00"
                                    />
                                    {errors.costPrice && <p className="text-xs text-red-500 mt-1">{errors.costPrice.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Retail Price ($)
                                    </label>
                                    <input
                                        {...register('retailPrice', { valueAsNumber: true })}
                                        type="number"
                                        step="0.01"
                                        className={cn("input font-bold text-primary-600", errors.retailPrice && "border-red-500")}
                                        placeholder="0.00"
                                    />
                                    {errors.retailPrice && <p className="text-xs text-red-500 mt-1">{errors.retailPrice.message}</p>}
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Sidebar Info */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Boxes className="h-5 w-5 text-blue-500" />
                                Stock & Supplier
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Current Stock
                                    </label>
                                    <div className="flex gap-2">
                                        <input
                                            {...register('currentStock', { valueAsNumber: true })}
                                            type="number"
                                            className={cn("input", errors.currentStock && "border-red-500")}
                                        />
                                        <button 
                                            type="button"
                                            onClick={() => router.push(`/inventory/${id}/adjust`)}
                                            className="btn btn-secondary px-3"
                                            title="Stock Adjustment"
                                        >
                                            <ArrowUpDown className="h-4 w-4" />
                                        </button>
                                    </div>
                                    {errors.currentStock && <p className="text-xs text-red-500 mt-1">{errors.currentStock.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Reorder Level
                                    </label>
                                    <div className="relative">
                                        <AlertCircle className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                        <input
                                            {...register('reorderLevel', { valueAsNumber: true })}
                                            type="number"
                                            className={cn("input pl-11", errors.reorderLevel && "border-red-500")}
                                        />
                                    </div>
                                    {errors.reorderLevel && <p className="text-xs text-red-500 mt-1">{errors.reorderLevel.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Supplier
                                    </label>
                                    <div className="relative">
                                        <Truck className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                        <input
                                            {...register('supplier')}
                                            type="text"
                                            className="input pl-11"
                                            placeholder="Supplier name"
                                        />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="p-6 bg-slate-900 rounded-2xl text-white shadow-xl animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                            <div className="flex items-center gap-3 mb-4">
                                <div className="p-2 bg-white/10 rounded-lg">
                                    <Sparkles className="h-5 w-5 text-amber-400" />
                                </div>
                                <h3 className="font-bold text-lg">Inventory Status</h3>
                            </div>
                            <div className="space-y-3">
                                <div className="flex justify-between items-center text-sm">
                                    <span className="text-slate-400">Status</span>
                                    <span className={cn(
                                        'px-2 py-0.5 rounded-full font-bold text-[10px] uppercase tracking-wider',
                                        currentStock === 0 ? 'bg-red-500/20 text-red-400' :
                                        currentStock <= reorderLevel ? 'bg-amber-500/20 text-amber-400' :
                                        'bg-emerald-500/20 text-emerald-400'
                                    )}>
                                        {currentStock === 0 ? 'Out of Stock' :
                                         currentStock <= reorderLevel ? 'Low Stock' : 'Healthy'}
                                    </span>
                                </div>
                                <div className="flex justify-between items-center text-sm">
                                    <span className="text-slate-400">Potential Revenue</span>
                                    <span className="font-bold text-emerald-400">
                                        ${(currentStock * retailPrice).toLocaleString()}
                                    </span>
                                </div>
                            </div>
                        </div>

                        <div className="flex flex-col gap-3 pt-2">
                            <button
                                type="submit"
                                disabled={loading}
                                className="w-full btn btn-primary py-4 shadow-xl shadow-primary-500/25"
                            >
                                {loading ? (
                                    <>
                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                        Updating...
                                    </>
                                ) : (
                                    <>
                                        <Save className="h-5 w-5" />
                                        Save Changes
                                    </>
                                )}
                            </button>
                            <Link
                                href="/inventory"
                                className="w-full btn btn-secondary text-center py-4"
                            >
                                Cancel
                            </Link>
                        </div>
                    </div>
                </div>
            </form>
        </div>
    );
}
