'use client';

import { useEffect, useState } from 'react';
import { Beaker, X, RefreshCw, Zap } from 'lucide-react';
import { apiClient as api } from '@/lib/api';

interface DemoStatus {
    isSandbox: boolean;
    tenantName: string;
    message?: string;
}

/**
 * DemoModeBanner — renders a sticky top banner whenever the authenticated
 * tenant has `IsSandbox = true`. Dismissible per-session.
 *
 * Usage: Drop once into your root layout (layout.tsx) just inside <body>:
 *   <DemoModeBanner />
 */
export default function DemoModeBanner() {
    const [status, setStatus] = useState<DemoStatus | null>(null);
    const [dismissed, setDismissed] = useState(false);
    const [seeding, setSeeding] = useState(false);
    const [disabling, setDisabling] = useState(false);

    useEffect(() => {
        const check = async () => {
            try {
                const res = await api.get('/api/v1/demo/status');
                setStatus(res.data?.data ?? null);
            } catch {
                // Not authenticated or endpoint unavailable — stay hidden
            }
        };
        check();
    }, []);

    const handleSeedData = async () => {
        setSeeding(true);
        try {
            await api.post('/api/v1/demo/seed');
            window.location.reload();
        } catch (err: any) {
            alert(err?.response?.data?.message ?? 'Seed failed');
        } finally {
            setSeeding(false);
        }
    };

    const handleDisable = async () => {
        if (!confirm('Exit demo mode and switch to production? This cannot be undone.')) return;
        setDisabling(true);
        try {
            await api.post('/api/v1/demo/disable');
            setStatus(null);
            window.location.reload();
        } catch {
            alert('Failed to disable demo mode');
        } finally {
            setDisabling(false);
        }
    };

    if (!status?.isSandbox || dismissed) return null;

    return (
        <div
            role="banner"
            aria-label="Demo mode active"
            className="sticky top-0 z-[9999] bg-amber-400 text-amber-950 shadow-md"
        >
            <div className="max-w-7xl mx-auto px-4 py-2 flex items-center gap-3 text-sm">
                {/* Icon */}
                <Beaker className="w-4 h-4 flex-shrink-0" />

                {/* Message */}
                <p className="flex-1 font-medium">
                    <span className="font-bold">DEMO MODE</span>
                    {status.message ? ` — ${status.message}` : ' — Changes are isolated and will not affect real data.'}
                </p>

                {/* Actions */}
                <div className="flex items-center gap-2 flex-shrink-0">
                    <button
                        onClick={handleSeedData}
                        disabled={seeding}
                        className="flex items-center gap-1 px-2.5 py-1 bg-amber-500 hover:bg-amber-600 text-amber-950 rounded-md text-xs font-semibold transition-colors disabled:opacity-50"
                    >
                        {seeding ? (
                            <RefreshCw className="w-3 h-3 animate-spin" />
                        ) : (
                            <Zap className="w-3 h-3" />
                        )}
                        Seed Data
                    </button>

                    <button
                        onClick={handleDisable}
                        disabled={disabling}
                        className="flex items-center gap-1 px-2.5 py-1 bg-amber-900/20 hover:bg-amber-900/30 rounded-md text-xs font-semibold transition-colors disabled:opacity-50"
                    >
                        Exit Demo
                    </button>

                    <button
                        onClick={() => setDismissed(true)}
                        className="p-1 hover:bg-amber-500 rounded transition-colors"
                        aria-label="Dismiss banner"
                    >
                        <X className="w-3.5 h-3.5" />
                    </button>
                </div>
            </div>
        </div>
    );
}
