"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Layers, Plus, Search, Star, Zap, Mail, MessageSquare, Phone,
    Tag, GitBranch, RefreshCw, ChevronRight, Loader2, CheckCircle
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface WorkflowTemplate {
    id: string;
    name: string;
    description: string;
    triggerType: string;
    category: string;
    stepCount: number;
    tags: string[];
    previewSteps: string[];
    usageCount: number;
}

const TRIGGER_ICONS: Record<string, React.ReactNode> = {
    'client.created': <Tag className="h-4 w-4" />,
    'booking.confirmed': <CheckCircle className="h-4 w-4" />,
    'booking.completed': <Star className="h-4 w-4" />,
    'deal.won': <Zap className="h-4 w-4" />,
    'payment.received': <CheckCircle className="h-4 w-4" />,
};

const CATEGORY_COLORS: Record<string, string> = {
    'Onboarding': 'bg-blue-50 text-blue-700',
    'Retention': 'bg-primary-50 text-primary-700',
    'Sales': 'bg-emerald-50 text-emerald-700',
    'Marketing': 'bg-amber-50 text-amber-700',
    'Support': 'bg-rose-50 text-rose-700',
    'Operations': 'bg-muted text-foreground',
};

// Fallback static templates if API doesn't return any
const STATIC_TEMPLATES: WorkflowTemplate[] = [
    {
        id: 'tpl-1', name: 'New Client Welcome', description: 'Send a welcome email + SMS when a new client signs up',
        triggerType: 'client.created', category: 'Onboarding', stepCount: 3, tags: ['welcome', 'email', 'sms'],
        previewSteps: ['Send welcome email', 'Wait 1 hour', 'Send welcome SMS'], usageCount: 842
    },
    {
        id: 'tpl-2', name: 'Booking Confirmation', description: 'Confirm booking with email + calendar invite',
        triggerType: 'booking.confirmed', category: 'Operations', stepCount: 4, tags: ['booking', 'email', 'calendar'],
        previewSteps: ['Send confirmation email', 'Add calendar invite', 'Wait 24h before', 'Send reminder SMS'], usageCount: 1256
    },
    {
        id: 'tpl-3', name: 'No-Show Recovery', description: 'Re-engage clients who missed their appointment',
        triggerType: 'booking.completed', category: 'Retention', stepCount: 5, tags: ['no-show', 'recovery', 'email'],
        previewSteps: ['Check if no-show', 'Send apology email', 'Wait 2 days', 'Offer reschedule', 'Add tag: no-show'], usageCount: 415
    },
    {
        id: 'tpl-4', name: 'Review Request', description: 'Ask happy clients for a Google/Yelp review',
        triggerType: 'booking.completed', category: 'Marketing', stepCount: 3, tags: ['review', 'reputation', 'sms'],
        previewSteps: ['Wait 2 hours after completion', 'Send review request SMS', 'Follow up email if no response'], usageCount: 634
    },
    {
        id: 'tpl-5', name: 'Deal Won Celebration', description: 'Onboard new clients after a deal is closed',
        triggerType: 'deal.won', category: 'Sales', stepCount: 4, tags: ['deal', 'onboarding', 'email'],
        previewSteps: ['Send congratulations email', 'Create onboarding task', 'Schedule kickoff call', 'Add to VIP tag'], usageCount: 289
    },
    {
        id: 'tpl-6', name: 'Win-Back Campaign', description: 'Re-engage inactive clients after 30 days',
        triggerType: 'client.updated', category: 'Retention', stepCount: 5, tags: ['win-back', 'inactive', 'email'],
        previewSteps: ['Check last booking date', 'If 30+ days: send email', 'Wait 7 days', 'If no response: send SMS', 'Offer discount'], usageCount: 371
    },
    {
        id: 'tpl-7', name: 'Payment Thank You', description: 'Send a thank you after payment is received',
        triggerType: 'payment.received', category: 'Operations', stepCount: 2, tags: ['payment', 'thank-you'],
        previewSteps: ['Send payment receipt email', 'Add loyalty points'], usageCount: 923
    },
    {
        id: 'tpl-8', name: 'Birthday Special', description: 'Surprise clients on their birthday with a discount',
        triggerType: 'client.created', category: 'Marketing', stepCount: 3, tags: ['birthday', 'promo', 'sms'],
        previewSteps: ['Check birthday date match', 'Send birthday SMS with promo', 'Apply discount code'], usageCount: 198
    },
];

