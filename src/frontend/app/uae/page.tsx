// Not a Client Component — see the note in app/au/page.tsx. No state, no effects, no
// handlers; the directive only blocked metadata export, leaving this page with no <title>.
import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Upkilo UAE — Booking Software for UAE Service Businesses',
  description: 'Booking, client records and payments for UAE salons, spas and clinics. Arabic RTL interface, 5% VAT-compliant invoicing and WhatsApp reminders.',
  alternates: { canonical: 'https://upkilo.com/uae' },
  openGraph: {
    title: 'Upkilo UAE — Booking Software for UAE Businesses',
    description: 'Arabic-ready, VAT-compliant booking and client management built for the UAE market.',
    type: 'website',
  },
};

const features = [
  { title: 'Arabic RTL Interface', desc: 'Full right-to-left UI in Arabic. Staff and clients can switch languages seamlessly.' },
  // "AED Pricing / Plans in UAE Dirhams" contradicted the plan cards below, which are USD —
  // Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency).
  { title: 'Transparent Pricing', desc: 'Flat monthly plans with no per-booking commission and no setup fee.' },
  { title: 'VAT Invoicing (5%)', desc: 'UAE VAT-compliant invoices and receipts. Export for FTA filing.' },
  { title: 'WhatsApp-First Reminders', desc: 'Send booking reminders and confirmations via WhatsApp — the #1 platform in UAE.' },
  { title: 'UAE Public Holiday Aware', desc: 'Scheduling engine respects UAE public holidays and Islamic calendar events.' },
  { title: 'DED License Display', desc: 'Show your Dubai trade license number on client receipts and booking pages.' },
];

// USD — Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency), so
// quoting AED advertised a currency the system cannot charge in.
const plans = [
  { name: 'Starter', price: '$149', period: '/شهر', features: ['حتى 10 موظفين', 'حتى 3 فروع', 'AI Copilot (2,000 إجراء/شهر)', 'تذكيرات SMS والبريد'] },
  { name: 'نمو', price: '$499', period: '/شهر', features: ['حتى 25 موظفاً', 'حتى 10 فروع', 'AI Workflows والتحليلات', 'العلامة البيضاء وواجهة API'], highlight: true },
  { name: 'Enterprise', price: 'Custom', period: '', features: ['موظفون وفروع غير محدودة', 'SSO / SAML', 'حسابات فرعية للوكالات', 'دعم مخصص'] },
];

export default function UAEPage() {
  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-green-800 to-red-900 text-white py-20 px-4 text-center">
        <div className="max-w-4xl mx-auto">
          {/* Emoji removed as hero decoration — see the equivalent note in app/au/page.tsx.
              Flag emoji are the worst case of it: several platforms render them as two-letter
              country codes rather than a flag at all. */}
          <h1 className="text-4xl md:text-5xl font-bold mb-4">
            نظام الحجز المثالي للأعمال الخدمية في الإمارات
          </h1>
          <p className="text-xl opacity-90 mb-4">
            Booking software built for UAE service businesses
          </p>
          <p className="text-lg opacity-80 mb-8 max-w-2xl mx-auto">
            واجهة عربية كاملة، فواتير ضريبة القيمة المضافة، وتذكيرات WhatsApp — كل ما تحتاجه لتنمية عملك.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/register?locale=ar-AE"
              className="bg-yellow-400 text-green-900 font-bold px-8 py-3 rounded-lg hover:bg-yellow-300 transition">
              ابدأ مجاناً / Start Free
            </Link>
            <Link href="/uae/demo"
              className="border-2 border-white text-white font-semibold px-8 py-3 rounded-lg hover:bg-white hover:text-green-900 transition">
              احجز عرضاً توضيحياً
            </Link>
          </div>
          {/* "وصالة رياضية" (and gyms) removed — Upkilo no longer serves that vertical. */}
          <p className="mt-4 text-sm opacity-75">موثوق به من قِبل أكثر من 500 صالون وعيادة في الإمارات</p>
        </div>
      </section>

      {/* Features */}
      <section className="py-16 px-4 max-w-6xl mx-auto">
        <h2 className="text-3xl font-bold text-center mb-4 text-gray-900">
          مصمم للسوق الإماراتي
        </h2>
        <p className="text-center text-gray-600 mb-12">Built specifically for UAE market requirements</p>
        <div className="grid md:grid-cols-3 gap-8">
          {features.map((f) => (
            <div key={f.title} className="border border-gray-100 rounded-xl p-6 shadow-sm hover:shadow-md transition">
              <h3 className="text-lg font-semibold mb-2 text-gray-900">{f.title}</h3>
              <p className="text-gray-600 text-sm">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Pricing — RTL-friendly labels */}
      <section className="py-16 px-4 bg-gray-50">
        <div className="max-w-5xl mx-auto">
          {/* Heading read "أسعار بالدرهم الإماراتي" (prices in UAE Dirhams) and claimed
              VAT-inclusive pricing, directly above USD plan cards. Upkilo bills exclusively in
              USD, exclusive of tax. */}
          <h2 className="text-3xl font-bold text-center mb-4 text-gray-900">أسعار بسيطة وشفافة</h2>
          <p className="text-center text-gray-600 mb-12">الأسعار بالدولار الأمريكي، غير شاملة ضريبة القيمة المضافة. إلغاء في أي وقت.</p>
          <div className="grid md:grid-cols-3 gap-8">
            {plans.map((p) => (
              <div key={p.name} className={`rounded-2xl p-8 ${p.highlight ? 'bg-green-800 text-white shadow-xl scale-105' : 'bg-white border border-gray-200'}`}>
                <div className="text-sm font-semibold mb-2 opacity-75">{p.name}</div>
                <div className="text-3xl font-bold mb-1">{p.price}</div>
                <div className={`text-sm mb-6 ${p.highlight ? 'opacity-75' : 'text-gray-500'}`}>{p.period}</div>
                <ul className="space-y-2 mb-8">
                  {p.features.map((feat) => (
                    <li key={feat} className="flex items-center gap-2 text-sm">
                      <span className="text-yellow-400">✓</span> {feat}
                    </li>
                  ))}
                </ul>
                <Link href={`/register?plan=${p.name.toLowerCase()}&locale=ar-AE`}
                  className={`block text-center py-3 rounded-lg font-semibold transition ${p.highlight ? 'bg-yellow-400 text-green-900 hover:bg-yellow-300' : 'bg-green-800 text-white hover:bg-green-700'}`}>
                  ابدأ الآن
                </Link>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Trust CTA */}
      <section className="py-16 px-4 text-center">
        <div className="max-w-3xl mx-auto">
          {/* Replaced a fabricated testimonial attributed to "ليلى الحمدان، صالون بيوتي لاونج، دبي"
              claiming "90% من عملائنا يحجزون أونلاين" (90% of our clients book online). No such
              customer and no such measurement exist. See the equivalent note in app/au/page.tsx. */}
          <h2 className="text-2xl font-bold mb-6 text-gray-900">ما الذي تحصل عليه من اليوم الأول</h2>
          <p className="text-lg text-gray-700 max-w-2xl mx-auto">
            حجوزات أونلاين، سجلات عملاء، وتذكيرات تلقائية عبر واتساب — مع فواتير متوافقة مع
            ضريبة القيمة المضافة الإماراتية بنسبة 5% وواجهة عربية بالكامل.
          </p>
          <div className="mt-12">
            <Link href="/register?locale=ar-AE"
              className="bg-green-800 text-white font-bold px-10 py-4 rounded-lg hover:bg-green-700 transition text-lg">
              ابدأ تجربتك المجانية لمدة 14 يوماً →
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
