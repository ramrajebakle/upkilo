/**
 * Visual regression tests using Playwright's built-in screenshot comparison.
 * Run: npx playwright test tests/visual-regression.spec.ts
 * Update baselines: npx playwright test --update-snapshots
 *
 * Snapshots are stored in tests/__snapshots__/ and committed to git.
 * CI fails if visual diffs exceed the configured threshold.
 */
import { test, expect } from '@playwright/test';

const BASE = process.env.BASE_URL ?? 'http://localhost:3000';

test.use({ viewport: { width: 1280, height: 800 } });

// ─── Landing page ─────────────────────────────────────────────────────────────

test('VR-1 — landing page above-the-fold (desktop)', async ({ page }) => {
  await page.goto(`${BASE}/en`);
  await page.waitForLoadState('networkidle');
  // Crop to above-the-fold only to reduce noise
  await expect(page).toHaveScreenshot('landing-hero-desktop.png', {
    clip: { x: 0, y: 0, width: 1280, height: 800 },
    maxDiffPixelRatio: 0.02,
  });
});

test('VR-2 — landing page above-the-fold (mobile 390px)', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${BASE}/en`);
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveScreenshot('landing-hero-mobile.png', {
    clip: { x: 0, y: 0, width: 390, height: 844 },
    maxDiffPixelRatio: 0.02,
  });
});

// ─── Login page ──────────────────────────────────────────────────────────────

test('VR-3 — login page (desktop)', async ({ page }) => {
  await page.goto(`${BASE}/en/login`);
  await page.waitForLoadState('networkidle');
  // Mask dynamic content (timestamps, etc.) to prevent flaky diffs
  await expect(page).toHaveScreenshot('login-desktop.png', {
    maxDiffPixelRatio: 0.02,
    mask: [page.locator('[data-testid="timestamp"]')],
  });
});

test('VR-4 — login page (mobile 390px)', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${BASE}/en/login`);
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveScreenshot('login-mobile.png', {
    maxDiffPixelRatio: 0.02,
  });
});

// ─── Register page ───────────────────────────────────────────────────────────

test('VR-5 — register page (desktop)', async ({ page }) => {
  await page.goto(`${BASE}/en/register`);
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveScreenshot('register-desktop.png', {
    maxDiffPixelRatio: 0.02,
  });
});

// ─── Dark mode ───────────────────────────────────────────────────────────────

test('VR-6 — login page dark mode', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' });
  await page.goto(`${BASE}/en/login`);
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveScreenshot('login-dark.png', {
    maxDiffPixelRatio: 0.02,
  });
});

// ─── Public booking widget ───────────────────────────────────────────────────

test('VR-7 — booking widget (light theme)', async ({ page }) => {
  const response = await page.goto(`${BASE}/en/book/demo`);
  if (!response || response.status() >= 400) {
    test.skip();
    return;
  }
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveScreenshot('booking-widget-light.png', {
    maxDiffPixelRatio: 0.02,
  });
});
