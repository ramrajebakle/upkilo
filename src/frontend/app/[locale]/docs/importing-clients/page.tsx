import type { Metadata } from 'next';
import { Link } from '@/navigation';
import { Upload, Users, ShieldCheck, ArrowLeft, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB, SITE_URL } from '@/lib/seo';

// A Server Component, unlike docs/custom-domains which is 'use client' and therefore cannot
// export metadata. Documentation is the content type answer engines cite most readily because
// it is specific and factual, so it needs a real title, description and canonical, and it needs
// to be readable without executing JavaScript. Nothing on this page is interactive.
//
// Every statement here is checked against MigrationWizardController:
//   - .csv only, rejected by extension check          (Upload)
//   - 10 MB cap                                        (file.Length > 10 * 1024 * 1024)
//   - header row + at least one data row required      (lines.Length < 2)
//   - auto-detect from headers, or explicit platform   (DetectPlatform / GetParser)
//   - dedup on email or phone, duplicates skipped      (preview + execute)
//   - completion email on execute                      (IEmailService)
// Do not add steps here that the wizard does not perform.
export const metadata: Metadata = {
  title: 'Importing & Migrating Clients — Upkilo Docs',
  description:
    'Move your client list into Upkilo from a CSV export. Covers Mindbody, Vagaro and Acuity exports, column mapping, duplicate detection and what happens on import.',
  alternates: { canonical: `${SITE_URL}/en/docs/importing-clients` },
  openGraph: {
    title: 'Importing & Migrating Clients — Upkilo Docs',
    description: 'Move your client list into Upkilo from a CSV export, with duplicate detection.',
    url: `${SITE_URL}/en/docs/importing-clients`,
    type: 'article',
  },
};

// TechArticle rather than the generic WebPage: it tells an engine this is procedural reference
// material, which is what makes it quotable as an answer to "how do I move my clients to X".
const ARTICLE_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'TechArticle',
  headline: 'Importing & Migrating Clients',
  description:
    'How to move an existing client list into Upkilo from a CSV export, including Mindbody, Vagaro and Acuity formats.',
  url: `${SITE_URL}/en/docs/importing-clients`,
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  proficiencyLevel: 'Beginner',
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  HOME_CRUMB,
  { name: 'Docs', path: '/en/docs' },
  { name: 'Importing Clients', path: '/en/docs/importing-clients' },
]);

const SUPPORTED = [
  { name: 'Mindbody', where: 'Reports → Client List → Export' },
  { name: 'Vagaro', where: 'Reports → Client Report → Download CSV' },
  { name: 'Acuity Scheduling', where: 'Clients → Import/Export → Export client list' },
  { name: 'Any other platform', where: 'Export your clients as CSV — the importer matches columns by name' },
];

const COLUMNS = [
  { field: 'First name', accepted: 'first_name, firstname, first' },
  { field: 'Last name', accepted: 'last_name, lastname, last' },
  { field: 'Email', accepted: 'email, email_address' },
  { field: 'Phone', accepted: 'phone, mobile, cell_phone, home_phone' },
  { field: 'Date of birth', accepted: 'birth_date, dob, date_of_birth' },
];

