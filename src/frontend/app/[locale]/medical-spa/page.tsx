// Not a Client Component. This page has no state, no effects and no event handlers — the
// 'use client' directive that stood here only prevented the file from exporting metadata,
// so the page fell back to app/[locale]/layout.tsx's generic site-wide title and
// description, identical to every other page under [locale].
import type { Metadata } from "next";
import React from "react";
import Link from "next/link";
import { Check, Shield, Lock, FileCheck, Heart, Users, Calendar } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { safeJsonLd } from "@/lib/jsonLd";
import { breadcrumbJsonLd, HOME_CRUMB } from "@/lib/seo";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://upkilo.com";

// Canonical is pinned to /en rather than built from the visitor's locale, matching the
// middleware rule that folds non-English marketing URLs onto English while those pages
// carry no real translations. When a locale passes the content gate, this becomes
// locale-aware in the same change that removes it from that redirect.
export const metadata: Metadata = {
  title: "Upkilo for Medical Spas — HIPAA-Ready Booking, Waivers & Consent",
  description:
    "Booking and client management for medical spas and aesthetic clinics. Digital waivers, contraindication tracking, treatment photos and a full compliance audit trail.",
  alternates: { canonical: `${SITE_URL}/en/medical-spa` },
  openGraph: {
    title: "Upkilo for Medical Spas — HIPAA-Ready Booking & Consent",
    description:
      "Digital waivers, contraindication tracking and compliance audit logs, in the same system that runs your bookings.",
    type: "website",
  },
};

const complianceFeatures = [
  { icon: Shield, title: "HIPAA-Ready Infrastructure", description: "BAA available, encrypted data at rest and in transit, audit logs retained 90 days." },
  { icon: Lock, title: "Digital Waivers & Consent", description: "Collect signed intake forms and treatment consent digitally. Auto-stored to client record." },
  { icon: FileCheck, title: "Medical History Tracking", description: "Contraindication alerts, skin analysis notes, and allergy records attached to every client." },
  { icon: Heart, title: "Treatment Photo Uploads", description: "Secure before/after photo storage with client consent. Never stored publicly." },
  { icon: Users, title: "Staff Role Permissions", description: "Granular roles ensure front desk cannot access medical records. Provider-only fields enforced." },
  { icon: Calendar, title: "Compliance Audit Trail", description: "Every record access, form submission, and data change is logged with timestamp and user ID." },
];

const medSpaUseCases = [
  "Botox & Dermal Filler Clinics",
  "Laser Hair Removal Studios",
  "IV Therapy Lounges",
  "Medi-Spas with Licensed Providers",
  "Aesthetic Nursing Practices",
  "Body Contouring Centers",
];

// A named customer testimonial ("Dr. Priya S., Medical Director, Aesthetic Clinic,
// Dubai") stood here and was fabricated — there are no customers to quote yet.
// Attributing an endorsement to a person who did not give one is a legal exposure under
// endorsement-advertising rules, independent of any SEO consideration, and it is the
// opposite of the first-hand-experience signal search engines actually reward.
//
// Replaced with a claim that is true today and needs no attribution. Restore a real
// quote here only with a named, consenting customer.

// Service rather than SoftwareApplication: this is a vertical landing page describing what
// Upkilo does *for medical spas*, not a second software product. Declaring another
// SoftwareApplication would describe a distinct application that does not exist and split the
// entity; a Service provided by the existing Organization is what this page actually is.
//
// serviceType and audience carry the vertical, which is the part an engine needs in order to
// return this page for "booking software for medical spas" rather than the generic homepage.
const MEDICAL_SPA_JSON_LD = {
  '@context': 'https://schema.org',
  '@type': 'Service',
  name: 'Upkilo for Medical Spas',
  serviceType: 'Medical spa booking and client management software',
  description:
    'Booking and client management for medical spas and aesthetic clinics, with digital waivers, contraindication tracking, treatment photos and a compliance audit trail.',
  url: `${SITE_URL}/en/medical-spa`,
  provider: { '@type': 'Organization', '@id': `${SITE_URL}/#organization` },
  audience: { '@type': 'BusinessAudience', name: 'Medical spas and aesthetic clinics' },
};

const BREADCRUMB_JSON_LD = breadcrumbJsonLd([
  HOME_CRUMB,
  { name: 'Medical Spas', path: '/en/medical-spa' },
]);

