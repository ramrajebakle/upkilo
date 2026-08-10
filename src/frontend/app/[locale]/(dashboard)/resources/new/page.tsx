'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
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
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const resourceSchema = z.object({
    name: z.string().min(2, 'Resource name must be at least 2 characters'),
    type: z.string().min(1, 'Please select a resource type'),
    location: z.string().optional(),
    capacity: z.number().min(1, 'Capacity must be at least 1'),
    description: z.string().optional(),
    status: z.string().min(1, 'Please select a status'),
});

type ResourceFormData = z.infer<typeof resourceSchema>;

export default function NewResourcePage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        watch,
    } = useForm<ResourceFormData>({
        resolver: zodResolver(resourceSchema),
        defaultValues: {
            name: '',
            type: 'Room',
            location: '',
            capacity: 1,
            description: '',
            status: 'available',
        },
    });

    const onSubmit = async (data: ResourceFormData) => {
        setLoading(true);
        try {
            await api.resources.create(data);
            toastSuccess('Resource added successfully');
            router.push('/resources?added=true');
        } catch (error) {
            console.error('Failed to add resource', error);
            toastError('Failed to add resource');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/resources"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-teal-500 to-emerald-600 rounded-xl shadow-lg shadow-teal-500/25">
                            <Monitor className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Add New Resource
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Register a new room, equipment, or facility</p>
                </div>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Main Info */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Tag className="h-5 w-5 text-primary-500" />
                                Resource Overview
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Resource Name <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        {...register('name')}
                                        type="text"
                                        className={cn("input", errors.name && "border-red-500")}
                                        placeholder="e.g. VIP Treatment Room 1"
                                    />
                                    {errors.name && <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>}
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Resource Type <span className="text-red-500">*</span>
                                        </label>
                                        <div className="relative">
                                            <Briefcase className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
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
                                        {errors.type && <p className="text-xs text-red-500 mt-1">{errors.type.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Capacity <span className="text-red-500">*</span>
                                        </label>
                                        <div className="relative">
                                            <Users className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                            <input
                                                {...register('capacity', { valueAsNumber: true })}
                                                type="number"
                                                min="1"
                                                className={cn("input pl-11", errors.capacity && "border-red-500")}
                                            />
                                        </div>
                                        {errors.capacity && <p className="text-xs text-red-500 mt-1">{errors.capacity.message}</p>}
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Location
                                    </label>
                                    <div className="relative">
                                        <MapPin className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                        <input
                                            {...register('location')}
                                            type="text"
                                            className="input pl-11"
                                            placeholder="Floor, Wing, or Room Number"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
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
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Activity className="h-5 w-5 text-emerald-500" />
                                Status & Availability
                            </h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Initial Status <span className="text-red-500">*</span>
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
                                    {errors.status && <p className="text-xs text-red-500 mt-1">{errors.status.message}</p>}
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Sidebar Info */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Info className="h-5 w-5 text-blue-500" />
                                Helpful Tips
                            </h2>
                            <div className="space-y-4 text-sm text-slate-600">
                                <p>Resources are used to prevent overbooking rooms or equipment shared between services.</p>
                                <div className="flex items-start gap-3 p-3 bg-blue-50 rounded-xl text-blue-700 border border-blue-100">
                                    <Sparkles className="h-4 w-4 mt-0.5 flex-shrink-0" />
                                    <p className="text-xs">You can link services to specific resources later in the service management section.</p>
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
                                        Adding...
                                    </>
                                ) : (
                                    <>
                                        <CheckCircle2 className="h-5 w-5" />
                                        Save Resource
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
        </div>
    );
}
