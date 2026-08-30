'use client';

import { useState, useEffect } from 'react';
import { SearchX, MapPin } from 'lucide-react';

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
      <span className="text-sm text-foreground-secondary">{rating.toFixed(1)} ({count})</span>
    </div>
  );

  const BusinessCard = ({ b }: { b: Business }) => (
    <div className="bg-card rounded-2xl border border-border p-6 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-bold text-foreground truncate">{b.name}</h3>
          {b.tagline && <p className="text-sm text-foreground-secondary mt-0.5 line-clamp-2">{b.tagline}</p>}
          {b.reviewCount > 0 && <div className="mt-2"><StarRating rating={b.averageRating} count={b.reviewCount} /></div>}
          {b.city && (
            <p className="inline-flex items-center gap-1 text-xs text-foreground-secondary mt-2">
              {/* Was a 📍 emoji at text-foreground-muted. The emoji is a font glyph that renders
                  differently per platform and is announced as "round pushpin" by screen
                  readers; gray-400 on white is ~2.8:1, under the 4.5:1 floor. */}
              <MapPin className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              {b.city}{b.country ? `, ${b.country}` : ''}
            </p>
          )}
          {b.industry && (
            <span className="inline-block mt-2 text-xs bg-brand-subtle text-primary px-2 py-0.5 rounded-full">{b.industry}</span>
          )}
        </div>
        <a
          href={b.bookingUrl}
          className="shrink-0 bg-primary-600 text-white text-sm px-4 py-2 rounded-xl font-semibold hover:bg-primary-700 transition-colors"
        >
          Book Now
        </a>
      </div>
    </div>
  );

  return (
    <main className="min-h-screen bg-muted">
      {/* Hero Search */}
      <section className="bg-gradient-to-br from-primary-600 to-primary-700 text-white py-20 px-4">
        <div className="max-w-3xl mx-auto text-center">
          <h1 className="text-4xl font-bold mb-3">Find & Book Local Services</h1>
          <p className="text-primary-100 mb-8">Salons, spas, clinics, therapists — book online in seconds.</p>

          <form onSubmit={handleSearch} className="bg-card rounded-2xl shadow-xl p-2 flex gap-2 flex-wrap md:flex-nowrap">
            <input
              value={q}
              onChange={e => setQ(e.target.value)}
              placeholder="Service (e.g. haircut, massage...)"
              className="flex-1 px-4 py-3 text-foreground rounded-xl text-sm outline-none min-w-0"
            />
            <input
              value={city}
              onChange={e => setCity(e.target.value)}
              placeholder="City"
              className="flex-1 px-4 py-3 text-foreground rounded-xl text-sm outline-none border-l border-border min-w-0"
            />
            <button
              type="submit"
              className="bg-primary-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-primary-500 shrink-0"
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
            <div className="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full mx-auto mb-3" />
            <p className="text-foreground-secondary">Searching...</p>
          </div>
        )}

        {!loading && searched && results.length === 0 && (
          <div className="text-center py-16">
            {/* A drawn icon rather than the 🔍 emoji that stood here — emoji render
                per-platform and belong to no design system. */}
            <SearchX className="mx-auto mb-3 h-10 w-10 text-gray-300" aria-hidden="true" />
            <h2 className="text-lg font-semibold text-foreground">No results found</h2>
            <p className="text-foreground-secondary mt-1">Try a different search or browse featured businesses below.</p>
          </div>
        )}

        {!loading && searched && results.length > 0 && (
          <>
            <h2 className="text-xl font-bold text-foreground mb-6">{results.length} businesses found</h2>
            <div className="grid gap-4 md:grid-cols-2">
              {results.map(b => <BusinessCard key={b.id} b={b} />)}
            </div>
          </>
        )}

        {!searched && (
          <>
            <h2 className="text-xl font-bold text-foreground mb-6">Featured Businesses</h2>
            {featured.length === 0 ? (
              <p className="text-foreground-secondary text-sm">No featured listings yet.</p>
            ) : (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {featured.map(b => <BusinessCard key={b.id} b={b} />)}
              </div>
            )}

            {/* Popular categories */}
            <div className="mt-16">
              <h3 className="text-lg font-bold text-foreground mb-4">Browse by Category</h3>
              <div className="flex flex-wrap gap-3">
                {/* "Fitness & Gym" and "Yoga & Pilates" removed — Upkilo no longer serves that
                    vertical, and offering the categories here invites searches that return
                    nothing. */}
                {['Hair & Beauty', 'Wellness & Spa', 'Nail Care', 'Massage', 'Skincare', 'Barbershop'].map(cat => (
                  <button
                    key={cat}
                    onClick={() => { setQ(cat); }}
                    className="bg-card border border-border rounded-full px-4 py-2 text-sm font-medium text-foreground hover:border-primary-400 hover:text-primary transition-colors"
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* CTA for businesses */}
            {/* bg-primary-50 is a fixed light tint — on a dark page it was a pale slab, and the
                primary-900 heading on it went unreadable. --brand-subtle is the same idea
                expressed as a percentage of the brand over whatever the page currently is, so
                it stays a wash in both themes. */}
            <div className="mt-16 bg-brand-subtle border border-primary-500/25 rounded-2xl p-8 text-center">
              <h3 className="text-lg font-bold text-foreground mb-2">Own a service business?</h3>
              <p className="text-foreground-secondary text-sm mb-4">List your business on Upkilo Discover for free and get more clients.</p>
              <a href="/register" className="inline-block bg-primary-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-primary-700">
                List Your Business Free →
              </a>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
