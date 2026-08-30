import { test, expect, type Page } from '@playwright/test';

/**
 * End-to-end checks for the theme system.
 *
 * These are deliberately behavioural rather than pixel-based: a screenshot baseline tells you
 * *that* something changed, this tells you *what* is wrong. Every assertion here corresponds
 * to a failure mode that was live in the app before the theme refactor:
 *
 *   · the toggle changed CSS variables but not `dark:` utilities (they were wired to the OS)
 *   · nine root layouts had no theme at all
 *   · the theme was applied in an effect, so every load flashed light first
 *   · overlays rendered outside the themed tree
 *   · the settings page's theme picker was local state that saved nothing
 */

const ROUTES = [
    { path: '/en', name: 'marketing home' },
    { path: '/en/login', name: 'login' },
    { path: '/en/register', name: 'register' },
    { path: '/en/pricing', name: 'pricing' },
    { path: '/en/contact', name: 'contact' },
    { path: '/en/privacy-policy', name: 'privacy policy' },
    { path: '/discover', name: 'discover (own root layout)' },
    { path: '/enterprise', name: 'enterprise (own root layout)' },
    { path: '/powered-by', name: 'powered-by (own root layout)' },
    { path: '/uk', name: 'uk geo (own root layout)' },
    { path: '/au', name: 'au geo (own root layout)' },
    { path: '/ca', name: 'ca geo (own root layout)' },
    { path: '/uae', name: 'uae geo (own root layout)' },
    { path: '/offline', name: 'offline (own root layout)' },
];

/** Seed localStorage before the page's own scripts run, the way a returning user arrives. */
async function withStoredTheme(page: Page, theme: string) {
    await page.addInitScript((t) => {
        try {
            window.localStorage.setItem('theme', t);
        } catch {
            /* ignore */
        }
    }, theme);
}

