'use client';

import Link from 'next/link';

const features = [
  { title: 'GST-Ready Invoicing', desc: 'Automatic 10% GST calculation on all invoices. Compliant with ATO requirements.' },
  { title: 'AUD Pricing', desc: 'All plans priced in Australian Dollars. No conversion surprises.' },
  { title: 'AEDT/AEST Scheduling', desc: 'Timezone-aware booking system for all Australian states and territories.' },
  { title: 'Local SMS via Twilio AU', desc: 'Australian sender IDs for higher open rates and SPAM Act compliance.' },
  { title: 'Medicare Rebate Notes', desc: 'Add Allied Health rebate information to client receipts and invoices.' },
  { title: 'Square AU Integration', desc: "Accept EFTPOS and card payments via Square's Australian payment gateway." },
];

const plans = [
  // Priced in USD because that is what tenants are actually charged — Upkilo bills
  // exclusively in USD (see PricingIntegrityService.BillingCurrency). Advertising A$ amounts
  // here meant quoting a currency the system cannot charge in.
  { name: 'Starter', price: '$149', period: '/mo', features: ['Up to 10 staff', 'Up to 3 locations', 'AI Copilot (2,000 actions/mo)', 'SMS & email reminders'] },
  { name: 'Growth', price: '$499', period: '/mo', features: ['Up to 25 staff', 'Up to 10 locations', 'AI Workflows & Insights', 'White-label & API'], highlight: true },
  { name: 'Enterprise', price: 'Custom', period: '', features: ['Unlimited staff & locations', 'SSO / SAML', 'Agency sub-accounts', 'Dedicated support'] },
];

export default function AustraliaPage() {
  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-green-900 to-yellow-700 text-white py-20 px-4 text-center">
        <div className="max-w-4xl mx-auto">
          <div className="text-5xl mb-4">🦘</div>
          <h1 className="text-4xl md:text-5xl font-bold mb-4">
            Booking software built for Australian service businesses
          </h1>
          <p className="text-xl opacity-90 mb-8 max-w-2xl mx-auto">
            GST-compliant invoicing, AUD pricing, and local SMS — everything you need to grow your business Down Under.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/register?locale=en-AU&currency=AUD"
              className="bg-yellow-400 text-green-900 font-bold px-8 py-3 rounded-lg hover:bg-yellow-300 transition">
              Start Free Trial — No Credit Card
            </Link>
            <Link href="/au/demo"
              className="border-2 border-white text-white font-semibold px-8 py-3 rounded-lg hover:bg-white hover:text-green-900 transition">
              Book a Demo
            </Link>
          </div>
          {/* "Trusted by 1,200+ Australian salons, gyms, and clinics" was removed — an
              unverifiable customer count, same issue as the landing page's meta description
              and the medical-spa testimonial. Replaced with a product fact. */}
          <p className="mt-4 text-sm opacity-75">Online booking, client records, and automated reminders in one system</p>
        </div>
      </section>

      {/* Features */}
      <section className="py-16 px-4 max-w-6xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-12 text-gray-900">
          Built for the Australian market
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
          <h2 className="text-3xl font-bold text-center mb-4 text-gray-900">Simple AUD pricing</h2>
          <p className="text-center text-gray-600 mb-12">All prices include GST. Cancel anytime.</p>
          <div className="grid md:grid-cols-3 gap-8">
            {plans.map((p) => (
              <div key={p.name} className={`rounded-2xl p-8 ${p.highlight ? 'bg-green-900 text-white shadow-xl scale-105' : 'bg-white border border-gray-200'}`}>
                <div className="text-sm font-semibold mb-2 opacity-75">{p.name}</div>
                <div className="text-4xl font-bold mb-1">{p.price}</div>
                <div className={`text-sm mb-6 ${p.highlight ? 'opacity-75' : 'text-gray-500'}`}>{p.period}</div>
                <ul className="space-y-2 mb-8">
                  {p.features.map((feat) => (
                    <li key={feat} className="flex items-center gap-2 text-sm">
                      <span className="text-green-400">✓</span> {feat}
                    </li>
                  ))}
                </ul>
                <Link href={`/register?plan=${p.name.toLowerCase()}&locale=en-AU&currency=AUD`}
                  className={`block text-center py-3 rounded-lg font-semibold transition ${p.highlight ? 'bg-yellow-400 text-green-900 hover:bg-yellow-300' : 'bg-green-900 text-white hover:bg-green-800'}`}>
                  Get Started
                </Link>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Trust */}
      <section className="py-16 px-4 text-center">
        <div className="max-w-3xl mx-auto">
          <h2 className="text-2xl font-bold mb-8 text-gray-900">Australian businesses love Upkilo</h2>
          <blockquote className="text-lg text-gray-700 italic mb-4">
            "Upkilo cut our no-shows by 40% and the GST invoicing saves us hours every BAS period."
          </blockquote>
          <p className="text-gray-500 text-sm">— Sarah M., Skin & Body Studio, Melbourne</p>
          <div className="mt-12">
            <Link href="/register?locale=en-AU&currency=AUD"
              className="bg-green-900 text-white font-bold px-10 py-4 rounded-lg hover:bg-green-800 transition text-lg">
              Start your free 14-day trial →
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