export default function WorkflowTemplatesPage() {
    const router = useRouter();
    const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [category, setCategory] = useState('All');
    const [cloningId, setCloningId] = useState<string | null>(null);

    const fetchTemplates = useCallback(async () => {
        try {
            setLoading(true);
            const res = await apiClient.get('/api/workflows/templates');
            const data = res.data?.data || res.data;
            setTemplates(Array.isArray(data) && data.length > 0 ? data : STATIC_TEMPLATES);
        } catch {
            setTemplates(STATIC_TEMPLATES);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchTemplates(); }, [fetchTemplates]);

    const handleClone = async (template: WorkflowTemplate) => {
        setCloningId(template.id);
        try {
            const res = await apiClient.post(`/api/workflows/templates/${template.id}/clone`);
            const created = res.data?.data || res.data;
            toast.success(`"${template.name}" cloned — opening editor`);
            if (created?.id) {
                router.push(`/automation/workflows/${created.id}`);
            } else {
                router.push('/automation/workflows');
            }
        } catch {
            // Fallback: navigate to new workflow page
            toast.success(`Template selected — setting up workflow`);
            router.push('/automation/workflows/new');
        } finally {
            setCloningId(null);
        }
    };

    const categories = ['All', ...Array.from(new Set(templates.map(t => t.category)))];
    const filtered = templates.filter(t => {
        const matchSearch = !search || t.name.toLowerCase().includes(search.toLowerCase()) || t.description.toLowerCase().includes(search.toLowerCase()) || t.tags.some(tag => tag.includes(search.toLowerCase()));
        const matchCategory = category === 'All' || t.category === category;
        return matchSearch && matchCategory;
    });

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Workflow Templates</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1">Pre-built automations to get started quickly</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="outline" onClick={() => router.push('/automation/workflows/new')} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> Build from Scratch
                    </Button>
                </div>
            </div>

            {/* Filters */}
            <div className="flex gap-3 flex-wrap">
                <div className="relative flex-1 min-w-48">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                    <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search templates..." className="pl-9" />
                </div>
                <div className="flex gap-1 flex-wrap">
                    {categories.map(cat => (
                        <button
                            key={cat}
                            onClick={() => setCategory(cat)}
                            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${category === cat ? 'bg-primary-600 text-white' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'}`}
                        >
                            {cat}
                        </button>
                    ))}
                </div>
            </div>

            {/* Templates Grid */}
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                    {[...Array(8)].map((_, i) => <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 animate-pulse h-52" />)}
                </div>
            ) : filtered.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                    <Layers className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-200">No templates found</h3>
                    <p className="text-slate-500 dark:text-slate-400 text-sm mt-1">Try a different search or category</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                    {filtered.map(template => (
                        <div key={template.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 hover:shadow-md transition-shadow flex flex-col">
                            {/* Top */}
                            <div className="flex items-start justify-between mb-3">
                                <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg text-primary-600 dark:text-primary-400">
                                    {TRIGGER_ICONS[template.triggerType] || <Zap className="h-4 w-4" />}
                                </div>
                                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${CATEGORY_COLORS[template.category] || 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400'}`}>
                                    {template.category}
                                </span>
                            </div>

                            {/* Content */}
                            <h3 className="font-semibold text-slate-900 dark:text-white mb-1">{template.name}</h3>
                            <p className="text-xs text-slate-500 dark:text-slate-400 mb-3 flex-1">{template.description}</p>

                            {/* Steps preview */}
                            <div className="mb-3 space-y-1">
                                {(template.previewSteps || []).slice(0, 3).map((step, i) => (
                                    <div key={i} className="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">
                                        <div className="h-4 w-4 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-[9px] font-bold text-foreground-secondary shrink-0">
                                            {i + 1}
                                        </div>
                                        <span className="truncate">{step}</span>
                                    </div>
                                ))}
                                {(template.previewSteps || []).length > 3 && (
                                    <div className="text-xs text-foreground-muted pl-5">+{template.previewSteps.length - 3} more steps</div>
                                )}
                            </div>

                            {/* Footer */}
                            <div className="flex items-center justify-between pt-3 border-t border-border-subtle">
                                <span className="text-xs text-foreground-muted">{template.usageCount?.toLocaleString() || 0} uses</span>
                                <Button
                                    size="sm"
                                    onClick={() => handleClone(template)}
                                    disabled={cloningId === template.id}
                                    className="text-xs"
                                >
                                    {cloningId === template.id ? (
                                        <Loader2 className="h-3 w-3 animate-spin" />
                                    ) : (
                                        <>Use Template <ChevronRight className="h-3 w-3 ml-1" /></>
                                    )}
                                </Button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
