"use client";

import { useState, useEffect } from 'react';
import { Clock, Plus, Search, X } from 'lucide-react';
import { useTranslations } from 'next-intl';
import api from '@/lib/api';
import { toast } from 'sonner';
import { IWaitlistEntry, IBooking } from '@/types';
import { AxiosError } from 'axios';

export default function WaitlistPage() {
    const t = useTranslations('Waitlist');
    const common = useTranslations('Common');
    const [waitlist, setWaitlist] = useState<IWaitlistEntry[]>([]);
    const [services, setServices] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    const [stats, setStats] = useState({ total: 0, waiting: 0, notified: 0, converted: 0 });
    const [formData, setFormData] = useState({
        serviceId: '',
        email: '',
        firstName: '',
        lastName: '',
        phone: '',
        preferredDate: '',
        preferredTimeRange: 'Morning',
        notes: ''
    });

    const fetchData = async () => {
        try {
            setLoading(true);
            const [waitlistRes, servicesRes, statsRes] = await Promise.all([
                api.waitlist.list().catch(() => ({ data: { data: [] } })),
                api.services.list().catch(() => ({ data: { data: [] } })),
                api.waitlist.stats().catch(() => ({ data: { total: 0, waiting: 0, notified: 0, converted: 0 } }))
            ]);
            
            setWaitlist(waitlistRes.data?.data || []);
            setServices(servicesRes.data?.data || []);
            setStats(statsRes.data);
        } catch (err) {
            console.error('Failed to fetch data:', err);
            toast.error(t('issueError') || 'Failed to load waitlist data');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            setSubmitting(true);
            await api.waitlist.add({
                serviceId: formData.serviceId,
                email: formData.email,
                firstName: formData.firstName,
                lastName: formData.lastName,
                phone: formData.phone,
                preferredDate: formData.preferredDate ? new Date(formData.preferredDate).toISOString() : new Date().toISOString(),
                preferredTimeRange: formData.preferredTimeRange,
                notes: formData.notes
            });
            setIsModalOpen(false);
            setFormData({
                serviceId: '', email: '', firstName: '', lastName: '', phone: '', preferredDate: '', preferredTimeRange: 'Morning', notes: ''
            });
            fetchData();
            toast.success(t('addSuccess'));
        } catch (err) {
            console.error('Failed to add to waitlist:', err);
            toast.error(t('issueError'));
        } finally {
            setSubmitting(false);
        }
    };

    const handleNotify = async (id: string) => {
        try {
            await api.waitlist.notify(id);
            toast.success(t('notifySuccess'));
        } catch (err) {
            toast.error(t('issueError'));
        }
    };

    const handleConvert = async (id: string) => {
        try {
            const res = await api.waitlist.convert(id);
            if (res.data.success) {
                toast.success(t('convertSuccess'));
                fetchData();
            }
        } catch (err) {
            const error = err as AxiosError<{ error: string }>;
            toast.error(error.response?.data?.error || 'Failed to convert to booking');
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground" style={{ fontFamily: 'var(--font-display)' }}>
                        {t('title')}
                    </h1>
                    <p className="text-sm text-foreground-secondary">{t('manageDescription')}</p>
                </div>
                <button className="btn btn-primary" onClick={() => setIsModalOpen(true)}>
                    <Plus className="h-4 w-4" />
                    {t('addToWaitlist')}
                </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div className="card-elevated p-4">
                    <p className="text-sm text-foreground-secondary mb-1">{t('totalEntries')}</p>
                    <p className="text-2xl font-bold">{stats.total || 0}</p>
                </div>
                <div className="card-elevated p-4">
                    <p className="text-sm text-foreground-secondary mb-1">{t('currentlyWaiting')}</p>
                    <p className="text-2xl font-bold text-blue-600">{stats.waiting || 0}</p>
                </div>
                <div className="card-elevated p-4">
                    <p className="text-sm text-foreground-secondary mb-1">{t('notified')}</p>
                    <p className="text-2xl font-bold text-warning-fg">{stats.notified || 0}</p>
                </div>
                <div className="card-elevated p-4">
                    <p className="text-sm text-foreground-secondary mb-1">{t('converted')}</p>
                    <p className="text-2xl font-bold text-success-fg">{stats.converted || 0}</p>
                </div>
            </div>

            <div className="card-elevated">
                <div className="p-4 border-b border-border-subtle flex gap-4">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                        <input
                            type="text"
                            placeholder={t('searchPlaceholder')}
                            className="input-field pl-10 w-full"
                        />
                    </div>
                </div>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-muted text-foreground-secondary font-medium border-b border-border-subtle">
                            <tr>
                                <th className="px-6 py-4">{t('client')}</th>
                                <th className="px-6 py-4">{t('requestedService')}</th>
                                <th className="px-6 py-4">{t('preferredTime')}</th>
                                <th className="px-6 py-4">{t('addedOn')}</th>
                                <th className="px-6 py-4">{t('status')}</th>
                                <th className="px-6 py-4"></th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border-subtle">
                            {loading ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-8 text-center text-foreground-secondary">
                                        {common('loading')}
                                    </td>
                                </tr>
                            ) : waitlist.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center">
                                        <div className="w-12 h-12 bg-muted rounded-xl flex items-center justify-center mx-auto mb-3">
                                            <Clock className="h-6 w-6 text-foreground-muted" />
                                        </div>
                                        <h3 className="text-sm font-medium text-foreground mb-1">{t('emptyHeader')}</h3>
                                        <p className="text-sm text-foreground-secondary">{t('emptyDesc')}</p>
                                    </td>
                                </tr>
                            ) : (
                                waitlist.map((entry) => (
                                    <tr key={entry.id} className="hover:bg-muted/50 transition-colors">
                                        <td className="px-6 py-4 font-medium text-foreground">
                                            {entry.firstName} {entry.lastName}
                                            <div className="text-xs text-foreground-secondary font-normal">{entry.email}</div>
                                        </td>
                                        <td className="px-6 py-4">
                                            {services.find(s => s.id === entry.serviceId)?.name || 'Service'}
                                        </td>
                                        <td className="px-6 py-4 text-foreground-secondary">{entry.preferredTimeRange} on {new Date(entry.preferredDate).toLocaleDateString()}</td>
                                        <td className="px-6 py-4 text-foreground-secondary">
                                            {new Date(entry.createdAt).toLocaleDateString()}
                                        </td>
                                        <td className="px-6 py-4">
                                            <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${
                                                entry.isConverted || entry.status === 'Converted'
                                                ? 'bg-emerald-50 text-emerald-600' 
                                                : entry.status === 'Notified'
                                                ? 'bg-amber-50 text-amber-600'
                                                : 'bg-blue-50 text-blue-600'
                                            }`}>
                                                {entry.isConverted || entry.status === 'Converted' ? t('converted') : entry.status === 'Notified' ? t('notified') : t('currentlyWaiting')}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 text-right">
                                            <div className="flex justify-end gap-3">
                                                {entry.status === 'Notified' && !entry.isConverted && (
                                                    <button 
                                                        onClick={() => handleConvert(entry.id)}
                                                        className="text-success-fg hover:text-emerald-700 font-medium text-sm"
                                                    >
                                                        {t('convert')}
                                                    </button>
                                                )}
                                                <button 
                                                    onClick={() => handleNotify(entry.id)}
                                                    className="text-primary hover:text-primary font-medium text-sm"
                                                >
                                                    {t('notify')}
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
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
                    <div className="bg-card rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden max-h-[90vh] flex flex-col" onClick={(e) => e.stopPropagation()}>
                        <div className="flex justify-between items-center p-6 border-b border-border-subtle shrink-0">
                            <h2 className="text-xl font-bold text-foreground" style={{ fontFamily: 'var(--font-display)' }}>{t('addToWaitlist')}</h2>
                            <button onClick={() => setIsModalOpen(false)} className="text-foreground-muted hover:text-foreground-secondary transition-colors">
                                <X className="h-5 w-5" />
                            </button>
                        </div>
                        
                        <div className="flex-1 overflow-y-auto p-6">
                            <form id="waitlistForm" onSubmit={handleSubmit} className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-1">{t('service')} *</label>
                                    <select
                                        required
                                        className="input-field w-full"
                                        value={formData.serviceId}
                                        onChange={(e) => setFormData({...formData, serviceId: e.target.value})}
                                    >
                                        <option value="" disabled>{t('selectService')}</option>
                                        {services.map(s => (
                                            <option key={s.id} value={s.id}>{s.name} ({s.durationMinutes} min)</option>
                                        ))}
                                    </select>
                                </div>
                                
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('firstName')} *</label>
                                        <input
                                            type="text"
                                            required
                                            className="input-field w-full"
                                            value={formData.firstName}
                                            onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('lastName')} *</label>
                                        <input
                                            type="text"
                                            required
                                            className="input-field w-full"
                                            value={formData.lastName}
                                            onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                                        />
                                    </div>
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('email')} *</label>
                                        <input
                                            type="email"
                                            required
                                            className="input-field w-full"
                                            value={formData.email}
                                            onChange={(e) => setFormData({...formData, email: e.target.value})}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('phone')}</label>
                                        <input
                                            type="tel"
                                            className="input-field w-full"
                                            value={formData.phone}
                                            onChange={(e) => setFormData({...formData, phone: e.target.value})}
                                        />
                                    </div>
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('preferredDate')} *</label>
                                        <input
                                            type="date"
                                            required
                                            className="input-field w-full"
                                            value={formData.preferredDate}
                                            onChange={(e) => setFormData({...formData, preferredDate: e.target.value})}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-foreground mb-1">{t('preferredTimeDay')} *</label>
                                        <select
                                            required
                                            className="input-field w-full"
                                            value={formData.preferredTimeRange}
                                            onChange={(e) => setFormData({...formData, preferredTimeRange: e.target.value})}
                                        >
                                            <option value="Anytime">{t('anytime')}</option>
                                            <option value="Morning">{t('morning')}</option>
                                            <option value="Afternoon">{t('afternoon')}</option>
                                            <option value="Evening">{t('evening')}</option>
                                        </select>
                                    </div>
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-foreground mb-1">{t('notes')}</label>
                                    <textarea
                                        className="input-field w-full resize-none"
                                        rows={3}
                                        value={formData.notes}
                                        onChange={(e) => setFormData({...formData, notes: e.target.value})}
                                    />
                                </div>
                            </form>
                        </div>
                        
                        <div className="p-6 border-t border-border-subtle flex justify-end gap-3 shrink-0">
                            <button
                                type="button"
                                onClick={() => setIsModalOpen(false)}
                                className="px-4 py-2 text-sm font-medium text-foreground hover:text-foreground transition-colors"
                            >
                                {common('cancel')}
                            </button>
                            <button
                                type="submit"
                                form="waitlistForm"
                                disabled={submitting}
                                className="btn btn-primary"
                            >
                                {submitting ? t('adding') : t('addToWaitlist')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
