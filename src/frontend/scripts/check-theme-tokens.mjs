#!/usr/bin/env node
/**
 * CI guard for the theme system. Exits non-zero on any regression.
 *
 * Four checks, each one a bug this codebase actually shipped:
 *
 *   1. DEAD TOKENS — a colour utility whose theme token was never declared. Tailwind v4
 *      emits nothing for these, so the element renders with no background, border or text
 *      colour at all and the build stays green. There were 601 of them, including the
 *      entire Radix Select (`bg-popover` was undeclared, so the dropdown was transparent).
 *
 *   2. INVERTED DARK PAIRS — `text-slate-400 dark:text-slate-500`. Text must get lighter in
 *      dark mode, not darker. 414 of these existed, and they are the hardest to spot in
 *      review because the presence of a `dark:` twin reads as "handled".
 *
 *   3. PINNED PAIRS BROKEN — a theme-following token on a fixed-colour fill, or the reverse
 *      (`bg-amber-400 text-foreground`). Fails in exactly one theme, so it passes whichever
 *      one the author was looking at.
 *
 *   4. THEME TEXT IN A PERMANENTLY-DARK PANEL — a `text-foreground*` token nested inside a
 *      `bg-slate-900` hero, floating action bar or footer. The panel is dark in both themes,
 *      so its foreground has to be light in both; a theme-following token there is correct in
 *      dark mode and 3.2:1 in light. This is check 3's problem one level up the tree, and it
 *      is what a class-string-scoped rule cannot see.
 *
 *   5. NEW LIGHT-ONLY MARKUP — a file with a meaningful amount of `bg-white` /
 *      `text-slate-900` / `border-gray-200` and no `dark:` variant anywhere. This is a
 *      warning rather than an error, because a genuinely light-only surface is legitimate
 *      (a printable invoice, a fixed brand band); it exists so that adding one is a
 *      decision rather than an accident.
 *
 * Usage:  node scripts/check-theme-tokens.mjs
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const ROOT = process.cwd();
const CSS = readFileSync(join(ROOT, 'app/globals.css'), 'utf8');

const declared = new Set([...CSS.matchAll(/--color-([a-zA-Z0-9-]+)\s*:/g)].map((m) => m[1]));
// `shadow-*` resolves against its own namespace, not the colour one, so --shadow-glow has to
// be read separately or every use of it looks like a dead colour token.
const declaredShadow = new Set([...CSS.matchAll(/--shadow-([a-zA-Z0-9-]+)\s*:/g)].map((m) => m[1]));
const HUES = ['red', 'orange', 'amber', 'yellow', 'lime', 'green', 'emerald', 'teal', 'cyan', 'sky', 'blue', 'indigo', 'violet', 'purple', 'fuchsia', 'pink', 'rose', 'slate', 'gray', 'zinc', 'neutral', 'stone'];
const STEPS = ['50', '100', '200', '300', '400', '500', '600', '700', '800', '900', '950'];
for (const h of HUES) for (const s of STEPS) declared.add(`${h}-${s}`);
for (const k of ['white', 'black', 'transparent', 'current', 'inherit']) declared.add(k);

// Utility prefixes that resolve against the colour namespace, with the non-colour values
// that legitimately share each prefix (text-sm, border-2, shadow-lg …).
const NON_COLOUR = {
    text: /^(xs|sm|base|lg|xl|[2-9]xl|left|right|center|justify|start|end|balance|pretty|wrap|nowrap|clip|ellipsis|\[.*\])$/,
    bg: /^(none|cover|contain|center|top|bottom|left|right|fixed|local|scroll|repeat.*|no-repeat|auto|origin-.*|clip-.*|blend-.*|gradient-to-.*|linear-.*|radial-.*|conic-.*|\[.*\])$/,
    // `color` is here because `border-color` is a CSS property that turns up inside
    // transition values ("border-color 0.2s ease"); it is never a Tailwind utility.
    border: /^([xyseltrb]?-?\d+|none|solid|dashed|dotted|double|hidden|collapse|separate|color|[xyseltrb]|spacing.*|[xyseltrb]-\[.*\]|\[.*\])$/,
    ring: /^(\d+|inset|offset-\d+|\[.*\])$/,
    shadow: /^(2?xs|sm|md|lg|xl|[2-9]xl|inner|none|ai|\[.*\])$/,
    outline: /^(\d+|none|dashed|dotted|double|hidden|solid|offset-\d+|\[.*\])$/,
    divide: /^([xy]|\d+|solid|dashed|dotted|double|none|[xy]-reverse|\[.*\])$/,
    from: /^(\d+%|\[.*\])$/, via: /^(\d+%|\[.*\])$/, to: /^(\d+%|\[.*\])$/,
    fill: /^(none|\[.*\])$/, stroke: /^(\d+|none|\[.*\])$/,
    accent: /^(auto|\[.*\])$/, caret: /^\[.*\]$/, placeholder: /^\[.*\]$/,
    decoration: /^(slice|clone|solid|double|dotted|dashed|wavy|auto|from-font|\d+|\[.*\])$/,
};
const PREFIXES = Object.keys(NON_COLOUR);
const VARIANT = /^(group-[\w[\]-]+|peer-[\w[\]-]+|dark|hover|focus|focus-visible|focus-within|active|disabled|visited|checked|indeterminate|required|invalid|first|last|only|odd|even|empty|target|open|motion-reduce|motion-safe|sm|md|lg|xl|2xl|min-\[.*\]|max-\[.*\]|rtl|ltr|print|before|after|placeholder|file|marker|selection|backdrop|supports-\[.*\]|has-\[.*\]|aria-[\w[\]-]+|data-\[.*\]|\[&.*\])$/;

// `primary` and `ai` belong here: a NUMBERED brand step (text-primary-950) is a fixed
// colour exactly like amber-900 is, and pins its background the same way. The bare
// `text-primary` is theme-following and is excluded by the -(700|800|900|950) suffix.
const FIXED_HUE = 'amber|yellow|orange|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|red|primary|ai';
// Both the fixed patterns and the theme-following ones capture their variant prefix, so
// the comparison below is prefix-to-prefix. `hover:bg-white hover:text-green-900` is a
// fixed PAIR (one state, both halves), while `hover:bg-primary-600 text-foreground-muted`
// is not - the fill only exists on hover and the resting text may follow the theme.
const BOUNDARY = "(?:^|[\\s'\"\\x60])";   // start, whitespace, or a quote of any kind
const FIXED_HUE_TEXT = new RegExp(`${BOUNDARY}((?:[a-z0-9-]+:)*)text-(?:${FIXED_HUE})-(?:700|800|900|950)(?![\\w-])`, 'g');
const FIXED_HUE_FILL = new RegExp(`${BOUNDARY}((?:[a-z0-9-]+:)*)bg-(?:(?:${FIXED_HUE})-[2-9]00|black|slate-[89]\d0|gray-[89]\d0|neutral-[89]\d0)(?![\w-])`, 'g');
const FOLLOWS_THEME_BG = new RegExp(`${BOUNDARY}((?:[a-z0-9-]+:)*)bg-(?:card|muted|accent|background|popover)(?![\w-])`, 'g');
const FOLLOWS_THEME_TEXT = new RegExp(`${BOUNDARY}((?:[a-z0-9-]+:)*)text-foreground(?:-secondary|-muted)?(?![\w-])`, 'g');

/** The set of variant prefixes a pattern appears under; '' means unprefixed. */
const prefixesOf = (re, seg) => new Set([...seg.matchAll(re)].map((m) => m[1] ?? ''));
const shareAPrefix = (a, b) => [...a].some((p) => b.has(p));
const INVERTED = /\btext-(?:slate|gray|zinc|neutral|stone)-(\d{3})\s+dark:text-(?:slate|gray|zinc|neutral|stone)-(\d{3})\b/g;
const LIGHT_ONLY = /\b(?:bg-white|bg-slate-50|bg-gray-50|text-slate-900|text-gray-900|text-slate-600|text-gray-600|border-slate-200|border-gray-200)\b/g;

