import type { Metadata } from 'next';
import { Link } from '@/navigation';
import {
  Calendar,
  Users,
  Zap,
  BarChart3,
  MessageSquare,
  CreditCard,
  CheckCircle2,
  ShieldCheck,
  Lock,
  Server,
  Download,
  ArrowRight,
  Sparkles,
  UserPlus,
  Settings2,
  Share2,
  TrendingUp,
  Scissors,
  Flower2,
  Syringe,
  Smile,
  Car,
  HeartHandshake,
  Activity,
  PersonStanding,
  Mail,
  MessageCircle,
  Phone,
} from 'lucide-react';
import Reveal from '@/components/landing/Reveal';
import FaqAccordion from '@/components/landing/FaqAccordion';
import LandingNav from '@/components/landing/LandingNav';
import EmailCapture from '@/components/landing/EmailCapture';
import { safeJsonLd } from '@/lib/jsonLd';

// Apex host. Must match sitemap.ts / robots.ts and middleware.ts's SITE_URL — the JSON-LD
// entity below is identified by a URL, so a mismatch here would describe a different entity.
const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

export const metadata: Metadata = {
  title: 'Upkilo — Grow Your Service Business on Autopilot',
  // "Trusted by 500+ service businesses" was removed from the end of this description.
  // It is unverifiable today, and a meta description is the worst place to carry an
  // unsupportable claim — it is the text shown in the search result itself, read by people
  // who have not visited the site yet.
  description:
    'AI-powered booking, CRM, payments and marketing for med spas, aesthetic and dental clinics, hair salons, spas, massage, physiotherapy, chiropractic and auto detailing. Start free, no credit card needed.',
  // Every locale prefix renders this same English page — the marketing pages carry no
  // translations (only the dashboard calls useTranslations), so /en, /fr, /de … are 15 URLs
  // of identical content competing with each other. Pinning the canonical to /en consolidates
  // them onto one, matching what pricing/layout.tsx already does. If these pages are ever
  // genuinely translated, this must become per-locale canonicals plus hreflang alternates —
  // a hard canonical would otherwise deindex the translations.
  alternates: { canonical: `${SITE_URL}/en` },
  openGraph: {
    title: 'Upkilo — Grow Your Service Business on Autopilot',
    description:
      'AI-powered booking, CRM, payments, and marketing — built for service businesses. Start your 14-day free trial.',
    url: `${SITE_URL}/en`,
    type: 'website',
  },
};

const FEATURES = [
  {
    icon: Calendar,
    title: 'Smart Booking',
    description:
      'AI-powered scheduling adapts to your business rules, staff availability, and client preferences — automatically filling gaps.',
  },
  {
    icon: Users,
    title: 'CRM & Client Hub',
    description:
      'Full client history, preferences, notes, loyalty points, and purchases in one place. Know your client before they walk in.',
  },
  {
    icon: Zap,
    title: 'Marketing Automation',
    description:
      'Drip campaigns, SMS sequences, and email workflows that run automatically. Re-engage lapsed clients and fill empty slots.',
  },
  {
    icon: CreditCard,
    title: 'Payments & Billing',
    description:
      'Accept cards, UPI, subscriptions, packages, and gift cards. Send invoices, collect deposits, and track revenue in real time.',
  },
  {
    icon: MessageSquare,
    title: 'AI Chatbot',
    description:
      'A 24/7 booking assistant that handles new inquiries, rescheduling, and FAQs — without ever involving your staff.',
  },
  {
    icon: BarChart3,
    title: 'Analytics & Insights',
    description:
      'Revenue funnels, staff performance, campaign ROI, and retention metrics — in a dashboard built for growth decisions.',
  },
];

const STEPS = [
  {
    icon: UserPlus,
    title: 'Create your account',
    description: 'Sign up free in under two minutes — no credit card, no setup fees, no contracts.',
  },
  {
    icon: Settings2,
    title: 'Set up services & staff',
    description: 'Add your services, team, and working hours. Import existing clients with our migration wizard.',
  },
  {
    icon: Share2,
    title: 'Share your booking page',
    description: 'Drop your booking link anywhere — website, Instagram bio, or WhatsApp. Clients book themselves, 24/7.',
  },
  {
    icon: TrendingUp,
    title: 'Grow on autopilot',
    description: 'Automated reminders, waitlists, and campaigns keep your calendar full while you focus on clients.',
  },
];

