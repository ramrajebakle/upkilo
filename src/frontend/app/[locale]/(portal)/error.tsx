'use client';

import { Button } from '@/components/ui/Button';
import { AlertCircle } from 'lucide-react';

export default function PortalError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <div className="min-h-[70vh] flex flex-col items-center justify-center px-4">
            <div className="text-center max-w-md bg-card p-8 rounded-2xl shadow-sm border border-border">
                <div className="mx-auto w-16 h-16 bg-rose-50 rounded-full flex items-center justify-center mb-6">
                    <AlertCircle className="w-8 h-8 text-danger-fg" />
                </div>
                
                <h2 className="text-2xl font-bold text-foreground mb-3">Portal Error</h2>
                <p className="text-foreground-secondary mb-8 leading-relaxed">
                    We're having trouble loading this portal page. Please try refreshing or contact the business if the issue persists.
                </p>

                <div className="flex gap-4 justify-center">
                    <Button onClick={reset} variant="primary">
                        Try Again
                    </Button>
                    <Button onClick={() => window.location.reload()} variant="outline">
                        Refresh Page
                    </Button>
                </div>
                
                {error.digest && (
                    <p className="mt-6 text-xs text-foreground-muted font-mono">
                        Error ID: {error.digest}
                    </p>
                )}
            </div>
        </div>
    );
}
