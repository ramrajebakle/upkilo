'use client';

import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { THEME_STORAGE_KEY } from '@/components/ThemeScript';

export type Theme = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

interface ThemeContextType {
    /** What the user chose. 'system' means "follow the OS". */
    theme: Theme;
    setTheme: (theme: Theme) => void;
    /** What is actually on screen — 'system' already resolved. */
    resolvedTheme: ResolvedTheme;
    /**
     * False during the server render and the first client render, true afterwards.
     *
     * Only needed by UI that must render *different markup* per theme (a chart library
     * that takes colours as props, for example). Anything that can express both states in
     * CSS should do that instead — `dark:` utilities are correct before hydration, because
     * ThemeScript has already put the class on <html>, whereas this flag is not.
     */
    mounted: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

const MEDIA = '(prefers-color-scheme: dark)';

/** Read what ThemeScript already applied, so the first client render agrees with the DOM. */
function readAppliedTheme(): ResolvedTheme {
    if (typeof document === 'undefined') return 'light';
    return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
}

function readStoredTheme(): Theme {
    if (typeof window === 'undefined') return 'system';
    try {
        const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
        if (stored === 'light' || stored === 'dark' || stored === 'system') return stored;
    } catch {
        /* storage blocked — fall through to system */
    }
    return 'system';
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    // Lazy initialisers, not a `useEffect` that corrects the value afterwards. The previous
    // implementation started every render at 'system'/'light' and fixed it in an effect,
    // which is why the header toggle briefly showed the wrong icon and why consumers that
    // read `resolvedTheme` during the first render got a confident, wrong answer.
    //
    // These run on the client only (the server render uses the parameter default), and by
    // the time they run ThemeScript has already resolved the theme onto <html>, so they
    // are reading a fact rather than guessing.
    const [theme, setThemeState] = useState<Theme>(readStoredTheme);
    const [resolvedTheme, setResolvedTheme] = useState<ResolvedTheme>(readAppliedTheme);
    const [mounted, setMounted] = useState(false);

    const transitionTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    /**
     * Write the resolved theme to the DOM. This is the ONLY place in the app that touches
     * the theme class — that is what makes it a single source of truth rather than a
     * suggestion.
     *
     * `animate` gates the cross-fade: it is on for a deliberate user switch, and off for
     * the initial sync and for OS-driven changes, where there is nothing to transition
     * from.
     */
    const applyTheme = useCallback((next: ResolvedTheme, animate: boolean) => {
        const root = document.documentElement;
        if (root.classList.contains(next)) {
            // Already correct (ThemeScript's work, usually). Skip the DOM writes so the
            // first mount does not invalidate style for the whole document.
            setResolvedTheme(next);
            return;
        }

        if (animate) {
            // Scoped to <html> and lasting only as long as the switch — see the
            // [data-theme-transition] block in globals.css for why this is not a permanent
            // global transition.
            root.setAttribute('data-theme-transition', '');
            if (transitionTimer.current) clearTimeout(transitionTimer.current);
            transitionTimer.current = setTimeout(() => {
                root.removeAttribute('data-theme-transition');
                transitionTimer.current = null;
            }, 200);
        }

        root.classList.remove('light', 'dark');
        root.classList.add(next);
        // Keeps native surfaces — scrollbars, date pickers, autofill, the overscroll
        // canvas — in the same theme as the page.
        root.style.colorScheme = next;
        setResolvedTheme(next);
    }, []);

    useEffect(() => {
        setMounted(true);
    }, []);

    // Keep the DOM in step with the chosen theme, and follow the OS while 'system'.
    useEffect(() => {
        const media = window.matchMedia(MEDIA);
        const resolve = (t: Theme): ResolvedTheme =>
            t === 'system' ? (media.matches ? 'dark' : 'light') : t;

        applyTheme(resolve(theme), false);

        if (theme !== 'system') return;
        const onChange = () => applyTheme(media.matches ? 'dark' : 'light', true);
        media.addEventListener('change', onChange);
        return () => media.removeEventListener('change', onChange);
    }, [theme, applyTheme]);

    const setTheme = useCallback((next: Theme) => {
        try {
            window.localStorage.setItem(THEME_STORAGE_KEY, next);
        } catch {
            /* storage blocked — the theme still applies for this session */
        }
        const resolved: ResolvedTheme =
            next === 'system'
                ? window.matchMedia(MEDIA).matches
                    ? 'dark'
                    : 'light'
                : next;
        // Apply synchronously rather than waiting for the effect, so the switch lands in
        // the same frame as the click.
        applyTheme(resolved, true);
        setThemeState(next);
    }, [applyTheme]);

    useEffect(() => {
        // A theme chosen in another tab should not leave this one stale.
        const onStorage = (e: StorageEvent) => {
            if (e.key !== THEME_STORAGE_KEY) return;
            setThemeState(readStoredTheme());
        };
        window.addEventListener('storage', onStorage);
        return () => window.removeEventListener('storage', onStorage);
    }, []);

    useEffect(() => () => {
        if (transitionTimer.current) clearTimeout(transitionTimer.current);
    }, []);

    // Memoised: this context sits above the entire app, so an unmemoised object would
    // re-render every consumer on every render of every ancestor.
    const value = useMemo(
        () => ({ theme, setTheme, resolvedTheme, mounted }),
        [theme, setTheme, resolvedTheme, mounted]
    );

    // The screen-reader announcement lives in ThemeToggle, not here. This provider is mounted
    // by RootHtml, which sits ABOVE NextIntlClientProvider and is also used by the nine root
    // layouts that have no intl provider at all — an announcement here could only ever be
    // English. It belongs with the control regardless: it exists to confirm a deliberate user
    // action, and that is where the action happens.
    return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
    const context = useContext(ThemeContext);
    if (context === undefined) {
        throw new Error('useTheme must be used within a ThemeProvider');
    }
    return context;
}
