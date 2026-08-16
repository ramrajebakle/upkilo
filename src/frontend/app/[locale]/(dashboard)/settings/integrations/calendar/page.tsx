'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    ArrowLeft,
    Calendar,
    CheckCircle2,
    AlertCircle,
    RefreshCw,
    Settings,
    Unlink,
    Globe,
    ExternalLink,
    Loader2
} from 'lucide-react';
import { useToast } from '@/components/ui/Toast';
import { cn, formatDate } from '@/lib/utils';
import api from '@/lib/api';
import { useAuthStore } from '@/store/authStore';
import { useSignalR } from '@/hooks/useSignalR';

interface CalendarProvider {
    id: string;
    name: string;
    icon: string;
    description: string;
    isConnected: boolean;
    syncEnabled: boolean;
    lastSync?: string;
    accountEmail?: string;
}

export default function CalendarIntegrationsPage() {
    const { addToast } = useToast();
    const { user } = useAuthStore();
    const [loading, setLoading] = useState(true);
    const [providers, setProviders] = useState<CalendarProvider[]>([
        {
            id: 'google',
            name: 'Google Calendar',
            icon: 'https://upload.wikimedia.org/wikipedia/commons/a/a5/Google_Calendar_icon_%282020%29.svg',
            description: 'Sync your bookings with your main Google Calendar. Ideal for personal schedule blocking.',
            isConnected: false,
            syncEnabled: false
        },
        {
            id: 'outlook',
            name: 'Outlook Calendar',
            icon: 'https://upload.wikimedia.org/wikipedia/commons/d/df/Microsoft_Office_Outlook_%282018%E2%80%93present%29.svg',
            description: 'Connect your Office 365 or Outlook.com calendar for work schedule synchronization.',
            isConnected: false,
            syncEnabled: false
        }
    ]);
    const { connection } = useSignalR();

    useEffect(() => {
        const fetchConnections = async () => {
            if (!user?.id) return;
            setLoading(true);
            try {
                const response = await api.calendar.connections(user.id);
                const connections = response.data;

                setProviders(prev => prev.map(p => {
                    const conn = connections.find((c: any) => c.provider.toLowerCase() === p.id);
                    return {
                        ...p,
                        isConnected: !!conn,
                        lastSync: conn?.lastSyncAt,
                        syncEnabled: !!conn
                    };
                }));
            } catch (error) {
                console.error('Failed to fetch calendar connections', error);
                addToast('Failed to load calendar status', 'error');
            } finally {
                setLoading(false);
            }
        };
        fetchConnections();
    }, [user?.id]);

    const handleConnect = async (providerId: string) => {
        if (!user?.id) return;
        try {
            addToast(`Initiating ${providerId} connection...`, 'info');
            const response = await api.calendar.getAuthUrl(providerId, user.id);
            if (response.data?.url) {
                window.location.href = response.data.url;
            }
        } catch (error) {
            console.error('Failed to get auth URL', error);
            addToast('Could not start connection process', 'error');
        }
    };

    const handleDisconnect = async (providerId: string) => {
        if (!confirm('Are you sure? This will stop syncing events.')) return;
        // In this MVP, we just disconnect locally or we could add a DELETE endpoint
        addToast('Calendar disconnected locally', 'info');
        setProviders(prev => prev.map(p =>
            p.id === providerId ? { ...p, isConnected: false, syncEnabled: false } : p
        ));
    };

    const handleSyncNow = async (providerId: string) => {
        if (!user?.id) return;
        try {
            addToast(`Syncing ${providerId}...`, 'info');
            await api.calendar.sync(user.id);
            addToast('Sync initiated successfully', 'success');

            // Refresh connections state after a delay
            setTimeout(async () => {
                const response = await api.calendar.connections(user.id);
                const connections = response.data;
                setProviders(prev => prev.map(p => {
                    const conn = connections.find((c: any) => c.provider.toLowerCase() === p.id);
                    return {
                        ...p,
                        lastSync: conn?.lastSyncAt
                    };
                }));
            }, 2000);
        } catch (error) {
            console.error('Sync failed', error);
            addToast('Synchronisation failed', 'error');
        }
    };

    if (loading) {
        return (
            <div className="max-w-4xl mx-auto py-8">
                <div className="flex items-center gap-4 mb-8">
                    <div className="h-10 w-10 bg-slate-200 rounded-xl animate-pulse" />
                    <div className="h-8 w-64 bg-slate-200 rounded animate-pulse" />
                </div>
                <div className="grid gap-6">
                    <div className="h-64 bg-slate-200 rounded-xl animate-pulse" />
                    <div className="h-64 bg-slate-200 rounded-xl animate-pulse" />
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link href="/settings" className="p-2 hover:bg-slate-100 rounded-xl transition-colors">
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div>
                    <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>
                        Calendar Integrations
                    </h1>
                    <p className="text-slate-500">Sync your bookings with external calendars to avoid double booking.</p>
                </div>
            </div>

            <div className="grid gap-6">
                {providers.map((provider, index) => (
                    <div
                        key={provider.id}
                        className={cn(
                            "card-elevated p-6 animate-fade-in-up transition-all duration-300",
                            provider.isConnected ? "border-emerald-100 bg-emerald-50/10" : "bg-white"
                        )}
                        style={{ animationDelay: `${index * 100}ms` }}
                    >
                        <div className="flex flex-col md:flex-row gap-6">
                            {/* Icon */}
                            <div className="flex-shrink-0">
                                <div className="w-16 h-16 rounded-2xl bg-white shadow-sm border border-slate-100 flex items-center justify-center p-3">
                                    <img
                                        src={provider.icon}
                                        alt={provider.name}
                                        className="w-full h-full object-contain"
                                    />
                                </div>
                            </div>

                            {/* Content */}
                            <div className="flex-1">
                                <div className="flex items-start justify-between mb-2">
                                    <div>
                                        <h3 className="text-lg font-semibold text-slate-900 flex items-center gap-2">
                                            {provider.name}
                                            {provider.isConnected && (
                                                <span className="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700 text-xs font-medium flex items-center gap-1">
                                                    <CheckCircle2 className="h-3 w-3" />
                                                    Connected
                                                </span>
                                            )}
                                        </h3>
                                        {provider.isConnected && provider.accountEmail && (
                                            <p className="text-sm font-medium text-slate-600 mt-1">
                                                Connected as: <span className="text-slate-900">{provider.accountEmail}</span>
                                            </p>
                                        )}
                                    </div>

                                    <div className="flex items-center gap-2">
                                        {provider.isConnected ? (
                                            <>
                                                <button
                                                    onClick={() => handleSyncNow(provider.id)}
                                                    className="p-2 text-slate-500 hover:text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                                                    title="Sync Now"
                                                >
                                                    <RefreshCw className="h-4 w-4" />
                                                </button>
                                                <button
                                                    onClick={() => handleDisconnect(provider.id)}
                                                    className="btn btn-secondary text-red-600 hover:bg-red-50 px-3 py-1.5 h-auto text-sm"
                                                >
                                                    Disconnect
                                                </button>
                                            </>
                                        ) : (
                                            <button
                                                onClick={() => handleConnect(provider.id)}
                                                className="btn btn-primary px-4 py-2 h-auto text-sm"
                                            >
                                                Connect
                                            </button>
                                        )}
                                    </div>
                                </div>

                                <p className="text-slate-600 text-sm mb-4 leading-relaxed">
                                    {provider.description}
                                </p>

                                {provider.isConnected && (
                                    <div className="flex items-center gap-6 pt-4 border-t border-slate-100">
                                        <div className="flex items-center gap-2 text-xs text-slate-500">
                                            <RefreshCw className="h-3.5 w-3.5" />
                                            Last synced: {provider.lastSync ? formatDate(provider.lastSync) : 'Never'}
                                        </div>
                                        <div className="flex items-center gap-2 text-xs text-slate-500">
                                            <Globe className="h-3.5 w-3.5" />
                                            Two-way sync active
                                        </div>
                                        <div className="flex items-center gap-2 text-xs text-emerald-600 ml-auto">
                                            <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                                            Live Updates On
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                ))}

                {/* Info Card */}
                <div className="bg-blue-50 rounded-xl p-4 border border-blue-100 flex items-start gap-3 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                    <AlertCircle className="h-5 w-5 text-blue-600 flex-shrink-0 mt-0.5" />
                    <div>
                        <h4 className="text-sm font-semibold text-blue-900">How syncing works</h4>
                        <p className="text-sm text-blue-800 mt-1">
                            Upkilo will check your connected calendars for conflicts before allowing clients to book.
                            New bookings made in Upkilo will automatically appear on your external calendar.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
}
