'use client';

import { useState, useEffect } from 'react';
import {
    Globe, Plus, Loader2, CheckCircle2, AlertCircle,
    Trash2, ExternalLink, RefreshCw, ShieldCheck,
    Settings2, Copy, Check, Mail, Send, Activity,
    Shield, Zap, Globe2, ChevronRight, Info, Save
} from 'lucide-react';
import { useParams } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';

export function CustomDomainSettings() {
    const params = useParams();
    const locale = params.locale as string || 'en';
    const { success: toastSuccess, error: toastError } = useToast();

    const [domains, setDomains] = useState<any[]>([]);
    const [newHostname, setNewHostname] = useState('');
    const [loading, setLoading] = useState(false);
    const [adding, setAdding] = useState(false);
    const [verifying, setVerifying] = useState<string | null>(null);
    const [copied, setCopied] = useState<string | null>(null);

    // Email Domain state
    const [emailDomain, setEmailDomain] = useState('');
    const [emailStatus, setEmailStatus] = useState<{ spfValid: boolean, dkimValid: boolean } | null>(null);
    const [savingEmail, setSavingEmail] = useState(false);
    const [verifyingEmail, setVerifyingEmail] = useState(false);

    useEffect(() => {
        fetchDomains();
        fetchEmailConfig();
    }, []);

    const fetchEmailConfig = async () => {
        try {
            const res = await api.whitelabel.getConfig();
            if (res.data.customEmailDomain) {
                setEmailDomain(res.data.customEmailDomain);
            }
        } catch (err) {
            console.error('Failed to fetch email config', err);
        }
    };

    const fetchDomains = async () => {
        setLoading(true);
        try {
            const res = await api.domains.list();
            setDomains(res.data);
        } catch (err) {
            console.error('Failed to fetch domains', err);
        } finally {
            setLoading(false);
        }
    };

    const handleAddDomain = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newHostname) return;
        setAdding(true);
        try {
            await api.domains.add(newHostname);
            setNewHostname('');
            toastSuccess('Domain provisioned. Configure DNS to activate.');
            fetchDomains();
        } catch (err: any) {
            toastError(err.response?.data || 'Failed to add domain');
        } finally {
            setAdding(false);
        }
    };

    const handleVerify = async (id: string) => {
        setVerifying(id);
        try {
            await api.domains.verify(id);
            toastSuccess('Domain uplink synchronized.');
            fetchDomains();
        } catch (err: any) {
            toastError(err.response?.data || 'Verification probe failed');
        } finally {
            setVerifying(null);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Abort this domain uplink? This action is irreversible.')) return;
        try {
            await api.domains.delete(id);
            toastSuccess('Uplink terminated.');
            fetchDomains();
        } catch (err) {
            toastError('Failed to remove domain');
        }
    };

    const copyToClipboard = (text: string, id: string) => {
        navigator.clipboard.writeText(text);
        setCopied(id);
        toastSuccess('Protocol cloned to clipboard.');
        setTimeout(() => setCopied(null), 2000);
    };

    const handleSaveEmailDomain = async () => {
        setSavingEmail(true);
        try {
            const config = await api.whitelabel.getConfig();
            await api.whitelabel.updateConfig({
                ...config.data,
                customEmailDomain: emailDomain
            });
            toastSuccess('Email domain committed. Update DNS protocols.');
        } catch (err) {
            toastError('Failed to update email domain');
        } finally {
            setSavingEmail(false);
        }
    };

    const handleVerifyEmail = async () => {
        setVerifyingEmail(true);
        try {
            const res = await api.whitelabel.verifyEmailDomain();
            setEmailStatus({ spfValid: res.data.spfValid, dkimValid: res.data.dkimValid });
            if (res.data.success) {
                toastSuccess('Comms domain verified and operational.');
            } else {
                toastError('Verification probe failed. Check DNS latency.');
            }
        } catch (err) {
            toastError('Failed to verify email domain');
        } finally {
            setVerifyingEmail(false);
        }
    };

    return (
        <div className="space-y-12 animate-fade-in relative">
            {/* Header / Identity Oracle */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-gradient-to-br from-primary-600 to-primary-950 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                        <Globe2 className="h-8 w-8 text-white" />
                    </div>
                    <div>
                        <h2 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Identity Nexus</h2>
                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Foundational Domain Uplinks and Protocol Authority</p>
                    </div>
                </div>
                <div className="flex items-center gap-4">
                    <div className="px-5 py-2.5 bg-slate-50 dark:bg-slate-950 rounded-xl border border-transparent dark:border-slate-850 text-[10px] font-black uppercase tracking-widest flex items-center gap-2">
                        <Shield className="h-4 w-4 text-success-fg" /> Security: Optimal
                    </div>
                </div>
            </div>

            {/* Protocol Provisioning (Add Form) */}
            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-8 relative overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row gap-6">
                    <div className="relative flex-1 group">
                        <Globe className="absolute left-6 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 group-focus-within:text-primary-500 transition-colors" />
                        <input
                            type="text"
                            value={newHostname}
                            onChange={(e) => setNewHostname(e.target.value)}
                            placeholder="BOOKING.YOURDOMAIN.COM"
                            className="w-full h-16 pl-16 pr-6 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 dark:text-white rounded-[24px] text-xs font-black uppercase tracking-widest focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 outline-none transition-all shadow-inner"
                        />
                    </div>
                    <Button 
                        onClick={handleAddDomain} 
                        disabled={adding || !newHostname} 
                        className="h-16 px-10 rounded-[24px] font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all bg-primary-600 hover:bg-primary-700"
                    >
                        {adding ? <Loader2 className="h-5 w-5 animate-spin" /> : <Plus className="h-5 w-5 mr-3" />}
                        Provision Node
                    </Button>
                </div>
                <div className="relative z-10 flex items-center gap-4 pl-2 opacity-60">
                    <Zap className="h-4 w-4 text-primary-500" />
                    <p className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">Requires CNAME allocation at regional DNS registry</p>
                </div>
                <div className="absolute top-0 right-0 w-64 h-64 bg-primary-500/5 blur-3xl rounded-full" />
            </div>

            {/* Matrix Fleet (List) */}
            <div className="space-y-6">
                {loading ? (
                    <div className="py-24 flex flex-col items-center gap-6 text-foreground-muted">
                        <Loader2 className="h-12 w-12 animate-spin text-primary-500" />
                        <span className="text-[10px] font-black uppercase tracking-[0.4em]">Syncing Host Matrix...</span>
                    </div>
                ) : domains.length === 0 ? (
                    <div className="py-24 flex flex-col items-center gap-8 bg-slate-50 dark:bg-slate-950/20 border border-slate-100 dark:border-slate-850 rounded-[40px] text-slate-300 group">
                        <div className="p-8 bg-white dark:bg-slate-900 rounded-full shadow-inner transform group-hover:rotate-12 transition-transform duration-700">
                            <Shield className="h-16 w-16" />
                        </div>
                        <p className="text-[11px] font-black uppercase tracking-[0.5em]">Zero Custom Uplinks Detected</p>
                    </div>
                ) : (
                    domains.map((domain) => (
                        <div key={domain.id} className="group relative bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] overflow-hidden shadow-2xl shadow-slate-200/40 dark:shadow-none hover:border-primary-500/20 transition-all">
                            <div className="p-10">
                                <div className="flex flex-col xl:flex-row items-center justify-between gap-10">
                                    <div className="flex items-center gap-8 flex-1">
                                        <div className={cn(
                                            "h-16 w-16 rounded-[24px] flex items-center justify-center shadow-xl transition-all group-hover:scale-110",
                                            domain.isVerified 
                                                ? "bg-emerald-500 text-white shadow-emerald-500/20" 
                                                : "bg-slate-100 dark:bg-slate-950 text-foreground-muted border border-transparent dark:border-slate-850"
                                        )}>
                                            {domain.isVerified ? <CheckCircle2 className="h-8 w-8" /> : <RefreshCw className="h-8 w-8 animate-spin-slow opacity-50" />}
                                        </div>
                                        <div className="space-y-2">
                                            <div className="flex items-center gap-4">
                                                <h3 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">{domain.hostname}</h3>
                                                <span className={cn(
                                                    "px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest border",
                                                    domain.isVerified 
                                                        ? "bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-500/20" 
                                                        : "bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-100 dark:border-amber-500/20"
                                                )}>
                                                    {domain.isVerified ? 'Uplink: Active' : 'Uplink: Syncing'}
                                                </span>
                                            </div>
                                            <div className="flex items-center gap-4">
                                                <span className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">Protocol: SSL {domain.sslStatus}</span>
                                                <div className="w-1 h-1 rounded-full bg-slate-200 dark:bg-slate-800" />
                                                <span className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">Edge Node: Global</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-4 w-full xl:w-auto justify-end">
                                        {!domain.isVerified && (
                                            <Button
                                                onClick={() => handleVerify(domain.id)}
                                                disabled={verifying === domain.id}
                                                className="h-14 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] bg-slate-900 dark:bg-slate-800 hover:bg-black text-white shadow-xl flex items-center gap-3"
                                            >
                                                {verifying === domain.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <ShieldCheck className="h-4 w-4" />}
                                                Validate DNS
                                            </Button>
                                        )}
                                        <button
                                            onClick={() => handleDelete(domain.id)}
                                            className="h-14 w-14 flex items-center justify-center text-foreground-muted hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/20 rounded-2xl transition-all border border-transparent hover:border-rose-100 dark:hover:border-rose-900/40"
                                        >
                                            <Trash2 className="h-5 w-5" />
                                        </button>
                                        <ChevronRight className="h-6 w-6 text-slate-100 hidden xl:block group-hover:translate-x-1 transition-transform" />
                                    </div>
                                </div>

                                {!domain.isVerified && (
                                    <div className="mt-10 p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-slate-100 dark:border-slate-850 space-y-6">
                                        <div className="flex items-center gap-3 text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">
                                            <Settings2 className="h-4 w-4 text-primary-500" />
                                            Required DNS Matrix Allocation
                                        </div>
                                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                                            {[
                                                { label: 'Record Type', value: 'CNAME' },
                                                { label: 'Host Alias', value: domain.hostname.split('.')[0] || '@' },
                                                { label: 'Target Value', value: 'proxy.upkilo.com', copy: true }
                                            ].map((record, idx) => (
                                                <div key={idx} className="p-5 bg-white dark:bg-slate-900 rounded-[24px] border border-slate-200 dark:border-slate-800 shadow-sm relative group/record">
                                                    <p className="text-[9px] font-black text-foreground-muted uppercase tracking-widest mb-2">{record.label}</p>
                                                    <div className="flex items-center justify-between gap-4 font-mono text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-tighter">
                                                        <span className="truncate">{record.value}</span>
                                                        {record.copy && (
                                                            <button onClick={() => copyToClipboard(record.value, domain.id)} className="p-1 hover:text-primary-500 transition-colors">
                                                                {copied === domain.id ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                                                            </button>
                                                        )}
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </div>
                            <div className="absolute top-0 left-0 h-full w-1.5 bg-primary-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                        </div>
                    ))
                )}
            </div>

            {/* Comms Authority Overlay (Email) */}
            <div className="space-y-10 pt-16 border-t border-slate-100 dark:border-slate-800">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-8">
                    <div className="flex items-center gap-6">
                        <div className="p-4 bg-gradient-to-br from-emerald-600 to-emerald-950 rounded-[28px] shadow-2xl shadow-emerald-500/20 border border-emerald-500/20">
                            <Mail className="h-8 w-8 text-white" />
                        </div>
                        <div>
                            <h2 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Comms Authority</h2>
                            <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Authorized Dispatch Protocols and Sender Integrity</p>
                        </div>
                    </div>
                </div>

                <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
                    <div className="flex flex-col lg:flex-row gap-6">
                        <div className="relative flex-1 group">
                            <Mail className="absolute left-6 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 group-focus-within:text-emerald-500 transition-colors" />
                            <input
                                type="text"
                                value={emailDomain}
                                onChange={(e) => setEmailDomain(e.target.value)}
                                placeholder="YOURBRAND.COM"
                                className="w-full h-16 pl-16 pr-6 bg-slate-50 dark:bg-slate-950 border border-transparent dark:border-slate-850 dark:text-white rounded-[24px] text-xs font-black uppercase tracking-widest focus:ring-4 focus:ring-emerald-500/10 focus:border-emerald-500 outline-none transition-all shadow-inner"
                            />
                        </div>
                        <div className="flex gap-4">
                            <Button
                                onClick={handleSaveEmailDomain}
                                disabled={savingEmail || !emailDomain}
                                className="h-16 px-8 rounded-[24px] font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-emerald-500/30 active:scale-95 transition-all bg-emerald-600 hover:bg-emerald-700 text-white"
                            >
                                {savingEmail ? <Loader2 className="h-5 w-5 animate-spin" /> : <Save className="h-5 w-5 mr-3" />}
                                Commit Domain
                            </Button>
                            {emailDomain && (
                                <Button
                                    variant="outline"
                                    onClick={handleVerifyEmail}
                                    disabled={verifyingEmail}
                                    className="h-16 px-8 rounded-[24px] font-black uppercase tracking-widest text-[10px] dark:border-slate-800 dark:text-slate-400 flex items-center gap-3"
                                >
                                    {verifyingEmail ? <Loader2 className="h-5 w-5 animate-spin" /> : <ShieldCheck className="h-5 w-5" />}
                                    Sync Comms
                                </Button>
                            )}
                        </div>
                    </div>

                    <AnimatePresence>
                        {emailDomain && (
                            <motion.div 
                                initial={{ opacity: 0, height: 0 }}
                                animate={{ opacity: 1, height: 'auto' }}
                                className="space-y-10 pt-10 border-t border-slate-50 dark:border-slate-850"
                            >
                                <div className="flex items-center gap-4">
                                    <div className="h-1 w-8 bg-emerald-500 rounded-full" />
                                    <h4 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.4em]">Security Handshake Matrix</h4>
                                </div>

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                                    {/* SPF Record */}
                                    <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-slate-100 dark:border-slate-850 space-y-6 relative overflow-hidden group/proto">
                                        <div className="relative z-10 flex items-center justify-between">
                                            <div className="text-[10px] font-black text-foreground-muted uppercase tracking-widest flex items-center gap-2">
                                                <Shield className="h-3.5 w-3.5" /> Protocol: SPF (TXT)
                                            </div>
                                            {emailStatus?.spfValid ? (
                                                <span className="text-[9px] font-black text-success-fg bg-emerald-500/10 px-3 py-1 rounded-lg border border-emerald-500/20 uppercase tracking-widest flex items-center gap-2">
                                                    <Check className="h-3 w-3" /> Operational
                                                </span>
                                            ) : (
                                                <span className="text-[9px] font-black text-warning-fg bg-amber-500/10 px-3 py-1 rounded-lg border border-amber-500/20 uppercase tracking-widest">Pending Sync</span>
                                            )}
                                        </div>
                                        <div className="relative z-10 p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 font-mono text-[10px] text-slate-900 dark:text-white flex items-center justify-between group/code overflow-hidden">
                                            <code className="truncate pr-4">v=spf1 include:upkilo.com ~all</code>
                                            <button onClick={() => copyToClipboard('v=spf1 include:upkilo.com ~all', 'spf')} className="p-2 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-lg transition-all text-slate-300 hover:text-primary-500">
                                                {copied === 'spf' ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                            </button>
                                        </div>
                                        <div className="absolute top-0 right-0 w-24 h-24 bg-emerald-500/[0.03] blur-3xl rounded-full" />
                                    </div>

                                    {/* DKIM Record */}
                                    <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-slate-100 dark:border-slate-850 space-y-6 relative overflow-hidden group/proto">
                                        <div className="relative z-10 flex items-center justify-between">
                                            <div className="text-[10px] font-black text-foreground-muted uppercase tracking-widest flex items-center gap-2">
                                                <Shield className="h-3.5 w-3.5" /> Protocol: DKIM (CNAME)
                                            </div>
                                            {emailStatus?.dkimValid ? (
                                                <span className="text-[9px] font-black text-success-fg bg-emerald-500/10 px-3 py-1 rounded-lg border border-emerald-500/20 uppercase tracking-widest flex items-center gap-2">
                                                    <Check className="h-3 w-3" /> Operational
                                                </span>
                                            ) : (
                                                <span className="text-[9px] font-black text-warning-fg bg-amber-500/10 px-3 py-1 rounded-lg border border-amber-500/20 uppercase tracking-widest">Pending Sync</span>
                                            )}
                                        </div>
                                        <div className="relative z-10 space-y-4">
                                            <div className="space-y-2">
                                                <p className="text-[8px] font-black text-foreground-muted uppercase tracking-widest ml-1">Selector Hash</p>
                                                <div className="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 font-mono text-[10px] text-slate-900 dark:text-white flex items-center justify-between group/code">
                                                    <code>upkilo._domainkey</code>
                                                    <button onClick={() => copyToClipboard('upkilo._domainkey', 'dkim-h')} className="p-2 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-lg transition-all text-slate-300 hover:text-primary-500">
                                                        {copied === 'dkim-h' ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                                                    </button>
                                                </div>
                                            </div>
                                            <div className="space-y-2">
                                                <p className="text-[8px] font-black text-foreground-muted uppercase tracking-widest ml-1">Operational Value</p>
                                                <div className="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 font-mono text-[10px] text-slate-900 dark:text-white flex items-center justify-between group/code">
                                                    <code>dkim.upkilo.com</code>
                                                    <button onClick={() => copyToClipboard('dkim.upkilo.com', 'dkim-v')} className="p-2 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-lg transition-all text-slate-300 hover:text-primary-500">
                                                        {copied === 'dkim-v' ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                        <div className="absolute top-0 right-0 w-24 h-24 bg-emerald-500/[0.03] blur-3xl rounded-full" />
                                    </div>
                                </div>
                            </motion.div>
                        )}
                    </AnimatePresence>
                </div>
            </div>

            {/* Diagnostic Corridor */}
            <div className="p-10 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl relative overflow-hidden group">
                <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
                    <div className="p-6 bg-slate-800 rounded-[32px] border border-slate-700 shadow-inner group-hover:rotate-12 transition-transform duration-1000">
                        <Activity className="h-10 w-10 text-primary-400" />
                    </div>
                    <div className="flex-1 space-y-4">
                        <h3 className="text-xl font-black text-white uppercase tracking-tight">Identity Probe Diagnostics</h3>
                        <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest leading-relaxed">
                            DNS propagation nodes are currently being queried across <span className="text-primary-400 font-black">24 Global POPs</span>. If latency exceeds 48 cycles, consult our <span className="text-emerald-400 underline font-black cursor-pointer hover:text-white">UPLINK ORACLE</span> for assistance.
                        </p>
                        <div className="flex flex-wrap gap-4 pt-4">
                            <button className="h-12 px-8 rounded-xl bg-white/5 border border-white/10 text-primary-400 font-black uppercase tracking-widest text-[9px] hover:bg-white/10 flex items-center gap-2">
                                <ExternalLink className="h-4 w-4" /> GoDaddy Sync
                            </button>
                            <button className="h-12 px-8 rounded-xl bg-white/5 border border-white/10 text-foreground-muted font-black uppercase tracking-widest text-[9px] hover:bg-white/10 flex items-center gap-2">
                                <ExternalLink className="h-4 w-4" /> Cloudflare Sync
                            </button>
                        </div>
                    </div>
                </div>
                <div className="absolute top-0 right-0 w-80 h-80 bg-primary-500/5 blur-3xl rounded-full" />
            </div>
        </div>
    );
}