const GUARANTEES = [
  { icon: CheckCircle2, label: '14-day free trial', sub: 'Every feature included' },
  { icon: CreditCard, label: 'No credit card', sub: 'Zero risk to start' },
  { icon: Zap, label: '~10-minute setup', sub: 'Go live the same day' },
  { icon: Sparkles, label: 'Cancel anytime', sub: 'No lock-in contracts' },
];

const INTEGRATIONS = ['Stripe', 'Razorpay', 'Google Calendar', 'WhatsApp', 'Twilio', 'Mailgun', 'Zoom', 'QuickBooks'];

// Fitness & Yoga Studios removed — Upkilo no longer serves that vertical, and a landing page
// that advertises it draws trials from businesses the product is not built for, which converts
// badly and costs support time on both sides. Removed from the metadata, JSON-LD and hero copy
// on this page too, so the claim does not survive anywhere a search engine can read it.
//
// The eight below are the verticals the product genuinely supports: each is appointment-led,
// staff-and-room based, and served by features that already exist — digital waivers and
// consent for the clinical ones, treatment plans and insurance pre-auth in the medical
// vertical, deposits and per-service refund policies throughout. Eight also fills the
// md:grid-cols-4 grid as two even rows, where three left a ragged 2+1.
//
// Auto detailing is included: the gap that argued against it — no record of the customer's
// vehicle — is now filled by the Vehicle entity and per-vehicle-class pricing, so a quote can
// reflect an SUV taking longer than a coupe rather than pretending every job is the same size.
// Nine entries render as a clean 3×3; the grid below was md:grid-cols-4, which left a ragged row.
const INDUSTRIES = [
  { icon: Syringe, label: 'Med Spas' },
  { icon: Sparkles, label: 'Aesthetic & Beauty Clinics' },
  { icon: Smile, label: 'Dental Practices' },
  { icon: Scissors, label: 'Hair Salons' },
  { icon: Flower2, label: 'Spas' },
  { icon: HeartHandshake, label: 'Massage Businesses' },
  { icon: Activity, label: 'Physiotherapy Clinics' },
  { icon: PersonStanding, label: 'Chiropractic Clinics' },
  { icon: Car, label: 'Auto Detailing' },
];

const SECURITY = [
  { icon: Lock, label: 'Encrypted by default', sub: 'TLS in transit · AES-256 at rest' },
  { icon: Download, label: 'Data export & erasure', sub: 'Built-in data-request tools' },
  { icon: ShieldCheck, label: 'Role-based access', sub: 'Granular permissions + audit logs' },
  { icon: Server, label: 'Secure cloud hosting', sub: 'Automated encrypted backups' },
];

// planKey must match a plan Name in PricingSeeder.cs — it is the lookup key into the live
// prices fetched below. The keys here were left on the pre-consolidation names
// (Professional / Agency) after the tiers became Starter / Growth / Enterprise, so two of
// the three cards silently fell through to "Contact us" instead of showing a price.
const PLANS = [
  {
    name: 'Starter',
    planKey: 'Starter',
    description: 'For small teams running bookings and clients in one place.',
    features: [
      'Up to 10 staff',
      'Up to 3 locations',
      'Up to 5,000 clients',
      'AI Copilot (2,000 actions/mo)',
      'SMS & email reminders',
    ],
    cta: 'Start free trial',
    href: '/register',
    highlight: false,
  },
  {
    name: 'Growth',
    planKey: 'Growth',
    description: 'For scaling businesses that need AI automation.',
    features: [
      'Up to 25 staff',
      'Up to 10 locations',
      'Unlimited clients',
      'AI Workflows & Insights (10,000 actions/mo)',
      'Marketing automation & campaigns',
      'White-label booking pages, API & webhooks',
      'Priority support',
    ],
    cta: 'Start free trial',
    href: '/register',
    highlight: true,
  },
  {
    name: 'Enterprise',
    planKey: 'Enterprise',
    description: 'For multi-brand and large organisations.',
    features: [
      'Everything in Growth',
      'Unlimited staff & locations',
      'SSO / SAML & extended audit logs',
      '100,000 AI actions / month',
      'Agency sub-account management',
      'Custom integrations & SLA',
      'Dedicated account manager',
    ],
    cta: 'Contact sales',
    href: '/enterprise',
    highlight: false,
  },
];

