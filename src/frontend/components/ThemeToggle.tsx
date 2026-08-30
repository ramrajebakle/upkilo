'use client';

import { Monitor, Moon, Sun } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { cn } from '@/lib/utils';
import { useTheme, type Theme } from '@/components/ThemeProvider';

/**
 * The two theme controls, in one place so they cannot disagree with each other.
 *
 * ── Why the icons are chosen by CSS rather than by JavaScript ──
 * The header toggle used to render `{resolvedTheme === 'dark' ? <Sun/> : <Moon/>}`. On the
 * server `resolvedTheme` is not knowable, so the server always emitted the light-mode icon;
 * a dark-mode user got a hydration mismatch and watched the icon swap one frame after load.
 * Gating an `aria-label` on the same value made the accessible name wrong for that frame too.
 *
 * Both icons are rendered and the `dark:` variant picks one. That is correct in the server
 * HTML, because ThemeScript has already put the class on <html> before the browser paints —
 * CSS knows the theme strictly earlier than React does. The accessible name comes from
 * sr-only text switched the same way, rather than from an aria-label, because an aria-label
 * cannot be varied by CSS.
 *
 * ── Why the live region lives here and not in ThemeProvider ──
 * ThemeProvider is mounted by RootHtml, which sits ABOVE NextIntlClientProvider and is also
 * used by the nine root layouts that have no intl provider at all. An announcement there
 * could only ever be English. It belongs with the control anyway: it exists to confirm a
 * deliberate user action, and this is where that action happens.
 */

/** Single-press light/dark switch, for toolbars. */
export function ThemeToggle({ className }: { className?: string }) {
    const { resolvedTheme, setTheme, mounted } = useTheme();
    const t = useTranslations('Theme');

    return (
        <>
            <button
                type="button"
                onClick={() => setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')}
                className={cn(
                    'p-2 min-h-11 min-w-11 sm:min-h-0 sm:min-w-0 rounded-lg transition-colors',
                    'text-foreground-secondary hover:text-foreground hover:bg-accent',
                    className
                )}
            >
                <Sun className="hidden h-5 w-5 dark:block" aria-hidden="true" />
                <Moon className="h-5 w-5 dark:hidden" aria-hidden="true" />
                <span className="sr-only dark:hidden">{t('switchToDark')}</span>
                <span className="sr-only hidden dark:inline">{t('switchToLight')}</span>
            </button>
            {/* Empty until mounted so the server and client agree on the initial markup; after
                that it only changes when the user actually switches, which is exactly when an
                announcement is wanted. */}
            <div role="status" aria-live="polite" aria-atomic="true" className="sr-only">
                {mounted ? t(resolvedTheme === 'dark' ? 'darkModeActive' : 'lightModeActive') : ''}
            </div>
        </>
    );
}

const OPTIONS: { value: Theme; icon: typeof Sun; labelKey: string; hintKey: string }[] = [
    { value: 'light', icon: Sun, labelKey: 'light', hintKey: 'lightHint' },
    { value: 'dark', icon: Moon, labelKey: 'dark', hintKey: 'darkHint' },
    { value: 'system', icon: Monitor, labelKey: 'system', hintKey: 'systemHint' },
];

/**
 * Full three-way picker, for settings.
 *
 * Unlike the toolbar toggle this one shows the stored *preference*, so 'System' can be
 * selected and read back — a two-state toggle can never express it. That distinction is the
 * whole reason the provider keeps `theme` and `resolvedTheme` separate.
 *
 * Selection is marked with a border, a fill, an icon treatment AND aria-checked, never with
 * colour alone.
 */
export function ThemeSelector({ className }: { className?: string }) {
    const { theme, setTheme, resolvedTheme, mounted } = useTheme();
    const t = useTranslations('Theme');

    return (
        <div role="radiogroup" aria-label={t('colourTheme')} className={cn('grid gap-4 sm:grid-cols-3', className)}>
            {OPTIONS.map(({ value, icon: Icon, labelKey, hintKey }) => {
                // Before hydration the stored preference is genuinely unknown — it lives in
                // localStorage, which the server cannot read. Rendering nothing as selected
                // for that first frame is honest; guessing would mean showing the user a
                // choice they did not make.
                const selected = mounted && theme === value;
                return (
                    <button
                        key={value}
                        type="button"
                        role="radio"
                        aria-checked={selected}
                        onClick={() => setTheme(value)}
                        className={cn(
                            'flex flex-col items-start gap-3 rounded-2xl border p-5 text-left transition-all duration-200',
                            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
                            selected
                                ? 'border-primary bg-brand-subtle'
                                : 'border-border bg-card hover:border-border-strong hover:bg-accent'
                        )}
                    >
                        <span
                            className={cn(
                                'inline-flex h-10 w-10 items-center justify-center rounded-xl transition-colors',
                                selected ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground-secondary'
                            )}
                        >
                            <Icon className="h-5 w-5" aria-hidden="true" />
                        </span>
                        <span>
                            <span className="block text-sm font-semibold text-foreground">{t(labelKey)}</span>
                            <span className="mt-0.5 block text-xs text-foreground-muted">
                                {t(hintKey)}
                                {/* Naming what 'System' currently resolves to turns an abstract
                                    setting into an observable one. */}
                                {value === 'system' && mounted && theme === 'system'
                                    ? ` — ${t('resolvedNow', { mode: t(resolvedTheme) })}`
                                    : ''}
                            </span>
                        </span>
                    </button>
                );
            })}
        </div>
    );
}
