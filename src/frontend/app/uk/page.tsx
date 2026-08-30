import { Metadata } from 'next';
import Link from 'next/link';
import {
  PoundSterling,
  ShieldCheck,
  Smartphone,
  Landmark,
  FileText,
  Clock,
  Check,
} from 'lucide-react';

// Two corrections here, both visible to searchers rather than buried in the page:
//  - "The #1 booking and client management platform" was an unverifiable superlative sitting
//    in the meta description, which is the text shown in the search result itself.
//  - "Prices in GBP" contradicted the page's own plan cards, which were already corrected to
//    USD because Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency).
//    The snippet was advertising a currency the system cannot charge in.
export const metadata: Metadata = {
  title: 'Upkilo UK — Booking Software for UK Service Businesses',
  description: 'Booking, client records and payments for UK salons, spas and service businesses. GDPR compliant, with UK SMS sender IDs and VAT-ready invoicing.',
  alternates: { canonical: 'https://upkilo.com/uk' },
  openGraph: {
    title: 'Upkilo UK — Booking Software for UK Businesses',
    description: 'GDPR-compliant booking software built for the UK market.',
  },
};

// Icons are Lucide SVGs, not emoji. Emoji are font glyphs the platform substitutes: they render
// differently on every OS, are read aloud by screen readers as their CLDR name ("money with
// wings"), and cannot inherit colour or stroke weight — so they were never a controlled visual.
// The same substitution was already removed from the au/ca/uae hero sections for this reason.
const UK_FEATURES = [
  // Was "GBP Pricing / All plans billed in pounds" — Upkilo bills exclusively in USD, so this
  // advertised a currency the system cannot charge in. What IS true is that a UK tenant still
  // takes payment from their own clients in GBP through their own Stripe account.
  { icon: PoundSterling, title: 'Charge Your Clients in GBP', desc: 'Your bookings and invoices settle in pounds through your own Stripe account. Upkilo subscriptions are billed in USD.' },
  { icon: ShieldCheck, title: 'GDPR Compliant', desc: 'Full GDPR compliance including data residency, right to erasure, and DPA documentation.' },
  { icon: Smartphone, title: 'UK Phone Support', desc: 'SMS sent from UK numbers via Twilio. No international prefix issues.' },
  { icon: Landmark, title: 'Stripe UK', desc: 'Payments processed through Stripe UK with full VAT support.' },
  { icon: FileText, title: 'Companies House Ready', desc: 'Invoice templates with UK business number and VAT number fields.' },
  // "Dedicated UK support team available 9am–6pm GMT" claimed staffing that does not exist.
  { icon: Clock, title: 'UK Time Zone Support', desc: 'Support requests handled in UK business hours, with GMT/BST-aware scheduling throughout.' },
];

// USD — Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency), so
// quoting £ here advertised a currency the system cannot charge in.
const UK_PLANS = [
  { name: 'Starter', price: '$149', period: '/mo', desc: 'Small teams running bookings and clients in one place' },
  { name: 'Growth', price: '$499', period: '/mo', desc: 'Scaling businesses needing AI automation, white-label and API' },
  { name: 'Enterprise', price: 'Custom', period: '', desc: 'Multi-location chains and franchise groups' },
];

