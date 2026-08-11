'use client';

import { useState, useEffect } from 'react';
import { 
    Building2, Mail, Phone, Globe, MapPin, 
    Camera, Save, Loader2, Palette, ShieldCheck,
    Zap, ExternalLink, ChevronRight, Info,
    ArrowUpRight, Target, Activity, Layout,
    Globe2, Server, Cloud
} from 'lucide-react';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';

export default function BusinessSettingsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(true);
    
    const [businessSettings, setBusinessSettings] = useState({
        name: '',
        email: '',
        phone: '',
        address: {
            line1: '',
            line2: '',
            city: '',
            state: '',
            postalCode: '',
            country: ''
        },
        website: '',
        timezone: 'UTC',
        logoUrl: '',
        primaryColor: '#6366f1',
    });

    useEffect(() => {
        fetchBusinessSettings();
    }, []);

    const fetchBusinessSettings = async () => {
        try {
            setLoading(true);
            const res = await api.settings.getBusiness();
            const data = res.data;
            
            setBusinessSettings({
                ...businessSettings,
                name: data.name || '',
                email: data.email || '',
                phone: data.phone || '',
                address: {
                    line1: data.address?.line1 || '',
                    line2: data.address?.line2 || '',
                    city: data.address?.city || '',
                    state: data.address?.state || '',
                    postalCode: data.address?.postalCode || '',
                    country: data.address?.country || ''
                },
                website: data.website || '',
                logoUrl: data.logoUrl || '',
                primaryColor: data.primaryColor || '#6366f1'
            });
        } catch (err) {
            console.error('Failed to fetch business settings:', err);
            toastError('Failed to load corporate core');
        } finally {
            setLoading(false);
        }
    };

    const handleSave = async () => {
        setSaving(true);
        try {
            await api.settings.updateBusiness(businessSettings);
            toastSuccess('Corporate settings committed');
        } catch (error) {
            console.error('Save failed:', error);
            toastError('Failed to commit settings');
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Corporate Nexus...</p>
            </div>
        );
    }

    return (
        <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header Bundle */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-4">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-gradient-to-br from-slate-800 to-slate-950 rounded-[28px] shadow-2xl shadow-slate-500/10 border border-slate-700">
                        <Building2 className="h-8 w-8 text-white" />
                    </div>
                    <div>
                        <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Corporate Nexus</h1>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Foundational Entity Parameters and Global Identification</p>
                    </div>
                </div>
                <div className="flex items-center gap-4">
                    <Button
                        onClick={handleSave}
                        loading={saving}
                        className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-3 bg-primary-600 hover:bg-primary-700"
                    >
                        <Save className="h-4 w-4" /> Commit Protocol
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                <div className="lg:col-span-2 space-y-12">
                    {/* Brand Identity / Visual Core */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                        <div className="flex items-center gap-4">
                            <div className="h-8 w-1 rounded-full bg-primary-500 shadow-lg" />
                            <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Visual Identity Core</h2>
                        </div>

                        <div className="flex flex-col md:flex-row gap-12 items-center lg:items-start">
                            <div className="group relative">
                                <div className="w-44 h-44 rounded-[32px] bg-slate-50 dark:bg-slate-950 flex items-center justify-center overflow-hidden shadow-2xl border-8 border-white dark:border-slate-900 ring-1 ring-slate-100 dark:ring-slate-800 transition-all group-hover:scale-105 duration-500">
                                    {businessSettings.logoUrl ? (
                                        // eslint-disable-next-line @next/next/no-img-element
                                        <img src={businessSettings.logoUrl} alt="Logo" className="w-full h-full object-cover" />
                                    ) : (
                                        <div className="flex flex-col items-center">
                                            <span className="text-slate-900 dark:text-white text-5xl font-black tracking-tighter">
                                                {businessSettings.name?.substring(0, 2).toUpperCase() || 'UP'}
                                            </span>
                                            <div className="w-8 h-1 bg-primary-500 mt-2 rounded-full" />
                                        </div>
                                    )}
                                </div>
                                <button className="absolute -bottom-2 -right-2 p-4 bg-primary-600 text-white rounded-2xl shadow-xl hover:bg-primary-700 transition-all hover:scale-110 active:scale-95 border-4 border-white dark:border-slate-900">
                                    <Camera className="h-6 w-6" />
                                </button>
                            </div>

                            <div className="flex-1 space-y-8 w-full">
                                <div className="space-y-4">
                                    <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em] ml-1">Logo Asset URL</label>
                                    <div className="relative group">
                                        <Globe className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                        <input
                                            type="url"
                                            placeholder="HTTPS://ASSETS.ENTITY.COM/LOGO.PNG"
                                            className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                            value={businessSettings.logoUrl}
                                            onChange={(e) => setBusinessSettings({ ...businessSettings, logoUrl: e.target.value })}
                                        />
                                    </div>
                                </div>

                                <div className="space-y-4">
                                    <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em] ml-1">Brand Luminosity</label>
                                    <div className="flex items-center gap-4">
                                        <div className="relative w-24 h-14 rounded-2xl overflow-hidden border-2 border-slate-100 dark:border-slate-800 shadow-xl group">
                                            <input 
                                                type="color" 
                                                className="absolute inset-0 w-full h-full cursor-pointer border-0 p-0 transform scale-150"
                                                value={businessSettings.primaryColor}
                                                onChange={(e) => setBusinessSettings({ ...businessSettings, primaryColor: e.target.value })}
                                            />
                                        </div>
                                        <div className="relative flex-1 group">
                                            <Palette className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                            <input 
                                                type="text"
                                                className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                                value={businessSettings.primaryColor}
                                                onChange={(e) => setBusinessSettings({ ...businessSettings, primaryColor: e.target.value })}
                                                placeholder="#6366F1"
                                            />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Operational Matrix / Entity Details */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                        <div className="flex items-center gap-4">
                            <div className="h-8 w-1 rounded-full bg-emerald-500 shadow-lg" />
                            <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Operational Parameters</h2>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
                            {[
                                { label: 'Legal: Entity Title', value: businessSettings.name, icon: Building2, key: 'name' },
                                { label: 'Comm: Support Route', value: businessSettings.email, icon: Mail, key: 'email', type: 'email' },
                                { label: 'Comm: Tactical Voice', value: businessSettings.phone, icon: Phone, key: 'phone', type: 'tel' },
                                { label: 'Web: Active Namespace', value: businessSettings.website, icon: Globe, key: 'website', type: 'url' }
                            ].map((field) => (
                                <div key={field.key} className="space-y-4">
                                    <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.2em] ml-1">{field.label}</label>
                                    <div className="relative group">
                                        <field.icon className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                        <input
                                            type={field.type || 'text'}
                                            value={field.value}
                                            onChange={(e) => setBusinessSettings({ ...businessSettings, [field.key]: e.target.value })}
                                            className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                        />
                                    </div>
                                </div>
                            ))}
                        </div>

                        <div className="space-y-8 pt-12 border-t border-slate-50 dark:border-slate-850">
                            <div className="flex items-center justify-between">
                                <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em] ml-1 flex items-center gap-2">
                                    <MapPin className="h-4 w-4" /> Physical Matrix Allocation
                                </label>
                            </div>
                            
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div className="md:col-span-2 relative group">
                                    <MapPin className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                    <input
                                        type="text"
                                        placeholder="STREET INFRASTRUCTURE"
                                        value={businessSettings.address.line1}
                                        onChange={(e) => setBusinessSettings({ ...businessSettings, address: { ...businessSettings.address, line1: e.target.value } })}
                                        className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl pl-12 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                    />
                                </div>
                                {['city', 'state', 'postalCode', 'country'].map((f) => (
                                    <input
                                        key={f}
                                        type="text"
                                        placeholder={f.toUpperCase()}
                                        value={(businessSettings.address as any)[f]}
                                        onChange={(e) => setBusinessSettings({ ...businessSettings, address: { ...businessSettings.address, [f]: e.target.value } })}
                                        className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-2xl px-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                    />
                                ))}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Strategic Guidance / Information Corridor (Referencing User Screenshot) */}
                <div className="space-y-8">
                    <div className="p-8 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl space-y-8 sticky top-8">
                        <div className="flex items-center gap-4">
                            <div className="p-3 bg-primary-500/10 rounded-2xl">
                                <Info className="h-5 w-5 text-primary-400" />
                            </div>
                            <h3 className="text-xs font-black text-white uppercase tracking-widest">Protocol Insights</h3>
                        </div>

                        <div className="space-y-8">
                            <div>
                                <h4 className="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-4">What happens when you save?</h4>
                                <ul className="space-y-4">
                                    {[
                                        { icon: Globe2, text: 'Global Namespace Synchronization' },
                                        { icon: Server, text: 'Core Database Integrity Update' },
                                        { icon: Layout, text: 'Frontend Brand Component Patch' },
                                        { icon: Cloud, text: 'Regional Edge Flush and Reload' }
                                    ].map((item, i) => (
                                        <li key={i} className="flex items-center gap-4 p-4 bg-white/5 rounded-2xl border border-white/5 group hover:bg-white/10 transition-all">
                                            <item.icon className="h-4 w-4 text-primary-400" />
                                            <span className="text-[9px] font-black text-slate-300 uppercase tracking-widest">{item.text}</span>
                                        </li>
                                    ))}
                                </ul>
                            </div>

                            <div className="p-6 bg-gradient-to-br from-primary-900/40 to-slate-950 rounded-3xl border border-primary-500/20 space-y-4">
                                <div className="flex items-center gap-2">
                                    <Target className="h-4 w-4 text-emerald-400" />
                                    <span className="text-[10px] font-black text-white uppercase tracking-widest">Next Step Protocol</span>
                                </div>
                                <p className="text-[9px] font-bold text-slate-400 uppercase tracking-widest leading-loose">
                                    Verify your <span className="text-primary-400">Custom Domain</span> to finalize white-labeling and establish domain authority across all tactical nodes.
                                </p>
                                <Button className="w-full h-12 bg-white text-slate-900 rounded-xl font-black uppercase tracking-widest text-[9px] hover:bg-slate-100 shadow-xl">
                                    Finalise Domain Access
                                </Button>
                            </div>

                            <div className="pt-4 flex items-center justify-between border-t border-white/10">
                                <div className="flex items-center gap-2">
                                    <ShieldCheck className="h-4 w-4 text-emerald-400" />
                                    <span className="text-[9px] font-black text-slate-500 uppercase tracking-widest">Security: Optimal</span>
                                </div>
                                <span className="text-[9px] font-black text-slate-600 uppercase tracking-widest">v2.4.0-CORE</span>
                            </div>
                        </div>
                    </div>
                    
                    {/* Live Performance Matrix (Small Telemetry) */}
                    <div className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-6">
                        <div className="flex items-center justify-between">
                            <h3 className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">Core Flux Telemetry</h3>
                            <Activity className="h-4 w-4 text-primary-500" />
                        </div>
                        <div className="space-y-4">
                            {[
                                { label: 'Asset Load', value: '42ms', color: 'bg-emerald-500' },
                                { label: 'Sync Latency', value: '12ms', color: 'bg-primary-500' }
                            ].map((s, i) => (
                                <div key={i} className="space-y-2">
                                    <div className="flex justify-between text-[9px] font-black uppercase tracking-widest">
                                        <span className="text-slate-400">{s.label}</span>
                                        <span className="text-slate-900 dark:text-white tabular-nums">{s.value}</span>
                                    </div>
                                    <div className="h-1 w-full bg-slate-50 dark:bg-slate-950 rounded-full overflow-hidden">
                                        <div className={cn("h-full rounded-full opacity-60", s.color)} style={{ width: '70%' }} />
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

