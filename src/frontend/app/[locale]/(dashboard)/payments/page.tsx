'use client';

import { useState, useEffect } from 'react';
import {
    Search, Filter, Download, DollarSign, CreditCard, Check, X,
    Clock, ArrowUpRight, ArrowDownRight, Plus, MoreVertical,
    Eye, RefreshCcw, Wallet, TrendingUp, Receipt, ShieldCheck,
    Globe, Activity, PieChart, ChevronRight, Zap, Target,
    CreditCard as CardIcon, Loader2
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { Button } from '@/components/ui/Button';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';

interface Payment {
    id: string;
    clientName: string;
    clientInitials: string;
    serviceName: string;
    amount: number;
    status: 'completed' | 'pending' | 'failed' | 'refunded';
    method: string;
    methodIcon: 'card' | 'cash' | 'wallet';
    date: string;
    transactionId: string;
}

export default function PaymentsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [payments, setPayments] = useState<Payment[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState('all');

    useEffect(() => {
        fetchPayments();
    }, []);

    const fetchPayments = async () => {
        setLoading(true);
        try {
            const res = await api.payments.list();
            setPayments(res.data.data || res.data.payments || res.data || []);
        } catch (err) {
            console.error('Failed to fetch payments:', err);
            toastError('Failed to load financial matrix');
        } finally {
            setLoading(false);
        }
    };

    const filteredPayments = payments.filter(payment => {
        const matchesSearch = (payment.clientName || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
            (payment.transactionId || '').toLowerCase().includes(searchQuery.toLowerCase());
        const matchesStatus = statusFilter === 'all' || payment.status === statusFilter;
        return matchesSearch && matchesStatus;
    });

    // Stats
    const totalRevenue = payments.filter(p => p.status === 'completed').reduce((sum, p) => sum + p.amount, 0);
    const pendingAmount = payments.filter(p => p.status === 'pending').reduce((sum, p) => sum + p.amount, 0);
    const refundedAmount = payments.filter(p => p.status === 'refunded').reduce((sum, p) => sum + p.amount, 0);
    const completedCount = payments.filter(p => p.status === 'completed').length;

    const getStatusStyles = (status: string) => {
        switch (status) {
            case 'completed': return { bg: 'bg-emerald-50 dark:bg-emerald-500/10', text: 'text-emerald-600', icon: Check, border: 'border-emerald-100 dark:border-emerald-500/20' };
            case 'pending': return { bg: 'bg-amber-50 dark:bg-amber-500/10', text: 'text-amber-600', icon: Clock, border: 'border-amber-100 dark:border-amber-500/20' };
            case 'failed': return { bg: 'bg-rose-50 dark:bg-rose-500/10', text: 'text-rose-600', icon: X, border: 'border-rose-100 dark:border-rose-500/20' };
            case 'refunded': return { bg: 'bg-blue-50 dark:bg-blue-500/10', text: 'text-blue-600', icon: RefreshCcw, border: 'border-blue-100 dark:border-blue-500/20' };
            default: return { bg: 'bg-slate-50 dark:bg-slate-500/10', text: 'text-slate-600', icon: Clock, border: 'border-slate-100 dark:border-slate-500/20' };
        }
    };

    const getMethodIcon = (method: string) => {
        switch (method?.toLowerCase()) {
            case 'card': return CreditCard;
            case 'wallet': return Wallet;
            case 'cash': return DollarSign;
            default: return CreditCard;
        }
    };

    if (loading && payments.length === 0) return (
        <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
            <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
            <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Financial Matrix...</p>
        </div>
    );

    return (
        <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header Bundle */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-gradient-to-br from-emerald-600 to-teal-900 rounded-[28px] shadow-2xl shadow-emerald-500/20 border border-emerald-500/20">
                        <Receipt className="h-8 w-8 text-white" />
                    </div>
                    <div>
                        <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Financial Nexus</h1>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Global Revenue Tracking and Capitalization Corridor</p>
                    </div>
                </div>
                <div className="flex items-center gap-4">
                    <Button variant="outline" className="h-14 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] dark:bg-slate-900 dark:border-slate-800 shadow-xl active:scale-95 transition-all flex items-center gap-3">
                        <Download className="h-4 w-4" /> Export Protocol
                    </Button>
                    <Link href="/payments/split">
                        <Button className="h-14 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-emerald-500/20 active:scale-95 transition-all flex items-center gap-3 bg-emerald-600 hover:bg-emerald-700">
                            <Plus className="h-4 w-4" /> Initialize Split
                        </Button>
                    </Link>
                </div>
            </div>

            {/* Performance Stats Corridor */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
                {[
                    { label: 'Cumulative Revenue', value: formatCurrency(totalRevenue), icon: DollarSign, color: 'text-emerald-500', bg: 'bg-emerald-500/10', trend: '+14.2%' },
                    { label: 'Awaiting Uplink', value: formatCurrency(pendingAmount), icon: Clock, color: 'text-amber-500', bg: 'bg-amber-500/10', trend: 'Audit Req' },
                    { label: 'Resolved Refunds', value: formatCurrency(refundedAmount), icon: RefreshCcw, color: 'text-blue-500', bg: 'bg-blue-500/10', trend: 'Stable' },
                    { label: 'Protocol Yield', value: `${completedCount}/${payments.length}`, icon: TrendingUp, color: 'text-primary-500', bg: 'bg-primary-500/10', trend: '92%' }
                ].map((stat, i) => (
                    <div key={i} className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-4 group relative overflow-hidden">
                        <div className="relative z-10 flex items-center justify-between">
                            <div className={cn("p-4 rounded-2xl border", stat.bg, "border-transparent group-hover:scale-110 transition-transform")}>
                                <stat.icon className={cn("h-5 w-5", stat.color)} />
                            </div>
                            <span className="text-[9px] font-black text-emerald-500 uppercase tracking-widest">{stat.trend}</span>
                        </div>
                        <div className="relative z-10 space-y-1">
                            <p className="text-3xl font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{stat.value}</p>
                            <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">{stat.label}</p>
                        </div>
                        <div className="absolute top-0 right-0 w-24 h-24 bg-current opacity-[0.02] blur-2xl rounded-full" />
                    </div>
                ))}
            </div>

            {/* Matrix Filters Corridor */}
            <div className="flex flex-col lg:flex-row gap-6 items-center">
                <div className="relative flex-1 w-full group">
                    <Search className="absolute left-6 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 dark:text-slate-700 group-focus-within:text-emerald-500 transition-colors" />
                    <input
                        type="text"
                        placeholder="SEARCH TRANSACTION ALIAS OR CLIENT HASH..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="w-full h-16 pl-16 pr-6 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[28px] text-xs font-black uppercase tracking-widest dark:text-white outline-none focus:ring-4 focus:ring-emerald-500/10 focus:border-emerald-500 transition-all shadow-xl"
                    />
                </div>
                {/* flex-wrap: five status chips at px-6 each measured 488px against a 390px
                    viewport, and `flex` alone has nowhere to put the surplus, so the whole page
                    scrolled sideways. The table below was a red herring — it is already inside
                    an overflow-x-auto wrapper and never contributed to the page scrollWidth. */}
                <div className="p-1.5 bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-850 rounded-[28px] shadow-xl flex flex-wrap w-full lg:w-auto">
                    {['all', 'completed', 'pending', 'refunded', 'failed'].map((status) => (
                        <button
                            key={status}
                            onClick={() => setStatusFilter(status)}
                            className={cn(
                                'flex-1 lg:flex-none px-6 py-3 rounded-[24px] text-[9px] font-black uppercase tracking-widest transition-all duration-500',
                                statusFilter === status
                                    ? 'bg-white dark:bg-slate-900 text-emerald-600 dark:text-white shadow-xl scale-105 active:scale-95'
                                    : 'text-slate-400 dark:text-slate-600 hover:text-slate-900 dark:hover:text-slate-300'
                            )}
                        >
                            {status}
                        </button>
                    ))}
                </div>
            </div>

            {/* Transaction Matrix */}
            {/* min-w-0 on both: a scroll container only scrolls if it is allowed to be narrower
                than its content, and the default min-width:auto on a flex/grid item silently
                refuses that — the container grows instead and the whole page scrolls sideways
                (ux-guidelines #69, High). max-w-full pins it to the viewport regardless of what
                layout context this card is dropped into later. */}
            <div className="max-w-full min-w-0 bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/40 dark:shadow-none overflow-hidden xl:min-h-[500px]">
                <div className="overflow-x-auto max-w-full min-w-0">
                    <table className="w-full text-left">
                        <thead>
                            <tr className="border-b border-slate-50 dark:border-slate-850 bg-slate-50/50 dark:bg-slate-950/50">
                                <th className="px-10 py-6 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Originator / Hash</th>
                                <th className="px-10 py-6 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Service Vector</th>
                                <th className="px-10 py-6 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Capitalization</th>
                                <th className="px-10 py-6 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Protocol</th>
                                <th className="px-10 py-6 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Status</th>
                                <th className="px-10 py-6 text-right text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Audit</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-50 dark:divide-slate-850">
                            {filteredPayments.map((payment, i) => {
                                const status = getStatusStyles(payment.status);
                                const MethodIcon = getMethodIcon(payment.methodIcon);
                                
                                return (
                                    <tr key={payment.id} className="group hover:bg-slate-50/50 dark:hover:bg-slate-950/20 transition-all duration-300">
                                        <td className="px-10 py-6">
                                            <div className="flex items-center gap-6">
                                                <div className={cn(
                                                    "w-12 h-12 rounded-2xl flex items-center justify-center text-white font-black text-xs shadow-xl group-hover:scale-110 group-hover:rotate-3 transition-all",
                                                    payment.status === 'completed' ? 'bg-gradient-to-br from-emerald-500 to-teal-700' : 'bg-slate-800'
                                                )}>
                                                    {payment.clientInitials || payment.clientName?.substring(0, 2).toUpperCase()}
                                                </div>
                                                <div>
                                                    <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">{payment.clientName}</p>
                                                    <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 mt-0.5 tracking-tighter uppercase font-mono">{payment.transactionId || payment.id.substring(0, 12)}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-10 py-6">
                                            <p className="text-[10px] font-black text-slate-600 dark:text-slate-400 uppercase tracking-widest">{payment.serviceName || 'Global Services'}</p>
                                        </td>
                                        <td className="px-10 py-6">
                                            <p className={cn(
                                                "text-sm font-black tabular-nums tracking-tighter",
                                                payment.status === 'completed' ? 'text-emerald-500' : 'text-slate-900 dark:text-white'
                                            )}>
                                                {formatCurrency(payment.amount)}
                                            </p>
                                        </td>
                                        <td className="px-10 py-6">
                                            <div className="flex items-center gap-3 px-3 py-1.5 bg-slate-50 dark:bg-slate-950 rounded-xl border border-transparent dark:border-slate-850 w-fit">
                                                <MethodIcon className="h-3.5 w-3.5 text-slate-400" />
                                                <span className="text-[9px] font-black text-slate-500 dark:text-slate-500 uppercase tracking-widest">{payment.method || 'Unknown'}</span>
                                            </div>
                                        </td>
                                        <td className="px-10 py-6">
                                            <span className={cn(
                                                "inline-flex items-center gap-2 px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest border",
                                                status.bg, status.text, status.border
                                            )}>
                                                <status.icon className="h-3 w-3" />
                                                {payment.status}
                                            </span>
                                        </td>
                                        <td className="px-10 py-6 text-right">
                                            <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-all transform translate-x-4 group-hover:translate-x-0">
                                                <Link href={`/payments/invoices/${payment.id}`} className="p-3 hover:bg-white dark:hover:bg-slate-800 rounded-xl border border-transparent hover:border-slate-100 dark:hover:border-slate-700 shadow-sm transition-all">
                                                    <Eye className="h-4 w-4 text-slate-400 hover:text-primary-500" />
                                                </Link>
                                                <button className="p-3 hover:bg-white dark:hover:bg-slate-800 rounded-xl border border-transparent hover:border-slate-100 dark:hover:border-slate-700 shadow-sm transition-all">
                                                    <MoreVertical className="h-4 w-4 text-slate-400" />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>

                {filteredPayments.length === 0 && (
                    <div className="p-20 text-center">
                        <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-full inline-block mb-8 shadow-inner">
                            <Receipt className="h-12 w-12 text-slate-200 dark:text-slate-800" />
                        </div>
                        <h3 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Zero Transactions Logged</h3>
                        <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mt-2">The financial vault is currently at zero density.</p>
                    </div>
                )}
            </div>

            {/* Billing Integrity Protocol */}
            <div className="p-10 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl relative overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
                    <div className="p-6 bg-slate-800 rounded-[32px] border border-slate-700 shadow-inner group-hover:rotate-12 transition-transform duration-1000">
                        <ShieldCheck className="h-10 w-10 text-emerald-400" />
                    </div>
                    <div className="flex-1 space-y-4">
                        <h3 className="text-xl font-black text-white uppercase tracking-tight">Sec-Pay Protocol active</h3>
                        <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest leading-relaxed">
                            All transactions are processed via end-to-end encrypted tunnels. Fraud detection nodes are operational across all regional availability zones.
                        </p>
                        <div className="flex flex-wrap gap-4 pt-4">
                            <Button variant="outline" className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-emerald-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700">
                                <Activity className="h-4 w-4 mr-2" /> Live Security Feed
                            </Button>
                            <Button variant="outline" className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-slate-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700">
                                <Zap className="h-4 w-4 mr-2" /> Global DNS Map
                            </Button>
                        </div>
                    </div>
                </div>
                <div className="absolute top-0 right-0 w-80 h-80 bg-emerald-500/5 blur-3xl rounded-full" />
            </div>
        </div>
    );
}

