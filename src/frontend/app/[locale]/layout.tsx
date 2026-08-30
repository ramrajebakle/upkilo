import {NextIntlClientProvider} from 'next-intl';
import {getMessages} from 'next-intl/server';
import {ReactNode} from 'react';
import { ToastProvider } from '@/components/ui/Toast';
import { QueryProvider } from '@/components/providers/QueryProvider';
import { AuthProvider } from '@/components/providers/AuthProvider';
import { AuthBridge } from '@/components/providers/AuthBridge';
import { AnimationProvider } from '@/components/providers/AnimationProvider';
import { ShortcutManager } from '@/components/layout/ShortcutManager';
import { ClientSideProvider } from '@/components/layout/ClientSideProvider';
import { RootHtml } from '@/components/layout/RootHtml';
import { ThemedToaster } from '@/components/ThemedToaster';
import { CookieConsent } from '@/components/CookieConsent';
import { SessionExpiryWarning } from '@/components/SessionExpiryWarning';
import { MilestoneCelebration } from '@/components/MilestoneCelebration';
import { OfflineIndicator } from '@/components/OfflineIndicator';
import { LiveChatWidget } from '@/components/LiveChatWidget';
import '../globals.css';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

export const metadata = {
    title: 'Upkilo - Scale Without Limits',
    description: 'AI-Powered Booking & CRM Platform for Growing Businesses',
    manifest: '/manifest.json',
    appleWebApp: {
        capable: true,
        statusBarStyle: 'default',
        title: 'Upkilo',
    },
    formatDetection: { telephone: false },
    icons: {
        icon: '/icons/icon-192x192.png',
        apple: [
            { url: '/icons/icon-192x192.png', sizes: '192x192', type: 'image/png' },
        ],
    },
    openGraph: {
        title: 'Upkilo — Scale Without Limits',
        description: 'AI-Powered Booking & CRM Platform for Growing Businesses',
        images: [{ url: '/icons/icon-512x512.png', width: 512, height: 512 }],
    },
    other: {
        'mobile-web-app-capable': 'yes',
    },
};

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const messages = await getMessages();
  const isRtl = locale === 'ar' || locale === 'he' || locale === 'fa';

  // <html>, <body>, the font variables, the pre-paint theme script and ThemeProvider all
  // live in RootHtml now, shared with the nine other root layouts in this app that used to
  // get none of them. What stays here is only what is genuinely locale-scoped.
  return (
    <RootHtml lang={locale} dir={isRtl ? 'rtl' : 'ltr'}>
      {/* Scroll-reveal wrappers are server-rendered with an inline opacity:0 — Framer
          Motion serialises the `initial` prop — and only JavaScript animates them back.
          Without scripting the marketing page renders completely blank, headline included.
          This restores every wrapper for that case; it costs nothing when scripting is on,
          since <noscript> content is inert then. */}
      <noscript>
        <style>{`.reveal { opacity: 1 !important; transform: none !important; }`}</style>
      </noscript>
      <NextIntlClientProvider messages={messages} locale={locale}>
        <AuthProvider>
          <AuthBridge />
          <QueryProvider>
            <ToastProvider>
              <ShortcutManager />
              <AnimationProvider>
                <main className={isRtl ? 'rtl' : 'ltr'}>
                  {children}
                </main>
              </AnimationProvider>
              <ClientSideProvider>
                <SessionExpiryWarning />
                <CookieConsent />
                <MilestoneCelebration />
                <OfflineIndicator />
                <LiveChatWidget />
              </ClientSideProvider>
              <ThemedToaster />
            </ToastProvider>
          </QueryProvider>
        </AuthProvider>
      </NextIntlClientProvider>
    </RootHtml>
  );
}
