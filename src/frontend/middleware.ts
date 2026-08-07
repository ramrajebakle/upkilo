import { auth } from "@/auth";
import { NextResponse, type NextRequest } from "next/server";

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
  '', 'pricing', 'features', 'marketplace', 'medical-spa', 'docs',
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
  const publicPrefixes = ['/', '/pricing', '/features', '/marketplace', '/docs', '/cookie-policy', '/privacy-policy', '/terms-of-service'];
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
  return `/${locale}/dashboard`;
}

// ── Host routing, before any auth logic ──────────────────────────────────
// Deliberately a PLAIN function, never passed through auth() — see the comment on
// `middleware` below for why that distinction is the actual fix here, not a style choice.
// Only applies to real upkilo.com traffic. localhost, *.azurewebsites.net and staging
// return null and fall straight through to auth(), so dev and staging behave exactly as
// before.
function hostRoutingResponse(req: NextRequest): NextResponse | null {
  const { nextUrl } = req;
  const pathname = nextUrl.pathname;
  const host = (req.headers.get('host') ?? '').toLowerCase().split(':')[0];

  if (host !== 'upkilo.com' && host !== 'www.upkilo.com' && host !== 'app.upkilo.com') {
    return null;
  }

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

  // Marketing content correctly requested on the apex: !onAppHost && marketing. Handled
  // fully here — including the same locale auto-prefix / bare-root logic auth()'s
  // callback applies below — because a marketing page has no session to check, and
  // routing it through auth() at all is exactly what caused the loop this function
  // exists to avoid.
  //
  // Explicitly gated (not "else falls through"): the only other way to reach this point
  // is onAppHost && !marketing — dashboard/portal/auth traffic correctly on
  // app.upkilo.com — which MUST fall through to auth() unchanged, since that is where
  // req.auth gets populated and the login-redirect check happens. An early version of
  // this function ran the block below unconditionally after the two redirects above,
  // which would have skipped auth() — and therefore the login check — for every
  // dashboard request. Caught before this was ever deployed, but the guard stays
  // explicit so it cannot regress silently.
  if (!onAppHost && marketing) {
    const firstSegment = pathname.split('/')[1];
    if (pathname !== '/' && !SUPPORTED_LOCALES.includes(firstSegment) && !NON_LOCALE_SEGMENTS.has(firstSegment)) {
      return NextResponse.redirect(new URL(`/en${pathname}${nextUrl.search}`, nextUrl));
    }
    if (pathname === '/') {
      return NextResponse.redirect(new URL('/en', nextUrl));
    }

    // Consolidate non-English marketing URLs onto English until real translations exist.
    //
    // The marketing pages (landing, features, pricing, marketplace, medical-spa) call
    // useTranslations nowhere — verified by grep, zero hits across all five. They render
    // byte-identical hardcoded English JSX no matter which locale prefix is requested, so
    // /de/features and /fr/features are not "thin" translations, they are exact duplicates
    // of /en/features. Fifteen URLs, one page's worth of content.
    //
    // There is also no language switcher anywhere on the marketing surface (also verified),
    // so nothing today sends a real visitor to a non-English marketing URL — this closes the
    // duplicate-content exposure before a crawler finds it by guessing, at zero UX cost.
    //
    // Scoped deliberately: only marketing paths on the apex reach this branch. Dashboard,
    // portal and auth routes on app.upkilo.com are untouched, and DO have real partial
    // translations in messages/*.json plus a session-based locale — they must keep working
    // in all 15 locales.
    //
    // Remove this per-locale, not wholesale, as each locale passes the content gate: wire
    // the marketing pages to useTranslations, get the copy genuinely translated and
    // reviewed, then drop that locale from the redirect and add it to sitemap.ts's
    // `locales` array and hreflang in the same change. Doing any one of those without the
    // others produces either duplicate content or orphaned URLs.
    const requestedLocale = extractLocale(pathname);
    if (requestedLocale !== 'en' && SUPPORTED_LOCALES.includes(firstSegment)) {
      const englishPath = `/en${pathname.slice(`/${firstSegment}`.length)}`;
      return NextResponse.redirect(new URL(`${englishPath}${nextUrl.search}`, SITE_URL), 308);
    }

    const requestHeaders = new Headers(req.headers);
    requestHeaders.set('x-next-intl-locale', extractLocale(pathname));
    return NextResponse.next({ request: { headers: requestHeaders } });
  }

  // onAppHost && !marketing: dashboard/portal/auth traffic on app.upkilo.com.
  // Fall through to auth() below, exactly as before this function existed.
  return null;
}

const authMiddleware = auth((req) => {
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

    if ((role === 'tenant_owner' || role === 'team_member') && isPlatformPath) {
      return Response.redirect(new URL(`/${locale}/dashboard`, nextUrl));
    }

    // The mirrored guard that used to bounce platform staff off `/tenant` paths is gone
    // with those routes. It was matching on `pathname.includes('/tenant')`, which also
    // matched `/platform/tenants` — so platform owners were redirected away from their
    // own tenant list and could never open it.

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

// Split from a single `export default auth((req) => {...})` into two layers. The reason
// is not style: Auth.js's `auth()` wrapper reads NEXTAUTH_URL — set to
// https://app.upkilo.com in deploy.yml, since that is genuinely the correct host for
// login/session/CSRF cookies to be scoped to — and, independently of anything in the
// callback it wraps, redirects any request whose Host header does not match that
// configured URL to app.upkilo.com. That happens BEFORE the marketing-vs-app routing
// logic above ever ran, which is what actually caused https://upkilo.com/ (and the www
// variant) to bounce to https://app.upkilo.com/en — which itself immediately redirects
// marketing paths straight back to the apex. An infinite loop between the two hosts, and
// the reason Razorpay's website verifier reported upkilo.com unreachable.
//
// The fix is not to change NEXTAUTH_URL — that would risk reintroducing the login bug it
// was added to fix. It is to make sure marketing-on-apex requests never reach auth() at
// all, since a marketing page has no session to check in the first place.
export default function middleware(req: NextRequest, event: unknown) {
  const hostResponse = hostRoutingResponse(req);
  if (hostResponse) return hostResponse;
  return (authMiddleware as unknown as (req: NextRequest, event: unknown) => unknown)(req, event);
}

export const config = {
  // `.well-known` MUST be excluded. Apple does not follow redirects when fetching
  // apple-app-site-association, and Google requires assetlinks.json to be served
  // directly — if host routing 308s these to another origin, universal links,
  // App Links and password autofill all silently stop working.
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|icons|screenshots|sw.js|manifest.json|\\.well-known).*)'],
};
