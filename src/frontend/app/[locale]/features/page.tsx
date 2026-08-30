import type { Metadata } from "next";
import Link from "next/link";
import {
  Calendar, Users, Zap, CreditCard, MessageSquare, BarChart3,
  Bot, ShieldCheck, Plug, Bell, Globe, Search, Smartphone, Clock,
  ArrowRight, Check,
} from "lucide-react";
import { safeJsonLd } from "@/lib/jsonLd";
import { breadcrumbJsonLd, HOME_CRUMB } from "@/lib/seo";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://upkilo.com";

// A dedicated, indexable page — unlike the homepage #features anchor, which Google
// collapses into the homepage URL. Fragments are stripped before the request is sent,
// so /en#features can never rank as its own result.
export const metadata: Metadata = {
  title: "Features — Bookings, CRM, Payments & AI Automation | Upkilo",
  description:
    "Everything Upkilo does: smart scheduling, client CRM, Stripe payments, marketing automation, AI receptionist and chatbot, analytics, and enterprise-grade security — in one platform.",
  alternates: { canonical: `${SITE_URL}/en/features` },
  openGraph: {
    title: "Features — Bookings, CRM, Payments & AI Automation | Upkilo",
    description:
      "Smart scheduling, client CRM, payments, marketing automation and AI agents for service businesses. One platform instead of five tools.",
    url: `${SITE_URL}/en/features`,
    siteName: "Upkilo",
    type: "website",
  },
};

const CORE = [
  {
    icon: Calendar,
    title: "Smart Booking",
    description:
      "AI-powered scheduling adapts to your business rules, staff availability, and client preferences — automatically filling gaps.",
    points: ["Waitlists & automatic gap-filling", "Packages and memberships", "Multi-location support"],
  },
  {
    icon: Users,
    title: "CRM & Client Hub",
    description:
      "Full client history, preferences, notes, loyalty points, and purchases in one place. Know your client before they walk in.",
    points: ["Client records & photo history", "Segments and duplicate merging", "Consent forms and waivers"],
  },
  {
    icon: CreditCard,
    title: "Payments & Billing",
    description:
      "Accept cards, subscriptions, packages, and gift cards. Send invoices, collect deposits, and track revenue in real time.",
    points: ["Stripe subscriptions & payouts", "Usage-based invoicing", "Automated dunning & retries"],
  },
  {
    icon: Zap,
    title: "Marketing Automation",
    description:
      "Drip campaigns, SMS sequences, and email workflows that run automatically. Re-engage lapsed clients and fill empty slots.",
    points: ["Email & SMS campaigns", "Automated reminders", "Referrals, coupons and loyalty"],
  },
  {
    icon: MessageSquare,
    title: "AI Chatbot & Receptionist",
    description:
      "A 24/7 booking assistant that handles new inquiries, rescheduling, and FAQs — without ever involving your staff.",
    points: ["Voice agent for phone bookings", "Chatbot on your booking page", "Churn-retention outreach"],
  },
  {
    icon: BarChart3,
    title: "Analytics & Insights",
    description:
      "Revenue funnels, staff performance, campaign ROI, and retention metrics — in a dashboard built for growth decisions.",
    points: ["Revenue and retention reporting", "Staff performance tracking", "Campaign ROI attribution"],
  },
];

const PLATFORM = [
  { icon: Bot, title: "AI automation", description: "AI receptionist, voice agent, dynamic pricing and churn-retention agents, powered by Azure OpenAI." },
  { icon: Plug, title: "Integrations", description: "A 14-provider catalogue using your own API keys, encrypted with AES-256-GCM. Bring your own keys, no platform markup." },
  { icon: ShieldCheck, title: "Enterprise security", description: "WebAuthn, TOTP multi-factor auth, per-tenant SAML SSO, tenant-scoped rate limiting and audit logs." },
  { icon: Bell, title: "Realtime updates", description: "Live booking and notification updates across every open device, with no refresh required." },
  { icon: Clock, title: "Background automation", description: "Reminders, billing reconciliation, dunning and daily digests run on schedule without you touching them." },
  { icon: Smartphone, title: "Mobile & offline", description: "Installable progressive web app with offline support and push notifications, plus native iOS and Android apps." },
  { icon: Globe, title: "Multi-language", description: "Locale-aware routing and translated interfaces so your team works in the language they prefer." },
  { icon: Search, title: "Fast search", description: "Full-text search across clients, bookings and services that stays quick as your database grows." },
];

// SoftwareApplication with an explicit featureList. "What does Upkilo do" is the question this
// page exists to answer, and a prose grid of cards only answers it to a reader — featureList
// states the same capabilities in a form an engine can quote without inferring them from markup.
//
// Built from the CORE and PLATFORM arrays the page renders, so the declared capabilities cannot
// drift from the visible ones. The @id ties this to the single Organization node on the
// homepage rather than describing a second, unrelated "Upkilo".
const FEATURES_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'Upkilo',
  applicationCategory: 'BusinessApplication',
  operatingSystem: 'Web, iOS, Android',
  url: `${SITE_URL}/en/features`,
  publisher: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  featureList: [...CORE, ...PLATFORM].map((f) => f.title),
  description:
    'Booking, client CRM, payments, marketing automation, AI agents and analytics for service businesses.',
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([HOME_CRUMB, { name: 'Features', path: '/en/features' }]);

