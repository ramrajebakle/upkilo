#!/usr/bin/env node
/**
 * Rewrites hardcoded light-mode neutrals to semantic theme tokens.
 *
 * WHY
 * Roughly half this app's screens were authored with `bg-white`, `text-slate-900` and
 * `border-slate-200` and no `dark:` counterpart anywhere. That is invisible while the theme
 * system is broken — everything is light, so light-only markup looks correct — and becomes
 * the dominant defect the moment dark mode actually works: a page of white cards on a dark
 * shell, or worse, near-black text on a near-black background.
 *
 * Adding a `dark:` twin to each of those ~2,000 utilities would double the surface area and
 * leave the next page to make the same mistake again. Pointing them at semantic tokens
 * instead means the value follows the theme with no variant at all, which is the property
 * that makes a new page correct by default.
 *
 * SAFETY
 * This is applied ONLY to files that (a) contain no `dark:` variant — so it cannot fight a
 * deliberate authored pairing — and (b) contain no permanently-dark container. (b) matters:
 * inside a dark hero band or an always-dark code panel, `text-slate-500` is correct in BOTH
 * themes, and converting it to a theme-following token would make it dark-on-dark in light
 * mode. Those files are listed by --report and left for manual work.
 *
 * Opacity modifiers (`bg-white/10`) are skipped: they are almost always a decorative scrim
 * over a gradient rather than a surface, and they are legible in both themes as-is.
 * Gradient stops (from-/via-/to-) are skipped for the same reason.
 *
 * Usage:
 *   node scripts/migrate-theme-tokens.mjs --report        list candidates, change nothing
 *   node scripts/migrate-theme-tokens.mjs --dry           show the diff stats
 *   node scripts/migrate-theme-tokens.mjs --write         apply to the safe set
 *   node scripts/migrate-theme-tokens.mjs --fix-inverted  repair dark: twins that go the
 *                                                         wrong way (darker in dark mode)
 *   node scripts/migrate-theme-tokens.mjs --force a.tsx…  apply to named files, skipping the
 *                                                         safety filters — for files a human
 *                                                         has read and cleared
 */
