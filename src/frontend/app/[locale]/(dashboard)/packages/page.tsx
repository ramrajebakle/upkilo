"use client";

import { useState, useEffect } from 'react';
import { Package, Plus, Search, MoreVertical, X, XCircle } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import { useTranslations } from 'next-intl';
import api from '@/lib/api';
import { toast } from 'sonner';
import { IServicePackage, IService } from '@/types';

export default function PackagesPage() {
    const t = useTranslations('Packages');
    const [packages, setPackages] = useState<IServicePackage[]>([]);
    const [services, setServices] = useState<IService[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    const [formData, setFormData] = useState({
        name: '',
        description: '',
        price: '',
        originalPrice: '',
        validityDays: '90',
        services: [] as { serviceId: string, quantity: number }[],
        selectedServiceId: '',
        selectedServiceQuantity: '1'
    });

    const fetchData = async () => {
        try {
            setLoading(true);
            const [pkgRes, srvRes] = await Promise.all([
                api.packages.list().catch(() => ({ data: { data: [] } as any })),
                api.services.list().catch(() => ({ data: { data: [] } as any }))
            ]);
            setPackages(pkgRes.data?.data || []);
            setServices(srvRes.data?.data || []);
        } catch (err) {
            console.error('Failed to fetch data:', err);
            toast.error(t('fetchError'));
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleAddService = () => {
        if (!formData.selectedServiceId) return;
        const exists = formData.services.find(s => s.serviceId === formData.selectedServiceId);
        if (exists) {
            setFormData(prev => ({
                ...prev,
                services: prev.services.map(s => 
                    s.serviceId === formData.selectedServiceId 
                        ? { ...s, quantity: s.quantity + parseInt(formData.selectedServiceQuantity) }
                        : s
                )
            }));
        } else {
            setFormData(prev => ({
                ...prev,
                services: [...prev.services, { 
                    serviceId: formData.selectedServiceId, 
                    quantity: parseInt(formData.selectedServiceQuantity) 
                }]
            }));
        }
    };

    const handleRemoveService = (serviceId: string) => {
        setFormData(prev => ({
            ...prev,
            services: prev.services.filter(s => s.serviceId !== serviceId)
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (formData.services.length === 0) {
            toast.error(t('noServicesError'));
            return;
        }

        try {
            setSubmitting(true);
            await api.packages.create({
                name: formData.name,
                description: formData.description,
                price: parseFloat(formData.price),
                originalPrice: formData.originalPrice ? parseFloat(formData.originalPrice) : parseFloat(formData.price),
                validityDays: parseInt(formData.validityDays, 10),
                services: formData.services
            } as any);
            setIsModalOpen(false);
            setFormData({
                name: '', description: '', price: '', originalPrice: '', validityDays: '90',
                services: [], selectedServiceId: '', selectedServiceQuantity: '1'
            });
            fetchData();
            toast.success(t('createSuccess'));
        } catch (err) {
            console.error('Failed to create package:', err);
            toast.error(t('createError'));
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>
                        {t('title')}
                    </h1>
                    <p className="text-sm text-slate-500">{t('description')}</p>
                </div>
                <button className="btn btn-primary" onClick={() => setIsModalOpen(true)}>
                    <Plus className="h-4 w-4" />
                    {t('newPackage')}
                </button>
            </div>

            <div className="card-elevated">
                <div className="p-4 border-b border-slate-100 flex gap-4">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                        <input
                            type="text"
                            placeholder={t('searchPlaceholder')}
                            className="input-field pl-10 w-full"
                        />
                    </div>
                </div>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-slate-50 text-slate-500 font-medium border-b border-slate-100">
                            <tr>
                                <th className="px-6 py-4">{t('tableName')}</th>
                                <th className="px-6 py-4">{t('tableSessions')}</th>
                                <th className="px-6 py-4">{t('tablePrice')}</th>
                                <th className="px-6 py-4">{t('tableValidity')}</th>
                                <th className="px-6 py-4">{t('tableStatus')}</th>
                                <th className="px-6 py-4"></th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {loading ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-8 text-center text-slate-500">
                                        {t('loading')}
                                    </td>
                                </tr>
                            ) : packages.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center">
                                        <div className="w-12 h-12 bg-slate-50 rounded-xl flex items-center justify-center mx-auto mb-3">
                                            <Package className="h-6 w-6 text-slate-400" />
                                        </div>
                                        <h3 className="text-sm font-medium text-slate-900 mb-1">{t('emptyHeader')}</h3>
                                        <p className="text-sm text-slate-500">{t('emptyDesc')}</p>
                                    </td>
                                </tr>
                            ) : (
                                packages.map((pkg) => (
                                    <tr key={pkg.id} className="hover:bg-slate-50/50 transition-colors">
                                        <td className="px-6 py-4 font-medium text-slate-900">{pkg.name}</td>
                                        <td className="px-6 py-4 text-slate-500">{pkg.sessionCount} sessions</td>
                                        <td className="px-6 py-4 font-medium text-slate-900">
                                            {formatCurrency(pkg.price)}
                                            {pkg.savings > 0 && <span className="ml-2 text-xs text-emerald-600 font-normal">{t('saveSavings', { amount: formatCurrency(pkg.savings) })}</span>}
                                        </td>
                                        <td className="px-6 py-4 text-slate-500">
                                            {pkg.validityDays ? t('days', { count: pkg.validityDays }) : t('noExpiry')}
                                        </td>
                                        <td className="px-6 py-4">
                                            <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${
                                                pkg.isActive ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-600'
                                            }`}>
                                                {pkg.isActive ? t('statusActive') : t('statusInactive')}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 text-right">
                                            <button className="p-2 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100">
                                                <MoreVertical className="h-4 w-4" />
                                            </button>
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
                    <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl overflow-hidden max-h-[90vh] flex flex-col" onClick={(e) => e.stopPropagation()}>
                        <div className="flex justify-between items-center p-6 border-b border-slate-100 shrink-0">
                            <h2 className="text-xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>{t('modalTitle')}</h2>
                            <button onClick={() => setIsModalOpen(false)} className="text-slate-400 hover:text-slate-600 transition-colors">
                                <X className="h-5 w-5" />
                            </button>
                        </div>
                        
                        <div className="flex-1 overflow-y-auto p-6">
                            <form id="packageForm" onSubmit={handleSubmit} className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">{t('formName')}</label>
                                    <input
                                        type="text"
                                        required
                                        className="input-field w-full"
                                        value={formData.name}
                                        onChange={(e) => setFormData({...formData, name: e.target.value})}
                                    />
                                </div>
                                
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">{t('formDesc')}</label>
                                    <input
                                        type="text"
                                        className="input-field w-full"
                                        value={formData.description}
                                        onChange={(e) => setFormData({...formData, description: e.target.value})}
                                    />
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">{t('formPrice')}</label>
                                        <input
                                            type="number"
                                            required
                                            min="0"
                                            step="0.01"
                                            className="input-field w-full"
                                            value={formData.price}
                                            onChange={(e) => setFormData({...formData, price: e.target.value})}
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">{t('formOriginalPrice')}</label>
                                        <input
                                            type="number"
                                            min="0"
                                            step="0.01"
                                            className="input-field w-full"
                                            value={formData.originalPrice}
                                            onChange={(e) => setFormData({...formData, originalPrice: e.target.value})}
                                        />
                                    </div>
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">{t('formValidity')}</label>
                                    <input
                                        type="number"
                                        required
                                        min="1"
                                        className="input-field w-full"
                                        value={formData.validityDays}
                                        onChange={(e) => setFormData({...formData, validityDays: e.target.value})}
                                    />
                                </div>

                                <div className="border border-slate-200 rounded-lg p-4 bg-slate-50 space-y-3">
                                    <label className="block text-sm font-medium text-slate-700">{t('includedServices')}</label>
                                    
                                    {formData.services.length > 0 && (
                                        <div className="space-y-2 mb-4">
                                            {formData.services.map((svc) => {
                                                const sName = services.find(s => s.id === svc.serviceId)?.name || t('unknownService');
                                                return (
                                                    <div key={svc.serviceId} className="flex justify-between items-center bg-white border border-slate-200 p-2 rounded-md">
                                                        <span className="text-sm font-medium">{sName} (x{svc.quantity})</span>
                                                        <button 
                                                            type="button" 
                                                            className="text-red-500 hover:text-red-700"
                                                            onClick={() => handleRemoveService(svc.serviceId)}
                                                        >
                                                            <XCircle className="h-4 w-4" />
                                                        </button>
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    )}

                                    <div className="flex gap-2 items-end">
                                        <div className="flex-1">
                                            <label className="block text-xs text-slate-500 mb-1">{t('formService')}</label>
                                            <select
                                                className="input-field w-full text-sm"
                                                value={formData.selectedServiceId}
                                                onChange={(e) => setFormData({...formData, selectedServiceId: e.target.value})}
                                            >
                                                <option value="" disabled>Select a service</option>
                                                {services.map(s => (
                                                    <option key={s.id} value={s.id}>{s.name}</option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="w-24">
                                            <label className="block text-xs text-slate-500 mb-1">{t('formQty')}</label>
                                            <input
                                                type="number"
                                                min="1"
                                                className="input-field w-full text-sm"
                                                value={formData.selectedServiceQuantity}
                                                onChange={(e) => setFormData({...formData, selectedServiceQuantity: e.target.value})}
                                            />
                                        </div>
                                        <button 
                                            type="button" 
                                            onClick={handleAddService}
                                            disabled={!formData.selectedServiceId}
                                            className="btn bg-slate-200 hover:bg-slate-300 text-slate-700 disabled:opacity-50 h-10 px-4"
                                        >
                                            {t('formAddBtn')}
                                        </button>
                                    </div>
                                </div>

                            </form>
                        </div>
                        
                        <div className="p-6 border-t border-slate-100 flex justify-end gap-3 shrink-0">
                            <button
                                type="button"
                                onClick={() => setIsModalOpen(false)}
                                className="px-4 py-2 text-sm font-medium text-slate-700 hover:text-slate-900 transition-colors"
                            >
                                {t('cancelBtn')}
                            </button>
                            <button
                                type="submit"
                                form="packageForm"
                                disabled={submitting}
                                className="btn btn-primary"
                            >
                                {submitting ? t('creatingBtn') : t('createBtn')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

