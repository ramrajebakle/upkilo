import type { Metadata } from 'next';
import type { ReactNode } from 'react';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// Carries metadata for the marketplace route. The page itself is now a Server Component and
// could export this directly, but it stays here so metadata sits in one predictable place
// alongside the route's other page-level concerns, and so a future move back to a client
// page cannot silently drop the title again.
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
