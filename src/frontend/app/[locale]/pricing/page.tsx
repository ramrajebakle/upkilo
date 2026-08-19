"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { PRICING_FAQS } from "@/lib/pricingFaqs";

// ⚠️ These figures MUST match PricingSeeder.cs. They are currently duplicated: the seeder
// is the billing source of truth, this array only renders the page. That duplication is
// exactly how Business once advertised $149 here while the database charged $199.
// Tracked follow-up: expose GET /api/v1/pricing/plans and render from it instead.
//
// priceAnnually is the EFFECTIVE MONTHLY cost when billed annually. The seeder stores the
// annual TOTAL (monthly × 10 — "2 months free"), so: 149→1,490 ($124.17) and
// 499→4,990 ($415.83).
const plans = [
  {
    id: "starter",
    name: "Starter",
    description: "For small teams running bookings and clients in one place.",
    priceMonthly: 149,
    priceAnnually: 124,
    features: [
      "Up to 10 Staff",
      "Up to 3 Locations",
      "Up to 5,000 Clients",
      "Online Booking Widget",
      "AI Copilot (2,000 actions/mo)",
      "SMS & Email Reminders",
      "Email Support",
    ],
  },
  {
    id: "growth",
    name: "Growth",
    description: "Most popular — AI automation, white-label and API for scaling businesses.",
    priceMonthly: 499,
    priceAnnually: 416,
    popular: true,
    features: [
      "Up to 25 Staff",
      "Up to 10 Locations",
      "Unlimited Clients",
      "AI Copilot, Workflows & Insights (10,000 actions/mo)",
      "Marketing Automation & Campaigns",
      "White-label Booking Pages",
      "API & Webhooks",
      "Priority Support",
    ],
  },
];

// Sold alongside the tiers so customers scale without being forced into a higher plan.
//
// These cards used to render a billing cadence with no amount next to it, because no add-on
// price existed anywhere in the codebase — only a Stripe Price ID per environment. Prices now
// come from GET /api/v1/billing/addons (seeded by PricingSeeder.SeedAddOnsAsync), which is the
// published-price source of truth.
//
// The array below is the offline fallback, used only if that call fails: an empty section reads
// as broken, and this is the same catalogue the seeder writes. It MUST be kept in step with
// PricingSeeder — same duplication caveat as the tiers above.
interface AddOn {
  key: string;
  name: string;
  billingUnit: string;
  amount: number | null;
  currency: string;
  isAvailable: boolean;
}

const FALLBACK_ADDONS: AddOn[] = [
  { key: "extra_staff", name: "Extra Staff", billingUnit: "per seat / month", amount: 19, currency: "USD", isAvailable: true },
  { key: "extra_locations", name: "Extra Locations", billingUnit: "per location / month", amount: 49, currency: "USD", isAvailable: true },
  { key: "ai_credits", name: "AI Credits", billingUnit: "per 1,000 actions", amount: 10, currency: "USD", isAvailable: true },
  { key: "sms_credits", name: "SMS Credits", billingUnit: "per 1,000 messages", amount: 10, currency: "USD", isAvailable: true },
  { key: "agency_sub_accounts", name: "Agency Sub-Accounts", billingUnit: "per account / month", amount: null, currency: "USD", isAvailable: false },
  { key: "premium_support", name: "Premium Support", billingUnit: "monthly", amount: null, currency: "USD", isAvailable: false },
];

// Upkilo bills exclusively in USD, but the code is formatted from the row rather than assumed —
// an amount rendered without the currency it is denominated in is how "₹X,XXX" shipped before.
function formatAddOnPrice(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    }).format(amount);
  } catch {
    return `${currency} ${amount}`;
  }
}

// Replaces the former Agency tier. Agency carried identical limits to Business and differed
// only by 20 sub-accounts for $100/mo more — not a defensible tier. Sub-accounts are now an
// add-on, and larger organisations route to sales instead.
const enterprisePlan = {
  id: "enterprise",
  name: "Enterprise",
  description: "Security, compliance and scale — for multi-brand and large organisations.",
  features: [
    "Everything in Growth",
    "Unlimited Staff & Locations",
    "SSO / SAML & Extended Audit Logs",
    "100,000 AI Actions / Month",
    "Agency Sub-Account Management",
    "Custom Integrations & SLA",
    "Dedicated Account Manager",
  ],
};

