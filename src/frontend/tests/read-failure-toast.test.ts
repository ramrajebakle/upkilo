/**
 * The global read-failure notice. Until every page renders its own error branch,
 * this is what stops a failed GET from being presented as an empty account.
 *
 * Mirrors shouldNotifyReadFailure() in lib/api.ts, which is module-private
 * (it lives inside the interceptor closure).
 */
import { describe, it, expect } from 'vitest';

type Cfg = { method?: string; suppressErrorToast?: boolean };
type Err = { config?: Cfg; response?: { status: number } };

function shouldNotifyReadFailure(error: Err): boolean {
  const config = error.config;
  if (!config || config.suppressErrorToast) return false;
  if ((config.method ?? 'get').toLowerCase() !== 'get') return false;
  const status = error.response?.status;
  if (status === 401 || status === 429) return false;
  return true;
}

describe('global read-failure notice', () => {
  it('fires for a failed read, which is what becomes a lying empty state', () => {
    expect(shouldNotifyReadFailure({ config: { method: 'get' }, response: { status: 500 } })).toBe(true);
    expect(shouldNotifyReadFailure({ config: { method: 'get' }, response: { status: 403 } })).toBe(true);
    // A network failure has no response at all — the most important case.
    expect(shouldNotifyReadFailure({ config: { method: 'get' } })).toBe(true);
  });

  it('treats a config with no method as a read, since GET is axios\' default', () => {
    expect(shouldNotifyReadFailure({ config: {}, response: { status: 500 } })).toBe(true);
  });

  it('stays quiet for writes, which toast at their own call sites', () => {
    for (const method of ['post', 'put', 'patch', 'delete']) {
      expect(shouldNotifyReadFailure({ config: { method }, response: { status: 500 } })).toBe(false);
    }
  });

  it('stays quiet for 401 and 429, which are handled elsewhere', () => {
    // 401 redirects to login; 429 is retried and may still succeed.
    expect(shouldNotifyReadFailure({ config: { method: 'get' }, response: { status: 401 } })).toBe(false);
    expect(shouldNotifyReadFailure({ config: { method: 'get' }, response: { status: 429 } })).toBe(false);
  });

  it('stays quiet for hook-managed reads, which render ErrorState instead', () => {
    expect(shouldNotifyReadFailure({
      config: { method: 'get', suppressErrorToast: true },
      response: { status: 500 },
    })).toBe(false);
  });
});
