'use client';

import { useState, useEffect, useCallback } from 'react';
import {
    Megaphone,
    Send,
    Clock,
    Users,
    Mail,
    MessageSquare,
    TrendingUp,
    Eye,
    MousePointer,
    UserMinus,
    Play,
    Calendar,
    ChevronDown,
    Plus,
    X,
    Check,
} from 'lucide-react';
import { apiClient as api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

type CampaignChannel = 'email' | 'sms';
type CampaignStatus  = 'draft' | 'scheduled' | 'sending' | 'sent' | 'cancelled';
type TargetSegment   = 'all' | 'active' | 'inactive' | 'vip';
type TabKey          = 'all' | 'email' | 'sms' | 'scheduled' | 'sent';
type SendMode        = 'now' | 'schedule';

interface BroadcastCampaign {
    id:              string;
    name:            string;
    channel:         CampaignChannel;
    subject:         string | null;
    body:            string | null;
    status:          CampaignStatus;
    targetSegment:   TargetSegment;
    scheduledAt:     string | null;
    sentAt:          string | null;
    totalRecipients: number;
    delivered:       number;
    opened:          number;
    clicked:         number;
    unsubscribed:    number;
    createdAt:       string;
}

interface BroadcastStats {
    campaignCount:     number;
    totalSent:         number;
    openRate:          number;
    clickRate:         number;
    totalUnsubscribed: number;
}

interface CampaignFormState {
    name:           string;
    channel:        CampaignChannel;
    subject:        string;
    body:           string;
    targetSegment:  TargetSegment;
    sendMode:       SendMode;
    scheduledAt:    string;
}

const INITIAL_FORM: CampaignFormState = {
    name:          '',
    channel:       'email',
    subject:       '',
    body:          '',
    targetSegment: 'all',
    sendMode:      'now',
    scheduledAt:   '',
};

const SEGMENT_LABELS: Record<TargetSegment, string> = {
    all:      'All clients',
    active:   'Active clients (booked in 90 days)',
    inactive: 'Inactive clients',
    vip:      'VIP clients (top 20%)',
};

const TABS: { key: TabKey; label: string }[] = [
    { key: 'all',       label: 'All'       },
    { key: 'email',     label: 'Email'     },
    { key: 'sms',       label: 'SMS'       },
    { key: 'scheduled', label: 'Scheduled' },
    { key: 'sent',      label: 'Sent'      },
];

// ─── Helpers ──────────────────────────────────────────────────────────────────

function pct(part: number, total: number): string {
    if (!total) return '0%';
    return `${Math.round((part / total) * 100)}%`;
}

function fmtDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString(undefined, {
        dateStyle: 'medium',
        timeStyle: 'short',
    });
}

// ─── Sub-components ──────────────────────────────────────────────────────────

function ChannelBadge({ channel }: { channel: CampaignChannel }) {
    return (
        <span
            className={cn(
                'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                channel === 'email'
                    ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300'
                    : 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
            )}
        >
            {channel === 'email'
                ? <Mail className="h-3 w-3" />
                : <MessageSquare className="h-3 w-3" />}
            {channel.toUpperCase()}
        </span>
    );
}

function StatusBadge({ status }: { status: CampaignStatus }) {
    const map: Record<CampaignStatus, string> = {
        draft:     'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300',
        scheduled: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300',
        sending:   'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
        sent:      'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
        cancelled: 'bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-300',
    };
    return (
        <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium capitalize', map[status])}>
            {status}
        </span>
    );
}

function StatCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string | number }) {
    return (
        <div className="flex items-center gap-3 rounded-xl border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary-50 text-primary-600 dark:bg-primary-900/30 dark:text-primary-400">
                {icon}
            </div>
            <div>
                <p className="text-xs text-gray-500 dark:text-gray-400">{label}</p>
                <p className="text-lg font-semibold text-gray-900 dark:text-gray-100">{value}</p>
            </div>
        </div>
    );
}

