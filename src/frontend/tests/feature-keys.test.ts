import { describe, it, expect } from 'vitest';
import { readFileSync, readdirSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { ALL_FEATURE_KEYS, NUMERIC_FEATURE_KEYS, FEATURES } from '../lib/featureKeys';

/**
 * Cross-language contract guard for the entitlement vocabulary.
 *
 * The frontend and backend each hold a list of feature keys, and there is no compiler that
 * spans both. When they drifted apart the failure was silent and total: every FeatureGate in
 * the app passed a PascalCase name ("AiFeatures", "CustomBranding", "ApiAccess",
 * "WhiteLabelDomain", "Webhooks") that the API's payload — keyed by snake_case catalogue keys —
 * never contained. `hasFeature` indexed a plain object, missed on every lookup, and rendered
 * "Upgrade your plan" to paying customers on every gated page.
 *
 * These tests read the backend's FeatureKeys.cs directly and compare, so the two lists cannot
 * drift again without a red build.
 */

const BACKEND_FEATURE_KEYS = resolve(
  __dirname,
  '../../backend/Upkilo.Core/Entities/Pricing/FeatureKeys.cs',
);

/** Pulls the string literals out of `public const string X = "y";` declarations. */
function backendKeys(): string[] {
  const src = readFileSync(BACKEND_FEATURE_KEYS, 'utf8');
  const matches = [...src.matchAll(/public const string \w+ = "([a-z0-9_]+)";/g)];
  return matches.map((m) => m[1]);
}

describe('feature key contract', () => {
  it('backend FeatureKeys.cs is present and parseable', () => {
    // If this fails the file moved; fix the path rather than deleting the guard, or the
    // comparison below silently becomes vacuous.
    expect(backendKeys().length).toBeGreaterThan(0);
  });

  it('frontend FEATURES matches backend FeatureKeys exactly', () => {
    expect([...ALL_FEATURE_KEYS].sort()).toEqual([...backendKeys()].sort());
  });

  it('every key is snake_case', () => {
    // The original defect was a case/separator mismatch, so the shape itself is pinned.
    for (const key of ALL_FEATURE_KEYS) {
      expect(key).toMatch(/^[a-z][a-z0-9_]*$/);
    }
  });

  it('numeric keys are a subset of all keys', () => {
    for (const key of NUMERIC_FEATURE_KEYS) {
      expect(ALL_FEATURE_KEYS).toContain(key);
    }
  });

  it('has no duplicate keys', () => {
    expect(new Set(ALL_FEATURE_KEYS).size).toBe(ALL_FEATURE_KEYS.length);
  });
});

/**
 * No gate may pass a bare string. FeatureGate's prop is typed to FeatureKey, so a literal that
 * happens to be valid still compiles — but a literal is how the drift started, and the union
 * type does not stop someone widening it back to `string` later.
 */
describe('feature gates use the shared constants', () => {
  const ROOT = resolve(__dirname, '..');
  const SCAN_DIRS = ['app', 'components', 'lib', 'hooks', 'store', 'stores', 'contexts'];
  const EXT = /\.(ts|tsx)$/;
  const SKIP = /node_modules|\.next|[\\/]tests[\\/]/;

  function walk(dir: string, out: string[] = []): string[] {
    let entries: string[];
    try {
      entries = readdirSync(dir);
    } catch {
      return out;
    }
    for (const entry of entries) {
      const full = join(dir, entry);
      if (SKIP.test(full)) continue;
      if (statSync(full).isDirectory()) walk(full, out);
      else if (EXT.test(full)) out.push(full);
    }
    return out;
  }

  const files = SCAN_DIRS.flatMap((d) => walk(join(ROOT, d)));

  it('scans a non-trivial number of source files', () => {
    expect(files.length).toBeGreaterThan(50);
  });

  it('no featureName is passed as a string literal', () => {
    const offenders: string[] = [];
    for (const file of files) {
      const src = readFileSync(file, 'utf8');
      // featureName="..." or featureName={'...'} — both bypass the shared constants.
      const literal = /featureName\s*=\s*(?:"[^"]*"|'[^']*'|\{\s*['"][^'"]*['"]\s*\})/g;
      for (const match of src.matchAll(literal)) {
        offenders.push(`${file.replace(ROOT, '')}: ${match[0]}`);
      }
    }
    expect(offenders).toEqual([]);
  });

  it('exposes the keys the gated settings pages depend on', () => {
    // Pins the specific mappings chosen when the PascalCase names were replaced, so a later
    // rename of one of these constants cannot quietly re-point a live gate.
    expect(FEATURES.WHITE_LABEL).toBe('white_label');
    expect(FEATURES.API_ACCESS).toBe('api_access');
    expect(FEATURES.AI_WORKFLOWS).toBe('ai_workflows');
    expect(FEATURES.AI_INSIGHTS).toBe('ai_insights');
    expect(FEATURES.AI_COPILOT).toBe('ai_copilot');
  });
});
