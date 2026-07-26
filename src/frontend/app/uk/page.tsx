import { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Upkilo UK — Booking Software for UK Service Businesses',
  description: 'The #1 booking and client management platform for UK salons, spas, gyms, and service businesses. GDPR compliant. Prices in GBP.',
  alternates: { canonical: 'https://upkilo.com/uk' },
  openGraph: {
    title: 'Upkilo UK — Booking Software for UK Businesses',
    description: 'GDPR-compliant booking software built for the UK market. Prices in GBP.',
  },
};

const UK_FEATURES = [
  { icon: '💷', title: 'GBP Pricing', desc: 'All plans billed in pounds. No hidden currency conversion fees.' },
  { icon: '🔒', title: 'GDPR Compliant', desc: 'Full GDPR compliance including data residency, right to erasure, and DPA documentation.' },
  { icon: '📱', title: 'UK Phone Support', desc: 'SMS sent from UK numbers via Twilio. No international prefix issues.' },
  { icon: '🏦', title: 'Stripe UK', desc: 'Payments processed through Stripe UK with full VAT support.' },
  { icon: '📋', title: 'Companies House Ready', desc: 'Invoice templates with UK business number and VAT number fields.' },
  { icon: '🤝', title: 'UK Support Hours', desc: 'Dedicated UK support team available 9am–6pm GMT, Mon–Fri.' },
];

const UK_PLANS = [
  { name: 'Starter', price: '£29', period: '/mo', desc: 'Perfect for solo therapists and small salons' },
  { name: 'Pro', price: '£69', period: '/mo', desc: 'For growing businesses with a team' },
  { name: 'Business', price: '£159', period: '/mo', desc: 'Multi-location chains and franchise groups' },
];

export default function UKLandingPage() {
  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-blue-900 to-blue-700 text-white py-24 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <div className="inline-flex items-center gap-2 bg-white/10 border border-white/20 rounded-full px-4 py-1.5 text-sm mb-6">
            🇬🇧 Built for UK businesses
          </div>
          <h1 className="text-4xl md:text-5xl font-bold mb-4 leading-tight">
            The booking platform<br />UK service businesses love
          </h1>
          <p className="text-blue-100 text-lg max-w-2xl mx-auto mb-8">
            GDPR-compliant, GBP-priced, and built to handle everything from Mayfair salons to Manchester gyms.
            Join over 500 UK businesses already on Upkilo.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-4">
            <Link href="/register?locale=en-GB&currency=GBP"
              className="bg-white text-blue-900 px-8 py-4 rounded-2xl font-bold text-lg hover:bg-blue-50 transition-colors">
              Start Free Trial →
            </Link>
            <Link href="#pricing" className="text-white border border-white/30 px-8 py-4 rounded-2xl font-semibold hover:bg-white/10 transition-colors">
              See GBP Pricing
            </Link>
          </div>
        </div>
      </section>

      {/* Compliance badges */}
      <section className="bg-gray-50 py-6 px-4 border-b">
        <div className="max-w-4xl mx-auto flex flex-wrap items-center justify-center gap-6">
          {['🔒 GDPR Compliant', '🇬🇧 UK Data Residency', '✅ ICO Registered', '📋 DPA Template Included', '💳 VAT Supported'].map(t => (
            <span key={t} className="text-sm text-gray-600 font-medium">{t}</span>
          ))}
        </div>
      </section>

      {/* UK-specific features */}
      <section className="max-w-5xl mx-auto py-20 px-4">
        <h2 className="text-2xl md:text-3xl font-bold text-gray-900 text-center mb-4">
          Everything UK businesses need
        </h2>
        <p className="text-gray-500 text-center mb-12">No generic "international" support — we're built for the UK market.</p>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {UK_FEATURES.map(f => (
            <div key={f.title} className="bg-white border border-gray-200 rounded-2xl p-6 shadow-sm">
              <div className="text-3xl mb-3">{f.icon}</div>
              <h3 className="font-bold text-gray-900 mb-1">{f.title}</h3>
              <p className="text-sm text-gray-600">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* GBP Pricing */}
      <section id="pricing" className="bg-gray-50 py-20 px-4 border-t">
        <div className="max-w-4xl mx-auto">
          <h2 className="text-2xl md:text-3xl font-bold text-gray-900 text-center mb-4">GBP Pricing</h2>
          <p className="text-gray-500 text-center mb-12">No USD conversion. What you see is what you pay — in pounds.</p>
          <div className="grid md:grid-cols-3 gap-6">
            {UK_PLANS.map((plan, i) => (
              <div key={plan.name} className={`rounded-2xl p-8 border ${i === 1 ? 'bg-blue-700 text-white border-blue-700 shadow-xl scale-105' : 'bg-white text-gray-900 border-gray-200 shadow-sm'}`}>
                <p className={`text-sm font-semibold mb-2 ${i === 1 ? 'text-blue-200' : 'text-gray-500'}`}>{plan.name}</p>
                <div className="flex items-end gap-1 mb-1">
                  <span className="text-4xl font-bold">{plan.price}</span>
                  <span className={`text-sm mb-1 ${i === 1 ? 'text-blue-200' : 'text-gray-400'}`}>{plan.period}</span>
                </div>
                <p className={`text-sm mb-6 ${i === 1 ? 'text-blue-100' : 'text-gray-500'}`}>{plan.desc}</p>
                <Link href={`/register?plan=${plan.name.toLowerCase()}&locale=en-GB&currency=GBP`}
                  className={`block text-center py-2.5 rounded-xl font-semibold transition-colors ${i === 1 ? 'bg-white text-blue-700 hover:bg-blue-50' : 'bg-blue-700 text-white hover:bg-blue-600'}`}>
                  Start Free Trial
                </Link>
              </div>
            ))}
          </div>
          <p className="text-center text-sm text-gray-400 mt-6">All prices exclude VAT. 14-day free trial. No credit card required.</p>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 px-4">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Join UK businesses growing with Upkilo</h2>
          <p className="text-gray-500 mb-8">Start your 14-day free trial. No credit card. Cancel anytime.</p>
          <Link href="/register?locale=en-GB&currency=GBP"
            className="inline-block bg-blue-700 text-white px-10 py-4 rounded-2xl font-bold text-lg hover:bg-blue-600 transition-colors">
            Get Started Free →
          </Link>
        </div>
      </section>
    </main>
  );
}
