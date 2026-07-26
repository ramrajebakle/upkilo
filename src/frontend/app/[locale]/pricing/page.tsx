"use client";

import React, { useState } from "react";
import Link from "next/link";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/Button";

const plans = [
  {
    id: "starter",
    name: "Starter",
    description: "Perfect for solo professionals just starting out.",
    priceMonthly: 39,
    priceAnnually: 31,
    features: ["1 Staff Member", "Up to 1,000 Clients", "Basic CRM", "Online Booking Widget", "AI Copilot (500 actions/mo)", "Email Support"],
  },
  {
    id: "professional",
    name: "Professional",
    description: "Most popular for growing teams and clinics.",
    priceMonthly: 89,
    priceAnnually: 70,
    popular: true,
    features: ["Up to 5 Staff", "Unlimited Clients", "AI Workflows + Copilot", "SMS & Email Campaigns", "Stripe Payments", "Priority Support"],
  },
  {
    id: "business",
    name: "Business",
    description: "Advanced controls for multi-location businesses.",
    priceMonthly: 149,
    priceAnnually: 118,
    features: ["Up to 20 Staff", "Multi-Location Support", "Advanced API Access", "White-label Booking Pages", "Dedicated Success Manager"],
  }
];

const agencyPlan = {
  id: "agency",
  name: "Agency",
  description: "Manage multiple client businesses from one account.",
  priceMonthly: 249,
  priceAnnually: 197,
  features: [
    "Everything in Business",
    "Up to 20 Client Sub-Accounts",
    "Unified Agency Dashboard",
    "15,000 AI Actions / Month",
    "White-label for Clients",
    "Agency Commission Tracking",
    "Dedicated Account Manager",
  ],
};

export default function PricingPage() {
  const [annual, setAnnual] = useState(true);

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
                For Agencies & Consultants
              </span>
              <h2 className="text-3xl font-extrabold text-white tracking-tight">
                {agencyPlan.name} Plan
              </h2>
              <p className="mt-2 text-gray-300 text-sm max-w-lg">{agencyPlan.description}</p>
              <ul className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-2">
                {agencyPlan.features.map((f, i) => (
                  <li key={i} className="flex items-center text-sm text-gray-200">
                    <Check className="h-4 w-4 mr-2 text-amber-400 flex-shrink-0" />
                    {f}
                  </li>
                ))}
              </ul>
            </div>
            <div className="flex flex-col items-center md:items-end gap-4 min-w-[180px]">
              <div className="text-center md:text-right">
                <div className="text-4xl font-extrabold text-white">
                  ${annual ? agencyPlan.priceAnnually : agencyPlan.priceMonthly}
                  <span className="text-lg font-medium text-gray-400">/mo</span>
                </div>
                <p className="text-xs text-gray-400 mt-1">{annual ? 'Billed annually' : 'Billed monthly'}</p>
              </div>
              <Link href={`/checkout/${agencyPlan.id}?billing=${annual ? 'annual' : 'monthly'}`} className="w-full md:w-auto">
                <Button className="w-full bg-amber-400 hover:bg-amber-300 text-gray-900 font-bold py-4 px-8 text-base">
                  Start Agency Trial
                </Button>
              </Link>
              <p className="text-xs text-gray-500 text-center">14-day free trial · No credit card</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
