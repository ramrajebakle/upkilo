import type { Metadata } from 'next';
import { Link } from '@/navigation';
import { Clock, CreditCard, Shield, ArrowLeft, AlertTriangle } from 'lucide-react';
import { safeJsonLd } from '@/lib/jsonLd';
import { breadcrumbJsonLd, HOME_CRUMB, SITE_URL } from '@/lib/seo';

// Every field, unit and default below is taken from the BookingPolicies interface and DEFAULT
// object in app/[locale]/(dashboard)/settings/booking-policies/page.tsx. Documented defaults are
// the thing readers trust most and check least, so they are copied rather than recalled — a
// wrong default here silently teaches people the wrong thing about their own account.
export const metadata: Metadata = {
  title: 'Booking Policies, Deposits & Cancellations — Upkilo Docs',
  description:
    'Configure notice periods, booking windows, cancellation rules, deposits, no-show fees and reminders in Upkilo, and what each setting does to the client booking flow.',
  alternates: { canonical: `${SITE_URL}/en/docs/booking-policies` },
  openGraph: {
    title: 'Booking Policies, Deposits & Cancellations — Upkilo Docs',
    description: 'Notice periods, cancellation windows, deposits, no-show fees and reminders.',
    url: `${SITE_URL}/en/docs/booking-policies`,
    type: 'article',
  },
};

const ARTICLE_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'TechArticle',
  headline: 'Booking policies, deposits and cancellations',
  description:
    'How Upkilo booking policies work: advance notice, booking window, cancellation window, buffers, reminders, deposits and no-show fees.',
  url: `${SITE_URL}/en/docs/booking-policies`,
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  proficiencyLevel: 'Beginner',
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  HOME_CRUMB,
  { name: 'Docs', path: '/en/docs' },
  { name: 'Booking Policies', path: '/en/docs/booking-policies' },
]);

const SCHEDULING = [
  {
    label: 'Min. advance notice',
    unit: 'hours',
    def: '2',
    what: 'How soon before a slot a client may still book it. At the default, a 3pm slot stops being bookable online at 1pm. Raise it if you need preparation time; lower it if you want to fill same-day gaps.',
  },
  {
    label: 'Max. days ahead',
    unit: 'days',
    def: '60',
    what: 'How far into the future the booking page will show availability. Keeping this tight reduces the number of bookings that get rescheduled because plans changed.',
  },
  {
    label: 'Cancellation window',
    unit: 'hours',
    def: '24',
    what: 'The cut-off before the appointment after which a client can no longer cancel themselves online. This is also the boundary your no-show fee is meant to sit behind.',
  },
  {
    label: 'Buffer between appointments',
    unit: 'minutes',
    def: '0',
    what: 'Padding added after every booking, for turnaround, cleaning or notes. The buffer is held on the calendar, so it cannot be booked over.',
  },
  {
    label: 'Reminder before',
    unit: 'hours',
    def: '24',
    what: 'When the automatic reminder goes out. Set this comfortably outside your cancellation window, so a client who cannot attend still has time to cancel rather than simply not turning up.',
  },
];

const REFUND_TIERS = [
  {
    label: 'Full refund beyond',
    unit: 'hours',
    def: '18',
    what: 'Cancel with more notice than this and the whole deposit is returned automatically.',
  },
  {
    label: 'Partial refund beyond',
    unit: 'hours',
    def: '12',
    what: 'Between this and the full-refund mark, part of the deposit is returned. Below it, nothing is. Must be shorter than the full-refund window — the form and the API both refuse to save it the other way round.',
  },
  {
    label: 'Partial refund amount',
    unit: '%',
    def: '50',
    what: 'How much comes back inside that middle band.',
  },
];

const PERMISSIONS = [
  { label: 'Allow online rescheduling', def: 'On', what: 'Clients can move their own appointment from the booking portal instead of contacting you.' },
  { label: 'Allow online cancellation', def: 'On', what: 'Clients can cancel without calling. Turning this off does not stop cancellations — it moves them to your phone.' },
  { label: 'Auto-confirm bookings', def: 'On', what: 'New bookings are confirmed immediately with no manual review. Turn it off if you want to approve each one first.' },
  { label: 'Require client confirmation', def: 'Off', what: 'The client must confirm the booking by email or SMS before it is treated as confirmed.' },
];

