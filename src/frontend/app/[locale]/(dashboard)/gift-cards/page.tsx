"use client";

import { useState, useEffect } from 'react';
import { Gift, Plus, Search, MoreVertical, X, Calendar, Mail, User, MessageSquare } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import api from '@/lib/api';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export default function GiftCardsPage() {
    const t = useTranslations('Promo');
    const common = useTranslations('Common');
    const [giftCards, setGiftCards] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [actionMenuId, setActionMenuId] = useState<string | null>(null);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [reloadModal, setReloadModal] = useState<{ cardId: string } | null>(null);
    const [reloadAmount, setReloadAmount] = useState('');
    const [formData, setFormData] = useState({
        amount: '',
        recipientEmail: '',
        senderName: '',
        message: '',
        expiryDate: ''
    });

    const fetchGiftCards = async () => {
        try {
            setLoading(true);
            const res = await api.giftCards.list({ search: searchQuery });
            // The backend might return data in different structures, but we normalize to res.data.data
            setGiftCards(res.data?.data || res.data || []);
        } catch (err) {
            console.error('Failed to fetch gift cards:', err);
            setGiftCards([]);
            toast.error('Failed to load gift cards');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchGiftCards();
    }, [searchQuery]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            setSubmitting(true);
            await api.giftCards.create({
                amount: parseFloat(formData.amount),
                recipientEmail: formData.recipientEmail || null,
                senderName: formData.senderName || null,
                message: formData.message || null,
                expiryDate: formData.expiryDate ? new Date(formData.expiryDate).toISOString() : null
            });
            toast.success(t('issueSuccess'));
            setIsModalOpen(false);
            setFormData({ amount: '', recipientEmail: '', senderName: '', message: '', expiryDate: '' });
            fetchGiftCards();
        } catch (err: any) {
            console.error('Failed to issue gift card:', err);
            toast.error(err.response?.data?.message || t('issueError'));
        } finally {
            setSubmitting(false);
        }
    };

    const handleVoid = async (cardId: string) => {
        setActionLoading(cardId);
        setActionMenuId(null);
        try {
            await api.giftCards.void(cardId, {});
            toast.success('Gift card voided successfully');
            fetchGiftCards();
        } catch (err: any) {
            toast.error(err.response?.data?.message || 'Failed to void gift card');
        } finally {
            setActionLoading(null);
        }
    };

    const handleReload = async () => {
        if (!reloadModal) return;
        const amount = parseFloat(reloadAmount);
        if (!amount || amount <= 0) {
            toast.error('Enter a valid reload amount');
            return;
        }
        setActionLoading(reloadModal.cardId);
        try {
            await api.giftCards.reload(reloadModal.cardId, { amount });
            toast.success(`Gift card reloaded with ${formatCurrency(amount)}`);
            setReloadModal(null);
            setReloadAmount('');
            fetchGiftCards();
        } catch (err: any) {
            toast.error(err.response?.data?.message || 'Failed to reload gift card');
        } finally {
            setActionLoading(null);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h1 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-slate-900 to-slate-600" style={{ fontFamily: 'var(--font-display)' }}>
                        {t('giftCards')}
                    </h1>
                    <p className="text-sm text-slate-500">Manage digital and physical gift cards</p>
                </div>
                <button 
                    className="flex items-center gap-2 px-4 py-2 bg-slate-900 text-white rounded-xl hover:bg-slate-800 transition-all shadow-sm hover:shadow-md active:scale-95"
                    onClick={() => setIsModalOpen(true)}
                >
                    <Plus className="h-4 w-4" />
                    <span>{t('newGiftCard')}</span>
                </button>
            </div>

            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
                <div className="p-4 border-b border-slate-100 flex gap-4 bg-slate-50/30">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                        <input
                            type="text"
                            placeholder="Search by code or buyer name..."
                            className="w-full pl-10 pr-4 py-2 bg-white border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all"
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                        />
                    </div>
                </div>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-slate-50/50 text-slate-500 font-semibold uppercase tracking-wider text-[11px] border-b border-slate-100">
                            <tr>
                                <th className="px-6 py-4">{t('code')}</th>
                                <th className="px-6 py-4">{t('value')}</th>
                                <th className="px-6 py-4">{t('balance')}</th>
                                <th className="px-6 py-4">{t('status')}</th>
                                <th className="px-6 py-4">{t('expires')}</th>
                                <th className="px-6 py-4 w-10"></th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {loading && giftCards.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center text-slate-400 italic">
                                        <div className="flex flex-col items-center gap-2">
                                            <div className="h-5 w-5 border-2 border-slate-300 border-t-slate-900 rounded-full animate-spin"></div>
                                            <span>{common('loading')}</span>
                                        </div>
                                    </td>
                                </tr>
                            ) : giftCards.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-16 text-center">
                                        <div className="w-16 h-16 bg-slate-50 rounded-2xl flex items-center justify-center mx-auto mb-4 border border-slate-100">
                                            <Gift className="h-8 w-8 text-slate-300" />
                                        </div>
                                        <h3 className="text-base font-semibold text-slate-900 mb-1">No gift cards found</h3>
                                        <p className="text-sm text-slate-500 max-w-[200px] mx-auto">Create your first gift card to reward your customers.</p>
                                    </td>
                                </tr>
                            ) : (
                                giftCards.map((card) => (
                                    <tr key={card.id || card.code} className="group hover:bg-slate-50/50 transition-colors">
                                        <td className="px-6 py-4 font-mono font-medium text-slate-900 tracking-wider">
                                            {card.code}
                                        </td>
                                        <td className="px-6 py-4 text-slate-600 font-medium">
                                            {formatCurrency(card.initialAmount || card.initialValue || 0)}
                                        </td>
                                        <td className="px-6 py-4">
                                            <span className="font-semibold text-slate-900">
                                                {formatCurrency(card.remainingAmount || card.balance || 0)}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4">
                                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                                                card.status?.toLowerCase() === 'active' 
                                                    ? 'bg-emerald-50 text-emerald-700 border border-emerald-100' 
                                                    : 'bg-slate-100 text-slate-600 border border-slate-200'
                                            }`}>
                                                {card.status}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 text-slate-500">
                                            {card.expiryDate || card.expiresAt 
                                                ? new Date(card.expiryDate || card.expiresAt).toLocaleDateString() 
                                                : <span className="text-slate-300 italic">Never</span>}
                                        </td>
                                        <td className="px-6 py-4 text-right">
                                            <div className="relative inline-block">
                                                <button
                                                    disabled={actionLoading === (card.id || card.code)}
                                                    onClick={() => setActionMenuId(actionMenuId === (card.id || card.code) ? null : (card.id || card.code))}
                                                    className="p-2 text-slate-400 hover:text-slate-900 rounded-lg hover:bg-white border border-transparent hover:border-slate-200 transition-all opacity-0 group-hover:opacity-100 focus:opacity-100 disabled:opacity-50"
                                                >
                                                    {actionLoading === (card.id || card.code)
                                                        ? <div className="h-4 w-4 border-2 border-slate-300 border-t-slate-700 rounded-full animate-spin" />
                                                        : <MoreVertical className="h-4 w-4" />}
                                                </button>
                                                {actionMenuId === (card.id || card.code) && (
                                                    <div className="absolute right-0 top-full mt-1 z-20 bg-white border border-slate-200 rounded-xl shadow-lg py-1 w-36"
                                                         onBlur={() => setActionMenuId(null)}>
                                                        <button
                                                            onClick={() => { setReloadModal({ cardId: card.id || card.code }); setActionMenuId(null); }}
                                                            className="w-full text-left px-4 py-2 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
                                                        >
                                                            Reload Balance
                                                        </button>
                                                        {card.status?.toLowerCase() === 'active' && (
                                                            <button
                                                                onClick={() => handleVoid(card.id || card.code)}
                                                                className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
                                                            >
                                                                Void Card
                                                            </button>
                                                        )}
                                                    </div>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {reloadModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-[2px] animate-in fade-in duration-200"
                     onClick={() => setReloadModal(null)}>
                    <div className="bg-white rounded-3xl shadow-2xl w-full max-w-sm overflow-hidden animate-in zoom-in-95 duration-200"
                         onClick={(e) => e.stopPropagation()}>
                        <div className="flex justify-between items-center p-6 border-b border-slate-100 bg-slate-50/50">
                            <h2 className="text-lg font-bold text-slate-900">Reload Gift Card</h2>
                            <button onClick={() => setReloadModal(null)} className="w-8 h-8 rounded-full border border-slate-200 flex items-center justify-center text-slate-400 hover:text-slate-600 transition-all">
                                <X className="h-4 w-4" />
                            </button>
                        </div>
                        <div className="p-6 space-y-4">
                            <div>
                                <label className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2 block">
                                    Reload Amount ($)
                                </label>
                                <input
                                    type="number"
                                    min="1"
                                    step="0.01"
                                    autoFocus
                                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all font-semibold"
                                    value={reloadAmount}
                                    onChange={(e) => setReloadAmount(e.target.value)}
                                    placeholder="50.00"
                                    onKeyDown={(e) => e.key === 'Enter' && handleReload()}
                                />
                            </div>
                            <div className="flex gap-3 pt-2">
                                <button
                                    onClick={() => setReloadModal(null)}
                                    className="flex-1 px-4 py-2.5 text-sm font-semibold text-slate-700 bg-slate-100 rounded-xl hover:bg-slate-200 transition-all active:scale-95"
                                >
                                    Cancel
                                </button>
                                <button
                                    onClick={handleReload}
                                    disabled={actionLoading === reloadModal.cardId}
                                    className="flex-[2] btn btn-primary py-2.5 rounded-xl shadow-lg shadow-slate-900/10 active:scale-95 disabled:opacity-70"
                                >
                                    {actionLoading === reloadModal.cardId ? 'Reloading...' : 'Reload'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-[2px] animate-in fade-in duration-200">
                    <div className="bg-white rounded-3xl shadow-2xl w-full max-w-lg overflow-hidden animate-in zoom-in-95 duration-200" onClick={(e) => e.stopPropagation()}>
                        <div className="flex justify-between items-center p-6 border-b border-slate-100 bg-slate-50/50">
                            <div className="flex items-center gap-3">
                                <div className="w-10 h-10 bg-slate-900 rounded-xl flex items-center justify-center">
                                    <Gift className="h-5 w-5 text-white" />
                                </div>
                                <h2 className="text-xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>{t('issueGiftCard')}</h2>
                            </div>
                            <button onClick={() => setIsModalOpen(false)} className="w-8 h-8 rounded-full border border-slate-200 flex items-center justify-center text-slate-400 hover:text-slate-600 hover:bg-white transition-all">
                                <X className="h-4 w-4" />
                            </button>
                        </div>
                        
                        <form onSubmit={handleSubmit} className="p-6 space-y-5">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="col-span-1">
                                    <label className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                                        {t('amount')} ($)
                                    </label>
                                    <input
                                        type="number"
                                        min="1"
                                        step="0.01"
                                        required
                                        className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all font-semibold"
                                        value={formData.amount}
                                        onChange={(e) => setFormData({...formData, amount: e.target.value})}
                                        placeholder="50.00"
                                    />
                                </div>
                                <div className="col-span-1">
                                    <label className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                                        <Calendar className="h-3 w-3" />
                                        {t('expiryDate')}
                                    </label>
                                    <input
                                        type="date"
                                        className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all"
                                        value={formData.expiryDate}
                                        onChange={(e) => setFormData({...formData, expiryDate: e.target.value})}
                                    />
                                </div>
                            </div>
                            
                            <div className="grid grid-cols-2 gap-4">
                                <div className="col-span-1">
                                    <label className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                                        <Mail className="h-3 w-3" />
                                        {t('recipientEmail')}
                                    </label>
                                    <input
                                        type="email"
                                        className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all"
                                        value={formData.recipientEmail}
                                        onChange={(e) => setFormData({...formData, recipientEmail: e.target.value})}
                                        placeholder="customer@example.com"
                                    />
                                </div>
                                <div className="col-span-1">
                                    <label className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                                        <User className="h-3 w-3" />
                                        {t('senderName')}
                                    </label>
                                    <input
                                        type="text"
                                        className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all"
                                        value={formData.senderName}
                                        onChange={(e) => setFormData({...formData, senderName: e.target.value})}
                                        placeholder="Your Name"
                                    />
                                </div>
                            </div>
                            
                            <div>
                                <label className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                                    <MessageSquare className="h-3 w-3" />
                                    {t('message')}
                                </label>
                                <textarea
                                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-900/5 focus:border-slate-900 transition-all resize-none"
                                    rows={3}
                                    value={formData.message}
                                    onChange={(e) => setFormData({...formData, message: e.target.value})}
                                    placeholder="Happy Birthday!"
                                />
                            </div>

                            <div className="pt-4 flex gap-3">
                                <button
                                    type="button"
                                    onClick={() => setIsModalOpen(false)}
                                    className="flex-1 px-4 py-2.5 text-sm font-semibold text-slate-700 bg-slate-100 rounded-xl hover:bg-slate-200 transition-all active:scale-95"
                                >
                                    {common('cancel')}
                                </button>
                                <button
                                    type="submit"
                                    disabled={submitting}
                                    className="flex-[2] btn btn-primary py-2.5 rounded-xl shadow-lg shadow-slate-900/10 active:scale-95 disabled:opacity-70 disabled:active:scale-100"
                                >
                                    {submitting ? 'Issuing...' : t('issueGiftCard')}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
