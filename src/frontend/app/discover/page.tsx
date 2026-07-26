'use client';

import { useState, useEffect } from 'react';

interface Business {
  id: string;
  name: string;
  slug: string;
  tagline: string | null;
  city: string | null;
  country: string | null;
  averageRating: number;
  reviewCount: number;
  industry: string;
  bookingUrl: string;
}

export default function DiscoverPage() {
  const [q, setQ] = useState('');
  const [city, setCity] = useState('');
  const [results, setResults] = useState<Business[]>([]);
  const [featured, setFeatured] = useState<Business[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);

  useEffect(() => {
    fetch('/api/v1/marketplace/featured')
      .then(r => r.json())
      .then(j => setFeatured(j.data || []));
  }, []);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setSearched(true);
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (city) params.set('city', city);
    const res = await fetch(`/api/v1/marketplace/search?${params}`);
    const json = await res.json();
    setResults(json.data?.results || []);
    setLoading(false);
  };

  const StarRating = ({ rating, count }: { rating: number; count: number }) => (
    <div className="flex items-center gap-1">
      <span className="text-yellow-400 text-sm">{'★'.repeat(Math.round(rating))}{'☆'.repeat(5 - Math.round(rating))}</span>
      <span className="text-sm text-gray-500">{rating.toFixed(1)} ({count})</span>
    </div>
  );

  const BusinessCard = ({ b }: { b: Business }) => (
    <div className="bg-white rounded-2xl border border-gray-200 p-6 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-bold text-gray-900 truncate">{b.name}</h3>
          {b.tagline && <p className="text-sm text-gray-500 mt-0.5 line-clamp-2">{b.tagline}</p>}
          {b.reviewCount > 0 && <div className="mt-2"><StarRating rating={b.averageRating} count={b.reviewCount} /></div>}
          {b.city && (
            <p className="text-xs text-gray-400 mt-2">📍 {b.city}{b.country ? `, ${b.country}` : ''}</p>
          )}
          {b.industry && (
            <span className="inline-block mt-2 text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded-full">{b.industry}</span>
          )}
        </div>
        <a
          href={b.bookingUrl}
          className="shrink-0 bg-indigo-600 text-white text-sm px-4 py-2 rounded-xl font-semibold hover:bg-indigo-700 transition-colors"
        >
          Book Now
        </a>
      </div>
    </div>
  );

  return (
    <main className="min-h-screen bg-gray-50">
      {/* Hero Search */}
      <section className="bg-gradient-to-br from-indigo-600 to-violet-700 text-white py-20 px-4">
        <div className="max-w-3xl mx-auto text-center">
          <h1 className="text-4xl font-bold mb-3">Find & Book Local Services</h1>
          <p className="text-indigo-100 mb-8">Salons, spas, gyms, therapists — book online in seconds.</p>

          <form onSubmit={handleSearch} className="bg-white rounded-2xl shadow-xl p-2 flex gap-2 flex-wrap md:flex-nowrap">
            <input
              value={q}
              onChange={e => setQ(e.target.value)}
              placeholder="Service (e.g. haircut, massage...)"
              className="flex-1 px-4 py-3 text-gray-900 rounded-xl text-sm outline-none min-w-0"
            />
            <input
              value={city}
              onChange={e => setCity(e.target.value)}
              placeholder="City"
              className="flex-1 px-4 py-3 text-gray-900 rounded-xl text-sm outline-none border-l border-gray-200 min-w-0"
            />
            <button
              type="submit"
              className="bg-indigo-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-indigo-500 shrink-0"
            >
              Search
            </button>
          </form>
        </div>
      </section>

      {/* Results or Featured */}
      <section className="max-w-5xl mx-auto py-12 px-4">
        {loading && (
          <div className="text-center py-16">
            <div className="animate-spin w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full mx-auto mb-3" />
            <p className="text-gray-500">Searching...</p>
          </div>
        )}

        {!loading && searched && results.length === 0 && (
          <div className="text-center py-16">
            <div className="text-5xl mb-3">🔍</div>
            <h2 className="text-lg font-semibold text-gray-700">No results found</h2>
            <p className="text-gray-500 mt-1">Try a different search or browse featured businesses below.</p>
          </div>
        )}

        {!loading && searched && results.length > 0 && (
          <>
            <h2 className="text-xl font-bold text-gray-900 mb-6">{results.length} businesses found</h2>
            <div className="grid gap-4 md:grid-cols-2">
              {results.map(b => <BusinessCard key={b.id} b={b} />)}
            </div>
          </>
        )}

        {!searched && (
          <>
            <h2 className="text-xl font-bold text-gray-900 mb-6">Featured Businesses</h2>
            {featured.length === 0 ? (
              <p className="text-gray-400 text-sm">No featured listings yet.</p>
            ) : (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {featured.map(b => <BusinessCard key={b.id} b={b} />)}
              </div>
            )}

            {/* Popular categories */}
            <div className="mt-16">
              <h3 className="text-lg font-bold text-gray-900 mb-4">Browse by Category</h3>
              <div className="flex flex-wrap gap-3">
                {['Hair & Beauty', 'Wellness & Spa', 'Fitness & Gym', 'Yoga & Pilates', 'Nail Care', 'Massage', 'Skincare', 'Barbershop'].map(cat => (
                  <button
                    key={cat}
                    onClick={() => { setQ(cat); }}
                    className="bg-white border border-gray-200 rounded-full px-4 py-2 text-sm font-medium text-gray-700 hover:border-indigo-400 hover:text-indigo-700 transition-colors"
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* CTA for businesses */}
            <div className="mt-16 bg-indigo-50 border border-indigo-100 rounded-2xl p-8 text-center">
              <h3 className="text-lg font-bold text-indigo-900 mb-2">Own a service business?</h3>
              <p className="text-indigo-700 text-sm mb-4">List your business on Upkilo Discover for free and get more clients.</p>
              <a href="/register" className="inline-block bg-indigo-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-indigo-700">
                List Your Business Free →
              </a>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
