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

  // Static public pages
  const staticPages: MetadataRoute.Sitemap = [
    { url: SITE_URL,                           priority: 1.0, changeFrequency: 'weekly' },
    { url: `${SITE_URL}/pricing`,              priority: 0.9, changeFrequency: 'monthly' },
    { url: `${SITE_URL}/features`,             priority: 0.9, changeFrequency: 'monthly' },
    { url: `${SITE_URL}/marketplace`,          priority: 0.7, changeFrequency: 'weekly' },
    { url: `${SITE_URL}/medical-spa`,          priority: 0.8, changeFrequency: 'monthly' },
    { url: `${SITE_URL}/enterprise`,           priority: 0.7, changeFrequency: 'monthly' },
    { url: `${SITE_URL}/discover`,             priority: 0.7, changeFrequency: 'daily' },
    { url: `${SITE_URL}/terms-of-service`,     priority: 0.3, changeFrequency: 'yearly' },
    { url: `${SITE_URL}/privacy-policy`,       priority: 0.3, changeFrequency: 'yearly' },
    { url: `${SITE_URL}/cookie-policy`,        priority: 0.3, changeFrequency: 'yearly' },
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
