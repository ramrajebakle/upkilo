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

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
});

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap',
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