const SKIP = new Set(['node_modules', '.next', '.git', 'playwright-report', 'test-results', 'scripts', 'tests']);
function walk(dir, out = []) {
    for (const e of readdirSync(dir)) {
        if (SKIP.has(e)) continue;
        const p = join(dir, e);
        if (statSync(p).isDirectory()) walk(p, out);
        else if (/\.(tsx|jsx)$/.test(p)) out.push(p);
    }
    return out;
}
const commentRanges = (src) => {
    const r = [];
    for (const m of src.matchAll(/\/\*[\s\S]*?\*\//g)) r.push([m.index, m.index + m[0].length]);
    for (const m of src.matchAll(/(^|[^:])\/\/[^\n]*/g)) {
        const at = m.index + m[0].indexOf('//');
        if (!r.some(([a, b]) => at >= a && at < b)) r.push([at, m.index + m[0].length]);
    }
    return r;
};

// bg-[#0a0a0a] and friends: an arbitrary-value dark background is just as permanent as a
// named one, and the escalation queue paints its whole page that way. Without this the
// scan read that page as themed and let theme-following text sit on it at 3.5:1.
const OPEN_DARK = /\bbg-(?:gray|slate|neutral|zinc)-(?:8|9)\d0\b|\bbg-black\b|bg-\[#[0-2][0-9a-fA-F]{2,5}\]/;

/**
 * A dark background that only applies in dark mode is not a permanently-dark container.
 *
 * Stripping the whole `dark:`-prefixed token is what handles the chained form: a lookbehind
 * for `dark:` immediately before `bg-` misses `dark:hover:bg-slate-950`, whose nearest prefix
 * is `hover:`. That one omission made every themed table row read as a fixed dark panel and
 * exempted its cells from the nesting check.
 */
const withoutDarkVariants = (line) => line.replace(/\bdark:[\w:[\]&<>/.,%#()-]*/g, '');
/**
 * A light surface below a dark one re-lights the subtree, so the nesting scan stops there.
 *
 * Translucent forms (`bg-white/5`) are deliberately INCLUDED. Excluding them is defensible
 * in theory - a 5% white scrim over a dark panel does not make it light - but it was tried
 * and produced 239 findings that a full browser sweep of both themes measured as correct.
 * The indentation model is not precise enough to carry that distinction, and a check that
 * cries wolf 239 times is worse than one that occasionally stays quiet.
 */
const OPEN_LIGHT = /\bbg-(?:card|background|muted|white|popover)\b|\bbg-(?:gray|slate)-(?:50|100)\b/;
const THEME_TEXT = /\btext-foreground(?:-secondary|-muted)?\b/;

const errors = { dead: [], inverted: [], pinned: [], nested: [] };
const warnings = [];

for (const file of walk(ROOT)) {
    const src = readFileSync(file, 'utf8');
    const rel = relative(ROOT, file).replace(/\\/g, '/');
    const comments = commentRanges(src);
    const inComment = (i) => comments.some(([a, b]) => i >= a && i < b);
    const lineAt = (i) => src.slice(0, i).split('\n').length;

    // 1. dead tokens
    for (const m of src.matchAll(/(?<![\w:/-])((?:[a-z0-9-]+:)*)([a-z]+)-([a-z][a-zA-Z0-9]*(?:-[a-zA-Z0-9]+)*)(?:\/[\w.[\]%]+)?(?![\w-])/g)) {
        if (inComment(m.index)) continue;
        // `border-radius:16px` inside a CSS-in-JS object, an embed snippet or a <style>
        // string is a CSS property, not a class. A Tailwind utility is never immediately
        // followed by a colon; a CSS declaration always is. (`dark:` and friends are already
        // consumed as the variant prefix by the time we get here.)
        if (src[m.index + m[0].length] === ':') continue;
        const [, prefixRaw, prefix, rawName] = m;
        if (!PREFIXES.includes(prefix)) continue;
        const variants = prefixRaw ? prefixRaw.slice(0, -1).split(':') : [];
        if (variants.some((v) => !VARIANT.test(v))) continue;
        if (NON_COLOUR[prefix].test(rawName)) continue;
        // ring-offset-<colour> and border-<side>-<colour> resolve against the same namespace.
        const name = rawName.replace(/^offset-/, '').replace(/^([xyseltrb])-/, '');
        if (prefix === 'shadow' && (declaredShadow.has(rawName) || declaredShadow.has(name))) continue;
        if (declared.has(rawName) || declared.has(name)) continue;
        errors.dead.push(`${rel}:${lineAt(m.index)}  ${prefix}-${rawName}`);
    }

    // 2. inverted dark pairs
    for (const m of src.matchAll(INVERTED)) {
        if (inComment(m.index)) continue;
        if (Number(m[2]) >= Number(m[1])) errors.inverted.push(`${rel}:${lineAt(m.index)}  ${m[0]}`);
    }

    // 3. pinned pairs broken
    for (const m of src.matchAll(/(["'`])([^"'`\n]{0,800}?)\1/g)) {
        if (inComment(m.index)) continue;
        // Dark variants are stripped first: `bg-slate-100 dark:bg-slate-800` is a properly
        // themed pair, not a fixed dark fill, so `text-foreground-secondary` on it is right.
        // Without this, every correctly-themed chip in the app read as a broken pair.
        const seg = withoutDarkVariants(m[2]);
        if (!seg) continue;
        const fixedText = prefixesOf(FIXED_HUE_TEXT, seg);
        const fixedFill = prefixesOf(FIXED_HUE_FILL, seg);
        if (shareAPrefix(fixedText, prefixesOf(FOLLOWS_THEME_BG, seg))) {
            errors.pinned.push(`${rel}:${lineAt(m.index)}  theme-following background under fixed text: ${seg.trim().slice(0, 100)}`);
        } else if (shareAPrefix(fixedFill, prefixesOf(FOLLOWS_THEME_TEXT, seg))) {
            errors.pinned.push(`${rel}:${lineAt(m.index)}  theme-following text on fixed fill: ${seg.trim().slice(0, 100)}`);
        }
    }

    // 4. theme-following text nested inside a permanently-dark container.
    // Indentation-based nesting: crude, but reliable on this codebase's formatting, and it is
    // the only way to see a relationship that spans elements rather than one class string.
    //
    // A STACK, not a single indent. A dark panel routinely contains a second dark element (an
    // icon well, a nested card); tracking only the innermost one meant that well's closing
    // popped the outer panel too, and every sibling after it looked like it was on the page.
    // That is how the payments panel's body copy stayed theme-following at 3.4:1.
    {
        const darkStack = [];
        const srcLines = src.split(/\r?\n/);
        for (let i = 0; i < srcLines.length; i++) {
            const line = srcLines[i];
            const stripped = line.trimStart();
            if (!stripped) continue;
            const indent = line.length - stripped.length;
            while (darkStack.length && indent <= darkStack[darkStack.length - 1]) darkStack.pop();
            // A dark class inside a ternary is a CONDITIONAL state of one element, not a
            // container that everything below sits in — `status === 'ok' ? 'bg-emerald-500'
            // : 'bg-slate-800'` on a 48px avatar was making the whole table row look like a
            // dark panel and exempting its cells.
            const conditional = /\?\s*['"`]|:\s*['"`][^'"`]*bg-/.test(line);
            const bare = withoutDarkVariants(line);
            if (!conditional && OPEN_DARK.test(bare)) { darkStack.push(indent); continue; }
            // A light surface re-lights the subtree below it.
            if (darkStack.length && OPEN_LIGHT.test(bare)) { darkStack.length = 0; continue; }
            if (darkStack.length && THEME_TEXT.test(line)) {
                errors.nested.push(`${rel}:${i + 1}  ${stripped.slice(0, 110)}`);
            }
        }
    }

    // 5. new light-only files
    const lightOnly = (src.match(LIGHT_ONLY) || []).length;
    if (lightOnly >= 12 && !/\bdark:/.test(src)) warnings.push(`${rel}  (${lightOnly} light-only utilities, no dark: variant)`);
}

let failed = false;
const report = (title, rows, hint) => {
    if (!rows.length) return;
    failed = true;
    console.error(`\n✖ ${title} — ${rows.length}`);
    console.error(`  ${hint}`);
    for (const r of rows.slice(0, 40)) console.error(`    ${r}`);
    if (rows.length > 40) console.error(`    … and ${rows.length - 40} more`);
};

report('Colour utilities with no theme token', errors.dead,
    'These compile to nothing. Declare the token in app/globals.css @theme, or use an existing one.');
report('Dark-mode text that goes darker', errors.inverted,
    'Use text-foreground-secondary / text-foreground-muted, or run: node scripts/migrate-theme-tokens.mjs --fix-inverted');
report('Broken fixed/theme colour pairs', errors.pinned,
    'A fixed fill needs a fixed foreground, and vice versa — the pair must not be half theme-following.');
report('Theme-following text inside a permanently-dark panel', errors.nested,
    'That panel is dark in both themes, so use a fixed light tone (text-slate-100/300/400).');

if (warnings.length) {
    console.warn(`\n⚠ Light-only files (${warnings.length}) — intentional? use semantic tokens if not:`);
    for (const w of warnings.slice(0, 20)) console.warn(`    ${w}`);
}

if (failed) {
    console.error('\ntheme check FAILED\n');
    process.exit(1);
}
console.log(`✓ theme check passed${warnings.length ? ` (${warnings.length} light-only warnings)` : ''}`);
