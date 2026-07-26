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
            <div className="text-center max-w-md bg-white p-8 rounded-2xl shadow-sm border border-slate-200">
                <div className="mx-auto w-16 h-16 bg-rose-50 rounded-full flex items-center justify-center mb-6">
                    <AlertCircle className="w-8 h-8 text-rose-500" />
                </div>
                
                <h2 className="text-2xl font-bold text-slate-900 mb-3">Portal Error</h2>
                <p className="text-slate-500 mb-8 leading-relaxed">
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
                    <p className="mt-6 text-xs text-slate-400 font-mono">
                        Error ID: {error.digest}
                    </p>
                )}
            </div>
        </div>
    );
}
