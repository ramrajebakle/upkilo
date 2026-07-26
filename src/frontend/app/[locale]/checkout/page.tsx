'use client';

import { useState, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { Check, ArrowRight } from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { apiClient } from '@/lib/api';

const plans = [
    { id: 'starter', name: 'Starter', price: 29 },
    { id: 'professional', name: 'Professional', price: 49 },
    { id: 'business', name: 'Business', price: 99 },
    { id: 'enterprise', name: 'Enterprise', price: 199 },
];

function CheckoutContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const planId = searchParams.get('plan') || 'professional';
    const isAnnual = searchParams.get('annual') === 'true';

    const [loading, setLoading] = useState(false);
    const [selectedPlan, setSelectedPlan] = useState(
        plans.find((p) => p.id === planId) || plans[1]
    );

    const handleCheckout = async () => {
        setLoading(true);
        try {
            const res = await apiClient.post('/api/v1/billing/create-checkout-session', {
                planId: selectedPlan.id,
                billingCycle: isAnnual ? 'annual' : 'monthly',
            });

            const checkoutUrl = res.data?.url || res.data?.checkoutUrl;
            if (checkoutUrl) {
                window.location.href = checkoutUrl;
            } else {
                // Fallback to success if no redirect URL returned
                router.push('/checkout/success');
            }
        } catch (err) {
            console.error('Failed to create checkout session:', err);
            setLoading(false);
        }
    };

    const price = isAnnual ? selectedPlan.price * 10 : selectedPlan.price;
    const savings = isAnnual ? selectedPlan.price * 2 : 0;

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-4xl mx-auto py-12 px-4">
                <div className="text-center mb-8">
                    <h1 className="text-3xl font-bold text-gray-900">Complete your order</h1>
                    <p className="text-gray-500 mt-2">14-day free trial included</p>
                </div>

                <div className="grid md:grid-cols-5 gap-8">
                    {/* Order summary */}
                    <div className="md:col-span-3">
                        <div className="bg-white rounded-xl shadow-sm p-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4">Order Summary</h2>

                            {/* Plan selection */}
                            <div className="space-y-3 mb-6">
                                {plans.map((plan) => (
                                    <button
                                        key={plan.id}
                                        onClick={() => setSelectedPlan(plan)}
                                        className={cn(
                                            'w-full flex items-center justify-between p-4 rounded-lg border-2 transition-all',
                                            selectedPlan.id === plan.id
                                                ? 'border-primary-500 bg-primary-50'
                                                : 'border-gray-200 hover:border-gray-300'
                                        )}
                                    >
                                        <div className="flex items-center gap-3">
                                            <div
                                                className={cn(
                                                    'w-5 h-5 rounded-full border-2 flex items-center justify-center',
                                                    selectedPlan.id === plan.id
                                                        ? 'border-primary-500 bg-primary-500'
                                                        : 'border-gray-300'
                                                )}
                                            >
                                                {selectedPlan.id === plan.id && (
                                                    <Check className="h-3 w-3 text-white" />
                                                )}
                                            </div>
                                            <span className="font-medium text-gray-900">{plan.name}</span>
                                        </div>
                                        <span className="font-semibold text-gray-900">
                                            {formatCurrency(isAnnual ? plan.price * 10 : plan.price)}
                                            <span className="text-gray-400 font-normal">
                                                /{isAnnual ? 'year' : 'month'}
                                            </span>
                                        </span>
                                    </button>
                                ))}
                            </div>

                            {/* Billing cycle */}
                            <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-lg mb-6">
                                <span className="text-gray-600">Billing cycle:</span>
                                <div className="flex gap-2">
                                    <button
                                        onClick={() => router.push(`?plan=${selectedPlan.id}&annual=false`)}
                                        className={cn(
                                            'px-3 py-1 rounded-full text-sm font-medium',
                                            !isAnnual ? 'bg-primary-500 text-white' : 'bg-gray-200 text-gray-600'
                                        )}
                                    >
                                        Monthly
                                    </button>
                                    <button
                                        onClick={() => router.push(`?plan=${selectedPlan.id}&annual=true`)}
                                        className={cn(
                                            'px-3 py-1 rounded-full text-sm font-medium',
                                            isAnnual ? 'bg-primary-500 text-white' : 'bg-gray-200 text-gray-600'
                                        )}
                                    >
                                        Annual (Save 17%)
                                    </button>
                                </div>
                            </div>

                            {/* Promo code */}
                            <div className="flex gap-2 mb-6">
                                <input
                                    type="text"
                                    placeholder="Promo code"
                                    className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                                />
                                <button className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200">
                                    Apply
                                </button>
                            </div>

                            {/* Price breakdown */}
                            <div className="border-t border-gray-200 pt-4 space-y-2">
                                <div className="flex justify-between text-gray-600">
                                    <span>{selectedPlan.name} plan</span>
                                    <span>{formatCurrency(price)}</span>
                                </div>
                                {savings > 0 && (
                                    <div className="flex justify-between text-green-600">
                                        <span>Annual discount</span>
                                        <span>-{formatCurrency(savings)}</span>
                                    </div>
                                )}
                                <div className="flex justify-between text-lg font-semibold text-gray-900 pt-2 border-t">
                                    <span>Total today</span>
                                    <span>$0.00</span>
                                </div>
                                <p className="text-sm text-gray-400">
                                    You won't be charged until after your 14-day free trial
                                </p>
                            </div>
                        </div>
                    </div>

                    {/* Checkout button */}
                    <div className="md:col-span-2">
                        <div className="bg-white rounded-xl shadow-sm p-6 sticky top-6">
                            <div className="mb-6">
                                <h3 className="font-semibold text-gray-900 mb-2">What's included:</h3>
                                <ul className="space-y-2">
                                    {[
                                        '14-day free trial',
                                        'No credit card required to start',
                                        'Cancel anytime',
                                        'All features unlocked',
                                        'Priority support during trial',
                                    ].map((item, i) => (
                                        <li key={i} className="flex items-center gap-2 text-sm text-gray-600">
                                            <Check className="h-4 w-4 text-green-500" />
                                            {item}
                                        </li>
                                    ))}
                                </ul>
                            </div>

                            <button
                                onClick={handleCheckout}
                                disabled={loading}
                                className="w-full bg-primary-500 hover:bg-primary-600 disabled:bg-gray-300 text-white py-3 rounded-lg font-semibold flex items-center justify-center gap-2"
                            >
                                {loading ? (
                                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                ) : (
                                    <>
                                        Start Free Trial
                                        <ArrowRight className="h-5 w-5" />
                                    </>
                                )}
                            </button>

                            <p className="text-xs text-gray-400 text-center mt-4">
                                By continuing, you agree to our{' '}
                                <a href="/terms" className="underline">
                                    Terms of Service
                                </a>{' '}
                                and{' '}
                                <a href="/privacy" className="underline">
                                    Privacy Policy
                                </a>
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default function CheckoutPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <div className="w-8 h-8 border-4 border-primary-500 border-t-transparent rounded-full animate-spin" />
            </div>
        }>
            <CheckoutContent />
        </Suspense>
    );
}
