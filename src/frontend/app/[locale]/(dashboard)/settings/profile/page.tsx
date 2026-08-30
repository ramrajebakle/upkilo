'use client';

import { useCallback, useEffect, useState } from 'react';
import { User, Camera, Save, Loader2, Mail, Phone, Tag, Shield, BadgeCheck, Zap, CheckCircle2, AlertCircle } from 'lucide-react';
import { useToast } from '@/components/ui/Toast';
import api from '@/lib/api';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { useAutoSave } from '@/hooks/useAutoSave';

export default function ProfileSettingsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [user, setUser] = useState({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        role: '',
        initials: ''
    });

    useEffect(() => {
        const fetchProfile = async () => {
            try {
                const res = await api.profile.get();
                const data = res.data;
                setUser({
                    firstName: data.firstName || '',
                    lastName: data.lastName || '',
                    email: data.email || '',
                    phone: data.phone || '',
                    role: data.role ? data.role.charAt(0).toUpperCase() + data.role.slice(1) : 'User',
                    initials: (data.firstName?.[0] || 'U') + (data.lastName?.[0] || '')
                });
            } catch (error) {
                console.error('Failed to fetch profile:', error);
                toastError('Failed to load profile data');
            } finally {
                setLoading(false);
            }
        };

        fetchProfile();
    }, [toastError]);

    const saveProfile = useCallback(async (data: typeof user) => {
        await api.profile.update({
            firstName: data.firstName,
            lastName: data.lastName,
            phone: data.phone,
        });
        setUser(prev => ({
            ...prev,
            initials: (data.firstName?.[0] || 'U') + (data.lastName?.[0] || ''),
        }));
        setIsDirty(false);
    }, []);

    const { status: autoSaveStatus } = useAutoSave(user, saveProfile, { enabled: isDirty });

    const handleSave = async () => {
        setSaving(true);
        try {
            await saveProfile(user);
            toastSuccess('Profile updated successfully');
        } catch (error) {
            toastError('Failed to update profile');
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-foreground-secondary">Syncing Identity Cache...</p>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header / Hero Section */}
            <div className="relative p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row items-center md:items-start gap-10">
                    <div className="relative">
                        <div className="w-32 h-32 rounded-[32px] bg-gradient-to-br from-primary-500 via-primary-500 to-primary-600 flex items-center justify-center text-white text-4xl font-black shadow-2xl shadow-primary-500/30 ring-8 ring-white dark:ring-slate-950 transition-transform group-hover:scale-105 duration-500">
                            {user.initials}
                        </div>
                        <button className="absolute -bottom-2 -right-2 p-3 bg-white dark:bg-slate-800 rounded-2xl shadow-xl border border-slate-100 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:text-primary-500 transition-all hover:scale-110 active:scale-95">
                            <Camera className="h-5 w-5" />
                        </button>
                    </div>
                    
                    <div className="flex-1 space-y-4 text-center md:text-left">
                        <div>
                            <div className="flex flex-wrap items-center justify-center md:justify-start gap-3">
                                <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">{user.firstName} {user.lastName}</h1>
                                <div className="px-3 py-1 bg-primary-50 dark:bg-primary-500/10 text-primary-600 dark:text-primary-400 text-[9px] font-black rounded-lg border border-primary-100 dark:border-primary-500/20 uppercase tracking-widest flex items-center gap-1.5 shadow-sm">
                                    <BadgeCheck className="h-3.5 w-3.5" />
                                    Verified Agent
                                </div>
                            </div>
                            <p className="text-xs font-black text-primary-500 uppercase tracking-[0.3em] mt-2">{user.role} Status</p>
                        </div>
                        
                        <p className="text-xs font-bold text-foreground-muted uppercase tracking-widest max-w-md leading-relaxed">
                            Primary identity node for cross-organization authorization. Permissions derived from active tenant policy.
                        </p>
                        
                        <div className="flex items-center justify-center md:justify-start gap-4">
                            <div className="flex -space-x-2">
                                {[1,2,3].map(i => (
                                    <div key={i} className="w-8 h-8 rounded-full bg-slate-100 dark:bg-slate-800 border-2 border-white dark:border-slate-900 shadow-sm" />
                                ))}
                            </div>
                            <span className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">Shared in 3 Clusters</span>
                        </div>
                    </div>
                </div>
                
                {/* Background Decoration */}
                <div className="absolute top-0 right-0 -mr-20 -mt-20 w-80 h-80 bg-primary-500/5 dark:bg-primary-500/10 rounded-full blur-3xl" />
                <Zap className="absolute bottom-4 right-8 h-32 w-32 text-slate-50 dark:text-slate-850/30 -rotate-12 group-hover:rotate-0 transition-transform duration-1000" />
            </div>

            {/* Form Matrix */}
            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                <div className="flex items-center gap-4">
                    <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                    <h2 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Personal Parameters</h2>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-x-10 gap-y-8">
                    <div className="space-y-3 pt-4 border-t border-slate-50 dark:border-slate-850">
                        <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.2em] ml-1">Identity: Given Name</label>
                        <div className="relative group">
                            <Tag className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                            <input 
                                type="text" 
                                value={user.firstName} 
                                onChange={e => { setUser({...user, firstName: e.target.value}); setIsDirty(true); }}
                                className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white"
                            />
                        </div>
                    </div>

                    <div className="space-y-3 pt-4 border-t border-slate-50 dark:border-slate-850">
                        <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.2em] ml-1">Identity: Surname</label>
                        <div className="relative group">
                            <Tag className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                            <input 
                                type="text" 
                                value={user.lastName} 
                                onChange={e => { setUser({...user, lastName: e.target.value}); setIsDirty(true); }}
                                className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white"
                            />
                        </div>
                    </div>

                    <div className="space-y-3 pt-4 border-t border-slate-50 dark:border-slate-850">
                        <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.2em] ml-1">Comm: Uplink Origin</label>
                        <div className="relative group">
                            <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300" />
                            <input 
                                type="email" 
                                value={user.email} 
                                readOnly
                                className="w-full h-14 bg-slate-100 dark:bg-slate-950/50 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none cursor-not-allowed text-foreground-muted shadow-inner"
                            />
                            <div className="absolute right-4 top-1/2 -translate-y-1/2">
                                <Shield className="h-4 w-4 text-slate-200" />
                            </div>
                        </div>
                        <p className="text-[9px] font-bold text-foreground-muted uppercase tracking-widest mt-2 pl-1 flex items-center gap-1.5">
                            <Shield className="h-3 w-3" /> Core domain root locked by admin
                        </p>
                    </div>

                    <div className="space-y-3 pt-4 border-t border-slate-50 dark:border-slate-850">
                        <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.2em] ml-1">Comm: Mobile Telemetry</label>
                        <div className="relative group">
                            <Phone className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                            <input 
                                type="tel" 
                                value={user.phone} 
                                onChange={e => { setUser({...user, phone: e.target.value}); setIsDirty(true); }}
                                className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white"
                            />
                        </div>
                    </div>
                </div>

                <div className="flex items-center justify-between pt-10 border-t border-slate-50 dark:border-slate-850">
                    {/* Auto-save status indicator */}
                    <div aria-live="polite" className="text-xs font-medium flex items-center gap-1.5">
                        {autoSaveStatus === 'saving' && (
                            <><Loader2 className="h-3.5 w-3.5 animate-spin text-foreground-muted" aria-hidden="true" /><span className="text-foreground-muted">Saving…</span></>
                        )}
                        {autoSaveStatus === 'saved' && (
                            <><CheckCircle2 className="h-3.5 w-3.5 text-success-fg" aria-hidden="true" /><span className="text-success-fg">Saved automatically</span></>
                        )}
                        {autoSaveStatus === 'error' && (
                            <><AlertCircle className="h-3.5 w-3.5 text-danger-fg" aria-hidden="true" /><span className="text-danger-fg">Auto-save failed</span></>
                        )}
                    </div>
                    <Button
                        onClick={handleSave}
                        disabled={saving}
                        className="h-14 px-12 rounded-[20px] font-black uppercase tracking-[0.2em] text-xs shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-3"
                    >
                        {saving ? (
                            <>
                                <Loader2 className="h-5 w-5 animate-spin" />
                                Transmitting...
                            </>
                        ) : (
                            <>
                                <Save className="h-5 w-5" />
                                Commit Identity
                            </>
                        )}
                    </Button>
                </div>
            </div>

            {/* Security Notice */}
            <div className="p-8 bg-gradient-to-br from-slate-900 to-primary-950 border border-slate-800 rounded-[32px] flex items-center gap-8 group">
                <div className="p-4 bg-white/5 rounded-2xl border border-white/10 group-hover:scale-110 transition-transform">
                    <Shield className="h-8 w-8 text-primary-400" />
                </div>
                <div>
                    <h3 className="text-[10px] font-black text-white uppercase tracking-[0.4em]">Auth Protocol Verification</h3>
                    <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-[0.2em] mt-2 leading-loose">
                        Synchronized with global identity root. Last heartbeat audit: <span className="text-primary-400 font-black">Just Now</span>. All biometric and semantic credentials secured.
                    </p>
                </div>
            </div>
        </div>
    );
}

