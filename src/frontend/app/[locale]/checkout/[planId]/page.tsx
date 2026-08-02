"use client";

import React, { use, useState, useEffect } from "react";
import Link from "next/link";
import { CheckCircle2, ShieldCheck, CreditCard, Loader2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { apiClient } from "@/lib/api";

export default function CheckoutPage({ params }: { params: Promise<{ planId: string }> }) {
  const { planId } = use(params);
  // ⚠️ MUST match PricingSeeder.cs — this is the payment screen. These were previously
  // hardcoded at 24/65/165 against seeded prices of 39/89/149, so the checkout page showed
  // a price the customer was never charged. Enterprise is IsCustom (no price rows) and is
  // sales-led, so it has no self-serve amount.
  // Tracked follow-up: source these from GET /api/v1/pricing/plans instead of duplicating.
  const planName =
    planId === "growth" ? "Growth" :
    planId === "enterprise" ? "Enterprise" : "Starter";
  const price = planId === "growth" ? 499 : planId === "enterprise" ? 0 : 149;

  const [loading, setLoading] = useState(false);
  const [nameOnCard, setNameOnCard] = useState('');
  const [address, setAddress] = useState({ street: '', city: '', state: '', zip: '', country: 'United States' });

  const handlePayment = async () => {
    setLoading(true);
    try {
      const res = await apiClient.post('/api/v1/billing/create-checkout-session', {
        planId: planId,
        billingCycle: 'annual',
        nameOnCard,
        billingAddress: address,
      });

      const checkoutUrl = res.data?.url || res.data?.checkoutUrl;
      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err) {
      console.error('Payment failed:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="text-center mb-10">
          <Link href="/pricing" className="text-sm text-primary hover:underline font-medium mb-4 inline-block">&larr; Back to Pricing</Link>
          <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight">Complete your subscription</h1>
          <p className="mt-2 text-gray-600">You're upgrading to the {planName} plan.</p>
        </div>

        <div className="grid lg:grid-cols-12 gap-8 items-start">
          {/* Checkout Form */}
          <div className="lg:col-span-7 space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Payment Details</CardTitle>
                <CardDescription>All transactions are secure and encrypted.</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Stripe-hosted payment — redirect flow */}
                <div className="bg-gradient-to-r from-indigo-50 to-purple-50 p-4 rounded-md border border-indigo-100 text-center text-sm text-indigo-600 font-medium">
                  <ShieldCheck className="w-5 h-5 mx-auto mb-1 text-indigo-500" />
                  Secure payment via Stripe — you'll be redirected to complete payment
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Name on card</label>
                  <Input placeholder="Jane Doe" value={nameOnCard} onChange={e => setNameOnCard(e.target.value)} />
                </div>
                
                <div className="pt-4 border-t mt-4">
                  <h3 className="font-semibold text-sm mb-4">Billing Address</h3>
                  <div className="space-y-4">
                    <Input placeholder="Street Address" value={address.street} onChange={e => setAddress(prev => ({ ...prev, street: e.target.value }))} />
                    <div className="grid grid-cols-2 gap-4">
                      <Input placeholder="City" value={address.city} onChange={e => setAddress(prev => ({ ...prev, city: e.target.value }))} />
                      <Input placeholder="State / Province" value={address.state} onChange={e => setAddress(prev => ({ ...prev, state: e.target.value }))} />
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <Input placeholder="ZIP / Postal Code" value={address.zip} onChange={e => setAddress(prev => ({ ...prev, zip: e.target.value }))} />
                      <select className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm" value={address.country} onChange={e => setAddress(prev => ({ ...prev, country: e.target.value }))}>
                        <option>United States</option>
                        <option>Canada</option>
                        <option>United Kingdom</option>
                      </select>
                    </div>
                  </div>
                </div>

                <Button className="w-full py-6 text-lg mt-6 shadow-md" onClick={handlePayment} disabled={loading}>
                  {loading ? (
                    <Loader2 className="w-5 h-5 mr-2 animate-spin" />
                  ) : (
                    <CreditCard className="w-5 h-5 mr-2" />
                  )}
                  {loading ? 'Processing...' : `Pay $${price}.00 / month`}
                </Button>
              </CardContent>
            </Card>
          </div>

          {/* Order Summary */}
          <div className="lg:col-span-5 space-y-6">
            <Card className="bg-primary/5 border-primary/20 shadow-none">
              <CardHeader pb-2>
                <CardTitle className="text-lg">Order Summary</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex justify-between py-2 border-b border-primary/10">
                  <span className="font-medium">{planName} Plan (Annual)</span>
                  <span className="font-medium">${price}.00</span>
                </div>
                <div className="flex justify-between py-2 text-sm text-gray-600">
                  <span>Taxes</span>
                  <span>Calculated at next step</span>
                </div>
                <div className="flex justify-between py-4 mt-2 border-t font-bold text-lg">
                  <span>Total Due Today</span>
                  <span>${price}.00</span>
                </div>
                
                <div className="mt-6 flex items-start gap-3 text-sm text-gray-600">
                  <ShieldCheck className="w-5 h-5 text-green-600 shrink-0" />
                  <p>Guaranteed safe & secure checkout powered by Stripe. 14-day money-back guarantee.</p>
                </div>
              </CardContent>
            </Card>

            <div className="bg-white p-6 rounded-xl border border-gray-200 flex gap-4 items-center">
              <div className="w-12 h-12 bg-gray-100 rounded-full flex items-center justify-center shrink-0">
                <CheckCircle2 className="w-6 h-6 text-primary" />
              </div>
              <div>
                <h4 className="font-semibold text-sm">Instant Access</h4>
                <p className="text-xs text-gray-500 mt-1">Your account will be upgraded immediately upon successful payment.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