export default function MedicalSpaPage() {
  return (
    <div className="min-h-screen bg-card">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(MEDICAL_SPA_JSON_LD) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(BREADCRUMB_JSON_LD) }} />
      {/* Hero */}
      <section className="bg-gradient-to-br from-rose-50 to-pink-50 py-20 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <span className="inline-flex items-center rounded-full bg-rose-100 px-4 py-1 text-sm font-semibold text-rose-700 mb-6">
            <Shield className="h-4 w-4 mr-2" /> HIPAA-Ready Platform for Medical Spas
          </span>
          <h1 className="text-4xl sm:text-5xl font-extrabold text-foreground tracking-tight leading-tight">
            The Booking System Built for <span className="text-danger-fg">Medical-Grade Spas</span>
          </h1>
          <p className="mt-6 text-xl text-foreground-secondary max-w-2xl mx-auto">
            Upkilo handles digital waivers, contraindication tracking, before/after photos, and HIPAA audit logs — so you can focus on client outcomes, not compliance paperwork.
          </p>
          <div className="mt-10 flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/register?vertical=medical-spa">
              <Button className="px-8 py-4 text-lg bg-rose-600 hover:bg-rose-700 text-white">
                Start 14-Day Free Trial
              </Button>
            </Link>
            <Link href="/enterprise?interest=medical-spa">
              <Button className="px-8 py-4 text-lg bg-card text-foreground border border-border hover:bg-accent">
                Talk to Compliance Team
              </Button>
            </Link>
          </div>
          <p className="mt-4 text-sm text-foreground-secondary">No credit card required · BAA available on Business plan and above</p>
        </div>
      </section>

      {/* Use Cases */}
      <section className="py-16 px-4 bg-card">
        <div className="max-w-5xl mx-auto">
          <h2 className="text-2xl font-bold text-center text-foreground mb-10">Built for Every Medical Aesthetics Business</h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
            {medSpaUseCases.map((useCase) => (
              <div key={useCase} className="flex items-center gap-3 p-4 rounded-xl border border-border-subtle bg-muted">
                <Check className="h-5 w-5 text-danger-fg flex-shrink-0" />
                <span className="text-sm font-medium text-foreground">{useCase}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Compliance Features */}
      <section className="py-16 px-4 bg-rose-500/8">
        <div className="max-w-5xl mx-auto">
          <h2 className="text-3xl font-extrabold text-center text-foreground mb-4">Compliance Without the Complexity</h2>
          <p className="text-center text-foreground-secondary mb-12 max-w-2xl mx-auto">
            Every feature is designed around the real workflows of licensed aesthetic providers — not retrofitted from generic booking software.
          </p>
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-8">
            {complianceFeatures.map((f) => (
              <div key={f.title} className="bg-card rounded-2xl p-6 shadow-[var(--shadow-card)] border border-rose-500/20">
                <f.icon className="h-8 w-8 text-danger-fg mb-4" />
                <h3 className="font-bold text-foreground mb-2">{f.title}</h3>
                <p className="text-sm text-foreground-secondary">{f.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* HIPAA Detail Block */}
      <section className="py-16 px-4 bg-card">
        <div className="max-w-3xl mx-auto bg-gray-900 rounded-3xl p-10 text-white">
          <Shield className="h-10 w-10 text-rose-400 mb-4" />
          <h2 className="text-2xl font-bold mb-3">What "HIPAA-Ready" means in Upkilo</h2>
          <ul className="space-y-3 text-gray-300 text-sm">
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> Business Associate Agreement (BAA) available on Business and above plans</li>
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> All Protected Health Information (PHI) encrypted at rest (AES-256) and in transit (TLS 1.3)</li>
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> 90-day audit log retention with immutable record of all data access events</li>
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> Role-based access control — clinical notes visible only to authorized providers</li>
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> Digital consent forms with e-signature capture stored per client</li>
            <li className="flex gap-3"><Check className="h-4 w-4 text-rose-400 mt-0.5 flex-shrink-0" /> Data residency options available for EU, UK, and Australia (Enterprise)</li>
          </ul>
          <p className="text-xs text-slate-300 mt-6">
            Upkilo is not a covered entity and does not provide legal compliance advice. Please consult your HIPAA compliance officer before deployment.
          </p>
        </div>
      </section>

      {/* Capability summary — see note above the removed testimonial constant */}
      <section className="py-16 px-4 bg-rose-500/8">
        <div className="max-w-2xl mx-auto text-center">
          <p className="text-xl text-foreground">
            Digital waivers, contraindication tracking, and a full compliance audit trail — in
            the same system that runs your bookings, clients, and payments.
          </p>
          <p className="mt-4 text-sm text-foreground-secondary">
            No separate consent tool, no paper intake forms, no spreadsheet of allergies.
          </p>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 px-4 bg-card text-center">
        <h2 className="text-3xl font-extrabold text-foreground mb-4">Ready to modernize your medical spa?</h2>
        {/* "Join aesthetic clinics across the US, UK, UAE, and Australia running..." stood
            here — same problem as the removed testimonial: it asserts an existing customer
            base. Rewritten to describe what the product supports, which is verifiable. */}
        <p className="text-foreground-secondary mb-8 max-w-xl mx-auto">
          Set up digital waivers, consent tracking, and HIPAA-ready audit logs in an afternoon.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link href="/register?vertical=medical-spa">
            <Button className="px-10 py-4 text-lg bg-rose-600 hover:bg-rose-700 text-white">
              Get Started Free
            </Button>
          </Link>
          <Link href="/pricing">
            <Button className="px-10 py-4 text-lg bg-card text-foreground border border-border hover:bg-accent">
              View Pricing
            </Button>
          </Link>
        </div>
      </section>
    </div>
  );
}
