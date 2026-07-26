'use client';

import { useEffect } from 'react';

// global-error must be a Client Component
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
                    type: 'GlobalError',
                    message: error.message,
                    digest: error.digest,
                    url: typeof window !== 'undefined' ? window.location.href : '',
                    userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : '',
                    timestamp: new Date().toISOString(),
                }),
                keepalive: true,
            }).catch(() => null);
        }
    }, [error]);

    return (
        <html lang="en">
            <body>
                <div
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        minHeight: '100vh',
                        padding: '2rem',
                        fontFamily: 'system-ui, -apple-system, sans-serif',
                        background: '#0f172a',
                        color: '#f8fafc',
                    }}
                >
                    <div
                        style={{
                            maxWidth: '480px',
                            textAlign: 'center',
                            padding: '2.5rem',
                            borderRadius: '16px',
                            background: '#1e293b',
                            border: '1px solid #334155',
                            boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
                        }}
                    >
                        <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🚨</div>
                        <h1 style={{ fontSize: '1.5rem', fontWeight: '700', marginBottom: '1rem' }}>
                            Critical System Error
                        </h1>
                        <p style={{ color: '#94a3b8', marginBottom: '1.5rem', lineHeight: '1.6' }}>
                            A critical error occurred while rendering the application root.
                        </p>
                        
                        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
                            <button
                                onClick={() => reset()}
                                style={{
                                    padding: '0.75rem 1.5rem',
                                    borderRadius: '8px',
                                    border: 'none',
                                    background: '#3b82f6',
                                    color: 'white',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                }}
                            >
                                Try Again
                            </button>
                            <button
                                onClick={() => (window.location.href = '/')}
                                style={{
                                    padding: '0.75rem 1.5rem',
                                    borderRadius: '8px',
                                    border: '1px solid #475569',
                                    background: 'transparent',
                                    color: '#cbd5e1',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                }}
                            >
                                Reload App
                            </button>
                        </div>

                        {process.env.NODE_ENV === 'development' && (
                            <div style={{ marginTop: '2rem', textAlign: 'left' }}>
                                <p style={{ color: '#f87171', fontSize: '0.875rem', fontWeight: 'bold', marginBottom: '0.5rem' }}>
                                    Developer Details
                                </p>
                                <pre
                                    style={{
                                        background: '#0f172a',
                                        padding: '1rem',
                                        borderRadius: '8px',
                                        fontSize: '0.75rem',
                                        color: '#fca5a5',
                                        overflow: 'auto',
                                        maxHeight: '200px',
                                    }}
                                >
                                    {error.message}
                                    {'\n\n'}
                                    {error.stack}
                                </pre>
                            </div>
                        )}
                    </div>
                </div>
            </body>
        </html>
    );
}
