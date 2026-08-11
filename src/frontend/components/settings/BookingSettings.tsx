'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { apiClient } from '@/lib/api';

export default function BookingSettings() {
    const [loading, setLoading] = useState(true);
    const [settings, setSettings] = useState({
        booking_allow_online: true,
        booking_require_payment: false,
        booking_allow_cancel: true,
        booking_allow_reschedule: true,
        booking_notice_period_hours: 24,
        booking_min_advance_hours: 1,
        booking_max_advance_days: 30,
        booking_slot_duration: 30,
        booking_policy_text: 'Please cancel at least 24 hours in advance.'
    });

    useEffect(() => {
        loadSettings();
    }, []);

    const loadSettings = async () => {
        try {
            // Fetch dedicated booking settings
            const res = await apiClient.get('/api/v1/settings/booking');
            const saved = res.data; 


            // If the API returns flat settings or specific object, adjust here.
            // Relying on previous implementation where SettingsController returns 'settings' dictionary?
            // Actually SettingsController UpdateBusinessSettings updates specific fields.
            // We might need to ensure GET /settings/business returns these values.

            // Quick fix: The current GET /settings/business might NOT return the settings dictionary fully exposed.
            // Let's assume we can GET/POST to a generic settings endpoint or specific one.
            // We'll use the existing business settings structure if possible, but let's check `SettingsController` later.
            // For now, using what's likely available or falling back to defaults.
            if (saved) {
                setSettings(prev => ({ 
                    ...prev, 
                    ...saved,
                    booking_policy_text: saved.booking_policy_text || prev.booking_policy_text || ''
                }));
            }
        } catch (error) {
            console.error('Failed to load settings', error);
        } finally {
            setLoading(false);
        }
    };

    const handleSave = async () => {
        try {
            // We need an endpoint to update these specific keys in Tenant.Settings
            // The existing UpdateBusinessSettingsRequest might not cover generic dictionary updates.
            // We might need to add a new endpoint or update the DTO.
            await apiClient.put('/api/v1/settings/booking', settings);
            alert('Settings saved successfully');
        } catch (error) {
            console.error('Failed to save', error);
            alert('Failed to save settings');
        }
    };

    if (loading) return <div>Loading...</div>;

    return (
        <div className="space-y-10">
            {/* General Booking Settings */}
            <Card className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                <CardHeader className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/20">
                    <CardTitle className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Logistics Configuration</CardTitle>
                    <CardDescription className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Global scheduling authorizations</CardDescription>
                </CardHeader>
                <CardContent className="p-8 space-y-8">
                    <div className="grid gap-6">
                        <label className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/50 border border-slate-100 dark:border-slate-800 cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-all group">
                            <input
                                type="checkbox"
                                checked={settings.booking_allow_online}
                                onChange={e => setSettings({ ...settings, booking_allow_online: e.target.checked })}
                                className="h-5 w-5 rounded-lg border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                            />
                            <div className="space-y-0.5">
                                <span className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight block">Online Terminal Enabled</span>
                                <span className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest block">Clients can initiate bookings via network portal</span>
                            </div>
                        </label>

                        <label className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/50 border border-slate-100 dark:border-slate-800 cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-all group">
                            <input
                                type="checkbox"
                                checked={settings.booking_require_payment}
                                onChange={e => setSettings({ ...settings, booking_require_payment: e.target.checked })}
                                className="h-5 w-5 rounded-lg border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                            />
                            <div className="space-y-0.5">
                                <span className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight block">Pre-emptive settlement</span>
                                <span className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest block">Mandatory financial clearance upon booking dispatch</span>
                            </div>
                        </label>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                        <div className="space-y-3">
                            <Label className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Min. Preparation (Hours)</Label>
                            <Input
                                type="number"
                                value={settings.booking_min_advance_hours}
                                onChange={e => setSettings({ ...settings, booking_min_advance_hours: parseInt(e.target.value) || 0 })}
                                className="h-14 px-6 bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white font-black rounded-2xl"
                            />
                        </div>
                        <div className="space-y-3">
                            <Label className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Horizon limit (Days)</Label>
                            <Input
                                type="number"
                                value={settings.booking_max_advance_days}
                                onChange={e => setSettings({ ...settings, booking_max_advance_days: parseInt(e.target.value) || 0 })}
                                className="h-14 px-6 bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white font-black rounded-2xl"
                            />
                        </div>
                    </div>
                </CardContent>
            </Card>

            <Card className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                <CardHeader className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/20">
                    <CardTitle className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Contractual Clauses</CardTitle>
                    <CardDescription className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Rules for engagement modification</CardDescription>
                </CardHeader>
                <CardContent className="p-8 space-y-10">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <label className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/50 border border-slate-100 dark:border-slate-800 cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-all group">
                            <input
                                type="checkbox"
                                checked={settings.booking_allow_cancel}
                                onChange={e => setSettings({ ...settings, booking_allow_cancel: e.target.checked })}
                                className="h-5 w-5 rounded-lg border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                            />
                            <div className="space-y-0.5">
                                <span className="text-[11px] font-black text-slate-900 dark:text-white uppercase tracking-tight block">Termination Rights</span>
                                <span className="text-[8px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest block">Clients can abort confirmed slots</span>
                            </div>
                        </label>

                        <label className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50 dark:bg-slate-950/50 border border-slate-100 dark:border-slate-800 cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-all group">
                            <input
                                type="checkbox"
                                checked={settings.booking_allow_reschedule}
                                onChange={e => setSettings({ ...settings, booking_allow_reschedule: e.target.checked })}
                                className="h-5 w-5 rounded-lg border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                            />
                            <div className="space-y-0.5">
                                <span className="text-[11px] font-black text-slate-900 dark:text-white uppercase tracking-tight block">Temporal Shifting</span>
                                <span className="text-[8px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest block">Authorized reallocation of booking time</span>
                            </div>
                        </label>
                    </div>

                    <div className="space-y-4">
                        <Label className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Protocol Dead-Zone (Hours)</Label>
                        <Input
                            type="number"
                            value={settings.booking_notice_period_hours}
                            onChange={e => setSettings({ ...settings, booking_notice_period_hours: parseInt(e.target.value) || 0 })}
                            className="h-14 px-6 bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white font-black rounded-2xl"
                        />
                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-600 uppercase tracking-widest pl-2">Minimum latency before appointment for modification authorization.</p>
                    </div>

                    <div className="space-y-4">
                        <Label className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Policy Legalist Text</Label>
                        <textarea
                            className="w-full min-h-[160px] p-6 bg-slate-50 dark:bg-slate-950 rounded-3xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-bold uppercase tracking-widest focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all outline-none resize-none leading-relaxed"
                            value={settings.booking_policy_text}
                            onChange={e => setSettings({ ...settings, booking_policy_text: e.target.value })}
                        />
                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-600 uppercase tracking-widest pl-2">Dispatched to nodes during terminal engagement.</p>
                    </div>

                    <div className="flex pt-6">
                        <Button onClick={handleSave} className="w-full md:w-auto px-12 h-14 rounded-2xl font-black uppercase tracking-widest text-xs shadow-xl shadow-primary-500/20 active:scale-95 transition-all">
                            Commit Policy Parameters
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
