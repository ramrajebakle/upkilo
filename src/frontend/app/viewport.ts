import type { Viewport } from 'next';

/**
 * The browser chrome around the page — the Android status bar, Safari's toolbar tint, the
 * colour behind an overscroll bounce — is painted from `theme-color`, and nothing declared
 * one. manifest.json carried a single brand violet, so a dark-mode user got a violet band
 * above a near-black page on every mobile visit.
 *
 * Two entries with media queries is how the metadata API expresses "follow the device", and
 * the values are the two `--surface-base` tokens so the chrome and the page agree.
 *
 * It cannot read the in-app override — no metadata API can, because theme-color is resolved
 * from the served HTML before any script runs. Someone who has explicitly chosen light on a
 * dark phone still gets the dark bar. That is a one-line mismatch at the very top of the
 * viewport, rather than the full-width wrong colour it replaces.
 *
 * Shared rather than declared per-layout because `viewport` is a per-route-segment export and
 * this app has TEN root layouts: the nine standalone trees would each have silently kept the
 * old behaviour, which is the same trap the fonts and the theme class fell into.
 */
export const themedViewport: Viewport = {
    themeColor: [
        { media: '(prefers-color-scheme: light)', color: '#ffffff' },
        { media: '(prefers-color-scheme: dark)', color: '#0b1120' },
    ],
};
