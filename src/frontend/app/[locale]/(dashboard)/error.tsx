'use client';

import { useEffect } from 'react';

export default function DashboardError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    useEffect(() => {
        console.error('Dashboard error:', error);
    }, [error]);

    return (
        <div className="flex items-center justify-center min-h-[60vh] p-6">
            <div className="text-center space-y-6 max-w-md animate-fade-in-up">
                <div className="mx-auto h-16 w-16 flex items-center justify-center rounded-2xl bg-gradient-to-br from-red-50 to-orange-50 border border-red-100">
                    <svg className="h-8 w-8 text-red-500" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                    </svg>
                </div>

                <div className="space-y-2">
                    <h2
                        className="text-xl font-bold text-slate-800"
                        style={{ fontFamily: 'var(--font-display)' }}
                    >
                        Something went wrong
                    </h2>
                    <p className="text-sm text-slate-500 leading-relaxed">
                        An unexpected error occurred while loading this page. Try refreshing
                        or contact support if the issue persists.
                    </p>
                    {error.digest && (
                        <p className="text-xs text-slate-400 font-mono mt-2">
                            Error ID: {error.digest}
                        </p>
                    )}
                </div>

                <div className="flex flex-col sm:flex-row gap-3 justify-center">
                    <button
                        onClick={reset}
                        className="inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-primary-500/25 hover:shadow-xl transition-all hover:-translate-y-0.5 text-sm"
                    >
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182" />
                        </svg>
                        Try Again
                    </button>
                    <button
                        onClick={() => typeof window !== 'undefined' && (window.location.href = '/dashboard')}
                        className="inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-white text-slate-700 rounded-xl font-medium border border-slate-200 hover:border-primary-300 transition-all text-sm"
                    >
                        Back to Dashboard
                    </button>
                </div>
            </div>
        </div>
    );
}
