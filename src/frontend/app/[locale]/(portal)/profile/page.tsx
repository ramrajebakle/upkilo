'use client';

import React, { useEffect, useState } from 'react';
import {
    User,
    Mail,
    Phone,
    Camera,
    Shield,
    CreditCard,
    Save,
    AlertTriangle
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { apiClient } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

export default function CustomerProfilePage() {
    const { addToast } = useToast();
    const [profile, setProfile] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    // Form state
    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        phone: '',
        email: '' // Email usually readonly
    });

    useEffect(() => {
        const fetchProfile = async () => {
            try {
                const headers = { 'Authorization': `Bearer ${localStorage.getItem('client_token')}` };
                const res = await apiClient.get('/api/client-portal/profile', { headers });
                setProfile(res.data);
                setFormData({
                    firstName: res.data.firstName || '',
                    lastName: res.data.lastName || '',
                    phone: res.data.phone || '',
                    email: res.data.email || ''
                });
            } catch (err) {
                console.error('Failed to load profile', err);
                addToast('Failed to load profile', 'error');
            } finally {
                setLoading(false);
            }
        };
        fetchProfile();
    }, [addToast]);

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        try {
            const headers = { 'Authorization': `Bearer ${localStorage.getItem('client_token')}` };
            await apiClient.put('/api/client-portal/profile', formData, { headers });
            addToast('Profile updated successfully', 'success');
        } catch (err) {
            console.error('Update failed', err);
            addToast('Failed to update profile', 'error');
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="max-w-2xl mx-auto space-y-6">
                <div className="h-32 bg-muted rounded-2xl animate-pulse" />
                <div className="space-y-4">
                    <div className="h-12 bg-muted rounded-lg animate-pulse" />
                    <div className="h-12 bg-muted rounded-lg animate-pulse" />
                    <div className="h-12 bg-muted rounded-lg animate-pulse" />
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto space-y-8 animate-fade-in">
            <div>
                <h1 className="text-3xl font-black text-foreground tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                    Profile Settings
                </h1>
                <p className="text-foreground-secondary mt-1">Manage your personal information and preferences</p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Aside Info */}
                <div className="space-y-6">
                    <Card className="p-6 text-center border-none shadow-xl shadow-slate-200/50">
                        <div className="relative w-24 h-24 mx-auto mb-4 group">
                            <div className="w-full h-full rounded-2xl bg-gradient-to-br from-primary to-primary-600 flex items-center justify-center text-white text-3xl font-bold shadow-lg shadow-primary/20">
                                {profile?.firstName?.[0]}{profile?.lastName?.[0]}
                            </div>
                            <button className="absolute -bottom-2 -right-2 p-2 bg-card rounded-xl shadow-lg border border-border-subtle text-foreground-secondary hover:text-primary transition-colors">
                                <Camera className="h-4 w-4" />
                            </button>
                        </div>
                        <h3 className="text-lg font-bold text-foreground">{profile?.firstName} {profile?.lastName}</h3>
                        <p className="text-sm text-foreground-secondary mt-1">Client since {new Date(profile?.createdAt).toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}</p>
                    </Card>

                    <Card className="p-6 border-none shadow-xl shadow-slate-200/50 overflow-hidden relative">
                        <div className="absolute top-0 right-0 w-24 h-24 -mr-8 -mt-8 bg-primary/5 rounded-full" />
                        <h4 className="font-bold text-foreground mb-4 flex items-center gap-2">
                            <Shield className="h-4 w-4 text-success-fg" />
                            Security
                        </h4>
                        <p className="text-sm text-foreground-secondary mb-4">Your account is secured with magic links. No password needed.</p>
                        <Button variant="ghost" size="sm" className="w-full text-primary font-bold">
                            Learn More
                        </Button>
                    </Card>
                </div>

                {/* Form */}
                <div className="lg:col-span-2">
                    <Card className="p-8 border-none shadow-xl shadow-slate-200/50">
                        <form onSubmit={handleSave} className="space-y-6">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div className="space-y-2">
                                    <label className="text-sm font-bold text-foreground">First Name</label>
                                    <input
                                        type="text"
                                        value={formData.firstName}
                                        onChange={e => setFormData(prev => ({ ...prev, firstName: e.target.value }))}
                                        className="w-full px-4 py-3 rounded-xl border border-border focus:border-primary focus:ring-4 focus:ring-primary/5 outline-none transition-all"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <label className="text-sm font-bold text-foreground">Last Name</label>
                                    <input
                                        type="text"
                                        value={formData.lastName}
                                        onChange={e => setFormData(prev => ({ ...prev, lastName: e.target.value }))}
                                        className="w-full px-4 py-3 rounded-xl border border-border focus:border-primary focus:ring-4 focus:ring-primary/5 outline-none transition-all"
                                    />
                                </div>
                            </div>

                            <div className="space-y-2">
                                <label className="text-sm font-bold text-foreground">Phone Number</label>
                                <div className="relative">
                                    <Phone className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-foreground-muted" />
                                    <input
                                        type="tel"
                                        value={formData.phone}
                                        onChange={e => setFormData(prev => ({ ...prev, phone: e.target.value }))}
                                        className="w-full pl-12 pr-4 py-3 rounded-xl border border-border focus:border-primary focus:ring-4 focus:ring-primary/5 outline-none transition-all"
                                    />
                                </div>
                            </div>

                            <div className="space-y-2 opacity-60">
                                <label className="text-sm font-bold text-foreground">Email Address (Readonly)</label>
                                <div className="relative">
                                    <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-foreground-muted" />
                                    <input
                                        type="email"
                                        value={formData.email}
                                        readOnly
                                        className="w-full pl-12 pr-4 py-3 rounded-xl border border-border bg-muted outline-none cursor-not-allowed"
                                    />
                                </div>
                            </div>

                            <div className="pt-6 border-t border-border-subtle flex justify-end">
                                <Button type="submit" className="font-bold py-6 px-10 shadow-lg shadow-primary/20" disabled={saving}>
                                    {saving ? 'Saving...' : 'Save Changes'}
                                    {!saving && <Save className="ml-2 h-4 w-4" />}
                                </Button>
                            </div>
                        </form>
                    </Card>

                    <div className="mt-8 p-4 bg-amber-50 rounded-2xl border border-amber-100 flex items-start gap-4">
                        <div className="p-2 bg-card rounded-lg text-warning-fg shadow-sm">
                            <AlertTriangle className="h-5 w-5" />
                        </div>
                        <div>
                            <h4 className="font-bold text-amber-900 text-sm">Need to delete your account?</h4>
                            <p className="text-xs text-amber-700 mt-1">Please contact support or the business owner directly to remove your personal data from our systems.</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
