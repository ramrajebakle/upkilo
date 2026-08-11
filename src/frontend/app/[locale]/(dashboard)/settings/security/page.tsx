'use client';

import { useState, useEffect } from 'react';
import { Lock, Key, Shield, Loader2, ShieldCheck, AlertCircle, MonitorSmartphone, LogIn, KeyRound, Activity } from 'lucide-react';
import api, { apiClient } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { cn, formatRelativeTime } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { MfaSetup } from '@/components/auth/MfaSetup';
import { useSignalR } from '@/contexts/SignalRContext';

interface SecurityEvent {
    id: string;
    type: 'login' | 'password_change' | 'mfa_enabled' | 'mfa_disabled' | 'api_key_created' | 'suspicious_login';
    description: string;
    ipAddress: string;
    device: string;
    timestamp: string;
}

const EVENT_ICONS: Record<SecurityEvent['type'], React.ElementType> = {
    login: LogIn,
    password_change: KeyRound,
    mfa_enabled: ShieldCheck,
    mfa_disabled: AlertCircle,
    api_key_created: Key,
    suspicious_login: MonitorSmartphone,
};

const EVENT_COLORS: Record<SecurityEvent['type'], string> = {
    login: 'text-blue-500 bg-blue-50 dark:bg-blue-500/10',
    password_change: 'text-violet-500 bg-violet-50 dark:bg-violet-500/10',
    mfa_enabled: 'text-emerald-500 bg-emerald-50 dark:bg-emerald-500/10',
    mfa_disabled: 'text-amber-500 bg-amber-50 dark:bg-amber-500/10',
    api_key_created: 'text-slate-500 bg-slate-50 dark:bg-slate-500/10',
    suspicious_login: 'text-red-500 bg-red-50 dark:bg-red-500/10',
};