import { readFileSync, writeFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const ROOT = process.cwd();
const MODE = process.argv.find((a) => a.startsWith('--'))?.slice(2) ?? 'report';

/**
 * Neutral utility -> semantic token.
 *
 * The mapping compresses eleven grey steps onto four text roles and three line weights,
 * which is the point: the old code had no shared vocabulary, so `text-slate-600` and
 * `text-gray-600` meant the same thing in different files and nothing enforced either.
 */
const MAP = new Map(Object.entries({
    // Surfaces
    'bg-white': 'bg-card',
    'bg-slate-50': 'bg-muted',
    'bg-gray-50': 'bg-muted',
    'bg-slate-100': 'bg-muted',
    'bg-gray-100': 'bg-muted',

    // Text. 900/800/700 are all "the main thing on this line"; 600/500 are supporting copy;
    // 400 is the faintest tier that still has to clear 4.5:1, which --text-tertiary does and
    // slate-400 (2.6:1 on white) never did.
    'text-slate-900': 'text-foreground',
    'text-gray-900': 'text-foreground',
    'text-slate-800': 'text-foreground',
    'text-gray-800': 'text-foreground',
    'text-slate-700': 'text-foreground',
    'text-gray-700': 'text-foreground',
    'text-slate-600': 'text-foreground-secondary',
    'text-gray-600': 'text-foreground-secondary',
    'text-slate-500': 'text-foreground-secondary',
    'text-gray-500': 'text-foreground-secondary',
    'text-slate-400': 'text-foreground-muted',
    'text-gray-400': 'text-foreground-muted',

    // Lines
    'border-slate-100': 'border-border-subtle',
    'border-gray-100': 'border-border-subtle',
    'border-slate-200': 'border-border',
    'border-gray-200': 'border-border',
    'border-slate-300': 'border-border-strong',
    'border-gray-300': 'border-border-strong',
    'divide-slate-100': 'divide-border-subtle',
    'divide-gray-100': 'divide-border-subtle',
    'divide-slate-200': 'divide-border',
    'divide-gray-200': 'divide-border',
    'ring-slate-200': 'ring-border',
    'ring-gray-200': 'ring-border',

    // Placeholders
    'placeholder-slate-400': 'placeholder-foreground-muted',
    'placeholder-gray-400': 'placeholder-foreground-muted',

    // ── Brand and AI accent, as theme-resolved PAIRS ──
    // A numbered brand step is a fixed lightness picked to read on white. `text-primary-600`
    // (#4535d4) measures 2.05:1 on a dark card — it fails AA outright, and it fails for the
    // same reason in every one of its ~350 uses. `text-primary` resolves to primary-500 in
    // light and primary-400 in dark, which clears AA on both.
    //
    // The tints move with the text, and that pairing is the point: `text-primary` on
    // `bg-brand-subtle` is 5.2:1 light and 6.2:1 dark, whereas converting only one half would
    // put a light tone on a light wash. The numbered steps stay available and unchanged for
    // fills, borders and data-visualisation, where a fixed hue is what is wanted.
    'text-primary-500': 'text-primary',
    'text-primary-600': 'text-primary',
    'text-primary-700': 'text-primary',
    'bg-primary-50': 'bg-brand-subtle',
    'bg-primary-100': 'bg-brand-subtle',
    'border-primary-100': 'border-primary/25',
    'border-primary-200': 'border-primary/25',

    'text-ai-500': 'text-ai',
    'text-ai-600': 'text-ai',
    'text-ai-700': 'text-ai',
    'bg-ai-50': 'bg-ai-subtle',
    'bg-ai-100': 'bg-ai-subtle',
    'border-ai-200': 'border-ai/25',
    'border-ai-300': 'border-ai/25',
}));

/**
 * Interactive washes get --surface-accent rather than --surface-muted. They are the same
 * colour today, but they answer different questions ("is this row hovered" vs "is this
 * region inert"), and a design system that cannot tell them apart cannot later make a
 * hover state distinct without breaking every disabled control.
 */
const INTERACTIVE = new Set(['hover', 'focus', 'active', 'group-hover', 'peer-hover', 'focus-within']);
const WASH = new Map(Object.entries({
    'bg-white': 'bg-card',
    'bg-slate-50': 'bg-accent',
    'bg-gray-50': 'bg-accent',
    'bg-slate-100': 'bg-accent',
    'bg-gray-100': 'bg-accent',
}));

const FIXED_DARK = /\b(?:bg-slate-9\d0|bg-slate-8\d0|bg-gray-9\d0|bg-gray-8\d0|bg-neutral-9\d0|bg-black|from-slate-9\d0|from-slate-8\d0|via-slate-9\d0|to-slate-9\d0|from-gray-9\d0|from-neutral-9\d0|bg-\[#0|bg-\[#1|glass-dark)\b/;
const SKIP_DIRS = new Set(['node_modules', '.next', '.git', 'playwright-report', 'test-results', 'scripts']);

function walk(dir, out = []) {
    for (const entry of readdirSync(dir)) {
        if (SKIP_DIRS.has(entry)) continue;
        const p = join(dir, entry);
        if (statSync(p).isDirectory()) walk(p, out);
        else if (/\.(tsx|jsx)$/.test(p)) out.push(p);
    }
    return out;
}

/** Split `md:hover:bg-slate-50` into its variants and its base utility. */
function parts(token) {
    const bits = token.split(':');
    return { variants: bits.slice(0, -1), base: bits.at(-1) };
}

/**
 * Byte ranges occupied by comments, so prose is never rewritten.
 *
 * The obvious alternative — only rewrite inside matched quotes — silently skips the most
 * common className shape in this codebase:
 *
 *     className={`rounded-3xl bg-white ${plan.popular ? 'ring-2' : 'ring-gray-200'}`}
 *
 * A quote-matching regex cannot span that backtick literal, because the interpolation
 * contains its own single quotes. The first version of this script had exactly that bug and
 * left every templated className untouched — which reads as success (the file changed, the
 * build passed) while the cards that actually needed migrating kept their light-only markup.
 * Excluding comments and rewriting everything else is the inversion that gets those.
 */
function commentRanges(src) {
    const ranges = [];
    for (const m of src.matchAll(/\/\*[\s\S]*?\*\//g)) ranges.push([m.index, m.index + m[0].length]);
    for (const m of src.matchAll(/(^|[^:])\/\/[^\n]*/g)) {
        const at = m.index + m[0].indexOf('//');
        if (ranges.some(([a, b]) => at >= a && at < b)) continue; // '//' inside a block comment
        ranges.push([at, m.index + m[0].length]);
    }
    return ranges;
}

/**
 * A class string can pin one half of a colour pair, which pins the other half too.
 *
 *   "bg-white text-blue-900 …"     a white pill on a blue hero band
 *   "bg-amber-400 text-slate-900"  a dark label on a yellow button
 *
 * In both, the neutral is fixed *because of the brand colour beside it*, not because the page
 * is light — so converting it to a theme-following token breaks exactly one theme. These were
 * found the hard way: the geo landing pages' CTAs came out dark-on-dark at 1.7:1, and a
 * hand-fix was silently undone the next time this script ran.
 *
 * Encoding the rule rather than maintaining an ignore-list is what makes the script
 * idempotent, and what makes it get the next such page right without being told.
 *
 * The protection is directional: a fixed hue in the TEXT pins the background, and a fixed hue
 * in the FILL pins the text. Protecting the whole segment either way would skip legitimate
 * conversions on any card that happens to have a coloured hover state.
 *
 * It is also PER-VARIANT, which is the subtler half. `hover:bg-white hover:text-green-900` is
 * a fixed pair — both halves describe the same hover state — while
 * `hover:bg-primary-600 text-foreground-muted` is not, because the fill only exists on hover
 * and the resting text is free to follow the theme. Matching prefix to prefix distinguishes
 * them; treating any prefixed token as "not pinning" un-protected every geo landing CTA, and
 * treating any token as pinning flagged every correctly authored hover state.
 */
// `primary` and `ai` belong here: a NUMBERED brand step (text-primary-950) is a fixed
// colour exactly like amber-900 is, and pins its background the same way. The bare
// `text-primary` is theme-following and is excluded by the -(700|800|900|950) suffix.
const FIXED_HUE = 'amber|yellow|orange|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|red|primary|ai';
const FIXED_HUE_TEXT = new RegExp(`(?:^|[\\s'"\`])((?:[a-z0-9-]+:)*)text-(?:${FIXED_HUE})-(?:700|800|900|950)(?![\\w-])`, 'g');
const FIXED_HUE_FILL = new RegExp(`(?:^|[\\s'"\`])((?:[a-z0-9-]+:)*)bg-(?:(?:${FIXED_HUE})-[2-9]00|black|slate-[89]\\d0|gray-[89]\\d0|neutral-[89]\\d0)(?![\\w-])`, 'g');

/** The set of variant prefixes a pattern appears under, '' meaning unprefixed. */
function prefixesOf(re, seg) {
    const found = new Set();
    for (const m of seg.matchAll(re)) found.add(m[1] ?? '');
    return found;
}

/** Byte ranges of single-line quoted strings, with the variants each one pins. */
function pinnedRanges(src) {
    const out = [];
    for (const m of src.matchAll(/(["'`])([^"'`\n]{0,800}?)\1/g)) {
        const seg = m[2];
        if (!seg) continue;
        const pinsBackground = prefixesOf(FIXED_HUE_TEXT, seg);
        const pinsText = prefixesOf(FIXED_HUE_FILL, seg);
        if (pinsBackground.size || pinsText.size) {
            out.push([m.index, m.index + m[0].length, pinsBackground, pinsText]);
        }
    }
    return out;
}

function migrate(src) {
    let changes = 0;
    const comments = commentRanges(src);
    const inComment = (i) => comments.some(([a, b]) => i >= a && i < b);
    const pinned = pinnedRanges(src);
    const pinsAt = (i) => pinned.find(([a, b]) => i >= a && i < b);

    // Match a class token with any variant prefixes: `md:hover:bg-slate-50`.
    // The lookbehind stops it firing inside a longer identifier, and the lookahead stops
    // `bg-white` matching the head of `bg-white/10`.
    const NAMES = [...new Set([...MAP.keys(), ...WASH.keys()])].join('|');
    const RE = new RegExp(`(?<![\\w:/-])((?:[a-z0-9-]+:)+)?(${NAMES})(?![\\w/-])`, 'g');

    const out = src.replace(RE, (whole, prefix, base, offset) => {
        if (inComment(offset)) return whole;
        const pin = pinsAt(offset);
        if (pin) {
            const [, , pinsBackground, pinsText] = pin;
            // Compare prefix to prefix: a hover-state fill pins hover-state text, and the
            // resting fill pins the resting text. `prefix` already carries the trailing colon.
            const variant = prefix ?? '';
            if (pinsBackground.has(variant) && /^(bg|border|divide|ring)-/.test(base)) return whole;
            if (pinsText.has(variant) && /^(text|placeholder)-/.test(base)) return whole;
        }
        const variants = prefix ? prefix.slice(0, -1).split(':') : [];
        const interactive = variants.some((v) => INTERACTIVE.has(v));
        const table = interactive && WASH.has(base) ? WASH : MAP;
        const next = table.get(base);
        if (!next) return whole;
        changes++;
        return (prefix ?? '') + next;
    });
    return { out, changes };
}

/**
 * Repairs `text-slate-400 dark:text-slate-500` and its 400-odd siblings.
 *
 * These pairs read as finished work — somebody wrote a dark: twin, so the element looks
 * migrated — but the twin moves the wrong way. In dark mode text has to get LIGHTER; going
 * from slate-400 to slate-500 makes a faint label fainter against a darker ground. The
 * calendar's hour gutter measured 2.35:1 that way, and the settings pages' section labels
 * 2.63:1. A pair that is present but inverted is worse than a missing one, because nothing
 * flags it for review.
 *
 * Two shapes, treated differently:
 *   · A light step of 300 or lighter is text that must already be sitting on a dark band —
 *     slate-300 on a white page would be 1.5:1, so nobody wrote it for a light surface. The
 *     band is dark in both themes, so the fix is to delete the dark: twin, not to add a
 *     theme-following token that would go dark in light mode.
 *   · 400 and above is a muted label on a page or card surface, and becomes the semantic
 *     token, which clears AA in both themes by construction.
 */
const INVERTED = /\btext-(?:slate|gray|zinc|neutral|stone)-(\d{3})(\s+)dark:text-(?:slate|gray|zinc|neutral|stone)-(\d{3})\b/g;

function fixInverted(src) {
    let changes = 0;
    const comments = commentRanges(src);
    const pinned = pinnedRanges(src);
    const out = src.replace(INVERTED, (whole, lightStep, gap, darkStep, offset) => {
        if (comments.some(([a, b]) => offset >= a && offset < b)) return whole;
        const light = Number(lightStep), dark = Number(darkStep);
        if (dark < light) return whole; // correct direction already
        changes++;
        if (light <= 300) return whole.slice(0, whole.indexOf(gap)); // drop the twin, keep the fixed tone
        // On a fixed fill the tone is pinned too — drop the twin rather than follow the theme.
        if (pinned.some(([a, b, , pinsText]) => pinsText && offset >= a && offset < b)) {
            return whole.slice(0, whole.indexOf(gap));
        }
        return light >= 500 ? 'text-foreground-secondary' : 'text-foreground-muted';
    });
    return { out, changes };
}

const files = walk(ROOT);
const safe = [];
const needsReview = [];

for (const f of files) {
    const src = readFileSync(f, 'utf8');
    const { changes } = migrate(src);
    if (!changes) continue;
    if (/\bdark:/.test(src)) continue;            // author already made deliberate choices
    (FIXED_DARK.test(src) ? needsReview : safe).push({ f, changes, src });
}

if (MODE === 'report' || MODE === 'dry') {
    let total = 0;
    console.log(`SAFE (${safe.length} files):`);
    for (const s of safe.sort((a, b) => b.changes - a.changes)) {
        total += s.changes;
        console.log(`  ${String(s.changes).padStart(4)}  ${relative(ROOT, s.f)}`);
    }
    console.log(`\n  ${total} replacements across ${safe.length} files`);
    console.log(`\nNEEDS MANUAL REVIEW — contains permanently-dark containers (${needsReview.length} files):`);
    for (const s of needsReview.sort((a, b) => b.changes - a.changes)) {
        console.log(`  ${String(s.changes).padStart(4)}  ${relative(ROOT, s.f)}`);
    }
} else if (MODE === 'write') {
    let total = 0;
    for (const s of safe) {
        const { out, changes } = migrate(s.src);
        writeFileSync(s.f, out);
        total += changes;
    }
    console.log(`rewrote ${safe.length} files, ${total} replacements`);
    console.log(`left ${needsReview.length} files for manual review (see --report)`);
} else if (MODE === 'fix-inverted') {
    // Runs across every file, including those with dark: variants — an inverted pair is by
    // definition inside a file that already has them.
    let total = 0, touched = 0;
    for (const f of files) {
        const src = readFileSync(f, 'utf8');
        const { out, changes } = fixInverted(src);
        if (!changes) continue;
        writeFileSync(f, out);
        total += changes; touched++;
        console.log(`  ${String(changes).padStart(4)}  ${relative(ROOT, f)}`);
    }
    console.log(`repaired ${total} inverted dark-mode text pairs across ${touched} files`);
} else if (MODE === 'force') {
    // Escape hatch for files the safety filters exclude but a human has actually read: a page
    // that mixes a deliberate dark hero with light-only body markup needs the migration, and
    // needs someone to have checked which is which first.
    const targets = process.argv.slice(process.argv.indexOf('--force') + 1);
    if (!targets.length) { console.error('--force needs one or more file paths'); process.exit(1); }
    let total = 0;
    for (const t of targets) {
        const src = readFileSync(t, 'utf8');
        const { out, changes } = migrate(src);
        writeFileSync(t, out);
        total += changes;
        console.log(`  ${String(changes).padStart(4)}  ${t}`);
    }
    console.log(`forced ${targets.length} files, ${total} replacements`);
} else {
    console.error('usage: --report | --dry | --write | --fix-inverted | --force <files…>');
    process.exit(1);
}