const FAQS = [
  {
    q: 'Do I need a credit card to start the free trial?',
    a: "No. You can explore all features for 14 days without entering any payment information. Upgrade only when you're ready.",
  },
  {
    q: 'Can I import my existing client and booking data?',
    // Was "CSV, Fresha, Mindbody, and Vagaro". Fresha has no parser and appears nowhere in the
    // codebase — the claim was unsupportable. Acuity does have one and was missing from the list.
    // Checked against MigrationWizardController.GetParser, which is the authority.
    a: 'Yes. Upkilo imports clients from CSV exports, with built-in support for Mindbody, Vagaro and Acuity formats. Our migration wizard walks you through upload, column mapping and duplicate review.',
  },
  {
    q: 'Is Upkilo compliant with data protection law?',
    a: "Yes. We meet GDPR and CCPA requirements, and also comply with India's DPDP Act 2023.",
  },
  {
    q: 'Does Upkilo work for multi-location businesses?',
    a: 'Yes. Growth covers up to 10 locations with white-label booking pages and cross-location analytics. Enterprise adds unlimited locations and agency sub-account management.',
  },
  {
    q: 'What currency is Upkilo billed in?',
    a: 'All Upkilo subscriptions are billed in USD, excluding applicable taxes. This is separate from what you charge your own clients — that settles through your connected Stripe account in your own currency.',
  },
  {
    q: 'What payment methods can my clients use?',
    a: 'Credit/debit cards and wallets via Stripe, in your own currency. In India, UPI and net banking are also supported via Razorpay. Gift cards and packages are available everywhere.',
  },
];


/**
 * Live pricing, fetched server-side so the plan cards are crawlable.
 *
 * The cards previously rendered the literal string "₹X,XXX" — placeholder copy shipped to
 * visitors, in a currency the platform does not price in: Upkilo bills exclusively in USD
 * (PricingIntegrityService.BillingCurrency). If the API is unreachable the cards fall back
 * to "Contact us" rather than inventing a number. Enterprise is IsCustom in PricingSeeder —
 * it has no price rows, so "Contact us" is the correct render for it, not a failure.
 */
interface PublishedPlan { name: string; monthlyPrice: number | null; currency: string | null; }

async function fetchPlans(): Promise<Record<string, string>> {
  const base = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  try {
    const res = await fetch(`${base}/api/v1/billing/plans`, { next: { revalidate: 3600 } });
    if (!res.ok) return {};
    const body = await res.json();
    const plans: PublishedPlan[] = Array.isArray(body) ? body : body?.data ?? [];
    const out: Record<string, string> = {};
    for (const plan of plans) {
      if (plan.monthlyPrice == null) continue;
      const code = plan.currency || 'USD';
      try {
        out[plan.name] = new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: code,
          maximumFractionDigits: 0,
        }).format(plan.monthlyPrice);
      } catch {
        out[plan.name] = `${code} ${plan.monthlyPrice}`;
      }
    }
    return out;
  } catch {
    return {};
  }
}

// ── Structured data ─────────────────────────────────────────────────────────
// Entity clarity — a consistent, machine-readable statement of what Upkilo is — is the
// most firmly established of the levers that make a site eligible to be cited by search
// engines and answer engines alike. Until now JSON-LD existed only on the two /book
// templates; the landing page, which is the page most likely to be the cited source for
// "what is Upkilo", carried none.
//
// The `name`, `url` and `logo` here must stay identical to every other place the entity is
// described (footer, OG tags, any future About page). Inconsistent facts about the same
// entity are what prevent it from being resolved as one thing.
const ORGANIZATION_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'Organization',
  '@id': `${SITE_URL}/#organization`,
  name: 'Upkilo',
  url: SITE_URL,
  // A real asset in public/icons/. /images/logo.png — referenced elsewhere in this app as a
  // fallback — does not exist on disk, and a logo URL that 404s is worse than omitting the
  // field, since it is what a Knowledge Panel would try to render.
  logo: `${SITE_URL}/icons/icon-512x512.png`,
  description:
    'AI-powered booking, CRM, payments and marketing software for appointment-led businesses — med spas, aesthetic and dental clinics, hair salons, spas, massage, physiotherapy, chiropractic and auto detailing.',
};

// SoftwareApplication rather than Product: this is software accessed over the web, and the
// type carries the fields that actually describe it (category, platform). No aggregateRating
// is declared — there are no genuine reviews yet, and inventing one would be both a policy
// violation and the same integrity problem as the testimonial removed in the previous commit.
const SOFTWARE_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'Upkilo',
  applicationCategory: 'BusinessApplication',
  applicationSubCategory: 'Appointment Scheduling Software',
  operatingSystem: 'Web',
  url: SITE_URL,
  publisher: { '@id': `${SITE_URL}/#organization` },
  description:
    'Booking, client CRM, payments, reminders and marketing automation for service businesses in one platform.',
  offers: {
    '@type': 'Offer',
    category: 'free trial',
    priceSpecification: {
      '@type': 'UnitPriceSpecification',
      price: '0',
      priceCurrency: 'USD',
      description: '14-day free trial, no credit card required',
    },
  },
};

