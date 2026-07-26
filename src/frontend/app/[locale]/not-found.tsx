import Link from 'next/link';

export default function NotFound() {
  return (
    <html lang="en">
      <body style={{ margin: 0, fontFamily: 'Inter, system-ui, sans-serif', backgroundColor: '#F8F8FA' }}>
        <div
          style={{
            minHeight: '100vh',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '24px',
            textAlign: 'center',
          }}
        >
          {/* Logo mark */}
          <div
            style={{
              width: 72,
              height: 72,
              borderRadius: 20,
              background: 'linear-gradient(135deg, #7C3AED 0%, #5B4CF5 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: 32,
              boxShadow: '0 8px 24px rgba(124,58,237,0.3)',
            }}
          >
            <span style={{ color: '#fff', fontSize: 32, fontWeight: 800 }}>U</span>
          </div>

          {/* 404 number */}
          <p
            style={{
              fontSize: 13,
              fontWeight: 700,
              color: '#7C3AED',
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              marginBottom: 12,
            }}
          >
            Error 404
          </p>

          <h1
            style={{
              fontSize: 'clamp(32px, 6vw, 56px)',
              fontWeight: 800,
              color: '#111120',
              lineHeight: 1.1,
              marginBottom: 16,
            }}
          >
            Page not found
          </h1>

          <p
            style={{
              fontSize: 18,
              color: '#66667A',
              maxWidth: 420,
              lineHeight: 1.6,
              marginBottom: 40,
            }}
          >
            The page you're looking for doesn't exist or has been moved.
          </p>

          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', justifyContent: 'center' }}>
            <Link
              href="/en"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '14px 28px',
                backgroundColor: '#7C3AED',
                color: '#fff',
                borderRadius: 12,
                fontWeight: 600,
                fontSize: 15,
                textDecoration: 'none',
                boxShadow: '0 4px 14px rgba(124,58,237,0.35)',
              }}
            >
              Go to homepage
            </Link>
            <Link
              href="/en/dashboard"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '14px 28px',
                backgroundColor: '#F0F0F4',
                color: '#333344',
                borderRadius: 12,
                fontWeight: 600,
                fontSize: 15,
                textDecoration: 'none',
              }}
            >
              Open dashboard
            </Link>
          </div>

          {/* Subtle help text */}
          <p style={{ marginTop: 48, fontSize: 13, color: '#9999B0' }}>
            Need help?{' '}
            <a href="mailto:support@upkilo.com" style={{ color: '#7C3AED', textDecoration: 'none' }}>
              Contact support
            </a>
          </p>
        </div>
      </body>
    </html>
  );
}
