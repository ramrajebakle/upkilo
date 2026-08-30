'use client';

import { useEffect, useState, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { CheckCircle2, XCircle, Loader2, Mail } from 'lucide-react';

function VerifyEmailContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const token = searchParams.get('token');
    const tid = searchParams.get('tid');
    const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
    const [message, setMessage] = useState('');

    useEffect(() => {
        if (!token) {
            setStatus('error');
            setMessage('Invalid or missing verification token.');
            return;
        }

        const verify = async () => {
            try {
                // Pass tid (tenantId) to resolve correct DB context in multi-tenant environments
                await api.auth.verifyEmail(token, tid || undefined);
                setStatus('success');
                setMessage('Your email has been successfully verified! You can now access all features.');
            } catch (err: any) {
                setStatus('error');
                setMessage(err.response?.data?.message || 'Verification failed. The link may have expired.');
            }
        };

        verify();
    }, [token, tid]);

    return (
        <Card className="w-full max-w-md p-8 text-center space-y-6 shadow-2xl border-border bg-card animate-fade-in-up">
            <div className="flex justify-center">
                <div className="p-5 bg-primary/10 rounded-2xl shadow-glow">
                    <Mail className="h-10 w-10 text-primary" />
                </div>
            </div>

            <h1 className="text-3xl font-black text-foreground tracking-tight font-display">
                Email Verification
            </h1>

            {status === 'loading' && (
                <div className="flex flex-col items-center gap-4 py-8">
                    <Loader2 className="h-14 w-14 text-primary animate-spin" />
                    <p className="text-muted-foreground font-medium animate-pulse">Verifying your email address...</p>
                </div>
            )}

            {status === 'success' && (
                <div className="space-y-6 animate-fade-in-up">
                    <div className="flex flex-col items-center gap-3 text-success-fg">
                        <div className="p-3 bg-emerald-500/10 rounded-full shadow-[0_0_20px_rgba(16,185,129,0.2)]">
                            <CheckCircle2 className="h-16 w-16" />
                        </div>
                        <p className="font-bold text-xl tracking-tight font-display">Verified Successfully</p>
                    </div>
                    <p className="text-muted-foreground leading-relaxed">
                        {message}
                    </p>
                    <Button
                        className="w-full h-14 text-lg font-bold shadow-glow hover:scale-[1.01] active:scale-95 transition-all"
                        onClick={() => router.push('/login')}
                    >
                        Go to Dashboard
                    </Button>
                </div>
            )}

            {status === 'error' && (
                <div className="space-y-6 animate-fade-in-up">
                    <div className="flex flex-col items-center gap-3 text-destructive">
                        <div className="p-3 bg-destructive/10 rounded-full shadow-[0_0_20px_rgba(239,68,68,0.2)]">
                            <XCircle className="h-16 w-16" />
                        </div>
                        <p className="font-bold text-xl tracking-tight font-display">Verification Failed</p>
                    </div>
                    <p className="text-muted-foreground leading-relaxed">
                        {message}
                    </p>
                    <Button
                        variant="outline"
                        className="w-full h-14 text-lg font-bold hover:bg-muted/50"
                        onClick={() => router.push('/login')}
                    >
                        Back to Login
                    </Button>
                </div>
            )}
        </Card>
    );
}

export default function VerifyEmailPage() {
    return (
        <div className="min-h-screen flex items-center justify-center bg-background p-4 pattern-grid-slate-100">
            <Suspense fallback={<Loader2 className="h-12 w-12 text-primary animate-spin" />}>
                <VerifyEmailContent />
            </Suspense>
        </div>
    );
}
