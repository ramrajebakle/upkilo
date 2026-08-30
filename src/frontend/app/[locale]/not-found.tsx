import Link from 'next/link';
import { RootHtml } from '@/components/layout/RootHtml';
import '../globals.css';

/**
 * The 404 page rendered its own `<html>` with every colour inlined as a literal — a #F8F8FA
 * body and #111120 headline — and never imported globals.css. That gave it two problems at
 * once: a dark-mode user hitting a bad link got a full-screen white flash, and because no
 * stylesheet or font variables were loaded, the whole page fell back to the browser's default
 * serif. It was the only route in the app in Times New Roman.
 *
 * It renders through RootHtml like every other root now, so it inherits the fonts, the
 * pre-paint theme script and the token layer, and the markup is plain utilities.
 */
export default function NotFound() {
  return (
    <RootHtml lang="en">
      <div className="flex min-h-screen flex-col items-center justify-center px-6 text-center">
        {/* Brand mark. The gradient and its white letter are fixed on purpose: this is the
            logo, not a themed surface. */}
        <div className="mb-8 flex h-18 w-18 items-center justify-center rounded-[20px] bg-gradient-to-br from-ai-500 to-primary-500 shadow-[0_8px_24px_rgba(124,58,237,0.3)]">
          <span className="text-[32px] font-extrabold text-white">U</span>
        </div>

        <p className="mb-3 text-[13px] font-bold uppercase tracking-[0.1em] text-primary">
          Error 404
        </p>

        <h1 className="mb-4 text-[clamp(32px,6vw,56px)] font-extrabold leading-[1.1] text-foreground">
          Page not found
        </h1>

        <p className="mb-10 max-w-[420px] text-lg leading-relaxed text-foreground-secondary">
          The page you&rsquo;re looking for doesn&rsquo;t exist or has been moved.
        </p>

        <div className="flex flex-wrap justify-center gap-3">
          <Link
            href="/en"
            className="inline-flex items-center gap-2 rounded-xl bg-primary px-7 py-3.5 text-[15px] font-semibold text-primary-foreground no-underline shadow-[var(--shadow-glow)]"
          >
            Go to homepage
          </Link>
          <Link
            href="/en/dashboard"
            className="inline-flex items-center gap-2 rounded-xl bg-muted px-7 py-3.5 text-[15px] font-semibold text-foreground no-underline hover:bg-accent"
          >
            Open dashboard
          </Link>
        </div>

        <p className="mt-12 text-[13px] text-foreground-muted">
          Need help?{' '}
          <a href="mailto:support@upkilo.com" className="text-primary no-underline hover:underline">
            Contact support
          </a>
        </p>
      </div>
    </RootHtml>
  );
}
