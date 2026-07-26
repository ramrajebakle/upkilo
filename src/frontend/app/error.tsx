'use client';

import { useEffect } from 'react';

/**
 * Next.js App Router error boundary.
 * Catches errors in route segments and renders the ErrorBoundary recovery UI.
 * This file is automatically used by Next.js when an error occurs in any route.
 */
export default function GlobalError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    useEffect(() => {
        if (process.env.NODE_ENV === 'production') {
            const apiBase = process.env.NEXT_PUBLIC_API_URL ?? '';
            fetch(`${apiBase}/api/v1/telemetry/client-error`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    type: 'RouteError',
                    message: error.message,
                    digest: error.digest,
                    url: typeof window !== 'undefined' ? window.location.href : '',
                    timestamp: new Date().toISOString(),
                }),
                keepalive: true,
            }).catch(() => null);
        }
    }, [error]);

    return (
        <html>
            <body style={{ margin: 0 }}>
                <div
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        minHeight: '100vh',
                        padding: '2rem',
                        fontFamily: 'Inter, system-ui, sans-serif',
                        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
                        color: '#e2e8f0',
                    }}
                >
                    <div
                        style={{
                            maxWidth: '480px',
                            textAlign: 'center',
                            padding: '2.5rem',
                            borderRadius: '16px',
                            background: 'rgba(30, 41, 59, 0.8)',
                            border: '1px solid rgba(148, 163, 184, 0.1)',
                            backdropFilter: 'blur(12px)',
                            boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
                        }}
                    >
                        <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>⚠️</div>
                        <h1
                            style={{
                                fontSize: '1.5rem',
                                fontWeight: '700',
                                marginBottom: '0.75rem',
                                color: '#f1f5f9',
                            }}
                        >
                            Security System Error
                        </h1>
                        <p
                            style={{
                                fontSize: '0.95rem',
                                color: '#94a3b8',
                                marginBottom: '1.5rem',
                                lineHeight: '1.6',
                            }}
                        >
                            The platform encountered a critical initialization error. 
                            Please restart your browser or clear session cache.
                        </p>
                        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center' }}>
                            <button
                                onClick={() => reset()}
                                style={{
                                    padding: '0.65rem 1.5rem',
                                    borderRadius: '8px',
                                    border: 'none',
                                    background: 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)',
                                    color: '#fff',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                    fontSize: '0.9rem',
                                }}
                            >
                                Re-verify System
                            </button>
                            <button
                                onClick={() => (window.location.href = '/')}
                                style={{
                                    padding: '0.65rem 1.5rem',
                                    borderRadius: '8px',
                                    border: '1px solid rgba(148, 163, 184, 0.2)',
                                    background: 'transparent',
                                    color: '#94a3b8',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                    fontSize: '0.9rem',
                                }}
                            >
                                Go Back
                            </button>
                        </div>
                        {process.env.NODE_ENV === 'development' && error && (
                            <details
                                style={{
                                    marginTop: '1.5rem',
                                    textAlign: 'left',
                                    padding: '1rem',
                                    borderRadius: '8px',
                                    background: 'rgba(15, 23, 42, 0.6)',
                                    border: '1px solid rgba(239, 68, 68, 0.2)',
                                }}
                            >
                                <summary style={{ cursor: 'pointer', color: '#f87171', fontWeight: '600', fontSize: '0.85rem' }}>
                                    System Diagnostics
                                </summary>
                                <pre
                                    style={{
                                        marginTop: '0.75rem',
                                        fontSize: '0.75rem',
                                        color: '#fca5a5',
                                        whiteSpace: 'pre-wrap',
                                        wordBreak: 'break-word',
                                        overflow: 'auto',
                                        maxHeight: '200px',
                                    }}
                                >
                                    {error.message || "Unknown error"}
                                    {'\n\n'}
                                    {error.stack || "No stack trace available"}
                                </pre>
                            </details>
                        )}
                    </div>
                </div>
            </body>
        </html>
    );
}