export default function PricingPage() {
  const [annual, setAnnual] = useState(true);
  const [addOns, setAddOns] = useState<AddOn[]>(FALLBACK_ADDONS);

  useEffect(() => {
    const base = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    let active = true;
    fetch(`${base}/api/v1/billing/addons`)
      .then((res) => (res.ok ? res.json() : null))
      .then((body) => {
        const rows: AddOn[] = body?.data ?? [];
        // Keep the fallback on an empty response — a database seeded before add-ons existed
        // returns [], and rendering nothing there is worse than rendering the known catalogue.
        if (active && rows.length > 0) setAddOns(rows);
      })
      .catch(() => {
        /* keep fallback */
      });
    return () => {
      active = false;
    };
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 py-16 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        <div className="text-center max-w-3xl mx-auto">
          <h1 className="text-4xl font-extrabold text-gray-900 sm:text-5xl tracking-tight">
            Simple, transparent pricing
          </h1>
          <p className="mt-4 text-xl text-gray-600">
            No hidden fees. No surprise charges. Choose the plan that fits your growth.
          </p>
          
          <div className="mt-8 flex justify-center items-center gap-3">
            <span className={`text-sm font-medium ${!annual ? 'text-gray-900' : 'text-gray-500'}`}>Monthly</span>
            <button 
              onClick={() => setAnnual(!annual)}
              className="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent bg-primary transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
            >
              <span className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${annual ? 'translate-x-5' : 'translate-x-0'}`} />
            </button>
            <span className={`text-sm font-medium ${annual ? 'text-gray-900' : 'text-gray-500'}`}>Annually <span className="text-green-600 text-xs bg-green-100 px-2 py-0.5 rounded-full ml-1">Save 21%</span></span>
          </div>
        </div>

        <div className="mt-16 grid gap-8 lg:grid-cols-3 max-w-md mx-auto lg:max-w-none">
          {plans.map((plan) => (
            <div key={plan.id} className={`flex flex-col rounded-3xl bg-white shadow-xl ring-1 ${plan.popular ? 'ring-2 ring-primary scale-105 z-10' : 'ring-gray-200'}`}>
              <div className="p-8 sm:p-10">
                {plan.popular && (
                  <span className="inline-flex items-center rounded-full bg-primary/10 px-2.5 py-1 text-xs font-semibold text-primary mb-4">
                    Most Popular
                  </span>
                )}
                <h3 className="text-2xl font-bold tracking-tight text-gray-900">{plan.name}</h3>
                <p className="mt-4 text-sm leading-6 text-gray-600 h-12">{plan.description}</p>
                <div className="mt-4 flex items-baseline text-5xl font-extrabold tracking-tight text-gray-900">
                  ${annual ? plan.priceAnnually : plan.priceMonthly}
                  <span className="ml-1 text-xl font-medium tracking-normal text-gray-500">/mo</span>
                </div>
                <p className="mt-1 text-sm text-gray-500">{annual ? 'Billed annually' : 'Billed monthly'}</p>

                <Link href={`/checkout/${plan.id}?billing=${annual ? 'annual' : 'monthly'}`} className="block mt-8">
                  <Button className={`w-full py-6 text-lg ${plan.popular ? '' : 'bg-gray-900 hover:bg-gray-800'}`}>
                    Get Started
                  </Button>
                </Link>
              </div>
              <div className="flex flex-1 flex-col justify-between p-8 sm:p-10 bg-gray-50 rounded-b-3xl border-t border-gray-100">
                <ul className="space-y-4">
                  {plan.features.map((feature, i) => (
                    <li key={i} className="flex items-start">
                      <Check className="h-5 w-5 flex-shrink-0 text-primary" />
                      <span className="ml-3 text-sm leading-6 text-gray-600">{feature}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          ))}
        </div>

        {/* Agency Plan CTA — full-width banner below main plans */}
        <div className="mt-16 max-w-4xl mx-auto rounded-3xl bg-gradient-to-r from-gray-900 to-gray-800 shadow-2xl overflow-hidden">
          <div className="px-8 py-10 sm:px-12 sm:py-12 flex flex-col md:flex-row md:items-center md:justify-between gap-8">
            <div className="flex-1">
              <span className="inline-flex items-center rounded-full bg-amber-400/20 px-3 py-1 text-xs font-semibold text-amber-300 mb-3">
                For Agencies & Large Organisations
              </span>
              <h2 className="text-3xl font-extrabold text-white tracking-tight">
                {enterprisePlan.name} Plan
              </h2>
              <p className="mt-2 text-gray-300 text-sm max-w-lg">{enterprisePlan.description}</p>
              <ul className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-2">
                {enterprisePlan.features.map((f, i) => (
                  <li key={i} className="flex items-center text-sm text-gray-200">
                    <Check className="h-4 w-4 mr-2 text-amber-400 flex-shrink-0" />
                    {f}
                  </li>
                ))}
              </ul>
            </div>
            <div className="flex flex-col items-center md:items-end gap-4 min-w-[180px]">
              <div className="text-center md:text-right">
                <div className="text-4xl font-extrabold text-white">Custom</div>
                <p className="text-xs text-gray-400 mt-1">Tailored to your organisation</p>
              </div>
              {/* Enterprise is IsCustom in PricingSeeder — no price rows, so no self-serve
                  checkout. Routing to sales instead of /checkout keeps the page honest. */}
              <Link href="/enterprise" className="w-full md:w-auto">
                <Button className="w-full bg-amber-400 hover:bg-amber-300 text-gray-900 font-bold py-4 px-8 text-base">
                  Contact Sales
                </Button>
              </Link>
              <p className="text-xs text-gray-500 text-center">30-day trial · Custom onboarding</p>
            </div>
          </div>
        </div>

        {/* Add-ons keep the tier count low. Rather than a plan for every combination of
            staff/locations/credits, customers scale the plan they already have. */}
        <div className="mt-16">
          <h2 className="text-2xl font-bold text-center tracking-tight">Scale without changing plan</h2>
          <p className="mt-2 text-center text-sm text-gray-500 max-w-2xl mx-auto">
            Need more than your plan includes? Add exactly what you use — no forced upgrade.
          </p>
          <div className="mt-8 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Unavailable add-ons are distinguished by surface and border rather than by
                dimming the whole card: opacity-50 over this text would push it under the 4.5:1
                contrast floor, trading one defect for another. The "Coming soon" wording carries
                the state on its own, so nothing here depends on colour alone. */}
            {addOns.map((a) => (
              <div
                key={a.key}
                className={`rounded-xl px-5 py-4 flex items-center justify-between gap-4 ${
                  a.isAvailable
                    ? "border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900"
                    : "border border-dashed border-gray-300 dark:border-gray-700 bg-transparent"
                }`}
              >
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                    <span className="font-semibold text-sm text-gray-900 dark:text-gray-100">{a.name}</span>
                    {!a.isAvailable && (
                      <span className="shrink-0 rounded-full bg-gray-200 dark:bg-gray-800 px-2 py-0.5 text-xs font-semibold text-gray-700 dark:text-gray-300">
                        Coming soon
                      </span>
                    )}
                  </div>
                  <p className="mt-1 text-xs text-gray-600 dark:text-gray-400">{a.billingUnit}</p>
                </div>
                {/* No amount = nothing published yet (roadmap add-ons). Showing a price there
                    would promise a checkout path that does not exist. */}
                {a.amount != null ? (
                  <span className="shrink-0 text-lg font-bold tracking-tight text-gray-900 dark:text-gray-100">
                    {formatAddOnPrice(a.amount, a.currency)}
                  </span>
                ) : (
                  <span className="shrink-0 text-xs font-medium text-gray-600 dark:text-gray-400">
                    Contact sales
                  </span>
                )}
              </div>
            ))}
          </div>
          <p className="mt-6 text-center text-sm text-gray-600">
            Add-on prices are in USD, excluding applicable taxes, and are billed on top of your plan.
          </p>
        </div>

        {/* Rendered from the same PRICING_FAQS array the layout emits as FAQPage JSON-LD.
            Native <details>/<summary> rather than a JS accordion: it is keyboard-operable and
            expandable without hydration, and crawlers read the answer text whether or not the
            item is open — a div toggled by useState would ship the same markup but depend on
            client JS for something that needs none. */}
        <section className="mt-20 max-w-3xl mx-auto" aria-labelledby="pricing-faq-heading">
          <h2 id="pricing-faq-heading" className="text-2xl font-bold text-center tracking-tight text-gray-900">
            Pricing questions
          </h2>
          <div className="mt-8 divide-y divide-gray-200 rounded-2xl bg-white ring-1 ring-gray-200">
            {PRICING_FAQS.map((faq) => (
              <details key={faq.question} className="group px-6 py-5">
                <summary className="flex cursor-pointer items-center justify-between gap-4 list-none font-semibold text-gray-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 rounded">
                  {faq.question}
                  <span
                    aria-hidden="true"
                    className="shrink-0 text-gray-500 transition-transform duration-200 group-open:rotate-45 motion-reduce:transition-none"
                  >
                    +
                  </span>
                </summary>
                <p className="mt-3 text-sm leading-6 text-gray-600">{faq.answer}</p>
              </details>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
