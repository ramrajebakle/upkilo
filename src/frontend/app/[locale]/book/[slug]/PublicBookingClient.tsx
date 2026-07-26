"use client";

import React, { useEffect } from "react";
import { BookingWizard } from "@/components/booking/BookingWizard";

interface Business {
  name: string;
  logo?: string;
  primaryColor?: string;
  email?: string;
  phone?: string;
}

interface Props {
  business: Business;
  slug: string;
  /** "widget" hides outer chrome for clean iframe embedding on the tenant's own site. */
  mode?: string;
  /** Overrides the tenant brand colour (from widget embed config, e.g. "#000000"). */
  color?: string;
  /** Transparent background so the embed blends into the host page. */
  transparent?: boolean;
  /** Deep-link: pre-select this service and skip straight to the time step. */
  preselectServiceId?: string;
}

export default function PublicBookingClient({ business, slug, mode, color, transparent, preselectServiceId }: Props) {
  const isWidget = mode === 'widget';
  const primaryColor = color || business.primaryColor;

  // Inject brand colours into CSS variables (widget `color` overrides the tenant default).
  useEffect(() => {
    if (!primaryColor) return;
    const hex = primaryColor.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    document.documentElement.style.setProperty('--primary-color', primaryColor);
    document.documentElement.style.setProperty('--primary-color-hover', `rgb(${Math.max(0, r - 20)}, ${Math.max(0, g - 20)}, ${Math.max(0, b - 20)})`);
    document.documentElement.style.setProperty('--primary-color-light', `rgba(${r}, ${g}, ${b}, 0.1)`);
  }, [primaryColor]);

  // Widget mode: post the content height to the parent page so the embedding
  // iframe can auto-resize (the embed snippet listens for "resize-upkilo-widget").
  useEffect(() => {
    if (!isWidget || typeof window === 'undefined' || window.parent === window) return;
    const postHeight = () => {
      const height = document.body.scrollHeight;
      window.parent.postMessage({ type: 'resize-upkilo-widget', height }, '*');
    };
    postHeight();
    const observer = new ResizeObserver(postHeight);
    observer.observe(document.body);
    return () => observer.disconnect();
  }, [isWidget]);

  return (
    <div className={`min-h-screen flex flex-col ${transparent ? 'bg-transparent' : 'bg-slate-50'}`}>
      {/* Header — hidden in widget mode (the host site provides its own branding) */}
      {!isWidget && (
      <header className="bg-white border-b sticky top-0 z-10 shadow-sm">
        <div className="max-w-4xl mx-auto px-4 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            {business.logo ? (
              <img src={business.logo} alt={business.name} className="h-8 w-auto" />
            ) : (
              <div
                className="w-8 h-8 rounded-lg flex items-center justify-center text-white font-bold"
                style={{ backgroundColor: business.primaryColor || '#6366f1' }}
              >
                {business.name?.[0] || 'U'}
              </div>
            )}
            <span className="font-bold text-lg tracking-tight text-slate-900">
              {business.name}
            </span>
          </div>
        </div>
      </header>
      )}

      {/* Booking wizard */}
      <main className={`max-w-4xl mx-auto px-4 flex-1 w-full ${isWidget ? 'py-4' : 'py-8 md:py-12'}`}>
        <BookingWizard tenantSlug={slug} preselectServiceId={preselectServiceId} />
      </main>

      {/* Footer — hidden in widget mode to keep the embed compact */}
      {!isWidget && (
      <footer className="py-8 text-center text-xs text-slate-400">
        &copy; {new Date().getFullYear()} {business.name}. All rights reserved.
        <span className="mx-2">·</span>
        <a href="https://upkilo.com" target="_blank" rel="noopener" className="hover:text-slate-600 transition-colors">
          Powered by Upkilo
        </a>
      </footer>
      )}
    </div>
  );
}
