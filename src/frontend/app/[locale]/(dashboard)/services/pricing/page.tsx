'use client';

import { useState, useEffect } from 'react';
import {
    TrendingUp,
    TrendingDown,
    Plus,
    Trash2,
    ToggleLeft,
    ToggleRight,
    Zap,
    Clock,
    Calendar,
    BarChart3,
    DollarSign,
    ChevronDown,
    ChevronUp,
    AlertCircle,
    CheckCircle2,
    Loader2,
    Tag,
    Percent,
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { apiClient as api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

// ─── Types ────────────────────────────────────────────────────────────────────

interface PricingRule {
    id: string;
    name: string;
    type: 'surge' | 'off_peak' | 'seasonal' | 'demand' | 'early_bird' | 'last_minute';
    adjustmentType: 'percentage' | 'fixed';
    adjustmentValue: number;
    isActive: boolean;
    applicableDays: string[];
    startTime?: string;
    endTime?: string;
    startDate?: string;
    endDate?: string;
    serviceIds: string[];
    minBookingsThreshold?: number;
    createdAt: string;
}

interface Service {
    id: string;
    name: string;
    price: number;
}

const RULE_TYPES = [
    { value: 'surge', label: 'Surge Pricing', icon: TrendingUp, color: 'text-danger-fg', desc: 'Increase price during high demand' },
    { value: 'off_peak', label: 'Off-Peak Discount', icon: TrendingDown, color: 'text-blue-500', desc: 'Reduce price during slow periods' },
    { value: 'seasonal', label: 'Seasonal', icon: Calendar, color: 'text-orange-500', desc: 'Apply within a date range' },
    { value: 'demand', label: 'Demand-Based', icon: BarChart3, color: 'text-purple-500', desc: 'Activate when bookings exceed threshold' },
    { value: 'early_bird', label: 'Early Bird', icon: Zap, color: 'text-success-fg', desc: 'Discount for advance bookings' },
    { value: 'last_minute', label: 'Last Minute', icon: Clock, color: 'text-warning-fg', desc: 'Deals close to booking time' },
];

const DAYS_OF_WEEK = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

const EMPTY_FORM = {
    name: '',
    type: 'surge' as PricingRule['type'],
    adjustmentType: 'percentage' as 'percentage' | 'fixed',
    adjustmentValue: 10,
    applicableDays: [] as string[],
    startTime: '',
    endTime: '',
    startDate: '',
    endDate: '',
    serviceIds: [] as string[],
    minBookingsThreshold: undefined as number | undefined,
};

// ─── Rule badge ───────────────────────────────────────────────────────────────

function RuleTypeBadge({ type }: { type: string }) {
    const rt = RULE_TYPES.find(r => r.value === type);
    if (!rt) return null;
    const Icon = rt.icon;
    return (
        <span className={cn('inline-flex items-center gap-1 text-xs font-medium', rt.color)}>
            <Icon className="w-3 h-3" />
            {rt.label}
        </span>
    );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function DynamicPricingPage() {
    const { success: toastSuccess, error: toastError } = useToast();

    const [rules, setRules] = useState<PricingRule[]>([]);
    const [services, setServices] = useState<Service[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [showForm, setShowForm] = useState(false);
    const [expandedRule, setExpandedRule] = useState<string | null>(null);
    const [form, setForm] = useState({ ...EMPTY_FORM });

    // Simulator state
    const [simServiceId, setSimServiceId] = useState('');
    const [simDate, setSimDate] = useState(new Date().toISOString().slice(0, 16));
    const [simDemand, setSimDemand] = useState(5);
    const [simResult, setSimResult] = useState<{ basePrice: number; effectivePrice: number; appliedRules: any[] } | null>(null);
    const [simLoading, setSimLoading] = useState(false);

    // ── Load ─────────────────────────────────────────────────────────────────
    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const [rulesRes, servicesRes] = await Promise.all([
                    api.get('/api/v1/dynamicpricing/rules'),
                    api.get('/api/v1/services?limit=100'),
                ]);
                setRules(rulesRes.data?.data?.rules ?? []);
                setServices(servicesRes.data?.data ?? []);
            } catch {
                // keep empty
            } finally {
                setLoading(false);
            }
        };
        load();
    }, []);

    // ── Helpers ──────────────────────────────────────────────────────────────
    const toggleDay = (day: string) => {
        setForm(f => ({
            ...f,
            applicableDays: f.applicableDays.includes(day)
                ? f.applicableDays.filter(d => d !== day)
                : [...f.applicableDays, day],
        }));
    };

    const toggleServiceId = (id: string) => {
        setForm(f => ({
            ...f,
            serviceIds: f.serviceIds.includes(id)
                ? f.serviceIds.filter(s => s !== id)
                : [...f.serviceIds, id],
        }));
    };

    // ── CRUD ─────────────────────────────────────────────────────────────────
    const handleCreate = async () => {
        if (!form.name.trim()) {
            toastError('Rule name is required');
            return;
        }
        setSaving(true);
        try {
            const payload = {
                name: form.name,
                type: form.type,
                adjustmentType: form.adjustmentType,
                adjustmentValue: form.adjustmentValue,
                applicableDays: form.applicableDays,
                startTime: form.startTime || undefined,
                endTime: form.endTime || undefined,
                startDate: form.startDate || undefined,
                endDate: form.endDate || undefined,
                serviceIds: form.serviceIds,
                minBookingsThreshold: form.minBookingsThreshold,
            };
            const res = await api.post('/api/v1/dynamicpricing/rules', payload);
            const newRule: PricingRule = {
                ...payload,
                id: res.data?.data?.id,
                isActive: true,
                createdAt: new Date().toISOString(),
                applicableDays: payload.applicableDays,
                serviceIds: payload.serviceIds,
            };
            setRules(prev => [newRule, ...prev]);
            setForm({ ...EMPTY_FORM });
            setShowForm(false);
            toastSuccess('Pricing rule created');
        } catch {
            toastError('Failed to create rule');
        } finally {
            setSaving(false);
        }
    };

    const handleToggle = async (id: string) => {
        try {
            await api.put(`/api/v1/dynamicpricing/rules/${id}/toggle`);
            setRules(prev =>
                prev.map(r => (r.id === id ? { ...r, isActive: !r.isActive } : r))
            );
        } catch {
            toastError('Failed to toggle rule');
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this pricing rule?')) return;
        try {
            await api.delete(`/api/v1/dynamicpricing/rules/${id}`);
            setRules(prev => prev.filter(r => r.id !== id));
            toastSuccess('Rule deleted');
        } catch {
            toastError('Failed to delete rule');
        }
    };

    // ── Price simulator ───────────────────────────────────────────────────────
    const runSimulation = async () => {
        if (!simServiceId) {
            toastError('Select a service to simulate');
            return;
        }
        setSimLoading(true);
        try {
            const res = await api.get(
                `/api/v1/dynamicpricing/calculate?serviceId=${simServiceId}&bookingTime=${encodeURIComponent(simDate)}&currentBookingsCount=${simDemand}`
            );
            setSimResult(res.data?.data ?? null);
        } catch {
            toastError('Simulation failed');
        } finally {
            setSimLoading(false);
        }
    };

    // ── Stats ─────────────────────────────────────────────────────────────────
    const activeRules = rules.filter(r => r.isActive).length;
    const surgeRules = rules.filter(r => r.type === 'surge' && r.isActive).length;
    const discountRules = rules.filter(r => r.type === 'off_peak' && r.isActive).length;

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-slate-950">
            {/* Header */}
            <div className="bg-white dark:bg-slate-900 border-b border-gray-100 dark:border-slate-800 px-6 py-5">
                <div className="max-w-6xl mx-auto flex items-center justify-between">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
                            <TrendingUp className="w-6 h-6 text-primary-600 dark:text-primary-400" />
                            Dynamic Pricing
                        </h1>
                        <p className="text-gray-500 dark:text-slate-400 text-sm mt-0.5">
                            Yield management — surge, off-peak, seasonal, and demand-based rules
                        </p>
                    </div>
                    <button
                        onClick={() => setShowForm(true)}
                        className="flex items-center gap-2 px-4 py-2.5 bg-primary-600 dark:bg-primary-500 text-white rounded-xl font-medium text-sm hover:bg-primary-700 dark:hover:bg-primary-600 transition-colors shadow-sm"
                    >
                        <Plus className="w-4 h-4" />
                        New Rule
                    </button>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-6 py-6 space-y-6">
                {/* KPI cards */}
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                    {[
                        { label: 'Total Rules', value: rules.length, icon: Tag, color: 'bg-primary-50 dark:bg-primary-900/10 text-primary-600 dark:text-primary-400' },
                        { label: 'Active Rules', value: activeRules, icon: CheckCircle2, color: 'bg-emerald-50 dark:bg-emerald-900/10 text-emerald-600 dark:text-emerald-400' },
                        { label: 'Surge Rules', value: surgeRules, icon: TrendingUp, color: 'bg-red-50 dark:bg-red-900/10 text-red-500 dark:text-red-400' },
                        { label: 'Discount Rules', value: discountRules, icon: TrendingDown, color: 'bg-blue-50 dark:bg-blue-900/10 text-blue-500 dark:text-blue-400' },
                    ].map(kpi => {
                        const Icon = kpi.icon;
                        return (
                            <div key={kpi.label} className="bg-white dark:bg-slate-900 rounded-xl border border-gray-100 dark:border-slate-800 p-4 shadow-sm transition-all hover:shadow-md">
                                <div className={cn('w-9 h-9 rounded-lg flex items-center justify-center mb-3', kpi.color)}>
                                    <Icon className="w-4 h-4" />
                                </div>
                                <p className="text-2xl font-bold text-gray-900 dark:text-white">{kpi.value}</p>
                                <p className="text-xs text-gray-500 dark:text-slate-400 mt-0.5">{kpi.label}</p>
                            </div>
                        );
                    })}
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Rules list */}
                    <div className="lg:col-span-2 space-y-3">
                        <h2 className="font-semibold text-gray-900 dark:text-white">Pricing Rules</h2>

                        {loading ? (
                            <div className="flex justify-center py-12">
                                <Loader2 className="w-6 h-6 text-primary-500 animate-spin" />
                            </div>
                        ) : rules.length === 0 ? (
                            <div className="bg-white dark:bg-slate-900 rounded-xl border border-dashed border-gray-200 dark:border-slate-800 p-10 text-center">
                                <TrendingUp className="w-8 h-8 text-gray-300 mx-auto mb-2" />
                                <p className="text-slate-300 text-sm">No pricing rules yet.</p>
                                <button
                                    onClick={() => setShowForm(true)}
                                    className="mt-3 text-primary-600 dark:text-primary-400 text-sm font-medium hover:underline"
                                >
                                    Create your first rule →
                                </button>
                            </div>
                        ) : (
                            rules.map(rule => {
                                const expanded = expandedRule === rule.id;
                                const typeInfo = RULE_TYPES.find(t => t.value === rule.type);
                                const isPositive = rule.adjustmentValue >= 0;

                                return (
                                    <div
                                        key={rule.id}
                                        className={cn(
                                            'bg-white dark:bg-slate-900 rounded-xl border shadow-sm transition-all',
                                            rule.isActive ? 'border-gray-100 dark:border-slate-800' : 'border-gray-100 dark:border-slate-800 opacity-60'
                                        )}
                                    >
                                        <div className="flex items-center gap-3 p-4">
                                            <button
                                                onClick={() => handleToggle(rule.id)}
                                                className="flex-shrink-0"
                                                title={rule.isActive ? 'Disable' : 'Enable'}
                                            >
                                                {rule.isActive ? (
                                                    <ToggleRight className="w-8 h-8 text-success-fg" />
                                                ) : (
                                                    <ToggleLeft className="w-8 h-8 text-gray-300" />
                                                )}
                                            </button>

                                            <div className="flex-1 min-w-0">
                                                <div className="flex items-center gap-2 flex-wrap">
                                                    <span className="font-semibold text-gray-900 dark:text-white text-sm">
                                                        {rule.name}
                                                    </span>
                                                    <RuleTypeBadge type={rule.type} />
                                                </div>
                                                <p className="text-xs text-foreground-muted mt-0.5">
                                                    {rule.adjustmentType === 'percentage'
                                                        ? `${isPositive ? '+' : ''}${rule.adjustmentValue}%`
                                                        : `${isPositive ? '+' : ''}${formatCurrency(rule.adjustmentValue)}`}{' '}
                                                    adjustment
                                                    {rule.applicableDays.length > 0 && (
                                                        <> · {rule.applicableDays.join(', ')}</>
                                                    )}
                                                    {rule.startTime && rule.endTime && (
                                                        <> · {rule.startTime}–{rule.endTime}</>
                                                    )}
                                                </p>
                                            </div>

                                            <div className="flex items-center gap-1 flex-shrink-0">
                                                <span
                                                    className={cn(
                                                        'px-2 py-0.5 rounded-full text-xs font-bold',
                                                        isPositive
                                                            ? 'bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 border border-red-100 dark:border-red-900/30'
                                                            : 'bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-900/30'
                                                    )}
                                                >
                                                    {isPositive ? '+' : ''}
                                                    {rule.adjustmentType === 'percentage'
                                                        ? `${rule.adjustmentValue}%`
                                                        : formatCurrency(rule.adjustmentValue)}
                                                </span>
                                                <button
                                                    onClick={() => setExpandedRule(expanded ? null : rule.id)}
                                                    className="p-1.5 hover:bg-gray-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                                                >
                                                    {expanded ? (
                                                        <ChevronUp className="w-4 h-4 text-foreground-muted" />
                                                    ) : (
                                                        <ChevronDown className="w-4 h-4 text-foreground-muted" />
                                                    )}
                                                </button>
                                                <button
                                                    onClick={() => handleDelete(rule.id)}
                                                    className="p-1.5 hover:bg-red-50 dark:hover:bg-red-900/20 hover:text-red-500 dark:hover:text-red-400 rounded-lg transition-colors text-foreground-muted"
                                                >
                                                    <Trash2 className="w-4 h-4" />
                                                </button>
                                            </div>
                                        </div>

                                        {/* Expanded details */}
                                        {expanded && (
                                            <div className="border-t border-gray-50 dark:border-slate-800 px-4 pb-4 pt-3 text-xs text-gray-500 dark:text-slate-400 space-y-1 bg-gray-50/50 dark:bg-slate-800/20 rounded-b-xl">
                                                {rule.serviceIds.length > 0 && (
                                                    <p>
                                                        <span className="font-medium text-gray-600 dark:text-slate-300">Services:</span>{' '}
                                                        {rule.serviceIds.map(id => services.find(s => s.id === id)?.name ?? id).join(', ')}
                                                    </p>
                                                )}
                                                {rule.startDate && (
                                                    <p>
                                                        <span className="font-medium text-gray-600 dark:text-slate-300">Date range:</span>{' '}
                                                        {rule.startDate} → {rule.endDate || 'no end'}
                                                    </p>
                                                )}
                                                {rule.minBookingsThreshold && (
                                                    <p>
                                                        <span className="font-medium text-gray-600 dark:text-slate-300">Demand threshold:</span>{' '}
                                                        ≥ {rule.minBookingsThreshold} bookings
                                                    </p>
                                                )}
                                                <p>
                                                    <span className="font-medium text-gray-600 dark:text-slate-300">Created:</span>{' '}
                                                    {new Date(rule.createdAt).toLocaleDateString()}
                                                </p>
                                            </div>
                                        )}
                                    </div>
                                );
                            })
                        )}
                    </div>

                    {/* Sidebar: Price Simulator */}
                    <div className="space-y-4">
                        <div className="bg-white dark:bg-slate-900 rounded-xl border border-gray-100 dark:border-slate-800 shadow-sm p-5">
                            <h3 className="font-semibold text-gray-900 dark:text-white mb-4 flex items-center gap-2 text-sm uppercase tracking-wider">
                                <Zap className="w-3.5 h-3.5 text-primary-600 dark:text-primary-400" />
                                Price Simulator
                            </h3>

                            <div className="space-y-3">
                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">Service</label>
                                    <select
                                        value={simServiceId}
                                        onChange={e => setSimServiceId(e.target.value)}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    >
                                        <option value="">Select service...</option>
                                        {services.map(s => (
                                            <option key={s.id} value={s.id}>
                                                {s.name} ({formatCurrency(s.price)})
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        Booking date &amp; time
                                    </label>
                                    <input
                                        type="datetime-local"
                                        value={simDate}
                                        onChange={e => setSimDate(e.target.value)}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>

                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        Current day bookings (demand)
                                    </label>
                                    <input
                                        type="number"
                                        min={0}
                                        value={simDemand}
                                        onChange={e => setSimDemand(+e.target.value)}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>

                                <button
                                    onClick={runSimulation}
                                    disabled={simLoading || !simServiceId}
                                    className="w-full py-2 bg-primary-600 dark:bg-primary-500 text-white rounded-lg text-sm font-medium hover:bg-primary-700 dark:hover:bg-primary-600 disabled:opacity-40 transition-all flex items-center justify-center gap-2 shadow-sm"
                                >
                                    {simLoading ? (
                                        <Loader2 className="w-4 h-4 animate-spin" />
                                    ) : (
                                        <Zap className="w-4 h-4" />
                                    )}
                                    Simulate
                                </button>
                            </div>

                            {simResult && (
                                <div className="mt-4 pt-4 border-t border-gray-50 dark:border-slate-800">
                                    <div className="flex justify-between text-sm mb-1.5">
                                        <span className="text-gray-500 dark:text-slate-400">Base price</span>
                                        <span className="font-medium text-slate-900 dark:text-white">{formatCurrency(simResult.basePrice)}</span>
                                    </div>
                                    {simResult.appliedRules.map((r: any, i: number) => (
                                        <div key={i} className="flex justify-between text-sm mb-1 text-primary-600 dark:text-primary-400 bg-primary-50/50 dark:bg-primary-900/10 px-2 py-0.5 rounded">
                                            <span>{r.name}</span>
                                            <span className="font-semibold">
                                                {r.adjustment >= 0 ? '+' : ''}
                                                {r.adjustmentType === 'percentage'
                                                    ? `${r.adjustmentValue}%`
                                                    : formatCurrency(r.adjustment)}
                                            </span>
                                        </div>
                                    ))}
                                    <div className="flex justify-between font-bold text-base mt-2 pt-2 border-t border-gray-100 dark:border-slate-800">
                                        <span className="text-slate-900 dark:text-white">Effective price</span>
                                        <span
                                            className={cn(
                                                simResult.effectivePrice > simResult.basePrice
                                                    ? 'text-red-600 dark:text-red-400'
                                                    : simResult.effectivePrice < simResult.basePrice
                                                    ? 'text-emerald-600 dark:text-emerald-400'
                                                    : 'text-gray-900 dark:text-white'
                                            )}
                                        >
                                            {formatCurrency(simResult.effectivePrice)}
                                        </span>
                                    </div>
                                    {simResult.appliedRules.length === 0 && (
                                        <p className="text-xs text-foreground-muted mt-2">No rules applied at this time.</p>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* Rule type legend */}
                        <div className="bg-white dark:bg-slate-900 rounded-xl border border-gray-100 dark:border-slate-800 shadow-sm p-5">
                            <h3 className="font-semibold text-gray-900 dark:text-white mb-3 text-sm">Rule Types</h3>
                            <div className="space-y-2">
                                {RULE_TYPES.map(rt => {
                                    const Icon = rt.icon;
                                    return (
                                        <div key={rt.value} className="flex items-start gap-2">
                                            <Icon className={cn('w-4 h-4 mt-0.5 flex-shrink-0', rt.color)} />
                                            <div>
                                                <p className="text-xs font-semibold text-gray-700 dark:text-slate-300">{rt.label}</p>
                                                <p className="text-xs text-foreground-muted">{rt.desc}</p>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* ── Create rule modal ─────────────────────────────────────────── */}
            {showForm && (
                <div className="fixed inset-0 bg-black/40 dark:bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
                    <div className="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto border border-white/10">
                        <div className="p-6 border-b border-gray-100 dark:border-slate-800">
                            <h2 className="text-lg font-bold text-gray-900 dark:text-white">New Pricing Rule</h2>
                            <p className="text-sm text-gray-500 dark:text-slate-400 mt-0.5">
                                Configure when and how prices should adjust automatically
                            </p>
                        </div>

                        <div className="p-6 space-y-5">
                            {/* Name */}
                            <div>
                                <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-1.5">
                                    Rule name *
                                </label>
                                <input
                                    type="text"
                                    placeholder="e.g. Weekend Surge"
                                    value={form.name}
                                    onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                                    className="w-full px-3 py-2.5 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                />
                            </div>

                            {/* Type */}
                            <div>
                                <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-2">
                                    Rule type
                                </label>
                                <div className="grid grid-cols-2 gap-2">
                                    {RULE_TYPES.map(rt => {
                                        const Icon = rt.icon;
                                        return (
                                            <button
                                                key={rt.value}
                                                onClick={() => setForm(f => ({ ...f, type: rt.value as PricingRule['type'] }))}
                                                className={cn(
                                                    'flex items-center gap-2 p-2.5 rounded-lg border text-left text-xs transition-all',
                                                    form.type === rt.value
                                                        ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/10'
                                                        : 'border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:border-gray-300 dark:hover:border-slate-600'
                                                )}
                                            >
                                                <Icon className={cn('w-4 h-4', rt.color)} />
                                                <span className="font-medium text-gray-700 dark:text-slate-300">{rt.label}</span>
                                            </button>
                                        );
                                    })}
                                </div>
                            </div>

                            {/* Adjustment */}
                            <div>
                                <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-2">
                                    Price adjustment
                                </label>
                                <div className="flex gap-2">
                                    <select
                                        value={form.adjustmentType}
                                        onChange={e =>
                                            setForm(f => ({ ...f, adjustmentType: e.target.value as 'percentage' | 'fixed' }))
                                        }
                                        className="px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 appearance-none"
                                    >
                                        <option value="percentage">% Percentage</option>
                                        <option value="fixed">$ Fixed amount</option>
                                    </select>
                                    <input
                                        type="number"
                                        value={form.adjustmentValue}
                                        onChange={e =>
                                            setForm(f => ({ ...f, adjustmentValue: parseFloat(e.target.value) || 0 }))
                                        }
                                        className="flex-1 px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                        placeholder="e.g. 20 for +20%"
                                    />
                                </div>
                                <p className="text-xs text-foreground-muted mt-1">
                                    Use negative values for discounts (e.g. −15 for 15% off)
                                </p>
                            </div>

                            {/* Days of week */}
                            <div>
                                <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-2">
                                    Applicable days (leave blank = all days)
                                </label>
                                <div className="flex flex-wrap gap-1.5">
                                    {DAYS_OF_WEEK.map(day => (
                                        <button
                                            key={day}
                                            onClick={() => toggleDay(day)}
                                            className={cn(
                                                'px-2.5 py-1 rounded-full text-xs font-medium border transition-all',
                                                form.applicableDays.includes(day)
                                                    ? 'bg-primary-600 text-white border-primary-600'
                                                    : 'bg-white dark:bg-slate-800 text-gray-600 dark:text-slate-400 border-gray-200 dark:border-slate-700 hover:border-gray-300 dark:hover:border-slate-600'
                                            )}
                                        >
                                            {day.slice(0, 3)}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Time window */}
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        Start time (optional)
                                    </label>
                                    <input
                                        type="time"
                                        value={form.startTime}
                                        onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>
                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        End time (optional)
                                    </label>
                                    <input
                                        type="time"
                                        value={form.endTime}
                                        onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>
                            </div>

                            {/* Date range (seasonal) */}
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        Start date (optional)
                                    </label>
                                    <input
                                        type="date"
                                        value={form.startDate}
                                        onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>
                                <div>
                                    <label className="text-xs font-semibold text-gray-600 dark:text-slate-400 block mb-1">
                                        End date (optional)
                                    </label>
                                    <input
                                        type="date"
                                        value={form.endDate}
                                        onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))}
                                        className="w-full px-3 py-2 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>
                            </div>

                            {/* Demand threshold */}
                            {form.type === 'demand' && (
                                <div>
                                    <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-1.5">
                                        Demand threshold (min bookings today)
                                    </label>
                                    <input
                                        type="number"
                                        min={1}
                                        value={form.minBookingsThreshold ?? ''}
                                        onChange={e =>
                                            setForm(f => ({ ...f, minBookingsThreshold: parseInt(e.target.value) || undefined }))
                                        }
                                        placeholder="e.g. 10"
                                        className="w-full px-3 py-2.5 border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 transition-shadow"
                                    />
                                </div>
                            )}

                            {/* Services */}
                            {services.length > 0 && (
                                <div>
                                    <label className="text-sm font-semibold text-gray-700 dark:text-slate-300 block mb-2">
                                        Apply to services (leave blank = all)
                                    </label>
                                    <div className="max-h-36 overflow-y-auto space-y-1 border border-gray-100 dark:border-slate-800 rounded-lg p-2 dark:bg-slate-800/50">
                                        {services.map(s => (
                                            <label
                                                key={s.id}
                                                className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-slate-800 px-2 py-1 rounded-md transition-colors"
                                            >
                                                <input
                                                    type="checkbox"
                                                    checked={form.serviceIds.includes(s.id)}
                                                    onChange={() => toggleServiceId(s.id)}
                                                    className="rounded accent-primary-600 dark:bg-slate-700 dark:border-slate-600"
                                                />
                                                <span className="text-sm text-gray-700 dark:text-slate-300">{s.name}</span>
                                                <span className="text-xs text-foreground-muted ml-auto">
                                                    {formatCurrency(s.price)}
                                                </span>
                                            </label>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>

                        <div className="p-6 border-t border-gray-100 dark:border-slate-800 flex gap-3 bg-gray-50/50 dark:bg-slate-800/30 rounded-b-2xl">
                            <button
                                onClick={() => { setShowForm(false); setForm({ ...EMPTY_FORM }); }}
                                className="flex-1 py-2.5 border border-gray-200 dark:border-slate-700 text-gray-700 dark:text-slate-300 rounded-xl text-sm font-medium hover:bg-gray-50 dark:hover:bg-slate-800 transition-colors"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleCreate}
                                disabled={saving || !form.name.trim()}
                                className="flex-1 py-2.5 bg-primary-600 text-white rounded-xl text-sm font-bold hover:bg-primary-700 disabled:opacity-40 transition-colors flex items-center justify-center gap-2"
                            >
                                {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                                Create Rule
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
