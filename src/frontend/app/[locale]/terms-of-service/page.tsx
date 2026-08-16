import React from 'react';

export default function TermsOfServicePage() {
    return (
        <div className="min-h-screen bg-slate-50 py-16 px-4 sm:px-6 lg:px-8 dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
            <div className="max-w-3xl mx-auto bg-white dark:bg-slate-800 rounded-3xl shadow-xl overflow-hidden p-8 sm:p-12">
                <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4" style={{ fontFamily: 'var(--font-display)' }}>Terms of Service</h1>
                <p className="text-slate-500 dark:text-slate-400 mb-1 font-medium">Last Updated: June 2026 · Version 2.0</p>
                <p className="text-slate-500 dark:text-slate-400 mb-8 text-sm">
                    <strong>Upkilo Technologies Private Limited</strong> · Incorporated in India · Governed by Indian law
                </p>

                <div className="space-y-8 text-slate-700 dark:text-slate-300">

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">1. Acceptance of Terms</h2>
                        <p>By accessing or using the Upkilo platform (&quot;Service&quot;) provided by Upkilo Technologies Private Limited, you agree to be bound by these Terms of Service. If you do not agree to any part of these Terms, you may not access the Service.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">2. Description of Service</h2>
                        <p>Upkilo is a cloud-based SaaS platform that provides appointment scheduling, client management, billing, and business operations tools for service-based businesses worldwide.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">3. User Accounts</h2>
                        <p className="mb-4">You are responsible for maintaining the confidentiality of your account credentials and for all activities that occur under your account. You must notify us immediately of any unauthorized use. You must not:</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li>Share account credentials with unauthorized persons</li>
                            <li>Transmit malicious code, viruses, or content of a destructive nature</li>
                            <li>Use the Service for any illegal or unauthorized purpose</li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">4. Subscription &amp; Billing</h2>
                        <p>Subscriptions are billed in advance on a monthly or annual basis. You may cancel at any time; your access continues until the end of the current billing period. Refunds are not provided for partial periods. Prices are exclusive of applicable Indian GST and other taxes unless stated. We will notify you of price changes at least 30 days in advance.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">5. Data Ownership &amp; Portability</h2>
                        <p>You retain full ownership of all data you upload to the Service. We process your data only to deliver the Service as described in our <a href="/privacy-policy" className="underline">Privacy Policy</a>. You may export your data at any time via the built-in export tools. Upon account termination, your data is retained for 30 days then permanently deleted.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">6. Privacy &amp; Data Protection</h2>
                        <p>Your use of the Service is subject to our <a href="/privacy-policy" className="underline">Privacy Policy</a>. We are committed to protecting your privacy in compliance with the Digital Personal Data Protection Act, 2023, the Information Technology Act, 2000, and other applicable laws. We do not sell your data. Government and law enforcement data requests are handled per Section 12 of the Privacy Policy.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">7. Prohibited Uses</h2>
                        <p>You may not: (a) use the Service for any unlawful purpose under Indian law or the laws of your jurisdiction; (b) attempt to gain unauthorized access to any part of the Service or its infrastructure; (c) transmit malware, spam, or harmful content; (d) resell or sublicense the Service without written authorization; (e) store or transmit content that infringes intellectual property rights; (f) interfere with the Service&apos;s integrity or performance.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">8. Limitation of Liability</h2>
                        <p>To the maximum extent permitted by applicable law, Upkilo Technologies Private Limited shall not be liable for any indirect, incidental, special, consequential, or punitive damages, or any loss of profits or revenues. Our total aggregate liability shall not exceed the amounts paid by you in the 12 months preceding the claim. Nothing in these Terms limits liability for death, personal injury, or fraud caused by gross negligence.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">9. Termination</h2>
                        <p>We may suspend or terminate your access if you violate these Terms or applicable law. You may terminate your account at any time through account settings. Upon termination, your data is retained for 30 days per Section 5, after which it is permanently deleted.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">10. Changes to Terms</h2>
                        <p>We may update these Terms from time to time. We will notify you of material changes via email or in-app notification at least 30 days before they take effect. Continued use of the Service after changes constitutes acceptance.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">11. Governing Law &amp; Dispute Resolution</h2>
                        <p>These Terms are governed by the laws of India. Any disputes arising out of or in connection with these Terms shall first be attempted to be resolved through good-faith negotiation. If unresolved, disputes shall be subject to arbitration under the Arbitration and Conciliation Act, 1996 (India), conducted in English. The seat of arbitration shall be India. For users located in the European Union or other jurisdictions, mandatory consumer protection laws of your jurisdiction may also apply.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">12. Grievance Officer</h2>
                        <p>In accordance with the Information Technology Act, 2000 and the Digital Personal Data Protection Act, 2023, a Grievance Officer has been designated. Contact: <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a>. Grievances are acknowledged within 24 hours and resolved within 30 days.</p>
                    </section>

                </div>
            </div>
        </div>
    );
}
