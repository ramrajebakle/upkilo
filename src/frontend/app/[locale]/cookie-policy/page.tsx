import React from 'react';
import type { Metadata } from 'next';
import { ManageCookiesButton } from '@/components/ManageCookiesButton';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

export const metadata: Metadata = {
  title: 'Cookie Policy — Upkilo',
  description:
    'Which cookies Upkilo sets, what each one does, how long it lasts, and how to change or withdraw your consent at any time.',
  alternates: { canonical: `${SITE_URL}/en/cookie-policy` },
  openGraph: {
    title: 'Cookie Policy — Upkilo',
    description: 'Which cookies Upkilo sets, what they do, and how to manage your consent.',
    url: `${SITE_URL}/en/cookie-policy`,
    type: 'website',
  },
};

export default function CookiePolicyPage() {
    return (
        <div className="min-h-screen bg-slate-50 py-16 px-4 sm:px-6 lg:px-8 dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
            <div className="max-w-3xl mx-auto bg-white dark:bg-slate-800 rounded-3xl shadow-xl overflow-hidden p-8 sm:p-12">
                <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4" style={{ fontFamily: 'var(--font-display)' }}>Cookie Policy</h1>
                <p className="text-slate-500 dark:text-slate-400 mb-1 font-medium">Last Updated: June 2026 · Version 2.0</p>
                <p className="text-slate-500 dark:text-slate-400 mb-8 text-sm">
                    <strong>Upkilo Technologies Private Limited</strong> · Incorporated in India ·
                    Governing law: DPDP Act 2023, IT Act 2000, ePrivacy Directive (EU users)
                </p>

                <div className="space-y-8 text-slate-700 dark:text-slate-300">

                    <div className="flex items-center justify-between bg-primary-50 dark:bg-primary-900/20 border border-primary-200 dark:border-primary-700 rounded-xl px-5 py-4">
                        <div>
                            <p className="font-semibold text-slate-900 dark:text-white text-sm">Manage your cookie preferences</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">You can change or withdraw your consent at any time.</p>
                        </div>
                        <ManageCookiesButton className="shrink-0 ml-4 px-4 py-2 rounded-lg bg-primary-600 text-white text-sm font-semibold cursor-pointer border-0 hover:bg-primary-700 transition-colors">
                            Open Preferences
                        </ManageCookiesButton>
                    </div>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">1. What Are Cookies</h2>
                        <p className="mb-3">Cookies are small text files stored on your device by your browser when you visit a website. They help websites remember your preferences, keep you logged in, and understand how the site is used.</p>
                        <p>Upkilo also uses similar technologies including localStorage (client-side browser storage) and session storage. This policy covers all such technologies.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">2. Cookie Categories &amp; Legal Basis</h2>

                        <div className="space-y-4">
                            <div className="border border-slate-200 dark:border-slate-600 rounded-xl overflow-hidden">
                                <div className="bg-slate-100 dark:bg-slate-700 px-4 py-3 flex items-center justify-between">
                                    <div>
                                        <span className="font-semibold text-slate-900 dark:text-white">Essential</span>
                                        <span className="ml-2 text-xs px-2 py-0.5 rounded-full bg-slate-300 dark:bg-slate-600 text-slate-700 dark:text-slate-200">Always Active</span>
                                    </div>
                                    <span className="text-xs text-slate-500">Legal basis: Strictly necessary (no consent required)</span>
                                </div>
                                <p className="px-4 py-3 text-sm">Required for the platform to operate. Without these cookies, services such as authentication and booking management cannot function. Consent is not required under ePrivacy Directive strictly-necessary exemption and DPDP Act 2023.</p>
                            </div>

                            <div className="border border-slate-200 dark:border-slate-600 rounded-xl overflow-hidden">
                                <div className="bg-primary-50 dark:bg-primary-900/20 px-4 py-3 flex items-center justify-between">
                                    <div>
                                        <span className="font-semibold text-slate-900 dark:text-white">Functional</span>
                                        <span className="ml-2 text-xs px-2 py-0.5 rounded-full bg-primary-200 dark:bg-primary-700 text-primary-800 dark:text-primary-200">Consent Required</span>
                                    </div>
                                    <span className="text-xs text-slate-500">Legal basis: DPDP Act S.6 consent / GDPR Art. 6(1)(a)</span>
                                </div>
                                <p className="px-4 py-3 text-sm">Remember your preferences (dark/light mode, language, UI layout) across sessions to personalise your experience.</p>
                            </div>

                            <div className="border border-slate-200 dark:border-slate-600 rounded-xl overflow-hidden">
                                <div className="bg-amber-50 dark:bg-amber-900/20 px-4 py-3 flex items-center justify-between">
                                    <div>
                                        <span className="font-semibold text-slate-900 dark:text-white">Analytics</span>
                                        <span className="ml-2 text-xs px-2 py-0.5 rounded-full bg-amber-200 dark:bg-amber-700 text-amber-800 dark:text-amber-200">Consent Required</span>
                                    </div>
                                    <span className="text-xs text-slate-500">Legal basis: DPDP Act S.6 consent / GDPR Art. 6(1)(a)</span>
                                </div>
                                <p className="px-4 py-3 text-sm">Help us understand how users interact with the platform. Data is aggregated and anonymised. No personally identifiable information leaves our systems.</p>
                            </div>

                            <div className="border border-slate-200 dark:border-slate-600 rounded-xl overflow-hidden">
                                <div className="bg-rose-50 dark:bg-rose-900/20 px-4 py-3 flex items-center justify-between">
                                    <div>
                                        <span className="font-semibold text-slate-900 dark:text-white">Marketing</span>
                                        <span className="ml-2 text-xs px-2 py-0.5 rounded-full bg-rose-200 dark:bg-rose-700 text-rose-800 dark:text-rose-200">Consent Required</span>
                                    </div>
                                    <span className="text-xs text-slate-500">Legal basis: DPDP Act S.6 consent / GDPR Art. 6(1)(a)</span>
                                </div>
                                <p className="px-4 py-3 text-sm">Track the source and effectiveness of marketing campaigns (UTM parameters, referral attribution). No data is shared with advertising networks.</p>
                            </div>
                        </div>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">3. Cookie Inventory</h2>
                        <p className="mb-4 text-sm text-slate-500 dark:text-slate-400">Complete list of cookies and storage items used by Upkilo. &quot;S&quot; = Session (deleted when browser closes). &quot;P&quot; = Persistent (retained until expiry date).</p>

                        <h3 className="font-semibold text-slate-800 dark:text-slate-200 mb-2 mt-4">Essential Cookies</h3>
                        <div className="overflow-x-auto mb-6">
                            <table className="w-full text-sm border-collapse">
                                <thead>
                                    <tr className="bg-slate-100 dark:bg-slate-700 text-left">
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Name</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Type</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Purpose</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Retention</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Party</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                                    {[
                                        { name: '__Host-auth_token', type: 'S/P', purpose: 'JWT authentication token — keeps you logged in', retention: 'Session / 7 days', party: '1st' },
                                        { name: 'session_id', type: 'S', purpose: 'Server-side session identifier for stateful requests', retention: 'Session', party: '1st' },
                                        { name: '__Host-csrf', type: 'S', purpose: 'CSRF protection token for form submissions', retention: 'Session', party: '1st' },
                                        { name: 'timezone', type: 'P', purpose: 'IANA timezone string for server-side time rendering (appointment times, booking dates). Contains no personal identifier.', retention: '1 year', party: '1st' },
                                    ].map(row => (
                                        <tr key={row.name} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 font-mono text-xs">{row.name}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.type}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.purpose}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 whitespace-nowrap">{row.retention}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 text-center">{row.party}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <h3 className="font-semibold text-slate-800 dark:text-slate-200 mb-2 mt-4">Functional Storage</h3>
                        <div className="overflow-x-auto mb-6">
                            <table className="w-full text-sm border-collapse">
                                <thead>
                                    <tr className="bg-slate-100 dark:bg-slate-700 text-left">
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Name</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Storage</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Purpose</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Retention</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Party</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                                    {[
                                        { name: 'theme', storage: 'localStorage', purpose: 'Dark/light mode preference', retention: 'Indefinite (until cleared)', party: '1st' },
                                        { name: 'language', storage: 'localStorage', purpose: 'Preferred interface language / locale', retention: 'Indefinite', party: '1st' },
                                        { name: 'sidebar_collapsed', storage: 'localStorage', purpose: 'Sidebar open/collapsed state preference', retention: 'Indefinite', party: '1st' },
                                    ].map(row => (
                                        <tr key={row.name} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 font-mono text-xs">{row.name}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.storage}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.purpose}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 whitespace-nowrap">{row.retention}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 text-center">{row.party}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <h3 className="font-semibold text-slate-800 dark:text-slate-200 mb-2 mt-4">Analytics Storage</h3>
                        <div className="overflow-x-auto mb-6">
                            <table className="w-full text-sm border-collapse">
                                <thead>
                                    <tr className="bg-slate-100 dark:bg-slate-700 text-left">
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Name</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Storage</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Purpose</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Retention</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Party</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                                    {[
                                        { name: 'usage_session', storage: 'sessionStorage', purpose: 'Tracks page views and feature interactions within a single session. Aggregated server-side; PII is not stored.', retention: 'Session', party: '1st' },
                                        { name: 'feature_flags_cache', storage: 'localStorage', purpose: 'Caches evaluated feature flags to avoid redundant API calls', retention: '24 hours', party: '1st' },
                                    ].map(row => (
                                        <tr key={row.name} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 font-mono text-xs">{row.name}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.storage}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.purpose}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 whitespace-nowrap">{row.retention}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 text-center">{row.party}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <h3 className="font-semibold text-slate-800 dark:text-slate-200 mb-2 mt-4">Marketing Storage</h3>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm border-collapse">
                                <thead>
                                    <tr className="bg-slate-100 dark:bg-slate-700 text-left">
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Name</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Storage</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Purpose</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Retention</th>
                                        <th className="px-3 py-2 font-semibold border border-slate-200 dark:border-slate-600 text-slate-700 dark:text-slate-200">Party</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                                    {[
                                        { name: 'utm_source', storage: 'sessionStorage', purpose: 'Tracks the marketing channel that referred you (e.g., email, social, paid)', retention: 'Session', party: '1st' },
                                        { name: 'referral_code', storage: 'localStorage', purpose: 'Stores referral attribution code for affiliate tracking', retention: '30 days', party: '1st' },
                                    ].map(row => (
                                        <tr key={row.name} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 font-mono text-xs">{row.name}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.storage}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600">{row.purpose}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 whitespace-nowrap">{row.retention}</td>
                                            <td className="px-3 py-2 border border-slate-200 dark:border-slate-600 text-center">{row.party}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">4. Consent Management</h2>
                        <p className="mb-3">When you first visit Upkilo, a consent banner allows you to:</p>
                        <ul className="list-disc pl-5 space-y-2 mb-4">
                            <li><strong>Accept All</strong> — enables all cookie categories</li>
                            <li><strong>Reject All</strong> — only essential cookies are used</li>
                            <li><strong>Customise</strong> — choose individual categories with toggle switches</li>
                        </ul>
                        <p className="mb-3">Your consent is stored locally and synced to our servers under GDPR Art. 7(1) for proof-of-consent purposes. Your choice is versioned: if we update our cookie practices materially, you will be prompted to re-consent.</p>
                        <p className="font-medium">To change your preferences at any time, click <strong>&quot;Cookies&quot;</strong> in the footer of any page.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">5. Browser Cookie Controls</h2>
                        <p className="mb-3">You can also control cookies through your browser settings. Note that blocking essential cookies will prevent the platform from functioning correctly.</p>
                        <ul className="list-disc pl-5 space-y-1 text-sm">
                            <li><a href="https://support.google.com/chrome/answer/95647" className="underline text-primary-600 dark:text-primary-400" rel="noopener noreferrer">Google Chrome</a></li>
                            <li><a href="https://support.mozilla.org/en-US/kb/enable-and-disable-cookies-website-preferences" className="underline text-primary-600 dark:text-primary-400" rel="noopener noreferrer">Mozilla Firefox</a></li>
                            <li><a href="https://support.apple.com/en-gb/guide/safari/sfri11471/mac" className="underline text-primary-600 dark:text-primary-400" rel="noopener noreferrer">Apple Safari</a></li>
                            <li><a href="https://support.microsoft.com/en-us/windows/manage-cookies-in-microsoft-edge" className="underline text-primary-600 dark:text-primary-400" rel="noopener noreferrer">Microsoft Edge</a></li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">6. Third-Party Services</h2>
                        <p className="mb-3">Upkilo integrates the following third-party processors that may set their own cookies or access identifiers:</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Stripe</strong> (payments) — sets cookies for fraud detection and secure payment processing. <a href="https://stripe.com/privacy" className="underline text-primary-600 dark:text-primary-400" rel="noopener noreferrer">Stripe Privacy Policy</a></li>
                            <li><strong>SendGrid / Twilio</strong> (email/SMS) — used only to deliver transactional messages; no tracking pixels are embedded by Upkilo</li>
                            <li><strong>Microsoft Azure</strong> (cloud infrastructure) — your data is processed on Azure servers; no Azure-specific tracking cookies are set</li>
                        </ul>
                        <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Upkilo does not currently use Google Analytics, Meta Pixel, HubSpot, or any other third-party advertising or analytics platform. This policy will be updated if third-party analytics are added.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">7. Do Not Track</h2>
                        <p>Upkilo respects the Do Not Track (DNT) browser signal. When DNT is enabled, we disable all non-essential tracking and analytics automatically, regardless of stored cookie consent.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">8. Policy Updates</h2>
                        <p>We will update this Cookie Policy when we add new cookies or third-party services. Material changes will be notified via the consent banner (triggering a re-consent prompt) and via email to registered users.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">9. Contact</h2>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Privacy inquiries:</strong> <a href="mailto:privacy@upkilo.com" className="underline">privacy@upkilo.com</a></li>
                            <li><strong>Grievance Officer (India — DPDP Act / IT Act):</strong> <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a></li>
                            <li>Full details: see our <a href="/privacy-policy" className="underline">Privacy Policy</a></li>
                        </ul>
                    </section>

                </div>
            </div>
        </div>
    );
}
