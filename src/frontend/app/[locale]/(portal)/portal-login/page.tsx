'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Mail, ArrowRight, CheckCircle2, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { apiClient } from '@/lib/api';

export default function ClientLoginPage() {
    const router = useRouter();
    const [email, setEmail] = useState('');
    const [loading, setLoading] = useState(false);
    const [sent, setSent] = useState(false);
    const [successMessage, setSuccessMessage] = useState('');
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!email) return;

        setLoading(true);
        setError(null);

        try {
            const response = await apiClient.post('/api/client-portal/login', {
                email,
                businessSlug: 'demo' // This should be dynamic based on hostname or context
            });
            setSuccessMessage(response.data?.message || `We've sent a magic link to ${email}.`);
            setSent(true);
        } catch (err: any) {
            console.error('Login failed', err);
            setError(err.response?.data?.message || 'Something went wrong. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    if (sent) {
        return (
            <div className="min-h-screen flex items-center justify-center p-4 bg-background">
                <Card className="w-full max-w-md p-8 text-center animate-fade-in-up border-border">
                    <div className="w-20 h-20 bg-primary/10 text-primary rounded-full flex items-center justify-center mx-auto mb-6 shadow-glow">
                        <CheckCircle2 className="w-10 h-10" />
                    </div>
                    <h1 className="text-2xl font-bold text-foreground mb-2 font-display">Check your email</h1>
                    <p className="text-muted-foreground mb-8">
                        {successMessage}
                    </p>
                    <Button variant="outline" className="w-full" onClick={() => setSent(false)}>
                        Back to Login
                    </Button>
                </Card>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex items-center justify-center p-4 bg-background">
            <Card className="w-full max-w-md p-8 shadow-2xl border-border bg-card animate-fade-in-up">
                <div className="text-center mb-8">
                    <div className="w-16 h-16 bg-primary rounded-2xl flex items-center justify-center text-primary-foreground text-2xl font-bold mx-auto mb-4 shadow-glow">
                        U
                    </div>
                    <h1 className="text-3xl font-black text-foreground tracking-tight font-display">
                        Welcome Back
                    </h1>
                    <p className="text-muted-foreground mt-2">Sign in to manage your appointments</p>
                </div>

                {error && (
                    <div className="mb-6 p-4 bg-destructive/10 border border-destructive/20 rounded-xl flex items-start gap-3 text-destructive animate-shake">
                        <AlertCircle className="w-5 h-5 mt-0.5 shrink-0" />
                        <p className="text-sm">{error}</p>
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-6">
                    <div>
                        <label htmlFor="email" className="block text-sm font-semibold text-muted-foreground mb-2">
                            Email Address
                        </label>
                        <div className="relative group">
                            <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground group-focus-within:text-primary transition-colors" />
                            <input
                                id="email"
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                placeholder="name@example.com"
                                className="w-full pl-12 pr-4 py-3.5 bg-muted/30 border border-input rounded-xl focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground placeholder:text-text-tertiary"
                                required
                            />
                        </div>
                    </div>

                    <Button
                        type="submit"
                        className="w-full py-7 text-lg font-bold shadow-glow hover:scale-[1.01] active:scale-95 transition-all"
                        disabled={loading}
                    >
                        {loading ? 'Sending link...' : 'Send Magic Link'}
                        {!loading && <ArrowRight className="ml-2 h-5 w-5" />}
                    </Button>
                </form>

                <div className="mt-10 pt-8 border-t border-border text-center">
                    <p className="text-sm text-muted-foreground">
                        Don't have an account? <br className="sm:hidden" />
                        <Link href="/book/demo" className="text-primary font-bold hover:underline">
                            Book your first appointment
                        </Link>
                    </p>
                </div>
            </Card>
        </div>
    );
}

// Inline Link component to avoid import issues in this scratchpad-like creation, 
// though next/link should be fine in a real project.

