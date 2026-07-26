"use client";

import React, { createContext, useContext, useEffect, useState } from 'react';

type Theme = 'light' | 'dark' | 'system';

interface ThemeContextType {
    theme: Theme;
    setTheme: (theme: Theme) => void;
    resolvedTheme: 'light' | 'dark';
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const [theme, setThemeState] = useState<Theme>('system');
    const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light');
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        setMounted(true);
        const storedTheme = localStorage.getItem('theme') as Theme | null;
        if (storedTheme) {
            setThemeState(storedTheme);
        }
    }, []);

    useEffect(() => {
        if (!mounted) return;

        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        
        const applyTheme = () => {
            const root = window.document.documentElement;
            root.classList.remove('light', 'dark');

            let effectiveTheme = theme;
            if (theme === 'system') {
                effectiveTheme = mediaQuery.matches ? 'dark' : 'light';
            }

            root.classList.add(effectiveTheme);
            setResolvedTheme(effectiveTheme as 'light' | 'dark');
            localStorage.setItem('theme', theme);
        };

        applyTheme();

        const listener = () => {
            if (theme === 'system') applyTheme();
        };

        mediaQuery.addEventListener('change', listener);
        return () => mediaQuery.removeEventListener('change', listener);
    }, [theme, mounted]);

    const setTheme = (newTheme: Theme) => {
        setThemeState(newTheme);
    };

    // Prevent hydration mismatch by rendering nothing until mounted (or returning children without theme classes)
    if (!mounted) {
        return (
            <ThemeContext.Provider value={{ theme: 'system', setTheme: () => {}, resolvedTheme: 'light' }}>
                {children}
            </ThemeContext.Provider>
        );
    }

    return (
        <ThemeContext.Provider value={{ theme, setTheme, resolvedTheme }}>
            {/* aria-live region announces theme changes to screen readers */}
            <div
                role="status"
                aria-live="polite"
                aria-atomic="true"
                className="sr-only"
            >
                {mounted ? `${resolvedTheme === 'dark' ? 'Dark' : 'Light'} mode active` : ''}
            </div>
            {children}
        </ThemeContext.Provider>
    );
}

export function useTheme() {
    const context = useContext(ThemeContext);
    if (context === undefined) {
        throw new Error('useTheme must be used within a ThemeProvider');
    }
    return context;
}