// Built from the same FAQS array the visible accordion renders, so the two can never drift
// apart — a mismatch between rendered content and structured data is a policy violation.
//
// Google phased out the visual FAQ rich result during 2026, so this will not produce a
// SERP snippet. It is still read by Bing and by retrieval-style AI crawlers, and costs
// nothing beyond mapping an array that already exists.
const FAQ_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'FAQPage',
  mainEntity: FAQS.map(({ q, a }) => ({
    '@type': 'Question',
    name: q,
    acceptedAnswer: { '@type': 'Answer', text: a },
  })),
};

export default async function HomePage() {
  const livePrices = await fetchPlans();
  return (
    <div className="min-h-screen bg-white text-slate-900">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(ORGANIZATION_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(SOFTWARE_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(FAQ_JSON_LD) }} />
      <LandingNav />

      {/* ───────────────────────── HERO (dark gradient) ───────────────────────── */}
      <section className="relative overflow-hidden bg-slate-950 pt-32 pb-40 text-white">
        {/* gradient glows */}
        <div className="pointer-events-none absolute inset-0" aria-hidden="true">
          <div className="absolute left-1/2 top-0 h-[500px] w-[900px] -translate-x-1/2 rounded-full bg-primary-600/25 blur-[120px]" />
          <div className="absolute right-1/4 top-40 h-[300px] w-[300px] rounded-full bg-primary-500/20 blur-[100px]" />
          <div className="absolute left-1/4 top-60 h-[300px] w-[300px] rounded-full bg-blue-500/10 blur-[100px]" />
        </div>
        {/* grid texture */}
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.04]"
          style={{
            backgroundImage:
              'linear-gradient(to right, white 1px, transparent 1px), linear-gradient(to bottom, white 1px, transparent 1px)',
            backgroundSize: '56px 56px',
          }}
          aria-hidden="true"
        />

        <div className="relative mx-auto max-w-6xl px-4 text-center">
          <Reveal>
            <div className="mb-8 inline-flex items-center gap-2 rounded-full border border-primary-400/20 bg-primary-500/10 px-4 py-1.5 text-sm font-medium text-primary-200">
              <Sparkles className="h-3.5 w-3.5" aria-hidden="true" />
              <span>AI-powered booking, CRM &amp; payments in one place</span>
            </div>
          </Reveal>

          <Reveal delay={0.08}>
            <h1 className="mx-auto max-w-4xl text-5xl font-bold leading-[1.05] tracking-tight md:text-7xl">
              Grow your service business{' '}
              {/* Solid brand colour, not bg-clip-text over a three-stop gradient.
                  Gradient text is decorative rather than meaningful — it is one of the more
                  recognisable AI-generated-UI tells, and it costs legibility: the lightest
                  stop sets the effective contrast, so part of the phrase is always the
                  weakest-contrast text on the page. Emphasis here comes from the colour
                  break against white, which the surrounding words already carry at full
                  weight. */}
              <span className="text-primary-400">on autopilot</span>
            </h1>
          </Reveal>

          <Reveal delay={0.16}>
            <p className="mx-auto mt-6 max-w-2xl text-lg leading-relaxed text-slate-300 md:text-xl">
              AI-powered booking, CRM, payments, and marketing — built for salons, spas, and clinics.
              Start free, no credit card needed.
            </p>
          </Reveal>

          <Reveal delay={0.24}>
            <div className="mt-10 flex flex-col justify-center gap-4 sm:flex-row">
              <Link
                href="/register"
                className="group inline-flex items-center justify-center gap-2 rounded-xl bg-primary-600 px-8 py-4 text-lg font-semibold text-white shadow-xl shadow-primary-500/30 transition-all hover:-translate-y-0.5 hover:bg-primary-500 hover:shadow-primary-500/50"
              >
                Start 14-day free trial
                <ArrowRight className="h-5 w-5 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
              </Link>
              <a
                href="#how"
                className="inline-flex items-center justify-center rounded-xl border border-white/20 px-8 py-4 text-lg font-medium text-white transition-all hover:border-white/40 hover:bg-white/5"
              >
                See how it works
              </a>
            </div>
          </Reveal>

          <Reveal delay={0.32}>
            <p className="mt-6 text-sm text-slate-400">
              No credit card · Cancel anytime · Setup in under 10 minutes
            </p>
          </Reveal>

          {/* product preview mock */}
          <Reveal delay={0.4}>
            <div className="relative mx-auto mt-16 max-w-3xl">
              <div className="absolute -inset-4 rounded-3xl bg-gradient-to-r from-primary-600/20 to-blue-600/20 blur-2xl" aria-hidden="true" />
              <div className="relative animate-float rounded-2xl border border-white/10 bg-slate-900/80 p-5 text-left shadow-2xl backdrop-blur">
                {/* window bar */}
                <div className="mb-4 flex items-center gap-1.5" aria-hidden="true">
                  <span className="h-2.5 w-2.5 rounded-full bg-rose-400/70" />
                  <span className="h-2.5 w-2.5 rounded-full bg-amber-400/70" />
                  <span className="h-2.5 w-2.5 rounded-full bg-emerald-400/70" />
                  <span className="ml-3 text-xs text-slate-500">Today’s schedule · Glow Studio</span>
                </div>
                <div className="grid gap-3 sm:grid-cols-3">
                  {/* stat tiles */}
                  <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
                    <p className="text-xs text-slate-400">Bookings today</p>
                    <p className="mt-1 text-2xl font-bold text-white">28</p>
                    <p className="mt-1 text-xs text-emerald-400">▲ 12% vs last week</p>
                  </div>
                  <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
                    <p className="text-xs text-slate-400">Revenue</p>
                    <p className="mt-1 text-2xl font-bold text-white">$4,280</p>
                    <p className="mt-1 text-xs text-emerald-400">▲ 8% vs last week</p>
                  </div>
                  <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
                    <p className="text-xs text-slate-400">Utilization</p>
                    <p className="mt-1 text-2xl font-bold text-white">94%</p>
                    <p className="mt-1 text-xs text-primary-300">Waitlist auto-fills gaps</p>
                  </div>
                </div>
                {/* appointment rows */}
                <div className="mt-3 space-y-2">
                  {[
                    { t: '10:00', s: 'Hair Color · Priya', a: 'P', st: 'Confirmed', c: 'text-emerald-400' },
                    { t: '11:30', s: 'Deep Tissue · Rahul', a: 'R', st: 'Checked in', c: 'text-blue-400' },
                    { t: '13:00', s: 'Manicure · Aisha', a: 'A', st: 'Deposit paid', c: 'text-primary-300' },
                  ].map((row) => (
                    <div
                      key={row.t}
                      className="flex items-center gap-3 rounded-lg border border-white/5 bg-white/[0.02] px-3 py-2.5"
                    >
                      <span className="w-12 text-xs font-medium text-slate-400">{row.t}</span>
                      <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary-500/20 text-xs font-bold text-primary-200">
                        {row.a}
                      </span>
                      <span className="flex-1 truncate text-sm text-slate-200">{row.s}</span>
                      <span className={`text-xs font-medium ${row.c}`}>{row.st}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </Reveal>
        </div>

        {/* fade into light body */}
        <div
          className="pointer-events-none absolute inset-x-0 bottom-0 h-32 bg-gradient-to-b from-transparent to-white"
          aria-hidden="true"
        />
      </section>

      {/* ───────────────────────── TRUSTED BY / INTEGRATIONS ───────────────────────── */}
      <section className="border-b border-slate-100 bg-white py-14" aria-label="Integrations">
        <div className="mx-auto max-w-6xl px-4">
          <Reveal>
            <p className="text-center text-sm font-medium uppercase tracking-widest text-slate-400">
              Works with the tools you already use
            </p>
            <div className="mt-8 flex flex-wrap items-center justify-center gap-x-10 gap-y-5">
              {INTEGRATIONS.map((name) => (
                <span
                  key={name}
                  className="text-lg font-semibold text-slate-400 grayscale transition-all hover:text-slate-700"
                >
                  {name}
                </span>
              ))}
            </div>
          </Reveal>
        </div>
      </section>

      {/* ───────────────────────── GUARANTEES STRIP ───────────────────────── */}
      <section className="bg-slate-50 py-16">
        <div className="mx-auto max-w-6xl px-4">
          <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
            {GUARANTEES.map((g, i) => (
              <Reveal key={g.label} delay={i * 0.08}>
                <div className="flex flex-col items-center text-center">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white shadow-sm ring-1 ring-slate-200">
                    <g.icon className="h-6 w-6 text-primary-600" aria-hidden="true" />
                  </div>
                  <p className="mt-3 text-base font-bold text-slate-900">{g.label}</p>
                  <p className="mt-1 text-sm text-slate-500">{g.sub}</p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── FEATURES ───────────────────────── */}
      <section id="features" className="scroll-mt-20 bg-white py-24">
        <div className="mx-auto max-w-7xl px-4">
          <Reveal>
            <div className="mx-auto mb-16 max-w-2xl text-center">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-primary-50 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-primary-600">
                <Sparkles className="h-3.5 w-3.5" aria-hidden="true" /> Features
              </span>
              <h2 className="mt-4 text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
                Everything you need to scale
              </h2>
              <p className="mt-4 text-lg text-slate-600">
                One platform for bookings, clients, payments, and marketing. No more juggling five different tools.
              </p>
            </div>
          </Reveal>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map((feature, i) => (
              <Reveal key={feature.title} delay={(i % 3) * 0.08}>
                <div className="group h-full rounded-2xl border border-slate-200 bg-white p-7 transition-all hover:-translate-y-1 hover:border-primary-200 hover:shadow-xl hover:shadow-primary-500/5">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 shadow-lg shadow-primary-500/25 transition-transform group-hover:scale-110">
                    <feature.icon className="h-6 w-6 text-white" aria-hidden="true" />
                  </div>
                  <h3 className="mt-5 text-lg font-semibold text-slate-900">{feature.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-slate-600">{feature.description}</p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── HOW IT WORKS ───────────────────────── */}
      <section id="how" className="scroll-mt-20 bg-slate-50 py-24">
        <div className="mx-auto max-w-6xl px-4">
          <Reveal>
            <div className="mx-auto mb-16 max-w-2xl text-center">
              <span className="rounded-full bg-primary-50 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-primary-600">
                How it works
              </span>
              <h2 className="mt-4 text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
                Up and running in minutes
              </h2>
              <p className="mt-4 text-lg text-slate-600">
                From sign-up to your first booking — no technical skills required.
              </p>
            </div>
          </Reveal>

          <div className="grid gap-8 md:grid-cols-4">
            {STEPS.map((step, i) => (
              <Reveal key={step.title} delay={i * 0.1}>
                <div className="relative text-center">
                  {/* connector line (desktop) */}
                  {i < STEPS.length - 1 && (
                    <div
                      className="absolute left-[60%] top-8 hidden h-px w-[80%] bg-gradient-to-r from-primary-300 to-transparent md:block"
                      aria-hidden="true"
                    />
                  )}
                  <div className="relative mx-auto flex h-16 w-16 items-center justify-center rounded-2xl border border-primary-100 bg-white shadow-md">
                    <step.icon className="h-7 w-7 text-primary-600" aria-hidden="true" />
                    <span className="absolute -right-2 -top-2 flex h-6 w-6 items-center justify-center rounded-full bg-primary-600 text-xs font-bold text-white">
                      {i + 1}
                    </span>
                  </div>
                  <h3 className="mt-5 text-base font-semibold text-slate-900">{step.title}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-slate-600">{step.description}</p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── BUILT FOR ───────────────────────── */}
      <section id="industries" className="scroll-mt-20 bg-white py-24">
        <div className="mx-auto max-w-6xl px-4">
          <Reveal>
            <div className="mb-16 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
                Built for every service business
              </h2>
              <p className="mt-4 text-lg text-slate-600">Whatever you book, Upkilo adapts to how you work.</p>
            </div>
          </Reveal>

          <div className="grid grid-cols-2 gap-6 md:grid-cols-3">
            {INDUSTRIES.map((ind, i) => (
              <Reveal key={ind.label} delay={i * 0.08}>
                <div className="group flex h-full flex-col items-center rounded-2xl border border-slate-200 bg-white p-8 text-center transition-all hover:-translate-y-1 hover:border-primary-200 hover:shadow-xl hover:shadow-primary-500/5">
                  <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-primary-500 to-primary-600 shadow-lg shadow-primary-500/25 transition-transform group-hover:scale-110">
                    <ind.icon className="h-7 w-7 text-white" aria-hidden="true" />
                  </div>
                  <p className="mt-5 text-base font-semibold text-slate-900">{ind.label}</p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── SECURITY / TRUST ───────────────────────── */}
      <section className="bg-slate-50 py-20">
        <div className="mx-auto max-w-6xl px-4">
          <Reveal>
            <div className="mb-12 text-center">
              <h2 className="text-2xl font-bold tracking-tight text-slate-900 md:text-3xl">
                Security &amp; privacy, built in
              </h2>
              <p className="mt-3 text-slate-600">Your data — and your clients’ data — is protected by design.</p>
            </div>
          </Reveal>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {SECURITY.map((item, i) => (
              <Reveal key={item.label} delay={i * 0.08}>
                <div className="flex h-full flex-col items-center rounded-2xl border border-slate-200 bg-white p-6 text-center transition-all hover:border-emerald-200 hover:shadow-lg">
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-emerald-50">
                    <item.icon className="h-6 w-6 text-emerald-600" aria-hidden="true" />
                  </div>
                  <p className="mt-4 text-sm font-bold text-slate-900">{item.label}</p>
                  <p className="mt-1 text-xs text-slate-500">{item.sub}</p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── PRICING ───────────────────────── */}
      <section id="pricing" className="scroll-mt-20 bg-white py-24">
        <div className="mx-auto max-w-6xl px-4">
          <Reveal>
            <div className="mb-16 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
                Simple, transparent pricing
              </h2>
              <p className="mt-4 text-lg text-slate-600">Start free. Scale as you grow. No hidden fees.</p>
            </div>
          </Reveal>

          <div className="grid items-start gap-6 md:grid-cols-3">
            {PLANS.map((plan, i) => (
              <Reveal key={plan.name} delay={i * 0.1}>
                <div
                  className={`relative flex h-full flex-col rounded-2xl p-7 transition-all ${
                    plan.highlight
                      ? 'border-2 border-primary-500 bg-slate-950 text-white shadow-2xl shadow-primary-500/20 md:-translate-y-3'
                      : 'border border-slate-200 bg-white hover:-translate-y-1 hover:shadow-xl hover:shadow-primary-500/5'
                  }`}
                >
                  {plan.highlight && (
                    <span className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-primary-600 px-4 py-1 text-xs font-bold uppercase tracking-wide text-white shadow-lg">
                      Most Popular
                    </span>
                  )}
                  <h3 className={`text-xl font-bold ${plan.highlight ? 'text-white' : 'text-slate-900'}`}>
                    {plan.name}
                  </h3>
                  <p className={`mt-1 text-sm ${plan.highlight ? 'text-slate-300' : 'text-slate-500'}`}>
                    {plan.description}
                  </p>
                  <div className="mt-6 flex items-baseline gap-1">
                    <span className={`text-4xl font-bold ${plan.highlight ? 'text-white' : 'text-slate-900'}`}>
                      {livePrices[plan.planKey] ?? 'Contact us'}
                    </span>
                    <span className={`text-sm ${plan.highlight ? 'text-slate-400' : 'text-slate-500'}`}>
                      {livePrices[plan.planKey] ? '/mo' : ''}
                    </span>
                  </div>
                  <ul className="mt-8 flex-1 space-y-3" role="list">
                    {plan.features.map((f) => (
                      <li key={f} className="flex items-center gap-2.5 text-sm">
                        <CheckCircle2
                          className={`h-4 w-4 flex-shrink-0 ${plan.highlight ? 'text-primary-400' : 'text-primary-600'}`}
                          aria-hidden="true"
                        />
                        <span className={plan.highlight ? 'text-slate-200' : 'text-slate-700'}>{f}</span>
                      </li>
                    ))}
                  </ul>
                  <Link
                    href={plan.href}
                    className={`mt-8 rounded-xl py-3 text-center text-sm font-semibold transition-all ${
                      plan.highlight
                        ? 'bg-primary-600 text-white hover:bg-primary-500'
                        : 'bg-slate-900 text-white hover:bg-slate-800'
                    }`}
                  >
                    {plan.cta}
                  </Link>
                </div>
              </Reveal>
            ))}
          </div>
          <p className="mt-8 text-center text-sm text-slate-500">
            All prices in USD, excluding applicable taxes. 14-day free trial — no credit card required.
          </p>
        </div>
      </section>

      {/* ───────────────────────── FAQ ───────────────────────── */}
      <section id="faq" className="scroll-mt-20 bg-slate-50 py-24">
        <div className="mx-auto max-w-3xl px-4">
          <Reveal>
            <div className="mb-12 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
                Frequently asked questions
              </h2>
              <p className="mt-4 text-slate-600">Everything you need to know before getting started.</p>
            </div>
          </Reveal>
          <Reveal delay={0.1}>
            <FaqAccordion faqs={FAQS} />
          </Reveal>
        </div>
      </section>

      {/* ───────────────────────── CONTACT + FINAL CTA ───────────────────────── */}
      <section className="relative overflow-hidden bg-slate-950 py-24 text-white">
        <div className="pointer-events-none absolute inset-0" aria-hidden="true">
          <div className="absolute left-1/2 top-1/2 h-[400px] w-[700px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary-600/20 blur-[120px]" />
        </div>
        <div className="relative mx-auto max-w-3xl px-4 text-center">
          <Reveal>
            <h2 className="text-3xl font-bold tracking-tight md:text-4xl">Ready to fill your calendar?</h2>
            <p className="mx-auto mt-4 max-w-xl text-lg text-slate-300">
              Start free in minutes — no credit card, no contracts, cancel anytime.
            </p>
          </Reveal>
          <Reveal delay={0.1}>
            <div className="mt-10">
              <EmailCapture dark />
            </div>
          </Reveal>

          {/* contact options */}
          <Reveal delay={0.2}>
            <div className="mt-14 grid gap-4 sm:grid-cols-3">
              {[
                { icon: Mail, label: 'Email us', value: 'hello@upkilo.com', href: 'mailto:hello@upkilo.com' },
                { icon: MessageCircle, label: 'Live chat', value: 'Mon–Sat, 9am–9pm', href: '/register' },
                { icon: Phone, label: 'Talk to sales', value: 'Book a demo', href: '/contact' },
              ].map((c) => (
                <a
                  key={c.label}
                  href={c.href}
                  className="flex flex-col items-center rounded-2xl border border-white/10 bg-white/[0.03] p-6 transition-all hover:border-primary-400/30 hover:bg-white/[0.06]"
                >
                  <c.icon className="h-6 w-6 text-primary-300" aria-hidden="true" />
                  <p className="mt-3 text-sm font-semibold text-white">{c.label}</p>
                  <p className="mt-1 text-xs text-slate-400">{c.value}</p>
                </a>
              ))}
            </div>
          </Reveal>
        </div>
      </section>

      {/* ───────────────────────── FOOTER ───────────────────────── */}
      <footer className="bg-slate-950 px-4 pb-12 pt-4 text-slate-400" role="contentinfo">
        <div className="mx-auto max-w-7xl border-t border-white/10 pt-12">
          <div className="grid gap-10 md:grid-cols-5">
            <div className="md:col-span-2">
              <div className="flex items-center gap-2.5">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary-500 to-primary-600">
                  <span className="text-sm font-bold text-white">U</span>
                </div>
                <span className="text-lg font-bold text-white">Upkilo</span>
              </div>
              <p className="mt-4 max-w-xs text-sm leading-relaxed text-slate-400">
                The all-in-one platform that helps service businesses book more, retain more, and grow on autopilot.
              </p>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-white">Product</h3>
              <ul className="mt-4 space-y-3 text-sm">
                {/* Real pages, not #anchors. A fragment is stripped before the request is
                    sent, so /en#pricing is just /en to Google — linking here starved the
                    dedicated pages that are actually trying to rank. */}
                <li><Link href="/features" className="transition-colors hover:text-white">Features</Link></li>
                <li><Link href="/pricing" className="transition-colors hover:text-white">Pricing</Link></li>
                <li><Link href="/marketplace" className="transition-colors hover:text-white">Marketplace</Link></li>
                <li><Link href="/docs" className="transition-colors hover:text-white">Docs</Link></li>
              </ul>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-white">Company</h3>
              <ul className="mt-4 space-y-3 text-sm">
                <li><Link href="/contact" className="transition-colors hover:text-white">Contact</Link></li>
                <li><Link href="/login" className="transition-colors hover:text-white">Sign in</Link></li>
                <li><Link href="/register" className="transition-colors hover:text-white">Start free trial</Link></li>
              </ul>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-white">Legal</h3>
              <ul className="mt-4 space-y-3 text-sm">
                <li><Link href="/privacy-policy" className="transition-colors hover:text-white">Privacy</Link></li>
                <li><Link href="/terms-of-service" className="transition-colors hover:text-white">Terms</Link></li>
                <li><Link href="/cookie-policy" className="transition-colors hover:text-white">Cookie Policy</Link></li>
              </ul>
            </div>
          </div>

          {/* slate-400, not slate-500: #64748b on the slate-950 footer measures 4.23:1,
              just under the 4.5:1 WCAG AA minimum, and axe failed the landing page on it.
              slate-400 measures 7.87:1. */}
          <div className="mt-12 border-t border-white/10 pt-8 text-center text-xs text-slate-400">
            <p>© 2026 Upkilo Technologies Pvt. Ltd. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
