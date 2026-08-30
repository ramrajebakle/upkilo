'use client';

import { useState } from 'react';
import { Smartphone, Loader2, CheckCircle2, ShieldCheck } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';

interface MfaSetupProps {
    isEnabled: boolean;
    qrCodeUrl: string | null;
    onRequestSetup: () => Promise<void>;
    onVerify: (code: string) => Promise<void>;
    onDisable?: () => Promise<void>;
    className?: string;
}

/**
 * Reusable MFA setup/verify/status widget.
 * Used in settings/security and admin login pages.
 */
export function MfaSetup({ isEnabled, qrCodeUrl, onRequestSetup, onVerify, onDisable, className }: MfaSetupProps) {
    const [code, setCode] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    const handleSetup = async () => {
        setLoading(true);
        setError('');
        try {
            await onRequestSetup();
        } catch {
            setError('Failed to start setup. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleVerify = async () => {
        if (code.length < 6) { setError('Enter the 6-digit code from your authenticator app.'); return; }
        setLoading(true);
        setError('');
        try {
            await onVerify(code);
            setCode('');
        } catch {
            setError('Invalid code. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const handleDisable = async () => {
        if (!onDisable) return;
        setLoading(true);
        try {
            await onDisable();
        } catch {
            setError('Failed to disable MFA.');
        } finally {
            setLoading(false);
        }
    };

    if (isEnabled) {
        return (
            <div className={cn('flex items-center gap-4 p-4 rounded-xl bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800', className)}>
                <CheckCircle2 className="h-5 w-5 text-success-fg shrink-0" aria-hidden="true" />
                <div className="flex-1">
                    <p className="text-sm font-semibold text-emerald-800 dark:text-emerald-300">Two-factor authentication is active</p>
                    <p className="text-xs text-emerald-600 dark:text-emerald-400">Your account is protected by an authenticator app.</p>
                </div>
                {onDisable && (
                    <Button variant="outline" size="sm" onClick={handleDisable} loading={loading} className="text-danger-fg border-red-200 hover:bg-red-50">
                        Disable
                    </Button>
                )}
            </div>
        );
    }

    if (qrCodeUrl) {
        return (
            <div className={cn('space-y-4', className)}>
                <div className="flex items-start gap-3">
                    <Smartphone className="h-5 w-5 text-foreground-muted mt-0.5 shrink-0" aria-hidden="true" />
                    <div>
                        <p className="text-sm font-semibold text-slate-900 dark:text-white">Scan QR code</p>
                        <p className="text-xs text-slate-500 dark:text-slate-400">Use Google Authenticator, Authy, or any TOTP app.</p>
                    </div>
                </div>

                <div className="flex justify-center">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img src={qrCodeUrl} alt="MFA QR code" className="w-40 h-40 rounded-xl border border-slate-200 dark:border-slate-700" />
                </div>

                <div className="space-y-2">
                    <label htmlFor="mfa-code" className="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                        Verification code
                    </label>
                    <input
                        id="mfa-code"
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        maxLength={6}
                        value={code}
                        onChange={e => { setCode(e.target.value.replace(/\D/g, '')); setError(''); }}
                        placeholder="000000"
                        className="w-full text-center text-xl font-mono tracking-[0.5em] px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-primary-500 transition"
                        aria-describedby={error ? 'mfa-error' : undefined}
                    />
                    {error && <p id="mfa-error" role="alert" className="text-xs text-danger-fg">{error}</p>}
                </div>

                <Button
                    className="w-full"
                    onClick={handleVerify}
                    loading={loading}
                    disabled={code.length < 6}
                >
                    <ShieldCheck className="h-4 w-4 mr-2" aria-hidden="true" />
                    Verify & enable
                </Button>
            </div>
        );
    }

    return (
        <div className={cn('', className)}>
            {error && <p role="alert" className="text-xs text-danger-fg mb-3">{error}</p>}
            <Button variant="outline" onClick={handleSetup} loading={loading} className="w-full sm:w-auto">
                <Smartphone className="h-4 w-4 mr-2" aria-hidden="true" />
                Set up authenticator app
            </Button>
        </div>
    );
}
