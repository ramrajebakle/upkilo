import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:3000';

export default defineConfig({
  // './tests', not './tests/e2e': accessibility.spec.ts and visual-regression.spec.ts sit
  // directly in tests/, so with testDir scoped to tests/e2e they were invisible to
  // Playwright. ci.yml's "Run accessibility tests" step invokes
  // `playwright test tests/accessibility.spec.ts` and got "Error: No tests found." — the
  // file exists and holds 6 WCAG 2.1 AA checks that had never once executed, despite the
  // step being declared as blocking.
  //
  // testMatch is required alongside this: tests/ also holds Vitest suites
  // (api-contract.test.ts, api-paths.test.ts, components.test.tsx) and Playwright's
  // default pattern matches *.test.ts too, so widening testDir without narrowing the
  // pattern would hand those files to the wrong runner.
  testDir: './tests',
  testMatch: '**/*.spec.ts',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [['html', { outputFolder: 'playwright-report' }], ['list']],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'on-first-retry',
    headless: true,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
});
