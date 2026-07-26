'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    User,
    Mail,
    Phone,
    MapPin,
    Home,
    FileText,
    Bell,
    MessageSquare,
    Save,
    UserPlus,
    Sparkles,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

export default function NewClientPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [step, setStep] = useState(1);
    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        address: '',
        city: '',
        state: '',
        postalCode: '',
        notes: '',
        tags: [] as string[],
        marketingConsent: false,
        smsConsent: false,
    });

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        try {
            await api.clients.create(formData);
            toastSuccess('Client created successfully');
            router.push('/clients?created=true');
        } catch (error) {
            console.error('Failed to create client', error);
            toastError('Failed to create client. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const availableTags = ['VIP', 'Regular', 'New', 'Premium'];

    const toggleTag = (tag: string) => {
        if (formData.tags.includes(tag)) {
            setFormData({ ...formData, tags: formData.tags.filter(t => t !== tag) });
        } else {
            setFormData({ ...formData, tags: [...formData.tags, tag] });
        }
    };

    return (
        <div className="max-w-3xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/clients"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-rose-500 to-pink-600 rounded-xl shadow-lg shadow-rose-500/25">
                            <UserPlus className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'Outfit, sans-serif' }}
                        >
                            Add New Client
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Create a new client profile</p>
                </div>
            </div>

            {/* Progress Steps */}
            <div className="mb-8 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                <div className="flex items-center justify-between relative">
                    <div className="absolute left-0 right-0 top-1/2 h-0.5 bg-slate-200 -translate-y-1/2" />
                    <div
                        className="absolute left-0 top-1/2 h-0.5 bg-gradient-to-r from-primary-500 to-cyan-500 -translate-y-1/2 transition-all duration-500"
                        style={{ width: step === 1 ? '0%' : step === 2 ? '50%' : '100%' }}
                    />
                    {[
                        { num: 1, label: 'Basic Info' },
                        { num: 2, label: 'Address' },
                        { num: 3, label: 'Preferences' },
                    ].map((s) => (
                        <div key={s.num} className="relative flex flex-col items-center">
                            <div className={cn(
                                'w-10 h-10 rounded-full flex items-center justify-center font-semibold text-sm transition-all z-10',
                                step >= s.num
                                    ? 'bg-gradient-to-br from-primary-500 to-cyan-500 text-white shadow-lg'
                                    : 'bg-slate-100 text-slate-400'
                            )}>
                                {s.num}
                            </div>
                            <span className={cn(
                                'text-sm mt-2 font-medium',
                                step >= s.num ? 'text-slate-900' : 'text-slate-400'
                            )}>
                                {s.label}
                            </span>
                        </div>
                    ))}
                </div>
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit} className="space-y-6">
                {/* Step 1: Basic Info */}
                {step === 1 && (
                    <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                        <div className="flex items-center gap-3 mb-6">
                            <div className="p-2 bg-blue-100 rounded-lg">
                                <User className="h-5 w-5 text-blue-600" />
                            </div>
                            <h2 className="text-lg font-semibold text-slate-900">Basic Information</h2>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    First Name <span className="text-red-500">*</span>
                                </label>
                                <div className="relative">
                                    <User className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                    <input
                                        type="text"
                                        value={formData.firstName}
                                        onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                                        className="input pl-11"
                                        placeholder="John"
                                        required
                                    />
                                </div>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    Last Name <span className="text-red-500">*</span>
                                </label>
                                <input
                                    type="text"
                                    value={formData.lastName}
                                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                                    className="input"
                                    placeholder="Doe"
                                    required
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    Email Address
                                </label>
                                <div className="relative">
                                    <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                    <input
                                        type="email"
                                        value={formData.email}
                                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                        className="input pl-11"
                                        placeholder="john@example.com"
                                    />
                                </div>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    Phone Number
                                </label>
                                <div className="relative">
                                    <Phone className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                    <input
                                        type="tel"
                                        value={formData.phone}
                                        onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                                        className="input pl-11"
                                        placeholder="+1 (555) 123-4567"
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Client Tags */}
                        <div className="mt-6 pt-6 border-t border-slate-100">
                            <label className="block text-sm font-medium text-slate-700 mb-3">
                                Client Tags
                            </label>
                            <div className="flex flex-wrap gap-2">
                                {availableTags.map((tag) => (
                                    <button
                                        key={tag}
                                        type="button"
                                        onClick={() => toggleTag(tag)}
                                        className={cn(
                                            'px-4 py-2 rounded-lg text-sm font-medium transition-all',
                                            formData.tags.includes(tag)
                                                ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                        )}
                                    >
                                        {tag}
                                    </button>
                                ))}
                            </div>
                        </div>
                    </div>
                )}

                {/* Step 2: Address */}
                {step === 2 && (
                    <div className="card-elevated p-6 animate-fade-in-up">
                        <div className="flex items-center gap-3 mb-6">
                            <div className="p-2 bg-emerald-100 rounded-lg">
                                <MapPin className="h-5 w-5 text-emerald-600" />
                            </div>
                            <h2 className="text-lg font-semibold text-slate-900">Address Details</h2>
                        </div>

                        <div className="space-y-6">
                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    Street Address
                                </label>
                                <div className="relative">
                                    <Home className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                    <input
                                        type="text"
                                        value={formData.address}
                                        onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                                        className="input pl-11"
                                        placeholder="123 Main Street"
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">City</label>
                                    <input
                                        type="text"
                                        value={formData.city}
                                        onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                                        className="input"
                                        placeholder="New York"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">State</label>
                                    <input
                                        type="text"
                                        value={formData.state}
                                        onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                                        className="input"
                                        placeholder="NY"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Postal Code</label>
                                    <input
                                        type="text"
                                        value={formData.postalCode}
                                        onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
                                        className="input"
                                        placeholder="10001"
                                    />
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {/* Step 3: Preferences */}
                {step === 3 && (
                    <div className="space-y-6 animate-fade-in-up">
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-violet-100 rounded-lg">
                                    <FileText className="h-5 w-5 text-violet-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Notes & Preferences</h2>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-slate-700 mb-2">
                                    Client Notes
                                </label>
                                <textarea
                                    className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all resize-none"
                                    rows={4}
                                    value={formData.notes}
                                    onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                                    placeholder="Any notes about this client (preferences, allergies, etc.)..."
                                />
                            </div>
                        </div>

                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-amber-100 rounded-lg">
                                    <Bell className="h-5 w-5 text-amber-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Communication Preferences</h2>
                            </div>

                            <div className="space-y-4">
                                <label className="flex items-center justify-between p-4 bg-slate-50 rounded-xl cursor-pointer hover:bg-slate-100 transition-colors">
                                    <div className="flex items-center gap-3">
                                        <Mail className="h-5 w-5 text-slate-400" />
                                        <div>
                                            <p className="font-medium text-slate-900">Marketing Emails</p>
                                            <p className="text-sm text-slate-500">Receive promotional offers and updates</p>
                                        </div>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => setFormData({ ...formData, marketingConsent: !formData.marketingConsent })}
                                        className={cn(
                                            'relative w-12 h-6 rounded-full transition-colors',
                                            formData.marketingConsent ? 'bg-primary-500' : 'bg-slate-300'
                                        )}
                                    >
                                        <span className={cn(
                                            'absolute top-1 w-4 h-4 bg-white rounded-full shadow transition-all',
                                            formData.marketingConsent ? 'left-7' : 'left-1'
                                        )} />
                                    </button>
                                </label>

                                <label className="flex items-center justify-between p-4 bg-slate-50 rounded-xl cursor-pointer hover:bg-slate-100 transition-colors">
                                    <div className="flex items-center gap-3">
                                        <MessageSquare className="h-5 w-5 text-slate-400" />
                                        <div>
                                            <p className="font-medium text-slate-900">SMS Notifications</p>
                                            <p className="text-sm text-slate-500">Get appointment reminders via text</p>
                                        </div>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => setFormData({ ...formData, smsConsent: !formData.smsConsent })}
                                        className={cn(
                                            'relative w-12 h-6 rounded-full transition-colors',
                                            formData.smsConsent ? 'bg-primary-500' : 'bg-slate-300'
                                        )}
                                    >
                                        <span className={cn(
                                            'absolute top-1 w-4 h-4 bg-white rounded-full shadow transition-all',
                                            formData.smsConsent ? 'left-7' : 'left-1'
                                        )} />
                                    </button>
                                </label>
                            </div>
                        </div>
                    </div>
                )}

                {/* Actions */}
                <div className="flex items-center justify-between pt-4 animate-fade-in" style={{ animationDelay: '300ms' }}>
                    {step > 1 ? (
                        <button
                            type="button"
                            onClick={() => setStep(step - 1)}
                            className="btn btn-secondary"
                        >
                            <ArrowLeft className="h-4 w-4" />
                            Previous
                        </button>
                    ) : (
                        <Link href="/clients" className="btn btn-secondary">
                            Cancel
                        </Link>
                    )}

                    {step < 3 ? (
                        <button
                            type="button"
                            onClick={() => setStep(step + 1)}
                            disabled={step === 1 && (!formData.firstName || !formData.lastName)}
                            className="btn btn-primary"
                        >
                            Continue
                            <Sparkles className="h-4 w-4" />
                        </button>
                    ) : (
                        <button
                            type="submit"
                            disabled={loading}
                            className="btn btn-primary"
                        >
                            {loading ? (
                                <>
                                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                    Creating...
                                </>
                            ) : (
                                <>
                                    <Save className="h-4 w-4" />
                                    Create Client
                                </>
                            )}
                        </button>
                    )}
                </div>
            </form>
        </div>
    );
}
