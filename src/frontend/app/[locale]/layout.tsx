import {NextIntlClientProvider} from 'next-intl';
import {getMessages} from 'next-intl/server';
import {ReactNode} from 'react';
import { Inter, Outfit, JetBrains_Mono } from 'next/font/google';
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

// Three faces, all self-hosted by next/font and exposed as CSS variables that globals.css
// consumes. Before this, only Inter was loaded here.
//
// Outfit was referenced 84 times across 58 files as the display face, but the only place it
// was ever loaded was an `@import url(fonts.googleapis.com/...)` inside a <style jsx global>
// block on one dashboard page. That had three consequences:
//
//   1. On the other 57 files it silently fell back to sans-serif, so headings rendered in a
//      face nobody chose. A missing font never errors — it just quietly looks wrong.
//   2. That same block set `body { font-family: 'Outfit' }` globally, so the whole app's body
//      text changed face depending on whether that one route happened to be mounted.
//   3. An @import inside a component is fetched late and render-blocking, and it defeats
//      everything next/font exists for: self-hosting, preloading, and a size-adjusted
//      fallback that prevents layout shift.
//
// JetBrains Mono had the same problem — named in --font-mono, loaded only in that one block.
const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
});

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap',
});

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

  // The three font variables MUST sit on <html>, not <body>.
  //
  // globals.css declares `--font-sans: var(--font-inter), ...` inside @theme, which Tailwind
  // emits at :root — i.e. on <html>. A custom property is resolved in the scope that declares
  // it, so a :root declaration cannot see a variable defined on <body>, its own descendant.
  // With the variables on <body>, --font-sans computed to the empty string, the .font-sans
  // utility emitted nothing, and every element on every page fell back to Times New Roman.
  // Putting them on <html> puts --font-inter in the same scope that reads it.
  return (
    <html
      lang={locale}
      dir={isRtl ? 'rtl' : 'ltr'}
      className={`${inter.variable} ${outfit.variable} ${jetbrainsMono.variable}`}
      suppressHydrationWarning
    >
      {/* Variables, not inter.className. The className hardcodes Inter onto body and leaves
          the other two faces unreachable; exposing all three as variables lets globals.css
          decide which face body text, display headings and code each use. */}
      <body className="font-sans" suppressHydrationWarning>
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
