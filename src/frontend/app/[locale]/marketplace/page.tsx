import { Sparkles, ShieldCheck } from 'lucide-react';
import { MarketplaceClient, type MarketplaceListing } from './MarketplaceClient';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

// Server-side so the listings land in the initial HTML. This page previously fetched them in
// a post-mount useEffect, so what a crawler received — and what determined the largest
// contentful paint — was a spinner. Interactivity still lives in MarketplaceClient; only the
// first load moved here.
//
// Uses raw fetch rather than lib/api's `api.marketplace.getFeaturedListings`, because that
// module is an axios instance built on js-cookie and auth interceptors and cannot run on the
// server. The client component still uses it for search and filtering, where it belongs.
//
// Fails soft: an unreachable API renders the empty state rather than throwing, matching how
// every other server-fetching page in this app behaves.
async function fetchFeaturedListings(): Promise<MarketplaceListing[]> {
  try {
    const res = await fetch(`${API_URL}/api/v1/marketplace/featured-listings`, {
      next: { revalidate: 300 },
    });
    if (!res.ok) return [];
    const json = await res.json();
    const data = json?.data ?? json;
    return Array.isArray(data) ? data : [];
  } catch {
    return [];
  }
}

export default async function ConsumerMarketplacePage() {
  const initialListings = await fetchFeaturedListings();

  return (
    <div className="min-h-screen bg-background">
      {/* Hero */}
      <div className="relative pt-20 pb-24 overflow-hidden">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full max-w-7xl h-full pointer-events-none" aria-hidden="true">
          <div className="absolute top-[-10%] right-[-10%] w-[40%] h-[40%] bg-primary-100/30 rounded-full blur-3xl opacity-50" />
          <div className="absolute bottom-[-10%] left-[-10%] w-[40%] h-[40%] bg-primary-100/30 rounded-full blur-3xl opacity-50" />
        </div>

        <div className="max-w-7xl mx-auto px-6 relative z-10 text-center">
          <div className="inline-flex items-center gap-2 px-3 py-1 bg-card/80 backdrop-blur-sm border border-primary/25 rounded-full text-primary text-xs font-black uppercase tracking-widest mb-6 shadow-sm">
            <Sparkles className="h-3 w-3 fill-primary-500 text-primary" aria-hidden="true" /> Book Local Services
          </div>
          <h1 className="text-5xl md:text-7xl font-black text-foreground tracking-tight leading-none mb-6">
            Upkilo <span className="text-primary italic">Marketplace</span>
          </h1>
          <p className="text-lg md:text-xl text-foreground-secondary max-w-2xl mx-auto font-medium mb-10">
            Find and book local salons, spas, studios and clinics. Real-time availability,
            instant confirmation.
          </p>

          <MarketplaceClient initialListings={initialListings} />
        </div>
      </div>

      {/* How booking works.
          This replaced a "Trust Section" that claimed "10K+ Professionals", "1M+ Bookings",
          and "Every Business is Verified by Upkilo — we manually vet every professional on our
          platform". All three were fabricated: there are no tenants yet, so there are no
          professionals, no bookings, and no vetting process to describe. Specific invented
          figures are the least defensible form of the unverifiable-claim problem being cleaned
          up across the marketing pages, and these were the largest numbers on the site.

          Restore a statistics block here only when the numbers can be read from real data. */}
      <div className="max-w-7xl mx-auto px-6 pb-32">
        <div className="p-12 bg-gray-900 rounded-[40px] text-white overflow-hidden relative">
          <div className="relative z-10 max-w-3xl">
            <ShieldCheck className="h-10 w-10 text-primary-400 mb-6" aria-hidden="true" />
            <h2 className="text-4xl font-black mb-6 leading-tight">
              Book directly with the business.
            </h2>
            <p className="text-slate-400 text-lg">
              Every listing here is a business running its own bookings on Upkilo — so the
              availability you see is their real calendar, and confirmation is immediate. No
              request forms, no waiting for a callback, no double bookings.
            </p>
          </div>
          <div className="absolute top-0 right-0 -mr-20 -mt-20 w-80 h-80 bg-primary-600/20 rounded-full blur-3xl" aria-hidden="true" />
        </div>
      </div>
    </div>
  );
}
