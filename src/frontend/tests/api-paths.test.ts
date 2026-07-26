import { describe, it, expect } from 'vitest';
import { readFileSync, readdirSync, statSync } from 'fs';
import { join, resolve } from 'path';

/**
 * Static regression guard for frontend→backend route mismatches.
 *
 * Each entry is a path pattern that was verified to NOT exist on the backend
 * (see the audit). The frontend must use the corrected path instead. If any of
 * these legacy patterns reappears in source, a call will 404 at runtime — so we
 * fail the build here rather than in production.
 */
const FORBIDDEN: Array<{ pattern: RegExp; reason: string }> = [
  { pattern: /['"`]\/api\/analytics\//, reason: 'analytics is versioned: use /api/v1/analytics/*' },
  { pattern: /\/api\/v1\/adminbilling\b/, reason: 'backend route is /api/v1/admin/billing/*' },
  { pattern: /\/api\/v1\/revenuetracking\b/, reason: 'backend route is /api/v1/admin/revenue/*' },
  { pattern: /\/api\/v1\/marketing-funnels\b/, reason: 'backend controller is marketingfunnels (no hyphen)' },
  { pattern: /\/api\/v1\/sales-pipeline\/deals\b/, reason: 'backend controller is salespipeline (no hyphen)' },
  { pattern: /\/api\/v1\/promocodes\b/, reason: 'backend route is /api/v1/coupons/*' },
  { pattern: /\/api\/v1\/landingpages\b/, reason: 'backend route is /api/landing-pages/* (hyphen, no v1)' },
  { pattern: /\/api\/v1\/schedule-blocks\b/, reason: 'backend route is /api/schedule-blocks (no v1)' },
  { pattern: /\/api\/v1\/ai\/image['"`]/, reason: 'backend route is /api/v1/ai/generate-image' },
  { pattern: /\/api\/v1\/ai\/sentiment['"`]/, reason: 'backend route is /api/v1/ai/analyze-sentiment' },
  { pattern: /\/api\/v1\/stafftimesheets\/my-timesheet\b/, reason: 'backend route is /api/v1/attendance/my-timesheet' },
];

const ROOT = resolve(__dirname, '..');
const SCAN_DIRS = ['app', 'components', 'lib', 'hooks', 'store', 'stores', 'contexts'];
const EXT = /\.(ts|tsx)$/;
const SKIP = /node_modules|\.next|[\\/]tests[\\/]/;

function walk(dir: string, acc: string[] = []): string[] {
  let entries: string[];
  try {
    entries = readdirSync(dir);
  } catch {
    return acc;
  }
  for (const name of entries) {
    const full = join(dir, name);
    if (SKIP.test(full)) continue;
    const st = statSync(full);
    if (st.isDirectory()) walk(full, acc);
    else if (EXT.test(name)) acc.push(full);
  }
  return acc;
}

describe('frontend API paths match verified backend routes', () => {
  const files = SCAN_DIRS.flatMap((d) => walk(join(ROOT, d)));

  it('scans a non-trivial number of source files', () => {
    expect(files.length).toBeGreaterThan(100);
  });

  for (const { pattern, reason } of FORBIDDEN) {
    it(`no source file uses a route matching ${pattern} (${reason})`, () => {
      const offenders: string[] = [];
      for (const file of files) {
        const src = readFileSync(file, 'utf8');
        if (pattern.test(src)) offenders.push(file.replace(ROOT, '.'));
      }
      expect(offenders, `Forbidden route pattern found in:\n${offenders.join('\n')}`).toHaveLength(0);
    });
  }
});
