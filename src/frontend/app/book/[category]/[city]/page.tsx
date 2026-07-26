import { Metadata } from 'next';

// Prevents </script> injection when data lands in JSON-LD blocks.
function safeJsonLd(obj: unknown): string {
  return JSON.stringify(obj).replace(/</g, '\\u003c');
}

interface Listing {
  id: string;
  name: string;
  tagline: string | null;
  averageRating: number;
  reviewCount: number;
  city: string;
  category: string;
}

interface PageData {
  listings: Listing[];
  total: number;
  page: number;
  pageSize: number;
  category: string;
  city: string;
  seo: {
    title: string;
    description: string;
    canonicalUrl: string;
  };
}

interface Props {
  params: Promise<{ category: string; city: string }>;
  searchParams: Promise<{ page?: string }>;
}

async function fetchListings(category: string, city: string, page = 1): Promise<PageData | null> {
  const apiBase = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
  try {
    const res = await fetch(
      `${apiBase}/api/v1/discovery/${encodeURIComponent(category)}/${encodeURIComponent(city)}?page=${page}&pageSize=20`,
      { next: { revalidate: 3600 } }
    );
    if (!res.ok) return null;
    const json = await res.json();
    return json.data ?? null;
  } catch {
    return null;
  }
}

// Pre-render the highest-traffic category × city combinations at build time.
// All other combinations are served via ISR (revalidate: 3600 set in fetchListings).
export async function generateStaticParams() {
  const categories = [
    "hair-salon", "nail-salon", "spa", "massage", "barbershop",
    "beauty-salon", "med-spa", "eyebrow-threading", "lash-extensions", "waxing",
  ];
  const cities = [
    "london", "new-york", "dubai", "sydney", "toronto",
    "melbourne", "los-angeles", "manchester", "birmingham", "chicago",
  ];

  return categories.flatMap((category) =>
    cities.map((city) => ({ category, city }))
  );
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { category, city } = await params;
  const data = await fetchListings(category, city);
  if (!data) {
    return { title: `Book ${category} in ${city} | Upkilo` };
  }
  return {
    title: data.seo.title,
    description: data.seo.description,
    alternates: { canonical: data.seo.canonicalUrl },
    openGraph: {
      title: data.seo.title,
      description: data.seo.description,
      type: 'website',
    },
  };
}

export default async function DiscoveryPage({ params, searchParams }: Props) {
  const { category, city } = await params;
  const { page: pageStr } = await searchParams;
  const page = parseInt(pageStr ?? '1', 10);
  const data = await fetchListings(category, city, page);

  const humanCategory = category.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
  const humanCity = city.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
  const appBase = process.env.NEXT_PUBLIC_APP_URL ?? 'https://upkilo.com';

  // JSON-LD: ItemList for the directory page + BreadcrumbList for rich snippets
  const jsonLdItemList = {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: `${humanCategory} in ${humanCity}`,
    description: `Directory of ${humanCategory.toLowerCase()} businesses available for online booking in ${humanCity}.`,
    numberOfItems: data?.total ?? 0,
    itemListElement: (data?.listings ?? []).map((listing, idx) => ({
      '@type': 'ListItem',
      position: idx + 1,
      name: listing.name,
      url: `${appBase}/book/${listing.id}`,
    })),
  };

  const jsonLdBreadcrumb = {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: [
      { '@type': 'ListItem', position: 1, name: 'Home', item: appBase },
      { '@type': 'ListItem', position: 2, name: 'Discover', item: `${appBase}/discover` },
      { '@type': 'ListItem', position: 3, name: humanCategory, item: `${appBase}/book/${category}` },
      { '@type': 'ListItem', position: 4, name: humanCity },
    ],
  };

  return (
    <main className="min-h-screen bg-gray-50">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(jsonLdItemList) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: safeJsonLd(jsonLdBreadcrumb) }} />
      {/* Hero */}
      <section className="bg-gradient-to-br from-indigo-600 to-violet-700 text-white py-16 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <h1 className="text-3xl md:text-4xl font-bold mb-3">
            Best {humanCategory} in {humanCity}
          </h1>
          <p className="text-indigo-100 text-lg">
            {data ? `${data.total} businesses ready to book online` : `Discover and book local ${humanCategory.toLowerCase()} businesses`}
          </p>
        </div>
      </section>

      {/* Results */}
      <section className="max-w-4xl mx-auto py-10 px-4">
        {!data || data.listings.length === 0 ? (
          <div className="text-center py-16">
            <div className="text-5xl mb-4">🔍</div>
            <h2 className="text-xl font-semibold text-gray-700 mb-2">No businesses found yet</h2>
            <p className="text-gray-500">
              Are you a {humanCategory.toLowerCase()} business in {humanCity}?{' '}
              <a href="/register" className="text-indigo-600 hover:underline font-medium">List your business for free →</a>
            </p>
          </div>
        ) : (
          <>
            <div className="grid gap-4 md:grid-cols-2">
              {data.listings.map(listing => (
                <article
                  key={listing.id}
                  className="bg-white rounded-2xl border border-gray-200 p-6 shadow-sm hover:shadow-md transition-shadow"
                >
                  <h2 className="text-lg font-bold text-gray-900 mb-1">{listing.name}</h2>
                  {listing.tagline && (
                    <p className="text-sm text-gray-500 mb-3">{listing.tagline}</p>
                  )}
                  {listing.reviewCount > 0 && (
                    <div className="flex items-center gap-1 mb-3">
                      <span className="text-yellow-400 text-sm">{'★'.repeat(Math.round(listing.averageRating))}</span>
                      <span className="text-sm text-gray-600">{listing.averageRating.toFixed(1)} ({listing.reviewCount} reviews)</span>
                    </div>
                  )}
                  <div className="flex items-center justify-between">
                    <span className="text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded-full font-medium">{listing.city}</span>
                    <a
                      href={`/book/${listing.id}`}
                      className="bg-indigo-600 text-white text-sm px-4 py-1.5 rounded-xl font-semibold hover:bg-indigo-700 transition-colors"
                    >
                      Book Now
                    </a>
                  </div>
                </article>
              ))}
            </div>

            {/* Pagination */}
            <div className="flex justify-center gap-3 mt-10">
              {page > 1 && (
                <a
                  href={`/book/${category}/${city}?page=${page - 1}`}
                  className="px-4 py-2 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-100"
                >
                  ← Previous
                </a>
              )}
              {data.listings.length === data.pageSize && (
                <a
                  href={`/book/${category}/${city}?page=${page + 1}`}
                  className="px-4 py-2 bg-indigo-600 text-white rounded-xl text-sm font-medium hover:bg-indigo-700"
                >
                  Next →
                </a>
              )}
            </div>
          </>
        )}

        {/* CTA for businesses */}
        <div className="mt-12 bg-indigo-50 border border-indigo-100 rounded-2xl p-8 text-center">
          <h3 className="text-lg font-bold text-indigo-900 mb-2">Own a {humanCategory} business in {humanCity}?</h3>
          <p className="text-indigo-700 text-sm mb-4">Join Upkilo to accept online bookings, automate reminders, and grow your client base.</p>
          <a href="/register" className="inline-block bg-indigo-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-indigo-700">
            List Your Business Free →
          </a>
        </div>
      </section>
    </main>
  );
}
