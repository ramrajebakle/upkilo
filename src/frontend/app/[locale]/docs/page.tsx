import type { Metadata } from 'next';
import { Link } from '@/navigation';
import { Globe, ArrowRight, BookOpen } from 'lucide-react';

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
    href: '/docs/custom-domains',
    icon: Globe,
    title: 'Custom Domains',
    description:
      'Point your own domain or subdomain at your Upkilo booking page, and set up SPF/DKIM so email sends from your own address.',
  },
];

export default function DocsIndexPage() {
  return (
    <main className="min-h-screen bg-white">
      <section className="mx-auto max-w-3xl px-4 py-20">
        <div className="mb-12">
          <span className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-600">
            <BookOpen className="h-4 w-4" aria-hidden="true" />
            Documentation
          </span>
          <h1 className="mt-6 text-4xl font-extrabold tracking-tight text-slate-900">
            Upkilo documentation
          </h1>
          <p className="mt-4 text-lg text-slate-600">
            Setup guides and reference for running your business on Upkilo. More guides are
            being added — if something you need is missing, the in-app help can point you at it.
          </p>
        </div>

        <ul className="space-y-4">
          {GUIDES.map(({ href, icon: Icon, title, description }) => (
            <li key={href}>
              <Link
                href={href}
                className="group flex gap-4 rounded-2xl border border-slate-200 p-6 transition-colors hover:border-slate-300 hover:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
              >
                <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-700">
                  <Icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <span className="min-w-0">
                  <span className="flex items-center gap-2 font-semibold text-slate-900">
                    {title}
                    <ArrowRight
                      className="h-4 w-4 text-slate-400 transition-transform group-hover:translate-x-0.5"
                      aria-hidden="true"
                    />
                  </span>
                  <span className="mt-1 block text-sm text-slate-600">{description}</span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
