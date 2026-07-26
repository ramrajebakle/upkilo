import React from 'react';

export default function PrivacyPolicyPage() {
    return (
        <div className="min-h-screen bg-slate-50 py-16 px-4 sm:px-6 lg:px-8 dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
            <div className="max-w-3xl mx-auto bg-white dark:bg-slate-800 rounded-3xl shadow-xl overflow-hidden p-8 sm:p-12">
                <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4" style={{ fontFamily: 'Outfit, sans-serif' }}>Privacy Policy</h1>
                <p className="text-slate-500 dark:text-slate-400 mb-1 font-medium">Last Updated: June 2026 · Version 2.0</p>
                <p className="text-slate-500 dark:text-slate-400 mb-1 text-sm">
                    <strong>Upkilo Technologies Private Limited</strong> · Incorporated in India · Operates globally
                </p>
                <p className="text-slate-500 dark:text-slate-400 mb-1 text-sm">
                    Grievance Officer: <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a>
                    &nbsp;·&nbsp;
                    Privacy: <a href="mailto:privacy@upkilo.com" className="underline">privacy@upkilo.com</a>
                </p>
                <p className="text-slate-400 dark:text-slate-500 mb-8 text-xs">
                    Primary law: Digital Personal Data Protection Act, 2023 (India) &amp; IT Act, 2000 ·
                    Extraterritorial: GDPR (EU/EEA), UK GDPR, CCPA (California)
                </p>

                <div className="space-y-8 text-slate-700 dark:text-slate-300">

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">1. About This Policy</h2>
                        <p>Upkilo Technologies Private Limited (&quot;Upkilo,&quot; &quot;we,&quot; &quot;us&quot;) is incorporated in India and provides a cloud-based scheduling and business management platform to users and businesses worldwide. This Privacy Policy explains how we collect, use, share, and protect your personal data, and describes your rights under applicable law.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">2. Information We Collect</h2>
                        <p className="mb-4">We collect information you provide directly when you create an account, use our services, or contact support:</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Account data:</strong> name, email address, phone number</li>
                            <li><strong>Business data:</strong> clients, bookings, services, and staff information you input</li>
                            <li><strong>Billing information:</strong> payment identifiers processed securely via Stripe (we do not store card numbers)</li>
                            <li><strong>Sensitive personal data (SPDI):</strong> financial information (payment records) handled under IT (SPDI) Rules, 2011</li>
                            <li><strong>Usage data:</strong> page views, feature interactions, and session activity</li>
                            <li><strong>Device information:</strong> browser type, operating system, IP address</li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">3. How We Use Your Information</h2>
                        <ul className="list-disc pl-5 space-y-2">
                            <li>Provide, maintain, and improve the Service</li>
                            <li>Process payments and send transaction confirmations</li>
                            <li>Send service notifications and customer support communications</li>
                            <li>Analyse usage patterns to improve features (with consent)</li>
                            <li>Comply with applicable Indian and international legal obligations</li>
                            <li>Detect and prevent fraud, abuse, and security threats</li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">4. Data Sharing</h2>
                        <p className="mb-4">We do not sell your personal data. We share data only with:</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Service providers</strong> acting as data processors under written agreements: Stripe (payments), SendGrid (email), Twilio (SMS), Microsoft Azure (cloud infrastructure)</li>
                            <li><strong>Government authorities</strong> only when required by valid legal process under applicable law (see Section 11)</li>
                            <li><strong>Business partners</strong> only with your explicit, informed, and freely given consent</li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">5. Data Retention</h2>
                        <p className="mb-4">We retain personal data only as long as necessary for the stated purpose or as required by applicable law (DPDP Act 2023; IT Act 2000; Indian tax and financial regulations):</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li>Account data: duration of account + 30 days after deletion</li>
                            <li>Audit logs: 90–730 days depending on subscription tier</li>
                            <li>Login history: 180 days</li>
                            <li>Financial records: 7 years under Indian tax law (in anonymised form only)</li>
                        </ul>
                        <p className="mt-4">You may request deletion at any time. See Section 6 for how.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">6. Security</h2>
                        <p>We implement reasonable security practices and procedures as required under the IT (Reasonable Security Practices and Procedures and Sensitive Personal Data or Information) Rules, 2011, including: AES-256 encryption at rest, TLS 1.3 in transit, multi-factor authentication, role-based access controls, multi-tenant data isolation, and regular security audits. In the event of a personal data breach, we will notify affected users and relevant authorities within 72 hours of becoming aware.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">7. Your Rights as a Data Principal</h2>
                        <p className="mb-3 font-medium">Under the Digital Personal Data Protection Act, 2023 (India):</p>
                        <ul className="list-disc pl-5 space-y-2 mb-4">
                            <li><strong>Right to information:</strong> know what personal data is being processed and for what purpose</li>
                            <li><strong>Right to correction and erasure:</strong> request correction of inaccurate data or erasure of data no longer needed</li>
                            <li><strong>Right to grievance redressal:</strong> contact our Grievance Officer at <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a></li>
                            <li><strong>Right to nominate:</strong> nominate a representative to exercise rights in the event of incapacity or death</li>
                            <li><strong>Right to withdraw consent:</strong> at any time, without affecting the lawfulness of prior processing</li>
                        </ul>
                        <p className="mb-3 font-medium">EU/EEA users additionally have GDPR rights:</p>
                        <ul className="list-disc pl-5 space-y-2 mb-4">
                            <li>Access (Art. 15), rectification (Art. 16), erasure (Art. 17), portability (Art. 20), restriction (Art. 18), objection (Art. 21)</li>
                        </ul>
                        <p className="mb-3 font-medium">California users additionally have CCPA rights:</p>
                        <ul className="list-disc pl-5 space-y-2">
                            <li>Right to Know (§1798.110), Delete (§1798.105), Portability (§1798.100), Opt-Out of Sale (§1798.120), Non-Discrimination (§1798.125)</li>
                        </ul>
                        <p className="mt-4">Contact <a href="mailto:privacy@upkilo.com" className="underline">privacy@upkilo.com</a> or use your account privacy settings. Requests are addressed within 30 days.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">8. Cookies &amp; Tracking</h2>
                        <p>We use essential cookies for authentication and session management. Optional analytics cookies help us improve the Service. You can manage all preferences through the cookie consent banner or account settings. See our <a href="/cookie-policy" className="underline">Cookie Policy</a>.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">9. Cross-Border Data Transfers</h2>
                        <p>We are incorporated in India and primarily process data in India. Where we transfer personal data outside India (via Stripe, SendGrid, Twilio, Azure), we comply with the DPDP Act, 2023, and transfer only to countries permitted under applicable Central Government notifications. For EU/EEA users, transfers outside the EEA are covered by Standard Contractual Clauses (SCCs) per EU Commission Decision 2021/914.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">10. Children&apos;s Privacy</h2>
                        <p>The Service is not directed to children under 18 years of age. We do not knowingly collect personal data from minors. If you believe a minor has provided us data, contact <a href="mailto:privacy@upkilo.com" className="underline">privacy@upkilo.com</a> and we will promptly delete it.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">11. Lawful Basis for Processing</h2>
                        <p className="mb-3 font-medium">Under the DPDP Act, 2023 (India), we process personal data based on:</p>
                        <ul className="list-disc pl-5 space-y-2 mb-4">
                            <li><strong>Consent (S.6):</strong> for analytics, marketing, and optional features — withdrawable at any time</li>
                            <li><strong>Legitimate uses (S.7):</strong> for compliance with Indian law, legal proceedings, medical emergencies, and State functions</li>
                            <li><strong>Contractual necessity:</strong> to provide the services you have subscribed to</li>
                        </ul>
                        <p className="text-sm text-slate-500">For EU/EEA users, corresponding GDPR Art. 6 bases apply: contract (6(1)(b)), legal obligation (6(1)(c)), consent (6(1)(a)), and legitimate interests (6(1)(f)).</p>
                    </section>

                    <section className="border-l-4 border-blue-500 pl-6 bg-blue-50 dark:bg-blue-900/20 py-4 rounded-r-xl">
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">12. Government &amp; Law Enforcement Requests</h2>
                        <p className="mb-4 font-semibold text-slate-900 dark:text-white">
                            We are committed to protecting user privacy and will not disclose user information to governments, agencies, organizations, or third parties except where required by applicable law and valid legal process. Where legally permitted, we seek to limit disclosures to the minimum information necessary and take reasonable measures to protect user privacy, security, and rights.
                        </p>
                        <p className="mb-3">As a company incorporated in India, we may be subject to lawful government requests under the IT Act, 2000 (Sections 69, 69A, 69B), the DPDP Act, 2023 (Chapter VII), the Code of Criminal Procedure, 1973 (Section 91), and orders from competent Indian courts. Regardless of jurisdiction, when we receive any such request, we:</p>
                        <ul className="list-disc pl-5 space-y-2 mb-4">
                            <li>Verify the request is legally valid, formally issued, and carries proper statutory authority</li>
                            <li>Require formal written submission with the specific legal instrument and statutory citation</li>
                            <li>Review the scope and <strong>challenge requests that are overbroad, legally deficient, or disproportionate</strong></li>
                            <li>Disclose <strong>only the minimum data categories strictly necessary</strong> to comply with the specific obligation</li>
                            <li>Log every request in our transparency register regardless of outcome</li>
                            <li>Notify affected users <strong>prior to or promptly after compliance, where legally permitted</strong></li>
                        </ul>
                        <p className="mb-2">Unauthorized requests and informal requests lacking legal process are rejected.</p>
                        <p className="text-sm">Annual aggregated transparency statistics: <a href="/api/v1/legal/government-requests/transparency-report" className="underline">Transparency Report</a>. Law enforcement must submit formal requests to <a href="mailto:legal@upkilo.com" className="underline">legal@upkilo.com</a>.</p>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">13. Grievance Redressal &amp; Regulatory Complaints</h2>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Grievance Officer (DPDP Act / IT Act):</strong> <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a> — acknowledged within 24 hours, resolved within 30 days</li>
                            <li><strong>Data Protection Board of India:</strong> you may approach the Board once constituted if your grievance is not resolved</li>
                            <li><strong>EU/EEA users:</strong> may also contact their national supervisory authority — see <a href="https://edpb.europa.eu" className="underline" rel="noopener noreferrer">edpb.europa.eu</a></li>
                            <li><strong>UK users:</strong> may contact the ICO at <a href="https://ico.org.uk" className="underline" rel="noopener noreferrer">ico.org.uk</a></li>
                        </ul>
                    </section>

                    <section>
                        <h2 className="text-2xl font-semibold text-slate-900 dark:text-white mb-4">14. Contact Us</h2>
                        <ul className="list-disc pl-5 space-y-2">
                            <li><strong>Company:</strong> Upkilo Technologies Private Limited, India</li>
                            <li><strong>Grievance Officer:</strong> <a href="mailto:grievance@upkilo.com" className="underline">grievance@upkilo.com</a></li>
                            <li><strong>Privacy inquiries:</strong> <a href="mailto:privacy@upkilo.com" className="underline">privacy@upkilo.com</a></li>
                            <li><strong>Legal / law enforcement requests:</strong> <a href="mailto:legal@upkilo.com" className="underline">legal@upkilo.com</a></li>
                        </ul>
                    </section>

                </div>
            </div>
        </div>
    );
}
