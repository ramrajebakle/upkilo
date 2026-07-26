'use client';

import Link from 'next/link';
import { CheckCircle2, ArrowRight } from 'lucide-react';

export default function CheckoutSuccessPage() {
    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
            <div className="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
                <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
                    <CheckCircle2 className="h-10 w-10 text-green-500" />
                </div>

                <h1 className="text-2xl font-bold text-gray-900 mb-2">
                    Welcome to Upkilo!
                </h1>
                <p className="text-gray-500 mb-8">
                    Your 14-day free trial has started. We've sent a confirmation email with your account details.
                </p>

                <div className="bg-gray-50 rounded-lg p-4 mb-6">
                    <h3 className="font-medium text-gray-900 mb-3">Next steps:</h3>
                    <ul className="space-y-3 text-left">
                        {[
                            'Set up your business profile',
                            'Add your services',
                            'Invite team members',
                            'Share your booking page',
                        ].map((step, i) => (
                            <li key={i} className="flex items-center gap-3 text-sm text-gray-600">
                                <span className="flex-shrink-0 w-6 h-6 bg-primary-100 text-primary-600 rounded-full flex items-center justify-center text-xs font-medium">
                                    {i + 1}
                                </span>
                                {step}
                            </li>
                        ))}
                    </ul>
                </div>

                <Link
                    href="/dashboard"
                    className="w-full bg-primary-500 hover:bg-primary-600 text-white py-3 rounded-lg font-semibold flex items-center justify-center gap-2 mb-4"
                >
                    Go to Dashboard
                    <ArrowRight className="h-5 w-5" />
                </Link>

                <p className="text-sm text-gray-400">
                    Need help?{' '}
                    <a href="/support" className="text-primary-500 hover:underline">
                        Contact support
                    </a>
                </p>
            </div>
        </div>
    );
}
