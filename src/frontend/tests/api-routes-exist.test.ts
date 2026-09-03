import { describe, it, expect } from 'vitest';
import { readFileSync, readdirSync, statSync, existsSync } from 'fs';
import { join, resolve } from 'path';

/**
 * Positive verification that frontend API calls hit routes the backend actually serves.
 *
 * api-paths.test.ts guards the same class of bug with a DENYLIST of paths already known to be
 * wrong. That can only catch a mistake someone has already found and written down — it is
 * structurally incapable of reporting a path that was never on the list. Which is exactly what
 * happened: /api/v1/ai/copilot, /api/v1/aichatbot/settings, /kb, /stats,
 * /api/v1/ai/fill-my-calendar/invite and both /super-admin/ai-infrastructure/* paths were all
 * called by shipped pages, all 404'd, and the denylist reported nothing wrong.
 *
 * This test inverts it: extract every route the controllers declare, extract every path the
 * frontend calls, and require the second to be a subset of the first.
 */

const FRONT = resolve(__dirname, '..');
const CONTROLLERS = resolve(FRONT, '..', 'backend', 'Upkilo.API', 'Controllers');

const SCAN_DIRS = ['app', 'components', 'lib', 'hooks', 'store', 'stores', 'contexts'];
const SKIP = /node_modules|\.next|[\\/]tests[\\/]/;

function walk(dir: string, match: RegExp, acc: string[] = []): string[] {
  let entries: string[];
  try {
    entries = readdirSync(dir);
  } catch {
    return acc;
  }
  for (const name of entries) {
    const full = join(dir, name);
    if (SKIP.test(full)) continue;
    if (statSync(full).isDirectory()) walk(full, match, acc);
    else if (match.test(name)) acc.push(full);
  }
  return acc;
}

/** Collapse route params and JS template holes so `/x/{id:guid}` and `/x/${id}` compare equal. */
function normalise(path: string): string {
  let p = path.split('?')[0];
  if (!p.startsWith('/')) p = `/${p}`;
  p = p.replace(/\/+$/, '');
  p = p.replace(/\$\{[^}]*\}/g, '{p}');
  p = p.replace(/\{[^}]*\}/g, '{p}');
  return p.toLowerCase();
}

function backendRoutes(): Map<string, Set<string>> {
  const routes = new Map<string, Set<string>>();
  for (const file of walk(CONTROLLERS, /\.cs$/)) {
    const src = readFileSync(file, 'utf8');
    // Each [Route("...")] opens a controller block; the actions that follow belong to it.
    for (const part of src.split(/(?=\[Route\(")/)) {
      const rm = /^\[Route\("([^"]+)"\)\]/.exec(part);
      if (!rm) continue;
      const cls = /class\s+(\w+)\s*:/.exec(part);
      const controller = cls ? cls[1].replace(/Controller$/, '') : '';
      const base = rm[1]
        .replace('[controller]', controller)
        .replace(/\{version:apiVersion\}/g, '1');

      const actionRe = /\[Http(Get|Post|Put|Delete|Patch)(?:\("([^"]*)"\))?\]/g;
      let am: RegExpExecArray | null;
      while ((am = actionRe.exec(part)) !== null) {
        const verb = am[1].toUpperCase();
        const sub = am[2] ?? '';
        const full = sub ? `${base.replace(/\/+$/, '')}/${sub.replace(/^\/+/, '')}` : base;
        const key = normalise(full);
        if (!routes.has(key)) routes.set(key, new Set());
        routes.get(key)!.add(verb);
      }
    }
  }
  return routes;
}

interface Call { verb: string; path: string; file: string; }

function frontendCalls(): Call[] {
  const calls: Call[] = [];
  const callRe = /(?:apiClient|api)\.(get|post|put|delete|patch)\(\s*[`'"]([^`'"]+)[`'"]/g;
  const fetchRe = /fetch\(\s*[`'"](\/api\/[^`'"]+)[`'"]/g;

  for (const dir of SCAN_DIRS) {
    for (const file of walk(join(FRONT, dir), /\.(ts|tsx)$/)) {
      const src = readFileSync(file, 'utf8');
      const rel = file.replace(FRONT, '.');
      let m: RegExpExecArray | null;
      while ((m = callRe.exec(src)) !== null) {
        calls.push({ verb: m[1].toUpperCase(), path: m[2], file: rel });
      }
      while ((m = fetchRe.exec(src)) !== null) {
        calls.push({ verb: 'ANY', path: m[1], file: rel });
      }
    }
  }
  return calls.filter((c) => c.path.startsWith('/api/'));
}

const AI_MARKERS = ['/ai', 'aichatbot', 'aidashboard', 'intelligence', 'knowledge-base', 'receptionist', 'copilot'];
const isAiPath = (p: string) => AI_MARKERS.some((k) => p.toLowerCase().includes(k));

describe('frontend API calls resolve to real backend routes', () => {
  // Skips cleanly rather than passing vacuously if the backend tree is not checked out.
  const haveBackend = existsSync(CONTROLLERS);

  it('finds the backend controllers and a non-trivial number of calls', () => {
    expect(haveBackend, `expected controllers at ${CONTROLLERS}`).toBe(true);
    expect(backendRoutes().size).toBeGreaterThan(500);
    expect(frontendCalls().length).toBeGreaterThan(300);
  });

  it('every AI-related call has a matching backend route', () => {
    const routes = backendRoutes();
    const offenders = frontendCalls()
      .filter((c) => isAiPath(c.path))
      .filter((c) => !routes.has(normalise(c.path)))
      .map((c) => `${c.verb} ${c.path}  <- ${c.file}`);

    expect(
      [...new Set(offenders)],
      `AI paths with no backend route (these 404 at runtime):\n${[...new Set(offenders)].join('\n')}`,
    ).toEqual([]);
  });

  it('every AI-related call uses a verb the backend route accepts', () => {
    const routes = backendRoutes();
    const offenders = frontendCalls()
      .filter((c) => isAiPath(c.path) && c.verb !== 'ANY')
      .filter((c) => routes.has(normalise(c.path)) && !routes.get(normalise(c.path))!.has(c.verb))
      .map((c) => `${c.verb} ${c.path} (backend accepts ${[...routes.get(normalise(c.path))!].join(',')})`);

    expect([...new Set(offenders)], offenders.join('\n')).toEqual([]);
  });

  /**
   * A ratchet, not a clean bill of health.
   *
   * The non-AI surface still has a substantial backlog of frontend paths with no matching
   * route. Asserting zero would fail immediately and tell nobody anything new, so this pins the
   * count instead: the number may fall, and lowering the bound is the fix, but a change that
   * adds a new broken path fails here. The AI assertions above are the ones held at zero.
   */
  it('does not add new unmatched paths anywhere else', () => {
    const routes = backendRoutes();
    const offenders = new Set(
      frontendCalls()
        .filter((c) => !routes.has(normalise(c.path)))
        .map((c) => `${c.verb} ${normalise(c.path)}`),
    );

    expect(offenders.size).toBeLessThanOrEqual(127);
  });
});
