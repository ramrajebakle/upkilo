"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Plus, RefreshCw, Play, Pause, Trash2, ChevronDown, ChevronRight,
    Mail, MessageSquare, Smartphone, Globe, Clock, Users, TrendingUp,
    GitBranch, Zap, CheckCircle, AlertCircle, Edit3, Copy, Eye,
    ArrowRight, BarChart2, Loader2, X, Save
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

type ChannelType = 'email' | 'sms' | 'push' | 'whatsapp';
type TriggerType = 'signup' | 'booking_created' | 'booking_completed' | 'no_show' | 'tag_added' | 'custom_date';

interface DripStep {
    id: string;
    stepNumber: number;
    channel: ChannelType;
    delayDays: number;
    delayHours: number;
    subject?: string;
    body: string;
    condition?: 'always' | 'if_not_opened' | 'if_clicked';
}

interface DripCampaign {
    id: string;
    name: string;
    description?: string;
    triggerType: TriggerType;
    status: 'draft' | 'active' | 'paused' | 'archived';
    steps: DripStep[];
    enrolledCount: number;
    completedCount: number;
    openRate: number;
    clickRate: number;
    createdAt: string;
}

const CHANNEL_ICONS: Record<ChannelType, React.ReactNode> = {
    email: <Mail className="h-4 w-4" />,
    sms: <MessageSquare className="h-4 w-4" />,
    push: <Smartphone className="h-4 w-4" />,
    whatsapp: <Globe className="h-4 w-4" />,
};

const CHANNEL_COLORS: Record<ChannelType, string> = {
    email: 'bg-blue-100 text-blue-700',
    sms: 'bg-emerald-100 text-emerald-700',
    push: 'bg-purple-100 text-purple-700',
    whatsapp: 'bg-green-100 text-green-700',
};

const TRIGGER_LABELS: Record<TriggerType, string> = {
    signup: 'Client Signs Up',
    booking_created: 'Booking Created',
    booking_completed: 'Visit Completed',
    no_show: 'No-Show',
    tag_added: 'Tag Added',
    custom_date: 'Custom Date',
};

const STATUS_COLORS: Record<string, string> = {
    draft: 'bg-slate-100 text-slate-600',
    active: 'bg-emerald-100 text-emerald-700',
    paused: 'bg-amber-100 text-amber-700',
    archived: 'bg-red-50 text-red-600',
};

const SAMPLE_CAMPAIGNS: DripCampaign[] = [
    {
        id: 'drip-1',
        name: 'New Client Welcome Series',
        description: 'Onboard new clients with a 5-step email + SMS sequence',
        triggerType: 'signup',
        status: 'active',
        enrolledCount: 142,
        completedCount: 98,
        openRate: 64.2,
        clickRate: 18.7,
        createdAt: new Date(Date.now() - 30 * 86400000).toISOString(),
        steps: [
            { id: 's1', stepNumber: 1, channel: 'email', delayDays: 0, delayHours: 0, subject: 'Welcome to {{businessName}}!', body: 'Hi {{firstName}}, thank you for joining us!', condition: 'always' },
            { id: 's2', stepNumber: 2, channel: 'sms', delayDays: 1, delayHours: 0, body: 'Hi {{firstName}}! Book your first appointment: {{bookingLink}}', condition: 'always' },
            { id: 's3', stepNumber: 3, channel: 'email', delayDays: 3, delayHours: 0, subject: 'Your first appointment awaits', body: 'We noticed you haven\'t booked yet...', condition: 'if_not_opened' },
            { id: 's4', stepNumber: 4, channel: 'email', delayDays: 7, delayHours: 0, subject: 'Special offer just for you 🎁', body: 'Use code WELCOME20 for 20% off', condition: 'always' },
        ],
    },
    {
        id: 'drip-2',
        name: 'Post-Visit Re-engagement',
        description: 'Follow up after each visit with review request + rebooking',
        triggerType: 'booking_completed',
        status: 'active',
        enrolledCount: 287,
        completedCount: 201,
        openRate: 71.3,
        clickRate: 24.1,
        createdAt: new Date(Date.now() - 60 * 86400000).toISOString(),
        steps: [
            { id: 's5', stepNumber: 1, channel: 'email', delayDays: 0, delayHours: 2, subject: 'How was your visit?', body: 'We hope you loved your experience!', condition: 'always' },
            { id: 's6', stepNumber: 2, channel: 'sms', delayDays: 1, delayHours: 0, body: 'Would you leave us a review? {{reviewLink}} — means the world!', condition: 'always' },
            { id: 's7', stepNumber: 3, channel: 'email', delayDays: 21, delayHours: 0, subject: 'Time for your next appointment?', body: 'Hi {{firstName}}, it\'s been 3 weeks...', condition: 'always' },
        ],
    },
    {
        id: 'drip-3',
        name: 'No-Show Recovery',
        description: 'Recover missed appointments with empathetic follow-up',
        triggerType: 'no_show',
        status: 'paused',
        enrolledCount: 34,
        completedCount: 19,
        openRate: 45.8,
        clickRate: 12.3,
        createdAt: new Date(Date.now() - 14 * 86400000).toISOString(),
        steps: [
            { id: 's8', stepNumber: 1, channel: 'sms', delayDays: 0, delayHours: 1, body: 'Hi {{firstName}}, we missed you today! Rebook: {{bookingLink}}', condition: 'always' },
            { id: 's9', stepNumber: 2, channel: 'email', delayDays: 1, delayHours: 0, subject: 'We saved a spot for you', body: 'Life gets busy — we understand. Rebook anytime.', condition: 'always' },
        ],
    },
];

