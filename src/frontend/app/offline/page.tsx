'use client';

import { useEffect, useState } from 'react';

export default function OfflinePage() {
  const [isOnline, setIsOnline] = useState(false);

  useEffect(() => {
    setIsOnline(navigator.onLine);
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, system-ui, -apple-system, sans-serif',
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
        color: '#f8fafc',
        padding: '1rem',
      }}
    >
      <div
        style={{
          maxWidth: '440px',
          width: '100%',
          textAlign: 'center',
          padding: '2.5rem 2rem',
          borderRadius: '20px',
          background: 'rgba(30, 41, 59, 0.85)',
          border: '1px solid rgba(148, 163, 184, 0.12)',
          backdropFilter: 'blur(16px)',
          boxShadow: '0 32px 64px -12px rgba(0, 0, 0, 0.6)',
        }}
      >
        {/* No-signal icon */}
        <svg
          width="64"
          height="64"
          viewBox="0 0 64 64"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
          style={{ marginBottom: '1.5rem' }}
        >
          <circle cx="32" cy="32" r="32" fill="rgba(124, 58, 237, 0.15)" />
          <line x1="14" y1="20" x2="50" y2="44" stroke="#7C3AED" strokeWidth="2.5" strokeLinecap="round" />
          <path d="M22 28c2.6-2 5.8-3.2 10-3.2" stroke="#94a3b8" strokeWidth="2.5" strokeLinecap="round" />
          <path d="M10 22c5-4 11.4-6.5 22-6.5 3 0 5.8.4 8.4 1.1" stroke="#94a3b8" strokeWidth="2.5" strokeLinecap="round" />
          <path d="M30 36c2.8 0 5.2 1 7 2.6" stroke="#94a3b8" strokeWidth="2.5" strokeLinecap="round" />
          <circle cx="32" cy="46" r="2.5" fill="#7C3AED" />
        </svg>

        <h1
          style={{
            fontSize: '1.6rem',
            fontWeight: 700,
            marginBottom: '0.75rem',
            color: '#f1f5f9',
            letterSpacing: '-0.02em',
          }}
        >
          You&apos;re offline
        </h1>

        <p
          style={{
            fontSize: '0.95rem',
            color: '#94a3b8',
            lineHeight: 1.65,
            marginBottom: '2rem',
          }}
        >
          Upkilo needs an internet connection to load. Check your network
          settings and try again.
        </p>

        {isOnline && (
          <div
            role="status"
            style={{
              padding: '0.6rem 1rem',
              borderRadius: '8px',
              background: 'rgba(34, 197, 94, 0.12)',
              border: '1px solid rgba(34, 197, 94, 0.3)',
              color: '#4ade80',
              fontSize: '0.85rem',
              fontWeight: 600,
              marginBottom: '1.25rem',
            }}
          >
            Connection restored — you can reload now
          </div>
        )}

        <button
          onClick={() => window.location.reload()}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.75rem 1.75rem',
            borderRadius: '10px',
            border: 'none',
            background: 'linear-gradient(135deg, #7C3AED 0%, #6D28D9 100%)',
            color: '#fff',
            fontWeight: 600,
            fontSize: '0.95rem',
            cursor: 'pointer',
          }}
        >
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <polyline points="1 4 1 10 7 10" />
            <path d="M3.51 15a9 9 0 1 0 .49-3.5" />
          </svg>
          Try again
        </button>

        <p style={{ marginTop: '2rem', fontSize: '0.8rem', color: '#475569' }}>
          Upkilo &mdash; Business Management Platform
        </p>
      </div>
    </div>
  );
}
