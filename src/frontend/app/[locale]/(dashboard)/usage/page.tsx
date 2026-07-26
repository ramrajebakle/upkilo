'use client';

import { useState, useEffect } from 'react';
import {
    BarChart3, TrendingUp, Zap, MessageSquare, HardDrive, Users, MapPin,
    Download, AlertCircle, CheckCircle, AlertTriangle, Loader2, Calendar,
    DollarSign, Settings2, Save, X, Activity, PieChart, Layers, 
    ArrowUpRight, ArrowDownRight, RefreshCw, ChevronRight, ShieldCheck,
    Cloud, Cpu, Database, Info, Plus
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { UsageProgress } from '@/components/ui/UsageProgress';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';
import { motion, AnimatePresence } from 'framer-motion';

export default function UsageDashboardPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(true);
    const [dashboard, setDashboard] = useState<any>(null);
    const [updatingBudget, setUpdatingBudget] = useState(false);
    const [newBudget, setNewBudget] = useState<string>('');
    const [showBudgetForm, setShowBudgetForm] = useState(false);

    useEffect(() => {
        fetchDashboard();
    }, []);

    const fetchDashboard = async () => {
        setLoading(true);
        try {
            const res = await api.usageDashboard.get();
            setDashboard(res.data);
        } catch (err) {
            console.error('Failed to fetch usage dashboard:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleUpdateBudget = async () => {
        const budgetValue = parseFloat(newBudget);
        if (isNaN(budgetValue) || budgetValue < 0) {
            toastError('Please enter a valid budget amount');
            return;
        }

        setUpdatingBudget(true);
        try {
            await api.billing.updateAiBudget(budgetValue);
            toastSuccess('AI monthly budget updated');
            setShowBudgetForm(false);
            fetchDashboard();
        } catch (err) {
            console.error('Failed to update AI budget:', err);
            toastError('Failed to update AI budget');
        } finally {
            setUpdatingBudget(false);
        }
    };

    const handleExport = async () => {
        try {
            const res = await api.usageDashboard.exportCsv({ from: '', to: '' });
            const blob = new Blob([res.data], { type: 'text/csv' });
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `usage-report-${new Date().toISOString().split('T')[0]}.csv`;
            a.click();
            window.URL.revokeObjectURL(url);
            toastSuccess('Usage report transmitted.');
        } catch (err) {
            toastError('Export failed.');
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Resource Gauges...</p>
            </div>
        );
    }

    const { summary, subscription, aiBreakdown, usageTrend, alerts } = dashboard || {};

    return (
        <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header / Primary Stats */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-gradient-to-br from-emerald-600 to-teal-900 rounded-[28px] shadow-2xl shadow-emerald-500/20 border border-emerald-500/20">
                        <Activity className="h-8 w-8 text-white" />
                    </div>
                    <div>
                        <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Resource Dashboard</h1>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">
                            Current Tier: {subscription?.planName || 'ACTIVE'} • Cycle Ends: {new Date(summary?.periodEnd).toLocaleDateString()}
                        </p>
                    </div>
                </div>
                <Button 
                    onClick={handleExport}
                    variant="outline"
                    className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] dark:bg-slate-900 dark:border-slate-800 dark:text-slate-400 hover:text-primary-500 transition-all shadow-xl active:scale-95 flex items-center gap-3"
                >
                    <Download className="h-4 w-4" /> Export Protocol
                </Button>
            </div>

            {/* Critical Signal Corridor (Alerts) */}
            <AnimatePresence>
                {alerts && alerts.length > 0 && (
                    <motion.div 
                        initial={{ opacity: 0, y: -20 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="space-y-3"
                    >
                        {alerts.map((alert: any, i: number) => (
                            <div
                                key={i}
                                className={cn(
                                    "flex items-center gap-6 p-6 rounded-[28px] border relative overflow-hidden group",
                                    alert.type === 'danger' && "bg-red-50 dark:bg-red-950/20 border-red-100 dark:border-red-900/30 text-red-600 dark:text-red-400",
                                    alert.type === 'warning' && "bg-amber-50 dark:bg-amber-950/10 border-amber-100 dark:border-amber-900/30 text-amber-600 dark:text-amber-400",
                                    alert.type === 'info' && "bg-blue-50 dark:bg-blue-950/10 border-blue-100 dark:border-blue-900/30 text-blue-600 dark:text-blue-400"
                                )}
                            >
                                <div className="p-3 bg-white/20 dark:bg-white/5 rounded-xl backdrop-blur-md">
                                    {alert.type === 'danger' ? <AlertCircle className="h-5 w-5" /> : alert.type === 'warning' ? <AlertTriangle className="h-5 w-5" /> : <Info className="h-5 w-5" />}
                                </div>
                                <div className="flex-1">
                                    <p className="text-[10px] font-black uppercase tracking-widest opacity-60 mb-0.5">{alert.type} Signal detected</p>
                                    <p className="text-xs font-bold uppercase tracking-widest">{alert.message}</p>
                                </div>
                                <div className="text-right">
                                    <p className="text-xl font-black tabular-nums">{alert.percentage}%</p>
                                    <div className="w-16 h-1.5 bg-current/20 rounded-full mt-1 overflow-hidden">
                                        <div className="h-full bg-current" style={{ width: `${alert.percentage}%` }} />
                                    </div>
                                </div>
                                <div className="absolute top-0 right-0 h-full w-1 bg-current opacity-20" />
                            </div>
                        ))}
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Consumption Matrix (Grid) */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                <UsageMetricCard
                    icon={<Calendar className="h-6 w-6 text-indigo-500" />}
                    title="Engagement Capacity"
                    subtitle="Monthly Bookings"
                    used={summary?.bookingsUsed || 0}
                    limit={summary?.bookingsLimit || 0}
                    color="from-indigo-500 to-blue-600"
                />
                <UsageMetricCard
                    icon={<MessageSquare className="h-6 w-6 text-emerald-500" />}
                    title="Communication Band"
                    subtitle="SMS Protocols"
                    used={summary?.smsUsed || 0}
                    limit={summary?.smsLimit || 0}
                    color="from-emerald-500 to-teal-600"
                />
                <UsageMetricCard
                    icon={<Zap className="h-6 w-6 text-primary-500" />}
                    title="Neural Processing"
                    subtitle="AI Cognitive Credits"
                    used={summary?.aiCreditsUsed || 0}
                    limit={summary?.aiCreditsLimit || 0}
                    color="from-primary-500 to-indigo-600"
                />
                <UsageMetricCard
                    icon={<HardDrive className="h-6 w-6 text-blue-500" />}
                    title="Spatial Storage"
                    subtitle="Binary Data Pool"
                    used={Math.round((summary?.storageUsedBytes || 0) / 1024 / 1024 / 1024 * 100) / 100}
                    limit={Math.round((summary?.storageLimitBytes || 0) / 1024 / 1024 / 1024 * 100) / 100}
                    unit="GB"
                    color="from-blue-500 to-cyan-600"
                />
                <UsageMetricCard
                    icon={<Users className="h-6 w-6 text-rose-500" />}
                    title="Operative Seats"
                    subtitle="Authorized Agents"
                    used={summary?.staffCount || 0}
                    limit={summary?.staffLimit || 0}
                    unit="Seats"
                    color="from-rose-500 to-pink-600"
                />
                
                {/* AI Budget Controller Card */}
                <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 p-10 shadow-2xl shadow-slate-200/40 dark:shadow-none flex flex-col justify-between group relative overflow-hidden">
                    <div className="relative z-10 space-y-8">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-4">
                                <div className="p-3 bg-emerald-50 dark:bg-emerald-500/10 rounded-2xl">
                                    <DollarSign className="h-6 w-6 text-emerald-500" />
                                </div>
                                <div>
                                    <h3 className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-tight">AI Monetary Budget</h3>
                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mt-0.5">Tactical Cost Management</p>
                                </div>
                            </div>
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                    setNewBudget(summary?.aiCostLimit?.toString() || '0');
                                    setShowBudgetForm(!showBudgetForm);
                                }}
                                className="h-10 w-10 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-850 transition-colors"
                            >
                                <Settings2 className="h-4 w-4 text-slate-400" />
                            </Button>
                        </div>
                        
                        <div className="space-y-4">
                            <div className="flex items-end justify-between">
                                <span className="text-4xl font-black text-slate-900 dark:text-white tracking-tighter tabular-nums">
                                    ${summary?.aiCostUsed?.toFixed(2)}
                                </span>
                                <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest pb-1.5">
                                    of ${summary?.aiCostLimit} Budget
                                </span>
                            </div>
                            <div className="h-3 w-full bg-slate-50 dark:bg-slate-950 rounded-full overflow-hidden shadow-inner">
                                <div 
                                    className="h-full bg-gradient-to-r from-emerald-500 to-teal-600 rounded-full transition-all duration-1000 shadow-glow shadow-emerald-500/30"
                                    style={{ width: `${Math.min((summary?.aiCostUsed / summary?.aiCostLimit) * 100, 100)}%` }}
                                />
                            </div>
                        </div>
                    </div>

                    <AnimatePresence>
                        {showBudgetForm && (
                            <motion.div 
                                initial={{ opacity: 0, height: 0 }}
                                animate={{ opacity: 1, height: 'auto' }}
                                exit={{ opacity: 0, height: 0 }}
                                className="mt-8 pt-8 border-t border-slate-50 dark:border-slate-850 relative z-10"
                            >
                                <div className="flex items-center gap-3">
                                    <div className="relative flex-1">
                                        <span className="absolute left-4 top-1/2 -translate-y-1/2 font-black text-slate-400 text-xs">$</span>
                                        <input
                                            type="number"
                                            value={newBudget}
                                            onChange={(e) => setNewBudget(e.target.value)}
                                            className="w-full h-12 pl-8 pr-4 bg-slate-50 dark:bg-slate-950 rounded-xl border border-transparent dark:border-slate-850 text-xs font-black uppercase tracking-widest outline-none focus:border-emerald-500 transition-all dark:text-white shadow-inner"
                                            placeholder="NEW BUDGET"
                                        />
                                    </div>
                                    <Button onClick={handleUpdateBudget} loading={updatingBudget} className="h-12 w-12 rounded-xl bg-emerald-500 hover:bg-emerald-600 shadow-lg shadow-emerald-500/20">
                                        <Save className="h-4 w-4" />
                                    </Button>
                                    <Button variant="ghost" onClick={() => setShowBudgetForm(false)} className="h-12 w-12 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-850">
                                        <X className="h-4 w-4 text-slate-400" />
                                    </Button>
                                </div>
                            </motion.div>
                        )}
                    </AnimatePresence>
                    
                    <div className="absolute top-0 right-0 w-32 h-32 bg-emerald-500/5 blur-3xl rounded-full" />
                </div>
            </div>

            {/* In-Depth AI Telemetry */}
            {aiBreakdown && (
                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12 relative overflow-hidden">
                    <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 relative z-10">
                        <div className="flex items-center gap-4">
                            <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg" />
                            <div>
                                <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">AI Cognitive Matrix</h2>
                                <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mt-0.5">System-Wide Inference Analysis</p>
                            </div>
                        </div>
                        <div className="flex gap-4">
                            <div className="px-5 py-2.5 bg-slate-50 dark:bg-slate-950 rounded-xl border border-transparent dark:border-slate-850 text-[10px] font-black uppercase tracking-widest flex items-center gap-2">
                                <Zap className="h-3 w-3 text-primary-500" /> {aiBreakdown.totalCredits?.toLocaleString()} Tokens
                            </div>
                        </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8 relative z-10">
                        {[
                            { label: 'Total Inferences', value: aiBreakdown.totalCredits?.toLocaleString(), icon: Cpu, color: 'text-primary-500' },
                            { label: 'Cumulative Cost', value: `$${aiBreakdown.totalCost?.toFixed(2)}`, icon: DollarSign, color: 'text-emerald-500' },
                            { label: 'Accuracy Density', value: `${aiBreakdown.successRate?.toFixed(1)}%`, icon: ShieldCheck, color: 'text-blue-500' }
                        ].map((b, i) => (
                            <div key={i} className="flex items-center gap-6 p-6 bg-slate-50/50 dark:bg-slate-950/50 rounded-[32px] border border-transparent dark:border-slate-850 group hover:bg-white dark:hover:bg-slate-900 hover:shadow-xl transition-all">
                                <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-800 group-hover:scale-110 transition-transform">
                                    <b.icon className={cn("h-6 w-6", b.color)} />
                                </div>
                                <div>
                                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{b.label}</p>
                                    <p className="text-2xl font-black text-slate-900 dark:text-white tracking-tighter tabular-nums">{b.value}</p>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Feature Breakdown Corridor */}
                    {aiBreakdown.byFeature && Object.entries(aiBreakdown.byFeature).length > 0 && (
                        <div className="space-y-6 pt-10 border-t border-slate-50 dark:border-slate-850 relative z-10">
                            <h3 className="text-[10px] font-black text-slate-400 uppercase tracking-widest ml-1">Linguistic & Cognitive Distribution</h3>
                            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                                {Object.entries(aiBreakdown.byFeature).map(([feature, data]: [string, any]) => (
                                    <div key={feature} className="flex items-center justify-between p-6 bg-slate-50 dark:bg-slate-950/20 rounded-[24px] border border-transparent hover:border-slate-200 dark:hover:border-slate-800 transition-colors">
                                        <div className="flex items-center gap-4">
                                            <div className="w-1.5 h-6 bg-primary-500/20 rounded-full" />
                                            <div>
                                                <p className="text-[10px] font-bold text-slate-900 dark:text-white uppercase tracking-widest capitalize">{feature.replace('-', ' ')}</p>
                                                <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mt-1">{data.requests} REQUEST NODES</p>
                                            </div>
                                        </div>
                                        <div className="text-right">
                                            <p className="text-sm font-black text-slate-900 dark:text-white tabular-nums">${data.cost?.toFixed(2)}</p>
                                            <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mt-1">{data.credits?.toLocaleString()} TOKENS</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                    
                    <div className="absolute -bottom-20 -left-20 w-96 h-96 bg-primary-500/[0.03] blur-3xl rounded-full" />
                </div>
            )}

            {/* Strategic Roadmap (Guiding Section like in screenshot) */}
            <div className="p-10 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl relative overflow-hidden group">
                <div className="relative z-10 flex flex-col lg:flex-row items-center gap-10">
                    <div className="p-6 bg-slate-800 rounded-[32px] border border-slate-700 shadow-inner group-hover:rotate-6 transition-transform duration-1000">
                        <TrendingUp className="h-10 w-10 text-emerald-400" />
                    </div>
                    <div className="flex-1 space-y-4">
                        <h3 className="text-xl font-black text-white uppercase tracking-tight">Optimization Roadmap</h3>
                        <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest leading-relaxed">
                            Your resource consumption is currently <span className="text-emerald-400">OPTIMAL</span> across all system nodes. Increase throughput by upgrading to the <span className="text-primary-400 font-black underline cursor-pointer hover:text-white">ENTERPRISE CLUSTER</span> for unlimited data spatialization.
                        </p>
                        <div className="flex flex-wrap gap-4 pt-4">
                            <button className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-primary-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700 flex items-center gap-2">
                                <Plus className="h-4 w-4" /> Capacity Expansion
                            </button>
                            <button className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-slate-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700 flex items-center gap-2">
                                <ShieldCheck className="h-4 w-4" /> Usage Integrity Audit
                            </button>
                        </div>
                    </div>
                    <ChevronRight className="h-12 w-12 text-white/10 hidden lg:block group-hover:translate-x-2 transition-transform" />
                </div>
                <div className="absolute top-0 right-0 w-80 h-80 bg-emerald-500/5 blur-3xl rounded-full" />
            </div>
        </div>
    );
}

function UsageMetricCard({ icon, title, subtitle, used, limit, unit = '', color }: {
    icon: React.ReactNode;
    title: string;
    subtitle: string;
    used: number;
    limit: number;
    unit?: string;
    color: string;
}) {
    const percentage = Math.min((used / limit) * 100, 100) || 0;
    
    return (
        <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 p-10 shadow-2xl shadow-slate-200/40 dark:shadow-none group transition-all hover:shadow-primary-500/5">
            <div className="flex items-center gap-5 mb-10">
                <div className="p-4 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-transparent dark:border-slate-850 shadow-inner group-hover:scale-110 transition-transform">
                    {icon}
                </div>
                <div>
                    <h3 className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-tight">{title}</h3>
                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest mt-0.5">{subtitle}</p>
                </div>
            </div>
            
            <div className="space-y-4">
                <div className="flex items-end justify-between">
                    <span className="text-4xl font-black text-slate-900 dark:text-white tracking-tighter tabular-nums">{used.toLocaleString()}</span>
                    <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest pb-1.5">{unit} of {limit.toLocaleString()}</span>
                </div>
                <div className="h-3 w-full bg-slate-50 dark:bg-slate-950 rounded-full overflow-hidden shadow-inner">
                    <div 
                        className={cn("h-full bg-gradient-to-r rounded-full transition-all duration-1000", color)}
                        style={{ width: `${percentage}%` }}
                    />
                </div>
            </div>
        </div>
    );
}

