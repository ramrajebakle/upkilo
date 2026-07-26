import { auth } from "@/auth";
import { NextResponse } from "next/server";

const SUPPORTED_LOCALES = ['en','hi','es','fr','de','ar','ja','pt','it','ru','nl','tr','zh','ko','he'];

const PUBLIC_SEGMENTS = ['login', 'register', 'reset-password', 'verify-email', 'invite'];

// Top-level app segments that intentionally have no locale prefix
const NON_LOCALE_SEGMENTS = new Set(['book', 'discover', 'powered-by', 'enterprise', 'offline', 'test', 'au', 'ca', 'uk', 'uae']);

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
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|icons|screenshots|sw.js|manifest.json).*)'],
};
