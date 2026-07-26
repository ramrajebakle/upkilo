'use client';

import { useState, useEffect } from 'react';
import { Search, MapPin, Filter, Loader2, Sparkles, TrendingUp, Compass, Globe } from 'lucide-react';
import { FeaturedListingCard } from '@/components/marketplace/FeaturedListingCard';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

export default function ConsumerMarketplacePage() {
    const [loading, setLoading] = useState(true);
    const [listings, setListings] = useState<any[]>([]);
    const [search, setSearch] = useState('');
    const [city, setCity] = useState('');
    const [category, setCategory] = useState('');

    useEffect(() => {
        fetchListings();
    }, []);

    const fetchListings = async () => {
        setLoading(true);
        try {
            const res = await api.marketplace.getFeaturedListings({
                city: city || undefined,
                category: category || undefined,
                search: search || undefined
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
        <div className="min-h-screen bg-[#FDFCFB]">
            {/* Hero Section */}
            <div className="relative pt-20 pb-24 overflow-hidden">
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full max-w-7xl h-full pointer-events-none">
                    <div className="absolute top-[-10%] right-[-10%] w-[40%] h-[40%] bg-primary-100/30 rounded-full blur-3xl opacity-50" />
                    <div className="absolute bottom-[-10%] left-[-10%] w-[40%] h-[40%] bg-indigo-100/30 rounded-full blur-3xl opacity-50" />
                </div>

                <div className="max-w-7xl mx-auto px-6 relative z-10 text-center">
                    <div className="inline-flex items-center gap-2 px-3 py-1 bg-white/80 backdrop-blur-sm border border-primary-100 rounded-full text-primary-700 text-xs font-black uppercase tracking-widest mb-6 shadow-sm">
                        <Sparkles className="h-3 w-3 fill-primary-500 text-primary-500" /> Discover the Best
                    </div>
                    <h1 className="text-5xl md:text-7xl font-black text-gray-900 tracking-tight leading-none mb-6">
                        Upkilo <span className="text-primary-600 italic">Marketplace</span>
                    </h1>
                    <p className="text-lg md:text-xl text-gray-600 max-w-2xl mx-auto font-medium mb-10">
                        Find and book the top-rated local services in your city. Verified professionals, seamless booking.
                    </p>

                    {/* Search Bar */}
                    <form 
                        onSubmit={handleSearch}
                        className="max-w-4xl mx-auto p-2 bg-white rounded-2xl shadow-2xl border border-gray-100 flex flex-wrap gap-2 items-center"
                    >
                        <div className="flex-1 min-w-[200px] flex items-center px-4 gap-3 border-r border-gray-100">
                            <Search className="h-5 w-5 text-gray-400" />
                            <input 
                                type="text" 
                                placeholder="What are you looking for?" 
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                                className="w-full h-12 bg-transparent border-none text-gray-900 font-medium placeholder:text-gray-400 focus:ring-0 focus:outline-none"
                            />
                        </div>
                        <div className="flex-1 min-w-[150px] flex items-center px-4 gap-3 border-r border-gray-100">
                            <MapPin className="h-5 w-5 text-gray-400" />
                            <input 
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
                </div>
            </div>

            {/* Results Section */}
            <div className="max-w-7xl mx-auto px-6 pb-32">
                <div className="flex flex-wrap items-center justify-between gap-6 mb-12">
                    <div className="flex items-center gap-4">
                        <h2 className="text-2xl font-black text-gray-900 uppercase tracking-tight">Featured Listings</h2>
                        <span className="h-0.5 w-12 bg-primary-500 rounded-full" />
                    </div>
                    
                    <div className="flex items-center gap-3">
                        <div className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-100 rounded-xl shadow-sm cursor-pointer hover:border-primary-300 transition-colors">
                            <Filter className="h-4 w-4 text-gray-500" />
                            <span className="text-sm font-bold text-gray-700">All Categories</span>
                        </div>
                    </div>
                </div>

                {loading ? (
                    <div className="flex flex-col items-center justify-center py-32">
                        <Loader2 className="h-10 w-10 text-primary-500 animate-spin mb-4" />
                        <p className="text-gray-500 font-medium">Scanning the marketplace for the best services...</p>
                    </div>
                ) : listings.length === 0 ? (
                    <div className="p-20 bg-white rounded-3xl border-2 border-dashed border-gray-100 text-center">
                        <div className="w-20 h-20 bg-gray-50 rounded-full flex items-center justify-center mx-auto mb-6">
                            <Compass className="h-10 w-10 text-gray-300" />
                        </div>
                        <h3 className="text-xl font-bold text-gray-900">No matching listings found</h3>
                        <p className="text-gray-500 mt-2 max-w-sm mx-auto">We couldn't find any listings in your area. Try broadening your search or exploring popular categories.</p>
                        <Button variant="outline" className="mt-8" onClick={() => { setCity(''); fetchListings(); }}>Show All Listings</Button>
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
                        <span className="h-0.5 w-12 bg-indigo-500 rounded-full" />
                    </div>
                    
                    <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
                        {['Wellness', 'Beauty', 'Fitness', 'Automotive', 'Professional', 'Home Services'].map((cat) => (
                            <div 
                                key={cat} 
                                onClick={() => { setCategory(cat); fetchListings(); }}
                                className={cn(
                                    "group p-6 bg-white rounded-2xl border border-gray-100 shadow-sm hover:border-primary-500 transition-all cursor-pointer text-center",
                                    category === cat && "border-primary-500 bg-primary-50/30"
                                )}
                            >
                                <div className="w-12 h-12 bg-gray-50 rounded-full flex items-center justify-center mx-auto mb-4 group-hover:bg-primary-50 transition-colors">
                                    <TrendingUp className="h-6 w-6 text-gray-400 group-hover:text-primary-500 transition-colors" />
                                </div>
                                <span className="text-sm font-bold text-gray-900 tracking-tight">{cat}</span>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Trust Section */}
                <div className="mt-40 p-12 bg-gray-900 rounded-[40px] text-white overflow-hidden relative">
                    <div className="relative z-10 grid md:grid-cols-2 items-center gap-12">
                        <div>
                            <h2 className="text-4xl font-black mb-6 leading-tight">Every Business is <br/><span className="text-primary-400">Verified by Upkilo.</span></h2>
                            <p className="text-gray-400 text-lg mb-8">
                                We manually vet every professional on our platform to ensure they meet our strict quality standards. 
                                Book with confidence knowing you're in good hands.
                            </p>
                            <div className="flex gap-4">
                                <div className="flex flex-col">
                                    <span className="text-3xl font-black">10K+</span>
                                    <span className="text-gray-500 text-xs font-bold uppercase tracking-widest">Professionals</span>
                                </div>
                                <div className="w-px h-12 bg-white/10" />
                                <div className="flex flex-col">
                                    <span className="text-3xl font-black">1M+</span>
                                    <span className="text-gray-500 text-xs font-bold uppercase tracking-widest">Bookings</span>
                                </div>
                            </div>
                        </div>
                        <div className="flex items-center justify-center">
                            <div className="w-full max-w-sm aspect-square bg-gradient-to-br from-primary-500/20 to-indigo-500/20 border border-white/10 rounded-3xl backdrop-blur-3xl flex items-center justify-center">
                                <Globe className="h-32 w-32 text-primary-400 opacity-20" />
                            </div>
                        </div>
                    </div>
                    <div className="absolute top-0 right-0 -mr-20 -mt-20 w-80 h-80 bg-primary-600/20 rounded-full blur-3xl" />
                </div>
            </div>
        </div>
    );
}
