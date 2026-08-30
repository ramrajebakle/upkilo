'use client';

import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useRouter } from '@/navigation';
import { Loader2, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { apiClient } from '@/lib/api';

export default function VerifyPortalPage() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const [status, setStatus] = useState<'verifying' | 'success' | 'error'>('verifying');
    const [error, setError] = useState('');

    useEffect(() => {
        const token = searchParams.get('token');
        const email = searchParams.get('email');

        if (!token || !email) {
            setStatus('error');
            setError('Missing verification information.');
            return;
        }

        const verify = async () => {
            try {
                const response = await apiClient.post('/api/client-portal/verify', { token, email });

                // Store client token separately from tenant token
                if (typeof window !== 'undefined') {
                    localStorage.setItem('client_token', response.data.token);
                    localStorage.setItem('client_data', JSON.stringify(response.data.client));
                }

                setStatus('success');
                setTimeout(() => {
                    router.push('/portal-bookings');
                }, 1500);
            } catch (err: any) {
                console.error('Verification failed', err);
                setStatus('error');
                setError(err.response?.data?.message || 'Invalid or expired magic link.');
            }
        };

        verify();
    }, [searchParams, router]);

    return (
        <div className="min-h-screen flex items-center justify-center p-4 bg-muted">
            <Card className="w-full max-w-md p-8 text-center animate-fade-in-up">
                {status === 'verifying' && (
                    <div className="space-y-6">
                        <div className="w-16 h-16 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto" />
                        <h1 className="text-2xl font-bold text-foreground">Verifying your link...</h1>
                        <p className="text-foreground-secondary text-sm">One moment while we secure your session.</p>
                    </div>
                )}

                {status === 'success' && (
                    <div className="space-y-6">
                        <div className="w-16 h-16 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto animate-bounce">
                            <CheckCircle2 className="w-8 h-8" />
                        </div>
                        <h1 className="text-2xl font-bold text-foreground">Sign in successful!</h1>
                        <p className="text-foreground-secondary">Redirecting to your dashboard...</p>
                    </div>
                )}

                {status === 'error' && (
                    <div className="space-y-6">
                        <div className="w-16 h-16 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto">
                            <AlertCircle className="w-8 h-8" />
                        </div>
                        <h1 className="text-2xl font-bold text-foreground">Verification failed</h1>
                        <p className="text-danger-fg">{error}</p>
                        <button
                            onClick={() => router.push('/portal-login')}
                            className="btn btn-primary w-full mt-4"
                        >
                            Back to Login
                        </button>
                    </div>
                )}
            </Card>
        </div>
    );
}
