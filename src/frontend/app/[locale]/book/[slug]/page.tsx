import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import PublicBookingClient from './PublicBookingClient';

// Prevents </script> injection when tenant-controlled data lands in JSON-LD blocks.
// JSON.stringify alone does NOT escape </script>; dangerouslySetInnerHTML bypasses React's escaping.
function safeJsonLd(obj: unknown): string {
  return JSON.stringify(obj).replace(/</g, '\\u003c');
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

interface Business {
  name: string;
  logo?: string;
  primaryColor?: string;
  email?: string;
  phone?: string;
  address?: {
    line1?: string;
    city?: string;
    state?: string;
    postalCode?: string;
    country?: string;
  };
  description?: string;
  website?: string;
  slug?: string;
  industry?: string;
  services?: Array<{ name: string; price: number; durationMinutes: number }>;
  reviews?: { averageRating: number; totalCount: number };
}

async function getBusiness(slug: string): Promise<Business | null> {
  try {
    // Use the SEO meta endpoint which includes services + review stats
    const res = await fetch(`${API_URL}/api/seo/meta/${slug}`, {
      next: { revalidate: 3600 },
    });
    if (!res.ok) {
      // Fallback to booking endpoint
      const fallback = await fetch(`${API_URL}/api/booking/${slug}`, { next: { revalidate: 3600 } });
      if (!fallback.ok) return null;
      const data = await fallback.json();
      return data.business ?? null;
    }
    return await res.json();
  } catch {
    return null;
  }
}

// ─── Per-tenant dynamic metadata (title, description, OG image) ───────────────
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string; locale: string }>;
}): Promise<Metadata> {
  const { slug, locale } = await params;
  const business = await getBusiness(slug);
  if (!business) {
    return { title: 'Business Not Found | Upkilo' };
  }

  const title = `Book ${business.name} — Online Booking`;
  const description =
    business.description ||
    `Book appointments online with ${business.name}. Fast, easy, and instant confirmation.`;
  const url = `${process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com'}/${locale}/book/${slug}`;
  const ogImage = business.logo || `${process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com'}/og-default.png`;

  return {
    title,
    description,
    alternates: { canonical: url },
    openGraph: {
      title,
      description,
      url,
      type: 'website',
      images: [{ url: ogImage, width: 1200, height: 630, alt: business.name }],
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images: [ogImage],
    },
  };
}

// ─── Server component — renders JSON-LD then delegates to client ──────────────
export default async function PublicBookingPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string; locale: string }>;
  searchParams: Promise<{ mode?: string; color?: string; transparent?: string; service?: string }>;
}) {
  const { slug, locale } = await params;
  const { mode, color, transparent, service } = await searchParams;
  const business = await getBusiness(slug);
  if (!business) notFound();

  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';
  const bookingUrl = `${siteUrl}/${locale}/book/${slug}`;

  // ── Comprehensive JSON-LD schema for Google Rich Results ──────────────────
  const businessType = business.industry?.toLowerCase().includes('spa') ? 'DaySpa'
    : business.industry?.toLowerCase().includes('hair') ? 'HairSalon'
    : business.industry?.toLowerCase().includes('nail') ? 'NailSalon'
    : business.industry?.toLowerCase().includes('barber') ? 'BarberShop'
    : 'LocalBusiness';

  const mapQuery = encodeURIComponent(
    [business.address?.line1, business.address?.city, business.address?.country]
      .filter(Boolean).join(', ')
  );

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': businessType,
    name: business.name,
    url: business.website || bookingUrl,
    image: business.logo,
    telephone: business.phone,
    email: business.email,
    description: business.description,
    ...(business.address && {
      address: {
        '@type': 'PostalAddress',
        streetAddress: business.address.line1,
        addressLocality: business.address.city,
        addressRegion: business.address.state,
        postalCode: business.address.postalCode,
        addressCountry: business.address.country,
      },
    }),
    // Review aggregate — shows star rating in Google results
    ...(business.reviews && business.reviews.totalCount > 0 && {
      aggregateRating: {
        '@type': 'AggregateRating',
        ratingValue: business.reviews.averageRating,
        reviewCount: business.reviews.totalCount,
        bestRating: 5,
        worstRating: 1,
      },
    }),
    // Each service becomes a rich result
    ...(business.services && business.services.length > 0 && {
      hasOfferCatalog: {
        '@type': 'OfferCatalog',
        name: `${business.name} Services`,
        itemListElement: business.services.map((svc) => ({
          '@type': 'Offer',
          itemOffered: {
            '@type': 'Service',
            name: svc.name,
          },
          price: svc.price,
          priceCurrency: 'USD',
        })),
      },
    }),
    hasMap: `https://www.google.com/maps/search/?api=1&query=${mapQuery}`,
    potentialAction: {
      '@type': 'ReserveAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: bookingUrl,
        actionPlatform: [
          'http://schema.org/DesktopWebPlatform',
          'http://schema.org/MobileWebPlatform',
        ],
      },
      result: { '@type': 'Reservation', name: 'Book appointment' },
    },
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: safeJsonLd(jsonLd) }}
      />
      <PublicBookingClient
        business={business}
        slug={slug}
        mode={mode}
        color={color}
        transparent={transparent === 'true'}
        preselectServiceId={service}
      />
    </>
  );
}
