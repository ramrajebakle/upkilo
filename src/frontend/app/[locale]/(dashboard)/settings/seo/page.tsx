'use client';

import { useState, useEffect, useCallback } from 'react';
import {
    Search, Globe, Link2, Copy, Check, Save, Loader2,
    CheckCircle, AlertCircle, Eye, Share2, Sparkles,
    MapPin, Phone, Mail, RefreshCw, ExternalLink,
    Info, ChevronRight, Star, Image as ImageIcon,
    Target, Zap, ShieldCheck, MessageSquare, ArrowRight,
    Building2, Layout, Sliders, PlayCircle, TrendingUp,
    BookOpen, Award, BarChart3
} from 'lucide-react';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { motion, AnimatePresence } from 'framer-motion';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

/* ─── helpers ─────────────────────────────────────────────── */
const charColor = (len: number, max: number) =>
    len === 0 ? 'text-slate-400 dark:text-slate-600'
    : len > max ? 'text-red-500'
    : len > max * 0.9 ? 'text-amber-500'
    : 'text-primary-500';

function Tip({ text }: { text: string }) {
    return (
        <div className="flex items-start gap-4 mt-4 p-5 bg-primary-50/50 dark:bg-primary-900/10 border border-primary-100/50 dark:border-primary-500/10 rounded-2xl">
            <Info className="h-4 w-4 text-primary-500 shrink-0 mt-0.5" />
            <p className="text-[10px] font-bold text-primary-700 dark:text-primary-400 uppercase tracking-widest leading-relaxed">{text}</p>
        </div>
    );
}

function FieldLabel({ label, hint }: { label: string; hint?: string }) {
    return (
        <div className="mb-4">
            <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.2em] ml-1">{label}</label>
            {hint && <p className="text-[9px] font-bold text-slate-500 dark:text-slate-400 mt-2 uppercase tracking-widest leading-relaxed">{hint}</p>}
        </div>
    );
}

function StatusBadge({ done, label }: { done: boolean; label: string }) {
    return (
        <div className={cn(
            'flex items-center gap-2 px-4 py-2 rounded-xl text-[9px] font-black uppercase tracking-widest transition-all',
            done
                ? 'bg-emerald-50 dark:bg-emerald-400/10 text-emerald-700 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-400/20 shadow-sm'
                : 'bg-amber-50 dark:bg-amber-400/10 text-amber-700 dark:text-amber-400 border border-amber-100 dark:border-amber-400/20 shadow-sm'
        )}>
            {done
                ? <CheckCircle className="h-3 w-3 shrink-0" />
                : <AlertCircle className="h-3 w-3 shrink-0" />}
            {label}
        </div>
    );
}

