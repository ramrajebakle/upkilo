import type { ReactNode } from 'react';
import type { Metadata } from 'next';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd } from '@/lib/seo';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// page.tsx is a client component and cannot export metadata, so it lives here. Without it the
// route shipped no title, description or canonical at all — search engines invented a snippet
// from whatever body copy they found first.
export const metadata: Metadata = {
  title: 'Discover Salons, Spas & Clinics Near You — Upkilo',
  description:
    'Browse and book appointments with salons, spas and clinics on Upkilo. See real availability, compare services and book instantly — no phone call needed.',
  alternates: { canonical: `${SITE_URL}/discover` },
  openGraph: {
    title: 'Discover Salons, Spas & Clinics — Upkilo',
    description: 'Browse real availability and book instantly with businesses on Upkilo.',
    url: `${SITE_URL}/discover`,
    type: 'website',
  },
};

// CollectionPage, with no ItemList — the businesses shown are fetched client-side and change as
// tenants join or leave, so enumerating them here would publish a snapshot that goes stale
// immediately. Same call as the marketplace layout, for the same reason.
const DISCOVER_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'CollectionPage',
  name: 'Discover Businesses on Upkilo',
  description:
    'Browse and book appointments with salons, spas and clinics taking online bookings on Upkilo.',
  url: `${SITE_URL}/discover`,
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  { name: 'Home', path: '/en' },
  { name: 'Discover', path: '/discover' },
]);

export default function DiscoverLayout({ children }: { children: ReactNode }) {
    return (
        <RootHtml
            lang="en"
            headChildren={
                <>
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(DISCOVER_JSON_LD) }} />
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
                </>
            }
        >
            {children}
        </RootHtml>
    );
}