function rgbToLuminance(rgb: string): number {
    const m = rgb.match(/\d+(\.\d+)?/g);
    if (!m) return -1;
    const [r, g, b] = m.slice(0, 3).map(Number);
    const lin = (c: number) => {
        const s = c / 255;
        return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
}

test.describe('theme class reaches every root layout', () => {
    for (const { path, name } of ROUTES) {
        test(`${name} honours a stored dark preference`, async ({ page }) => {
            await withStoredTheme(page, 'dark');
            await page.goto(path, { waitUntil: 'domcontentloaded' });

            // The class must be present in the very first frame — ThemeScript runs before
            // paint, so this holds without waiting for hydration.
            await expect(page.locator('html')).toHaveClass(/\bdark\b/);

            // color-scheme drives the browser's own surfaces (scrollbar, native controls).
            const scheme = await page.evaluate(() => document.documentElement.style.colorScheme);
            expect(scheme).toBe('dark');

            // The page must actually be dark, not just carry the class. This is what caught
            // the original bug: the class was applied but every `dark:` utility ignored it.
            const bg = await page.evaluate(() =>
                getComputedStyle(document.body).backgroundColor
            );
            expect(rgbToLuminance(bg), `body background on ${path} is ${bg}`).toBeLessThan(0.12);
        });

        test(`${name} honours a stored light preference`, async ({ page }) => {
            await withStoredTheme(page, 'light');
            await page.goto(path, { waitUntil: 'domcontentloaded' });
            await expect(page.locator('html')).toHaveClass(/\blight\b/);
            const bg = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
            expect(rgbToLuminance(bg), `body background on ${path} is ${bg}`).toBeGreaterThan(0.5);
        });
    }
});

test.describe('system preference', () => {
    test('follows the OS when no explicit choice is stored', async ({ page }) => {
        await page.emulateMedia({ colorScheme: 'dark' });
        await page.goto('/en', { waitUntil: 'domcontentloaded' });
        await expect(page.locator('html')).toHaveClass(/\bdark\b/);
    });

    test('an explicit light choice overrides a dark OS', async ({ page }) => {
        await page.emulateMedia({ colorScheme: 'dark' });
        await withStoredTheme(page, 'light');
        await page.goto('/en', { waitUntil: 'domcontentloaded' });
        await expect(page.locator('html')).toHaveClass(/\blight\b/);
        await expect(page.locator('html')).not.toHaveClass(/\bdark\b/);
    });
});

test.describe('no flash of the wrong theme', () => {
    test('the theme class is set before any page content is parsed', async ({ page }) => {
        await withStoredTheme(page, 'dark');

        // This asserts the ordering invariant directly: ThemeScript is the first child of
        // <body>, so by the time ANY other element has been parsed into the document, the
        // class must already be on <html>. If the theme were applied in a React effect — the
        // bug this replaces — the observer would capture an unthemed root.
        //
        // An earlier version sampled at the first requestAnimationFrame instead, which is a
        // race rather than an invariant: rAF can fire before the parser reaches the script on
        // a loaded machine, and the test failed while the product was correct.
        await page.addInitScript(() => {
            const w = window as unknown as { __classAtFirstContent?: string };
            new MutationObserver((records, observer) => {
                for (const record of records) {
                    // Only nodes appended directly to <body> count. Watching the whole
                    // document fires first on <head> and its meta/link tags, which are parsed
                    // before the theme script and would report an empty class every time.
                    if (record.target !== document.body) continue;
                    for (const node of Array.from(record.addedNodes)) {
                        if (node.nodeType !== Node.ELEMENT_NODE) continue;
                        const el = node as Element;
                        // Only nodes that actually PAINT count. Next.js opens the streamed
                        // body with a `<div hidden>` marker and ThemeScript sits after it, so
                        // requiring "the very first element" would fail on a document that
                        // has rendered nothing yet — a stricter reading than the invariant.
                        if (/^(SCRIPT|STYLE|LINK|TEMPLATE|META|TITLE)$/.test(el.tagName)) continue;
                        if (el.hasAttribute('hidden')) continue;
                        w.__classAtFirstContent = document.documentElement.className;
                        observer.disconnect();
                        return;
                    }
                }
            }).observe(document, { childList: true, subtree: true });
        });

        await page.goto('/en', { waitUntil: 'load' });
        const atFirstContent = await page.evaluate(
            () => (window as unknown as { __classAtFirstContent?: string }).__classAtFirstContent ?? ''
        );
        expect(
            atFirstContent,
            'the theme class was not on <html> by the time page content began parsing'
        ).toContain('dark');
    });

    test('no hydration error is logged', async ({ page }) => {
        const errors: string[] = [];
        page.on('console', (m) => {
            if (m.type() === 'error') errors.push(m.text());
        });
        await withStoredTheme(page, 'dark');
        await page.goto('/en', { waitUntil: 'networkidle' });
        const hydration = errors.filter((e) => /hydrat|did not match|server HTML/i.test(e));
        expect(hydration, hydration.join('\n')).toHaveLength(0);
    });
});

test.describe('persistence', () => {
    test('survives a reload and a client-side navigation', async ({ page }) => {
        await withStoredTheme(page, 'dark');
        await page.goto('/en', { waitUntil: 'domcontentloaded' });
        await expect(page.locator('html')).toHaveClass(/\bdark\b/);

        await page.reload({ waitUntil: 'domcontentloaded' });
        await expect(page.locator('html')).toHaveClass(/\bdark\b/);

        await page.goto('/en/pricing', { waitUntil: 'domcontentloaded' });
        await expect(page.locator('html')).toHaveClass(/\bdark\b/);
    });
});

test.describe('dark: utilities respond to the class, not to the OS', () => {
    // This is the regression test for the single largest defect: Tailwind v4 defaults the
    // `dark:` variant to prefers-color-scheme, so with a LIGHT OS and a DARK app preference
    // every dark: utility in the app used to stay in its light state.
    test('a light OS with a dark app preference still renders dark surfaces', async ({ page }) => {
        await page.emulateMedia({ colorScheme: 'light' });
        await withStoredTheme(page, 'dark');
        await page.goto('/en/login', { waitUntil: 'networkidle' });

        const probe = await page.evaluate(() => {
            const el = document.createElement('div');
            el.className = 'bg-white dark:bg-slate-900';
            document.body.appendChild(el);
            const bg = getComputedStyle(el).backgroundColor;
            el.remove();
            return bg;
        });
        // slate-900 is #0f172a; white is #ffffff. If the variant were still media-based this
        // would come back white on a light-OS machine.
        expect(rgbToLuminance(probe), `dark:bg-slate-900 resolved to ${probe}`).toBeLessThan(0.12);
    });
});

test.describe('previously-dead colour tokens now resolve', () => {
    // Every class below compiled to nothing before the token layer existed, so the elements
    // using them rendered with no background, border or text colour at all.
    const CLASSES = [
        'bg-card', 'bg-popover', 'bg-muted', 'bg-primary', 'bg-background', 'bg-destructive',
        'bg-ai-50', 'bg-success-50', 'bg-warning-50', 'bg-danger-50', 'bg-info-50',
        'bg-surface-base', 'bg-brand-subtle', 'bg-success-surface', 'bg-danger-surface',
    ];
    const TEXT = ['text-foreground', 'text-muted-foreground', 'text-primary', 'text-ai-600', 'text-danger-fg'];
    const BORDER = ['border-border', 'border-input', 'border-slate-850', 'border-ai-300', 'border-success-border'];

    test('background, text and border tokens all emit real colours', async ({ page }) => {
        await page.goto('/en', { waitUntil: 'networkidle' });
        const dead = await page.evaluate(
            ({ CLASSES, TEXT, BORDER }) => {
                const out: string[] = [];
                const probe = (cls: string, prop: string, deadValue: string) => {
                    const el = document.createElement('div');
                    el.className = `${cls} border-solid border`;
                    document.body.appendChild(el);
                    const v = getComputedStyle(el).getPropertyValue(prop);
                    el.remove();
                    if (v === deadValue || v === '') out.push(`${cls} -> ${prop}: ${v || '(empty)'}`);
                };
                CLASSES.forEach((c) => probe(c, 'background-color', 'rgba(0, 0, 0, 0)'));
                TEXT.forEach((c) => probe(c, 'color', ''));
                BORDER.forEach((c) => probe(c, 'border-top-color', 'rgb(0, 0, 0)'));
                return out;
            },
            { CLASSES, TEXT, BORDER }
        );
        expect(dead, `these utilities still resolve to nothing:\n${dead.join('\n')}`).toHaveLength(0);
    });
});

test.describe('overlays rendered through portals inherit the theme', () => {
    test('a Radix/portal surface is dark when the app is dark', async ({ page }) => {
        await withStoredTheme(page, 'dark');
        await page.goto('/en', { waitUntil: 'networkidle' });

        // Portals mount into <body>, which is a descendant of the themed <html>. The
        // `.dark *` half of the custom variant is what makes this hold.
        const bg = await page.evaluate(() => {
            const host = document.createElement('div');
            host.className = 'bg-popover text-popover-foreground';
            document.body.appendChild(host);
            const v = getComputedStyle(host).backgroundColor;
            host.remove();
            return v;
        });
        expect(rgbToLuminance(bg), `portal surface resolved to ${bg}`).toBeLessThan(0.12);
    });
});

test.describe('responsive', () => {
    for (const [name, size] of Object.entries({
        'small mobile': { width: 320, height: 640 },
        mobile: { width: 390, height: 844 },
        tablet: { width: 768, height: 1024 },
        laptop: { width: 1280, height: 800 },
        desktop: { width: 1920, height: 1080 },
    })) {
        test(`dark theme holds at ${name}`, async ({ page }) => {
            await page.setViewportSize(size);
            await withStoredTheme(page, 'dark');
            await page.goto('/en', { waitUntil: 'domcontentloaded' });
            await expect(page.locator('html')).toHaveClass(/\bdark\b/);
            const bg = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
            expect(rgbToLuminance(bg)).toBeLessThan(0.12);
        });
    }
});
