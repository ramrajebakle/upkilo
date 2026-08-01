import { auth } from "@/auth";
import { NextResponse } from "next/server";

const SUPPORTED_LOCALES = ['en','hi','es','fr','de','ar','ja','pt','it','ru','nl','tr','zh','ko','he'];

const PUBLIC_SEGMENTS = ['login', 'register', 'reset-password', 'verify-email', 'invite'];

// Top-level app segments that intentionally have no locale prefix
const NON_LOCALE_SEGMENTS = new Set(['book', 'discover', 'powered-by', 'enterprise', 'offline', 'test', 'au', 'ca', 'uk', 'uae']);

// ── Domain split ────────────────────────────────────────────────────────────
// Marketing + public SEO pages live on the apex (upkilo.com); the dashboard, portal
// and auth flows live on app.upkilo.com. Marketing is ALLOWLISTED rather than the
// dashboard being denylisted: there are ~47 dashboard segments and only a handful of
// marketing routes, so adding a dashboard route must not require editing this file.
const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';
const APP_URL = process.env.NEXT_PUBLIC_APP_URL || 'https://app.upkilo.com';

// Locale-prefixed marketing paths, i.e. /en/pricing. '' covers the /en landing page.
const MARKETING_LOCALE_SEGMENTS = new Set([
  '', 'pricing', 'marketplace', 'medical-spa', 'docs',
  'terms-of-service', 'privacy-policy', 'cookie-policy', 'book',
]);

// Marketing paths that carry no locale prefix, i.e. /discover, /uk
const MARKETING_ROOT_SEGMENTS = new Set([
  'book', 'discover', 'powered-by', 'enterprise', 'au', 'ca', 'uk', 'uae',
]);

function isMarketingPath(pathname: string): boolean {
  if (pathname === '/') return true;

  const segments = pathname.split('/').filter(Boolean);
  if (segments.length === 0) return true;

  // Strip a leading locale so /en/pricing and /pricing are treated alike.
  const rest = SUPPORTED_LOCALES.includes(segments[0]) ? segments.slice(1) : segments;
  if (rest.length === 0) return true;               // bare /en → landing page

  return MARKETING_LOCALE_SEGMENTS.has(rest[0]) || MARKETING_ROOT_SEGMENTS.has(rest[0]);
}

function extractLocale(pathname: string): string {
  const segment = pathname.split('/')[1];
  return SUPPORTED_LOCALES.includes(segment) ? segment : 'en';
}

function isPublicPath(pathname: string): boolean {
  // Allow all locale-prefixed auth pages and bare paths
  for (const locale of SUPPORTED_LOCALES) {
    for (const segment of PUBLIC_SEGMENTS) {
      if (pathname === `/${locale}/${segment}` || pathname.startsWith(`/${locale}/${segment}/`)) {
        return true;
      }
    }
  }
  // Also allow the root landing page and public marketing pages
  const publicPrefixes = ['/', '/pricing', '/marketplace', '/docs', '/cookie-policy', '/privacy-policy', '/terms-of-service'];
  for (const locale of SUPPORTED_LOCALES) {
    for (const prefix of publicPrefixes) {
      if (pathname === `/${locale}${prefix === '/' ? '' : prefix}` || pathname === `/${locale}`) {
        return true;
      }
    }
  }
  // Allow public booking widget, discover, and powered-by pages.
  // Use startsWith (not includes) to prevent path-traversal bypasses such as
  // /dashboard/book/anything or /admin?redirect=/discover matching as public routes.
  const publicRoutePrefixes = ['/book/', '/discover', '/powered-by'];
  const locale = extractLocale(pathname);
  if (publicRoutePrefixes.some(prefix => pathname.startsWith(`/${locale}${prefix}`) || pathname.startsWith(prefix))) {
    return true;
  }
  return false;
}

function roleDefaultRoute(role: string | undefined, locale: string): string {
  if (role?.startsWith('platform')) return `/${locale}/platform/command`;
  return `/${locale}/tenant/command`;
}