export default async function FeaturesPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;

  return (
    <main className="bg-card">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(FEATURES_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
      {/* ───────────────────────── HERO ───────────────────────── */}
      <section className="border-b border-border bg-gradient-to-b from-primary-50 to-white py-20">
        <div className="mx-auto max-w-4xl px-4 text-center">
          <span className="inline-flex items-center gap-1.5 rounded-full bg-brand-subtle px-3 py-1 text-xs font-semibold uppercase tracking-wide text-primary">
            Features
          </span>
          <h1 className="mt-5 text-4xl font-bold tracking-tight text-foreground md:text-5xl">
            Everything you need to run and grow your business
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-lg leading-relaxed text-foreground-secondary">
            Bookings, clients, payments, marketing and AI automation in one platform —
            so you can stop paying for five tools that don&apos;t talk to each other.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link
              href={`/${locale}/register`}
              className="inline-flex items-center gap-2 rounded-2xl bg-primary-600 px-7 py-3.5 font-semibold text-white shadow-lg shadow-primary-500/25 transition-colors hover:bg-primary-700"
            >
              Start free trial <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
            <Link
              href={`/${locale}/pricing`}
              className="rounded-2xl border border-border-strong px-7 py-3.5 font-semibold text-foreground transition-colors hover:bg-accent"
            >
              See pricing
            </Link>
          </div>
          <p className="mt-4 text-sm text-foreground-secondary">14-day free trial · No credit card required</p>
        </div>
      </section>

      {/* ───────────────────────── CORE FEATURES ───────────────────────── */}
      <section className="py-24">
        <div className="mx-auto max-w-7xl px-4">
          <div className="mx-auto mb-16 max-w-2xl text-center">
            <h2 className="text-3xl font-bold tracking-tight text-foreground md:text-4xl">
              Built for service businesses
            </h2>
            <p className="mt-4 text-lg text-foreground-secondary">
              Six core systems that cover the whole customer journey, from first enquiry to repeat booking.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {CORE.map((feature) => (
              <div
                key={feature.title}
                className="group h-full rounded-2xl border border-border bg-card p-7 transition-all hover:-translate-y-1 hover:border-primary/25 hover:shadow-xl hover:shadow-primary-500/5"
              >
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 shadow-lg shadow-primary-500/25 transition-transform group-hover:scale-110">
                  <feature.icon className="h-6 w-6 text-white" aria-hidden="true" />
                </div>
                <h3 className="mt-5 text-lg font-semibold text-foreground">{feature.title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-foreground-secondary">{feature.description}</p>
                <ul className="mt-4 space-y-2">
                  {feature.points.map((point) => (
                    <li key={point} className="flex items-start gap-2 text-sm text-foreground-secondary">
                      <Check className="mt-0.5 h-4 w-4 shrink-0 text-primary" aria-hidden="true" />
                      <span>{point}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── PLATFORM ───────────────────────── */}
      <section className="border-y border-border bg-muted py-24">
        <div className="mx-auto max-w-7xl px-4">
          <div className="mx-auto mb-16 max-w-2xl text-center">
            <h2 className="text-3xl font-bold tracking-tight text-foreground md:text-4xl">
              The platform underneath
            </h2>
            <p className="mt-4 text-lg text-foreground-secondary">
              The parts you shouldn&apos;t have to think about — because they simply work.
            </p>
          </div>

          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {PLATFORM.map((item) => (
              <div key={item.title} className="rounded-2xl border border-border bg-card p-6">
                <item.icon className="h-6 w-6 text-primary" aria-hidden="true" />
                <h3 className="mt-4 font-semibold text-foreground">{item.title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-foreground-secondary">{item.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ───────────────────────── CTA ───────────────────────── */}
      <section className="py-24">
        <div className="mx-auto max-w-3xl px-4 text-center">
          <h2 className="text-3xl font-bold tracking-tight text-foreground md:text-4xl">
            Ready to see it in action?
          </h2>
          <p className="mt-4 text-lg text-foreground-secondary">
            Start a 14-day free trial. No credit card, no setup fees, no contracts.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link
              href={`/${locale}/register`}
              className="inline-flex items-center gap-2 rounded-2xl bg-primary-600 px-7 py-3.5 font-semibold text-white shadow-lg shadow-primary-500/25 transition-colors hover:bg-primary-700"
            >
              Start free trial <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
            <Link
              href={`/${locale}/pricing`}
              className="rounded-2xl border border-border-strong px-7 py-3.5 font-semibold text-foreground transition-colors hover:bg-accent"
            >
              Compare plans
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
