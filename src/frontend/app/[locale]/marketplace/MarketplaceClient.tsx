'use client';

import { useState } from 'react';
import { Search, MapPin, Filter, Loader2, Compass, TrendingUp } from 'lucide-react';
import { FeaturedListingCard } from '@/components/marketplace/FeaturedListingCard';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

export interface MarketplaceListing {
  id: string;
  [key: string]: unknown;
}

// Search, filtering and re-fetching stay client-side — they are genuinely interactive.
//
// What changed is where the FIRST set of listings comes from. This previously fetched them
// in a post-mount useEffect, which meant the server-rendered HTML contained only a spinner:
// crawlers that do not execute JavaScript saw an empty page, and the largest contentful
// paint waited on a round trip that had not started until after hydration.
//
// Now the parent Server Component fetches them and passes them in. Client Components are
// still server-rendered in the App Router — only their interactivity is deferred to
// hydration — so seeding state from props puts real listing markup in the initial HTML
// while every interaction below continues to work exactly as before.
export function MarketplaceClient({ initialListings }: { initialListings: MarketplaceListing[] }) {
  const [listings, setListings] = useState<MarketplaceListing[]>(initialListings);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [city, setCity] = useState('');
  const [category, setCategory] = useState('');

  const fetchListings = async (overrides?: { city?: string; category?: string }) => {
    setLoading(true);
    try {
      const res = await api.marketplace.getFeaturedListings({
        city: overrides?.city ?? (city || undefined),
        category: overrides?.category ?? (category || undefined),
        search: search || undefined,
      });
      setListings(res.data || []);
    } catch (err) {
      console.error('Failed to fetch listings:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchListings();
  };

  return (
    <>
      {/* Search Bar */}
      <form
        onSubmit={handleSearch}
        className="max-w-4xl mx-auto p-2 bg-white rounded-2xl shadow-2xl border border-gray-100 flex flex-wrap gap-2 items-center"
      >
        <div className="flex-1 min-w-[200px] flex items-center px-4 gap-3 border-r border-gray-100">
          <Search className="h-5 w-5 text-gray-400" aria-hidden="true" />
          <label htmlFor="marketplace-search" className="sr-only">What are you looking for?</label>
          <input
            id="marketplace-search"
            type="text"
            placeholder="What are you looking for?"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full h-12 bg-transparent border-none text-gray-900 font-medium placeholder:text-gray-400 focus:ring-0 focus:outline-none"
          />
        </div>
        <div className="flex-1 min-w-[150px] flex items-center px-4 gap-3 border-r border-gray-100">
          <MapPin className="h-5 w-5 text-gray-400" aria-hidden="true" />
          <label htmlFor="marketplace-city" className="sr-only">City or location</label>
          <input
            id="marketplace-city"
            type="text"
            placeholder="City or Location"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            className="w-full h-12 bg-transparent border-none text-gray-900 font-medium placeholder:text-gray-400 focus:ring-0 focus:outline-none"
          />
        </div>
        <Button
          type="submit"
          className="h-12 px-8 bg-gray-900 hover:bg-black text-white font-bold rounded-xl"
        >
          Search Marketplace
        </Button>
      </form>

      {/* Results */}
      <div className="max-w-7xl mx-auto px-6 pb-32 mt-24">
        <div className="flex flex-wrap items-center justify-between gap-6 mb-12">
          <div className="flex items-center gap-4">
            <h2 className="text-2xl font-black text-gray-900 uppercase tracking-tight">Featured Listings</h2>
            <span className="h-0.5 w-12 bg-primary-500 rounded-full" />
          </div>

          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-100 rounded-xl shadow-sm">
              <Filter className="h-4 w-4 text-gray-500" aria-hidden="true" />
              <span className="text-sm font-bold text-gray-700">
                {category || 'All Categories'}
              </span>
            </div>
          </div>
        </div>

        {loading ? (
          <div className="flex flex-col items-center justify-center py-32">
            <Loader2 className="h-10 w-10 text-primary-500 animate-spin mb-4" aria-hidden="true" />
            <p className="text-gray-500 font-medium">Searching the marketplace…</p>
          </div>
        ) : listings.length === 0 ? (
          <div className="p-20 bg-white rounded-3xl border-2 border-dashed border-gray-100 text-center">
            <div className="w-20 h-20 bg-gray-50 rounded-full flex items-center justify-center mx-auto mb-6">
              <Compass className="h-10 w-10 text-gray-300" aria-hidden="true" />
            </div>
            <h3 className="text-xl font-bold text-gray-900">No matching listings found</h3>
            <p className="text-gray-500 mt-2 max-w-sm mx-auto">
              Try broadening your search, or explore the categories below.
            </p>
            <Button
              variant="outline"
              className="mt-8"
              onClick={() => { setCity(''); setCategory(''); fetchListings({ city: undefined, category: undefined }); }}
            >
              Show all listings
            </Button>
          </div>
        ) : (
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {listings.map((listing) => (
              <FeaturedListingCard key={listing.id} listing={listing} />
            ))}
          </div>
        )}

        {/* Popular Categories */}
        <div className="mt-40">
          <div className="flex items-center gap-4 mb-12">
            <h2 className="text-2xl font-black text-gray-900 uppercase tracking-tight">Popular Categories</h2>
            <span className="h-0.5 w-12 bg-primary-500 rounded-full" />
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {['Wellness', 'Beauty', 'Fitness', 'Automotive', 'Professional', 'Home Services'].map((cat) => (
              <button
                key={cat}
                type="button"
                onClick={() => { setCategory(cat); fetchListings({ category: cat }); }}
                aria-pressed={category === cat}
                className={cn(
                  'group p-6 bg-white rounded-2xl border border-gray-100 shadow-sm hover:border-primary-500 transition-all text-center focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600',
                  category === cat && 'border-primary-500 bg-primary-50/30'
                )}
              >
                <span className="w-12 h-12 bg-gray-50 rounded-full flex items-center justify-center mx-auto mb-4 group-hover:bg-primary-50 transition-colors">
                  <TrendingUp className="h-6 w-6 text-gray-400 group-hover:text-primary-500 transition-colors" aria-hidden="true" />
                </span>
                <span className="text-sm font-bold text-gray-900 tracking-tight">{cat}</span>
              </button>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}
