'use client';

import { useEffect, useState } from 'react';
import { Link } from '@/navigation';
import { Menu, X } from 'lucide-react';

const LINKS = [
  { href: '#features', label: 'Features' },
  { href: '#how', label: 'How it works' },
  { href: '#industries', label: 'Who it’s for' },
  { href: '#pricing', label: 'Pricing' },
  { href: '#faq', label: 'FAQ' },
];

export default function LandingNav() {
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  return (
    <nav
      className={`fixed top-0 z-50 w-full transition-all duration-300 ${
        scrolled
          // bg-background, not bg-white: this bar frosts whatever the page is, and the
          // page is dark in dark mode. An opacity modifier is the one shape the token
          // codemod deliberately skips (a `/10` scrim over a gradient is usually
          // decorative), so a sticky nav is exactly where that exception has to be
          // undone by hand — it was a pale bar pinned over every dark marketing page.
          ? 'border-b border-border bg-background/85 backdrop-blur-lg'
          : 'border-b border-transparent bg-transparent'
      }`}
      aria-label="Main navigation"
    >
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand */}
        <Link href="/" className="flex items-center gap-2.5" aria-label="Upkilo home">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary-500 to-primary-600 shadow-lg shadow-primary-500/30">
            <span className="text-sm font-bold text-white">U</span>
          </div>
          <span
            className={`text-lg font-bold tracking-tight transition-colors ${
              scrolled ? 'text-foreground' : 'text-white'
            }`}
          >
            Upkilo
          </span>
        </Link>

        {/* Desktop links */}
        <div
          className={`hidden items-center gap-8 text-sm font-medium md:flex ${
            scrolled ? 'text-foreground-secondary' : 'text-slate-300'
          }`}
        >
          {LINKS.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className={`transition-colors ${scrolled ? 'hover:text-primary' : 'hover:text-white'}`}
            >
              {l.label}
            </a>
          ))}
        </div>

        {/* Desktop CTAs */}
        <div className="hidden items-center gap-3 md:flex">
          <Link
            href="/login"
            className={`text-sm font-medium transition-colors ${
              scrolled ? 'text-foreground-secondary hover:text-foreground' : 'text-slate-300 hover:text-white'
            }`}
          >
            Sign in
          </Link>
          <Link
            href="/register"
            className="rounded-lg bg-primary-600 px-4 py-2 text-sm font-semibold text-white shadow-lg shadow-primary-500/25 transition-all hover:-translate-y-0.5 hover:bg-primary-500 hover:shadow-primary-500/40"
          >
            Start free trial
          </Link>
        </div>

        {/* Mobile toggle */}
        <button
          type="button"
          className={`md:hidden ${scrolled ? 'text-foreground' : 'text-white'}`}
          onClick={() => setOpen((o) => !o)}
          aria-label={open ? 'Close menu' : 'Open menu'}
          aria-expanded={open}
        >
          {open ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
        </button>
      </div>

      {/* Mobile panel */}
      {open && (
        <div className="border-t border-border bg-card px-4 py-4 md:hidden">
          <div className="flex flex-col gap-1">
            {LINKS.map((l) => (
              <a
                key={l.href}
                href={l.href}
                onClick={() => setOpen(false)}
                className="rounded-lg px-3 py-2.5 text-sm font-medium text-foreground hover:bg-accent"
              >
                {l.label}
              </a>
            ))}
            <div className="mt-2 flex flex-col gap-2 border-t border-border-subtle pt-3">
              <Link
                href="/login"
                onClick={() => setOpen(false)}
                className="rounded-lg px-3 py-2.5 text-sm font-medium text-foreground hover:bg-accent"
              >
                Sign in
              </Link>
              <Link
                href="/register"
                onClick={() => setOpen(false)}
                className="rounded-lg bg-primary-600 px-3 py-2.5 text-center text-sm font-semibold text-white"
              >
                Start free trial
              </Link>
            </div>
          </div>
        </div>
      )}
    </nav>
  );
}
