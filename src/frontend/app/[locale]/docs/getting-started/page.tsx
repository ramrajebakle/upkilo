import type { Metadata } from 'next';
import { Link } from '@/navigation';
import {
  Building2, Calendar, Briefcase, Users, Palette, CreditCard, PlusCircle, UserPlus,
  ArrowLeft, Info,
} from 'lucide-react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB, SITE_URL } from '@/lib/seo';

// The end-to-end setup path, and the hub the other guides hang off.
//
// The eight steps below are not an invented "best practice" list — they mirror STEP_META in
// app/[locale]/(auth)/onboarding/page.tsx exactly, in the same order, with the same
// destinations. That checklist is what a new account actually sees, and it gates each step on
// the previous one being complete, so documenting a different order would describe a flow the
// product does not offer. If STEP_META changes, this page changes with it.
export const metadata: Metadata = {
  title: 'Getting Started — Set Up Upkilo End to End | Upkilo Docs',
  description:
    'The complete setup path for a new Upkilo account: business profile, working hours, services, staff, booking page, payments, and your first booking and client.',
  alternates: { canonical: `${SITE_URL}/en/docs/getting-started` },
  openGraph: {
    title: 'Getting Started — Set Up Upkilo End to End',
    description:
      'Business profile, hours, services, staff, booking page, payments, first booking — the full setup path.',
    url: `${SITE_URL}/en/docs/getting-started`,
    type: 'article',
  },
};

// HowTo, not TechArticle: this page is a sequence of steps toward one outcome, which is exactly
// what HowTo describes — and it is the shape an answer engine can return as an ordered checklist
// rather than a paragraph. The step names and order match the in-app checklist.
const HOWTO_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'HowTo',
  name: 'Set up an Upkilo account end to end',
  description:
    'Configure a new Upkilo account so it can take online bookings: business profile, working hours, services, staff, booking page, payments, first booking and first client.',
  url: `${SITE_URL}/en/docs/getting-started`,
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  step: [
    { name: 'Set up your business profile', text: 'Add your business name, logo, address and contact details.' },
    { name: 'Configure working hours', text: 'Tell clients when you are open for bookings.' },
    { name: 'Add your first service', text: 'Create the services or treatments you offer, with duration and price.' },
    { name: 'Invite your team', text: 'Add the staff members who take bookings.' },
    { name: 'Customize your booking page', text: 'Brand your public booking link with your colours and logo.' },
    { name: 'Set up payments', text: 'Connect a payment gateway to collect deposits or full payments.' },
    { name: 'Create your first booking', text: 'Add a booking manually or share your booking link.' },
    { name: 'Add a client', text: 'Import your client list or add a client record manually.' },
  ].map((s, i) => ({
    '@type': 'HowToStep',
    position: i + 1,
    name: s.name,
    text: s.text,
    url: `${SITE_URL}/en/docs/getting-started#step-${i + 1}`,
  })),
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  HOME_CRUMB,
  { name: 'Docs', path: '/en/docs' },
  { name: 'Getting Started', path: '/en/docs/getting-started' },
]);

// Mirrors STEP_META in the onboarding checklist — same labels, same order, same destinations.
const STEPS = [
  {
    icon: Building2,
    label: 'Set up your business profile',
    where: 'Settings → Business',
    detail:
      'Your business name, logo, address and contact details. This is what clients see on your booking page and on every confirmation and reminder you send, so it is the first step for a reason.',
  },
  {
    icon: Calendar,
    label: 'Configure working hours',
    where: 'Settings → Hours',
    detail:
      'The days and times you are open. Upkilo only ever offers slots inside these hours, so nothing else you configure can produce a bookable slot until this is set.',
  },
  {
    icon: Briefcase,
    label: 'Add your first service',
    where: 'Services',
    detail:
      'Each service carries a duration and a price. Duration is what determines how a slot occupies the calendar, and price is what any deposit percentage is calculated against later.',
  },
  {
    icon: Users,
    label: 'Invite your team',
    where: 'Staff',
    detail:
      'Add the people who take bookings. Each staff member has their own availability, and your plan sets how many seats you get — extra seats are available as an add-on rather than requiring a higher tier.',
  },
  {
    icon: Palette,
    label: 'Customize your booking page',
    where: 'Settings → Branding',
    detail:
      'Your colours and logo on the public booking link. On paid plans the "Powered by Upkilo" footer is removed; on the free plan it stays.',
  },
  {
    icon: CreditCard,
    label: 'Set up payments',
    where: 'Payments',
    detail:
      'Connect a payment gateway so you can take deposits or full payment at the time of booking. Your clients pay you in your own currency through your own connected account — this is separate from your Upkilo subscription, which is billed in USD.',
  },
  {
    icon: PlusCircle,
    label: 'Create your first booking',
    where: 'Bookings → New',
    detail:
      'Add one manually to see the whole flow end to end — confirmation, reminder and calendar entry — before you send the link to real clients.',
  },
  {
    icon: UserPlus,
    label: 'Add a client',
    where: 'Clients',
    detail:
      'Add one by hand, or bring your whole list across from your previous platform in one go.',
  },
];

