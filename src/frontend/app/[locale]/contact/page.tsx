import type { Metadata } from 'next';
import { Link } from '@/navigation';
import { ShieldCheck, Building2, LifeBuoy } from 'lucide-react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB } from '@/lib/seo';

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([HOME_CRUMB, { name: 'Contact', path: '/en/contact' }]);

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// The landing page footer has linked to /contact since it was written, but no page existed —
// so the highest-authority page on the site sent both people and crawlers to a 404.
//
// Worth building properly rather than deleting the link: reachable, specific contact
// information is one of the clearer trust signals search engines weigh, and a software
// business with no way to reach a human reads as less legitimate to a reviewer or a buyer.
//
// Every address below is already used elsewhere in the product — hello@ on the landing page,
// contact@ on invoices, enterprise@ on the enterprise page, grievance@ in the privacy and
// cookie policies as the DPDP Act contact. Nothing here is invented.
export const metadata: Metadata = {
  title: 'Contact Upkilo — Support, Sales & Privacy Enquiries',
  description:
    'Get in touch with Upkilo. Product support, enterprise and multi-location enquiries, and privacy or data-protection requests.',
  alternates: { canonical: `${SITE_URL}/en/contact` },
  openGraph: {
    title: 'Contact Upkilo',
    description: 'Product support, enterprise enquiries, and privacy requests.',
    type: 'website',
  },
};

// ContactPage + ContactPoint schema. This is the structured form of the same trust signal:
// it lets a search engine and an answer engine resolve "how do I contact Upkilo" without
// parsing the page, and reinforces the Organization entity declared on the landing page by
// pointing at the same @id.
const CONTACT_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'ContactPage',
  url: `${SITE_URL}/en/contact`,
  mainEntity: {
    '@type': 'Organization',
    '@id': `${SITE_URL}/#organization`,
    name: 'Upkilo',
    url: SITE_URL,
    contactPoint: [
      {
        '@type': 'ContactPoint',
        contactType: 'customer support',
        email: 'hello@upkilo.com',
        availableLanguage: ['English'],
      },
      {
        '@type': 'ContactPoint',
        contactType: 'sales',
        email: 'enterprise@upkilo.com',
        availableLanguage: ['English'],
      },
      {
        '@type': 'ContactPoint',
        contactType: 'privacy',
        email: 'grievance@upkilo.com',
        availableLanguage: ['English'],
      },
    ],
  },
};

const CHANNELS = [
  {
    icon: LifeBuoy,
    title: 'Product support',
    body: 'Questions about bookings, clients, payments or anything else in the product.',
    email: 'hello@upkilo.com',
  },
  {
    icon: Building2,
    title: 'Enterprise & multi-location',
    body: 'Chains, franchise groups, agency sub-accounts and SSO requirements.',
    email: 'enterprise@upkilo.com',
  },
  {
    icon: ShieldCheck,
    title: 'Privacy & data protection',
    body: 'Data access, correction and erasure requests, and DPDP Act grievances.',
    email: 'grievance@upkilo.com',
  },
];

export default function ContactPage() {
  return (
    <main className="min-h-screen bg-white">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(CONTACT_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />

      <section className="mx-auto max-w-3xl px-4 py-20">
        <div className="mb-14">
          {/* No eyebrow pill above the heading — see the equivalent note in docs/page.tsx. */}
          <h1 className="text-4xl font-extrabold tracking-tight text-slate-900">
            Talk to us
          </h1>
          <p className="mt-4 text-lg text-slate-600">
            Email reaches a person. Pick whichever fits — anything unclear can go to the first
            one and we&apos;ll route it.
          </p>
        </div>

        <ul className="space-y-4">
          {CHANNELS.map(({ icon: Icon, title, body, email }) => (
            <li
              key={email}
              className="flex gap-4 rounded-2xl border border-slate-200 p-6"
            >
              <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-700">
                <Icon className="h-5 w-5" aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <h2 className="font-semibold text-slate-900">{title}</h2>
                <p className="mt-1 text-sm text-slate-600">{body}</p>
                <a
                  href={`mailto:${email}`}
                  className="mt-3 inline-block text-sm font-medium text-primary-600 underline underline-offset-2 hover:text-primary-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600"
                >
                  {email}
                </a>
              </div>
            </li>
          ))}
        </ul>

        <p className="mt-10 text-sm text-slate-500">
          Looking for setup help first? The{' '}
          <Link href="/docs" className="text-primary-600 underline underline-offset-2 hover:text-primary-700">
            documentation
          </Link>{' '}
          covers configuration and common questions.
        </p>
      </section>
    </main>
  );
}
