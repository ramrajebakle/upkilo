'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import {
    ArrowLeft,
    Clock,
    User,
    Mail,
    Phone,
    Briefcase,
    Calendar,
    Save,
    Sparkles,
    CheckCircle2,
    Info,
    Search,
    Trash2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { ConfirmModal } from '@/components/ui/Modal';

const waitlistSchema = z.object({
    clientName: z.string().min(2, 'Client name must be at least 2 characters'),
    email: z.string().email('Invalid email address').optional().or(z.literal('')),
    phone: z.string().optional(),
    serviceId: z.string().min(1, 'Please select a service'),
    preferredDate: z.string().optional(),
    preferredTime: z.string().optional(),
    status: z.enum(['Waiting', 'Notified', 'Booked', 'Cancelled', 'Expired']),
    notes: z.string().optional(),
});

type WaitlistFormData = z.infer<typeof waitlistSchema>;

export default function EditWaitlistPage() {
    const router = useRouter();
    const params = useParams();
    const id = params.id as string;
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);
    const [services, setServices] = useState<any[]>([]);
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [createdAt, setCreatedAt] = useState<string>('');

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        reset,
        watch,
    } = useForm<WaitlistFormData>({
        resolver: zodResolver(waitlistSchema),
    });

    const clientName = watch('clientName');

    useEffect(() => {
        const fetchData = async () => {
            if (!id) return;
            setFetching(true);
            try {
                const [servicesRes, entryRes] = await Promise.all([
                    api.services.list(),
                    api.waitlist.get(id),
                ]);
                setServices(servicesRes.data.data || []);
                
                // entryRes.data refers to the AxiosResponse data property.
                // The actual payload might be nested in data again based on IApiResponse.
                const rawData = entryRes.data;
                const data = "data" in rawData && rawData.data ? (rawData.data as any) : rawData;
                
                setCreatedAt(data.createdAt || new Date().toISOString());
                
                // Format date for the input
                const formattedData = {
                    ...data,
                    preferredDate: data.preferredDate ? new Date(data.preferredDate).toISOString().split('T')[0] : '',
                };
                
                reset(formattedData);
            } catch (error) {
                console.error('Failed to fetch waitlist data', error);
                toastError('Failed to load waitlist entry');
                router.push('/waitlist');
            } finally {
                setFetching(false);
            }
        };
        fetchData();
    }, [id, router, toastError, reset]);

    const onSubmit = async (data: WaitlistFormData) => {
        setLoading(true);
        try {
            const selectedService = services.find(s => s.id === data.serviceId);
            const submissionData = {
                ...data,
                serviceName: selectedService?.name || '',
            };

            await api.waitlist.update(id, submissionData);
            toastSuccess('Waitlist entry updated');
            router.push('/waitlist?updated=true');
        } catch (error) {
            console.error('Failed to update waitlist entry', error);
            toastError('Failed to update waitlist entry');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            await api.waitlist.remove(id);
            toastSuccess('Waitlist entry removed');
            router.push('/waitlist?removed=true');
        } catch (error) {
            console.error('Failed to remove waitlist entry', error);
            toastError('Failed to remove waitlist entry');
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
                    href="/waitlist"
                    className="p-2 hover:bg-accent rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-foreground-secondary" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-cyan-500 to-blue-600 rounded-xl shadow-lg shadow-cyan-500/25">
                            <Clock className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-foreground"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Edit Waitlist Entry
                        </h1>
                    </div>
                    <p className="text-foreground-secondary ml-12">Update client preferences and status</p>
                </div>
                <button 
                    type="button"
                    onClick={() => setIsDeleteModalOpen(true)}
                    className="p-2 hover:bg-red-50 text-red-400 hover:text-red-600 rounded-xl transition-colors"
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
                                <User className="h-5 w-5 text-blue-500" />
                                Client Information
                            </h2>
                            <div className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Client Name <span className="text-danger-fg">*</span>
                                        </label>
                                        <input
                                            {...register('clientName')}
                                            type="text"
                                            className={cn("input", errors.clientName && "border-red-500")}
                                            placeholder="Full Name"
                                        />
                                        {errors.clientName && <p className="text-xs text-danger-fg mt-1">{errors.clientName.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Email
                                        </label>
                                        <div className="relative">
                                            <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                            <input
                                                {...register('email')}
                                                type="email"
                                                className={cn("input pl-11", errors.email && "border-red-500")}
                                                placeholder="email@example.com"
                                            />
                                        </div>
                                        {errors.email && <p className="text-xs text-danger-fg mt-1">{errors.email.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Phone
                                        </label>
                                        <div className="relative">
                                            <Phone className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                            <input
                                                {...register('phone')}
                                                type="tel"
                                                className="input pl-11"
                                                placeholder="(555) 000-0000"
                                            />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                            <h2 className="text-lg font-semibold text-foreground mb-6 flex items-center gap-2">
                                <Briefcase className="h-5 w-5 text-primary" />
                                Preference & Status
                            </h2>
                            <div className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Service <span className="text-danger-fg">*</span>
                                        </label>
                                        <select
                                            {...register('serviceId')}
                                            className={cn("input", errors.serviceId && "border-red-500")}
                                        >
                                            <option value="">Select a service</option>
                                            {services.map(s => (
                                                <option key={s.id} value={s.id}>{s.name}</option>
                                            ))}
                                        </select>
                                        {errors.serviceId && <p className="text-xs text-danger-fg mt-1">{errors.serviceId.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Status <span className="text-danger-fg">*</span>
                                        </label>
                                        <select
                                            {...register('status')}
                                            className={cn("input", errors.status && "border-red-500")}
                                        >
                                            <option value="Waiting">Waiting</option>
                                            <option value="Notified">Notified</option>
                                            <option value="Booked">Booked</option>
                                            <option value="Cancelled">Cancelled</option>
                                            <option value="Expired">Expired</option>
                                        </select>
                                        {errors.status && <p className="text-xs text-danger-fg mt-1">{errors.status.message}</p>}
                                    </div>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Preferred Date
                                        </label>
                                        <div className="relative">
                                            <Calendar className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                            <input
                                                {...register('preferredDate')}
                                                type="date"
                                                className="input pl-11"
                                            />
                                        </div>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-2">
                                            Preferred Time
                                        </label>
                                        <select
                                            {...register('preferredTime')}
                                            className="input"
                                        >
                                            <option value="">Any Time</option>
                                            <option value="Morning">Morning</option>
                                            <option value="Afternoon">Afternoon</option>
                                            <option value="Evening">Evening</option>
                                        </select>
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Notes
                                    </label>
                                    <textarea
                                        {...register('notes')}
                                        className="input min-h-[100px] py-3"
                                        placeholder="Add notes..."
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Sidebar Info */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-foreground mb-6 flex items-center gap-2">
                                <Info className="h-5 w-5 text-cyan-500" />
                                Waitlist Info
                            </h2>
                            <div className="space-y-4 text-sm text-foreground-secondary">
                                <div className="p-3 bg-amber-50 rounded-xl text-amber-700 border border-amber-100">
                                    <Sparkles className="h-4 w-4 mb-2" />
                                    <p className="text-xs">Client has been waiting since {new Date(createdAt).toLocaleDateString()}</p>
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
                                href="/waitlist"
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
                title="Remove Entry"
                description={`Remove ${clientName} from the waitlist?`}
                confirmText="Remove"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
