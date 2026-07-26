import type { MetadataRoute } from 'next';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        // Allow all crawlers on public pages
        userAgent: '*',
        allow: [
          '/',
          '/en/book/',
          '/pricing',
          '/marketplace',
          '/terms-of-service',
          '/privacy-policy',
          '/cookie-policy',
          '/docs',
        ],
        // Block all private dashboard & portal routes
        disallow: [
          '/en/dashboard/',
          '/en/clients/',
          '/en/bookings/',
          '/en/staff/',
          '/en/settings/',
          '/en/analytics/',
          '/en/payments/',
          '/en/portal/',
          '/api/',
          '/_next/',
        ],
      },
    ],
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