/* ─── main component ─────────────────────────────────────────── */
export default function SeoSettingsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading]   = useState(true);
    const [saving, setSaving]     = useState(false);
    const [copied, setCopied]     = useState<string | null>(null);
    const [tab, setTab]           = useState<'google' | 'social'>('google');
    const [audit, setAudit]       = useState<any>(null);
    const [keywords, setKeywords] = useState<any>(null);
    const [auditTab, setAuditTab] = useState<'audit' | 'keywords' | 'citations'>('audit');

    const [form, setForm] = useState({
        name:        '',
        subdomain:   '',
        description: '',
        keywords:    '',
        phone:       '',
        email:       '',
        website:     '',
        logoUrl:     '',
        address: { 
            line1: '', 
            city: '', 
            state: '', 
            postalCode: '', 
            country: '' 
        },
    });

    useEffect(() => {
        Promise.all([
            api.seoTools.audit().catch(() => null),
            api.seoTools.keywords().catch(() => null),
        ]).then(([auditRes, kwRes]) => {
            if (auditRes) setAudit(auditRes.data);
            if (kwRes) setKeywords(kwRes.data);
        });
    }, []);

    useEffect(() => {
        api.settings.getBusiness()
            .then((res: any) => {
                const d = res.data;
                setForm({
                    name:        d.name        || '',
                    subdomain:   d.subdomain   || '',
                    description: d.description || '',
                    keywords:    d.keywords    || '',
                    phone:       d.phone       || '',
                    email:       d.email       || '',
                    website:     d.website     || '',
                    logoUrl:     d.logoUrl     || '',
                    address: {
                        line1:      d.address?.line1      || '',
                        city:       d.address?.city       || '',
                        state:      d.address?.state      || '',
                        postalCode: d.address?.postalCode || '',
                        country:    d.address?.country    || '',
                    },
                });
            })
            .catch(() => toastError('Could not load your settings'))
            .finally(() => setLoading(false));
    }, [toastError]);

    const set = (field: string, val: string) =>
        setForm(prev => ({ ...prev, [field]: val }));

    const setAddr = (field: string, val: string) =>
        setForm(prev => ({ ...prev, address: { ...prev.address, [field]: val } }));

    const handleSave = async () => {
        setSaving(true);
        try {
            await api.settings.updateBusiness(form);
            toastSuccess('SEO settings committed! Google indexing re-validation triggered.');
        } catch (err: any) {
            const msg = err?.response?.data?.error || 'Failed to sync. Matrix collision detected.';
            toastError(msg);
        } finally {
            setSaving(false);
        }
    };

    const copy = async (text: string, key: string) => {
        await navigator.clipboard.writeText(text);
        setCopied(key);
        setTimeout(() => setCopied(null), 2000);
    };

    /* derived values */
    const bookingUrl    = form.subdomain ? `${SITE_URL}/en/book/${form.subdomain}` : '';
    const pageTitle     = form.name ? `Book ${form.name} — Online Booking` : 'YOUR PAGE TITLE';
    const metaDesc      = form.description || `Book appointments online with ${form.name || 'your business'}. Fast, easy, and instant confirmation.`;
    const displayUrl    = bookingUrl || 'upkilo.com/en/book/your-name';

    const checks = [
        { label: 'Entity Name',      done: !!form.name },
        { label: 'Namespace',        done: !!form.subdomain },
        { label: 'Metadata',         done: form.description.length >= 50 },
        { label: 'Tag Matrix',       done: !!form.keywords },
        { label: 'Tactical Voice',   done: !!form.phone },
        { label: 'Comm Route',       done: !!form.email },
        { label: 'Physical Cell',    done: !!(form.address.city && form.address.country) },
        { label: 'Media Asset',      done: !!form.logoUrl },
    ];
    const score = Math.round((checks.filter(c => c.done).length / checks.length) * 100);

    if (loading) return (
        <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
            <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
            <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Visibility Matrix...</p>
        </div>
    );

    return (
        <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">

            {/* Header & Stats Bundle */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2 space-y-4">
                    <div className="flex items-center gap-6">
                        <div className="p-4 bg-gradient-to-br from-primary-600 to-indigo-900 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                            <Search className="h-8 w-8 text-white" />
                        </div>
                        <div>
                            <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Visibility Matrix</h1>
                            <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Global SEO and Discoverability Stance</p>
                        </div>
                    </div>
                </div>

                <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] p-8 shadow-2xl shadow-slate-200/40 dark:shadow-none relative overflow-hidden group">
                    <div className="relative z-10">
                        <div className="flex items-center justify-between mb-4">
                            <span className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">Visibility Integrity</span>
                            <span className={cn('text-xl font-black tabular-nums transition-colors', score >= 80 ? 'text-emerald-500' : score >= 50 ? 'text-amber-500' : 'text-red-500')}>
                                {score}%
                            </span>
                        </div>
                        <div className="w-full h-3 bg-slate-50 dark:bg-slate-950 rounded-full overflow-hidden shadow-inner">
                            <div
                                className={cn('h-full rounded-full transition-all duration-1000 ease-out shadow-glow',
                                    score >= 80 ? 'bg-emerald-500' : score >= 50 ? 'bg-amber-500' : 'bg-red-500'
                                )}
                                style={{ width: `${score}%` }}
                            />
                        </div>
                    </div>
                    <Target className="absolute -bottom-4 -right-4 h-24 w-24 text-slate-100 dark:text-slate-850/30 -rotate-12 group-hover:rotate-0 transition-transform duration-1000" />
                </div>
            </div>

            {/* Check Grid */}
            <div className="flex flex-wrap gap-3 p-2">
                {checks.map(c => <StatusBadge key={c.label} done={c.done} label={c.label} />)}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
                {/* ── LEFT: Form ──────────────────────────────── */}
                <div className="space-y-12">

                    {/* Booking URL (Namespace) */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-8">
                        <FieldLabel
                            label="Namespace Path"
                            hint="Strategic endpoint for inbound reservation traffic. Auto-formatted for URL compatibility."
                        />
                        <div className="flex items-center gap-4 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[24px] p-2 pr-6 shadow-inner focus-within:ring-4 focus-within:ring-primary-500/10 transition-all">
                            <div className="px-5 py-3 bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800 shadow-sm">
                                <span className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">
                                    UPKILO.ROOT/
                                </span>
                            </div>
                            <input
                                type="text"
                                placeholder="ENTITY-NAMESPACE"
                                value={form.subdomain}
                                onChange={e => set('subdomain', e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-'))}
                                className="flex-1 bg-transparent py-4 text-xs font-black uppercase tracking-widest text-slate-900 dark:text-white outline-none"
                            />
                        </div>
                        
                        {bookingUrl && (
                            <div className="pt-6 border-t border-slate-50 dark:border-slate-850 flex items-center gap-4">
                                <div className="flex-1 p-4 bg-slate-50 dark:bg-slate-950/50 rounded-2xl border border-transparent dark:border-slate-850 truncate">
                                    <span className="text-[10px] font-bold text-slate-400 dark:text-slate-600 uppercase tracking-widest leading-none">{bookingUrl}</span>
                                </div>
                                <Button
                                    onClick={() => copy(bookingUrl, 'url')}
                                    className="h-12 px-6 rounded-2xl font-black uppercase tracking-widest text-[9px] dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 transition-all active:scale-95 flex items-center gap-2"
                                    variant="outline"
                                >
                                    {copied === 'url' ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                    {copied === 'url' ? 'Synced' : 'Clone Path'}
                                </Button>
                            </div>
                        )}
                    </div>

                    {/* Entity Data Bundle (Metadata) */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                        <div className="space-y-4">
                            <FieldLabel label="Primary Entity Title" />
                            <div className="relative group">
                                <Building2 className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                <input
                                    type="text"
                                    placeholder="e.g. ACME QUANTUM SOLUTIONS"
                                    value={form.name}
                                    onChange={e => set('name', e.target.value)}
                                    className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                            </div>
                        </div>

                        <div className="space-y-4">
                            <div className="flex items-center justify-between">
                                <FieldLabel label="Linguistic Metadata" />
                                <span className={cn('text-[10px] font-black tabular-nums tracking-widest', charColor(form.description.length, 155))}>
                                    {form.description.length}/155
                                </span>
                            </div>
                            <div className="relative">
                                <textarea
                                    rows={4}
                                    maxLength={200}
                                    placeholder="Provide a high-fidelity summary of operative services and regional focus..."
                                    value={form.description}
                                    onChange={e => set('description', e.target.value)}
                                    className="w-full p-6 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[24px] text-xs font-bold uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner resize-none leading-relaxed"
                                />
                            </div>
                        </div>

                        <div className="space-y-4">
                            <FieldLabel label="Tactical Tag Matrix" />
                            <div className="relative group">
                                <Target className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                <input
                                    type="text"
                                    placeholder="comma, separated, key, values"
                                    value={form.keywords}
                                    onChange={e => set('keywords', e.target.value)}
                                    className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                            </div>
                        </div>
                    </div>

                    {/* NEW: Contact Details Matrix */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                        <FieldLabel label="Contact Matrix" hint="Operational comms routes picked up by global search clusters." />
                        
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 pt-4">
                            <div className="space-y-3">
                                <label className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Tactical Voice</label>
                                <div className="relative group">
                                    <Phone className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                    <input
                                        type="tel"
                                        placeholder="+00 0000 0000"
                                        value={form.phone}
                                        onChange={e => set('phone', e.target.value)}
                                        className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                    />
                                </div>
                            </div>
                            <div className="space-y-3">
                                <label className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Comm Route</label>
                                <div className="relative group">
                                    <Mail className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                    <input
                                        type="email"
                                        placeholder="SUPPORT@ENTITY.ROOT"
                                        value={form.email}
                                        onChange={e => set('email', e.target.value)}
                                        className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                    />
                                </div>
                            </div>
                            <div className="md:col-span-2 space-y-3">
                                <label className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Master Namespace URL</label>
                                <div className="relative group">
                                    <Globe className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                    <input
                                        type="url"
                                        placeholder="HTTPS://ENTITY.ROOT"
                                        value={form.website}
                                        onChange={e => set('website', e.target.value)}
                                        className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* NEW: Physical Matrix Allocation */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                        <FieldLabel label="Physical Cell" hint="Geographic coordinates for regional search relevance." />
                        
                        <div className="space-y-6 pt-4">
                            <div className="relative group">
                                <MapPin className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                                <input
                                    type="text"
                                    placeholder="INFRASTRUCTURE POINT (LINE 1)"
                                    value={form.address.line1}
                                    onChange={e => setAddr('line1', e.target.value)}
                                    className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                            </div>
                            
                            <div className="grid grid-cols-2 gap-6">
                                <input
                                    type="text"
                                    placeholder="CITY"
                                    value={form.address.city}
                                    onChange={e => setAddr('city', e.target.value)}
                                    className="h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] px-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                                <input
                                    type="text"
                                    placeholder="REGION/STATE"
                                    value={form.address.state}
                                    onChange={e => setAddr('state', e.target.value)}
                                    className="h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] px-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                                <input
                                    type="text"
                                    placeholder="TEMP-POSTAL"
                                    value={form.address.postalCode}
                                    onChange={e => setAddr('postalCode', e.target.value)}
                                    className="h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] px-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                                <input
                                    type="text"
                                    placeholder="COUNTRY"
                                    value={form.address.country}
                                    onChange={e => setAddr('country', e.target.value)}
                                    className="h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] px-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                                />
                            </div>
                        </div>
                    </div>

                    {/* NEW: Media Asset Matrix */}
                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                        <FieldLabel label="Media Asset" hint="Logo used in high-fidelity social anchors and search cards." />
                        
                        <div className="relative group pt-4">
                            <ImageIcon className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
                            <input
                                type="url"
                                placeholder="HTTPS://ENTITY.ROOT/ASSETS/LOGO.SVG"
                                value={form.logoUrl}
                                onChange={e => set('logoUrl', e.target.value)}
                                className="w-full h-14 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[20px] pl-14 pr-6 text-xs font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white shadow-inner"
                            />
                        </div>
                        
                        {form.logoUrl && (
                            <div className="mt-6 flex items-center justify-center p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner group">
                                <img src={form.logoUrl} alt="DNA" className="h-16 object-contain group-hover:scale-110 transition-transform duration-700" />
                            </div>
                        )}
                    </div>
                </div>

                {/* ── RIGHT: Live previews ─────────────────────── */}
                <div className="space-y-12 lg:sticky lg:top-8 lg:self-start">

                    <div className="p-1.5 bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-850 rounded-[32px] shadow-2xl flex">
                        {(['google', 'social'] as const).map(t => (
                            <button
                                key={t}
                                onClick={() => setTab(t)}
                                className={cn(
                                    'flex-1 py-4 text-[10px] font-black uppercase tracking-[0.2em] rounded-[28px] transition-all duration-500',
                                    tab === t
                                        ? 'bg-white dark:bg-slate-900 text-primary-600 dark:text-primary-400 shadow-xl'
                                        : 'text-slate-500 dark:text-slate-600 hover:text-slate-900 dark:hover:text-slate-300'
                                )}
                            >
                                {t === 'google' ? 'Search Index' : 'Social Broadcast'}
                            </button>
                        ))}
                    </div>

                    <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none min-h-[400px]">
                        <AnimatePresence mode="wait">
                            {tab === 'google' ? (
                                <motion.div
                                    key="google"
                                    initial={{ opacity: 0, y: 20 }}
                                    animate={{ opacity: 1, y: 0 }}
                                    exit={{ opacity: 0, scale: 0.95 }}
                                    className="space-y-8"
                                >
                                    <div className="flex items-center justify-between">
                                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.4em]">Search Engine Simulation</p>
                                        <div className="flex gap-1.5">
                                            {[1,2,3].map(i => <div key={i} className="w-1.5 h-1.5 rounded-full bg-slate-200 dark:bg-slate-800" />)}
                                        </div>
                                    </div>

                                    <div className="bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 rounded-[32px] p-8 shadow-inner space-y-4 group cursor-default">
                                        <div className="flex items-center gap-3">
                                            {form.logoUrl
                                                ? <img src={form.logoUrl} alt="" className="w-6 h-6 rounded-full object-cover shadow-sm group-hover:scale-110 transition-transform" />
                                                : <div className="w-6 h-6 rounded-full bg-primary-500 flex items-center justify-center shadow-lg shadow-primary-500/20">
                                                    <span className="text-white text-[9px] font-black">{form.name?.[0] || 'U'}</span>
                                                  </div>
                                            }
                                            <span className="text-[10px] font-bold text-slate-400 dark:text-slate-600 tracking-tight lowercase">{displayUrl}</span>
                                        </div>
                                        
                                        <p className="text-blue-600 dark:text-primary-400 text-xl font-black uppercase tracking-tight leading-tight group-hover:underline">
                                            {pageTitle}
                                        </p>
                                        
                                        <p className="text-[11px] font-bold text-slate-500 dark:text-slate-500 leading-relaxed uppercase tracking-wider">
                                            {metaDesc.slice(0, 155)}{metaDesc.length > 155 ? '...' : ''}
                                        </p>
                                        
                                        {(form.address.city || form.phone) && (
                                            <div className="pt-6 mt-6 flex flex-wrap gap-4 text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.2em] border-t border-slate-100 dark:border-slate-800">
                                                {form.address.city && (
                                                    <span className="flex items-center gap-2">
                                                        <MapPin className="h-3 w-3 text-primary-500" />
                                                        {[form.address.city, form.address.state].filter(Boolean).join(' // ')}
                                                    </span>
                                                )}
                                                {form.phone && (
                                                    <span className="flex items-center gap-2">
                                                        <Phone className="h-3 w-3 text-primary-500" />
                                                        {form.phone}
                                                    </span>
                                                )}
                                                <span className="flex items-center gap-2 text-emerald-500">
                                                    <Star className="h-3 w-3 fill-current animate-pulse text-emerald-500" /> Book Online
                                                </span>
                                            </div>
                                        )}
                                    </div>
                                    
                                    <div className="flex items-center gap-3 px-2">
                                        <Sparkles className="h-4 w-4 text-primary-500 animate-spin-slow" />
                                        <span className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">Updates automatically every time you save</span>
                                    </div>
                                </motion.div>
                            ) : (
                                <motion.div
                                    key="social"
                                    initial={{ opacity: 0, y: 20 }}
                                    animate={{ opacity: 1, y: 0 }}
                                    exit={{ opacity: 0, scale: 0.95 }}
                                    className="space-y-8"
                                >
                                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.4em]">Protocol Exchange Simulation</p>
                                    
                                    <div className="border border-slate-100 dark:border-slate-850 rounded-[40px] overflow-hidden shadow-2xl group">
                                        <div className="w-full h-48 bg-slate-50 dark:bg-slate-950 flex items-center justify-center relative overflow-hidden">
                                            {form.logoUrl ? (
                                                <img src={form.logoUrl} alt="OG" className="max-h-32 max-w-[70%] object-contain group-hover:scale-105 transition-transform duration-1000" />
                                            ) : (
                                                <div className="text-center space-y-4 opacity-50">
                                                    <div className="p-6 bg-white dark:bg-slate-900 rounded-full inline-block shadow-inner">
                                                        <ImageIcon className="h-8 w-8 text-slate-300 dark:text-slate-700 mx-auto" />
                                                    </div>
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest">Awaiting Media Asset</p>
                                                </div>
                                            )}
                                            <div className="absolute bottom-4 left-4">
                                                <div className="px-3 py-1 bg-black/50 backdrop-blur-md rounded-lg">
                                                    <p className="text-white text-[8px] font-black uppercase tracking-widest leading-none mt-0.5">{displayUrl.split('/')[0]}</p>
                                                </div>
                                            </div>
                                        </div>
                                        
                                        <div className="p-8 bg-white dark:bg-slate-900 border-t border-slate-50 dark:border-slate-850">
                                            <p className="font-black text-slate-900 dark:text-white text-sm uppercase tracking-tight mb-2 leading-tight">{pageTitle}</p>
                                            <p className="text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest leading-relaxed line-clamp-2">
                                                {metaDesc}
                                            </p>
                                        </div>
                                    </div>
                                </motion.div>
                            )}
                        </AnimatePresence>
                    </div>

                    {/* Operational Protocols (Refined to match screenshot) */}
                    <div className="p-8 bg-gradient-to-br from-slate-900 to-indigo-950 border border-slate-800 rounded-[40px] space-y-8 shadow-2xl">
                        <div className="flex items-center gap-3">
                            <ShieldCheck className="h-4 w-4 text-primary-400" />
                            <span className="text-[10px] font-black text-white uppercase tracking-[0.3em]">Operational Protocols</span>
                        </div>
                        
                        <div className="space-y-6">
                            {[
                                { icon: CheckCircle, color: 'text-emerald-500', text: 'Booking parameters synchronized instantly' },
                                { icon: Globe, color: 'text-blue-400', text: 'Regional spatial data re-validation' },
                                { icon: Target, color: 'text-red-400', text: 'Global sitemap re-indexing triggered' },
                                { icon: Share2, color: 'text-violet-400', text: 'Social anchors and OG headers updated' },
                                { icon: RefreshCw, color: 'text-amber-400', text: 'Broadcast picked up within 1–3 days' },
                            ].map((item, i) => (
                                <div key={i} className="flex items-start gap-4 text-[9px] font-bold text-slate-400 uppercase tracking-widest leading-loose">
                                    <item.icon className={cn("h-4 w-4 shrink-0 mt-0.5", item.color)} />
                                    {item.text}
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* NEW: Strategy Nexus (Next Steps) */}
                    <div className="p-10 bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl space-y-8 overflow-hidden relative group">
                        <div className="relative z-10 space-y-8">
                            <div className="flex items-center gap-4">
                                <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg" />
                                <h3 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Strategy Nexus</h3>
                            </div>
                            
                            <div className="space-y-4">
                                {[
                                    { label: 'Asset Deployment', desc: 'Create landing pages for cada service' },
                                    { label: 'Uplink Verification', desc: 'Set up automatic review requests' },
                                    { label: 'Network Expansion', desc: 'Sync with Social & Google Business' },
                                ].map((step, i) => (
                                    <button key={i} className="w-full flex items-center justify-between p-6 bg-slate-50 dark:bg-slate-900 rounded-[24px] border border-transparent hover:border-primary-500/20 hover:bg-white dark:hover:bg-slate-850 transition-all group overflow-hidden relative">
                                        <div className="text-left relative z-10">
                                            <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">{step.label}</p>
                                            <p className="text-[10px] font-bold text-slate-900 dark:text-white uppercase tracking-widest mt-1">{step.desc}</p>
                                        </div>
                                        <ChevronRight className="h-4 w-4 text-slate-300 group-hover:text-primary-500 group-hover:translate-x-1 transition-all relative z-10" />
                                        <div className="absolute top-0 right-0 h-full w-2 bg-primary-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                                    </button>
                                ))}
                            </div>
                        </div>
                        
                        {/* Help Trigger (Fab style like in screenshot) */}
                        <div className="absolute -bottom-4 -right-4">
                             <div className="p-12 rotate-45 bg-primary-500/5 group-hover:bg-primary-500/10 transition-colors rounded-full blur-3xl" />
                        </div>
                    </div>
                </div>
            </div>

            {/* ── Live SEO Audit + Keywords + Citations ─────────────────── */}
            <div className="space-y-6">
                <div className="flex items-center gap-3">
                    <div className="p-2.5 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-xl shadow-lg shadow-emerald-500/30">
                        <BarChart3 className="h-5 w-5 text-white" />
                    </div>
                    <h2 className="text-lg font-bold text-slate-900 dark:text-white">SEO Intelligence Dashboard</h2>
                </div>

                {/* Tab Nav */}
                <div className="flex gap-2 bg-slate-100 dark:bg-slate-800 rounded-xl p-1 w-fit">
                    {([['audit','SEO Audit'],['keywords','Keyword Ideas'],['citations','Directory Listings']] as const).map(([k,label]) => (
                        <button key={k} onClick={() => setAuditTab(k)} className={cn('px-4 py-2 rounded-lg text-sm font-semibold transition-all', auditTab === k ? 'bg-white dark:bg-slate-900 text-slate-900 dark:text-white shadow' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200')}>
                            {label}
                        </button>
                    ))}
                </div>

                {/* SEO Audit Tab */}
                {auditTab === 'audit' && (
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm space-y-5">
                        {!audit ? (
                            <p className="text-sm text-slate-400 text-center py-6">Save your settings first to see your live SEO audit score.</p>
                        ) : (
                            <>
                                {/* Score */}
                                <div className="flex items-center gap-6">
                                    <div className={cn('w-20 h-20 rounded-2xl flex flex-col items-center justify-center font-black text-2xl shadow-lg', audit.score >= 85 ? 'bg-emerald-500 text-white' : audit.score >= 70 ? 'bg-amber-400 text-white' : audit.score >= 50 ? 'bg-orange-500 text-white' : 'bg-red-500 text-white')}>
                                        {audit.score}
                                        <span className="text-xs font-bold">/ 100</span>
                                    </div>
                                    <div>
                                        <p className="text-lg font-bold text-slate-900 dark:text-white">SEO Health: Grade {audit.grade}</p>
                                        <p className="text-sm text-slate-500 dark:text-slate-400">
                                            {audit.score >= 85 ? 'Excellent! Your profile is well optimised for local search.' :
                                             audit.score >= 70 ? 'Good. Fix the remaining items to reach the top of local results.' :
                                             audit.score >= 50 ? 'Fair. Complete the critical items below to significantly boost visibility.' :
                                             'Needs attention. Complete the checklist below to start ranking.'}
                                        </p>
                                        <div className="mt-2 flex gap-3 text-xs text-slate-500 dark:text-slate-400">
                                            <span>{audit.summary?.totalReviews} reviews</span>
                                            <span>·</span>
                                            <span>{audit.summary?.servicesListed} services</span>
                                            <span>·</span>
                                            <span>{audit.summary?.publishedPosts} blog posts</span>
                                        </div>
                                    </div>
                                </div>
                                {/* Progress bar */}
                                <div className="w-full h-2.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                                    <div className={cn('h-full rounded-full transition-all duration-700', audit.score >= 85 ? 'bg-emerald-500' : audit.score >= 70 ? 'bg-amber-400' : audit.score >= 50 ? 'bg-orange-500' : 'bg-red-500')} style={{ width: `${audit.score}%` }} />
                                </div>
                                {/* Checklist */}
                                <div className="space-y-2">
                                    {audit.checks?.map((c: any) => (
                                        <div key={c.id} className={cn('flex items-start gap-3 p-3 rounded-xl', c.passed ? 'bg-emerald-50 dark:bg-emerald-900/20' : c.priority === 'critical' ? 'bg-red-50 dark:bg-red-900/20' : c.priority === 'high' ? 'bg-amber-50 dark:bg-amber-900/20' : 'bg-slate-50 dark:bg-slate-800/50')}>
                                            <div className="mt-0.5">
                                                {c.passed
                                                    ? <CheckCircle className="w-4 h-4 text-emerald-500" />
                                                    : <AlertCircle className={cn('w-4 h-4', c.priority === 'critical' ? 'text-red-500' : c.priority === 'high' ? 'text-amber-500' : 'text-slate-400')} />}
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <div className="flex items-center gap-2">
                                                    <span className={cn('text-sm font-semibold', c.passed ? 'text-emerald-700 dark:text-emerald-300' : 'text-slate-900 dark:text-white')}>{c.label}</span>
                                                    {!c.passed && c.priority === 'critical' && <span className="text-xs px-1.5 py-0.5 bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400 rounded font-medium">Critical</span>}
                                                    {!c.passed && c.priority === 'high' && <span className="text-xs px-1.5 py-0.5 bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 rounded font-medium">High</span>}
                                                    {c.weight > 0 && <span className="text-xs text-slate-400">+{c.weight} pts</span>}
                                                </div>
                                                {!c.passed && <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{c.tip}</p>}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                )}

                {/* Keywords Tab */}
                {auditTab === 'keywords' && (
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm space-y-4">
                        {!keywords?.suggestions?.length ? (
                            <p className="text-sm text-slate-400 text-center py-6">Add at least 3 services to get keyword suggestions tailored to your business and location.</p>
                        ) : (
                            <>
                                <div className="flex items-center gap-2 mb-2">
                                    <TrendingUp className="w-4 h-4 text-primary-500" />
                                    <p className="text-sm font-semibold text-slate-900 dark:text-white">
                                        Suggested keywords for {keywords.city ? <strong>{keywords.city}</strong> : 'your area'}
                                    </p>
                                </div>
                                <p className="text-xs text-slate-500 dark:text-slate-400">Copy these into your Keywords field above and into your business description to rank for them.</p>
                                <div className="space-y-2">
                                    {keywords.suggestions.map((s: any, i: number) => (
                                        <div key={i} className="flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-800 rounded-xl">
                                            <div>
                                                <span className="text-sm font-medium text-slate-900 dark:text-white">{s.keyword}</span>
                                                <span className="ml-3 text-xs text-slate-400">{s.intent}</span>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <span className={cn('text-xs font-semibold px-2 py-0.5 rounded-full', s.volume === 'High' ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300' : 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300')}>{s.volume}</span>
                                                <button onClick={() => copy(s.keyword, `kw-${i}`)} className="p-1.5 text-slate-400 hover:text-primary-500 transition-colors rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700">
                                                    {copied === `kw-${i}` ? <Check className="w-3.5 h-3.5 text-emerald-500" /> : <Copy className="w-3.5 h-3.5" />}
                                                </button>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                )}

                {/* Citations / Directory Tab */}
                {auditTab === 'citations' && (
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm space-y-4">
                        <div>
                            <p className="text-sm font-semibold text-slate-900 dark:text-white mb-1">Local Directory Listings</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400">Being listed on these directories = more backlinks, more trust signals, higher Google rank. Aim to be on all of them. Your business name, address, and phone (NAP) must be <strong>identical</strong> on every site.</p>
                        </div>
                        <div className="space-y-3">
                            {[
                                { name: 'Google Business Profile', priority: 'Must-have', desc: 'The single most important listing for local SEO. Free.', url: 'https://business.google.com', impact: 'Very High' },
                                { name: 'Yelp for Business',       priority: 'Must-have', desc: 'Second most-trusted reviews platform. Strong SEO signal.', url: 'https://biz.yelp.com', impact: 'High' },
                                { name: 'Facebook Business Page',  priority: 'Must-have', desc: 'Social trust signal. Reviews show in Google.', url: 'https://www.facebook.com/pages/create', impact: 'High' },
                                { name: 'Apple Maps Connect',      priority: 'Important', desc: 'iPhone users see Apple Maps by default.', url: 'https://mapsconnect.apple.com', impact: 'High' },
                                { name: 'Bing Places',             priority: 'Important', desc: 'Bing has 6% search market share — worth 10 minutes.', url: 'https://www.bingplaces.com', impact: 'Medium' },
                                { name: 'TripAdvisor',             priority: 'Worth it',  desc: 'Great for spas, salons, and wellness businesses.', url: 'https://www.tripadvisor.com/GetListedNew', impact: 'Medium' },
                                { name: 'Foursquare',              priority: 'Worth it',  desc: 'Powers many third-party apps and directories.', url: 'https://business.foursquare.com', impact: 'Medium' },
                                { name: 'Yellow Pages',            priority: 'Worth it',  desc: 'Old but still used by Google to verify business data.', url: 'https://www.yellowpages.com/free-business-listing', impact: 'Medium' },
                                { name: 'Thumbtack',               priority: 'Worth it',  desc: 'Great for service businesses. Brings direct leads.', url: 'https://www.thumbtack.com/pro', impact: 'Medium' },
                                { name: 'Nextdoor',                priority: 'Worth it',  desc: 'Hyper-local neighbourhood recommendations.', url: 'https://business.nextdoor.com', impact: 'Medium' },
                            ].map((dir) => (
                                <div key={dir.name} className="flex items-start justify-between gap-4 p-4 bg-slate-50 dark:bg-slate-800 rounded-xl">
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2 mb-0.5">
                                            <span className="text-sm font-semibold text-slate-900 dark:text-white">{dir.name}</span>
                                            <span className={cn('text-xs font-semibold px-2 py-0.5 rounded-full', dir.priority === 'Must-have' ? 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300' : dir.priority === 'Important' ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300' : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300')}>{dir.priority}</span>
                                        </div>
                                        <p className="text-xs text-slate-500 dark:text-slate-400">{dir.desc}</p>
                                    </div>
                                    <a href={dir.url} target="_blank" rel="noopener noreferrer" className="shrink-0 flex items-center gap-1.5 px-3 py-2 bg-primary-500 hover:bg-primary-600 text-white rounded-lg text-xs font-semibold transition-colors">
                                        List Now <ExternalLink className="w-3 h-3" />
                                    </a>
                                </div>
                            ))}
                        </div>
                        <div className="p-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/40 rounded-xl">
                            <p className="text-xs font-semibold text-amber-800 dark:text-amber-300">Important: NAP Consistency</p>
                            <p className="text-xs text-amber-700 dark:text-amber-400 mt-1">Use the <strong>exact same</strong> business name, address, and phone number on every directory. Even small differences (St vs Street, Suite vs Ste) confuse Google and hurt your ranking.</p>
                        </div>
                    </div>
                )}
            </div>

            {/* Commit Action */}
            <div className="flex flex-col md:flex-row items-center justify-between gap-8 pt-10 border-t border-slate-100 dark:border-slate-800">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-emerald-50 dark:bg-emerald-950 rounded-2xl border border-emerald-100 dark:border-emerald-500/20">
                        <Zap className="h-6 w-6 text-emerald-500 animate-pulse" />
                    </div>
                    <div>
                        <p className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-widest">Global Synchronization</p>
                        <p className="text-[9px] font-bold text-slate-500 dark:text-slate-600 uppercase tracking-widest mt-1">Updates propagate across all availability zones in 1-3 cycles</p>
                    </div>
                </div>
                
                <Button
                    onClick={handleSave}
                    disabled={saving}
                    className="h-16 px-16 rounded-[24px] font-black uppercase tracking-[0.2em] text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-4"
                >
                    {saving
                        ? <><Loader2 className="h-5 w-5 animate-spin" /> Transmitting...</>
                        : <><Save className="h-5 w-5" /> Commit Schema</>
                    }
                </Button>
            </div>
        </div>
    );
}

