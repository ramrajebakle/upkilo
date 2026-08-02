import type { MetadataRoute } from 'next';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';
const API_URL  = process.env.NEXT_PUBLIC_API_URL  || 'http://localhost:5000';

const DISCOVERY_CATEGORIES = [
  "hair-salon", "nail-salon", "spa", "massage", "barbershop",
  "beauty-salon", "med-spa", "eyebrow-threading", "lash-extensions", "waxing",
  "physiotherapy", "personal-training", "yoga", "pilates", "tattoo",
];

const DISCOVERY_CITIES = [
  "london", "new-york", "dubai", "sydney", "toronto",
  "melbourne", "los-angeles", "manchester", "birmingham", "chicago",
  "singapore", "delhi", "mumbai", "auckland", "edinburgh",
];

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

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const locales = ['en'];          // extend when more locales go live
  const slugs   = await getAllTenantSlugs();

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

  // Programmatic SEO: category × city discovery pages
  const discoveryPages: MetadataRoute.Sitemap = DISCOVERY_CATEGORIES.flatMap((cat) =>
    DISCOVERY_CITIES.map((city) => ({
      url: `${SITE_URL}/book/${cat}/${city}`,
      changeFrequency: 'weekly' as const,
      priority: 0.7,
    }))
  );

  return [...staticPages, ...bookingPages, ...discoveryPages];
}
