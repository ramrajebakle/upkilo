'use client';

import { Star, MapPin, ExternalLink, ShieldCheck } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';

interface FeaturedListingCardProps {
  listing: any;
}

export function FeaturedListingCard({ listing }: FeaturedListingCardProps) {
  return (
    <div className="bg-card rounded-2xl border border-border-subtle shadow-sm hover:shadow-xl transition-all duration-300 overflow-hidden group">
      <div className="relative h-48 w-full bg-muted overflow-hidden">
        {listing.logoUrl ? (
          <Image 
            src={listing.logoUrl} 
            alt={listing.businessName} 
            fill 
            className="object-cover group-hover:scale-105 transition-transform duration-500"
          />
        ) : (
          <div className="flex items-center justify-center h-full bg-brand-subtle text-primary-200">
            <span className="text-4xl font-black">{listing.businessName.charAt(0)}</span>
          </div>
        )}
        <div className="absolute top-4 left-4 flex gap-2">
           {listing.isFeatured && (
             <span className="px-3 py-1 bg-primary-600 text-white text-[10px] font-black rounded-full shadow-lg flex items-center gap-1 uppercase tracking-wider">
               <Star className="h-3 w-3 fill-white" /> Featured
             </span>
           )}
           {listing.isVerified && (
             <span className="px-3 py-1 bg-white/90 backdrop-blur-sm text-primary text-[10px] font-black rounded-full shadow-lg flex items-center gap-1 uppercase tracking-wider border border-white">
               <ShieldCheck className="h-3 w-3" /> Verified
             </span>
           )}
        </div>
      </div>

      <div className="p-6">
        <div className="flex items-start justify-between mb-2">
          <div>
            <h3 className="text-lg font-bold text-foreground group-hover:text-primary transition-colors">{listing.businessName}</h3>
            <p className="text-xs font-medium text-foreground-secondary flex items-center gap-1 mt-0.5">
              <span className="text-primary">{listing.category}</span>
              <span className="text-gray-300">•</span>
              <span className="flex items-center gap-1 bg-amber-50 text-amber-700 px-1.5 py-0.5 rounded leading-none text-[10px] font-bold">
                <Star className="h-2.5 w-2.5 fill-amber-500 text-warning-fg" /> {listing.averageRating || 5.0} ({listing.reviewCount || 0})
              </span>
            </p>
          </div>
        </div>

        <p className="text-sm text-foreground-secondary line-clamp-2 min-h-[40px] mt-3">
          {listing.description || `Leading ${listing.category} services in ${listing.city}. Book your appointment today with top-rated professionals.`}
        </p>

        <div className="mt-4 flex items-center gap-2 text-foreground-secondary text-xs">
          <MapPin className="h-3.5 w-3.5 text-foreground-muted" />
          <span>{listing.city}, {listing.state}</span>
        </div>

        <div className="mt-6 flex gap-3">
          <Link href={`/book/${listing.slug}`} className="flex-1">
            <Button className="w-full h-10 font-bold">Book Now</Button>
          </Link>
          <Button variant="outline" className="h-10 px-3">
            <ExternalLink className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
