'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { AlertTriangle } from 'lucide-react';

export default function AuthError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
    return (
        <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 px-4">
            <div className="w-full max-w-md">
                <div className="bg-card rounded-2xl shadow-xl p-8 text-center">
                    <div className="mx-auto w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mb-6">
                        <AlertTriangle className="w-8 h-8 text-danger-fg" />
                    </div>
                    
                    <h2 className="text-2xl font-bold text-foreground mb-2">Authentication Error</h2>
                    <p className="text-foreground-secondary mb-8">
                        We encountered an issue during authentication. Please try again.
                    </p>

                    <div className="space-y-3">
                        <Button 
                            onClick={reset} 
                            className="w-full"
                        >
                            Try Again
                        </Button>
                        <Link href="/login" passHref legacyBehavior>
                            <Button variant="outline" className="w-full">
                                Back to Login
                            </Button>
                        </Link>
                    </div>

                    {process.env.NODE_ENV === 'development' && (
                        <div className="mt-8 text-left bg-muted p-4 rounded-lg overflow-auto max-h-40">
                            <p className="text-xs font-semibold text-danger-fg mb-1">Developer Details:</p>
                            <p className="text-xs text-foreground font-mono break-all">{error.message}</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
