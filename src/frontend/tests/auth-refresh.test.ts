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
