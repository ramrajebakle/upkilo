import type { Metadata } from 'next';
import { Link } from '@/navigation';
import { Globe, ArrowRight, Upload, Rocket, ClipboardCheck } from 'lucide-react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB } from '@/lib/seo';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// robots.ts has allowed /docs since it was written, and middleware.ts lists 'docs' as a
// marketing segment — but no page existed at this level, only docs/custom-domains one level
// down, so /en/docs returned a 404 to anything that followed those signals.
//
// Deliberately a Server Component with real metadata: documentation is the content type
// answer engines cite most readily, because it is specific, structured and factual rather
// than persuasive. It needs to be crawlable without executing JavaScript.
export const metadata: Metadata = {
  title: 'Upkilo Docs — Setup Guides & Product Documentation',
  description:
    'Guides for setting up and running Upkilo: custom domains, booking pages, client records, payments and reminders.',
  alternates: { canonical: `${SITE_URL}/en/docs` },
  openGraph: {
    title: 'Upkilo Docs — Setup Guides & Product Documentation',
    description: 'Setup guides and product documentation for Upkilo.',
    type: 'website',
  },
};

// Only guides that actually exist are listed. An index that links to pages which have not
// been written yet produces 404s for both readers and crawlers — worse than a short index.
// Add entries here as guides are written.
const GUIDES = [
  {
    href: '/docs/getting-started',
    icon: Rocket,
    title: 'Getting Started',
    description:
      'The full setup path for a new account: business profile, working hours, services, staff, booking page, payments, and your first booking and client.',
  },
  {
    href: '/docs/booking-policies',
    icon: ClipboardCheck,
    title: 'Booking Policies, Deposits & Cancellations',
    description:
      'Advance notice, booking windows, cancellation rules, buffers, reminders, deposits and no-show fees — every setting and what it does.',
  },
  {
    href: '/docs/importing-clients',
    icon: Upload,
    title: 'Importing & Migrating Clients',
    description:
      'Move your client list in from a CSV export, including Mindbody, Vagaro and Acuity formats, with duplicate detection before anything is written.',
  },
  {
    href: '/docs/custom-domains',
    icon: Globe,
    title: 'Custom Domains',
    description:
      'Point your own domain or subdomain at your Upkilo booking page, and set up SPF/DKIM so email sends from your own address.',
  },
];

// ItemList generated from GUIDES, so the index declares exactly the guides that exist. Listing
// a guide here that has not been written would advertise a 404 to crawlers in machine-readable
// form — the same failure the comment above guards against for human readers.
const DOCS_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'ItemList',
  name: 'Upkilo Documentation',
  description: 'Setup guides and product documentation for Upkilo.',
  itemListElement: GUIDES.map((guide, i) => ({
    '@type': 'ListItem',
    position: i + 1,
    name: guide.title,
    description: guide.description,
    url: `${SITE_URL}/en${guide.href}`,
  })),
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([HOME_CRUMB, { name: 'Docs', path: '/en/docs' }]);

export default function DocsIndexPage() {
  return (
    <main className="min-h-screen bg-card">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(DOCS_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
      <section className="mx-auto max-w-3xl px-4 py-20">
        <div className="mb-12">
          {/* No eyebrow pill above the heading. A "Documentation" label sitting above a
              heading that already reads "Upkilo documentation" restates it in smaller type —
              the heading carries its own weight. */}
          <h1 className="text-4xl font-extrabold tracking-tight text-foreground">
            Upkilo documentation
          </h1>
          <p className="mt-4 text-lg text-foreground-secondary">
            Setup guides and reference for running your business on Upkilo. More guides are
            being added — if something you need is missing, the in-app help can point you at it.
          </p>
        </div>

        <ul className="space-y-4">
          {GUIDES.map(({ href, icon: Icon, title, description }) => (
            <li key={href}>
              <Link
                href={href}
                className="group flex gap-4 rounded-2xl border border-border p-6 transition-colors hover:border-border-strong hover:bg-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
              >
                <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-muted text-foreground">
                  <Icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <span className="min-w-0">
                  <span className="flex items-center gap-2 font-semibold text-foreground">
                    {title}
                    <ArrowRight
                      className="h-4 w-4 text-foreground-muted transition-transform group-hover:translate-x-0.5"
                      aria-hidden="true"
                    />
                  </span>
                  <span className="mt-1 block text-sm text-foreground-secondary">{description}</span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
