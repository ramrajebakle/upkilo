'use client';

import { Toaster } from 'sonner';
import { useTheme } from '@/components/ThemeProvider';

/**
 * Sonner's <Toaster> with the theme actually wired in.
 *
 * It was mounted as a bare `<Toaster richColors />`, and sonner defaults to `theme="light"`.
 * Toasts therefore rendered as white cards with near-black text no matter what the rest of
 * the app was doing — the most visible "this one component never got dark mode" surface in
 * the product, and one that appears on top of everything else.
 *
 * `theme="system"` would not have fixed it either: that follows prefers-color-scheme, so a
 * user who had explicitly chosen dark on a light-set machine would still get light toasts.
 * Passing the already-resolved theme is the only option that respects the in-app choice.
 *
 * Sonner renders into its own portal, but the portal mounts inside <body>, so the
 * `.dark *` half of the dark variant reaches it — the CSS-variable overrides below apply in
 * both themes without a `dark:` prefix.
 */
export function ThemedToaster() {
    const { resolvedTheme } = useTheme();

    return (
        <Toaster
            position="bottom-right"
            theme={resolvedTheme}
            richColors
            toastOptions={{
                // Was `fontFamily: 'Inter, system-ui, sans-serif'` — a hardcoded family name
                // that bypassed next/font. The variable resolves to the self-hosted face
                // that the rest of the app uses, so toasts stop being the one element in a
                // different font.
                style: { fontFamily: 'var(--font-sans)' },
            }}
            // sonner reads these off its own root, which is how it themes the default
            // (non-richColors) toast. Pointing them at the design tokens keeps a plain
            // toast identical to a popover; richColors still overrides them for
            // success/error/warning, which is the intent there.
            style={
                {
                    '--normal-bg': 'var(--surface-popover)',
                    '--normal-text': 'var(--text-primary)',
                    '--normal-border': 'var(--surface-border)',
                } as React.CSSProperties
            }
        />
    );
}
