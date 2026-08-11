'use client';

import { cn } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import { useEffect, useState } from 'react';
import {
    Monitor, Plus, Search, MoreHorizontal, Calendar,
    Clock, MapPin, Wrench, AlertTriangle, MonitorX, Trash2, Edit2
} from 'lucide-react';
import Link from 'next/link';
import { PageHeader, EmptyState, Pagination, SkeletonCard } from '@/components/ui';
import { ConfirmModal } from '@/components/ui/Modal';
import { useToast } from '@/components/ui/Toast';

interface Resource {
    id: string;
    name: string;
    type: string;
    location: string;
    capacity: number;
    status: string;
    nextAvailable: string;
    description: string;
}

export default function ResourcesPage() {
    const [resources, setResources] = useState<Resource[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const { success, error: toastError } = useToast();

    // Pagination & Delete State
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 6;
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [resourceToDelete, setResourceToDelete] = useState<Resource | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        const fetchResources = async () => {
            setLoading(true);
            try {
                const res = await apiClient.get('/api/v1/resources');
                setResources(res.data.data || []);
            } catch (err) {
                console.error('Failed to fetch resources:', err);
            } finally {
                setLoading(false);
            }
        };
        fetchResources();
    }, []);

    const filtered = resources.filter((r) =>
        r.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        r.type.toLowerCase().includes(searchQuery.toLowerCase())
    );

    // Pagination
    const totalPages = Math.ceil(filtered.length / itemsPerPage);
    const paginatedResources = filtered.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    );

    const handleDelete = async () => {
        if (!resourceToDelete) return;
        setIsDeleting(true);
        try {
            await apiClient.delete(`/api/v1/resources/${resourceToDelete.id}`);
            setResources(current => current.filter(r => r.id !== resourceToDelete.id));
            success('Resource deleted successfully');
            setIsDeleteModalOpen(false);
            if (paginatedResources.length === 1 && currentPage > 1) {
                setCurrentPage(currentPage - 1);
            }
        } catch (err) {
            console.error('Failed to delete resource:', err);
            toastError('Failed to delete resource');
        } finally {
            setIsDeleting(false);
        }
    };

    const confirmDelete = (resource: Resource) => {
        setResourceToDelete(resource);
        setIsDeleteModalOpen(true);
    };

    const statusIcons: Record<string, typeof Monitor> = {
        available: Clock,
        'in-use': Calendar,
        maintenance: Wrench,
        unavailable: AlertTriangle,
    };

    const statusColors: Record<string, string> = {
        available: 'bg-emerald-50 text-emerald-700 border-emerald-200',
        'in-use': 'bg-blue-50 text-blue-700 border-blue-200',
        maintenance: 'bg-amber-50 text-amber-700 border-amber-200',
        unavailable: 'bg-red-50 text-red-700 border-red-200',
    };

    return (
        <div className="space-y-6">
            <PageHeader
                title="Resources"
                description="Manage rooms, equipment, and shared resources"
                icon={Monitor}
                iconGradient="from-teal-500 to-emerald-600"
                iconShadow="shadow-teal-500/25"
                actions={
                    <Link 
                        href="/resources/new"
                        className="inline-flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-primary-500/25 hover:shadow-xl transition-all hover:-translate-y-0.5 text-sm"
                    >
                        <Plus className="h-4 w-4" />
                        Add Resource
                    </Link>
                }
            />

            {/* Search */}
            <div className="relative max-w-md">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                <input
                    type="text"
                    placeholder="Search resources..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-sm focus:ring-2 focus:ring-primary-500/20 focus:border-primary-300 transition-all"
                />
            </div>

            {/* Resource Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {loading ? (
                    Array.from({ length: itemsPerPage }).map((_, i) => (
                        <SkeletonCard key={i} />
                    ))
                ) : filtered.length === 0 ? (
                    <div className="col-span-full">
                        <EmptyState
                            icon={MonitorX}
                            title="No resources found"
                            description="Add rooms, equipment, or other shared resources you manage."
                            action={
                                <Link href="/resources/new" className="btn btn-primary">
                                    <Plus className="h-4 w-4" />
                                    Add Resource
                                </Link>
                            }
                        />
                    </div>
                ) : (
                    paginatedResources.map((resource, i) => {
                        const StatusIcon = statusIcons[resource.status] || Monitor;
                        return (
                            <div
                                key={resource.id}
                                className="bg-white rounded-2xl border border-slate-100 p-6 hover:shadow-xl hover:border-primary-200 transition-all group animate-fade-in-up"
                                style={{ animationDelay: `${i * 80}ms` }}
                            >
                                <div className="flex items-start justify-between mb-4">
                                    <div>
                                        <h3 className="text-lg font-semibold text-slate-900">{resource.name}</h3>
                                        <p className="text-sm text-slate-400 capitalize">{resource.type}</p>
                                    </div>
                                    <span className={cn(
                                        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border capitalize',
                                        statusColors[resource.status] || 'bg-slate-100 text-slate-500 border-slate-200'
                                    )}>
                                        <StatusIcon className="h-3 w-3" />
                                        {resource.status}
                                    </span>
                                </div>

                                {resource.description && (
                                    <p className="text-sm text-slate-500 mb-4 line-clamp-2">{resource.description}</p>
                                )}

                                <div className="space-y-2 text-sm text-slate-600">
                                    {resource.location && (
                                        <div className="flex items-center gap-2">
                                            <MapPin className="h-3.5 w-3.5 text-slate-400" />
                                            <span>{resource.location}</span>
                                        </div>
                                    )}
                                    <div className="flex items-center gap-2">
                                        <Calendar className="h-3.5 w-3.5 text-slate-400" />
                                        <span>Capacity: {resource.capacity}</span>
                                    </div>
                                </div>

                                <div className="mt-4 pt-4 border-t border-slate-100 flex justify-between items-center">
                                    <span className="text-xs text-slate-400">
                                        {resource.nextAvailable ? `Next available: ${new Date(resource.nextAvailable).toLocaleDateString()}` : 'Available now'}
                                    </span>
                                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                        <Link 
                                            href={`/resources/${resource.id}`}
                                            className="p-1.5 hover:bg-slate-100 rounded-lg transition-colors text-slate-400 hover:text-primary-600"
                                            title="Edit"
                                        >
                                            <Edit2 className="h-4 w-4" />
                                        </Link>
                                        <button 
                                            onClick={() => confirmDelete(resource)}
                                            className="p-1.5 hover:bg-red-50 text-red-400 hover:text-red-600 rounded-lg transition-colors"
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </button>
                                        <button className="p-1.5 hover:bg-slate-100 rounded-lg transition-colors">
                                            <MoreHorizontal className="h-4 w-4 text-slate-400" />
                                        </button>
                                    </div>
                                </div>
                            </div>
                        );
                    })
                )}
            </div>

            {!loading && totalPages > 1 && (
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
                title="Delete Resource"
                description={`Are you sure you want to delete ${resourceToDelete?.name}? This action cannot be undone.`}
                confirmText="Delete"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
