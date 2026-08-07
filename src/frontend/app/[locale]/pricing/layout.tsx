import type { Metadata } from 'next';
import type { ReactNode } from 'react';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// This layout exists only to carry metadata. app/[locale]/pricing/page.tsx is a genuine
// Client Component — it holds the monthly/annual billing toggle's useState — and a Client
// Component cannot export metadata, so the page was inheriting app/[locale]/layout.tsx's
// generic site-wide title and description, the same pair shown on every other page under
// [locale]. A layout can export metadata even when its page cannot.
//
// Canonical is pinned to /en rather than built from the visitor's locale, matching the
// middleware rule that folds non-English marketing URLs onto English while those pages carry
// no real translations. When a locale passes the content gate, this becomes locale-aware in
// the same change that removes it from that redirect.
export const metadata: Metadata = {
  title: 'Upkilo Pricing — Plans for Salons, Spas, Studios & Clinics',
  description:
    'Simple monthly plans for service businesses. Bookings, client CRM, payments, reminders and AI automation included. 14-day free trial, no credit card required.',
  alternates: { canonical: `${SITE_URL}/en/pricing` },
  openGraph: {
    title: 'Upkilo Pricing — Plans for Service Businesses',
    description:
      'Bookings, CRM, payments and AI automation in one plan. 14-day free trial, no credit card required.',
    type: 'website',
  },
};

export default function PricingLayout({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
