/**
 * A 401 must only end the session when the session endpoint actually says so.
 *
 * Deploys here are direct — the B1 App Service tier has no slots, so each release takes the site
 * down for 30-60s (deploy.yml documents this). During that window getSession() cannot complete.
 * The interceptor treated any failure to produce a token as "unauthenticated", cleared the stored
 * token and hard-redirected to /login, so every active user was bounced on every release.
 *
 * This is a SECOND path to the same symptom. Fixing AuthBridge (which signed out on a transient
 * refresh error) was necessary but not sufficient — this one bypasses it entirely by navigating
 * the browser directly.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

const getSession = vi.fn();
vi.mock('next-auth/react', () => ({ getSession: () => getSession() }));

vi.mock('js-cookie', () => ({ default: { remove: vi.fn(), get: vi.fn(), set: vi.fn() } }));

import { apiClient } from '@/lib/api';

/** Captures navigation without letting jsdom actually navigate. */
let navigatedTo: string | null = null;

beforeEach(() => {
  getSession.mockReset();
  navigatedTo = null;
  localStorage.setItem('token', 'stale-token');

  Object.defineProperty(window, 'location', {
    configurable: true,
    value: {
      pathname: '/en/dashboard',
      get href() { return 'http://localhost/en/dashboard'; },
      set href(v: string) { navigatedTo = v; },
    },
  });
});

afterEach(() => {
  vi.restoreAllMocks();
});

/** Drives the response interceptor directly with a 401, as axios would. */
async function trigger401() {
  const handlers = (apiClient.interceptors.response as unknown as {
    handlers: { rejected: (e: unknown) => Promise<unknown> }[];
  }).handlers.filter(Boolean);

  const error = {
    response: { status: 401 },
    config: { url: '/api/v1/clients', headers: {} as Record<string, string> },
  };

  let lastErr: unknown;
  for (const h of handlers) {
    try {
      return await h.rejected(error);
    } catch (e) {
      lastErr = e;
    }
  }
  throw lastErr;
}

describe('401 handling during a deployment', () => {
  it('does NOT log the user out when the session endpoint is unreachable', async () => {
    // getSession throws — the frontend is mid-restart.
    getSession.mockRejectedValue(new TypeError('Failed to fetch'));

    await expect(trigger401()).rejects.toThrow(/try again/i);

    expect(navigatedTo).toBeNull();
    expect(localStorage.getItem('token')).toBe('stale-token');
  });

  it('does NOT log the user out when the session comes back null mid-restart', async () => {
    // Equally ambiguous while the frontend is restarting, so also treated as transient.
    getSession.mockResolvedValue(null);

    await expect(trigger401()).rejects.toThrow(/try again/i);

    expect(navigatedTo).toBeNull();
    expect(localStorage.getItem('token')).toBe('stale-token');
  });

  it('DOES log the user out when the session answers and carries no token', async () => {
    // The endpoint was reachable and said there is no token — genuinely signed out.
    getSession.mockResolvedValue({ user: {} });

    await expect(trigger401()).rejects.toThrow(/session expired/i);

    expect(navigatedTo).toContain('/login');
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('retries the request with a refreshed token when one is available', async () => {
    getSession.mockResolvedValue({ user: { accessToken: 'fresh-token' } });

    // The retry re-enters axios and will fail on the network in jsdom; what matters is that the
    // refreshed token was stored and no navigation happened.
    await trigger401().catch(() => undefined);

    expect(localStorage.getItem('token')).toBe('fresh-token');
    expect(navigatedTo).toBeNull();
  });
});