export default function UKLandingPage() {
  return (
    <main className="min-h-screen bg-card">
      {/* Hero */}
      <section className="bg-gradient-to-br from-blue-900 to-blue-700 text-white py-24 px-4">
        <div className="max-w-4xl mx-auto text-center">
          {/* Flag emoji removed: several platforms render a regional-indicator pair as the two
              letters "GB" rather than a flag, so it was never reliably the visual intended.
              Matches the same removal in app/uae/page.tsx. */}
          <div className="inline-flex items-center gap-2 bg-white/10 border border-white/20 rounded-full px-4 py-1.5 text-sm mb-6">
            Built for UK businesses
          </div>
          <h1 className="text-4xl md:text-5xl font-bold mb-4 leading-tight">
            The booking platform<br />UK service businesses love
          </h1>
          <p className="text-blue-100 text-lg max-w-2xl mx-auto mb-8">
            GDPR-compliant and built to handle everything from Mayfair salons to Manchester clinics.
            Join over 500 UK businesses already on Upkilo.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-4">
            <Link href="/register?locale=en-GB"
              className="bg-white text-blue-900 px-8 py-4 rounded-2xl font-bold text-lg hover:bg-blue-50 transition-colors">
              Start Free Trial →
            </Link>
            <Link href="#pricing" className="text-white border border-white/30 px-8 py-4 rounded-2xl font-semibold hover:bg-white/10 transition-colors">
              See Pricing
            </Link>
          </div>
        </div>
      </section>

      {/* Compliance badges */}
      <section className="bg-muted py-6 px-4 border-b">
        <div className="max-w-4xl mx-auto flex flex-wrap items-center justify-center gap-6">
          {['GDPR Compliant', 'UK Data Residency', 'ICO Registered', 'DPA Template Included', 'VAT Supported'].map(t => (
            <span key={t} className="inline-flex items-center gap-1.5 text-sm text-foreground-secondary font-medium">
              <Check className="h-4 w-4 shrink-0 text-success-fg" aria-hidden="true" strokeWidth={2.5} />
              {t}
            </span>
          ))}
        </div>
      </section>

      {/* UK-specific features */}
      <section className="max-w-5xl mx-auto py-20 px-4">
        <h2 className="text-2xl md:text-3xl font-bold text-foreground text-center mb-4">
          Everything UK businesses need
        </h2>
        <p className="text-foreground-secondary text-center mb-12">No generic "international" support — we're built for the UK market.</p>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {UK_FEATURES.map(f => (
            <div key={f.title} className="bg-card border border-border rounded-2xl p-6 shadow-sm">
              {/* aria-hidden: the heading beside it already names the feature, so announcing
                  the icon too would just repeat it. */}
              <f.icon className="h-7 w-7 mb-3 text-blue-700" aria-hidden="true" strokeWidth={1.75} />
              <h3 className="font-bold text-foreground mb-1">{f.title}</h3>
              <p className="text-sm text-foreground-secondary">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Pricing — USD, matching PricingSeeder.cs. The heading and strapline previously read
          "GBP Pricing" / "No USD conversion … in pounds" directly above $ figures. */}
      <section id="pricing" className="bg-muted py-20 px-4 border-t">
        <div className="max-w-4xl mx-auto">
          <h2 className="text-2xl md:text-3xl font-bold text-foreground text-center mb-4">Simple, transparent pricing</h2>
          <p className="text-foreground-secondary text-center mb-12">One price for every region — billed in USD. What you see is what you pay.</p>
          <div className="grid md:grid-cols-3 gap-6">
            {UK_PLANS.map((plan, i) => (
              <div key={plan.name} className={`rounded-2xl p-8 border ${i === 1 ? 'bg-blue-700 text-white border-blue-700 shadow-xl scale-105' : 'bg-card text-foreground border-border shadow-sm'}`}>
                <p className={`text-sm font-semibold mb-2 ${i === 1 ? 'text-blue-200' : 'text-foreground-secondary'}`}>{plan.name}</p>
                <div className="flex items-end gap-1 mb-1">
                  <span className="text-4xl font-bold">{plan.price}</span>
                  <span className={`text-sm mb-1 ${i === 1 ? 'text-blue-200' : 'text-foreground-muted'}`}>{plan.period}</span>
                </div>
                <p className={`text-sm mb-6 ${i === 1 ? 'text-blue-100' : 'text-foreground-secondary'}`}>{plan.desc}</p>
                <Link href={`/register?plan=${plan.name.toLowerCase()}&locale=en-GB`}
                  className={`block text-center py-2.5 rounded-xl font-semibold transition-colors ${i === 1 ? 'bg-white text-blue-700 hover:bg-blue-50' : 'bg-blue-700 text-white hover:bg-blue-600'}`}>
                  Start Free Trial
                </Link>
              </div>
            ))}
          </div>
          <p className="text-center text-sm text-foreground-muted mt-6">Prices in USD, excluding VAT. 14-day free trial. No credit card required.</p>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 px-4">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-foreground mb-4">Join UK businesses growing with Upkilo</h2>
          <p className="text-foreground-secondary mb-8">Start your 14-day free trial. No credit card. Cancel anytime.</p>
          <Link href="/register?locale=en-GB"
            className="inline-block bg-blue-700 text-white px-10 py-4 rounded-2xl font-bold text-lg hover:bg-blue-600 transition-colors">
            Get Started Free →
          </Link>
        </div>
      </section>
    </main>
  );
}
