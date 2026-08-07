import type { Metadata } from 'next';
import type { ReactNode } from 'react';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// Carries metadata on behalf of app/[locale]/marketplace/page.tsx, which is a Client
// Component (it fetches and filters listings client-side) and therefore cannot export
// metadata itself. Without this the page inherited app/[locale]/layout.tsx's generic
// site-wide title and description.
//
// Note the page also fetches its listings in a post-mount effect, so the server-rendered
// HTML a crawler sees first is an empty shell. Correct metadata does not fix that; moving
// the initial fetch server-side is tracked separately and is the larger of the two issues
// for this page specifically.
//
// Canonical is pinned to /en, matching the middleware rule that folds non-English marketing
// URLs onto English while those pages carry no real translations.
export const metadata: Metadata = {
  title: 'Upkilo Marketplace — Discover Local Service Businesses',
  description:
    'Browse salons, spas, barbershops, studios and clinics taking online bookings on Upkilo. Find a business near you and book instantly.',
  alternates: { canonical: `${SITE_URL}/en/marketplace` },
  openGraph: {
    title: 'Upkilo Marketplace — Discover Local Service Businesses',
    description:
      'Browse salons, spas, studios and clinics taking online bookings. Find a business near you and book instantly.',
    type: 'website',
  },
};

export default function MarketplaceLayout({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
