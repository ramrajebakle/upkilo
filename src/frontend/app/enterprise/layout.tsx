import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import '../globals.css';

// Metadata lives in the layout rather than the page because app/enterprise/page.tsx is a
// genuine Client Component — it holds the enterprise lead form's useState — and a Client
// Component cannot export metadata. A layout can, even when its page cannot, so this is the
// low-risk fix for what was a page shipping with no <title> or <meta description> at all.
//
// Unlike the pages nested under app/[locale], this route has its own root layout and so had
// no parent metadata to inherit either — the tab title fell back to the bare URL.
//
// A fuller fix would extract the form into its own client component and make this page a
// Server Component, which would also let the static hero and feature sections stream as
// server-rendered HTML. That is a real improvement but a larger change; it is not required
// to give the page correct metadata, which is what actually blocked it from being indexed
// meaningfully.
export const metadata: Metadata = {
  title: 'Upkilo Enterprise — Multi-Location Booking & CRM for Chains',
  description: 'Enterprise booking, CRM and payments for multi-location chains and franchise groups. SSO/SAML, agency sub-accounts, unlimited staff and locations.',
  alternates: { canonical: 'https://upkilo.com/enterprise' },
  openGraph: {
    title: 'Upkilo Enterprise — Booking & CRM for Multi-Location Chains',
    description: 'SSO/SAML, agency sub-accounts and unlimited locations for franchise groups and chains.',
    type: 'website',
  },
};

export default function EnterpriseLayout({ children }: { children: ReactNode }) {
    return (
        <html lang="en">
            <body>{children}</body>
        </html>
    );
}
