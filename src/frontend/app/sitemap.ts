import type { MetadataRoute } from 'next';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';
const API_URL  = process.env.NEXT_PUBLIC_API_URL  || 'http://localhost:5000';

// Fetch every active tenant slug from the backend
async function getAllTenantSlugs(): Promise<string[]> {
  try {
    const res = await fetch(`${API_URL}/api/seo/slugs`, {
      next: { revalidate: 3600 },
    });
    if (!res.ok) return [];
    const data = await res.json();
    return Array.isArray(data.slugs) ? data.slugs : [];
  } catch {
    return [];
  }
}

// Discovery URLs come from the backend, which derives them from tenants that actually
// exist (GET /api/v1/discovery/sitemap → one entry per real active tenant's
// category × city, de-duplicated).
//
// This replaces a hardcoded 15-category × 15-city cross-product that emitted all 225
// combinations unconditionally, whether or not a single business had listed in any of
// them. Submitting 225 empty auto-generated pages is the scaled-content pattern Google's
// spam policy penalises at the domain level — see the matching noindex gate in
// app/book/[category]/[city]/page.tsx, which this keeps in agreement: the sitemap now
// advertises exactly the pages that will actually allow indexing.
//
// Fails closed. If the backend is unreachable, no discovery URLs are emitted at all,
// which is the safe direction — a missing sitemap entry costs some crawl discovery
// latency, whereas a wrongly-present one costs domain trust.
async function getDiscoveryUrls(): Promise<string[]> {
  try {
    const res = await fetch(`${API_URL}/api/v1/discovery/sitemap`, {
      next: { revalidate: 3600 },
    });
    if (!res.ok) return [];
    const data = await res.json();
    if (!Array.isArray(data.entries)) return [];
    return data.entries
      .map((e: { url?: string }) => e.url)
      .filter((u: unknown): u is string => typeof u === 'string' && u.startsWith('/book/'));
  } catch {
    return [];
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const locales = ['en'];          // extend when more locales go live
  const [slugs, discoveryUrls] = await Promise.all([
    getAllTenantSlugs(),
    getDiscoveryUrls(),
  ]);

  // Static public pages.
  //
  // These URLs must be the FINAL destination, not a URL that redirects to it — a sitemap
  // entry that 3xx's wastes crawl budget and delays indexing of the page it points at.
  //
  // Every one of these except /enterprise and /discover previously omitted the locale
  // prefix. navigation.ts sets localePrefix: 'always' and these routes only exist on disk
  // under app/[locale]/..., so /pricing, /features, /marketplace, /medical-spa,
  // /terms-of-service, /privacy-policy, /cookie-policy and the bare root all redirected
  // to their /en/... equivalent — 8 of 10 entries.
  //
  // /enterprise and /discover are genuinely bare: they live at app/enterprise and
  // app/discover, and appear in middleware.ts's NON_LOCALE_SEGMENTS. They are correct
  // without a prefix and must NOT gain one.
  const localedPages: Array<[string, number, 'weekly' | 'monthly' | 'yearly']> = [
    ['',                    1.0, 'weekly'],   // the /en landing page
    ['/pricing',            0.9, 'monthly'],
    ['/features',           0.9, 'monthly'],
    ['/marketplace',        0.7, 'weekly'],
    ['/medical-spa',        0.8, 'monthly'],
    ['/terms-of-service',   0.3, 'yearly'],
    ['/privacy-policy',     0.3, 'yearly'],
    ['/cookie-policy',      0.3, 'yearly'],
  ];

  const staticPages: MetadataRoute.Sitemap = [
    ...locales.flatMap((locale) =>
      localedPages.map(([path, priority, changeFrequency]) => ({
        url: `${SITE_URL}/${locale}${path}`,
        priority,
        changeFrequency,
      }))
    ),
    // Genuinely locale-free routes.
    { url: `${SITE_URL}/enterprise`,           priority: 0.7, changeFrequency: 'monthly' },
    { url: `${SITE_URL}/discover`,             priority: 0.7, changeFrequency: 'daily' },
  ];

  // One sitemap entry per tenant × locale
  const bookingPages: MetadataRoute.Sitemap = slugs.flatMap((slug) =>
    locales.map((locale) => ({
      url: `${SITE_URL}/${locale}/book/${slug}`,
      lastModified: new Date(),
      changeFrequency: 'daily' as const,
      priority: 0.8,
    }))
  );

  // Programmatic SEO: category × city discovery pages, only where real listings exist.
  const discoveryPages: MetadataRoute.Sitemap = discoveryUrls.map((path) => ({
    url: `${SITE_URL}${path}`,
    changeFrequency: 'weekly' as const,
    priority: 0.7,
  }));

  return [...staticPages, ...bookingPages, ...discoveryPages];
}
