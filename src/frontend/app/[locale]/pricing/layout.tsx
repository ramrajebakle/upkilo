import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import { safeJsonLd } from '@/lib/jsonLd';
import { PRICING_FAQS } from '@/lib/pricingFaqs';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// Offer schema for the two publicly-priced tiers, mirroring the `plans` array in page.tsx.
//
// USD is stated explicitly because Upkilo bills exclusively in USD
// (PricingIntegrityService.BillingCurrency) — the same fact that made the AUD/CAD/AED claims
// on the country pages wrong. A priceCurrency that disagreed with what is actually charged
// would be a misrepresentation in structured data, which is worse than in body copy because
// it is machine-read and can surface in a price comparison.
//
// Enterprise is deliberately absent: it is sales-led with no published price, and inventing
// a number to satisfy the schema would be the same class of fabrication as the testimonial
// and usage figures removed elsewhere in this branch. An Offer without a price is not useful,
// so the tier is simply not declared.
//
// Lives in the layout because page.tsx is a Client Component (billing-period toggle) and so
// cannot render server-side JSON-LD itself.
const PRICING_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'Product',
  name: 'Upkilo',
  description:
    'Booking, client CRM, payments, reminders and AI automation for service businesses.',
  brand: { '@type': 'Brand', name: 'Upkilo' },
  offers: [
    {
      '@type': 'Offer',
      name: 'Starter',
      description: 'For small teams running bookings and clients in one place.',
      price: '149',
      priceCurrency: 'USD',
      url: `${SITE_URL}/en/pricing`,
      availability: 'https://schema.org/InStock',
    },
    {
      '@type': 'Offer',
      name: 'Growth',
      description: 'For scaling businesses needing AI automation, white-label and API access.',
      price: '499',
      priceCurrency: 'USD',
      url: `${SITE_URL}/en/pricing`,
      availability: 'https://schema.org/InStock',
    },
  ],
};

// "How much does X cost" is the highest-intent question an answer engine gets asked about a
// SaaS product, and it was the one thing this page stated only in body copy — no machine-readable
// answer existed beyond the bare Offer prices above. FAQPage is what lets the answer be quoted
// directly, with attribution, rather than paraphrased from scraped markup.
//
// Built from the same PRICING_FAQS array the page renders visibly. That is not a convenience:
// FAQPage structured data must correspond to content the visitor can actually see, so the two
// cannot be allowed to drift apart.
const FAQ_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'FAQPage',
  mainEntity: PRICING_FAQS.map(({ question, answer }) => ({
    '@type': 'Question',
    name: question,
    acceptedAnswer: { '@type': 'Answer', text: answer },
  })),
};

// Breadcrumbs give the page a stated position in the site rather than leaving engines to infer
// one from URL depth, and render as a path in the search result instead of a bare URL.
const BREADCRUMB_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'BreadcrumbList',
  itemListElement: [
    { '@type': 'ListItem', position: 1, name: 'Home', item: `${SITE_URL}/en` },
    { '@type': 'ListItem', position: 2, name: 'Pricing', item: `${SITE_URL}/en/pricing` },
  ],
};

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
    url: `${SITE_URL}/en/pricing`,
    type: 'website',
  },
};

export default function PricingLayout({ children }: { children: ReactNode }) {
  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(PRICING_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(FAQ_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
      {children}
    </>
  );
}
