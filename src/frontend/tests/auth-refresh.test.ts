/**
 * The NextAuth jwt callback must not retry a refresh the backend has already refused.
 *
 * Production logged this from a single browser in 1.4 seconds:
 *   SECURITY: AUTH_FAILURE [401] on /api/v1/auth/refresh by User=anonymous  (x6)
 *
 * The callback runs on EVERY session read. Once a refresh failed, the token kept the same
 * invalid refreshToken and the same expired accessTokenExpires, so the next read reissued the
 * identical request that had just been refused — forever.
 *
 * A 401/403 is final and must not be retried. Anything else (5xx, gateway, network) is treated
 * as transient and stays retryable, so a backend blip does not sign every user out.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';

process.env.NEXTAUTH_SECRET ??= 'test-secret-at-least-32-characters-long!!';
process.env.NEXT_PUBLIC_API_URL ??= 'https://api.test';

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

// auth.ts calls NextAuth(authConfig) at module scope, which drags in next/server and fails to
// resolve under vitest. Only the exported authConfig is under test here — the callbacks in it are
// the real ones — so the NextAuth runtime itself is stubbed out.
vi.mock('next-auth', () => ({
  default: () => ({ handlers: {}, auth: vi.fn(), signIn: vi.fn(), signOut: vi.fn() }),
}));
vi.mock('next-auth/providers/credentials', () => ({ default: (o: unknown) => o }));

import { authConfig } from '@/auth';

type Jwt = NonNullable<NonNullable<typeof authConfig.callbacks>['jwt']>;
const jwt = authConfig.callbacks!.jwt! as unknown as (a: {
  token: Record<string, unknown>;
  user?: unknown;
}) => Promise<Record<string, unknown>>;

/** A session whose access token expired an hour ago, so a refresh is due. */
const expiredToken = () => ({
  accessToken: 'stale',
  refreshToken: 'refresh-abc',
  accessTokenExpires: Date.now() - 3_600_000,
});

const respond = (status: number, body: unknown = {}) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

beforeEach(() => {
  fetchMock.mockReset();
});

describe('token refresh — permanent refusal', () => {
  it('marks a 401 as rejected and does not retry it', async () => {
    fetchMock.mockResolvedValue(respond(401));

    const first = await jwt({ token: expiredToken() });

    expect(first.error).toBe('RefreshAccessTokenError');
    expect(first.refreshRejected).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);

    // Five further session reads — polling, revalidation, a navigation. Each one previously
    // fired another request that was certain to 401.
    let token = first;
    for (let i = 0; i < 5; i++) token = await jwt({ token });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(token.error).toBe('RefreshAccessTokenError');
  });

  it('treats 403 the same as 401', async () => {
    fetchMock.mockResolvedValue(respond(403));

    const t = await jwt({ token: expiredToken() });
    await jwt({ token: t });

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('does not call the backend at all when there is no refresh token', async () => {
    const t = await jwt({ token: { accessTokenExpires: Date.now() - 1000 } });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(t.refreshRejected).toBe(true);
  });
});

describe('token refresh — transient failure', () => {
  it('leaves a 500 retryable so a backend blip does not force re-login', async () => {
    fetchMock.mockResolvedValue(respond(500));

    const first = await jwt({ token: expiredToken() });
    expect(first.error).toBe('RefreshAccessTokenError');
    expect(first.refreshRejected).toBeFalsy();

    await jwt({ token: first });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('recovers when a later attempt succeeds', async () => {
    fetchMock.mockResolvedValueOnce(respond(500));
    const failed = await jwt({ token: expiredToken() });

    // A JWT with an exp far in the future, so the new token is treated as valid.
    const exp = Math.floor(Date.now() / 1000) + 3600;
    const fakeJwt = `x.${Buffer.from(JSON.stringify({ exp })).toString('base64url')}.y`;
    fetchMock.mockResolvedValueOnce(respond(200, { token: fakeJwt, refreshToken: 'refresh-new' }));

    const recovered = await jwt({ token: failed });

    expect(recovered.error).toBeUndefined();
    expect(recovered.accessToken).toBe(fakeJwt);
  });
});

describe('session error — only terminal failures reach the client', () => {
  const session = authConfig.callbacks!.session! as unknown as (a: {
    session: Record<string, unknown>;
    token: Record<string, unknown>;
  }) => Record<string, unknown>;

  const blank = () => ({ user: {} as Record<string, unknown> });

  it('does NOT report an error for a transient failure', async () => {
    // THE deployment bug. A deploy restarts the API; anyone needing a refresh in that window
    // gets a network or 5xx failure. AuthBridge signs out on session.error, so publishing a
    // retryable failure here logged every such user out over a blip that lasted seconds.
    fetchMock.mockResolvedValue(respond(503));
    const token = await jwt({ token: expiredToken() });

    const result = session({ session: blank(), token });

    expect(token.error).toBe('RefreshAccessTokenError');   // still recorded server-side
    expect(token.refreshRejected).toBeFalsy();             // and still retryable
    expect(result.error).toBeUndefined();                  // but NOT terminal for the client
  });

  it('does NOT report an error for a network failure', async () => {
    fetchMock.mockRejectedValue(new TypeError('fetch failed'));
    const token = await jwt({ token: expiredToken() });

    expect(session({ session: blank(), token }).error).toBeUndefined();
  });

  it('DOES report an error once the backend refuses the token', async () => {
    // A 401 is final, so the user genuinely must sign in again — that path must keep working.
    fetchMock.mockResolvedValue(respond(401));
    const token = await jwt({ token: expiredToken() });

    expect(session({ session: blank(), token }).error).toBe('RefreshAccessTokenError');
  });

  it('reports no error for a healthy session', () => {
    const token = { accessToken: 'fresh', accessTokenExpires: Date.now() + 3_600_000 };

    expect(session({ session: blank(), token }).error).toBeUndefined();
  });
});

describe('token refresh — healthy session', () => {
  it('reuses a still-valid access token without calling the backend', async () => {
    const token = {
      accessToken: 'fresh',
      refreshToken: 'refresh-abc',
      accessTokenExpires: Date.now() + 3_600_000,
    };

    const result = await jwt({ token });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(result.accessToken).toBe('fresh');
  });
});
