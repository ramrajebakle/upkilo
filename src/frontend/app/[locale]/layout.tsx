import {NextIntlClientProvider} from 'next-intl';
import {getMessages} from 'next-intl/server';
import {ReactNode} from 'react';
import { Inter } from 'next/font/google';
import { ToastProvider } from '@/components/ui/Toast';
import { Toaster } from 'sonner';
import { ThemeProvider } from '@/components/ThemeProvider';
import { QueryProvider } from '@/components/providers/QueryProvider';
import { AuthProvider } from '@/components/providers/AuthProvider';
import { AuthBridge } from '@/components/providers/AuthBridge';
import { AnimationProvider } from '@/components/providers/AnimationProvider';
import { ShortcutManager } from '@/components/layout/ShortcutManager';
import { ClientSideProvider } from '@/components/layout/ClientSideProvider';
import { CookieConsent } from '@/components/CookieConsent';
import { SessionExpiryWarning } from '@/components/SessionExpiryWarning';
import { MilestoneCelebration } from '@/components/MilestoneCelebration';
import { OfflineIndicator } from '@/components/OfflineIndicator';
import { LiveChatWidget } from '@/components/LiveChatWidget';
import '../globals.css';

const inter = Inter({ subsets: ['latin'] });

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

  return (
    <html lang={locale} dir={isRtl ? 'rtl' : 'ltr'} suppressHydrationWarning>
      <body className={inter.className} suppressHydrationWarning>
        <NextIntlClientProvider messages={messages} locale={locale}>
          <AuthProvider>
            <AuthBridge />
            <QueryProvider>
              <ThemeProvider>
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
                  <Toaster
                    position="bottom-right"
                    toastOptions={{
                      style: { fontFamily: 'Inter, system-ui, sans-serif' },
                    }}
                    richColors
                  />
                </ToastProvider>
              </ThemeProvider>
            </QueryProvider>
          </AuthProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
