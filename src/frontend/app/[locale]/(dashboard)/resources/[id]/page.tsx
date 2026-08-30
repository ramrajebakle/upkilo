'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import {
    ArrowLeft,
    Monitor,
    Tag,
    MapPin,
    Users,
    Info,
    Save,
    Sparkles,
    CheckCircle2,
    Briefcase,
    Activity,
    Trash2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { ConfirmModal } from '@/components/ui/Modal';

const resourceSchema = z.object({
    name: z.string().min(2, 'Resource name must be at least 2 characters'),
    type: z.string().min(1, 'Please select a resource type'),
    location: z.string().optional(),
    capacity: z.number().min(1, 'Capacity must be at least 1'),
    description: z.string().optional(),
    status: z.string().min(1, 'Please select a status'),
});

type ResourceFormData = z.infer<typeof resourceSchema>;

export default function EditResourcePage() {
    const router = useRouter();
    const params = useParams();
    const id = params.id as string;
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        reset,
        watch,
    } = useForm<ResourceFormData>({
        resolver: zodResolver(resourceSchema),
    });

    const resourceName = watch('name');

    useEffect(() => {
        const fetchResource = async () => {
            if (!id) return;
            setFetching(true);
            try {
                const res = await api.resources.get(id);
                reset(res.data);
            } catch (error) {
                console.error('Failed to fetch resource', error);
                toastError('Failed to load resource details');
                router.push('/resources');
            } finally {
                setFetching(false);
            }
        };
        fetchResource();
    }, [id, router, toastError, reset]);

    const onSubmit = async (data: ResourceFormData) => {
        setLoading(true);
        try {
            await api.resources.update(id, data);
            toastSuccess('Resource updated successfully');
            router.push('/resources?updated=true');
        } catch (error) {
            console.error('Failed to update resource', error);
            toastError('Failed to update resource');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            await api.resources.delete(id);
            toastSuccess('Resource deleted successfully');
            router.push('/resources?deleted=true');
        } catch (error) {
            console.error('Failed to delete resource', error);
            toastError('Failed to delete resource');
        } finally {
            setIsDeleting(false);
            setIsDeleteModalOpen(false);
        }
    };

    if (fetching) {
        return (
            <div className="max-w-4xl mx-auto animate-pulse space-y-8">
                <div className="h-20 bg-muted rounded-2xl w-full" />
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <div className="lg:col-span-2 h-96 bg-muted rounded-2xl" />
                    <div className="h-96 bg-muted rounded-2xl" />
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/resources"
                    className="p-2 hover:bg-accent rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-foreground-secondary" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-teal-500 to-emerald-600 rounded-xl shadow-lg shadow-teal-500/25">
                            <Monitor className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-foreground"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Edit Resource
                        </h1>
                    </div>
                    <p className="text-foreground-secondary ml-12">Modify resource details and availability</p>
                </div>
                <button 
                    type="button"
                    onClick={() => setIsDeleteModalOpen(true)}
                    className="p-2 hover:bg-red-50 text-red-400 hover:text-red-600 rounded-xl transition-colors"
                    title="Delete Resource"
                >
                    <Trash2 className="h-5 w-5" />
                </button>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Main Info */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                            <h2 className="text-lg font-semibold text-foreground mb-6 flex items-center gap-2">
                                <Tag className="h-5 w-5 text-primary" />
                                Resource Overview
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Resource Name <span className="text-danger-fg">*</span>
                                    </label>
                                    <input
                                        {...register('name')}
                                        type="text"
                                        className={cn("input", errors.name && "border-red-500")}
                                        placeholder="e.g. VIP Treatment Room 1"
                                    />
                                    {errors.name && <p className="text-xs text-danger-fg mt-1">{errors.name.message}</p>}
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Resource Type <span className="text-danger-fg">*</span>
                                        </label>
                                        <div className="relative">
                                            <Briefcase className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                            <select
                                                {...register('type')}
                                                className={cn("input pl-11 appearance-none", errors.type && "border-red-500")}
                                            >
                                                <option value="Room">Room / Studio</option>
                                                <option value="Equipment">Equipment</option>
                                                <option value="Facility">Facility</option>
                                                <option value="Vehicle">Vehicle</option>
                                            </select>
                                        </div>
                                        {errors.type && <p className="text-xs text-danger-fg mt-1">{errors.type.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Capacity <span className="text-danger-fg">*</span>
                                        </label>
                                        <div className="relative">
                                            <Users className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                            <input
                                                {...register('capacity', { valueAsNumber: true })}
                                                type="number"
                                                min="1"
                                                className={cn("input pl-11", errors.capacity && "border-red-500")}
                                            />
                                        </div>
                                        {errors.capacity && <p className="text-xs text-danger-fg mt-1">{errors.capacity.message}</p>}
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Location
                                    </label>
                                    <div className="relative">
                                        <MapPin className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                        <input
                                            {...register('location')}
                                            type="text"
                                            className="input pl-11"
                                            placeholder="Floor, Wing, or Room Number"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Description
                                    </label>
                                    <textarea
                                        {...register('description')}
                                        className="input min-h-[100px] py-3"
                                        placeholder="Tell us more about this resource..."
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                            <h2 className="text-lg font-semibold text-foreground mb-6 flex items-center gap-2">
                                <Activity className="h-5 w-5 text-success-fg" />
                                Status & Availability
                            </h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Resource Status <span className="text-danger-fg">*</span>
                                    </label>
                                    <select
                                        {...register('status')}
                                        className={cn("input", errors.status && "border-red-500")}
                                    >
                                        <option value="available">Available</option>
                                        <option value="in-use">In Use</option>
                                        <option value="maintenance">Maintenance</option>
                                        <option value="unavailable">Unavailable</option>
                                    </select>
                                    {errors.status && <p className="text-xs text-danger-fg mt-1">{errors.status.message}</p>}
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Sidebar Info */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-foreground mb-6 flex items-center gap-2">
                                <Info className="h-5 w-5 text-blue-500" />
                                Usage Stats
                            </h2>
                            <div className="space-y-4 text-sm text-foreground-secondary">
                                <p>Used in 12 bookings this week.</p>
                                <div className="flex items-start gap-3 p-3 bg-emerald-50 rounded-xl text-emerald-700 border border-emerald-100">
                                    <Sparkles className="h-4 w-4 mt-0.5 flex-shrink-0" />
                                    <p className="text-xs">Highly utilized: 85% occupancy rate.</p>
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
                                        <CheckCircle2 className="h-5 w-5" />
                                        Save Changes
                                    </>
                                )}
                            </button>
                            <Link
                                href="/resources"
                                className="w-full btn btn-secondary text-center py-4"
                            >
                                Cancel
                            </Link>
                        </div>
                    </div>
                </div>
            </form>

            <ConfirmModal
                isOpen={isDeleteModalOpen}
                onClose={() => setIsDeleteModalOpen(false)}
                onConfirm={handleDelete}
                title="Delete Resource"
                description={`Are you sure you want to delete "${resourceName}"? This action cannot be undone.`}
                confirmText="Delete"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
