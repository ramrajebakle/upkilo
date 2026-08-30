"use client";

import { useState, useEffect } from 'react';
import { Ticket, Plus, Search, MoreVertical, X, Calendar, Hash, Percent, Award, Trash2 } from 'lucide-react';
import api from '@/lib/api';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';
import { ICoupon } from '@/types';
import { AxiosError } from 'axios';

export default function CouponsPage() {
    const t = useTranslations('Promo');
    const common = useTranslations('Common');
    
    const [coupons, setCoupons] = useState<ICoupon[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    
    const [formData, setFormData] = useState({
        code: '',
        discountType: 'Percentage' as const,
        discountValue: '',
        maxUses: '',
        validFrom: '',
        validUntil: '',
        minimumOrderAmount: ''
    });

    const fetchCoupons = async () => {
        try {
            setLoading(true);
            const res = await api.coupons.list({ search: searchQuery });
            setCoupons(res.data.data || []);
        } catch (err) {
            console.error('Failed to fetch coupons:', err);
            setCoupons([]);
            toast.error(t('issueError'));
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchCoupons();
    }, [searchQuery]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            setSubmitting(true);
            await api.coupons.create({
                code: formData.code.toUpperCase(),
                discountType: formData.discountType,
                discountValue: parseFloat(formData.discountValue),
                usageLimit: formData.maxUses ? parseInt(formData.maxUses, 10) : null,
                validFrom: formData.validFrom ? new Date(formData.validFrom).toISOString() : null,
                expiresAt: formData.validUntil ? new Date(formData.validUntil).toISOString() : null,
                minimumOrderAmount: formData.minimumOrderAmount ? parseFloat(formData.minimumOrderAmount) : 0
            });
            toast.success('Coupon created successfully');
            setIsModalOpen(false);
            setFormData({ 
                code: '', 
                discountType: 'Percentage', 
                discountValue: '', 
                maxUses: '', 
                validFrom: '', 
                validUntil: '',
                minimumOrderAmount: ''
            });
            fetchCoupons();
        } catch (err) {
            const error = err as AxiosError<{ message: string }>;
            console.error('Failed to create coupon:', error);
            toast.error(error.response?.data?.message || 'Failed to create coupon');
        } finally {
            setSubmitting(false);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this coupon?')) return;
        try {
            await api.coupons.delete(id);
            toast.success('Coupon deleted');
            fetchCoupons();
        } catch (err) {
            toast.error('Failed to delete coupon');
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    {/* Solid colour, not bg-clip-text over a gradient. A page title is the one
                        piece of text on the screen that must be unambiguous, and the gradient's
                        lighter stop set the effective contrast for part of the word. */}
                    <h1 className="text-2xl font-bold text-foreground dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>
                        {t('coupons')}
                    </h1>
                    <p className="text-sm text-foreground-secondary">{t('manageDescription')}</p>
                </div>
                <button 
                    className="flex items-center gap-2 px-4 py-2 bg-slate-900 text-white rounded-xl hover:bg-slate-800 transition-all shadow-sm hover:shadow-md active:scale-95"
                    onClick={() => setIsModalOpen(true)}
                >
                    <Plus className="h-4 w-4" />
                    <span>{t('newCoupon')}</span>
                </button>
            </div>

            <div className="bg-card rounded-2xl border border-border shadow-sm overflow-hidden">
                <div className="p-4 border-b border-border-subtle flex gap-4 bg-muted/30">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                        <input
                            type="text"
                            placeholder={t('searchPlaceholder')}
                            className="w-full pl-10 pr-4 py-2 bg-card border border-border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all"
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                        />
                    </div>
                </div>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-muted/50 text-foreground-secondary font-semibold uppercase tracking-wider text-[11px] border-b border-border-subtle">
                            <tr>
                                <th className="px-6 py-4">{t('code')}</th>
                                <th className="px-6 py-4">{t('discount')}</th>
                                <th className="px-6 py-4">{t('used')}</th>
                                <th className="px-6 py-4">{common('status')}</th>
                                <th className="px-6 py-4">{t('expires')}</th>
                                <th className="px-6 py-4 w-10"></th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border-subtle">
                            {loading && coupons.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center text-foreground-muted italic">
                                        <div className="flex flex-col items-center gap-2">
                                            <div className="h-5 w-5 border-2 border-primary-300 border-t-primary-600 rounded-full animate-spin"></div>
                                            <span>{common('loading')}</span>
                                        </div>
                                    </td>
                                </tr>
                            ) : coupons.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-16 text-center">
                                        <div className="w-16 h-16 bg-muted rounded-2xl flex items-center justify-center mx-auto mb-4 border border-border-subtle">
                                            <Ticket className="h-8 w-8 text-slate-300" />
                                        </div>
                                        <h3 className="text-base font-semibold text-foreground mb-1">{t('noCoupons')}</h3>
                                        <p className="text-sm text-foreground-secondary max-w-[200px] mx-auto">{t('noCouponsDesc')}</p>
                                    </td>
                                </tr>
                            ) : (
                                coupons.map((coupon) => (
                                    <tr key={coupon.id} className="group hover:bg-muted/50 transition-colors">
                                        <td className="px-6 py-4">
                                            <div className="flex items-center gap-2">
                                                <span className="px-2 py-1 bg-brand-subtle text-primary rounded-lg text-xs font-bold font-mono tracking-wider border border-primary/25 uppercase">
                                                    {coupon.code}
                                                </span>
                                            </div>
                                        </td>
                                        <td className="px-6 py-4">
                                            <div className="flex items-center gap-1.5 font-semibold text-foreground">
                                                {coupon.discountType === 'Percentage' ? <Percent className="h-3 w-3 text-foreground-muted" /> : <span className="text-foreground-muted font-normal">$</span>}
                                                {coupon.discountValue}
                                                {coupon.discountType === 'Percentage' && '%'}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4">
                                            <div className="flex items-center gap-2">
                                                <div className="w-16 h-1.5 bg-muted rounded-full overflow-hidden">
                                                    <div 
                                                        className="h-full bg-slate-400 rounded-full" 
                                                        style={{ width: `${coupon.usageLimit ? (coupon.timesUsed / coupon.usageLimit) * 100 : 0}%` }}
                                                    />
                                                </div>
                                                <span className="text-foreground-secondary tabular-nums">
                                                    {coupon.timesUsed || 0} / {coupon.usageLimit || '∞'}
                                                </span>
                                            </div>
                                        </td>
                                        <td className="px-6 py-4">
                                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                                                coupon.isActive && !coupon.isExpired
                                                    ? 'bg-emerald-50 text-emerald-700 border border-emerald-100' 
                                                    : 'bg-rose-50 text-rose-700 border border-rose-100'
                                            }`}>
                                                {coupon.isExpired ? t('statusExpired') : coupon.isActive ? t('statusActive') : t('statusInactive')}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 text-foreground-secondary">
                                            {coupon.expiresAt 
                                                ? new Date(coupon.expiresAt).toLocaleDateString() 
                                                : <span className="text-slate-300 italic">{t('never')}</span>}
                                        </td>
                                        <td className="px-6 py-4 text-right">
                                            <div className="flex justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                                <button 
                                                    onClick={() => handleDelete(coupon.id)}
                                                    className="p-2 text-foreground-muted hover:text-rose-600 rounded-lg hover:bg-rose-50 transition-all"
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </button>
                                                <button className="p-2 text-foreground-muted hover:text-foreground rounded-lg hover:bg-accent transition-all">
                                                    <MoreVertical className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-[2px] animate-in fade-in duration-200">
                    <div className="bg-card rounded-3xl shadow-2xl w-full max-w-lg overflow-hidden animate-in zoom-in-95 duration-200" onClick={(e) => e.stopPropagation()}>
                        <div className="flex justify-between items-center p-6 border-b border-border-subtle bg-muted/50">
                            <div className="flex items-center gap-3">
                                <div className="w-10 h-10 bg-primary-600 rounded-xl flex items-center justify-center shadow-lg shadow-primary-200">
                                    <Ticket className="h-5 w-5 text-white" />
                                </div>
                                <h2 className="text-xl font-bold text-foreground" style={{ fontFamily: 'var(--font-display)' }}>{t('createCoupon')}</h2>
                            </div>
                            <button onClick={() => setIsModalOpen(false)} className="w-8 h-8 rounded-full border border-border flex items-center justify-center text-foreground-muted hover:text-foreground-secondary hover:bg-card transition-all">
                                <X className="h-4 w-4" />
                            </button>
                        </div>
                        
                        <form onSubmit={handleSubmit} className="p-6 space-y-5">
                            <div>
                                <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                    <Hash className="h-3 w-3" />
                                    {t('code')}
                                </label>
                                <input
                                    type="text"
                                    required
                                    placeholder="e.g. SUMMER2024"
                                    className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all font-mono font-bold tracking-widest uppercase placeholder:font-sans placeholder:tracking-normal placeholder:font-normal"
                                    value={formData.code}
                                    onChange={(e) => setFormData({...formData, code: e.target.value.toUpperCase()})}
                                />
                            </div>
                            
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        {t('type')}
                                    </label>
                                    <select
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all"
                                        value={formData.discountType}
                                        onChange={(e) => setFormData({...formData, discountType: e.target.value as any})}
                                    >
                                        <option value="Percentage">{t('typePercentage')}</option>
                                        <option value="Fixed">{t('typeFixed')}</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        Value
                                    </label>
                                    <input
                                        type="number"
                                        required
                                        min="1"
                                        step={formData.discountType === 'Percentage' ? "1" : "0.01"}
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all font-bold"
                                        value={formData.discountValue}
                                        onChange={(e) => setFormData({...formData, discountValue: e.target.value})}
                                        placeholder={formData.discountType === 'Percentage' ? "20" : "15.00"}
                                    />
                                </div>
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        <Award className="h-3 w-3" />
                                        {t('maxUsage')}
                                    </label>
                                    <input
                                        type="number"
                                        min="1"
                                        placeholder={t('unlimited')}
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all"
                                        value={formData.maxUses}
                                        onChange={(e) => setFormData({...formData, maxUses: e.target.value})}
                                    />
                                </div>
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        {t('minOrder')}
                                    </label>
                                    <input
                                        type="number"
                                        min="0"
                                        placeholder={t('noMinimum')}
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all"
                                        value={formData.minimumOrderAmount}
                                        onChange={(e) => setFormData({...formData, minimumOrderAmount: e.target.value})}
                                    />
                                </div>
                            </div>
                            
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        <Calendar className="h-3 w-3" />
                                        {t('validFrom')}
                                    </label>
                                    <input
                                        type="date"
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all text-xs"
                                        value={formData.validFrom}
                                        onChange={(e) => setFormData({...formData, validFrom: e.target.value})}
                                    />
                                </div>
                                <div>
                                    <label className="flex items-center gap-2 text-[10px] font-bold text-foreground-secondary uppercase tracking-widest mb-2">
                                        <Calendar className="h-3 w-3" />
                                        Expiry Date
                                    </label>
                                    <input
                                        type="date"
                                        className="w-full px-4 py-3 bg-muted border border-border rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/10 focus:border-primary-500 transition-all text-xs"
                                        value={formData.validUntil}
                                        onChange={(e) => setFormData({...formData, validUntil: e.target.value})}
                                    />
                                </div>
                            </div>

                            <div className="pt-4 flex gap-3">
                                <button
                                    type="button"
                                    onClick={() => setIsModalOpen(false)}
                                    className="flex-1 px-4 py-3 text-sm font-semibold text-foreground bg-muted rounded-2xl hover:bg-slate-200 transition-all active:scale-95"
                                >
                                    {common('cancel')}
                                </button>
                                <button
                                    type="submit"
                                    disabled={submitting}
                                    className="flex-[2] py-3 bg-primary-600 text-white text-sm font-bold rounded-2xl hover:bg-primary-700 shadow-lg shadow-primary-200 transition-all active:scale-95 disabled:opacity-70 disabled:active:scale-100"
                                >
                                    {submitting ? t('creating') : t('createCoupon')}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
