"use client";

import React, { useState, useEffect } from 'react';
import {
    Palette, Globe, Mail, Code, Save, CheckCircle, XCircle,
    Loader2, RefreshCw, Eye, EyeOff, Upload, Link, 
    Zap, Sparkles, Building2, ShieldCheck, ExternalLink,
    Search, Monitor, Play, History
} from 'lucide-react';
import { apiClient as api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';
import { motion, AnimatePresence } from 'framer-motion';

interface WhiteLabelConfig {
    customDomain?: string;
    customLogoUrl?: string;
    primaryColor?: string;
    secondaryColor?: string;
    removePoweredBy?: boolean;
    customFavicon?: string;
    customCss?: string;
    customEmailDomain?: string;
    isVerified?: boolean;
    domainVerifiedAt?: string;
}

import { FeatureGate } from '@/components/ui/FeatureGate';

export default function BrandingPage() {
    return (
        <FeatureGate 
            featureName="CustomBranding" 
            title="Custom Branding"
            description="Upgrade your plan to unlock advanced white-label features, custom CSS, and visual identity."
        >
            <BrandingContent />
        </FeatureGate>
    );
}

function BrandingContent() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [config, setConfig] = useState<WhiteLabelConfig>({
        primaryColor: '#6366f1',
        secondaryColor: '#8b5cf6',
        removePoweredBy: false,
    });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [verifyingDomain, setVerifyingDomain] = useState(false);
    const [verifyingEmail, setVerifyingEmail] = useState(false);
    const [domainVerifyResult, setDomainVerifyResult] = useState<{ success: boolean; message?: string } | null>(null);
    const [emailVerifyResult, setEmailVerifyResult] = useState<{ success: boolean; spfValid?: boolean; dkimValid?: boolean } | null>(null);
    const [activeTab, setActiveTab] = useState<'branding' | 'domain' | 'email' | 'css'>('branding');
    const [showCssPreview, setShowCssPreview] = useState(false);

    useEffect(() => {
        const load = async () => {
            try {
                const res = await api.get('/api/v1/whitelabel');
                setConfig(res.data?.data || res.data || {});
            } catch {
                // Config may not exist yet
            } finally {
                setLoading(false);
            }
        };
        load();
    }, []);

    const handleSave = async () => {
        setSaving(true);
        try {
            await api.put('/api/v1/whitelabel', config);
            toastSuccess('Branding parameters committed');
        } catch {
            toastError('Failed to save branding settings');
        } finally {
            setSaving(false);
        }
    };

    const handleVerifyDomain = async () => {
        setVerifyingDomain(true);
        setDomainVerifyResult(null);
        try {
            const res = await api.post('/api/v1/whitelabel/verify-domain');
            const data = res.data;
            setDomainVerifyResult({ 
                success: data.success, 
                message: data.error || (data.isVerified ? 'Domain verified successfully!' : 'Verification failed') 
            });
            if (data.isVerified) {
                setConfig(prev => ({ ...prev, isVerified: true, domainVerifiedAt: new Date().toISOString() }));
                toastSuccess('Domain synchronised successfully');
            } else {
                toastError(data.error || 'Domain verification failed');
            }
        } catch {
            setDomainVerifyResult({ success: false, message: 'Verification request failed' });
            toastError('Failed to verify domain');
        } finally {
            setVerifyingDomain(false);
        }
    };

    const handleVerifyEmailDomain = async () => {
        setVerifyingEmail(true);
        setEmailVerifyResult(null);
        try {
            const res = await api.post('/api/v1/whitelabel/verify-email-domain');
            const data = res.data;
            setEmailVerifyResult({ success: data.success, spfValid: data.spfValid, dkimValid: data.dkimValid });
            if (data.success) toastSuccess('Email pathway verified');
            else toastError(data.message || 'Email domain verification failed');
        } catch {
            toastError('Failed to verify email domain');
        } finally {
            setVerifyingEmail(false);
        }
    };

    const tabs = [
        { id: 'branding', label: 'Identity', icon: Palette },
        { id: 'domain', label: 'Namespace', icon: Globe },
        { id: 'email', label: 'Comms', icon: Mail },
        { id: 'css', label: 'Schema', icon: Code },
    ] as const;

    if (loading) return (
        <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
            <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
            <p className="text-[10px] font-black uppercase tracking-[0.4em] text-foreground-secondary">Syncing Branding Matrix...</p>
        </div>
    );

    return (
        <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header & Commit Bundle */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-gradient-to-br from-primary-600 to-primary-900 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                        <Palette className="h-8 w-8 text-white" />
                    </div>
                    <div>
                        <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Identity Nexus</h1>
                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">High-Fidelity White-Label Engine</p>
                    </div>
                </div>
                <Button 
                    onClick={handleSave} 
                    disabled={saving} 
                    className="h-14 px-12 rounded-2xl font-black uppercase tracking-[0.2em] text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-4"
                >
                    {saving ? <Loader2 className="h-5 w-5 animate-spin" /> : <Save className="h-5 w-5" />}
                    {saving ? 'Syncing...' : 'Commit Schema'}
                </Button>
            </div>

            {/* Navigation Spectrum */}
            <div className="p-1.5 bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-850 rounded-[32px] shadow-inner flex flex-wrap lg:flex-nowrap gap-1">
                {tabs.map(tab => (
                    <button
                        key={tab.id}
                        onClick={() => setActiveTab(tab.id as typeof activeTab)}
                        className={cn(
                            'flex-1 flex items-center justify-center gap-3 px-8 py-4 rounded-[28px] text-[10px] font-black uppercase tracking-widest transition-all duration-500 group',
                            activeTab === tab.id 
                                ? 'bg-white dark:bg-slate-900 text-primary-600 dark:text-primary-400 shadow-xl' 
                                : 'text-foreground-secondary hover:text-slate-900 dark:hover:text-slate-300'
                        )}
                    >
                        <tab.icon className={cn("h-4 w-4 transition-transform group-hover:scale-110", activeTab === tab.id ? "text-primary-500" : "text-foreground-muted")} />
                        {tab.label}
                    </button>
                ))}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                {/* Left Column: Configuration Matrix */}
                <div className="lg:col-span-2 space-y-12">
                    <AnimatePresence mode="wait">
                        <motion.div
                            key={activeTab}
                            initial={{ opacity: 0, y: 20 }}
                            animate={{ opacity: 1, y: 0 }}
                            exit={{ opacity: 0, scale: 0.98 }}
                            transition={{ duration: 0.4 }}
                        >
                            {activeTab === 'branding' && (
                                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                                    <div className="space-y-4">
                                        <div className="flex items-center gap-4">
                                            <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                                            <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Visual DNA</h2>
                                        </div>
                                        
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 pt-6">
                                            <div className="space-y-4">
                                                <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">Master Media URL</label>
                                                <div className="relative group">
                                                    <Building2 className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                                                    <Input
                                                        value={config.customLogoUrl || ''}
                                                        onChange={e => setConfig(p => ({ ...p, customLogoUrl: e.target.value }))}
                                                        className="h-14 pl-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white"
                                                        placeholder="HTTPS://ENTITY.ROOT/LOGO.PNG"
                                                    />
                                                </div>
                                                {config.customLogoUrl && (
                                                    <div className="mt-4 p-6 bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 rounded-[28px] inline-flex items-center justify-center shadow-inner group">
                                                        <img src={config.customLogoUrl} alt="DNA" className="h-12 object-contain group-hover:scale-110 transition-transform duration-700" onError={e => (e.target as HTMLImageElement).style.display = 'none'} />
                                                    </div>
                                                )}
                                            </div>
                                            
                                            <div className="space-y-4">
                                                <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">Interface Favicon</label>
                                                <div className="relative group">
                                                    <Monitor className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                                                    <Input
                                                        value={config.customFavicon || ''}
                                                        onChange={e => setConfig(p => ({ ...p, customFavicon: e.target.value }))}
                                                        className="h-14 pl-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white"
                                                        placeholder="HTTPS://ENTITY.ROOT/FAVICON.ICO"
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8 pt-12 border-t border-slate-50 dark:border-slate-850">
                                        <div className="space-y-6">
                                            <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">Primary Spectrum</label>
                                            <div className="flex items-center gap-4 p-3 bg-slate-50 dark:bg-slate-950 rounded-[24px] border border-transparent dark:border-slate-850 shadow-inner group">
                                                <div className="relative h-12 w-12 rounded-xl overflow-hidden border-2 border-white dark:border-slate-800 shadow-lg group-hover:scale-110 transition-transform duration-500 cursor-pointer">
                                                    <input
                                                        type="color"
                                                        value={config.primaryColor || '#6366f1'}
                                                        onChange={e => setConfig(p => ({ ...p, primaryColor: e.target.value }))}
                                                        className="absolute inset-0 scale-150 cursor-pointer"
                                                    />
                                                </div>
                                                <Input
                                                    value={config.primaryColor || '#6366f1'}
                                                    onChange={e => setConfig(p => ({ ...p, primaryColor: e.target.value }))}
                                                    className="flex-1 bg-transparent border-none text-xs font-black uppercase tracking-widest focus-visible:ring-0 dark:text-white"
                                                />
                                            </div>
                                        </div>
                                        <div className="space-y-6">
                                            <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">Secondary Spectrum</label>
                                            <div className="flex items-center gap-4 p-3 bg-slate-50 dark:bg-slate-950 rounded-[24px] border border-transparent dark:border-slate-850 shadow-inner group">
                                                <div className="relative h-12 w-12 rounded-xl overflow-hidden border-2 border-white dark:border-slate-800 shadow-lg group-hover:scale-110 transition-transform duration-500 cursor-pointer">
                                                    <input
                                                        type="color"
                                                        value={config.secondaryColor || '#8b5cf6'}
                                                        onChange={e => setConfig(p => ({ ...p, secondaryColor: e.target.value }))}
                                                        className="absolute inset-0 scale-150 cursor-pointer"
                                                    />
                                                </div>
                                                <Input
                                                    value={config.secondaryColor || '#8b5cf6'}
                                                    onChange={e => setConfig(p => ({ ...p, secondaryColor: e.target.value }))}
                                                    className="flex-1 bg-transparent border-none text-xs font-black uppercase tracking-widest focus-visible:ring-0 dark:text-white"
                                                />
                                            </div>
                                        </div>
                                    </div>

                                    <div className="pt-12 border-t border-slate-50 dark:border-slate-850">
                                        <div className="flex items-center justify-between p-8 bg-slate-50/50 dark:bg-slate-950/40 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner group transition-all">
                                            <div className="flex items-center gap-6">
                                                <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800 shadow-sm group-hover:rotate-6 transition-transform">
                                                    <ShieldCheck className="h-6 w-6 text-primary-500" />
                                                </div>
                                                <div>
                                                    <p className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest">Total Anonymization</p>
                                                    <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-1">Purge "Powered by Upkilo" artifacts from all Nodes</p>
                                                </div>
                                            </div>
                                            <button 
                                                onClick={() => setConfig(p => ({ ...p, removePoweredBy: !p.removePoweredBy }))}
                                                className={cn(
                                                    "h-12 w-20 rounded-[20px] transition-all duration-500 relative flex items-center px-1",
                                                    config.removePoweredBy ? "bg-primary-500 shadow-xl shadow-primary-500/30" : "bg-slate-200 dark:bg-slate-800"
                                                )}
                                            >
                                                <div className={cn(
                                                    "h-10 w-10 rounded-xl bg-card shadow-xl transition-all duration-500",
                                                    config.removePoweredBy ? "translate-x-8" : "translate-x-0"
                                                )} />
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            )}

                            {activeTab === 'domain' && (
                                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                                    <div className="space-y-4">
                                        <div className="flex items-center gap-4">
                                            <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                                            <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Namespace Routing</h2>
                                        </div>
                                        <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-2 ml-14">Map custom top-level domain to your reservations node</p>

                                        <div className="flex flex-col md:flex-row gap-6 pt-6">
                                            <div className="flex-1 relative group">
                                                <Globe className="absolute left-5 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                                                <Input
                                                    value={config.customDomain || ''}
                                                    onChange={e => setConfig(p => ({ ...p, customDomain: e.target.value, isVerified: false }))}
                                                    className="h-16 pl-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white"
                                                    placeholder="RESERVATIONS.ENTITY.ROOT"
                                                />
                                            </div>
                                            <Button 
                                                onClick={handleVerifyDomain} 
                                                disabled={verifyingDomain || !config.customDomain} 
                                                className="h-16 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl active:scale-95 transition-all flex items-center gap-4"
                                                variant="outline"
                                            >
                                                {verifyingDomain ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                                                {verifyingDomain ? 'Verifying Path...' : 'Ping DNS Record'}
                                            </Button>
                                        </div>

                                        <AnimatePresence>
                                            {domainVerifyResult && (
                                                <motion.div 
                                                    initial={{ opacity: 0, height: 0 }}
                                                    animate={{ opacity: 1, height: 'auto' }}
                                                    className={cn(
                                                        "p-6 rounded-[24px] border flex items-center gap-4",
                                                        domainVerifyResult.success 
                                                            ? "bg-emerald-50/10 dark:bg-emerald-400/5 border-emerald-100 dark:border-emerald-400/20 text-emerald-600 dark:text-emerald-400" 
                                                            : "bg-red-50/10 dark:bg-red-400/5 border-red-100 dark:border-red-400/20 text-red-600 dark:text-red-400"
                                                    )}
                                                >
                                                    {domainVerifyResult.success ? <CheckCircle className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}
                                                    <p className="text-[10px] font-black uppercase tracking-widest">{domainVerifyResult.message}</p>
                                                </motion.div>
                                            )}
                                        </AnimatePresence>
                                    </div>

                                    <div className="p-10 bg-slate-50 dark:bg-slate-950/50 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner space-y-8">
                                        <div className="flex items-center gap-4">
                                            <div className="p-3 bg-white dark:bg-slate-900 rounded-xl border border-slate-100 dark:border-slate-800 shadow-sm">
                                                <Code className="h-4 w-4 text-primary-500" />
                                            </div>
                                            <h3 className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Required DNS Archetype</h3>
                                        </div>
                                        
                                        <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[24px] p-8 font-mono text-xs overflow-hidden relative group">
                                            <div className="grid grid-cols-3 gap-8 text-[9px] font-black text-foreground-muted uppercase tracking-[0.2em] mb-4 border-b border-slate-50 dark:border-slate-850 pb-4">
                                                <span>Protocol</span><span>Namespace</span><span>Endpoint Target</span>
                                            </div>
                                            <div className="grid grid-cols-3 gap-8 text-[11px] font-black dark:text-white tracking-widest">
                                                <span className="text-primary-500">CNAME</span>
                                                <span className="truncate">{config.customDomain?.split('.')[0] || 'RESERVATIONS'}</span>
                                                <span className="text-slate-500 dark:text-slate-400">APP.UPKILO.ROOT</span>
                                            </div>
                                            <Zap className="absolute -bottom-6 -right-6 h-24 w-24 text-primary-500/5 -rotate-12 group-hover:rotate-0 transition-transform duration-1000" />
                                        </div>
                                        <div className="flex items-center gap-3 px-2">
                                            <History className="h-3 w-3 text-foreground-muted" />
                                            <p className="text-[9px] font-bold text-foreground-muted uppercase tracking-widest">Propagates across global nodes in 24-48 cycles</p>
                                        </div>
                                    </div>
                                </div>
                            )}

                            {activeTab === 'email' && (
                                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                                    <div className="space-y-4">
                                        <div className="flex items-center gap-4">
                                            <div className="h-10 w-1 rounded-full bg-emerald-500 shadow-lg shadow-emerald-500/50" />
                                            <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Communication Pathways</h2>
                                        </div>
                                        <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-2 ml-14">Initialise high-fidelity SMTP routing for identity mapping</p>

                                        <div className="flex flex-col md:flex-row gap-6 pt-6">
                                            <div className="flex-1 relative group">
                                                <Mail className="absolute left-5 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                                                <Input
                                                    value={config.customEmailDomain || ''}
                                                    onChange={e => setConfig(p => ({ ...p, customEmailDomain: e.target.value }))}
                                                    className="h-16 pl-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white"
                                                    placeholder="ENTITY.ROOT"
                                                />
                                            </div>
                                            <Button 
                                                onClick={handleVerifyEmailDomain} 
                                                disabled={verifyingEmail || !config.customEmailDomain} 
                                                className="h-16 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl active:scale-95 transition-all flex items-center gap-4"
                                                variant="outline"
                                            >
                                                {verifyingEmail ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                                                {verifyingEmail ? 'Authenticating...' : 'Sign Comms Record'}
                                            </Button>
                                        </div>

                                        <AnimatePresence>
                                            {emailVerifyResult && (
                                                <motion.div 
                                                    initial={{ opacity: 0, height: 0 }}
                                                    animate={{ opacity: 1, height: 'auto' }}
                                                    className="space-y-4 p-6 bg-slate-50 dark:bg-slate-950/50 rounded-[28px] border border-transparent dark:border-slate-850 shadow-inner"
                                                >
                                                    <div className={cn(
                                                        "flex items-center gap-4 text-[10px] font-black uppercase tracking-widest",
                                                        emailVerifyResult.spfValid ? "text-success-fg" : "text-danger-fg"
                                                    )}>
                                                        {emailVerifyResult.spfValid ? <ShieldCheck className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
                                                        SPF PROTOCOL: {emailVerifyResult.spfValid ? 'AUTHORISED' : 'UNRECOGNIZED'}
                                                    </div>
                                                    <div className={cn(
                                                        "flex items-center gap-4 text-[10px] font-black uppercase tracking-widest",
                                                        emailVerifyResult.dkimValid ? "text-success-fg" : "text-danger-fg"
                                                    )}>
                                                        {emailVerifyResult.dkimValid ? <ShieldCheck className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
                                                        DKIM PROTOCOL: {emailVerifyResult.dkimValid ? 'VALIDATED' : 'UNRECOGNIZED'}
                                                    </div>
                                                </motion.div>
                                            )}
                                        </AnimatePresence>
                                    </div>

                                    <div className="space-y-6">
                                        <div className="flex items-center gap-4 px-2">
                                            <Code className="h-4 w-4 text-foreground-muted" />
                                            <h3 className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Required Comm Signatures</h3>
                                        </div>
                                        
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                            {[
                                                { type: 'TXT', namespace: config.customEmailDomain || 'ENTITY.ROOT', target: 'v=spf1 include:UPKILO.ROOT ~all', label: 'SPF AUTHORISATION' },
                                                { type: 'CNAME', namespace: `UPKILO._DOMAINKEY.${config.customEmailDomain || 'ENTITY.ROOT'}`, target: 'DKIM.UPKILO.ROOT', label: 'DKIM SIGNATURE' },
                                            ].map(rec => (
                                                <div key={rec.label} className="p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner space-y-6 group">
                                                    <p className="text-[9px] font-black text-primary-500 uppercase tracking-widest">{rec.label}</p>
                                                    <div className="space-y-4 font-mono text-[10px] font-bold text-slate-600 dark:text-slate-500 leading-relaxed">
                                                        <div className="flex items-center gap-4"><span className="text-[9px] font-black text-slate-400 uppercase">TYPE:</span> <span className="text-slate-900 dark:text-white uppercase tracking-widest">{rec.type}</span></div>
                                                        <div className="flex flex-col gap-2"><span className="text-[9px] font-black text-foreground-muted uppercase">NAMESPACE:</span> <span className="bg-white dark:bg-slate-900 p-3 rounded-xl border border-slate-100 dark:border-slate-800 text-[9px] font-black dark:text-slate-300 truncate uppercase tracking-widest">{rec.namespace}</span></div>
                                                        <div className="flex flex-col gap-2"><span className="text-[9px] font-black text-foreground-muted uppercase">TARGET:</span> <span className="bg-white dark:bg-slate-900 p-3 rounded-xl border border-slate-100 dark:border-slate-800 text-[9px] font-black dark:text-slate-300 truncate uppercase tracking-widest">{rec.target}</span></div>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                </div>
                            )}

                            {activeTab === 'css' && (
                                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-4">
                                            <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                                            <div>
                                                <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Neural Override Schema</h2>
                                                <p className="text-[10px] font-black text-foreground-muted uppercase tracking-widest mt-1">Direct CSS Injection Terminal</p>
                                            </div>
                                        </div>
                                        <button
                                            onClick={() => setShowCssPreview(!showCssPreview)}
                                            className="h-12 px-6 rounded-xl bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 shadow-inner flex items-center gap-3 group transition-all"
                                        >
                                            {showCssPreview ? <EyeOff className="h-4 w-4 text-primary-500" /> : <Eye className="h-4 w-4 text-primary-500" />}
                                            <span className="text-[9px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest group-hover:text-primary-500 transition-colors">
                                                {showCssPreview ? 'Lock Vars' : 'Peek Vars'}
                                            </span>
                                        </button>
                                    </div>

                                    <AnimatePresence>
                                        {showCssPreview && (
                                            <motion.div 
                                                initial={{ opacity: 0, height: 0 }}
                                                animate={{ opacity: 1, height: 'auto' }}
                                                className="p-8 bg-slate-950 rounded-[32px] border border-slate-800 space-y-4 font-mono text-[10px] font-bold text-slate-400 shadow-2xl relative overflow-hidden group"
                                            >
                                                <div className="space-y-1 relative z-10">
                                                    <p className="text-primary-500 mb-4 opacity-50 uppercase tracking-widest">Available Injection Hooks:</p>
                                                    <p className="hover:text-white transition-colors">--CORE-PRIMARY: {config.primaryColor || '#6366F1'};</p>
                                                    <p className="hover:text-white transition-colors">--CORE-SECONDARY: {config.secondaryColor || '#8B5CF6'};</p>
                                                    <p className="hover:text-white transition-colors">--FONT-IDENTITY: 'OUTFIT', SANS-SERIF;</p>
                                                    <p className="hover:text-white transition-colors">--RADIUS-MATRIX: 40PX;</p>
                                                </div>
                                                <Play className="absolute -bottom-6 -right-6 h-24 w-24 text-primary-500/5 -rotate-12 group-hover:rotate-0 transition-transform duration-1000" />
                                            </motion.div>
                                        )}
                                    </AnimatePresence>

                                    <div className="relative group overflow-hidden rounded-[32px]">
                                        <textarea
                                            value={config.customCss || ''}
                                            onChange={e => setConfig(p => ({ ...p, customCss: e.target.value }))}
                                            className="w-full h-80 bg-slate-950 border border-slate-800 rounded-[32px] p-10 font-mono text-xs font-bold text-success-fg outline-none focus:ring-8 focus:ring-emerald-500/[0.03] transition-all resize-none leading-relaxed selection:bg-emerald-500/20"
                                            placeholder={`/* Neural Override Initialised... */\n\n.RESERVATION-GATEWAY {\n  BORDER-RADIUS: 40PX;\n  BACKDROP-FILTER: BLUR(20PX);\n}\n\n.AUTH-COMMIT-ACTION {\n  BACKGROUND: VAR(--CORE-PRIMARY);\n}`}
                                        />
                                        <div className="absolute top-6 right-8 flex gap-1.5 opacity-50">
                                            {[1,2,3].map(i => <div key={i} className="w-2 h-2 rounded-full bg-slate-800" />)}
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-3 px-2">
                                        <ShieldCheck className="h-4 w-4 text-foreground-muted" />
                                        <p className="text-[9px] font-black text-foreground-muted uppercase tracking-widest leading-relaxed">Schema is injected at runtime across all reservation nodes</p>
                                    </div>
                                </div>
                            )}
                        </motion.div>
                    </AnimatePresence>
                </div>

                {/* Right Column: Visual Telemetry / Live Preview */}
                <div className="space-y-12 lg:sticky lg:top-8 lg:self-start">
                    <div className="flex items-center gap-4 mb-4">
                        <Search className="h-4 w-4 text-primary-500" />
                        <h2 className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Visual Telemetry</h2>
                    </div>
                    
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl lg:shadow-slate-200/40 dark:lg:shadow-none space-y-10 group cursor-default">
                        <div className="flex items-center justify-between">
                            <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Live Feed: Reservation Node</p>
                            <div className="flex gap-2">
                                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse shadow-glow" />
                                <span className="w-2 h-2 rounded-full bg-slate-100 dark:bg-slate-850" />
                            </div>
                        </div>

                        {/* Interactive UI Preview */}
                        <div className="p-8 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[32px] shadow-inner space-y-8 group-hover:bg-white dark:group-hover:bg-slate-900 group-hover:border-slate-100 dark:group-hover:border-slate-800 transition-all duration-700">
                            <div className="flex items-center gap-4">
                                {config.customLogoUrl ? (
                                    <img src={config.customLogoUrl} alt="logo" className="h-8 object-contain transition-transform group-hover:scale-110" />
                                ) : (
                                    <div className="h-8 w-8 rounded-lg bg-primary-500 flex items-center justify-center shadow-lg shadow-primary-500/20">
                                        <Building2 className="h-4 w-4 text-white" />
                                    </div>
                                )}
                                <span className={cn("text-[11px] font-black uppercase tracking-widest dark:text-white transition-colors")}>Your Entity</span>
                            </div>
                            
                            <div className="space-y-4">
                                <div className="h-12 w-full rounded-xl flex items-center justify-center text-white text-[9px] font-black uppercase tracking-[0.2em] shadow-xl hover:translate-y-[-2px] transition-all cursor-pointer" style={{ backgroundColor: config.primaryColor || '#6366F1' }}>
                                    Initialise Commitment
                                </div>
                                <div className="h-12 w-full rounded-xl flex items-center justify-center text-white text-[9px] font-black uppercase tracking-[0.2em] shadow-xl hover:translate-y-[-2px] transition-all cursor-pointer" style={{ backgroundColor: config.secondaryColor || '#8B5CF6' }}>
                                    External Outreach
                                </div>
                            </div>

                            <p className="text-[8px] font-black uppercase tracking-[0.4em] text-center text-slate-300 bg-slate-100/50 dark:bg-slate-900/50 py-3 rounded-lg border border-slate-200/50 dark:border-slate-800/50 group-hover:opacity-60 transition-opacity">
                                Alpha Version: Reserv-Nodes
                            </p>
                        </div>
                        
                        <div className="p-8 bg-primary-500/10 dark:bg-primary-950 rounded-[32px] border border-primary-500/20 space-y-6">
                            <div className="flex items-center gap-3">
                                <Sparkles className="h-4 w-4 text-primary-400" />
                                <span className="text-[10px] font-black text-primary-600 dark:text-primary-400 uppercase tracking-[0.3em]">Neural Status</span>
                            </div>
                            <div className="space-y-3">
                                {[
                                    { label: 'MEDIA SYNC', status: 'SYNCHRONISED', color: 'text-success-fg' },
                                    { label: 'NAMESPACE', status: config.isVerified ? 'VERIFIED' : 'PENDING', color: config.isVerified ? 'text-success-fg' : 'text-warning-fg' },
                                    { label: 'TLS ENCRYPTION', status: 'ACTIVE', color: 'text-success-fg' },
                                ].map((node, i) => (
                                    <div key={i} className="flex items-center justify-between">
                                        <span className="text-[8px] font-black text-foreground-secondary uppercase tracking-widest">{node.label}</span>
                                        <span className={cn("text-[8px] font-black uppercase tracking-widest", node.color)}>{node.status}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

