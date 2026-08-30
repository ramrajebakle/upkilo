'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card } from '@/components/ui/Card';
import { Shield, Sparkles, CheckCircle2 } from 'lucide-react';

export default function AcceptInvitationPage() {
    const params = useParams();
    const router = useRouter();
    const token = params.token as string;

    const [inviteData, setInviteData] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const [success, setSuccess] = useState(false);

    // Form state
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [password, setPassword] = useState('');

    useEffect(() => {
        const fetchInvite = async () => {
            try {
                const res = await api.invitations.getPublic(token);
                setInviteData(res.data);
            } catch (err: any) {
                setError(err.response?.data?.message || 'Invalid or expired invitation link.');
            } finally {
                setLoading(false);
            }
        };

        if (token) fetchInvite();
    }, [token]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            await api.invitations.accept({
                token,
                firstName,
                lastName,
                password
            });
            setSuccess(true);
            setTimeout(() => {
                router.push('/login');
            }, 3000);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to accept invitation.');
            setSubmitting(false);
        }
    };

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-muted">
                <div className="text-foreground-secondary flex flex-col items-center gap-4">
                    <div className="w-12 h-12 border-4 border-primary/25 border-t-primary-600 rounded-full animate-spin"></div>
                    <p className="font-medium">Verifying invitation...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-muted px-4">
                <Card className="max-w-md w-full p-8 text-center space-y-6">
                    <div className="w-16 h-16 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto">
                        <Shield className="h-8 w-8" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-bold text-foreground">Oops!</h1>
                        <p className="text-foreground-secondary mt-2">{error}</p>
                    </div>
                    <Button className="w-full" onClick={() => router.push('/')}>
                        Return to Home
                    </Button>
                </Card>
            </div>
        );
    }

    if (success) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-muted px-4">
                <Card className="max-w-md w-full p-8 text-center space-y-6">
                    <div className="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto">
                        <CheckCircle2 className="h-8 w-8" />
                    </div>
                    <div>
                        <h1 className="text-2xl font-bold text-foreground">Welcome to the Team!</h1>
                        <p className="text-foreground-secondary mt-2">
                            Your account has been created successfully. Redirecting you to login...
                        </p>
                    </div>
                </Card>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-muted px-4 py-12">
            <div className="max-w-md w-full space-y-8">
                <div className="text-center">
                    <div className="flex justify-center mb-6">
                        <div className="p-3 bg-brand-subtle text-primary rounded-2xl">
                            <Sparkles className="h-8 w-8" />
                        </div>
                    </div>
                    <h1 className="text-3xl font-extrabold text-foreground tracking-tight">
                        Join {inviteData.businessName}
                    </h1>
                    <p className="mt-3 text-foreground-secondary">
                        You&apos;ve been invited as <span className="font-semibold text-foreground">{inviteData.role}</span>.
                        Complete your profile to get started.
                    </p>
                </div>

                <Card className="p-8 shadow-xl border-0 ring-1 ring-border">
                    <form onSubmit={handleSubmit} className="space-y-5">
                        <div className="grid grid-cols-2 gap-4">
                            <Input
                                label="First Name"
                                value={firstName}
                                onChange={(e) => setFirstName(e.target.value)}
                                placeholder="Jane"
                                required
                            />
                            <Input
                                label="Last Name"
                                value={lastName}
                                onChange={(e) => setLastName(e.target.value)}
                                placeholder="Doe"
                                required
                            />
                        </div>

                        <Input
                            label="Email Address"
                            type="email"
                            value={inviteData.email}
                            disabled
                            className="bg-muted"
                        />

                        <Input
                            label="Create Password"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="Min. 8 characters"
                            required
                        />

                        <div className="pt-2">
                            <Button
                                type="submit"
                                className="w-full text-lg h-12"
                                loading={submitting}
                            >
                                Accept & Join Team
                            </Button>
                        </div>

                        <p className="text-center text-xs text-foreground-muted">
                            By joining, you agree to Upkilo&apos;s Terms of Service and Privacy Policy.
                        </p>
                    </form>
                </Card>
            </div>
        </div>
    );
}
