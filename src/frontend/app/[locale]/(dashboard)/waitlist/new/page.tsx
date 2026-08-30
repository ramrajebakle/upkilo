'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
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
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const waitlistSchema = z.object({
    clientId: z.string().optional(),
    clientName: z.string().min(2, 'Client name must be at least 2 characters'),
    email: z.string().email('Invalid email address').optional().or(z.literal('')),
    phone: z.string().optional(),
    serviceId: z.string().min(1, 'Please select a service'),
    preferredDate: z.string().optional(),
    preferredTime: z.string().optional(),
    notes: z.string().optional(),
});

type WaitlistFormData = z.infer<typeof waitlistSchema>;

export default function NewWaitlistPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [services, setServices] = useState<any[]>([]);
    const [clients, setClients] = useState<any[]>([]);
    const [searchQuery, setSearchQuery] = useState('');

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        watch,
        reset,
    } = useForm<WaitlistFormData>({
        resolver: zodResolver(waitlistSchema),
        defaultValues: {
            clientId: '',
            clientName: '',
            email: '',
            phone: '',
            serviceId: '',
            preferredDate: '',
            preferredTime: '',
            notes: '',
        },
    });

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [servicesRes, clientsRes] = await Promise.all([
                    api.services.list(),
                    api.clients.list({ limit: 100 }),
                ]);
                setServices(servicesRes.data.data || []);
                setClients(clientsRes.data.data || []);
            } catch (error) {
                console.error('Failed to fetch initial data', error);
            }
        };
        fetchData();
    }, []);

    const handleClientSelect = (client: any) => {
        setValue('clientId', client.id);
        setValue('clientName', client.firstName + ' ' + client.lastName);
        setValue('email', client.email || '');
        setValue('phone', client.phone || '');
        setSearchQuery('');
    };

    const onSubmit = async (data: WaitlistFormData) => {
        setLoading(true);
        try {
            const selectedService = services.find(s => s.id === data.serviceId);
            const submissionData = {
                ...data,
                serviceName: selectedService?.name || '',
            };

            await api.waitlist.add(submissionData);
            toastSuccess('Client added to waitlist');
            router.push('/waitlist?added=true');
        } catch (error) {
            console.error('Failed to add to waitlist', error);
            toastError('Failed to add to waitlist');
        } finally {
            setLoading(false);
        }
    };

    const filteredClients = searchQuery.length >= 2 
        ? clients.filter(c => 
            (c.firstName + ' ' + c.lastName).toLowerCase().includes(searchQuery.toLowerCase()) ||
            c.email?.toLowerCase().includes(searchQuery.toLowerCase())
          )
        : [];

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
                            Add to Waitlist
                        </h1>
                    </div>
                    <p className="text-foreground-secondary ml-12">Capture client interest for future openings</p>
                </div>
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
                                <div className="relative">
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Search Existing Client
                                    </label>
                                    <div className="relative">
                                        <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                                        <input
                                            type="text"
                                            value={searchQuery}
                                            onChange={(e) => setSearchQuery(e.target.value)}
                                            className="input pl-11"
                                            placeholder="Type name or email..."
                                        />
                                    </div>
                                    {filteredClients.length > 0 && (
                                        <div className="absolute z-20 w-full mt-2 bg-card rounded-xl shadow-2xl border border-border-subtle overflow-hidden max-h-60 overflow-y-auto">
                                            {filteredClients.map(client => (
                                                <button
                                                    key={client.id}
                                                    type="button"
                                                    onClick={() => handleClientSelect(client)}
                                                    className="w-full p-4 hover:bg-accent text-left border-b border-slate-50 last:border-0 transition-colors"
                                                >
                                                    <div className="font-bold text-foreground">{client.firstName} {client.lastName}</div>
                                                    <div className="text-xs text-foreground-secondary">{client.email}</div>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>

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
                                Preference
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-2">
                                        Interested Service <span className="text-danger-fg">*</span>
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
                                        placeholder="Add any specific requirements or notes..."
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
                            <div className="space-y-4 text-sm text-foreground-secondary leading-relaxed">
                                <p>Adding a client to the waitlist ensures they are first in line for cancellations or new availability.</p>
                                <div className="flex items-start gap-3 p-3 bg-cyan-50 rounded-xl text-cyan-700 border border-cyan-100">
                                    <Sparkles className="h-4 w-4 mt-0.5 flex-shrink-0" />
                                    <p className="text-xs">Clients will be automatically prioritized based on their join date.</p>
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
                                        Add to Waitlist
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
        </div>
    );
}
