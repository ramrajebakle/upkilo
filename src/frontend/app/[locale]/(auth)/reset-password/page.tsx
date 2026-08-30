'use client';

import { useState, Suspense, useEffect, useRef } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { useToast } from '@/components/ui/Toast';
import { ShieldCheck, Lock, Loader2, CheckCircle2, AlertCircle, RotateCcw } from 'lucide-react';

function ResetPasswordContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const { success, error } = useToast();

    const token = searchParams.get('token');
    const tid = searchParams.get('tid');
    const [requestEmail, setRequestEmail] = useState('');
    const [requestLoading, setRequestLoading] = useState(false);
    const [requestSent, setRequestSent] = useState(false);
    const [resendCooldown, setResendCooldown] = useState(0);
    const cooldownRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        if (resendCooldown <= 0) return;
        cooldownRef.current = setInterval(() => {
            setResendCooldown((s) => {
                if (s <= 1) { clearInterval(cooldownRef.current!); return 0; }
                return s - 1;
            });
        }, 1000);
        return () => clearInterval(cooldownRef.current!);
    }, [resendCooldown]);

    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [loading, setLoading] = useState(false);
    const [isComplete, setIsComplete] = useState(false);

    const handleRequestReset = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!requestEmail) return;

        setRequestLoading(true);
        try {
            const res = await api.auth.forgotPassword(requestEmail);
            success(res.data?.message || 'Reset link sent if account exists.');
            setRequestSent(true);
            setResendCooldown(60);
        } catch (err: any) {
            error(err.response?.data?.message || 'Failed to send reset link.');
        } finally {
            setRequestLoading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token) {
            error('Reset token is missing.');
            return;
        }

        if (password !== confirmPassword) {
            error('Passwords do not match.');
            return;
        }

        if (password.length < 8) {
            error('Password must be at least 8 characters.');
            return;
        }

        setLoading(true);
        try {
            const res = await api.auth.resetPassword({ 
                token, 
                newPassword: password,
                tenantId: tid || undefined 
            });
            success(res.data?.message || 'Password successfully reset!');
            setIsComplete(true);
        } catch (err: any) {
            error(err.response?.data?.message || 'Failed to reset password.');
        } finally {
            setLoading(false);
        }
    };

    if (isComplete) {
        return (
            <Card className="w-full max-w-md p-8 text-center space-y-6 shadow-2xl animate-in fade-in zoom-in duration-300">
                <div className="flex flex-col items-center gap-4">
                    <div className="p-4 bg-emerald-50 rounded-full text-emerald-600">
                        <CheckCircle2 className="h-16 w-16" />
                    </div>
                    <h1 className="text-2xl font-bold text-foreground">Password Reset Complete</h1>
                    <p className="text-foreground-secondary">
                        Your password has been updated. You can now log in with your new credentials.
                    </p>
                </div>
                <Button className="w-full h-12 text-lg font-bold" onClick={() => router.push('/login')}>
                    Back to Login
                </Button>
            </Card>
        );
    }

    if (!token) {
        return (
            <Card className="w-full max-w-md p-8 space-y-8 shadow-2xl border-border animate-in fade-in zoom-in duration-300">
                <div className="text-center space-y-2">
                    <div className="flex justify-center mb-4">
                        <div className="p-4 bg-brand-subtle rounded-full">
                            <Lock className="h-10 w-10 text-primary" />
                        </div>
                    </div>
                    <h1 className="text-2xl font-bold text-foreground">Forgot Password?</h1>
                    <p className="text-foreground-secondary">Enter your email and we'll send you a link to reset your password.</p>
                </div>

                {requestSent ? (
                    <div className="space-y-6 text-center animate-in fade-in slide-in-from-bottom-4 duration-500">
                        <div className="p-4 bg-emerald-50 rounded-xl border border-emerald-100 text-emerald-700 text-sm">
                            <p className="font-semibold">Check your inbox</p>
                            <p className="mt-1">If an account matches <strong>{requestEmail}</strong>, you'll receive a link shortly.</p>
                        </div>
                        <div className="space-y-3">
                            <Button
                                variant="secondary"
                                className="w-full"
                                disabled={resendCooldown > 0 || requestLoading}
                                loading={requestLoading}
                                onClick={(e) => { e.preventDefault(); handleRequestReset(e as any); }}
                                leftIcon={<RotateCcw size={15} />}
                            >
                                {resendCooldown > 0 ? `Resend in ${resendCooldown}s` : 'Resend email'}
                            </Button>
                            <Button variant="outline" className="w-full" onClick={() => router.push('/login')}>
                                Return to Login
                            </Button>
                        </div>
                    </div>
                ) : (
                    <form onSubmit={handleRequestReset} className="space-y-6">
                        <div className="space-y-2">
                            <Label htmlFor="email">Email Address</Label>
                            <Input
                                id="email"
                                type="email"
                                placeholder="name@example.com"
                                className="h-11"
                                value={requestEmail}
                                onChange={(e) => setRequestEmail(e.target.value)}
                                required
                                disabled={requestLoading}
                            />
                        </div>

                        <Button type="submit" className="w-full h-12 text-lg font-bold" loading={requestLoading}>
                            {requestLoading ? 'Sending Link...' : 'Send Reset Link'}
                        </Button>

                        <div className="text-center">
                            <button
                                type="button"
                                onClick={() => router.push('/login')}
                                className="text-sm text-foreground-secondary hover:text-primary font-medium transition-colors"
                            >
                                Back to Login
                            </button>
                        </div>
                    </form>
                )}
            </Card>
        );
    }

    return (
        <Card className="w-full max-w-md p-8 space-y-8 shadow-2xl border-border animate-in fade-in zoom-in duration-300">
            <div className="text-center space-y-2">
                <div className="flex justify-center mb-4">
                    <div className="p-4 bg-brand-subtle rounded-full">
                        <ShieldCheck className="h-10 w-10 text-primary" />
                    </div>
                </div>
                <h1 className="text-2xl font-bold text-foreground">Reset Password</h1>
                <p className="text-foreground-secondary">Secure your account with a new password</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
                <div className="space-y-4">
                    <div className="space-y-2">
                        <Label htmlFor="password">New Password</Label>
                        <div className="relative">
                            <Lock className="absolute left-3 top-3 h-4 w-4 text-foreground-muted" />
                            <Input
                                id="password"
                                type="password"
                                placeholder="••••••••"
                                className="pl-10 h-11"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                            />
                        </div>
                    </div>
                    <div className="space-y-2">
                        <Label htmlFor="confirm-password">Confirm New Password</Label>
                        <div className="relative">
                            <Lock className="absolute left-3 top-3 h-4 w-4 text-foreground-muted" />
                            <Input
                                id="confirm-password"
                                type="password"
                                placeholder="••••••••"
                                className="pl-10 h-11"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                required
                            />
                        </div>
                    </div>
                </div>

                <Button type="submit" className="w-full h-12 text-lg font-bold" loading={loading}>
                    {loading ? 'Updating...' : 'Update Password'}
                </Button>
            </form>
        </Card>
    );
}

export default function ResetPasswordPage() {
    return (
        <div className="min-h-screen flex items-center justify-center bg-muted/50 p-4">
            <Suspense fallback={<Loader2 className="h-12 w-12 text-primary animate-spin" />}>
                <ResetPasswordContent />
            </Suspense>
        </div>
    );
}
