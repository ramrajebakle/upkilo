'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    CreditCard,
    DollarSign,
    Wallet,
    Plus,
    Minus,
    Calculator,
    CheckCircle2,
    Users,
    Receipt,
    Info,
    ChevronRight,
    Search,
    AlertCircle,
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

interface SplitItem {
    id: string;
    method: 'card' | 'cash' | 'wallet';
    amount: number;
    label: string;
}

export default function SplitPaymentPage() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const bookingId = searchParams.get('bookingId');
    const { success: toastSuccess, error: toastError } = useToast();
    
    const [loading, setLoading] = useState(false);
    const [totalAmount, setTotalAmount] = useState(0);
    const [booking, setBooking] = useState<any>(null);
    const [splits, setSplits] = useState<SplitItem[]>([
        { id: '1', method: 'card', amount: 0, label: 'Payment 1' }
    ]);

    useEffect(() => {
        const fetchBooking = async () => {
            if (!bookingId) return;
            try {
                const res = await api.bookings.get(bookingId);
                setBooking(res.data);
                setTotalAmount(res.data.totalPrice || 0);
                setSplits([{ id: '1', method: 'card', amount: res.data.totalPrice || 0, label: 'Full Payment' }]);
            } catch (error) {
                console.error('Failed to fetch booking', error);
                toastError('Failed to load booking details');
            }
        };
        fetchBooking();
    }, [bookingId, toastError]);

    const addSplit = () => {
        const newSplit: SplitItem = {
            id: Math.random().toString(36).substr(2, 9),
            method: 'card',
            amount: 0,
            label: `Payment ${splits.length + 1}`
        };
        setSplits([...splits, newSplit]);
    };

    const removeSplit = (id: string) => {
        if (splits.length === 1) return;
        setSplits(splits.filter(s => s.id !== id));
    };

    const updateSplit = (id: string, updates: Partial<SplitItem>) => {
        setSplits(splits.map(s => s.id === id ? { ...s, ...updates } : s));
    };

    const currentTotal = splits.reduce((sum, s) => sum + s.amount, 0);
    const remaining = totalAmount - currentTotal;
    const isValid = Math.abs(remaining) < 0.01 && splits.every(s => s.amount > 0);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!isValid) return;
        
        setLoading(true);
        try {
            await api.payments.split(bookingId || 'new', splits);
            toastSuccess('Split payment processed successfully');
            router.push('/payments?success=true');
        } catch (error) {
            console.error('Failed to process split payment', error);
            toastError('Failed to process split payment');
        } finally {
            setLoading(false);
        }
    };

    const distributeEvenly = () => {
        const amountPerSplit = totalAmount / splits.length;
        setSplits(splits.map(s => ({ ...s, amount: parseFloat(amountPerSplit.toFixed(2)) })));
    };

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/payments"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl shadow-lg shadow-indigo-500/25">
                            <Users className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'Outfit, sans-serif' }}
                        >
                            Split Payment
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Split total amount between multiple methods or people</p>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Left Side: Split Controls */}
                <div className="lg:col-span-2 space-y-6">
                    <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                        <div className="flex items-center justify-between mb-6">
                            <h2 className="text-lg font-semibold text-slate-900 flex items-center gap-2">
                                <Receipt className="h-5 w-5 text-indigo-500" />
                                Payment Splits
                            </h2>
                            <button
                                onClick={distributeEvenly}
                                className="text-xs font-semibold text-indigo-600 hover:text-indigo-700 bg-indigo-50 px-3 py-1.5 rounded-lg transition-colors"
                            >
                                Distribute Evenly
                            </button>
                        </div>

                        <div className="space-y-4">
                            {splits.map((split, index) => (
                                <div 
                                    key={split.id}
                                    className="p-4 bg-slate-50 border border-slate-100 rounded-2xl flex flex-col md:flex-row gap-4 items-center group animate-fade-in-up"
                                    style={{ animationDelay: `${index * 50}ms` }}
                                >
                                    <div className="flex-1 w-full">
                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1 ml-1">Label</label>
                                        <input
                                            type="text"
                                            value={split.label}
                                            onChange={(e) => updateSplit(split.id, { label: e.target.value })}
                                            className="w-full bg-white border-transparent focus:border-indigo-500 rounded-xl px-4 py-2 text-sm shadow-sm"
                                        />
                                    </div>
                                    <div className="w-full md:w-32">
                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1 ml-1">Method</label>
                                        <select
                                            value={split.method}
                                            onChange={(e) => updateSplit(split.id, { method: e.target.value as any })}
                                            className="w-full bg-white border-transparent focus:border-indigo-500 rounded-xl px-3 py-2 text-sm shadow-sm appearance-none"
                                        >
                                            <option value="card">Card</option>
                                            <option value="cash">Cash</option>
                                            <option value="wallet">Wallet</option>
                                        </select>
                                    </div>
                                    <div className="w-full md:w-40 relative">
                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1 ml-1">Amount</label>
                                        <div className="relative">
                                            <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
                                            <input
                                                type="number"
                                                step="0.01"
                                                value={split.amount || ''}
                                                onChange={(e) => updateSplit(split.id, { amount: parseFloat(e.target.value) || 0 })}
                                                className="w-full bg-white border-transparent focus:border-indigo-500 rounded-xl pl-9 pr-4 py-2 text-sm font-bold text-slate-900 shadow-sm"
                                                placeholder="0.00"
                                            />
                                        </div>
                                    </div>
                                    <button
                                        onClick={() => removeSplit(split.id)}
                                        disabled={splits.length === 1}
                                        className="p-2.5 mt-4 md:mt-0 bg-white hover:bg-red-50 text-slate-300 hover:text-red-500 rounded-xl shadow-sm border border-slate-100 transition-all disabled:opacity-30 self-end md:self-center"
                                    >
                                        <Minus className="h-4 w-4" />
                                    </button>
                                </div>
                            ))}

                            <button
                                onClick={addSplit}
                                className="w-full py-4 border-2 border-dashed border-slate-200 rounded-2xl text-slate-400 hover:text-indigo-500 hover:border-indigo-200 hover:bg-white transition-all flex items-center justify-center gap-2 group"
                            >
                                <Plus className="h-5 w-5 group-hover:scale-110 transition-transform" />
                                <span className="font-semibold uppercase tracking-widest text-xs">Add Payment Split</span>
                            </button>
                        </div>
                    </div>

                    {!bookingId && (
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Search className="h-5 w-5 text-blue-500" />
                                Select Transaction
                            </h2>
                            <div className="relative">
                                <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                <input
                                    type="text"
                                    className="input pl-11"
                                    placeholder="Search by client or booking ID..."
                                />
                            </div>
                            <div className="mt-4 p-4 border border-dashed border-slate-200 rounded-xl text-center text-sm text-slate-400">
                                Search for a pending booking to split its payment
                            </div>
                        </div>
                    )}
                </div>

                {/* Right Side: Summary & Actions */}
                <div className="space-y-6">
                    <div className="card-elevated p-6 animate-fade-in-up bg-slate-900 text-white shadow-2xl shadow-slate-900/20" style={{ animationDelay: '200ms' }}>
                        <h2 className="text-sm font-bold uppercase tracking-widest text-slate-400 mb-6">Summary</h2>
                        
                        <div className="space-y-4">
                            <div className="flex justify-between items-center text-slate-400 text-sm">
                                <span>Total Bill</span>
                                <span className="font-mono text-white text-base">{formatCurrency(totalAmount)}</span>
                            </div>
                            <div className="flex justify-between items-center text-slate-400 text-sm">
                                <span>Split Total</span>
                                <span className={cn(
                                    "font-mono text-base",
                                    isValid ? "text-emerald-400" : "text-amber-400"
                                )}>{formatCurrency(currentTotal)}</span>
                            </div>
                            
                            <div className="h-px bg-slate-800 my-2" />
                            
                            <div className="flex justify-between items-center">
                                <span className="text-sm font-bold text-slate-300">Remaining</span>
                                <div className="text-right">
                                    <p className={cn(
                                        "text-2xl font-bold font-mono",
                                        remaining === 0 ? "text-emerald-400" : remaining > 0 ? "text-amber-400" : "text-red-400"
                                    )}>
                                        {formatCurrency(remaining)}
                                    </p>
                                    {remaining !== 0 && (
                                        <p className="text-[10px] text-slate-500 uppercase tracking-wider font-bold">
                                            {remaining > 0 ? "Underpaid" : "Overpaid"}
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="mt-8 space-y-3">
                            <button
                                onClick={handleSubmit}
                                disabled={!isValid || loading}
                                className={cn(
                                    "w-full py-4 rounded-xl font-bold transition-all flex items-center justify-center gap-2",
                                    isValid && !loading
                                        ? "bg-gradient-to-r from-emerald-500 to-teal-600 text-white shadow-lg shadow-emerald-500/25 hover:scale-[1.02] active:scale-95"
                                        : "bg-slate-800 text-slate-500 cursor-not-allowed"
                                )}
                            >
                                {loading ? (
                                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                ) : (
                                    <>
                                        <CheckCircle2 className="h-5 w-5" />
                                        Process Split Payment
                                    </>
                                )}
                            </button>
                            
                            {!isValid && remaining !== 0 && (
                                <div className="flex items-start gap-2 p-3 bg-amber-500/10 rounded-xl border border-amber-500/20 text-amber-500 text-xs">
                                    <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" />
                                    <p>The total of all splits must exactly match the bill amount ({formatCurrency(totalAmount)}).</p>
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                        <h2 className="text-sm font-bold text-slate-900 mb-4 flex items-center gap-2 uppercase tracking-wider">
                            <Info className="h-4 w-4 text-indigo-500" />
                            Split Instructions
                        </h2>
                        <ul className="text-xs text-slate-500 space-y-3 leading-relaxed">
                            <li className="flex gap-2">
                                <div className="w-4 h-4 rounded-full bg-slate-100 flex items-center justify-center text-[8px] font-bold text-slate-400 flex-shrink-0">1</div>
                                Add a new split for each person or payment method.
                            </li>
                            <li className="flex gap-2">
                                <div className="w-4 h-4 rounded-full bg-slate-100 flex items-center justify-center text-[8px] font-bold text-slate-400 flex-shrink-0">2</div>
                                Assign amounts to each split.
                            </li>
                            <li className="flex gap-2">
                                <div className="w-4 h-4 rounded-full bg-slate-100 flex items-center justify-center text-[8px] font-bold text-slate-400 flex-shrink-0">3</div>
                                Ensure the "Remaining" balance is zero.
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
}
