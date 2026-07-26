import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
    // Unit/component tests only. Playwright specs (*.spec.ts, tests/e2e) run via `playwright test`,
    // and the Pact contract test runs against a live provider — neither belongs in the vitest run.
    include: ['tests/**/*.test.{ts,tsx}'],
    exclude: [
      'node_modules',
      'tests/e2e/**',
      'tests/**/*.spec.{ts,tsx}',
      'tests/api-contract.test.ts',
    ],
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '.'),
    },
  },
});
