"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    FlaskConical, Plus, Trash2, RefreshCw, ToggleLeft, ToggleRight,
    TrendingUp, Users, BarChart3, CheckCircle, Loader2, X, Save,
    ChevronDown, ChevronUp, Award
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface Experiment {
    id: string;
    name: string;
    variantA: string;
    variantB: string;
    isActive: boolean;
    trafficSplit: number;
    createdAt: string;
}

interface ExperimentResults {
    variantA: { impressions: number; conversions: number; conversionRate: number };
    variantB: { impressions: number; conversions: number; conversionRate: number };
    winner: 'A' | 'B';
    confidenceLevel: number;
}

interface ExperimentDetail extends Experiment {
    results: ExperimentResults;
}

export default function ExperimentsPage() {
    const [experiments, setExperiments] = useState<Experiment[]>([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState({ name: '', variantA: 'Control', variantB: 'Variation', trafficSplit: 50 });
    const [saving, setSaving] = useState(false);
    const [expandedId, setExpandedId] = useState<string | null>(null);
    const [expandedData, setExpandedData] = useState<ExperimentDetail | null>(null);
    const [loadingDetail, setLoadingDetail] = useState(false);
    const [togglingId, setTogglingId] = useState<string | null>(null);

    const fetchExperiments = useCallback(async () => {
        setLoading(true);
        try {
            const res = await apiClient.get('/api/v1/experiments');
            const data = res.data?.data || res.data;
            setExperiments(data?.experiments || data || []);
        } catch {
            toast.error('Failed to load experiments');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchExperiments(); }, [fetchExperiments]);

    const handleCreate = async () => {
        if (!form.name) { toast.error('Name is required'); return; }
        setSaving(true);
        try {
            const res = await apiClient.post('/api/v1/experiments', {
                name: form.name,
                variantA: form.variantA,
                variantB: form.variantB,
                trafficSplit: form.trafficSplit / 100,
            });
            toast.success('Experiment created');
            setShowForm(false);
            setForm({ name: '', variantA: 'Control', variantB: 'Variation', trafficSplit: 50 });
            fetchExperiments();
        } catch {
            toast.error('Failed to create experiment');
        } finally {
            setSaving(false);
        }
    };

    const handleToggle = async (exp: Experiment) => {
        setTogglingId(exp.id);
        try {
            await apiClient.post(`/api/v1/experiments/${exp.id}/toggle`);
            setExperiments(prev => prev.map(e => e.id === exp.id ? { ...e, isActive: !e.isActive } : e));
            toast.success(exp.isActive ? 'Experiment paused' : 'Experiment resumed');
        } catch {
            toast.error('Failed to toggle experiment');
        } finally {
            setTogglingId(null);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this experiment and its data?')) return;
        try {
            await apiClient.delete(`/api/v1/experiments/${id}`);
            setExperiments(prev => prev.filter(e => e.id !== id));
            if (expandedId === id) { setExpandedId(null); setExpandedData(null); }
            toast.success('Experiment deleted');
        } catch {
            toast.error('Failed to delete experiment');
        }
    };

    const handleExpand = async (id: string) => {
        if (expandedId === id) { setExpandedId(null); setExpandedData(null); return; }
        setExpandedId(id);
        setExpandedData(null);
        setLoadingDetail(true);
        try {
            const res = await apiClient.get(`/api/v1/experiments/${id}`);
            setExpandedData(res.data?.data || res.data);
        } catch {
            toast.error('Failed to load results');
        } finally {
            setLoadingDetail(false);
        }
    };

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">A/B Testing</h1>
                    <p className="text-slate-500 mt-1">Run experiments to optimize conversions and engagement</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchExperiments} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowForm(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Experiment
                    </Button>
                </div>
            </div>

            {/* Summary */}
            <div className="grid grid-cols-3 gap-4">
                {[
                    { label: 'Total Experiments', value: experiments.length, icon: <FlaskConical className="h-5 w-5 text-primary-500" /> },
                    { label: 'Active', value: experiments.filter(e => e.isActive).length, icon: <TrendingUp className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Paused', value: experiments.filter(e => !e.isActive).length, icon: <BarChart3 className="h-5 w-5 text-amber-500" /> },
                ].map(s => (
                    <div key={s.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                        <div className="p-2 bg-slate-50 rounded-lg">{s.icon}</div>
                        <div>
                            <div className="text-xl font-bold text-slate-900">{s.value}</div>
                            <div className="text-xs text-slate-500">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Create Form */}
            {showForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">New A/B Experiment</h2>
                        <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-600">
                            <X className="h-4 w-4" />
                        </button>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Experiment Name</label>
                        <Input
                            value={form.name}
                            onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                            placeholder="e.g., Booking CTA button color"
                        />
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Variant A (Control)</label>
                            <Input
                                value={form.variantA}
                                onChange={e => setForm(p => ({ ...p, variantA: e.target.value }))}
                                placeholder="Control"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Variant B (Test)</label>
                            <Input
                                value={form.variantB}
                                onChange={e => setForm(p => ({ ...p, variantB: e.target.value }))}
                                placeholder="Variation"
                            />
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">
                            Traffic Split: <span className="text-primary-600">{form.trafficSplit}% → A</span> / <span className="text-primary-600">{100 - form.trafficSplit}% → B</span>
                        </label>
                        <input
                            type="range"
                            min="10"
                            max="90"
                            step="5"
                            value={form.trafficSplit}
                            onChange={e => setForm(p => ({ ...p, trafficSplit: parseInt(e.target.value) }))}
                            className="w-full accent-primary-600"
                        />
                        <div className="flex justify-between text-xs text-slate-400 mt-1">
                            <span>10%</span>
                            <span>50/50</span>
                            <span>90%</span>
                        </div>
                    </div>
                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleCreate} disabled={saving}>
                            {saving ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Save className="h-4 w-4 mr-2" />}
                            {saving ? 'Creating...' : 'Create Experiment'}
                        </Button>
                        <Button variant="outline" onClick={() => setShowForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Experiments List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-24" />)}
                </div>
            ) : experiments.length === 0 ? (
                <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
                    <FlaskConical className="h-12 w-12 text-slate-300 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700">No experiments yet</h3>
                    <p className="text-slate-500 text-sm mt-1 mb-4">Create your first A/B test to start optimizing</p>
                    <Button onClick={() => setShowForm(true)}><Plus className="h-4 w-4 mr-2" /> New Experiment</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {experiments.map(exp => (
                        <div key={exp.id} className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                            <div className="flex items-center gap-4 p-4 hover:bg-slate-50 cursor-pointer" onClick={() => handleExpand(exp.id)}>
                                <div className={`w-2.5 h-2.5 rounded-full shrink-0 ${exp.isActive ? 'bg-emerald-500' : 'bg-slate-300'}`} />
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2">
                                        <span className="font-semibold text-slate-900">{exp.name}</span>
                                        <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${exp.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                                            {exp.isActive ? 'Active' : 'Paused'}
                                        </span>
                                    </div>
                                    <div className="text-xs text-slate-500 mt-0.5">
                                        <span className="text-primary-600 font-medium">{exp.variantA}</span>
                                        <span className="mx-2">vs</span>
                                        <span className="text-primary-600 font-medium">{exp.variantB}</span>
                                        <span className="mx-2">·</span>
                                        <span>{Math.round(exp.trafficSplit * 100)}% / {Math.round((1 - exp.trafficSplit) * 100)}% split</span>
                                    </div>
                                </div>
                                <div className="flex items-center gap-2 shrink-0">
                                    <button
                                        onClick={e => { e.stopPropagation(); handleToggle(exp); }}
                                        className="p-1.5 hover:bg-slate-100 rounded-lg"
                                        title={exp.isActive ? 'Pause' : 'Resume'}
                                        disabled={togglingId === exp.id}
                                    >
                                        {togglingId === exp.id
                                            ? <Loader2 className="h-4 w-4 animate-spin text-slate-400" />
                                            : exp.isActive
                                                ? <ToggleRight className="h-4 w-4 text-emerald-500" />
                                                : <ToggleLeft className="h-4 w-4 text-slate-400" />}
                                    </button>
                                    <button
                                        onClick={e => { e.stopPropagation(); handleDelete(exp.id); }}
                                        className="p-1.5 hover:bg-red-50 rounded-lg text-slate-400 hover:text-red-500"
                                        title="Delete"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                    {expandedId === exp.id ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
                                </div>
                            </div>

                            {/* Expanded Results */}
                            {expandedId === exp.id && (
                                <div className="border-t border-slate-100 p-4 bg-slate-50">
                                    {loadingDetail ? (
                                        <div className="flex items-center gap-2 text-slate-500 text-sm">
                                            <Loader2 className="h-4 w-4 animate-spin" /> Loading results...
                                        </div>
                                    ) : expandedData?.results ? (
                                        <div>
                                            <div className="flex items-center gap-2 mb-3">
                                                <BarChart3 className="h-4 w-4 text-slate-500" />
                                                <span className="font-semibold text-slate-900 text-sm">Experiment Results</span>
                                                <span className="text-xs text-slate-400">· {expandedData.results.confidenceLevel}% confidence</span>
                                                <span className={`ml-auto px-2 py-0.5 text-xs font-medium rounded-full flex items-center gap-1 ${expandedData.results.winner === 'A' ? 'bg-primary-50 text-primary-700' : 'bg-primary-50 text-primary-700'}`}>
                                                    <Award className="h-3 w-3" /> Variant {expandedData.results.winner} winning
                                                </span>
                                            </div>
                                            <div className="grid grid-cols-2 gap-4">
                                                {(['A', 'B'] as const).map(variant => {
                                                    const data = variant === 'A' ? expandedData.results.variantA : expandedData.results.variantB;
                                                    const name = variant === 'A' ? expandedData.variantA : expandedData.variantB;
                                                    const isWinner = expandedData.results.winner === variant;
                                                    return (
                                                        <div key={variant} className={`rounded-xl p-4 border ${isWinner ? (variant === 'A' ? 'border-primary-200 bg-primary-50' : 'border-primary-200 bg-primary-50') : 'border-slate-200 bg-white'}`}>
                                                            <div className="flex items-center gap-2 mb-3">
                                                                <span className={`text-xs font-bold px-1.5 py-0.5 rounded ${variant === 'A' ? 'bg-primary-600 text-white' : 'bg-primary-600 text-white'}`}>
                                                                    {variant}
                                                                </span>
                                                                <span className="font-medium text-slate-900 text-sm">{name}</span>
                                                                {isWinner && <CheckCircle className="h-3.5 w-3.5 text-emerald-500 ml-auto" />}
                                                            </div>
                                                            <div className="space-y-2 text-sm">
                                                                <div className="flex justify-between">
                                                                    <span className="text-slate-500">Impressions</span>
                                                                    <span className="font-semibold">{data.impressions.toLocaleString()}</span>
                                                                </div>
                                                                <div className="flex justify-between">
                                                                    <span className="text-slate-500">Conversions</span>
                                                                    <span className="font-semibold">{data.conversions.toLocaleString()}</span>
                                                                </div>
                                                                <div className="flex justify-between">
                                                                    <span className="text-slate-500">Conv. Rate</span>
                                                                    <span className={`font-bold ${isWinner ? 'text-emerald-600' : 'text-slate-700'}`}>{data.conversionRate}%</span>
                                                                </div>
                                                                {/* Progress bar */}
                                                                <div className="h-1.5 bg-white rounded-full overflow-hidden mt-1">
                                                                    <div
                                                                        className={`h-full rounded-full ${variant === 'A' ? 'bg-primary-500' : 'bg-primary-500'}`}
                                                                        style={{ width: `${Math.min(data.conversionRate * 5, 100)}%` }}
                                                                    />
                                                                </div>
                                                            </div>
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                        </div>
                                    ) : (
                                        <p className="text-sm text-slate-500">No results data available yet</p>
                                    )}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
