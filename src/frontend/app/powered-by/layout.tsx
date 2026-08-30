import type { ReactNode } from 'react';
import type { Metadata } from 'next';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd } from '@/lib/seo';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// page.tsx is a client component and cannot export metadata, so it lives here. This route is the
// landing target of the "Powered by Upkilo" badge on every free-tier booking page, which makes
// it a real acquisition entry point rather than an afterthought — it was shipping untitled.
export const metadata: Metadata = {
  title: 'Powered by Upkilo — Booking Software for Service Businesses',
  description:
    'You landed here from a booking page running on Upkilo. See what the platform does: online booking, client records, automated reminders and payments for salons, spas and clinics.',
  alternates: { canonical: `${SITE_URL}/powered-by` },
  openGraph: {
    title: 'Powered by Upkilo — Booking Software for Service Businesses',
    description: 'Online booking, client records, reminders and payments for service businesses.',
    url: `${SITE_URL}/powered-by`,
    type: 'website',
  },
};

// Plain WebPage about the software, pointing at the one Organization node. This route is where
// the "Powered by Upkilo" badge sends people, so its job is to identify the product behind a
// booking page the visitor has just used — an entity answer, not a catalogue or an offer.
const POWERED_BY_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'WebPage',
  name: 'Powered by Upkilo',
  description:
    'Upkilo provides the online booking, client records, reminders and payments behind this booking page.',
  url: `${SITE_URL}/powered-by`,
  about: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  { name: 'Home', path: '/en' },
  { name: 'Powered by Upkilo', path: '/powered-by' },
]);

export default function PoweredByLayout({ children }: { children: ReactNode }) {
    return (
        <RootHtml
            lang="en"
            headChildren={
                <>
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(POWERED_BY_JSON_LD) }} />
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
                </>
            }
        >
            {children}
        </RootHtml>
    );
}
