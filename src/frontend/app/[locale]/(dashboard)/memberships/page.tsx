"use client";

import { useState, useEffect } from 'react';
import { CreditCard, Plus, Search, MoreVertical, Users, X } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import { useTranslations } from 'next-intl';
import api from '@/lib/api';
import { toast } from 'sonner';

export default function MembershipsPage() {
    const t = useTranslations('Dashboard');
    const [memberships, setMemberships] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    const [formData, setFormData] = useState({
        name: '',
        description: '',
        price: '',
        billingInterval: 'monthly',
        servicesIncluded: '0',
        discountPercent: '0',
        features: ''
    });

    const fetchMemberships = async () => {
        try {
            setLoading(true);
            const res = await api.memberships.plans.list();
            setMemberships(res.data?.data || []);
        } catch (err) {
            console.error('Failed to fetch memberships:', err);
            toast.error('Failed to load membership plans');
            setMemberships([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchMemberships();
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            setSubmitting(true);
            await api.memberships.plans.create({
                name: formData.name,
                description: formData.description,
                price: parseFloat(formData.price),
                billingInterval: formData.billingInterval,
                servicesIncluded: parseInt(formData.servicesIncluded, 10),
                discountPercent: parseInt(formData.discountPercent, 10),
                features: formData.features.split('\n').filter(f => f.trim() !== '')
            });
            setIsModalOpen(false);
            setFormData({
                name: '', description: '', price: '', billingInterval: 'monthly', 
                servicesIncluded: '0', discountPercent: '0', features: ''
            });
            fetchMemberships();
            toast.success('Membership plan created successfully');
        } catch (err) {
            console.error('Failed to create membership plan:', err);
            toast.error('Failed to create membership plan');
        } finally {
            setSubmitting(false);
        }
    };

    const handleDeletePlan = async (id: string) => {
        if (!window.confirm('Are you sure you want to delete this plan?')) return;
        try {
            await api.memberships.plans.delete(id);
            toast.success('Plan deleted successfully');
            fetchMemberships();
        } catch (err: any) {
            toast.error(err.response?.data?.message || 'Failed to delete plan');
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'Outfit, sans-serif' }}>
                        Memberships
                    </h1>
                    <p className="text-sm text-slate-500">Manage recurring client membership tiers</p>
                </div>
                <button className="btn btn-primary" onClick={() => setIsModalOpen(true)}>
                    <Plus className="h-4 w-4" />
                    New Membership Tier
                </button>
            </div>

            <div className="card-elevated">
                <div className="p-4 border-b border-slate-100 flex gap-4">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                        <input
                            type="text"
                            placeholder="Search membership tiers..."
                            className="input-field pl-10 w-full"
                        />
                    </div>
                </div>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-slate-50 text-slate-500 font-medium border-b border-slate-100">
                            <tr>
                                <th className="px-6 py-4">Name</th>
                                <th className="px-6 py-4">Price</th>
                                <th className="px-6 py-4">Billing Cycle</th>
                                <th className="px-6 py-4">Status</th>
                                <th className="px-6 py-4"></th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {loading ? (
                                <tr>
                                    <td colSpan={5} className="px-6 py-8 text-center text-slate-500">
                                        Loading memberships...
                                    </td>
                                </tr>
                            ) : memberships.length === 0 ? (
                                <tr>
                                    <td colSpan={5} className="px-6 py-12 text-center">
                                        <div className="w-12 h-12 bg-slate-50 rounded-xl flex items-center justify-center mx-auto mb-3">
                                            <CreditCard className="h-6 w-6 text-slate-400" />
                                        </div>
                                        <h3 className="text-sm font-medium text-slate-900 mb-1">No membership tiers</h3>
                                        <p className="text-sm text-slate-500">Create a recurring membership plan to generate steady revenue.</p>
                                    </td>
                                </tr>
                            ) : (
                                memberships.map((tier) => (
                                    <tr key={tier.id} className="hover:bg-slate-50/50 transition-colors">
                                        <td className="px-6 py-4 font-medium text-slate-900">
                                            {tier.name}
                                            {tier.description && <div className="text-xs text-slate-500 font-normal">{tier.description}</div>}
                                        </td>
                                        <td className="px-6 py-4">{formatCurrency(tier.price)}</td>
                                        <td className="px-6 py-4 text-slate-500 capitalize">{tier.billingInterval}</td>
                                        <td className="px-6 py-4">
                                            <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${
                                                tier.isActive ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-600'
                                            }`}>
                                                {tier.isActive ? 'Active' : 'Inactive'}
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
                            <h2 className="text-xl font-bold text-slate-900" style={{ fontFamily: 'Outfit, sans-serif' }}>New Membership Tier</h2>
                            <button onClick={() => setIsModalOpen(false)} className="text-slate-400 hover:text-slate-600 transition-colors">
                                <X className="h-5 w-5" />
                            </button>
                        </div>
                        
                        <div className="flex-1 overflow-y-auto p-6">
                            <form id="membershipForm" onSubmit={handleSubmit} className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">Plan Name *</label>
                                    <input
                                        type="text"
                                        required
                                        className="input-field w-full"
                                        value={formData.name}
                                        onChange={(e) => setFormData({...formData, name: e.target.value})}
                                    />
                                </div>
                                
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                                    <input
                                        type="text"
                                        className="input-field w-full"
                                        value={formData.description}
                                        onChange={(e) => setFormData({...formData, description: e.target.value})}
                                    />
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">Price *</label>
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
                                        <label className="block text-sm font-medium text-slate-700 mb-1">Billing Interval *</label>
                                        <select
                                            required
                                            className="input-field w-full"
                                            value={formData.billingInterval}
                                            onChange={(e) => setFormData({...formData, billingInterval: e.target.value})}
                                        >
                                            <option value="weekly">Weekly</option>
                                            <option value="monthly">Monthly</option>
                                            <option value="yearly">Yearly</option>
                                        </select>
                                    </div>
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">Services Included (per interval)</label>
                                        <input
                                            type="number"
                                            min="-1"
                                            className="input-field w-full"
                                            value={formData.servicesIncluded}
                                            onChange={(e) => setFormData({...formData, servicesIncluded: e.target.value})}
                                        />
                                        <p className="text-xs text-slate-500 mt-1">-1 for unlimited</p>
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-1">Service Discount (%)</label>
                                        <input
                                            type="number"
                                            min="0"
                                            max="100"
                                            className="input-field w-full"
                                            value={formData.discountPercent}
                                            onChange={(e) => setFormData({...formData, discountPercent: e.target.value})}
                                        />
                                    </div>
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-1">Features (one per line)</label>
                                    <textarea
                                        className="input-field w-full resize-none"
                                        rows={4}
                                        placeholder="Free basic amenities&#10;Priority booking&#10;Bring a guest"
                                        value={formData.features}
                                        onChange={(e) => setFormData({...formData, features: e.target.value})}
                                    />
                                </div>
                            </form>
                        </div>
                        
                        <div className="p-6 border-t border-slate-100 flex justify-end gap-3 shrink-0">
                            <button
                                type="button"
                                onClick={() => setIsModalOpen(false)}
                                className="px-4 py-2 text-sm font-medium text-slate-700 hover:text-slate-900 transition-colors"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                form="membershipForm"
                                disabled={submitting}
                                className="btn btn-primary"
                            >
                                {submitting ? 'Creating...' : 'Create Tier'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
