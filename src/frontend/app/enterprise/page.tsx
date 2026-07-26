'use client';

import { useState } from 'react';

const FEATURES = [
  { icon: '🏢', title: 'Multi-Location Management', desc: 'Run unlimited locations from a single dashboard with per-location reporting.' },
  { icon: '🔐', title: 'SSO & SAML', desc: 'Enterprise-grade single sign-on with your existing identity provider.' },
  { icon: '📊', title: 'Advanced Analytics', desc: 'Custom dashboards, data exports, and BI integrations for enterprise reporting.' },
  { icon: '🤝', title: 'Dedicated Success Manager', desc: 'A named account manager who knows your business and is available 24/7.' },
  { icon: '🔌', title: 'Custom Integrations', desc: 'We build the integrations you need — CRM, ERP, payroll, and more.' },
  { icon: '📜', title: 'Custom SLA & BAA', desc: 'HIPAA BAAs, custom uptime SLAs, and enterprise security assessments.' },
  { icon: '🌍', title: 'Global Compliance', desc: 'GDPR, SOC 2 Type II, and regional compliance across 40+ countries.' },
  { icon: '🤖', title: 'AI Customization', desc: 'White-labelled AI receptionist and custom AI workflows built for your brand.' },
];

export default function EnterprisePage() {
  const [form, setForm] = useState({
    companyName: '', contactName: '', email: '', phone: '',
    teamSize: '', currentPlatform: '', useCase: '', message: ''
  });
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.companyName || !form.email) {
      setError('Company name and email are required.');
      return;
    }
    setSubmitting(true);
    setError('');

    const res = await fetch('/api/v1/enterprise/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form)
    });

    if (res.ok) {
      setSubmitted(true);
    } else {
      setError('Something went wrong. Please email enterprise@upkilo.com directly.');
    }
    setSubmitting(false);
  };

  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-slate-900 to-indigo-950 text-white py-24 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <span className="inline-block bg-indigo-500/20 text-indigo-300 text-sm font-semibold px-4 py-1.5 rounded-full mb-6 border border-indigo-500/30">
            Enterprise
          </span>
          <h1 className="text-4xl md:text-5xl font-bold mb-4 leading-tight">
            Built for businesses<br />that think bigger
          </h1>
          <p className="text-slate-300 text-lg max-w-2xl mx-auto mb-8">
            Multi-location chains, franchise networks, and enterprise service brands choose Upkilo for
            the reliability, compliance, and customization they need at scale.
          </p>
          <a href="#contact" className="inline-block bg-indigo-600 text-white px-8 py-4 rounded-2xl font-bold text-lg hover:bg-indigo-500 transition-colors">
            Talk to Enterprise Sales →
          </a>
        </div>
      </section>

      {/* Social proof */}
      <section className="bg-slate-50 py-8 px-4 border-b">
        <div className="max-w-4xl mx-auto flex flex-wrap items-center justify-center gap-8">
          {['10+ locations', 'HIPAA compliant', 'SOC 2 Type II', 'Custom SLA', '24/7 support'].map(t => (
            <div key={t} className="flex items-center gap-2 text-slate-600 text-sm font-medium">
              <span className="text-green-500">✓</span> {t}
            </div>
          ))}
        </div>
      </section>

      {/* Features grid */}
      <section className="max-w-5xl mx-auto py-20 px-4">
        <h2 className="text-2xl md:text-3xl font-bold text-slate-900 text-center mb-12">
          Everything enterprise teams need
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {FEATURES.map(f => (
            <div key={f.title} className="bg-slate-50 rounded-2xl p-5 border border-slate-100">
              <div className="text-3xl mb-3">{f.icon}</div>
              <h3 className="font-bold text-slate-900 mb-1">{f.title}</h3>
              <p className="text-sm text-slate-600">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Contact form */}
      <section id="contact" className="bg-slate-50 py-20 px-4 border-t">
        <div className="max-w-2xl mx-auto">
          <h2 className="text-2xl font-bold text-slate-900 mb-2">Let's talk about your business</h2>
          <p className="text-slate-500 mb-8">Fill in the form and our enterprise team will reach out within 24 hours.</p>

          {submitted ? (
            <div className="bg-green-50 border border-green-200 rounded-2xl p-10 text-center">
              <div className="text-5xl mb-4">✅</div>
              <h3 className="text-xl font-bold text-green-800 mb-2">We'll be in touch soon!</h3>
              <p className="text-green-700">Expect a reply within 24 hours from our enterprise team.</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="bg-white border border-slate-200 rounded-2xl p-8 shadow-sm space-y-4">
              {error && (
                <div className="bg-red-50 text-red-700 border border-red-200 rounded-xl px-4 py-3 text-sm">{error}</div>
              )}

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Company Name *</label>
                  <input required value={form.companyName} onChange={e => setForm(p => ({ ...p, companyName: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Your Name</label>
                  <input value={form.contactName} onChange={e => setForm(p => ({ ...p, contactName: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none" />
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Work Email *</label>
                  <input required type="email" value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                  <input type="tel" value={form.phone} onChange={e => setForm(p => ({ ...p, phone: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none" />
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Team Size</label>
                  <select value={form.teamSize} onChange={e => setForm(p => ({ ...p, teamSize: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none">
                    <option value="">Select...</option>
                    <option value="10-50">10–50 staff</option>
                    <option value="51-200">51–200 staff</option>
                    <option value="201-500">201–500 staff</option>
                    <option value="500+">500+ staff</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Current Platform</label>
                  <input placeholder="e.g. Mindbody, Vagaro..." value={form.currentPlatform}
                    onChange={e => setForm(p => ({ ...p, currentPlatform: e.target.value }))}
                    className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none" />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Primary Use Case</label>
                <select value={form.useCase} onChange={e => setForm(p => ({ ...p, useCase: e.target.value }))}
                  className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none">
                  <option value="">Select...</option>
                  <option value="multi-location">Multi-Location Management</option>
                  <option value="franchise">Franchise Network</option>
                  <option value="white-label">White-Label / Reseller</option>
                  <option value="api-integration">API Integration</option>
                  <option value="other">Other</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tell us more (optional)</label>
                <textarea rows={3} value={form.message} onChange={e => setForm(p => ({ ...p, message: e.target.value }))}
                  placeholder="What are you trying to accomplish? What's your timeline?"
                  className="w-full border border-gray-300 rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-indigo-400 outline-none resize-none" />
              </div>

              <button type="submit" disabled={submitting}
                className="w-full bg-indigo-600 text-white py-3 rounded-xl font-bold hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center justify-center gap-2">
                {submitting && <span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full" />}
                {submitting ? 'Submitting...' : 'Get in Touch →'}
              </button>
            </form>
          )}
        </div>
      </section>
    </main>
  );
}
