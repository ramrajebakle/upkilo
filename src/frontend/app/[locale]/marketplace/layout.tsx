import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB } from '@/lib/seo';

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

// CollectionPage rather than ItemList: the listings rendered here are fetched per-request and
// vary by what businesses are featured at the time, so enumerating them in structured data
// would publish a snapshot that stops matching the page within minutes. The page type and its
// purpose are stable; the contents are not.
const MARKETPLACE_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'CollectionPage',
  name: 'Upkilo Marketplace',
  description:
    'Browse salons, spas, barbershops, studios and clinics taking online bookings on Upkilo.',
  url: `${SITE_URL}/en/marketplace`,
  // No isPartOf: the site declares no WebSite node anywhere, and pointing at a `#website` @id
  // that is never defined is a dangling reference — it resolves to nothing and buys no entity
  // clarity. Organization is referenced instead because that node genuinely exists, on the
  // homepage. Add isPartOf here if a WebSite node is ever declared alongside it.
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  HOME_CRUMB,
  { name: 'Marketplace', path: '/en/marketplace' },
]);

export default function MarketplaceLayout({ children }: { children: ReactNode }) {
  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(MARKETPLACE_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
      {children}
    </>
  );
}
