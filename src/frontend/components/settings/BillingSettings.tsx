'use client';

import { useState, useEffect } from 'react';
import { 
    CreditCard, Check, Zap, Building2, ExternalLink, ShieldCheck, 
    Users, MapPin, AlertCircle, Loader2, Download, History 
} from 'lucide-react';
import api from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { UsageProgress } from '@/components/ui/UsageProgress';
import { cn, formatCurrency } from '@/lib/utils';

export function BillingSettings() {
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [plans, setPlans] = useState<any[]>([]);
    const [subscription, setSubscription] = useState<any>(null);
    const [usage, setUsage] = useState<any>(null);
    const [creditBalance, setCreditBalance] = useState<number>(0);
    const [invoices, setInvoices] = useState<any[]>([]);
    const [isAnnual, setIsAnnual] = useState(false);

    useEffect(() => {
        fetchBillingData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const fetchBillingData = async () => {
        setLoading(true);
        try {
            const [plansRes, subRes, usageRes, creditsRes, invoicesRes] = await Promise.all([
                api.billing.getPlans(),
                api.billing.getSubscription(),
                api.billing.getUsage(),
                api.credits.getBalance().catch(() => ({ data: 0 })),
                api.billing.getInvoices().catch(() => ({ data: { data: [] } }))
            ]);

            setPlans(plansRes.data.data || []);
            setSubscription(subRes.data);
            setUsage(usageRes.data.usage);
            setCreditBalance(typeof creditsRes?.data === 'number' ? creditsRes.data : 0);
            setInvoices(invoicesRes.data.data || []);

            if (subRes.data.interval === 'annual') {
                setIsAnnual(true);
            }
        } catch (err) {
            console.error('Failed to fetch billing data:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleUpgrade = async (planId: string) => {
        setActionLoading(planId);
        try {
            const res = await api.billing.createCheckout({
                planId: planId,
                isAnnual: isAnnual
            });

            if (res.data.success) {
                if (res.data.checkoutUrl) {
                    window.location.href = res.data.checkoutUrl;
                } else {
                    await fetchBillingData();
                }
            }
        } catch (err) {
            console.error('Upgrade error:', err);
        } finally {
            setActionLoading(null);
        }
    };

    const handleManageBilling = async () => {
        setActionLoading('portal');
        try {
            const res = await api.billing.createPortalSession(window.location.href);
            window.location.href = res.data.url;
        } catch (err) {
            console.error('Portal error:', err);
            setActionLoading(null);
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-foreground-secondary">Syncing Financial Ledger...</p>
            </div>
        );
    }

    return (
        <div className="space-y-12 pb-20 animate-fade-in">
            {/* Current Status */}
            <div className="flex flex-wrap items-center justify-between gap-8 p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none relative overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
                    <div className="w-20 h-20 bg-gradient-to-br from-primary-500 to-primary-600 rounded-[28px] flex items-center justify-center shadow-2xl shadow-primary-500/30 group-hover:scale-105 transition-transform duration-500">
                        <Zap className="h-10 w-10 text-white" />
                    </div>
                    <div>
                        <p className="text-[10px] text-primary-500 font-black uppercase tracking-[0.4em] mb-2">Operational Tier</p>
                        <h3 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">
                            {subscription?.plan?.name || 'Standard'} Protocol
                            {subscription?.status !== 'active' && subscription?.status !== 'none' && (
                                <span className="ml-4 px-4 py-1.5 text-[9px] font-black rounded-xl bg-amber-50 dark:bg-amber-400/10 text-amber-600 dark:text-amber-400 border border-amber-100 dark:border-amber-400/20 uppercase tracking-widest align-middle">
                                    {subscription?.status}
                                </span>
                            )}
                        </h3>
                        {subscription?.currentPeriodEnd && (
                            <p className="text-[10px] font-bold text-foreground-muted mt-2 uppercase tracking-[0.2em] flex items-center gap-2">
                                <History className="h-3 w-3" /> Next Synchronization: {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
                            </p>
                        )}
                    </div>
                </div>
                
                <div className="relative z-10">
                    <Button 
                        onClick={handleManageBilling} 
                        loading={actionLoading === 'portal'} 
                        className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl hover:translate-y-[-2px] transition-all flex items-center gap-3"
                    >
                        Security Portal
                        <ExternalLink className="h-4 w-4" />
                    </Button>
                </div>
                
                <div className="absolute top-0 right-0 w-80 h-80 bg-primary-500/5 dark:bg-primary-500/10 rounded-full blur-3xl" />
            </div>

            {/* Credit Matrix */}
            <div className="p-10 bg-gradient-to-br from-primary-950 to-slate-900 border border-slate-800 rounded-[40px] text-white shadow-2xl relative overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row items-center justify-between gap-10">
                    <div className="flex items-center gap-10">
                        <div className="w-24 h-24 bg-white/5 rounded-[32px] flex items-center justify-center backdrop-blur-2xl border border-white/10 shadow-inner group-hover:rotate-6 transition-transform">
                            <CreditCard className="h-10 w-10 text-primary-400" />
                        </div>
                        <div>
                            <p className="text-[10px] text-primary-400 font-black uppercase tracking-[0.4em] mb-2">Financial Liquidity</p>
                            <h3 className="text-5xl font-black tracking-tighter text-white">
                                {formatCurrency(creditBalance)}
                            </h3>
                            <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-2">Verified Ledger Balance</p>
                        </div>
                    </div>
                    <Button className="h-14 bg-white text-primary-950 hover:bg-slate-100 font-black px-12 rounded-2xl border-none shadow-xl uppercase tracking-widest text-[10px] hover:scale-105 transition-all active:scale-95">
                        Inject Liquidity
                    </Button>
                </div>
                <div className="absolute top-0 right-0 w-96 h-96 bg-primary-500/10 blur-3xl -mr-48 -mt-48 pointer-events-none" />
            </div>

            {/* Telemetry Matrix */}
            <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
                {[
                    { label: 'Agent Spectrum', used: usage?.staffCount || 0, limit: usage?.staffLimit || 0, icon: Users, color: 'text-primary-500', bg: 'bg-primary-500/10', border: 'border-primary-500/20', format: 'number' as const },
                    { label: 'Origin Points', used: usage?.locationCount || 0, limit: usage?.locationLimit || 0, icon: MapPin, color: 'text-primary-500', bg: 'bg-primary-500/10', border: 'border-primary-500/20', format: 'number' as const },
                    { label: 'Event Volume', used: usage?.bookingsUsed || 0, limit: usage?.bookingsLimit || 0, icon: Zap, color: 'text-success-fg', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20', format: 'number' as const },
                    { label: 'AI Consumption', used: usage?.aiCostUsed || 0, limit: usage?.aiCostLimit || 0, icon: Zap, color: 'text-warning-fg', bg: 'bg-amber-500/10', border: 'border-amber-500/20', format: 'currency' as const }
                ].map((item, i) => (
                    <div key={i} className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-6 group hover:border-primary-500/30 transition-all">
                        <div className="flex items-center gap-4">
                            <div className={cn("p-4 rounded-2xl border", item.bg, item.border)}>
                                <item.icon className={cn("h-6 w-6", item.color)} />
                            </div>
                            <h4 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">{item.label}</h4>
                        </div>
                        <UsageProgress
                            label=""
                            used={item.used}
                            limit={item.limit}
                            format={item.format}
                        />
                        <p className="text-[9px] font-black text-foreground-muted uppercase tracking-widest text-center">Live Feedback</p>
                    </div>
                ))}
            </div>

            {/* Cycle Selector */}
            <div className="flex justify-center">
                <div className="p-1.5 bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-850 rounded-[28px] shadow-inner flex">
                    <button
                        onClick={() => setIsAnnual(false)}
                        className={cn(
                            'px-10 py-3.5 rounded-[24px] text-[10px] font-black uppercase tracking-widest transition-all duration-500',
                            !isAnnual ? 'bg-white dark:bg-slate-900 shadow-xl text-primary-600 dark:text-primary-400' : 'text-foreground-secondary hover:text-slate-900 dark:hover:text-slate-300'
                        )}
                    >
                        Monthly Sync
                    </button>
                    <button
                        onClick={() => setIsAnnual(true)}
                        className={cn(
                            'px-10 py-3.5 rounded-[24px] text-[10px] font-black uppercase tracking-widest transition-all duration-500 flex items-center gap-3',
                            isAnnual ? 'bg-white dark:bg-slate-900 shadow-xl text-primary-600 dark:text-primary-400' : 'text-foreground-secondary hover:text-slate-900 dark:hover:text-slate-300'
                        )}
                    >
                        Annual Sync <span className="px-2 py-0.5 bg-emerald-500 text-white rounded-lg text-[8px] font-black uppercase tracking-widest shadow-lg shadow-emerald-500/30">-17%</span>
                    </button>
                </div>
            </div>

            {/* Tier Schema */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
                {plans.map((plan) => (
                    <div
                        key={plan.id}
                        className={cn(
                            "relative flex flex-col p-10 rounded-[40px] border transition-all duration-700 hover:shadow-2xl hover:-translate-y-2",
                            subscription?.plan?.id === plan.id
                                ? "border-primary-500/50 dark:border-primary-500/50 bg-primary-50/10 dark:bg-primary-500/5 ring-8 ring-primary-500/[0.03]"
                                : "border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900"
                        )}
                    >
                        {subscription?.plan?.id === plan.id && (
                            <div className="absolute top-6 right-6 p-2 bg-primary-500 text-white rounded-xl shadow-xl shadow-primary-500/40">
                                <ShieldCheck className="h-4 w-4" />
                            </div>
                        )}
                        <div className="mb-10">
                            <h4 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">{plan.name}</h4>
                            <div className="mt-8 flex items-baseline gap-2">
                                <span className="text-4xl font-black text-slate-900 dark:text-white tracking-tighter shadow-glow-sm">
                                    {formatCurrency(isAnnual ? plan.annualPrice / 12 : plan.monthlyPrice)}
                                </span>
                                <span className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">/ NODE</span>
                            </div>
                        </div>

                        <div className="space-y-5 mb-12 flex-1 pt-8 border-t border-slate-50 dark:border-slate-850">
                            <p className="text-[9px] font-black text-foreground-muted uppercase tracking-[0.3em] mb-4">Functional Matrix</p>
                            {plan.features && Object.entries(plan.features).map(([key, value]) => {
                                if (typeof value === 'boolean' && value) {
                                    return (
                                        <div key={key} className="flex items-start gap-4 group/feat">
                                            <div className="w-5 h-5 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800 flex items-center justify-center shrink-0 mt-0.5 group-hover/feat:border-primary-500/50 group-hover/feat:bg-primary-50 transition-all">
                                                <Check className="h-3 w-3 text-primary-500 opacity-0 group-hover/feat:opacity-100 transition-opacity" />
                                            </div>
                                            <span className="text-[10px] font-bold text-foreground-secondary uppercase tracking-widest leading-loose group-hover/feat:text-slate-900 dark:group-hover/feat:text-white transition-colors">
                                                {key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}
                                            </span>
                                        </div>
                                    );
                                }
                                return null;
                            })}
                        </div>

                        <Button
                            className={cn(
                                "h-14 w-full rounded-2xl font-black uppercase tracking-widest text-[10px] transition-all duration-500",
                                plan.id === subscription?.plan?.id 
                                    ? "bg-slate-100 dark:bg-slate-800 text-foreground-muted cursor-not-allowed border-none"
                                    : "shadow-2xl shadow-primary-500/20 active:scale-[0.98] hover:scale-105"
                            )}
                            variant={plan.id === subscription?.plan?.id ? "outline" : "primary"}
                            disabled={plan.id === subscription?.plan?.id || actionLoading === plan.id}
                            onClick={() => handleUpgrade(plan.id)}
                            loading={actionLoading === plan.id}
                        >
                            {plan.id === subscription?.plan?.id ? 'Locked Segment' : 'initialise tier'}
                        </Button>
                    </div>
                ))}
            </div>

            {/* Financial Ledger */}
            <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/40 dark:shadow-none overflow-hidden">
                <div className="p-10 border-b border-slate-50 dark:border-slate-850 flex items-center justify-between">
                    <div className="flex items-center gap-6">
                        <div className="p-4 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-100 dark:border-slate-850 shadow-inner">
                            <History className="h-6 w-6 text-foreground-muted" />
                        </div>
                        <div>
                            <h4 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Financial Timeline</h4>
                            <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-2">Immutable Transaction Logs</p>
                        </div>
                    </div>
                </div>
                
                <div className="overflow-x-auto overflow-y-hidden">
                    <table className="w-full text-left">
                        <thead>
                            <tr className="bg-slate-50/50 dark:bg-slate-950/20">
                                <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Transaction</th>
                                <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Timestamp</th>
                                <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Allocation</th>
                                <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Stance</th>
                                <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em] text-right">Artifact</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-50 dark:divide-slate-850">
                            {invoices.length === 0 ? (
                                <tr>
                                    <td colSpan={5} className="px-10 py-24 text-center">
                                        <div className="p-6 bg-slate-50 dark:bg-slate-950 rounded-full inline-block mb-6 shadow-inner">
                                            <History className="h-10 w-10 text-slate-200" />
                                        </div>
                                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">No financial artifacts found in timeline</p>
                                    </td>
                                </tr>
                            ) : (
                                invoices.map((inv) => (
                                    <tr key={inv.id} className="group hover:bg-slate-50/50 dark:hover:bg-primary-900/[0.03] transition-all">
                                        <td className="px-10 py-8">
                                            <p className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest group-hover:text-primary-500 transition-colors">{inv.invoiceNumber}</p>
                                        </td>
                                        <td className="px-10 py-8">
                                            <p className="text-[10px] font-bold text-foreground-secondary uppercase tracking-widest">{new Date(inv.issueDate).toLocaleDateString()}</p>
                                        </td>
                                        <td className="px-10 py-8">
                                            <p className="text-xs font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{formatCurrency(inv.totalAmount)}</p>
                                        </td>
                                        <td className="px-10 py-8">
                                            <span className={cn(
                                                "px-4 py-1.5 text-[9px] font-black rounded-lg uppercase tracking-widest border shadow-sm",
                                                inv.status === 'Paid' 
                                                    ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20" 
                                                    : "bg-amber-50 dark:bg-amber-400/10 text-amber-600 dark:text-amber-400 border-amber-100 dark:border-amber-400/20"
                                            )}>
                                                {inv.status}
                                            </span>
                                        </td>
                                        <td className="px-10 py-8 text-right">
                                            {inv.pdfUrl && (
                                                <a 
                                                    href={inv.pdfUrl} 
                                                    target="_blank" 
                                                    rel="noopener noreferrer"
                                                    className="inline-flex items-center gap-3 h-12 px-5 bg-white dark:bg-slate-800 hover:bg-primary-600 dark:hover:bg-primary-500 text-foreground-muted hover:text-white transition-all border border-slate-100 dark:border-slate-700 rounded-xl shadow-sm active:scale-95"
                                                >
                                                    <Download className="h-4 w-4" />
                                                    <span className="text-[8px] font-black uppercase tracking-widest">Download</span>
                                                </a>
                                            )}
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}

