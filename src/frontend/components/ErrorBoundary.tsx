'use client';

import React from 'react';

interface ErrorBoundaryState {
    hasError: boolean;
    error: Error | null;
}

/**
 * Global React Error Boundary that catches unhandled JS errors
 * and renders a recovery UI instead of crashing the entire app.
 * Wraps the root layout to protect against component-level failures.
 */
export class ErrorBoundary extends React.Component<
    { children: React.ReactNode },
    ErrorBoundaryState
> {
    constructor(props: { children: React.ReactNode }) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error): ErrorBoundaryState {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
        console.error('[ErrorBoundary] Uncaught error:', error, errorInfo);
        // Report to Sentry when DSN is configured
        if (typeof window !== 'undefined' && process.env.NEXT_PUBLIC_SENTRY_DSN) {
            import('@sentry/nextjs').then(({ captureException }) => {
                captureException(error, { extra: { componentStack: errorInfo.componentStack } });
            }).catch(() => { /* Sentry not available */ });
        }
    }

    render() {
        if (this.state.hasError) {
            return (
                <div style={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    minHeight: '100vh',
                    padding: '2rem',
                    fontFamily: 'Inter, system-ui, sans-serif',
                    background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
                    color: '#e2e8f0',
                }}>
                    <div style={{
                        maxWidth: '480px',
                        textAlign: 'center',
                        padding: '2.5rem',
                        borderRadius: '16px',
                        background: 'rgba(30, 41, 59, 0.8)',
                        border: '1px solid rgba(148, 163, 184, 0.1)',
                        backdropFilter: 'blur(12px)',
                        boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
                    }}>
                        <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>⚠️</div>
                        <h1 style={{
                            fontSize: '1.5rem',
                            fontWeight: '700',
                            marginBottom: '0.75rem',
                            color: '#f1f5f9',
                        }}>
                            Something went wrong
                        </h1>
                        <p style={{
                            fontSize: '0.95rem',
                            color: '#94a3b8',
                            marginBottom: '1.5rem',
                            lineHeight: '1.6',
                        }}>
                            An unexpected error occurred. Our team has been notified.
                            Please try refreshing the page.
                        </p>
                        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center' }}>
                            <button
                                onClick={() => window.location.reload()}
                                style={{
                                    padding: '0.65rem 1.5rem',
                                    borderRadius: '8px',
                                    border: 'none',
                                    background: 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)',
                                    color: '#fff',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                    fontSize: '0.9rem',
                                    transition: 'transform 0.15s ease',
                                }}
                                onMouseOver={(e) => (e.currentTarget.style.transform = 'scale(1.04)')}
                                onMouseOut={(e) => (e.currentTarget.style.transform = 'scale(1)')}
                            >
                                Refresh Page
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
                                    transition: 'transform 0.15s ease',
                                }}
                                onMouseOver={(e) => (e.currentTarget.style.transform = 'scale(1.04)')}
                                onMouseOut={(e) => (e.currentTarget.style.transform = 'scale(1)')}
                            >
                                Go to Home
                            </button>
                        </div>
                        {process.env.NODE_ENV === 'development' && this.state.error && (
                            <details style={{
                                marginTop: '1.5rem',
                                textAlign: 'left',
                                padding: '1rem',
                                borderRadius: '8px',
                                background: 'rgba(15, 23, 42, 0.6)',
                                border: '1px solid rgba(239, 68, 68, 0.2)',
                            }}>
                                <summary style={{
                                    cursor: 'pointer',
                                    color: '#f87171',
                                    fontWeight: '600',
                                    fontSize: '0.85rem',
                                }}>
                                    Developer Details
                                </summary>
                                <pre style={{
                                    marginTop: '0.75rem',
                                    fontSize: '0.75rem',
                                    color: '#fca5a5',
                                    whiteSpace: 'pre-wrap',
                                    wordBreak: 'break-word',
                                    overflow: 'auto',
                                    maxHeight: '200px',
                                }}>
                                    {this.state.error.message}
                                    {'\n\n'}
                                    {this.state.error.stack}
                                </pre>
                            </details>
                        )}
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}
