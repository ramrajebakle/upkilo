/**
 * The tenant's white-label brand colour, applied as CSS variables.
 *
 * This derivation was written out by hand in three places — the public booking page, the
 * client portal and the booking widget — each computing the hover shade with its own copy of
 * the same arithmetic. They had already drifted: the portal set `--primary-color` and
 * `--primary-color-hover` but not `--primary-color-light`, so every element styled with
 * `bg-[var(--primary-color-light)]` on that page fell back to transparent.
 *
 * One function, so a fourth caller cannot invent a fifth variant.
 *
 * The default values live in globals.css, which matters: before that, a tenant with no colour
 * configured got `var(--primary-color)` resolving to nothing, and every brand-coloured button
 * on their booking page rendered transparent with an invisible white label.
 */

const VARS = ['--primary-color', '--primary-color-hover', '--primary-color-light', '--primary-color-foreground'] as const;

/** #abc and #aabbcc, with or without the hash. Returns null for anything else. */
function parseHex(input: string): { r: number; g: number; b: number } | null {
    const hex = input.trim().replace(/^#/, '');
    const full = hex.length === 3 ? hex.replace(/./g, (c) => c + c) : hex;
    if (!/^[0-9a-f]{6}$/i.test(full)) return null;
    return {
        r: parseInt(full.slice(0, 2), 16),
        g: parseInt(full.slice(2, 4), 16),
        b: parseInt(full.slice(4, 6), 16),
    };
}

/** WCAG relative luminance. */
function luminance({ r, g, b }: { r: number; g: number; b: number }): number {
    const f = (c: number) => {
        const v = c / 255;
        return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
}

const contrast = (a: number, b: number) => (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);

/** WCAG AA for normal-size text. */
const AA_TEXT = 4.5;

const ON_LIGHT = '#000000';
const ON_DARK = '#ffffff';

/**
 * Pick the label colour for a fill the tenant chose, not one we did.
 *
 * A tenant is free to pick amber or lime, and white text on either is unreadable — the
 * hardcoded `text-white` beside every `bg-[var(--primary-color)]` was a bet that every tenant
 * would choose something dark. Measuring both candidates against the actual fill is the only
 * way to keep the label legible without constraining what they may choose.
 */
export function foregroundFor(hex: string): string {
    const rgb = parseHex(hex);
    if (!rgb) return ON_DARK;
    const l = luminance(rgb);
    // White wherever white actually clears AA, because that is the convention for a brand
    // button and looks like one. Only when it does not — indigo-500 reaches 4.45:1, violet
    // 4.20:1, rose 3.76:1 — does it fall back to the darker label, which those three hues
    // measure 4.7-5.6:1 against.
    //
    // The dark candidate is pure black, not the usual near-black ink: on a mid-tone fill the
    // two are close enough that the ~0.7:1 the ink gives away is the difference between
    // clearing AA and not (#111120 on indigo is 3.97:1; #000000 is 4.71:1).
    if (contrast(l, 1) >= AA_TEXT) return ON_DARK;
    return contrast(l, 0) >= contrast(l, 1) ? ON_LIGHT : ON_DARK;
}

/**
 * Best achievable contrast between a fill and its label.
 *
 * Some perfectly reasonable brand colours cannot clear 4.5:1 against EITHER white or black —
 * indigo-500 tops out at 4.45:1, mid amber and lime are worse. That is a property of the hue,
 * not a bug to be fixed by picking harder, so the honest thing is to surface it: the accent
 * picker warns rather than silently shipping a button whose label fails AA on every booking
 * page the tenant sends out.
 */
export function bestContrast(hex: string): number {
    const rgb = parseHex(hex);
    if (!rgb) return 21;
    const l = luminance(rgb);
    // Must use the SAME two candidates foregroundFor picks from, or it reports a figure the
    // page never actually achieves — it read 4.70:1 for indigo while the rendered button was
    // 3.97:1, which is precisely the kind of reassuring-but-wrong number this is meant to catch.
    return Math.max(contrast(l, 1), contrast(l, 0));
}

/**
 * Write the tenant brand onto <html>. Pass null/undefined to clear it and fall back to the
 * product's own brand, which is what the defaults in globals.css provide.
 */
export function applyTenantBrand(color: string | null | undefined): void {
    if (typeof document === 'undefined') return;
    const root = document.documentElement;

    const rgb = color ? parseHex(color) : null;
    if (!rgb) {
        // Removing the properties is not the same as setting them empty: removal lets the
        // stylesheet default apply, whereas an empty inline value would win and resolve to
        // nothing — the transparent-button bug, reintroduced.
        for (const v of VARS) root.style.removeProperty(v);
        return;
    }

    const { r, g, b } = rgb;
    const shade = (n: number) => Math.max(0, n - 20);
    root.style.setProperty('--primary-color', color!);
    root.style.setProperty('--primary-color-hover', `rgb(${shade(r)}, ${shade(g)}, ${shade(b)})`);
    root.style.setProperty('--primary-color-light', `rgba(${r}, ${g}, ${b}, 0.1)`);
    root.style.setProperty('--primary-color-foreground', foregroundFor(color!));
}