export default function GettingStartedGuidePage() {
  return (
    <main className="min-h-screen bg-white dark:bg-slate-950">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(HOWTO_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />

      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8 py-12">
        <Link
          href="/docs"
          className="inline-flex items-center gap-2 text-sm font-medium text-slate-600 hover:text-primary-700 dark:text-slate-400 dark:hover:text-primary-400 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          All docs
        </Link>

        <header className="mt-8 mb-10">
          <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            Getting started
          </h1>
          <p className="mt-4 text-lg leading-relaxed text-slate-600 dark:text-slate-400">
            Everything a new account needs before it can take its first online booking. These are
            the same eight steps as the setup checklist inside Upkilo, in the same order — each one
            unlocks as the previous is finished, because each genuinely depends on the last.
          </p>
        </header>

        <div className="mb-10 flex gap-3 rounded-xl border border-blue-200 bg-blue-50 p-4 dark:border-blue-900/40 dark:bg-blue-950/20">
          <Info className="mt-0.5 h-5 w-5 shrink-0 text-blue-600" aria-hidden="true" />
          <p className="text-sm text-blue-900 dark:text-blue-200">
            You can follow this guide, or work through the same checklist in the app — it tracks
            your progress automatically and shows what is left.
          </p>
        </div>

        <ol className="space-y-8">
          {STEPS.map((step, i) => (
            <li key={step.label} id={`step-${i + 1}`} className="scroll-mt-24">
              <div className="flex gap-4">
                <div className="flex flex-col items-center">
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary-100 font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-300">
                    {i + 1}
                  </span>
                  {i < STEPS.length - 1 && (
                    <span className="mt-2 w-px flex-1 bg-slate-200 dark:bg-slate-800" aria-hidden="true" />
                  )}
                </div>
                <div className="pb-2">
                  <h2 className="flex items-center gap-2 text-xl font-bold text-slate-900 dark:text-white">
                    <step.icon className="h-5 w-5 text-primary-600" aria-hidden="true" />
                    {step.label}
                  </h2>
                  <p className="mt-1 text-sm font-medium text-slate-500 dark:text-slate-500">{step.where}</p>
                  <p className="mt-2 leading-relaxed text-slate-700 dark:text-slate-300">{step.detail}</p>
                </div>
              </div>
            </li>
          ))}
        </ol>

        <section className="mt-14" aria-labelledby="next">
          <h2 id="next" className="text-2xl font-bold text-slate-900 dark:text-white">
            Where to go next
          </h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <Link
              href="/docs/importing-clients"
              className="rounded-2xl border border-slate-200 p-5 transition-colors hover:border-primary-400 dark:border-slate-800"
            >
              <h3 className="font-bold text-slate-900 dark:text-white">Importing your clients</h3>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                Bring your list across from Mindbody, Vagaro, Acuity or any CSV.
              </p>
            </Link>
            <Link
              href="/docs/booking-policies"
              className="rounded-2xl border border-slate-200 p-5 transition-colors hover:border-primary-400 dark:border-slate-800"
            >
              <h3 className="font-bold text-slate-900 dark:text-white">Booking policies &amp; deposits</h3>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                Notice periods, cancellation windows, deposits and no-show fees.
              </p>
            </Link>
            <Link
              href="/docs/custom-domains"
              className="rounded-2xl border border-slate-200 p-5 transition-colors hover:border-primary-400 dark:border-slate-800"
            >
              <h3 className="font-bold text-slate-900 dark:text-white">Custom domains</h3>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                Put your booking page on your own domain, and send email from it.
              </p>
            </Link>
            <Link
              href="/pricing"
              className="rounded-2xl border border-slate-200 p-5 transition-colors hover:border-primary-400 dark:border-slate-800"
            >
              <h3 className="font-bold text-slate-900 dark:text-white">Plans &amp; add-ons</h3>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                What each plan includes, and how to add seats or locations without changing tier.
              </p>
            </Link>
          </div>
        </section>
      </div>
    </main>
  );
}
