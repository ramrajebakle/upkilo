import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, SITE_URL } from '@/lib/seo';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

// Metadata lives in the layout rather than the page because app/enterprise/page.tsx is a
// genuine Client Component — it holds the enterprise lead form's useState — and a Client
// Component cannot export metadata. A layout can, even when its page cannot, so this is the
// low-risk fix for what was a page shipping with no <title> or <meta description> at all.
//
// Unlike the pages nested under app/[locale], this route has its own root layout and so had
// no parent metadata to inherit either — the tab title fell back to the bare URL.
//
// A fuller fix would extract the form into its own client component and make this page a
// Server Component, which would also let the static hero and feature sections stream as
// server-rendered HTML. That is a real improvement but a larger change; it is not required
// to give the page correct metadata, which is what actually blocked it from being indexed
// meaningfully.
export const metadata: Metadata = {
  title: 'Upkilo Enterprise — Multi-Location Booking & CRM for Chains',
  description: 'Enterprise booking, CRM and payments for multi-location chains and franchise groups. SSO/SAML, agency sub-accounts, unlimited staff and locations.',
  alternates: { canonical: 'https://upkilo.com/enterprise' },
  openGraph: {
    title: 'Upkilo Enterprise — Booking & CRM for Multi-Location Chains',
    description: 'SSO/SAML, agency sub-accounts and unlimited locations for franchise groups and chains.',
    type: 'website',
  },
};

// Service, not SoftwareApplication — same reasoning as the medical-spa page: this describes the
// enterprise engagement around the existing product, not a separate application.
//
// No Offer node: Enterprise is IsCustom in PricingSeeder with no price rows, and an Offer
// without a price tells an engine nothing while inviting it to infer one. The tier is sales-led,
// so the honest structured-data answer is that there is no published price.
const ENTERPRISE_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'Service',
  name: 'Upkilo Enterprise',
  serviceType: 'Multi-location booking, CRM and payments platform',
  description:
    'Enterprise booking, CRM and payments for multi-location chains and franchise groups, with SSO/SAML, agency sub-accounts and unlimited staff and locations.',
  url: `${SITE_URL}/enterprise`,
  provider: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  audience: { '@type': 'BusinessAudience', name: 'Multi-location chains and franchise groups' },
};

// This route sits at the apex, not under /en — the trail must match the real URL.
const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  { name: 'Home', path: '/en' },
  { name: 'Enterprise', path: '/enterprise' },
]);

export default function EnterpriseLayout({ children }: { children: ReactNode }) {
    return (
        <RootHtml
            lang="en"
            headChildren={
                <>
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(ENTERPRISE_JSON_LD) }} />
                    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
                </>
            }
        >
            {children}
        </RootHtml>
    );
}
