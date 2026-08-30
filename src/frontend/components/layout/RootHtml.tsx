import type { CSSProperties, ReactNode } from 'react';
import { fontVariables } from '@/app/fonts';
import { ThemeProvider } from '@/components/ThemeProvider';
import { ThemeScript } from '@/components/ThemeScript';
import { cn } from '@/lib/utils';

/**
 * The one <html>/<body> shell every root layout renders.
 *
 * This app has TEN root layouts, not one: app/[locale] plus nine standalone route groups
 * (au, ca, uae, uk, book, discover, enterprise, powered-by, offline) that sit outside the
 * locale segment and therefore have no shared parent. Each one hand-rolled its own
 * `<html><body>{children}</body></html>`, and only app/[locale] ever got the fonts, the
 * theme class or a ThemeProvider.
 *
 * That is the structural reason "dark mode works in the dashboard but not on the public
 * site": those nine trees had no theme at all — no toggle could reach them, no stored
 * preference was read, and the `.dark` class was never applied, so they rendered light
 * unconditionally even for a user whose OS and account were both set to dark.
 *
 * Centralising the shell means a new top-level route inherits the theme system by
 * existing, rather than by remembering to copy four things. That is the property the whole
 * refactor is for: adding a page must not mean re-implementing dark mode.
 */
export function RootHtml({
    lang = 'en',
    dir = 'ltr',
    children,
    bodyClassName,
    bodyStyle,
    headChildren,
}: {
    lang?: string;
    dir?: 'ltr' | 'rtl';
    children: ReactNode;
    bodyClassName?: string;
    bodyStyle?: CSSProperties;
    /** JSON-LD and other tags a route needs at the top of <body>. */
    headChildren?: ReactNode;
}) {
    return (
        <html
            lang={lang}
            dir={dir}
            className={fontVariables}
            // ThemeScript writes a class and style.colorScheme onto this element before
            // React hydrates, so the client tree will legitimately differ from the server
            // HTML here. Without this, React logs a hydration error on every page load.
            suppressHydrationWarning
        >
            <body className={cn('font-sans', bodyClassName)} style={bodyStyle} suppressHydrationWarning>
                {/*
                  First element in <body>, and synchronous. The parser runs it before it
                  paints anything below, which is what makes the theme correct on the very
                  first frame instead of one frame after hydration.
                */}
                <ThemeScript />
                {headChildren}
                {/*
                  Wrapping every root — not just the dashboard — is what lets a theme
                  toggle, or any useTheme() consumer, be dropped onto a public page later
                  without another provider audit.
                */}
                <ThemeProvider>{children}</ThemeProvider>
            </body>
        </html>
    );
}
