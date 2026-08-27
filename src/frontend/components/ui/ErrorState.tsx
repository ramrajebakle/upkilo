'use client';

import { memo } from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ErrorStateProps {
    /** What failed, in the user's terms — "Couldn't load bookings", not "Request failed". */
    title?: string;
    /** The underlying failure. A raw Error is fine; the message is read, never the stack. */
    error?: unknown;
    /** Omit to render the state without a retry affordance. */
    onRetry?: () => void;
    isRetrying?: boolean;
    className?: string;
}

/**
 * The counterpart to EmptyState, and the piece the UI kit was missing.
 *
 * 166 of the 169 dashboard pages that fetch had no error branch at all: the
 * prevailing pattern was `catch { setRows([]) }`, which renders the empty
 * state. A customer whose request just failed was told, in confident product
 * language, that they have no bookings / no clients / no revenue — the failure
 * mode most likely to make someone think a system lost their data.
 *
 * Distinguishing the two is the whole point, so the copy says plainly that the
 * data is intact and this is a display problem, and offers the retry that the
 * empty state has no reason to.
 */
export const ErrorState = memo(function ErrorState({
    title = "Couldn't load this",
    error,
    onRetry,
    isRetrying = false,
    className,
}: ErrorStateProps) {
    const detail = error instanceof Error ? error.message : undefined;

    return (
        <div
            role="alert"
            aria-live="polite"
            className={cn('card-elevated py-16 text-center animate-fade-in', className)}
        >
            <div className="mx-auto w-16 h-16 bg-danger-500/10 rounded-2xl flex items-center justify-center mb-4">
                <AlertCircle className="h-8 w-8 text-danger-500" aria-hidden="true" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-2">{title}</h3>
            <p className="text-text-secondary mb-6 max-w-md mx-auto text-sm">
                {detail ? `${detail} ` : ''}
                Your data is safe — this is a display problem.
            </p>
            {onRetry && (
                <button type="button" onClick={onRetry} className="btn btn-secondary" disabled={isRetrying}>
                    <RefreshCw className={cn('h-4 w-4', isRetrying && 'animate-spin')} aria-hidden="true" />
                    {isRetrying ? 'Retrying…' : 'Try again'}
                </button>
            )}
        </div>
    );
});