export default function SecuritySettingsPage() {
    const { success: toastSuccess, error: toastError, warning: toastWarning } = useToast();
    const { connection } = useSignalR();
    const [securityData, setSecurityData] = useState({
        twoFactorEnabled: false,
        loading: true,
        setupMode: false,
        qrCode: null as string | null,
        setupCode: '',
    });
    const [securityEvents, setSecurityEvents] = useState<SecurityEvent[]>([]);
    const [eventsLoading, setEventsLoading] = useState(true);

    useEffect(() => {
        fetchSecurityStatus();
        fetchSecurityEvents();
    }, []);

    // Real-time security event notifications via SignalR
    useEffect(() => {
        if (!connection) return;
        const handler = (event: SecurityEvent) => {
            setSecurityEvents(prev => [event, ...prev].slice(0, 20));
            if (event.type === 'suspicious_login') {
                toastWarning(`Suspicious login detected from ${event.ipAddress} — ${event.device}`);
            } else if (event.type === 'password_change') {
                toastSuccess('Password changed successfully');
            }
        };
        connection.on('SecurityEventReceived', handler);
        return () => { connection.off('SecurityEventReceived', handler); };
    }, [connection, toastWarning, toastSuccess]);

    const fetchSecurityEvents = async () => {
        setEventsLoading(true);
        try {
            const res = await apiClient.get('/api/v1/security/events?limit=10');
            setSecurityEvents(res.data ?? []);
        } catch {
            // Silently fail — events panel is non-critical
        } finally {
            setEventsLoading(false);
        }
    };

    const fetchSecurityStatus = async () => {
        try {
            // GET /profile carries the flag; there is no dedicated 2FA status route.
            const res = await api.auth.twoFactor.status();
            setSecurityData(prev => ({
                ...prev,
                twoFactorEnabled: !!res.data.twoFactorEnabled,
                loading: false
            }));
        } catch (err) {
            console.error('Failed to fetch 2FA status', err);
            setSecurityData(prev => ({ ...prev, loading: false }));
        }
    };

    const handleEnable2FA = async () => {
        setSecurityData(prev => ({ ...prev, loading: true }));
        try {
            const res = await api.auth.twoFactor.setup();
            setSecurityData(prev => ({ 
                ...prev, 
                setupMode: true, 
                qrCode: res.data.qrCodeUrl,
                loading: false 
            }));
        } catch (err) {
            toastError('Failed to start 2FA setup');
            setSecurityData(prev => ({ ...prev, loading: false }));
        }
    };

    const handleVerify2FA = async (code: string) => {
        try {
            await api.auth.twoFactor.verify(code);
            toastSuccess('Two-factor authentication enabled');
            setSecurityData(prev => ({ 
                ...prev, 
                setupMode: false, 
                twoFactorEnabled: true,
                qrCode: null 
            }));
        } catch (err) {
            toastError('Invalid verification code');
        }
    };

    if (securityData.loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Security Matrix...</p>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header Bundle */}
            <div className="flex items-center gap-6 mb-12">
                <div className="p-4 bg-gradient-to-br from-slate-900 to-primary-900 rounded-[28px] shadow-2xl shadow-primary-500/10 border border-slate-800">
                    <ShieldCheck className="h-8 w-8 text-primary-400" />
                </div>
                <div>
                    <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Security & Auth</h1>
                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Authorized Identity Protection</p>
                </div>
            </div>

            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-8">
                <div className="flex items-center gap-4 mb-4">
                    <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                    <h2 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Access Protocols</h2>
                </div>

                <div className="space-y-6">
                    {/* Password */}
                    <div className="group p-8 bg-slate-50/50 dark:bg-slate-950/30 border border-transparent dark:border-slate-850 rounded-[32px] flex flex-col md:flex-row items-center gap-8 transition-all hover:bg-white dark:hover:bg-slate-900 hover:shadow-xl hover:border-slate-100 dark:hover:border-slate-800">
                        <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-800 group-hover:scale-110 transition-transform">
                            <Lock className="h-6 w-6 text-slate-400 dark:text-slate-600 group-hover:text-primary-500 transition-colors" />
                        </div>
                        <div className="flex-1 text-center md:text-left">
                            <h3 className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest mb-1.5">Root Access Password</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest leading-relaxed">System-wide authorization credential. Periodic rotation recommended.</p>
                        </div>
                        <Button variant="outline" className="h-12 px-8 rounded-xl font-black uppercase tracking-widest text-[9px] dark:border-slate-800 dark:text-slate-400 dark:hover:bg-slate-800">
                            Rotate Credential
                        </Button>
                    </div>

                    {/* 2FA — now uses shared MfaSetup component */}
                    <div className={cn(
                        "p-8 rounded-[32px] border transition-all duration-700 space-y-6",
                        securityData.twoFactorEnabled
                            ? "bg-emerald-50/10 dark:bg-emerald-400/5 border-emerald-100 dark:border-emerald-400/20"
                            : "bg-slate-50/50 dark:bg-slate-950/30 border-transparent dark:border-slate-850"
                    )}>
                        <div>
                            <h3 className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest mb-1">Dual-Channel Verification</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest leading-relaxed">Authenticator app TOTP for account access.</p>
                        </div>
                        <MfaSetup
                            isEnabled={securityData.twoFactorEnabled}
                            qrCodeUrl={securityData.qrCode}
                            onRequestSetup={handleEnable2FA}
                            onVerify={handleVerify2FA}
                        />
                    </div>

                    {/* API Access */}
                    <div className="group p-8 bg-slate-50/50 dark:bg-slate-950/30 border border-transparent dark:border-slate-850 rounded-[32px] flex flex-col md:flex-row items-center gap-8 transition-all hover:bg-white dark:hover:bg-slate-900 hover:shadow-xl hover:border-slate-100 dark:hover:border-slate-800">
                        <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-800 group-hover:scale-110 transition-transform">
                            <Key className="h-6 w-6 text-slate-400 dark:text-slate-600 group-hover:text-primary-500 transition-colors" />
                        </div>
                        <div className="flex-1 text-center md:text-left">
                            <h3 className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest mb-1.5">Integration Tokens & API</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest leading-relaxed">Programmable access endpoints for cross-platform data pipeline orchestration.</p>
                        </div>
                        <Button variant="outline" className="h-12 px-8 rounded-xl font-black uppercase tracking-widest text-[9px] dark:border-slate-800 dark:text-slate-400 dark:hover:bg-slate-800">
                            Manage Tokens
                        </Button>
                    </div>
                </div>
            </div>

            {/* Recent Security Activity — L8 */}
            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-6">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-amber-500 shadow-lg shadow-amber-500/50" />
                        <h2 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Security Activity</h2>
                    </div>
                    <div className="flex items-center gap-2">
                        <Activity className="h-4 w-4 text-slate-400" aria-hidden="true" />
                        <span className="text-xs text-slate-400 uppercase tracking-widest font-bold">Live</span>
                        {connection && (
                            <div className="w-1.5 h-1.5 bg-emerald-400 rounded-full animate-pulse" aria-label="Connected" />
                        )}
                    </div>
                </div>

                {eventsLoading ? (
                    <div className="space-y-3" aria-busy="true" aria-label="Loading security events">
                        {[...Array(3)].map((_, i) => (
                            <div key={i} className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50 dark:bg-slate-800/50 animate-pulse">
                                <div className="w-9 h-9 rounded-xl bg-slate-200 dark:bg-slate-700 shrink-0" />
                                <div className="flex-1 space-y-2">
                                    <div className="h-3 bg-slate-200 dark:bg-slate-700 rounded w-3/4" />
                                    <div className="h-2 bg-slate-200 dark:bg-slate-700 rounded w-1/2" />
                                </div>
                            </div>
                        ))}
                    </div>
                ) : securityEvents.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-8 text-center">
                        <Shield className="h-10 w-10 text-slate-300 dark:text-slate-600 mb-3" aria-hidden="true" />
                        <p className="text-sm font-semibold text-slate-500 dark:text-slate-400">No recent security events</p>
                        <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">Login attempts and account changes will appear here.</p>
                    </div>
                ) : (
                    <ul className="space-y-2" aria-label="Recent security events">
                        {securityEvents.map(event => {
                            const Icon = EVENT_ICONS[event.type] ?? Shield;
                            return (
                                <li
                                    key={event.id}
                                    className="flex items-center gap-4 p-4 rounded-2xl bg-slate-50/50 dark:bg-slate-800/30 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition-colors"
                                >
                                    <div className={cn('p-2 rounded-xl shrink-0', EVENT_COLORS[event.type])}>
                                        <Icon className="h-4 w-4" aria-hidden="true" />
                                    </div>
                                    <div className="flex-1 min-w-0">
                                        <p className="text-sm font-medium text-slate-900 dark:text-white truncate">{event.description}</p>
                                        <p className="text-xs text-slate-400 dark:text-slate-500 mt-0.5 truncate">
                                            {event.device} · {event.ipAddress}
                                        </p>
                                    </div>
                                    <time
                                        className="text-xs text-slate-400 dark:text-slate-500 shrink-0"
                                        dateTime={event.timestamp}
                                    >
                                        {formatRelativeTime(event.timestamp)}
                                    </time>
                                </li>
                            );
                        })}
                    </ul>
                )}
            </div>

            {/* Danger Zone */}
            <div className="p-10 bg-red-50/10 dark:bg-red-950/20 border border-red-100/30 dark:border-red-900/40 rounded-[40px] shadow-2xl shadow-red-500/[0.02] flex flex-col md:flex-row items-center gap-10 group overflow-hidden relative">
                <div className="relative z-10 p-5 bg-red-100/50 dark:bg-red-900/50 rounded-3xl border border-red-200/50 dark:border-red-800/50">
                    <AlertCircle className="h-10 w-10 text-red-600 dark:text-red-400" />
                </div>
                <div className="relative z-10 flex-1 text-center md:text-left">
                    <h2 className="text-xl font-black text-red-600 dark:text-red-400 uppercase tracking-tight mb-2">Terminal: Immediate Purge</h2>
                    <p className="text-[10px] font-bold text-red-600/60 dark:text-red-400/60 uppercase tracking-widest leading-relaxed">
                        Execution of this protocol is irreversible. All clusters, temporal data, and identity records will be purged from core memory.
                    </p>
                </div>
                <button className="relative z-10 bg-red-600 hover:bg-red-700 text-white px-10 h-16 rounded-[24px] font-black uppercase tracking-[0.2em] text-[10px] shadow-2xl shadow-red-600/30 active:scale-95 transition-all">
                    Authorize Purge
                </button>
                
                <AlertCircle className="absolute -bottom-10 -right-10 h-40 w-40 text-red-600/5 -rotate-12 group-hover:rotate-0 transition-transform duration-1000" />
            </div>
        </div>
    );
}

