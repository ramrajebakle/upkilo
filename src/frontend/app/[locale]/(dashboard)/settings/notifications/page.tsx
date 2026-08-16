'use client';

import { useState, useEffect } from 'react';
import { Bell, Mail, Smartphone, Save, Loader2, CheckCircle2, Zap, Shield, Volume2, LayoutTemplate } from 'lucide-react';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';
import { registerPushNotifications } from '@/lib/push';
import { motion, AnimatePresence } from 'framer-motion';

export default function NotificationsSettingsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(true);
    const [lastSaved, setLastSaved] = useState<Date | null>(null);
    
    const [notifications, setNotifications] = useState({
        emailBookings: true,
        emailReminders: true,
        emailMarketing: false,
        smsReminders: true,
        pushNotifications: true,
        weeklyReport: true,
        playSound: true,
        showBadge: true,
    });

    useEffect(() => {
        fetchNotificationSettings();
    }, []);

    const fetchNotificationSettings = async () => {
        try {
            setLoading(true);
            const res = await api.settings.getNotifications();
            if (res.data) {
                setNotifications(prev => ({ ...prev, ...res.data }));
            }
        } catch (err) {
            console.error('Failed to fetch notification settings:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleToggle = async (key: keyof typeof notifications) => {
        const newValue = !notifications[key];
        
        if (key === 'pushNotifications' && newValue) {
            const sub = await registerPushNotifications();
            if (!sub) return;
        }

        const updatedNotifications = { ...notifications, [key]: newValue };
        setNotifications(updatedNotifications);
        
        setSaving(true);
        try {
            await api.settings.updateNotifications(updatedNotifications);
            setLastSaved(new Date());
        } catch (error) {
            console.error('Auto-save failed:', error);
            setNotifications(notifications);
            toastError('Failed to save preference');
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Alert Matrix...</p>
            </div>
        );
    }

    const sections = [
        {
            title: 'Digital Correspondence',
            icon: Mail,
            items: [
                { key: 'emailBookings', label: 'Inbound Reservations', desc: 'Sync reservation events to primary email node' },
                { key: 'emailReminders', label: 'Temporal Reminders', desc: 'Authorize client-side temporal synchronization' },
                { key: 'emailMarketing', label: 'Commercial Insights', desc: 'Receive ecosystem updates and brand data' },
                { key: 'weeklyReport', label: 'Performance Ledger', desc: 'Weekly aggregate analytics and telemetry' },
            ]
        },
        {
            title: 'Mobile Telemetry',
            icon: Smartphone,
            items: [
                { key: 'smsReminders', label: 'Cellular Uplink', desc: 'Transmit SMS-based temporal reminders' },
                { key: 'pushNotifications', label: 'Instant Intercept', desc: 'Real-time OS-level notification broadcast' },
            ]
        },
        {
            title: 'Sensory Feedback',
            icon: Volume2,
            items: [
                { key: 'playSound', label: 'Auditory Cue', desc: 'Execute acoustic signature on inbound event' },
                { key: 'showBadge', label: 'Visual Indicator', desc: 'Command-center badge incrementation' },
            ]
        }
    ];

    return (
        <div className="max-w-5xl mx-auto space-y-12 animate-fade-in pb-20">
            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
                    <div>
                        <h2 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Signal Configuration</h2>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Authorized communication protocols</p>
                    </div>
                    
                    <div className="flex items-center gap-4">
                        <AnimatePresence mode="wait">
                            {saving ? (
                                <motion.div
                                    key="saving"
                                    initial={{ opacity: 0, x: 20 }}
                                    animate={{ opacity: 1, x: 0 }}
                                    exit={{ opacity: 0, x: -20 }}
                                    className="px-5 py-2.5 bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 text-[10px] font-black rounded-2xl border border-primary-100 dark:border-primary-500/20 uppercase tracking-widest flex items-center gap-3"
                                >
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                    Synchronizing...
                                </motion.div>
                            ) : lastSaved && (
                                <motion.div
                                    key="saved"
                                    initial={{ opacity: 0, x: 20 }}
                                    animate={{ opacity: 1, x: 0 }}
                                    className="px-5 py-2.5 bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 text-[10px] font-black rounded-2xl border border-emerald-100 dark:border-emerald-400/20 uppercase tracking-widest flex items-center gap-3"
                                >
                                    <CheckCircle2 className="h-4 w-4" />
                                    Schema Locked
                                </motion.div>
                            )}
                        </AnimatePresence>
                    </div>
                </div>

                <div className="space-y-16">
                    {sections.map((section) => (
                        <div key={section.title} className="space-y-8">
                            <div className="flex items-center gap-4">
                                <div className="p-3 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-100 dark:border-slate-850 shadow-inner">
                                    <section.icon className="h-5 w-5 text-slate-400 dark:text-slate-600" />
                                </div>
                                <div className="h-px flex-1 bg-slate-50 dark:bg-slate-850" />
                                <h3 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.4em]">
                                    {section.title}
                                </h3>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                                {section.items.map((item) => {
                                    const isEnabled = notifications[item.key as keyof typeof notifications];
                                    return (
                                        <div 
                                            key={item.key} 
                                            className={cn(
                                                "group flex items-center justify-between p-7 rounded-[32px] border transition-all duration-500 cursor-pointer active:scale-[0.98]",
                                                isEnabled 
                                                    ? "bg-white dark:bg-slate-900 border-primary-100 dark:border-primary-500/20 shadow-xl shadow-primary-500/5" 
                                                    : "bg-slate-50/50 dark:bg-slate-950/50 border-transparent"
                                            )}
                                            onClick={() => handleToggle(item.key as keyof typeof notifications)}
                                        >
                                            <div className="flex-1 pr-8">
                                                <p className={cn(
                                                    "font-black text-xs uppercase tracking-widest mb-1.5 transition-colors",
                                                    isEnabled ? "text-primary-600 dark:text-primary-400" : "text-slate-600 dark:text-slate-400"
                                                )}>
                                                    {item.label}
                                                </p>
                                                <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest leading-relaxed">
                                                    {item.desc}
                                                </p>
                                            </div>
                                            
                                            <div
                                                className={cn(
                                                    'relative w-14 h-8 rounded-2xl transition-all duration-500 flex items-center px-1.5',
                                                    isEnabled 
                                                        ? 'bg-primary-500 shadow-lg shadow-primary-500/40' 
                                                        : 'bg-slate-200 dark:bg-slate-800 shadow-inner'
                                                )}
                                            >
                                                <motion.div
                                                    animate={{
                                                        x: isEnabled ? 28 : 0,
                                                        scale: isEnabled ? 1.1 : 1
                                                    }}
                                                    className="w-5 h-5 bg-white rounded-xl shadow-md"
                                                />
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Tactical Notice */}
            <div className="p-8 bg-gradient-to-br from-primary-950 to-slate-900 border border-slate-800 rounded-[40px] flex items-center gap-8 group">
                <div className="p-4 bg-white/5 rounded-2xl border border-white/10 group-hover:scale-110 transition-transform">
                    <Smartphone className="h-8 w-8 text-primary-400" />
                </div>
                <div>
                    <h3 className="text-[10px] font-black text-white uppercase tracking-[0.4em]">Environmental Override</h3>
                    <p className="text-[10px] font-bold text-slate-400 uppercase tracking-[0.2em] mt-2 leading-loose">
                        Global intercept requires browser-level authorization. Inbound signals are prioritized via the <Zap className="inline h-3 w-3 text-primary-400 mb-0.5" /> priority-matrix for mission-critical events.
                    </p>
                </div>
            </div>
        </div>
    );
}

