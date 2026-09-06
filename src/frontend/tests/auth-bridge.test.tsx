/**
 * AuthBridge must sign out at most once.
 *
 * In production this looped until the browser ran out of sockets:
 *   POST https://app.upkilo.com/api/auth/signout net::ERR_INSUFFICIENT_RESOURCES
 *   Uncaught (in promise) TypeError: Failed to fetch
 *
 * useSession returns a NEW session object on every poll and revalidation, and the effect depends
 * on `session`, so it re-runs constantly. While session.error stayed "RefreshAccessTokenError",
 * each re-run fired another signOut — and the error stays set exactly when the network is failing,
 * which is when signOut itself starts failing too. The failure fed itself.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import React from 'react';

const signOut = vi.fn();
let sessionState: { data: unknown; status: string } = { data: null, status: 'loading' };

vi.mock('next-auth/react', () => ({
  useSession: () => sessionState,
  signOut: (...a: unknown[]) => signOut(...a),
}));

vi.mock('@/lib/api', () => ({
  apiClient: { defaults: { headers: { common: {} } } },
}));

import { AuthBridge } from '@/components/providers/AuthBridge';

/** A fresh object each call, as NextAuth genuinely returns on every revalidation. */
const erroredSession = () => ({
  data: { error: 'RefreshAccessTokenError', user: {} },
  status: 'authenticated',
});

beforeEach(() => {
  signOut.mockReset();
  signOut.mockResolvedValue(undefined);
  sessionState = { data: null, status: 'loading' };
  try {
    localStorage.clear();
  } catch {
    /* jsdom provides it; guarded to match the component */
  }
});

describe('AuthBridge — refresh failure', () => {
  it('signs out once when the session can no longer be refreshed', async () => {
    sessionState = erroredSession();
    render(<AuthBridge />);

    await waitFor(() => expect(signOut).toHaveBeenCalledTimes(1));
  });

  it('does not sign out again when the session object changes identity', async () => {
    sessionState = erroredSession();
    const { rerender } = render(<AuthBridge />);
    await waitFor(() => expect(signOut).toHaveBeenCalledTimes(1));

    // Five more revalidations, each a distinct object with the error still set — precisely what
    // NextAuth polling produces. Previously each one fired another signOut.
    for (let i = 0; i < 5; i++) {
      sessionState = erroredSession();
      rerender(<AuthBridge />);
    }

    await waitFor(() => expect(signOut).toHaveBeenCalledTimes(1));
  });

  it('does not loop when signOut itself fails', async () => {
    // The production case: the network is failing, so signOut rejects and the error never clears.
    signOut.mockRejectedValue(new TypeError('Failed to fetch'));
    sessionState = erroredSession();

    const { rerender } = render(<AuthBridge />);
    for (let i = 0; i < 5; i++) {
      sessionState = erroredSession();
      rerender(<AuthBridge />);
    }

    await waitFor(() => expect(signOut).toHaveBeenCalledTimes(1));
  });

  it('clears the stored credentials on a refresh failure', async () => {
    localStorage.setItem('token', 'stale');
    localStorage.setItem('tenantId', 'stale-tenant');
    sessionState = erroredSession();

    render(<AuthBridge />);

    await waitFor(() => {
      expect(localStorage.getItem('token')).toBeNull();
      expect(localStorage.getItem('tenantId')).toBeNull();
    });
  });
});

describe('AuthBridge — healthy session', () => {
  it('mirrors the access token without signing out', async () => {
    sessionState = {
      data: { user: { accessToken: 'jwt-123', tenantId: 'tenant-abc' } },
      status: 'authenticated',
    };

    render(<AuthBridge />);

    await waitFor(() => expect(localStorage.getItem('token')).toBe('jwt-123'));
    expect(localStorage.getItem('tenantId')).toBe('tenant-abc');
    expect(signOut).not.toHaveBeenCalled();
  });

  it('clears credentials when unauthenticated, without signing out again', async () => {
    localStorage.setItem('token', 'stale');
    sessionState = { data: null, status: 'unauthenticated' };

    render(<AuthBridge />);

    await waitFor(() => expect(localStorage.getItem('token')).toBeNull());
    // signOut is for a session that cannot be refreshed; simply being logged out is not that.
    expect(signOut).not.toHaveBeenCalled();
  });
});