export default function ImportingClientsGuidePage() {
  return (
    <main className="min-h-screen bg-white dark:bg-slate-950">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(ARTICLE_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />

      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12">
        <Link
          href="/docs"
          className="inline-flex items-center gap-2 text-sm font-medium text-slate-600 hover:text-primary-700 dark:text-slate-400 dark:hover:text-primary-400 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          All docs
        </Link>

        <header className="mt-8 mb-12">
          <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            Importing &amp; migrating clients
          </h1>
          <p className="mt-4 text-lg leading-relaxed text-slate-600 dark:text-slate-400">
            Bring an existing client list into Upkilo from a CSV export. The migration wizard
            detects your source platform, maps the columns, and skips anyone already in your
            account before writing a single record.
          </p>
        </header>

        <section className="mb-12" aria-labelledby="before-you-start">
          <h2 id="before-you-start" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <Upload className="h-5 w-5 text-primary-600" aria-hidden="true" />
            Before you start
          </h2>
          <ul className="mt-4 space-y-2 text-slate-700 dark:text-slate-300">
            <li className="flex gap-2">
              <CheckCircle2 className="mt-1 h-4 w-4 shrink-0 text-green-600" aria-hidden="true" />
              <span>Your file must be a <strong>.csv</strong>. Other formats are rejected at upload.</span>
            </li>
            <li className="flex gap-2">
              <CheckCircle2 className="mt-1 h-4 w-4 shrink-0 text-green-600" aria-hidden="true" />
              <span>Maximum file size is <strong>10&nbsp;MB</strong>. Split larger exports and import them in batches.</span>
            </li>
            <li className="flex gap-2">
              <CheckCircle2 className="mt-1 h-4 w-4 shrink-0 text-green-600" aria-hidden="true" />
              <span>The file needs a <strong>header row</strong> plus at least one row of data.</span>
            </li>
          </ul>
        </section>

        <section className="mb-12" aria-labelledby="export-from">
          <h2 id="export-from" className="text-2xl font-bold text-slate-900 dark:text-white">
            Exporting from your current platform
          </h2>
          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 dark:border-slate-800">
                  <th scope="col" className="py-3 pr-4 font-semibold text-slate-900 dark:text-white">Platform</th>
                  <th scope="col" className="py-3 font-semibold text-slate-900 dark:text-white">Where to find the export</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {SUPPORTED.map((s) => (
                  <tr key={s.name}>
                    <td className="py-3 pr-4 font-medium text-slate-900 dark:text-slate-200">{s.name}</td>
                    <td className="py-3 text-slate-600 dark:text-slate-400">{s.where}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="mt-4 text-sm text-slate-600 dark:text-slate-400">
            You can leave the platform on <strong>Auto-detect</strong>. Upkilo inspects the header
            row and picks the right format itself; choosing one manually only overrides that.
          </p>
        </section>

        <section className="mb-12" aria-labelledby="columns">
          <h2 id="columns" className="text-2xl font-bold text-slate-900 dark:text-white">
            Which columns are read
          </h2>
          <p className="mt-4 text-slate-700 dark:text-slate-300">
            Columns are matched by name, case-insensitively, with spaces treated as underscores.
            Anything not listed below is ignored rather than rejected, so you do not need to trim
            your export first.
          </p>
          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 dark:border-slate-800">
                  <th scope="col" className="py-3 pr-4 font-semibold text-slate-900 dark:text-white">Field</th>
                  <th scope="col" className="py-3 font-semibold text-slate-900 dark:text-white">Accepted column names</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {COLUMNS.map((c) => (
                  <tr key={c.field}>
                    <td className="py-3 pr-4 font-medium text-slate-900 dark:text-slate-200">{c.field}</td>
                    <td className="py-3 font-mono text-xs text-slate-600 dark:text-slate-400">{c.accepted}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="mb-12" aria-labelledby="duplicates">
          <h2 id="duplicates" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <ShieldCheck className="h-5 w-5 text-primary-600" aria-hidden="true" />
            Duplicates are skipped, not merged
          </h2>
          <p className="mt-4 text-slate-700 dark:text-slate-300">
            Before anything is written, Upkilo compares your file against the clients already in
            your account, matching on <strong>email or phone number</strong>. Anyone who matches is
            skipped. The preview step shows you how many rows are new and how many will be skipped,
            so you can re-run an import safely without creating a second copy of everyone.
          </p>
          <div className="mt-4 flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/20">
            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-600" aria-hidden="true" />
            <p className="text-sm text-amber-900 dark:text-amber-200">
              Skipping means an existing client&apos;s details are left untouched. If your export has
              newer phone numbers or emails, those changes are not applied to clients who already
              exist — update those records directly instead.
            </p>
          </div>
        </section>

        <section className="mb-12" aria-labelledby="steps">
          <h2 id="steps" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <Users className="h-5 w-5 text-primary-600" aria-hidden="true" />
            Running the import
          </h2>
          <ol className="mt-4 space-y-4 text-slate-700 dark:text-slate-300">
            <li className="flex gap-3">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary-100 text-xs font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-300">1</span>
              <span>Go to <strong>Settings → Migration</strong> and upload your CSV.</span>
            </li>
            <li className="flex gap-3">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary-100 text-xs font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-300">2</span>
              <span>Check the preview. It reports the total rows found, how many are new, and how many are duplicates.</span>
            </li>
            <li className="flex gap-3">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary-100 text-xs font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-300">3</span>
              <span>Confirm the import. Only then are records written to your account.</span>
            </li>
            <li className="flex gap-3">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary-100 text-xs font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-300">4</span>
              <span>You will receive a confirmation email when the import finishes. Your new clients appear under <strong>Clients</strong>.</span>
            </li>
          </ol>
        </section>

        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-6 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="font-bold text-slate-900 dark:text-white">Next steps</h2>
          <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
            With your clients in place, point your own domain at your booking page so clients book
            somewhere that looks like you.
          </p>
          <Link
            href="/docs/custom-domains"
            className="mt-4 inline-flex items-center gap-1.5 text-sm font-semibold text-primary-700 hover:text-primary-800 dark:text-primary-400"
          >
            Custom domains guide →
          </Link>
        </div>
      </div>
    </main>
  );
}
