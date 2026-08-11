'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import {
    ArrowLeft,
    Send,
    Mail,
    MessageSquare,
    Users,
    Calendar,
    Save,
    Sparkles,
    Layout,
    Clock,
    CheckCircle2,
    ChevronRight,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const campaignSchema = z.object({
    name: z.string().min(3, 'Campaign name must be at least 3 characters'),
    type: z.enum(['email', 'sms']),
    subject: z.string().optional(),
    content: z.string().min(10, 'Content must be at least 10 characters'),
    segmentId: z.string().min(1, 'Please select a target audience'),
    scheduledAt: z.string().optional(),
});

type CampaignFormData = z.infer<typeof campaignSchema>;

export default function NewCampaignPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [step, setStep] = useState(1);
    const [loading, setLoading] = useState(false);
    const [segments, setSegments] = useState<any[]>([]);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        watch,
        trigger,
    } = useForm<CampaignFormData>({
        resolver: zodResolver(campaignSchema),
        defaultValues: {
            name: '',
            type: 'email',
            subject: '',
            content: '',
            segmentId: '',
            scheduledAt: '',
        },
    });

    const campaignType = watch('type');
    const segmentId = watch('segmentId');
    const campaignName = watch('name');

    useEffect(() => {
        const fetchSegments = async () => {
            try {
                const res = await api.campaigns.segments.list();
                const data = res.data?.data || res.data || [];
                setSegments(data.map((s: any) => ({
                    id: s.id,
                    name: s.name || s.label || 'Untitled',
                    count: s.count || s.clientCount || 0,
                })));
            } catch (error) {
                console.error('Failed to fetch segments', error);
                // Fallback: try fetching all clients count as a single segment
                try {
                    const clientsRes = await api.clients.list({ limit: 1 });
                    const totalClients = clientsRes.data?.total || clientsRes.data?.data?.length || 0;
                    setSegments([{ id: 'all', name: 'All Clients', count: totalClients }]);
                } catch {
                    setSegments([]);
                }
            }
        };
        fetchSegments();
    }, []);

    const nextStep = async () => {
        let fieldsToValidate: any[] = [];
        if (step === 1) {
            fieldsToValidate = ['name', 'segmentId'];
        } else if (step === 2) {
            fieldsToValidate = ['content'];
            if (campaignType === 'email') fieldsToValidate.push('subject');
        }

        const isValid = await trigger(fieldsToValidate as any);
        if (isValid) {
            setStep(step + 1);
        }
    };

    const onSubmit = async (data: CampaignFormData, isDraft: boolean = true) => {
        setLoading(true);
        try {
            await api.campaigns.create({
                ...data,
                status: isDraft ? 'Draft' : 'Sent',
            });
            toastSuccess(isDraft ? 'Campaign saved as draft' : 'Campaign sent successfully');
            router.push('/marketing/campaigns?created=true');
        } catch (error) {
            console.error('Failed to create campaign', error);
            toastError('Failed to create campaign');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-5xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/marketing"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl shadow-lg shadow-primary-500/25">
                            <Send className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Create Campaign
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Design and send a new marketing campaign</p>
                </div>
            </div>

            {/* Progress */}
            <div className="mb-10 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                <div className="flex items-center justify-between relative max-w-2xl mx-auto">
                    <div className="absolute left-0 right-0 top-5 h-0.5 bg-slate-100" />
                    <div
                        className="absolute left-0 top-5 h-0.5 bg-primary-500 transition-all duration-500"
                        style={{ width: `${((step - 1) / 2) * 100}%` }}
                    />
                    {[
                        { num: 1, label: 'Type & Audience', icon: Users },
                        { num: 2, label: 'Design Content', icon: Layout },
                        { num: 3, label: 'Review & Send', icon: CheckCircle2 },
                    ].map((s) => {
                        const Icon = s.icon;
                        return (
                            <div key={s.num} className="relative flex flex-col items-center z-10 w-32">
                                <div className={cn(
                                    'w-10 h-10 rounded-full flex items-center justify-center transition-all duration-300',
                                    step >= s.num
                                        ? 'bg-primary-600 text-white shadow-lg shadow-primary-200'
                                        : 'bg-white border-2 border-slate-100 text-slate-400'
                                )}>
                                    {step > s.num ? <CheckCircle2 className="h-6 w-6" /> : <Icon className="h-5 w-5" />}
                                </div>
                                <span className={cn(
                                    'text-xs mt-3 font-medium text-center',
                                    step >= s.num ? 'text-slate-900' : 'text-slate-400'
                                )}>
                                    {s.label}
                                </span>
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Step Content */}
            <div className="min-h-[500px]">
                {step === 1 && (
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 animate-fade-in-up">
                        <div className="lg:col-span-2 space-y-6">
                            <div className="card-elevated p-8">
                                <h2 className="text-xl font-bold text-slate-900 mb-6">Campaign Basics</h2>
                                <div className="space-y-6">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Campaign Name <span className="text-red-500">*</span>
                                        </label>
                                        <input
                                            {...register('name')}
                                            type="text"
                                            className={cn("input text-lg", errors.name && "border-red-500")}
                                            placeholder="e.g. Summer Special 2026"
                                        />
                                        {errors.name && <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-3">Choose Channel</label>
                                        <div className="grid grid-cols-2 gap-4">
                                            <button
                                                type="button"
                                                onClick={() => setValue('type', 'email')}
                                                className={cn(
                                                    'p-6 rounded-2xl border-2 transition-all flex flex-col items-center gap-3',
                                                    campaignType === 'email'
                                                        ? 'border-primary-600 bg-primary-50/50 shadow-md'
                                                        : 'border-slate-100 opacity-60 hover:opacity-100'
                                                )}
                                            >
                                                <div className={cn(
                                                    'p-3 rounded-xl',
                                                    campaignType === 'email' ? 'bg-primary-600 text-white' : 'bg-slate-100 text-slate-600'
                                                )}>
                                                    <Mail className="h-6 w-6" />
                                                </div>
                                                <span className="font-bold">Email</span>
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() => setValue('type', 'sms')}
                                                className={cn(
                                                    'p-6 rounded-2xl border-2 transition-all flex flex-col items-center gap-3',
                                                    campaignType === 'sms'
                                                        ? 'border-primary-600 bg-primary-50/50 shadow-md'
                                                        : 'border-slate-100 opacity-60 hover:opacity-100'
                                                )}
                                            >
                                                <div className={cn(
                                                    'p-3 rounded-xl',
                                                    campaignType === 'sms' ? 'bg-primary-600 text-white' : 'bg-slate-100 text-slate-600'
                                                )}>
                                                    <MessageSquare className="h-6 w-6" />
                                                </div>
                                                <span className="font-bold">SMS</span>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="card-elevated p-8">
                                <h2 className="text-xl font-bold text-slate-900 mb-6 font-display">
                                    Select Audience <span className="text-red-500 text-sm ml-1">*</span>
                                </h2>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    {segments.map((segment) => (
                                        <button
                                            key={segment.id}
                                            type="button"
                                            onClick={() => setValue('segmentId', segment.id)}
                                            className={cn(
                                                'p-4 rounded-xl border-2 text-left transition-all',
                                                segmentId === segment.id
                                                    ? 'border-primary-600 bg-primary-50 shadow-sm'
                                                    : 'border-slate-100 hover:border-slate-200'
                                            )}
                                        >
                                            <div className="flex justify-between items-start mb-2">
                                                <div className="p-2 bg-slate-100 rounded-lg">
                                                    <Users className="h-4 w-4 text-slate-600" />
                                                </div>
                                                <span className="text-xs font-bold text-slate-500 bg-white px-2 py-1 rounded-full border border-slate-100">
                                                    {segment.count} clients
                                                </span>
                                            </div>
                                            <h3 className="font-bold text-slate-900">{segment.name}</h3>
                                        </button>
                                    ))}
                                </div>
                                {errors.segmentId && <p className="text-xs text-red-500 mt-4">{errors.segmentId.message}</p>}
                            </div>
                        </div>

                        <div className="space-y-6">
                            <div className="p-6 bg-slate-900 rounded-2xl text-white shadow-xl">
                                <h3 className="font-bold text-lg mb-2 flex items-center gap-2">
                                    <Sparkles className="h-5 w-5 text-amber-400" />
                                    Smart Suggestion
                                </h3>
                                <p className="text-slate-400 text-sm leading-relaxed">
                                    SMS campaigns have a 98% open rate. For urgent offers or appointment reminders, SMS is often more effective than email.
                                </p>
                            </div>
                            <button
                                type="button"
                                onClick={nextStep}
                                className="w-full btn btn-primary py-4 group"
                            >
                                Next: Design Content
                                <ChevronRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
                            </button>
                        </div>
                    </div>
                )}

                {step === 2 && (
                    <div className="space-y-6 animate-fade-in-up">
                        <div className="card-elevated p-8">
                            <div className="flex items-center justify-between mb-8">
                                <h2 className="text-xl font-bold text-slate-900">Design Your {campaignType.toUpperCase()}</h2>
                                <div className="flex gap-2">
                                    <button type="button" className="btn btn-secondary text-sm">Preview</button>
                                    <button type="button" className="btn btn-secondary text-sm">Send Test</button>
                                </div>
                            </div>

                            <div className="space-y-6">
                                {campaignType === 'email' && (
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Email Subject Line <span className="text-red-500">*</span>
                                        </label>
                                        <input
                                            {...register('subject')}
                                            type="text"
                                            className={cn("input text-lg", errors.subject && "border-red-500")}
                                            placeholder="Catchy subject line..."
                                        />
                                        {errors.subject && <p className="text-xs text-red-500 mt-1">{errors.subject.message as string}</p>}
                                    </div>
                                )}
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Message Content <span className="text-red-500">*</span>
                                    </label>
                                    <textarea
                                        {...register('content')}
                                        className={cn("input min-h-[300px] py-4 resize-none", errors.content && "border-red-500")}
                                        placeholder={campaignType === 'email' ? 'Dear {{firstname}}, ...' : 'Hi {{firstname}}! Summer Sale starts now...'}
                                    />
                                    {errors.content && <p className="text-xs text-red-500 mt-1">{errors.content.message}</p>}
                                    <div className="mt-4 flex flex-wrap gap-2">
                                        <span className="text-xs text-slate-400 uppercase font-bold tracking-wider mr-2 self-center">Insert Tags:</span>
                                        {['{{firstname}}', '{{company}}', '{{booking_url}}', '{{unsubscribe_url}}'].map(tag => (
                                            <button
                                                key={tag}
                                                type="button"
                                                onClick={() => {
                                                    const cur = watch('content');
                                                    setValue('content', cur + tag);
                                                }}
                                                className="px-3 py-1 bg-slate-100 hover:bg-slate-200 rounded text-xs font-mono text-slate-600 transition-colors"
                                            >
                                                {tag}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="flex justify-between">
                            <button type="button" onClick={() => setStep(1)} className="btn btn-secondary py-4 px-8">Back</button>
                            <button
                                type="button"
                                onClick={nextStep}
                                className="btn btn-primary py-4 px-8 group"
                            >
                                Next: Review & Schedule
                                <ChevronRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
                            </button>
                        </div>
                    </div>
                )}

                {step === 3 && (
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 animate-fade-in-up">
                        <div className="lg:col-span-2 space-y-6">
                            <div className="card-elevated p-8">
                                <h2 className="text-xl font-bold text-slate-900 mb-6">Final Review</h2>
                                <div className="space-y-4">
                                    <div className="flex justify-between py-3 border-b border-slate-50 text-sm">
                                        <span className="text-slate-500">Campaign Name</span>
                                        <span className="font-bold text-slate-900">{watch('name')}</span>
                                    </div>
                                    <div className="flex justify-between py-3 border-b border-slate-50 text-sm">
                                        <span className="text-slate-500">Type</span>
                                        <span className="font-bold text-primary-600 flex items-center gap-2 capitalize">
                                            {campaignType === 'email' ? <Mail className="h-4 w-4" /> : <MessageSquare className="h-4 w-4" />}
                                            {campaignType}
                                        </span>
                                    </div>
                                    <div className="flex justify-between py-3 border-b border-slate-50 text-sm">
                                        <span className="text-slate-500">Audience</span>
                                        <span className="font-bold text-slate-900">
                                            {segments.find(s => s.id === segmentId)?.name || 'None selected'}
                                        </span>
                                    </div>
                                    {campaignType === 'email' && (
                                        <div className="flex justify-between py-3 border-b border-slate-50 text-sm">
                                            <span className="text-slate-500">Subject</span>
                                            <span className="font-bold text-slate-900">{watch('subject')}</span>
                                        </div>
                                    )}
                                </div>
                            </div>

                            <div className="card-elevated p-8 overflow-hidden">
                                <h3 className="text-sm font-bold text-slate-400 uppercase tracking-widest mb-4">Content Preview</h3>
                                <div className="p-6 bg-slate-50 rounded-2xl border border-slate-100 whitespace-pre-wrap font-sans text-slate-700">
                                    {watch('content')}
                                </div>
                            </div>
                        </div>

                        <div className="space-y-6">
                            <div className="card-elevated p-6">
                                <h3 className="font-bold text-slate-900 mb-4 flex items-center gap-2">
                                    <Calendar className="h-5 w-5 text-primary-500" />
                                    Schedule
                                </h3>
                                <div className="space-y-4">
                                    <label className="flex items-center gap-3 p-3 rounded-xl border border-slate-100 hover:bg-slate-50 transition-colors cursor-pointer">
                                        <input
                                            type="radio"
                                            name="schedule"
                                            defaultChecked
                                            className="w-4 h-4 text-primary-600 focus:ring-primary-500"
                                        />
                                        <span className="text-sm font-medium text-slate-700">Send Immediately</span>
                                    </label>
                                    <label className="flex items-center gap-3 p-3 rounded-xl border border-slate-100 hover:bg-slate-50 transition-colors cursor-pointer">
                                        <input
                                            type="radio"
                                            name="schedule"
                                            className="w-4 h-4 text-primary-600 focus:ring-primary-500"
                                        />
                                        <span className="text-sm font-medium text-slate-700">Schedule for later</span>
                                    </label>
                                </div>
                            </div>

                            <div className="flex flex-col gap-3">
                                <button
                                    type="button"
                                    onClick={handleSubmit((data) => onSubmit(data, false))}
                                    disabled={loading}
                                    className="w-full btn btn-primary py-4 shadow-xl shadow-primary-500/25 relative overflow-hidden group"
                                >
                                    {loading ? (
                                        <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin mx-auto" />
                                    ) : (
                                        <>
                                            <Send className="h-4 w-4" />
                                            Send Campaign Now
                                        </>
                                    )}
                                </button>
                                <button
                                    type="button"
                                    onClick={handleSubmit((data) => onSubmit(data, true))}
                                    className="w-full btn btn-secondary py-4"
                                >
                                    <Save className="h-4 w-4" />
                                    Save as Draft
                                </button>
                                <button type="button" onClick={() => setStep(2)} className="w-full text-sm text-slate-500 hover:text-slate-700 font-medium py-2">
                                    Edit Design
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
