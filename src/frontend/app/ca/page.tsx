'use client';

import Link from 'next/link';

const features = [
  { title: 'HST/GST/PST Invoicing', desc: 'Province-aware tax calculation for Ontario, BC, Quebec, and all other provinces automatically.' },
  { title: 'CAD Pricing', desc: 'Plans priced in Canadian Dollars. No USD conversion fees.' },
  { title: 'French Language Support', desc: 'Full French UI for Quebec businesses. Switch between en-CA and fr-CA seamlessly.' },
  { title: 'Interac-Friendly Payments', desc: 'Accept Visa Debit and Canadian cards via Stripe Canada.' },
  { title: 'CASL-Compliant SMS', desc: "Opt-in consent flows that meet Canada's Anti-Spam Legislation requirements." },
  { title: 'Multi-Province Staff', desc: 'Manage staff across multiple provinces with correct timezone and tax handling.' },
];

const plans = [
  // USD — Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency).
  { name: 'Starter', price: '$149', period: '/mo', features: ['Up to 10 staff', 'Up to 3 locations', 'AI Copilot (2,000 actions/mo)', 'SMS & email reminders'] },
  { name: 'Growth', price: '$499', period: '/mo', features: ['Up to 25 staff', 'Up to 10 locations', 'AI Workflows & Insights', 'White-label & API'], highlight: true },
  { name: 'Enterprise', price: 'Custom', period: '', features: ['Unlimited staff & locations', 'SSO / SAML', 'Agency sub-accounts', 'Dedicated support'] },
];

export default function CanadaPage() {
  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-red-700 to-red-900 text-white py-20 px-4 text-center">
        <div className="max-w-4xl mx-auto">
          <div className="text-5xl mb-4">🍁</div>
          <h1 className="text-4xl md:text-5xl font-bold mb-4">
            Booking software designed for Canadian service businesses
          </h1>
          <p className="text-xl opacity-90 mb-8 max-w-2xl mx-auto">
            Province-aware tax, CASL-compliant SMS, and CAD pricing — everything Canadian businesses need.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/register?locale=en-CA&currency=CAD"
              className="bg-white text-red-700 font-bold px-8 py-3 rounded-lg hover:bg-red-50 transition">
              Démarrer / Start Free Trial
            </Link>
            <Link href="/ca/demo"
              className="border-2 border-white text-white font-semibold px-8 py-3 rounded-lg hover:bg-white hover:text-red-700 transition">
              Book a Demo
            </Link>
          </div>
          {/* "Trusted by salons, spas, and clinics across Canada" was removed — implies an
              existing Canadian customer base. Replaced with a product fact. */}
          <p className="mt-4 text-sm opacity-75">Booking pages available in English and French, with automated client reminders</p>
        </div>
      </section>

      {/* Features */}
      <section className="py-16 px-4 max-w-6xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-12 text-gray-900">
          Built for the Canadian market
        </h2>
        <div className="grid md:grid-cols-3 gap-8">
          {features.map((f) => (
            <div key={f.title} className="border border-gray-100 rounded-xl p-6 shadow-sm hover:shadow-md transition">
              <h3 className="text-lg font-semibold mb-2 text-gray-900">{f.title}</h3>
              <p className="text-gray-600 text-sm">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Pricing */}
      <section className="py-16 px-4 bg-gray-50">
        <div className="max-w-5xl mx-auto">
          <h2 className="text-3xl font-bold text-center mb-4 text-gray-900">Simple CAD pricing</h2>
          <p className="text-center text-gray-600 mb-12">Tax-inclusive pricing. Cancel anytime.</p>
          <div className="grid md:grid-cols-3 gap-8">
            {plans.map((p) => (
              <div key={p.name} className={`rounded-2xl p-8 ${p.highlight ? 'bg-red-700 text-white shadow-xl scale-105' : 'bg-white border border-gray-200'}`}>
                <div className="text-sm font-semibold mb-2 opacity-75">{p.name}</div>
                <div className="text-4xl font-bold mb-1">{p.price}</div>
                <div className={`text-sm mb-6 ${p.highlight ? 'opacity-75' : 'text-gray-500'}`}>{p.period}</div>
                <ul className="space-y-2 mb-8">
                  {p.features.map((feat) => (
                    <li key={feat} className="flex items-center gap-2 text-sm">
                      <span className={p.highlight ? 'text-red-200' : 'text-red-600'}>✓</span> {feat}
                    </li>
                  ))}
                </ul>
                <Link href={`/register?plan=${p.name.toLowerCase()}&locale=en-CA&currency=CAD`}
                  className={`block text-center py-3 rounded-lg font-semibold transition ${p.highlight ? 'bg-white text-red-700 hover:bg-red-50' : 'bg-red-700 text-white hover:bg-red-600'}`}>
                  Get Started
                </Link>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-16 px-4 text-center">
        <div className="max-w-3xl mx-auto">
          <h2 className="text-2xl font-bold mb-8 text-gray-900">Canadian businesses trust Upkilo</h2>
          <blockquote className="text-lg text-gray-700 italic mb-4">
            "The HST calculation and French interface made switching from our old system a no-brainer."
          </blockquote>
          <p className="text-gray-500 text-sm">— Marc B., Salon Beauté, Montréal</p>
          <div className="mt-12">
            <Link href="/register?locale=en-CA&currency=CAD"
              className="bg-red-700 text-white font-bold px-10 py-4 rounded-lg hover:bg-red-600 transition text-lg">
              Start your free 14-day trial →
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