export default auth((req) => {
  const { nextUrl } = req;
  const pathname = nextUrl.pathname;

  // ── Host routing, before any auth logic ──────────────────────────────────
  // Only applies to real upkilo.com traffic. localhost, *.azurewebsites.net and
  // staging fall straight through, so dev and staging behave exactly as before.
  const host = (req.headers.get('host') ?? '').toLowerCase().split(':')[0];
  if (host === 'upkilo.com' || host === 'www.upkilo.com' || host === 'app.upkilo.com') {
    // Canonicalise www → apex so SEO does not split across two hostnames.
    if (host === 'www.upkilo.com') {
      return NextResponse.redirect(new URL(`${pathname}${nextUrl.search}`, SITE_URL), 308);
    }

    const onAppHost = host === 'app.upkilo.com';
    const marketing = isMarketingPath(pathname);

    // Dashboard/portal/auth requested on the apex → send to the app subdomain.
    if (!onAppHost && !marketing) {
      return NextResponse.redirect(new URL(`${pathname}${nextUrl.search}`, APP_URL), 308);
    }
    // Marketing requested on the app subdomain → send to the apex, which is the
    // canonical host in sitemap.ts and robots.ts.
    if (onAppHost && marketing) {
      return NextResponse.redirect(new URL(`${pathname}${nextUrl.search}`, SITE_URL), 308);
    }
  }

  // Auto-prefix locale: if the first segment is neither a supported locale nor a
  // known locale-free route, prepend /en/ so /tenant/command → /en/tenant/command.
  const firstSegment = pathname.split('/')[1];
  if (pathname !== '/' && !SUPPORTED_LOCALES.includes(firstSegment) && !NON_LOCALE_SEGMENTS.has(firstSegment)) {
    return NextResponse.redirect(new URL(`/en${pathname}${nextUrl.search}`, nextUrl));
  }
  // Root / → /en
  if (pathname === '/') {
    return NextResponse.redirect(new URL('/en', nextUrl));
  }

  const locale = extractLocale(pathname);
  const isLoggedIn = !!req.auth;

  if (!isLoggedIn && !isPublicPath(pathname)) {
    return Response.redirect(new URL(`/${locale}/login`, nextUrl));
  }

  if (isLoggedIn) {
    const role = req.auth?.user?.role as string | undefined;
    const isPlatformPath = pathname.includes('/platform');
    const isTenantPath = pathname.includes('/tenant');

    if ((role === 'tenant_owner' || role === 'team_member') && isPlatformPath) {
      return Response.redirect(new URL(`/${locale}/tenant/command`, nextUrl));
    }

    if ((role === 'platform_owner' || role === 'platform_admin') && isTenantPath) {
      return Response.redirect(new URL(`/${locale}/platform/command`, nextUrl));
    }

    // Redirect away from auth pages when already logged in
    if (isPublicPath(pathname) && PUBLIC_SEGMENTS.some(s => pathname.includes(`/${s}`))) {
      return Response.redirect(new URL(roleDefaultRoute(role, locale), nextUrl));
    }
  }

  // Tell next-intl which locale to use — getRequestConfig reads this via requestLocale.
  // Without this header, requestLocale is undefined and the i18n config defaults to 'en'
  // while logging "LOCALE NOT FOUND IN LIST!" noise on every request.
  const requestHeaders = new Headers(req.headers);
  requestHeaders.set('x-next-intl-locale', locale);
  return NextResponse.next({ request: { headers: requestHeaders } });
});

export const config = {
  // `.well-known` MUST be excluded. Apple does not follow redirects when fetching
  // apple-app-site-association, and Google requires assetlinks.json to be served
  // directly — if host routing 308s these to another origin, universal links,
  // App Links and password autofill all silently stop working.
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|icons|screenshots|sw.js|manifest.json|\\.well-known).*)'],
};
