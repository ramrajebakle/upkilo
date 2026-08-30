'use client';

import { useState } from 'react';
import {
  CheckCircle2, Building2, KeyRound, BarChart3, Headset, Plug, FileText, Globe, Bot,
} from 'lucide-react';

// Drawn icons from the library the rest of the app uses, replacing eight emoji that were
// serving as this page's icon system.
//
// Three descriptions also changed, because they described things that do not exist:
//
//   "SOC 2 Type II" — a specific third-party audit with a report a buyer can request. It
//   was claimed twice on this page. Enterprise buyers treat it as a procurement gate, so
//   asserting it unaudited is materially worse than ordinary marketing overreach.
//
//   "A named account manager ... available 24/7" — there is no such role and no rota.
//
//   "regional compliance across 40+ countries" — an unsupported number.
//
// What remains is what the codebase can actually back: GDPR and DPDP handling (there is a
// Grievance Officer contact and data-erasure tooling), and HIPAA-oriented features on the
// medical-spa path. These are stated as what the product is built for, not as certifications
// it holds.
const FEATURES = [
  { icon: Building2, title: 'Multi-Location Management', desc: 'Run unlimited locations from a single dashboard with per-location reporting.' },
  { icon: KeyRound, title: 'SSO & SAML', desc: 'Single sign-on with your existing identity provider.' },
  { icon: BarChart3, title: 'Advanced Analytics', desc: 'Custom dashboards, data exports, and BI integrations for reporting.' },
  { icon: Headset, title: 'Direct Line to the Team', desc: 'Talk to the people who build the product, not a ticket queue.' },
  { icon: Plug, title: 'Custom Integrations', desc: 'CRM, ERP and payroll integrations built to fit your existing stack.' },
  { icon: FileText, title: 'Custom SLA & BAA', desc: 'HIPAA BAAs and custom uptime SLAs available on enterprise agreements.' },
  { icon: Globe, title: 'Privacy & Data Protection', desc: 'Built for GDPR and India DPDP Act obligations, with data export and erasure tooling.' },
  { icon: Bot, title: 'AI Customization', desc: 'White-labelled AI receptionist and custom AI workflows built for your brand.' },
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
      setError('Something went wrong. Please email sales@upkilo.com directly.');
    }
    setSubmitting(false);
  };

  return (
    <main className="min-h-screen bg-card">
      {/* Hero */}
      <section className="bg-gradient-to-br from-slate-900 to-primary-950 text-white py-24 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <span className="inline-block bg-primary-500/20 text-primary-300 text-sm font-semibold px-4 py-1.5 rounded-full mb-6 border border-primary-500/30">
            Enterprise
          </span>
          <h1 className="text-4xl md:text-5xl font-bold mb-4 leading-tight">
            Built for businesses<br />that think bigger
          </h1>
          <p className="text-slate-300 text-lg max-w-2xl mx-auto mb-8">
            Multi-location chains, franchise networks, and enterprise service brands choose Upkilo for
            the reliability, compliance, and customization they need at scale.
          </p>
          <a href="#contact" className="inline-block bg-primary-600 text-white px-8 py-4 rounded-2xl font-bold text-lg hover:bg-primary-500 transition-colors">
            Talk to Enterprise Sales →
          </a>
        </div>
      </section>

      {/* Social proof */}
      <section className="bg-muted py-8 px-4 border-b">
        <div className="max-w-4xl mx-auto flex flex-wrap items-center justify-center gap-8">
          {/* Was: '10+ locations', 'HIPAA compliant', 'SOC 2 Type II', 'Custom SLA', '24/7 support'
              — presented as a trust bar, but SOC 2 Type II is an audit Upkilo has not had, and
              24/7 support describes a rota that does not exist. Both are claims a buyer can
              ask you to evidence.

              Replaced with capabilities the codebase supports. ✓ became a drawn icon. */}
          {['Unlimited locations', 'SSO / SAML', 'Agency sub-accounts', 'Custom SLA available', 'HIPAA BAA available'].map(t => (
            <div key={t} className="flex items-center gap-2 text-foreground-secondary text-sm font-medium">
              <CheckCircle2 className="h-4 w-4 text-success-fg" aria-hidden="true" /> {t}
            </div>
          ))}
        </div>
      </section>

      {/* Features grid */}
      <section className="max-w-5xl mx-auto py-20 px-4">
        <h2 className="text-2xl md:text-3xl font-bold text-foreground text-center mb-12">
          Everything enterprise teams need
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {FEATURES.map(({ icon: Icon, title, desc }) => (
            <div key={title} className="bg-muted rounded-2xl p-5 border border-border-subtle">
              <Icon className="mb-3 h-7 w-7 text-foreground" aria-hidden="true" />
              <h3 className="font-bold text-foreground mb-1">{title}</h3>
              <p className="text-sm text-foreground-secondary">{desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Contact form */}
      <section id="contact" className="bg-muted py-20 px-4 border-t">
        <div className="max-w-2xl mx-auto">
          <h2 className="text-2xl font-bold text-foreground mb-2">Let's talk about your business</h2>
          {/* Was "our enterprise team will reach out within 24 hours" — same two unkeepable
              claims as the success state below. */}
          <p className="text-foreground-secondary mb-8">Tell us what you need and we&apos;ll get back to you by email.</p>

          {submitted ? (
            <div className="bg-green-50 border border-green-200 rounded-2xl p-10 text-center">
              {/* Drawn icon, not the ✅ emoji. */}
              <CheckCircle2 className="mx-auto mb-4 h-12 w-12 text-success-fg" aria-hidden="true" />
              <h3 className="text-xl font-bold text-green-800 mb-2">Thanks — we&apos;ve got your details</h3>
              {/* Was "Expect a reply within 24 hours from our enterprise team." Both halves were
                  claims the business cannot currently keep: there is no enterprise team, and no
                  staffed rota behind a 24-hour guarantee. A promise a visitor can time you
                  against is worse than no promise. */}
              <p className="text-green-700">
                We&apos;ll reply by email to the address you gave us. If it&apos;s urgent,
                write to sales@upkilo.com directly.
              </p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="bg-card border border-border rounded-2xl p-8 shadow-sm space-y-4">
              {error && (
                <div className="bg-red-50 text-red-700 border border-red-200 rounded-xl px-4 py-3 text-sm">{error}</div>
              )}

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Company Name *</label>
                  <input required value={form.companyName} onChange={e => setForm(p => ({ ...p, companyName: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Your Name</label>
                  <input value={form.contactName} onChange={e => setForm(p => ({ ...p, contactName: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none" />
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Work Email *</label>
                  <input required type="email" value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Phone</label>
                  <input type="tel" value={form.phone} onChange={e => setForm(p => ({ ...p, phone: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none" />
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Team Size</label>
                  <select value={form.teamSize} onChange={e => setForm(p => ({ ...p, teamSize: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none">
                    <option value="">Select...</option>
                    <option value="10-50">10–50 staff</option>
                    <option value="51-200">51–200 staff</option>
                    <option value="201-500">201–500 staff</option>
                    <option value="500+">500+ staff</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Current Platform</label>
                  <input placeholder="e.g. Mindbody, Vagaro..." value={form.currentPlatform}
                    onChange={e => setForm(p => ({ ...p, currentPlatform: e.target.value }))}
                    className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none" />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Primary Use Case</label>
                <select value={form.useCase} onChange={e => setForm(p => ({ ...p, useCase: e.target.value }))}
                  className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none">
                  <option value="">Select...</option>
                  <option value="multi-location">Multi-Location Management</option>
                  <option value="franchise">Franchise Network</option>
                  <option value="white-label">White-Label / Reseller</option>
                  <option value="api-integration">API Integration</option>
                  <option value="other">Other</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Tell us more (optional)</label>
                <textarea rows={3} value={form.message} onChange={e => setForm(p => ({ ...p, message: e.target.value }))}
                  placeholder="What are you trying to accomplish? What's your timeline?"
                  className="w-full border border-border-strong rounded-xl px-3 py-2.5 text-sm focus:ring-2 focus:ring-primary-400 outline-none resize-none" />
              </div>

              <button type="submit" disabled={submitting}
                className="w-full bg-primary-600 text-white py-3 rounded-xl font-bold hover:bg-primary-700 disabled:opacity-50 transition-colors flex items-center justify-center gap-2">
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