function FieldTable({ rows, showUnit }: { rows: { label: string; unit?: string; def: string; what: string }[]; showUnit: boolean }) {
  return (
    <div className="mt-4 overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-200 dark:border-slate-800">
            <th scope="col" className="py-3 pr-4 font-semibold text-slate-900 dark:text-white">Setting</th>
            {showUnit && <th scope="col" className="py-3 pr-4 font-semibold text-slate-900 dark:text-white">Unit</th>}
            <th scope="col" className="py-3 pr-4 font-semibold text-slate-900 dark:text-white">Default</th>
            <th scope="col" className="py-3 font-semibold text-slate-900 dark:text-white">What it does</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {rows.map((r) => (
            <tr key={r.label}>
              <td className="py-3 pr-4 align-top font-medium text-slate-900 dark:text-slate-200">{r.label}</td>
              {showUnit && <td className="py-3 pr-4 align-top text-slate-600 dark:text-slate-400">{r.unit}</td>}
              <td className="py-3 pr-4 align-top font-mono text-xs text-slate-600 dark:text-slate-400">{r.def}</td>
              <td className="py-3 align-top leading-relaxed text-slate-600 dark:text-slate-400">{r.what}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default function BookingPoliciesGuidePage() {
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

        <header className="mt-8 mb-10">
          <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            Booking policies, deposits &amp; cancellations
          </h1>
          <p className="mt-4 text-lg leading-relaxed text-slate-600 dark:text-slate-400">
            These rules decide what your booking page will and will not let a client do. You will
            find them all under <strong>Settings → Booking Policies</strong>, and they take effect
            as soon as you save.
          </p>
        </header>

        <section className="mb-12" aria-labelledby="scheduling">
          <h2 id="scheduling" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <Clock className="h-5 w-5 text-primary-600" aria-hidden="true" />
            Scheduling rules
          </h2>
          <FieldTable rows={SCHEDULING} showUnit />
        </section>

        <section className="mb-12" aria-labelledby="deposits">
          <h2 id="deposits" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <CreditCard className="h-5 w-5 text-primary-600" aria-hidden="true" />
            Deposits &amp; refunds
          </h2>
          <p className="mt-4 leading-relaxed text-slate-700 dark:text-slate-300">
            Refund rules are set <strong>per service</strong>, not once for the whole business —
            you will find them on each service under <strong>Services → edit → Requires payment</strong>.
            A quick consultation and an all-day treatment rarely deserve the same notice period,
            and the service is the only place where that difference is actually known.
          </p>
          <p className="mt-3 leading-relaxed text-slate-700 dark:text-slate-300">
            Two thresholds produce three outcomes, measured against how long remains until the
            appointment starts:
          </p>
          <FieldTable rows={REFUND_TIERS} showUnit />
          <div className="mt-4 flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/20">
            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-600" aria-hidden="true" />
            <p className="text-sm text-amber-900 dark:text-amber-200">
              Deposits and no-show fees are charged to your clients through your own connected
              payment account, in your own currency. That is separate from your Upkilo
              subscription, which is billed in USD. Charging a no-show fee is also a decision with
              legal weight in some places — state the policy on your booking page before you
              enable it.
            </p>
          </div>
        </section>

        <section className="mb-12" aria-labelledby="permissions">
          <h2 id="permissions" className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <Shield className="h-5 w-5 text-primary-600" aria-hidden="true" />
            What clients are allowed to do
          </h2>
          <FieldTable rows={PERMISSIONS} showUnit={false} />
        </section>

        <section aria-labelledby="combining">
          <h2 id="combining" className="text-2xl font-bold text-slate-900 dark:text-white">
            How these interact
          </h2>
          <p className="mt-4 leading-relaxed text-slate-700 dark:text-slate-300">
            The settings are individually simple and easy to get into a contradictory state
            together. Two worth checking after any change:
          </p>
          <ul className="mt-4 space-y-3 text-slate-700 dark:text-slate-300">
            <li className="flex gap-2">
              <span aria-hidden="true" className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-primary-600" />
              <span>
                If your <strong>reminder</strong> goes out later than your{' '}
                <strong>cancellation window</strong> closes, clients receive their reminder at a
                point where they can no longer cancel — which converts would-be cancellations into
                no-shows.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden="true" className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-primary-600" />
              <span>
                Your <strong>full-refund window</strong> is set per service, while the{' '}
                <strong>cancellation window</strong> above is set once for the business. If a
                service allows a full refund at 18 hours but your cancellation window closes at
                24, a client hits a period where they can still be refunded but can no longer
                cancel themselves — so they call you instead.
              </span>
            </li>
          </ul>
          <p className="mt-6 text-sm text-slate-600 dark:text-slate-400">
            Every service starts at 18 hours for a full refund, 12 hours for 50%, and nothing
            inside 12. If those suit you, there is nothing to change — but they apply to each
            service individually, so a service you create later starts from the same defaults
            rather than inheriting whatever you set on the last one.
          </p>
        </section>
      </div>
    </main>
  );
}
