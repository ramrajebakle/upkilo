import type { MetadataRoute } from 'next';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

// Every locale this app routes, mirroring SUPPORTED_LOCALES in middleware.ts. The private
// path list below was previously hardcoded to `/en/...` only, so the same dashboard routes
// under the other 14 prefixes (/hi/dashboard/, /es/settings/, /fr/payments/ …) matched no
// disallow rule at all.
//
// Worth stating what this is and is not: the real protection for dashboard content is the
// auth wall — app.upkilo.com redirects anonymous requests to /login regardless of robots.txt,
// and robots.txt is a crawler convention, not an access control. This is defense in depth
// for archival and aggregator crawlers that handle auth redirects poorly, and it costs
// nothing to make complete.
const LOCALES = ['en', 'hi', 'es', 'fr', 'de', 'ar', 'ja', 'pt', 'it', 'ru', 'nl', 'tr', 'zh', 'ko', 'he'];

// Route prefixes that require a session. Expanded across every locale below.
const PRIVATE_SEGMENTS = [
  'dashboard', 'clients', 'bookings', 'staff', 'settings',
  'analytics', 'payments', 'portal', 'platform', 'admin',
];

const PUBLIC_PATHS = [
  '/',
  '/en/',
  '/enterprise',
  '/discover',
  '/book/',
];

export default function robots(): MetadataRoute.Robots {
  const privatePaths = LOCALES.flatMap((locale) =>
    PRIVATE_SEGMENTS.map((segment) => `/${locale}/${segment}/`)
  );

  const disallow = [...privatePaths, '/api/', '/_next/'];

  return {
    rules: [
      {
        userAgent: '*',
        allow: PUBLIC_PATHS,
        disallow,
      },

      // AI crawlers are named explicitly rather than left to the wildcard above.
      //
      // These are the crawlers that can cite a source in an answer — appearing in them is
      // the entire point of optimizing for answer engines, so they get the same access as
      // any search crawler. Naming them makes that an auditable decision rather than an
      // accident of the wildcard, and gives one obvious place to change course.
      //
      // Note this only governs crawling. It cannot compel attribution, and these operators
      // are free to change their own behavior at any time.
      {
        userAgent: [
          'GPTBot',           // OpenAI
          'OAI-SearchBot',    // OpenAI, search/citation surface specifically
          'ChatGPT-User',     // OpenAI, user-initiated fetch
          'ClaudeBot',        // Anthropic
          'Claude-User',      // Anthropic, user-initiated fetch
          'PerplexityBot',    // Perplexity
          'Perplexity-User',  // Perplexity, user-initiated fetch
          'Google-Extended',  // Google, Gemini/AI-Overview grounding
          'Applebot-Extended',// Apple Intelligence
        ],
        allow: PUBLIC_PATHS,
        disallow,
      },
    ],
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
