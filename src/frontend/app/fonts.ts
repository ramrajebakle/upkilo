/**
 * The three faces the design system uses, loaded once and shared by every root layout.
 *
 * These used to live inside app/[locale]/layout.tsx, which meant the nine *other* root
 * layouts in this app (au, ca, uae, uk, book, discover, enterprise, powered-by, offline)
 * loaded no fonts at all. Those routes still imported globals.css, so `--font-sans`
 * resolved to `var(--font-inter), …` with `--font-inter` undefined — and an undefined
 * custom property in a font-family list invalidates the whole declaration, dropping the
 * page to the browser's default serif. Every public marketing and booking page outside
 * /[locale] was rendering in Times New Roman.
 *
 * Defining them in one module rather than per-layout also matters for weight: next/font
 * deduplicates by call site, so three layouts each calling `Inter({...})` produce three
 * separate font instances and three preload sets.
 */
import { Inter, Outfit, JetBrains_Mono } from 'next/font/google';

/**
 * Only Inter is preloaded.
 *
 * All three variables sit on <html> for every route, so next/font emitted a
 * <link rel="preload"> for all three on every page. The browser then reported, on pages
 * that render body text and nothing else:
 *
 *   The resource .../<hash>.woff2 was preloaded using link preload but not used within a
 *   few seconds from the window's load event.
 *
 * That is not a false alarm — it is three font files fetched at high priority on the
 * critical path of every page, including /login, competing with the JS and CSS that page
 * actually needs.
 *
 * preload: false does NOT stop a font loading. The face is still declared and still
 * downloaded the moment CSS references it; it just stops being fetched eagerly before
 * anything asks for it. Inter keeps its preload because it backs --font-sans, so it IS on
 * the critical path of the first paint everywhere.
 *
 * The trade-off: on a page that uses Outfit above the fold, its download now starts when
 * the CSS is applied rather than in parallel with it. display: 'swap' means that shows as
 * a brief fallback face rather than invisible text.
 */
const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
});

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
  // Display face, used on a subset of pages — never required for a first paint.
  preload: false,
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap',
  // Monospace, used for code and numeric detail. Least likely of the three to appear at all.
  preload: false,
});

/**
 * Belongs on <html>, never on <body>.
 *
 * globals.css declares `--font-sans: var(--font-inter), …` inside @theme, which Tailwind
 * emits at :root — i.e. on <html>. A custom property is resolved in the scope that
 * declares it, so a :root declaration cannot see a variable defined on <body>, its own
 * descendant. With the variables on <body>, --font-sans computes to nothing.
 */
export const fontVariables = `${inter.variable} ${outfit.variable} ${jetbrainsMono.variable}`;
