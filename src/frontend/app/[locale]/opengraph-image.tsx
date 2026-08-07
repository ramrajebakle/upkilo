import { ImageResponse } from 'next/og';

// Every page on this site previously shared as a blank card. The metadata carried
// openGraph.title and openGraph.description but no image, so any link posted to WhatsApp,
// LinkedIn, Slack, X or iMessage rendered as bare text — on WhatsApp specifically, which is
// the dominant sharing channel for this product's market, a card without an image is
// dramatically less likely to be opened.
//
// Generated rather than a static file so it cannot drift from the brand, needs no design
// tooling to change, and costs no repo weight. Next.js renders this once at build time for
// static routes and caches it at the edge thereafter.
//
// Applies to every route under app/[locale] by convention. Routes with their own identity
// (a tenant's booking page) can add their own opengraph-image.tsx to override it.

export const runtime = 'edge';
export const alt = 'Upkilo — AI-powered booking, CRM and payments for service businesses';
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

// Matches --color-primary-500 / --color-primary-700 in globals.css. Hardcoded because
// ImageResponse renders outside the CSS pipeline and cannot read custom properties.
const BRAND = '#5b4cf5';
const BRAND_DEEP = '#3728ab';
const INK = '#0f1117';

export default async function OpengraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          height: '100%',
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          background: `linear-gradient(135deg, ${INK} 0%, #1a1030 55%, ${BRAND_DEEP} 100%)`,
          padding: '72px 80px',
          fontFamily: 'sans-serif',
        }}
      >
        {/* Wordmark */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 18 }}>
          <div
            style={{
              width: 56,
              height: 56,
              borderRadius: 16,
              background: BRAND,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
              fontSize: 34,
              fontWeight: 700,
            }}
          >
            U
          </div>
          <div style={{ color: '#fff', fontSize: 34, fontWeight: 700, letterSpacing: -0.5 }}>
            Upkilo
          </div>
        </div>

        {/* Proposition. Deliberately a capability statement — no customer counts, no ratings,
            nothing that would need to be true about a customer base that does not exist. */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div
            style={{
              color: '#ffffff',
              fontSize: 64,
              fontWeight: 700,
              lineHeight: 1.1,
              letterSpacing: -1.5,
              maxWidth: 900,
            }}
          >
            Bookings, clients and payments in one place.
          </div>
          <div style={{ color: '#b9b4d6', fontSize: 30, maxWidth: 820, lineHeight: 1.35 }}>
            AI-powered scheduling and CRM for salons, spas, studios and clinics.
          </div>
        </div>

        {/* Footer rule */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div style={{ width: 60, height: 4, background: BRAND, borderRadius: 2 }} />
          <div style={{ color: '#8a85a8', fontSize: 24 }}>upkilo.com</div>
        </div>
      </div>
    ),
    size
  );
}