function MetricBadge({ icon, value, label }: { icon: React.ReactNode; value: string; label: string }) {
    return (
        <div className="flex items-center gap-1.5 rounded-lg bg-gray-50 px-3 py-1.5 dark:bg-gray-700/50">
            <span className="text-foreground-muted">{icon}</span>
            <span className="text-sm font-medium text-gray-700 dark:text-gray-200">{value}</span>
            <span className="text-xs text-foreground-muted">{label}</span>
        </div>
    );
}

// ─── Campaign card ─────────────────────────────────────────────────────────────

interface CampaignCardProps {
    campaign:  BroadcastCampaign;
    onSend:    (id: string) => void;
    onCancel:  (id: string) => void;
    sending:   boolean;
    cancelling:boolean;
}

function CampaignCard({ campaign, onSend, onCancel, sending, cancelling }: CampaignCardProps) {
    const [expanded, setExpanded] = useState(false);

    const deliveredPct  = pct(campaign.delivered,    campaign.totalRecipients);
    const openedPct     = pct(campaign.opened,       campaign.delivered || campaign.totalRecipients);
    const clickedPct    = pct(campaign.clicked,      campaign.delivered || campaign.totalRecipients);
    const unsubPct      = pct(campaign.unsubscribed, campaign.delivered || campaign.totalRecipients);

    return (
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm transition-shadow hover:shadow-md dark:border-gray-700 dark:bg-gray-800">
            {/* Header row */}
            <div className="flex flex-wrap items-center gap-3 p-4">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-50 text-primary-600 dark:bg-primary-900/30 dark:text-primary-400">
                    <Megaphone className="h-5 w-5" />
                </div>

                <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                        <h3 className="truncate font-semibold text-gray-900 dark:text-gray-100">
                            {campaign.name}
                        </h3>
                        <ChannelBadge channel={campaign.channel} />
                        <StatusBadge  status={campaign.status}   />
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
                        <span className="flex items-center gap-1">
                            <Users className="h-3.5 w-3.5" />
                            {campaign.totalRecipients > 0
                                ? `${campaign.totalRecipients.toLocaleString()} recipients`
                                : SEGMENT_LABELS[campaign.targetSegment]}
                        </span>
                        {campaign.scheduledAt && campaign.status === 'scheduled' && (
                            <span className="flex items-center gap-1">
                                <Clock className="h-3.5 w-3.5" />
                                Scheduled {fmtDate(campaign.scheduledAt)}
                            </span>
                        )}
                        {campaign.sentAt && (
                            <span className="flex items-center gap-1">
                                <Check className="h-3.5 w-3.5 text-success-fg" />
                                Sent {fmtDate(campaign.sentAt)}
                            </span>
                        )}
                    </div>
                </div>

                {/* Inline metrics (sent campaigns) */}
                {campaign.status === 'sent' && campaign.delivered > 0 && (
                    <div className="hidden flex-wrap gap-2 sm:flex">
                        <MetricBadge icon={<Eye          className="h-3.5 w-3.5" />} value={openedPct}  label="open"  />
                        <MetricBadge icon={<MousePointer className="h-3.5 w-3.5" />} value={clickedPct} label="CTR"   />
                    </div>
                )}

                {/* Actions */}
                <div className="flex items-center gap-2">
                    {(campaign.status === 'draft') && (
                        <button
                            onClick={() => onSend(campaign.id)}
                            disabled={sending}
                            className="inline-flex items-center gap-1.5 rounded-lg bg-primary-600 px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
                        >
                            <Play className="h-3.5 w-3.5" />
                            {sending ? 'Sending…' : 'Send Now'}
                        </button>
                    )}
                    {campaign.status === 'scheduled' && (
                        <button
                            onClick={() => onCancel(campaign.id)}
                            disabled={cancelling}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 disabled:opacity-50 dark:border-red-600 dark:text-red-400 dark:hover:bg-red-900/20"
                        >
                            <X className="h-3.5 w-3.5" />
                            Cancel
                        </button>
                    )}
                    {campaign.status === 'sent' && (
                        <button
                            onClick={() => setExpanded(v => !v)}
                            className="inline-flex items-center gap-1 rounded-lg border border-gray-200 px-3 py-1.5 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
                        >
                            <TrendingUp className="h-3.5 w-3.5" />
                            Report
                            <ChevronDown className={cn('h-3.5 w-3.5 transition-transform', expanded && 'rotate-180')} />
                        </button>
                    )}
                </div>
            </div>

            {/* Expanded stats */}
            {expanded && campaign.status === 'sent' && (
                <div className="border-t border-gray-100 px-4 py-3 dark:border-gray-700">
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                        {[
                            { icon: <Send        className="h-4 w-4" />, label: 'Delivered',   value: deliveredPct,  count: campaign.delivered },
                            { icon: <Eye         className="h-4 w-4" />, label: 'Opened',      value: openedPct,     count: campaign.opened    },
                            { icon: <MousePointer className="h-4 w-4"/>, label: 'Clicked',     value: clickedPct,    count: campaign.clicked   },
                            { icon: <UserMinus   className="h-4 w-4" />, label: 'Unsubscribed',value: unsubPct,      count: campaign.unsubscribed },
                        ].map(({ icon, label, value, count }) => (
                            <div
                                key={label}
                                className="flex flex-col items-center rounded-lg bg-gray-50 p-3 text-center dark:bg-gray-700/50"
                            >
                                <span className="mb-1 text-foreground-muted">{icon}</span>
                                <span className="text-xl font-bold text-gray-900 dark:text-gray-100">{value}</span>
                                <span className="text-xs text-gray-500 dark:text-gray-400">{label}</span>
                                <span className="text-xs text-foreground-muted">({count.toLocaleString()})</span>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

// ─── Create / Edit modal ───────────────────────────────────────────────────────

interface CampaignModalProps {
    isOpen:   boolean;
    onClose:  () => void;
    onSaved:  (campaign: BroadcastCampaign) => void;
}

function CampaignModal({ isOpen, onClose, onSaved }: CampaignModalProps) {
    const { success, error: toastError } = useToast();
    const [form,    setForm]    = useState<CampaignFormState>(INITIAL_FORM);
    const [saving,  setSaving]  = useState(false);
    const [preview, setPreview] = useState(false);

    // Reset when modal opens
    useEffect(() => {
        if (isOpen) {
            setForm(INITIAL_FORM);
            setPreview(false);
        }
    }, [isOpen]);

    const charCount      = form.body.length;
    const smsMax         = 160;
    const smsOverLimit   = form.channel === 'sms' && charCount > smsMax;

    function patch(partial: Partial<CampaignFormState>) {
        setForm(prev => ({ ...prev, ...partial }));
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (saving) return;

        if (!form.name.trim()) {
            toastError('Campaign name is required.');
            return;
        }
        if (form.channel === 'email' && !form.subject.trim()) {
            toastError('Subject line is required for email campaigns.');
            return;
        }
        if (!form.body.trim()) {
            toastError('Message body is required.');
            return;
        }
        if (smsOverLimit) {
            toastError(`SMS body exceeds ${smsMax} characters.`);
            return;
        }
        if (form.sendMode === 'schedule' && !form.scheduledAt) {
            toastError('Please select a schedule date and time.');
            return;
        }

        setSaving(true);
        try {
            const payload = {
                name:          form.name.trim(),
                channel:       form.channel,
                subject:       form.channel === 'email' ? form.subject.trim() : undefined,
                body:          form.body.trim(),
                targetSegment: form.targetSegment,
                scheduledAt:   form.sendMode === 'schedule' && form.scheduledAt
                                   ? new Date(form.scheduledAt).toISOString()
                                   : undefined,
            };

            const res = await api.post<{ data: BroadcastCampaign }>('/api/v1/broadcast/campaigns', payload);
            const campaign = res.data?.data ?? (res.data as unknown as BroadcastCampaign);

            // If "send now" immediately trigger send
            if (form.sendMode === 'now') {
                await api.post(`/api/v1/broadcast/campaigns/${campaign.id}/send`, {});
                success('Campaign sent successfully!');
            } else {
                success('Campaign scheduled successfully!');
            }

            onSaved(campaign);
            onClose();
        } catch {
            toastError('Failed to create campaign. Please try again.');
        } finally {
            setSaving(false);
        }
    }

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
            <div className="relative max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl dark:bg-gray-900">
                {/* Modal header */}
                <div className="sticky top-0 z-10 flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-700 dark:bg-gray-900">
                    <div className="flex items-center gap-2.5">
                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary-100 text-primary-600 dark:bg-primary-900/40 dark:text-primary-400">
                            <Megaphone className="h-4 w-4" />
                        </div>
                        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                            New Broadcast Campaign
                        </h2>
                    </div>
                    <button
                        onClick={onClose}
                        className="rounded-lg p-1.5 text-foreground-muted transition-colors hover:bg-gray-100 hover:text-gray-600 dark:hover:bg-gray-800 dark:hover:text-gray-200"
                    >
                        <X className="h-5 w-5" />
                    </button>
                </div>

                <form onSubmit={handleSubmit} className="space-y-5 p-6">
                    {/* Campaign name */}
                    <div>
                        <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-200">
                            Campaign Name <span className="text-danger-fg">*</span>
                        </label>
                        <input
                            type="text"
                            value={form.name}
                            onChange={e => patch({ name: e.target.value })}
                            placeholder="e.g. Spring Promotion 2026"
                            className="w-full rounded-lg border border-gray-300 px-3.5 py-2.5 text-sm outline-none transition-colors focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100 dark:placeholder-gray-500"
                        />
                    </div>

                    {/* Channel selector */}
                    <div>
                        <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-200">
                            Channel
                        </label>
                        <div className="grid grid-cols-2 gap-3">
                            {(['email', 'sms'] as CampaignChannel[]).map(ch => (
                                <button
                                    key={ch}
                                    type="button"
                                    onClick={() => patch({ channel: ch })}
                                    className={cn(
                                        'flex items-center gap-2.5 rounded-xl border-2 p-3.5 text-sm font-medium transition-all',
                                        form.channel === ch
                                            ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                                            : 'border-gray-200 text-gray-600 hover:border-gray-300 dark:border-gray-700 dark:text-gray-300',
                                    )}
                                >
                                    {ch === 'email'
                                        ? <Mail        className="h-4 w-4" />
                                        : <MessageSquare className="h-4 w-4" />}
                                    {ch === 'email' ? 'Email' : 'SMS'}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Subject (email only) */}
                    {form.channel === 'email' && (
                        <div>
                            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-200">
                                Subject Line <span className="text-danger-fg">*</span>
                            </label>
                            <input
                                type="text"
                                value={form.subject}
                                onChange={e => patch({ subject: e.target.value })}
                                placeholder="e.g. Exclusive offer just for you 🎉"
                                className="w-full rounded-lg border border-gray-300 px-3.5 py-2.5 text-sm outline-none transition-colors focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100 dark:placeholder-gray-500"
                            />
                        </div>
                    )}

                    {/* Message body */}
                    <div>
                        <div className="mb-1.5 flex items-center justify-between">
                            <label className="text-sm font-medium text-gray-700 dark:text-gray-200">
                                Message Body <span className="text-danger-fg">*</span>
                            </label>
                            {form.channel === 'sms' && (
                                <span className={cn('text-xs', smsOverLimit ? 'text-danger-fg font-medium' : 'text-foreground-muted')}>
                                    {charCount}/{smsMax}
                                </span>
                            )}
                        </div>
                        <textarea
                            rows={5}
                            value={form.body}
                            onChange={e => patch({ body: e.target.value })}
                            placeholder={
                                form.channel === 'sms'
                                    ? 'Hi {name}, book your appointment this week and get 10% off…'
                                    : 'Write your email content here…'
                            }
                            className={cn(
                                'w-full resize-y rounded-lg border px-3.5 py-2.5 text-sm outline-none transition-colors focus:ring-2 dark:bg-gray-800 dark:text-gray-100 dark:placeholder-gray-500',
                                smsOverLimit
                                    ? 'border-red-400 focus:border-red-500 focus:ring-red-500/20'
                                    : 'border-gray-300 focus:border-primary-500 focus:ring-primary-500/20 dark:border-gray-600',
                            )}
                        />
                    </div>

                    {/* Target audience */}
                    <div>
                        <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-200">
                            Target Audience
                        </label>
                        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                            {(Object.entries(SEGMENT_LABELS) as [TargetSegment, string][]).map(([key, label]) => (
                                <button
                                    key={key}
                                    type="button"
                                    onClick={() => patch({ targetSegment: key })}
                                    className={cn(
                                        'flex items-center gap-2 rounded-lg border px-3.5 py-2.5 text-left text-sm transition-all',
                                        form.targetSegment === key
                                            ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                                            : 'border-gray-200 text-gray-600 hover:border-gray-300 dark:border-gray-700 dark:text-gray-300',
                                    )}
                                >
                                    <Users className="h-3.5 w-3.5 shrink-0" />
                                    {label}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Send mode */}
                    <div>
                        <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-200">
                            Delivery
                        </label>
                        <div className="grid grid-cols-2 gap-3">
                            {([
                                { mode: 'now'      as SendMode, icon: <Send     className="h-4 w-4" />, label: 'Send Now'  },
                                { mode: 'schedule' as SendMode, icon: <Calendar className="h-4 w-4" />, label: 'Schedule'  },
                            ]).map(({ mode, icon, label }) => (
                                <button
                                    key={mode}
                                    type="button"
                                    onClick={() => patch({ sendMode: mode })}
                                    className={cn(
                                        'flex items-center gap-2.5 rounded-xl border-2 p-3.5 text-sm font-medium transition-all',
                                        form.sendMode === mode
                                            ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                                            : 'border-gray-200 text-gray-600 hover:border-gray-300 dark:border-gray-700 dark:text-gray-300',
                                    )}
                                >
                                    {icon}
                                    {label}
                                </button>
                            ))}
                        </div>
                        {form.sendMode === 'schedule' && (
                            <div className="mt-3">
                                <input
                                    type="datetime-local"
                                    value={form.scheduledAt}
                                    min={new Date().toISOString().slice(0, 16)}
                                    onChange={e => patch({ scheduledAt: e.target.value })}
                                    className="w-full rounded-lg border border-gray-300 px-3.5 py-2.5 text-sm outline-none transition-colors focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
                                />
                            </div>
                        )}
                    </div>

                    {/* Preview toggle */}
                    {form.body.trim() && (
                        <div>
                            <button
                                type="button"
                                onClick={() => setPreview(v => !v)}
                                className="mb-2 flex items-center gap-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400"
                            >
                                <Eye className="h-4 w-4" />
                                {preview ? 'Hide Preview' : 'Show Preview'}
                            </button>
                            {preview && (
                                <div className="rounded-xl border border-dashed border-primary-200 bg-primary-50/50 p-4 dark:border-primary-700/40 dark:bg-primary-900/10">
                                    <p className="mb-1 text-xs font-medium uppercase tracking-wide text-primary-400">
                                        {form.channel === 'email' ? 'Email Preview' : 'SMS Preview'}
                                    </p>
                                    {form.channel === 'email' && form.subject && (
                                        <p className="mb-2 font-semibold text-gray-800 dark:text-gray-200">
                                            {form.subject}
                                        </p>
                                    )}
                                    <p className="whitespace-pre-wrap text-sm text-gray-700 dark:text-gray-300">
                                        {form.body}
                                    </p>
                                </div>
                            )}
                        </div>
                    )}

                    {/* Footer actions */}
                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            type="button"
                            onClick={onClose}
                            className="rounded-lg border border-gray-300 px-5 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-800"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={saving || smsOverLimit}
                            className="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
                        >
                            {saving ? (
                                <>
                                    <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                    </svg>
                                    {form.sendMode === 'now' ? 'Sending…' : 'Scheduling…'}
                                </>
                            ) : (
                                <>
                                    {form.sendMode === 'now'
                                        ? <><Send     className="h-4 w-4" /> Send Now</>
                                        : <><Calendar className="h-4 w-4" /> Schedule</>}
                                </>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

// ─── Main page ─────────────────────────────────────────────────────────────────

export default function BroadcastCampaignPage() {
    const { success, error: toastError } = useToast();

    const [campaigns,   setCampaigns]   = useState<BroadcastCampaign[]>([]);
    const [stats,       setStats]       = useState<BroadcastStats | null>(null);
    const [activeTab,   setActiveTab]   = useState<TabKey>('all');
    const [loading,     setLoading]     = useState(true);
    const [modalOpen,   setModalOpen]   = useState(false);
    const [sendingId,   setSendingId]   = useState<string | null>(null);
    const [cancellingId,setCancellingId]= useState<string | null>(null);

    // ── Fetch ────────────────────────────────────────────────────────────────
    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [campaignsRes, statsRes] = await Promise.all([
                api.get<{ data: { data: BroadcastCampaign[] } }>('/api/v1/broadcast/campaigns'),
                api.get<{ data: BroadcastStats }>('/api/v1/broadcast/stats'),
            ]);

            const rawCampaigns = campaignsRes.data?.data?.data ?? [];
            const rawStats     = statsRes.data?.data ?? null;

            setCampaigns(rawCampaigns);
            setStats(rawStats);
        } catch {
            toastError('Failed to load broadcast campaigns.');
        } finally {
            setLoading(false);
        }
    }, [toastError]);

    useEffect(() => { fetchData(); }, [fetchData]);

    // ── Send now ────────────────────────────────────────────────────────────
    async function handleSend(id: string) {
        setSendingId(id);
        try {
            await api.post(`/api/v1/broadcast/campaigns/${id}/send`, {});
            success('Campaign sent successfully!');
            await fetchData();
        } catch {
            toastError('Failed to send campaign.');
        } finally {
            setSendingId(null);
        }
    }

    // ── Cancel ──────────────────────────────────────────────────────────────
    async function handleCancel(id: string) {
        setCancellingId(id);
        try {
            await api.post(`/api/v1/broadcast/campaigns/${id}/cancel`, {});
            success('Campaign cancelled.');
            await fetchData();
        } catch {
            toastError('Failed to cancel campaign.');
        } finally {
            setCancellingId(null);
        }
    }

    // ── After modal save ────────────────────────────────────────────────────
    function handleSaved(_campaign: BroadcastCampaign) {
        fetchData();
    }

    // ── Tab filtering ────────────────────────────────────────────────────────
    const filtered = campaigns.filter(c => {
        if (activeTab === 'all')       return true;
        if (activeTab === 'email')     return c.channel === 'email';
        if (activeTab === 'sms')       return c.channel === 'sms';
        if (activeTab === 'scheduled') return c.status  === 'scheduled';
        if (activeTab === 'sent')      return c.status  === 'sent';
        return true;
    });

    // ── Skeleton rows ─────────────────────────────────────────────────────────
    const Skeleton = () => (
        <div className="animate-pulse rounded-xl border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
            <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-lg bg-gray-200 dark:bg-gray-700" />
                <div className="flex-1 space-y-2">
                    <div className="h-4 w-48 rounded bg-gray-200 dark:bg-gray-700" />
                    <div className="h-3 w-32 rounded bg-gray-200 dark:bg-gray-700" />
                </div>
            </div>
        </div>
    );

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
            <div className="mx-auto max-w-6xl space-y-6 px-4 py-8 sm:px-6">

                {/* ── Page header ─────────────────────────────────────────────── */}
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-600 text-white shadow-sm">
                            <Megaphone className="h-5 w-5" />
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                                Broadcast Campaigns
                            </h1>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                                Send mass email and SMS messages to your clients
                            </p>
                        </div>
                    </div>
                    <button
                        onClick={() => setModalOpen(true)}
                        className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700"
                    >
                        <Plus className="h-4 w-4" />
                        New Broadcast
                    </button>
                </div>

                {/* ── Stats bar ────────────────────────────────────────────────── */}
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                    <StatCard
                        icon={<Send         className="h-4 w-4" />}
                        label="Total Sent"
                        value={stats ? stats.totalSent.toLocaleString() : '—'}
                    />
                    <StatCard
                        icon={<Eye          className="h-4 w-4" />}
                        label="Avg Open Rate"
                        value={stats ? `${stats.openRate}%` : '—'}
                    />
                    <StatCard
                        icon={<MousePointer className="h-4 w-4" />}
                        label="Avg CTR"
                        value={stats ? `${stats.clickRate}%` : '—'}
                    />
                    <StatCard
                        icon={<TrendingUp   className="h-4 w-4" />}
                        label="Campaigns"
                        value={stats ? stats.campaignCount : campaigns.length}
                    />
                </div>

                {/* ── Tabs ─────────────────────────────────────────────────────── */}
                <div className="border-b border-gray-200 dark:border-gray-700">
                    <nav className="-mb-px flex gap-1 overflow-x-auto">
                        {TABS.map(tab => {
                            const count = tab.key === 'all'
                                ? campaigns.length
                                : campaigns.filter(c =>
                                    tab.key === 'email'     ? c.channel === 'email'   :
                                    tab.key === 'sms'       ? c.channel === 'sms'     :
                                    tab.key === 'scheduled' ? c.status  === 'scheduled' :
                                    tab.key === 'sent'      ? c.status  === 'sent'    : false
                                  ).length;

                            return (
                                <button
                                    key={tab.key}
                                    onClick={() => setActiveTab(tab.key)}
                                    className={cn(
                                        'flex shrink-0 items-center gap-1.5 border-b-2 px-4 py-3 text-sm font-medium transition-colors',
                                        activeTab === tab.key
                                            ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                                            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200',
                                    )}
                                >
                                    {tab.label}
                                    {count > 0 && (
                                        <span className={cn(
                                            'rounded-full px-1.5 py-0.5 text-xs',
                                            activeTab === tab.key
                                                ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300'
                                                : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400',
                                        )}>
                                            {count}
                                        </span>
                                    )}
                                </button>
                            );
                        })}
                    </nav>
                </div>

                {/* ── Campaign list ─────────────────────────────────────────────── */}
                <div className="space-y-3">
                    {loading ? (
                        Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} />)
                    ) : filtered.length === 0 ? (
                        <div className="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-gray-200 py-16 dark:border-gray-700">
                            <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-gray-100 text-foreground-muted dark:bg-gray-800">
                                <Megaphone className="h-6 w-6" />
                            </div>
                            <p className="font-medium text-gray-600 dark:text-gray-300">No campaigns yet</p>
                            <p className="mt-1 text-sm text-foreground-muted">
                                Click &ldquo;New Broadcast&rdquo; to create your first campaign.
                            </p>
                            <button
                                onClick={() => setModalOpen(true)}
                                className="mt-5 inline-flex items-center gap-1.5 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
                            >
                                <Plus className="h-4 w-4" />
                                New Broadcast
                            </button>
                        </div>
                    ) : (
                        filtered.map(campaign => (
                            <CampaignCard
                                key={campaign.id}
                                campaign={campaign}
                                onSend={handleSend}
                                onCancel={handleCancel}
                                sending={sendingId    === campaign.id}
                                cancelling={cancellingId === campaign.id}
                            />
                        ))
                    )}
                </div>
            </div>

            {/* ── Create modal ──────────────────────────────────────────────── */}
            <CampaignModal
                isOpen={modalOpen}
                onClose={() => setModalOpen(false)}
                onSaved={handleSaved}
            />
        </div>
    );
}