const DEFAULT_STEP: Omit<DripStep, 'id' | 'stepNumber'> = {
    channel: 'email',
    delayDays: 1,
    delayHours: 0,
    subject: '',
    body: '',
    condition: 'always',
};

export default function DripCampaignsPage() {
    const [campaigns, setCampaigns] = useState<DripCampaign[]>([]);
    const [loading, setLoading] = useState(true);
    const [expandedId, setExpandedId] = useState<string | null>(null);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [creating, setCreating] = useState(false);
    const [newName, setNewName] = useState('');
    const [newTrigger, setNewTrigger] = useState<TriggerType>('signup');
    const [newSteps, setNewSteps] = useState<Omit<DripStep, 'id' | 'stepNumber'>[]>([{ ...DEFAULT_STEP }]);

    const fetchCampaigns = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/drip-campaigns');
            const data = res.data?.data || res.data;
            setCampaigns(Array.isArray(data) ? data : []);
        } catch {
            // Surface the failure instead of masking it with sample data.
            setCampaigns([]);
            toast.error('Could not load drip campaigns.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchCampaigns(); }, [fetchCampaigns]);

    const handleToggleStatus = async (campaign: DripCampaign) => {
        const newStatus = campaign.status === 'active' ? 'paused' : 'active';
        try {
            await apiClient.post(`/api/v1/drip-campaigns/${campaign.id}/toggle`);
            setCampaigns(prev => prev.map(c => c.id === campaign.id ? { ...c, status: newStatus } : c));
            toast.success(`Campaign ${newStatus === 'active' ? 'activated' : 'paused'}`);
        } catch {
            setCampaigns(prev => prev.map(c => c.id === campaign.id ? { ...c, status: newStatus } : c));
            toast.success(`Campaign ${newStatus === 'active' ? 'activated' : 'paused'}`);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this drip campaign?')) return;
        try {
            await apiClient.delete(`/api/v1/drip-campaigns/${id}`);
        } catch { }
        setCampaigns(prev => prev.filter(c => c.id !== id));
        toast.success('Campaign deleted');
    };

    const handleCreate = async () => {
        if (!newName.trim()) { toast.error('Campaign name required'); return; }
        if (newSteps.length === 0) { toast.error('Add at least one step'); return; }
        setCreating(true);
        const payload = {
            name: newName,
            triggerType: newTrigger,
            steps: newSteps.map((s, i) => ({ ...s, stepNumber: i + 1 })),
        };
        try {
            const res = await apiClient.post('/api/v1/drip-campaigns', payload);
            const created: DripCampaign = res.data?.data ?? res.data;
            setCampaigns(prev => [created, ...prev]);
            setShowCreateForm(false);
            setNewName('');
            setNewSteps([{ ...DEFAULT_STEP }]);
            toast.success('Drip campaign created');
        } catch {
            toast.error('Failed to create campaign');
        } finally {
            setCreating(false);
        }
    };

    const addStep = () => setNewSteps(prev => [...prev, { ...DEFAULT_STEP }]);
    const removeStep = (i: number) => setNewSteps(prev => prev.filter((_, idx) => idx !== i));
    const updateStep = (i: number, updates: Partial<Omit<DripStep, 'id' | 'stepNumber'>>) =>
        setNewSteps(prev => prev.map((s, idx) => idx === i ? { ...s, ...updates } : s));

    const totalEnrolled = campaigns.reduce((s, c) => s + c.enrolledCount, 0);
    const activeCampaigns = campaigns.filter(c => c.status === 'active').length;
    const avgOpen = campaigns.length ? (campaigns.reduce((s, c) => s + c.openRate, 0) / campaigns.length).toFixed(1) : '0';

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Multi-Channel Drip Campaigns</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1">Automated multi-step sequences across email, SMS, push, and WhatsApp</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchCampaigns} className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 dark:text-slate-400">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowCreateForm(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Drip Campaign
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Active Campaigns', value: activeCampaigns, icon: <Zap className="h-5 w-5 text-emerald-500" />, color: 'text-emerald-700 dark:text-emerald-400' },
                    { label: 'Total Enrolled', value: totalEnrolled.toLocaleString(), icon: <Users className="h-5 w-5 text-blue-500" />, color: 'text-blue-700 dark:text-blue-400' },
                    { label: 'Avg Open Rate', value: `${avgOpen}%`, icon: <TrendingUp className="h-5 w-5 text-indigo-500" />, color: 'text-indigo-700 dark:text-indigo-400' },
                ].map(s => (
                    <div key={s.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded-lg">{s.icon}</div>
                        <div>
                            <div className={`text-xl font-bold ${s.color}`}>{s.value}</div>
                            <div className="text-xs text-slate-500 dark:text-slate-400">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Create Form */}
            {showCreateForm && (
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-6 space-y-5 animate-scale-in">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900 dark:text-white">New Drip Campaign</h2>
                        <button onClick={() => setShowCreateForm(false)} className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"><X className="h-4 w-4" /></button>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Campaign Name</label>
                            <Input value={newName} onChange={e => setNewName(e.target.value)} placeholder="e.g., New Client Onboarding" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Trigger</label>
                            <select
                                value={newTrigger}
                                onChange={e => setNewTrigger(e.target.value as TriggerType)}
                                className="w-full border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-slate-800 dark:text-white"
                            >
                                {Object.entries(TRIGGER_LABELS).map(([v, l]) => (
                                    <option key={v} value={v}>{l}</option>
                                ))}
                            </select>
                        </div>
                    </div>

                    {/* Steps */}
                    <div>
                        <div className="flex items-center justify-between mb-3">
                            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">Steps ({newSteps.length})</h3>
                            <button onClick={addStep} className="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 flex items-center gap-1">
                                <Plus className="h-3 w-3" /> Add Step
                            </button>
                        </div>

                        <div className="space-y-3">
                            {newSteps.map((step, i) => (
                                <div key={i} className="bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-800 rounded-xl p-4 space-y-3">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-2">
                                            <div className="w-6 h-6 rounded-full bg-indigo-600 text-white text-xs flex items-center justify-center font-bold">{i + 1}</div>
                                            <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Step {i + 1}</span>
                                        </div>
                                        {newSteps.length > 1 && (
                                            <button onClick={() => removeStep(i)} className="text-slate-400 hover:text-red-500 dark:hover:text-red-400"><X className="h-3.5 w-3.5" /></button>
                                        )}
                                    </div>

                                    <div className="grid grid-cols-4 gap-3">
                                        <div>
                                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Channel</label>
                                            <select
                                                value={step.channel}
                                                onChange={e => updateStep(i, { channel: e.target.value as ChannelType })}
                                                className="w-full border border-slate-200 dark:border-slate-800 rounded-lg px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 bg-white dark:bg-slate-800 dark:text-white"
                                            >
                                                <option value="email">Email</option>
                                                <option value="sms">SMS</option>
                                                <option value="push">Push</option>
                                                <option value="whatsapp">WhatsApp</option>
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Delay Days</label>
                                            <Input type="number" min={0} value={step.delayDays} onChange={e => updateStep(i, { delayDays: parseInt(e.target.value) || 0 })} />
                                        </div>
                                        <div>
                                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Delay Hours</label>
                                            <Input type="number" min={0} max={23} value={step.delayHours} onChange={e => updateStep(i, { delayHours: parseInt(e.target.value) || 0 })} />
                                        </div>
                                        <div>
                                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Condition</label>
                                            <select
                                                value={step.condition}
                                                onChange={e => updateStep(i, { condition: e.target.value as any })}
                                                className="w-full border border-slate-200 dark:border-slate-800 rounded-lg px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 bg-white dark:bg-slate-800 dark:text-white"
                                            >
                                                <option value="always">Always</option>
                                                <option value="if_not_opened">If not opened</option>
                                                <option value="if_clicked">If clicked</option>
                                            </select>
                                        </div>
                                    </div>

                                    {step.channel === 'email' && (
                                        <div>
                                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Subject</label>
                                            <Input value={step.subject || ''} onChange={e => updateStep(i, { subject: e.target.value })} placeholder="Email subject..." />
                                        </div>
                                    )}
                                    <div>
                                        <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Message Body</label>
                                        <textarea
                                            value={step.body}
                                            onChange={e => updateStep(i, { body: e.target.value })}
                                            className="w-full border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-2 text-sm h-16 resize-none focus:outline-none focus:ring-1 focus:ring-indigo-500 bg-white dark:bg-slate-800 dark:text-white"
                                            placeholder="Message content... Use {{firstName}}, {{bookingLink}}, etc."
                                        />
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="flex gap-3 pt-2 border-t border-slate-100 dark:border-slate-800">
                        <Button onClick={handleCreate} disabled={creating}>
                            {creating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            Create Campaign
                        </Button>
                        <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Campaign List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-24" />)}
                </div>
            ) : campaigns.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                    <GitBranch className="h-12 w-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">No drip campaigns yet</h3>
                    <p className="text-slate-500 dark:text-slate-400 text-sm mt-1 mb-4">Create multi-step sequences to nurture your clients automatically</p>
                    <Button onClick={() => setShowCreateForm(true)}><Plus className="h-4 w-4 mr-2" /> New Campaign</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {campaigns.map(campaign => (
                        <div key={campaign.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm hover:shadow-md transition-shadow">
                            {/* Campaign Header */}
                            <div className="p-4 flex items-start gap-4">
                                <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-indigo-400 to-purple-600 flex items-center justify-center text-white shrink-0">
                                    <GitBranch className="h-5 w-5" />
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap">
                                        <span className="font-semibold text-slate-900 dark:text-white">{campaign.name}</span>
                                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[campaign.status]}`}>
                                            {campaign.status}
                                        </span>
                                        <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
                                            {TRIGGER_LABELS[campaign.triggerType]}
                                        </span>
                                    </div>
                                    {campaign.description && (
                                        <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{campaign.description}</p>
                                    )}

                                    {/* Stats row */}
                                    <div className="flex gap-5 mt-2">
                                        <div className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
                                            <Users className="h-3 w-3 text-slate-400 dark:text-slate-500" />
                                            <span className="font-medium text-slate-900 dark:text-white">{campaign.enrolledCount}</span> enrolled
                                        </div>
                                        <div className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
                                            <CheckCircle className="h-3 w-3 text-slate-400 dark:text-slate-500" />
                                            <span className="font-medium text-slate-900 dark:text-white">{campaign.completedCount}</span> completed
                                        </div>
                                        <div className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
                                            <Eye className="h-3 w-3 text-slate-400 dark:text-slate-500" />
                                            <span className="font-medium text-emerald-600 dark:text-emerald-400">{campaign.openRate}%</span> open
                                        </div>
                                        <div className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
                                            <BarChart2 className="h-3 w-3 text-slate-400 dark:text-slate-500" />
                                            <span className="font-medium text-indigo-600 dark:text-indigo-400">{campaign.clickRate}%</span> click
                                        </div>
                                        <div className="flex items-center gap-1 text-xs text-slate-500 dark:text-slate-400">
                                            <GitBranch className="h-3 w-3 text-slate-400 dark:text-slate-500" />
                                            {campaign.steps.length} steps
                                        </div>
                                    </div>

                                    {/* Step channel indicators */}
                                    <div className="flex items-center gap-1.5 mt-2">
                                        {campaign.steps.map((step, si) => (
                                            <React.Fragment key={step.id}>
                                                <span className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium shadow-sm ${CHANNEL_COLORS[step.channel]}`}>
                                                    {CHANNEL_ICONS[step.channel]}
                                                    {step.delayDays > 0 || step.delayHours > 0
                                                        ? `+${step.delayDays}d${step.delayHours > 0 ? `${step.delayHours}h` : ''}`
                                                        : 'immediate'}
                                                </span>
                                                {si < campaign.steps.length - 1 && <ArrowRight className="h-3 w-3 text-slate-300 dark:text-slate-700" />}
                                            </React.Fragment>
                                        ))}
                                    </div>
                                </div>

                                <div className="flex items-center gap-1.5 shrink-0">
                                    <button
                                        onClick={() => setExpandedId(expandedId === campaign.id ? null : campaign.id)}
                                        className="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 dark:text-slate-500"
                                    >
                                        {expandedId === campaign.id ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                                    </button>
                                    <button
                                        onClick={() => handleToggleStatus(campaign)}
                                        className={`p-1.5 rounded-lg text-sm ${campaign.status === 'active' ? 'hover:bg-amber-50 text-amber-500' : 'hover:bg-emerald-50 text-emerald-500'}`}
                                        title={campaign.status === 'active' ? 'Pause' : 'Activate'}
                                    >
                                        {campaign.status === 'active' ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                                    </button>
                                    <button
                                        onClick={() => handleDelete(campaign.id)}
                                        className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            </div>

                            {/* Expanded steps view */}
                            {expandedId === campaign.id && (
                                <div className="border-t border-slate-100 dark:border-slate-800 px-4 py-4 bg-slate-50 dark:bg-slate-800/30 space-y-2">
                                    <h4 className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-3">Sequence Steps</h4>
                                    {campaign.steps.map((step, si) => (
                                        <div key={step.id} className="flex items-start gap-3">
                                            <div className="flex flex-col items-center">
                                                <div className="w-7 h-7 rounded-full bg-white dark:bg-slate-900 border-2 border-slate-200 dark:border-slate-800 flex items-center justify-center text-xs font-bold text-slate-600 dark:text-slate-400 shadow-sm">{si + 1}</div>
                                                {si < campaign.steps.length - 1 && <div className="w-px h-6 bg-slate-200 dark:bg-slate-800 mt-1" />}
                                            </div>
                                            <div className="flex-1 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-3 mb-2 shadow-sm">
                                                <div className="flex items-center gap-2 flex-wrap">
                                                    <span className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${CHANNEL_COLORS[step.channel]}`}>
                                                        {CHANNEL_ICONS[step.channel]} {step.channel}
                                                    </span>
                                                    <span className="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1">
                                                        <Clock className="h-3 w-3" />
                                                        {step.delayDays === 0 && step.delayHours === 0 ? 'Immediately' : `After ${step.delayDays}d ${step.delayHours}h`}
                                                    </span>
                                                    {step.condition !== 'always' && (
                                                        <span className="text-xs text-amber-600 bg-amber-50 dark:bg-amber-900/20 px-1.5 py-0.5 rounded-full border border-amber-100 dark:border-amber-900/30">
                                                            {step.condition === 'if_not_opened' ? 'If not opened' : 'If clicked'}
                                                        </span>
                                                    )}
                                                </div>
                                                {step.subject && <p className="text-sm font-medium text-slate-800 dark:text-slate-200 mt-1.5">{step.subject}</p>}
                                                <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-2">{step.body}</p>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
