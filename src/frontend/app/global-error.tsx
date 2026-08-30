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
        // global-error replaces the root layout entirely, so it cannot reach ThemeProvider,
        // globals.css or any token — by the time it renders, the tree that provides them has
        // already failed. That is why it is styled inline and why it carries its own copy of
        // the theme rather than importing one: a crash screen must not depend on the thing
        // that crashed.
        //
        // It was a fixed slate-900 panel, so a light-mode user's first sight of a fatal error
        // was a full-screen dark flash. The <style> block below is the smallest thing that
        // respects both themes without a single import: CSS custom properties keyed off
        // prefers-color-scheme, plus a .dark/.light class so an explicit in-app choice still
        // wins. The inline script that sets that class is the same three lines ThemeScript
        // runs, duplicated deliberately for the same isolation reason.
        <html lang="en">
            <head>
                <style
                    dangerouslySetInnerHTML={{
                        __html: `
:root{--ge-bg:#f8f8fa;--ge-panel:#ffffff;--ge-border:#e4e4eb;--ge-fg:#111120;--ge-muted:#66667a;--ge-danger:#b91c1c;--ge-code:#f0f0f4;--ge-shadow:0 25px 50px -12px rgb(0 0 0 / .15)}
@media (prefers-color-scheme:dark){:root:not(.light){--ge-bg:#0b1120;--ge-panel:#0f172a;--ge-border:#1e293b;--ge-fg:#f1f5f9;--ge-muted:#94a3b8;--ge-danger:#fca5a5;--ge-code:#070c16;--ge-shadow:0 25px 50px -12px rgb(0 0 0 / .5)}}
:root.dark{--ge-bg:#0b1120;--ge-panel:#0f172a;--ge-border:#1e293b;--ge-fg:#f1f5f9;--ge-muted:#94a3b8;--ge-danger:#fca5a5;--ge-code:#070c16;--ge-shadow:0 25px 50px -12px rgb(0 0 0 / .5)}
html,body{margin:0;background:var(--ge-bg);color:var(--ge-fg)}`,
                    }}
                />
                <script
                    dangerouslySetInnerHTML={{
                        __html: `(function(){try{var t=localStorage.getItem('theme');if(t!=='light'&&t!=='dark')t=matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light';document.documentElement.classList.add(t);document.documentElement.style.colorScheme=t;}catch(e){}})();`,
                    }}
                />
            </head>
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
                        background: 'var(--ge-bg)',
                        color: 'var(--ge-fg)',
                    }}
                >
                    <div
                        style={{
                            maxWidth: '480px',
                            textAlign: 'center',
                            padding: '2.5rem',
                            borderRadius: '16px',
                            background: 'var(--ge-panel)',
                            border: '1px solid var(--ge-border)',
                            boxShadow: 'var(--ge-shadow)',
                        }}
                    >
                        <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🚨</div>
                        <h1 style={{ fontSize: '1.5rem', fontWeight: '700', marginBottom: '1rem' }}>
                            Critical System Error
                        </h1>
                        <p style={{ color: 'var(--ge-muted)', marginBottom: '1.5rem', lineHeight: '1.6' }}>
                            A critical error occurred while rendering the application root.
                        </p>
                        
                        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
                            <button
                                onClick={() => reset()}
                                style={{
                                    padding: '0.75rem 1.5rem',
                                    borderRadius: '8px',
                                    border: 'none',
                                    background: '#4535d4',
                                    color: '#ffffff',
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
                                    border: '1px solid var(--ge-border)',
                                    background: 'transparent',
                                    color: 'var(--ge-fg)',
                                    fontWeight: '600',
                                    cursor: 'pointer',
                                }}
                            >
                                Reload App
                            </button>
                        </div>

                        {process.env.NODE_ENV === 'development' && (
                            <div style={{ marginTop: '2rem', textAlign: 'left' }}>
                                <p style={{ color: 'var(--ge-danger)', fontSize: '0.875rem', fontWeight: 'bold', marginBottom: '0.5rem' }}>
                                    Developer Details
                                </p>
                                <pre
                                    style={{
                                        background: 'var(--ge-code)',
                                        padding: '1rem',
                                        borderRadius: '8px',
                                        fontSize: '0.75rem',
                                        color: 'var(--ge-danger)',
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
