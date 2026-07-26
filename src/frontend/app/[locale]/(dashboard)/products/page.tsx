'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Plus,
    Search,
    Filter,
    Package,
    DollarSign,
    TrendingUp,
    ShoppingCart,
    MoreVertical,
    Edit,
    Trash2,
    Eye,
    ChevronRight,
    Tag,
    AlertCircle,
    CheckCircle,
} from 'lucide-react';
import { api, apiClient } from '@/lib/api';
import { cn, formatCurrency } from '@/lib/utils';
import { ConfirmModal } from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';
import { SkeletonCard, SkeletonTable } from '@/components/ui';

interface Product {
    id: string;
    name: string;
    category: string;
    price: number;
    costPrice: number;
    stock: number;
    lowStockThreshold: number;
    sku: string;
    status: 'active' | 'inactive' | 'out_of_stock';
    sales: number;
    revenue: number;
    image?: string;
}

export default function ProductsPage() {
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [categoryFilter, setCategoryFilter] = useState('all');
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const { success, error } = useToast();
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [productToDelete, setProductToDelete] = useState<Product | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        const fetchProducts = async () => {
            setLoading(true);
            try {
                const res = await apiClient.get('/api/v1/products');
                setProducts(res.data.data || res.data.products || res.data || []);
            } catch (err) {
                console.error('Failed to fetch products:', err);
            } finally {
                setLoading(false);
            }
        };
        fetchProducts();
    }, []);

    const handleDelete = async () => {
        if (!productToDelete) return;
        setIsDeleting(true);
        try {
            await apiClient.delete(`/api/v1/products/${productToDelete.id}`);
            setProducts(current => current.filter(p => p.id !== productToDelete.id));
            success('Product deleted successfully');
            setIsDeleteModalOpen(false);
        } catch (err) {
            console.error('Failed to delete product:', err);
            error('Failed to delete product');
        } finally {
            setIsDeleting(false);
        }
    };

    const confirmDelete = (product: Product) => {
        setProductToDelete(product);
        setIsDeleteModalOpen(true);
    };

    const categories = ['all', ...Array.from(new Set(products.map(p => p.category)))];

    const filteredProducts = products.filter(product => {
        const matchesSearch = product.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
            product.sku.toLowerCase().includes(searchQuery.toLowerCase());
        const matchesCategory = categoryFilter === 'all' || product.category === categoryFilter;
        return matchesSearch && matchesCategory;
    });

    // Stats
    const totalProducts = products.length;
    const lowStockCount = products.filter(p => p.stock > 0 && p.stock <= p.lowStockThreshold).length;
    const outOfStockCount = products.filter(p => p.stock === 0).length;
    const totalRevenue = products.reduce((sum, p) => sum + p.revenue, 0);

    const getCategoryColor = (category: string) => {
        switch (category) {
            case 'Hair Care': return 'from-violet-500 to-purple-600';
            case 'Skin Care': return 'from-rose-500 to-pink-600';
            case 'Aromatherapy': return 'from-emerald-500 to-teal-600';
            case 'Nail Care': return 'from-amber-500 to-orange-600';
            default: return 'from-slate-500 to-slate-600';
        }
    };

    const getStockStatus = (product: Product) => {
        if (product.stock === 0) return { label: 'Out of Stock', color: 'bg-red-50 text-red-700' };
        if (product.stock <= product.lowStockThreshold) return { label: 'Low Stock', color: 'bg-amber-50 text-amber-700' };
        return { label: 'In Stock', color: 'bg-emerald-50 text-emerald-700' };
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div className="animate-fade-in-up">
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2 bg-gradient-to-br from-cyan-500 to-blue-600 rounded-xl shadow-lg shadow-cyan-500/25">
                            <Package className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl lg:text-3xl font-bold text-slate-900"
                            style={{ fontFamily: 'Outfit, sans-serif' }}
                        >
                            Products
                        </h1>
                    </div>
                    <p className="text-slate-500">Manage your retail products and inventory</p>
                </div>
                <Link
                    href="/products/new"
                    className="btn btn-primary shadow-lg shadow-primary-500/25 animate-fade-in"
                    style={{ animationDelay: '100ms' }}
                >
                    <Plus className="h-5 w-5" />
                    Add Product
                </Link>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {[
                    { label: 'Total Products', value: totalProducts, icon: Package, color: 'blue' },
                    { label: 'Low Stock', value: lowStockCount, icon: AlertCircle, color: 'amber' },
                    { label: 'Out of Stock', value: outOfStockCount, icon: Tag, color: 'red' },
                    { label: 'Total Revenue', value: formatCurrency(totalRevenue), icon: DollarSign, color: 'emerald' },
                ].map((stat, i) => (
                    <div
                        key={stat.label}
                        className="stat-card animate-fade-in-up"
                        style={{ animationDelay: `${(i + 1) * 100}ms` }}
                    >
                        <div className="flex items-center gap-3">
                            <div className={cn(
                                'p-2.5 rounded-xl',
                                stat.color === 'blue' && 'bg-blue-100 text-blue-600',
                                stat.color === 'amber' && 'bg-amber-100 text-amber-600',
                                stat.color === 'red' && 'bg-red-100 text-red-600',
                                stat.color === 'emerald' && 'bg-emerald-100 text-emerald-600',
                            )}>
                                <stat.icon className="h-5 w-5" />
                            </div>
                            <div>
                                <p className="stat-value text-xl">{stat.value}</p>
                                <p className="text-sm text-slate-500">{stat.label}</p>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Filters */}
            <div className="flex flex-col sm:flex-row gap-4 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                <div className="relative flex-1">
                    <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <input
                        type="text"
                        placeholder="Search products by name or SKU..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="input pl-11"
                    />
                </div>
                <select
                    value={categoryFilter}
                    onChange={(e) => setCategoryFilter(e.target.value)}
                    className="input w-auto min-w-[150px]"
                >
                    {categories.map(cat => (
                        <option key={cat} value={cat}>
                            {cat === 'all' ? 'All Categories' : cat}
                        </option>
                    ))}
                </select>
                <div className="flex border border-slate-200 rounded-lg overflow-hidden">
                    <button
                        onClick={() => setViewMode('grid')}
                        className={cn(
                            'px-3 py-2 transition-colors',
                            viewMode === 'grid' ? 'bg-primary-500 text-white' : 'bg-white text-slate-600 hover:bg-slate-50'
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
                            'px-3 py-2 transition-colors',
                            viewMode === 'list' ? 'bg-primary-500 text-white' : 'bg-white text-slate-600 hover:bg-slate-50'
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

            {/* Products Grid */}
            {loading ? (
                viewMode === 'grid' ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                        {Array.from({ length: 4 }).map((_, i) => (
                            <SkeletonCard key={i} />
                        ))}
                    </div>
                ) : (
                    <SkeletonTable rows={10} cols={7} />
                )
            ) : viewMode === 'grid' ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {filteredProducts.map((product, index) => {
                        const stockStatus = getStockStatus(product);
                        const profit = ((product.price - product.costPrice) / product.costPrice * 100).toFixed(0);

                        return (
                            <div
                                key={product.id}
                                className="card-elevated group cursor-pointer overflow-hidden animate-fade-in-up"
                                style={{ animationDelay: `${400 + index * 100}ms` }}
                            >
                                {/* Product Image Placeholder */}
                                <div className={cn(
                                    'h-32 bg-gradient-to-br flex items-center justify-center relative',
                                    getCategoryColor(product.category)
                                )}>
                                    <Package className="h-12 w-12 text-white/50" />

                                    {/* Stock Badge */}
                                    <span className={cn(
                                        'absolute top-3 right-3 px-2 py-1 rounded-full text-xs font-medium',
                                        stockStatus.color
                                    )}>
                                        {stockStatus.label}
                                    </span>

                                    {/* Hover Actions */}
                                    <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                                        <button className="p-2 bg-white/20 backdrop-blur-sm rounded-lg text-white hover:bg-white/30 transition-colors">
                                            <Eye className="h-4 w-4" />
                                        </button>
                                        <button className="p-2 bg-white/20 backdrop-blur-sm rounded-lg text-white hover:bg-white/30 transition-colors">
                                            <Edit className="h-4 w-4" />
                                        </button>
                                        <button 
                                            onClick={(e) => { e.stopPropagation(); confirmDelete(product); }}
                                            className="p-2 bg-white/20 backdrop-blur-sm rounded-lg text-white hover:bg-red-500 transition-colors"
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </button>
                                    </div>
                                </div>

                                <div className="p-4">
                                    <p className="text-xs text-slate-500 mb-1">{product.category}</p>
                                    <h3 className="font-semibold text-slate-900 mb-2 line-clamp-1">{product.name}</h3>

                                    <div className="flex items-center justify-between mb-3">
                                        <p className="text-lg font-bold text-slate-900">{formatCurrency(product.price)}</p>
                                        <span className="text-xs text-emerald-600 font-medium bg-emerald-50 px-2 py-0.5 rounded-full">
                                            +{profit}% margin
                                        </span>
                                    </div>

                                    <div className="flex items-center justify-between text-sm text-slate-500">
                                        <span>{product.stock} in stock</span>
                                        <span>{product.sales} sold</span>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            ) : (
                /* List View */
                <div className="card-elevated overflow-hidden animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                    <div className="table-container">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>Product</th>
                                    <th>Category</th>
                                    <th>Price</th>
                                    <th>Stock</th>
                                    <th>Sales</th>
                                    <th>Revenue</th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredProducts.map((product, index) => {
                                    const stockStatus = getStockStatus(product);

                                    return (
                                        <tr key={product.id} className="animate-fade-in" style={{ animationDelay: `${index * 50}ms` }}>
                                            <td>
                                                <div className="flex items-center gap-3">
                                                    <div className={cn(
                                                        'w-10 h-10 rounded-lg bg-gradient-to-br flex items-center justify-center',
                                                        getCategoryColor(product.category)
                                                    )}>
                                                        <Package className="h-5 w-5 text-white/70" />
                                                    </div>
                                                    <div>
                                                        <p className="font-medium text-slate-900">{product.name}</p>
                                                        <p className="text-xs text-slate-500">{product.sku}</p>
                                                    </div>
                                                </div>
                                            </td>
                                            <td className="text-slate-600">{product.category}</td>
                                            <td className="font-semibold text-slate-900">{formatCurrency(product.price)}</td>
                                            <td>
                                                <span className={cn(
                                                    'px-2 py-1 rounded-full text-xs font-medium',
                                                    stockStatus.color
                                                )}>
                                                    {product.stock}
                                                </span>
                                            </td>
                                            <td className="text-slate-600">{product.sales}</td>
                                            <td className="font-semibold text-emerald-600">{formatCurrency(product.revenue)}</td>
                                            <td>
                                                <div className="flex gap-1 justify-end">
                                                    <button className="p-2 hover:bg-slate-100 rounded-lg transition-colors">
                                                        <Edit className="h-4 w-4 text-slate-400" />
                                                    </button>
                                                    <button 
                                                        onClick={() => confirmDelete(product)}
                                                        className="p-2 hover:bg-red-50 text-red-400 hover:text-red-600 rounded-lg transition-colors"
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* Empty State */}
            {!loading && filteredProducts.length === 0 && (
                <div className="card-elevated py-16 text-center animate-fade-in">
                    <Package className="h-12 w-12 text-slate-300 mx-auto mb-4" />
                    <h3 className="text-lg font-semibold text-slate-900 mb-2">No products found</h3>
                    <p className="text-slate-500 mb-6">Try adjusting your filters or add a new product</p>
                    <Link href="/products/new" className="btn btn-primary">
                        <Plus className="h-4 w-4" />
                        Add Product
                    </Link>
                </div>
            )}

            <ConfirmModal
                isOpen={isDeleteModalOpen}
                onClose={() => setIsDeleteModalOpen(false)}
                onConfirm={handleDelete}
                title="Delete Product"
                description={`Are you sure you want to delete ${productToDelete?.name}? This action cannot be undone.`}
                confirmText="Delete"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
