/**
 * Covers the two trust bugs the bookings bulk action had:
 *  - Promise.all + `.catch(() => null)` swallowed every rejection, so a run in
 *    which all requests failed still reported success.
 *  - Local state was hand-patched to the expected result and never reconciled,
 *    so a failed request left the row showing the wrong status permanently.
 */
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

/**
 * A plain swappable implementation rather than vi.fn(): Vitest tracks the
 * promises a mock returns, and clearing that bookkeeping between tests
 * (mockReset/mockClear) drops a rejected promise before its settled-result
 * handler is attached, which surfaces as a spurious unhandled rejection even
 * though the code under test handles it. Nothing here asserts on call counts,
 * so the tracking buys nothing.
 */
let cancelImpl: (id: string, reason?: string) => Promise<unknown> =
  async () => ({ data: {} });

vi.mock('@/lib/api', () => ({
  api: { bookings: { cancel: (id: string, reason?: string) => cancelImpl(id, reason) } },
  apiClient: {},
}));

import { useCancelBookings, type Booking } from '@/lib/query/bookings';
import { qk } from '@/lib/query/keys';

function makeClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
}

function wrapperFor(client: QueryClient) {
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}

const row = (id: string, status: Booking['status'] = 'confirmed') =>
  ({ id, status, clientName: 'A', clientEmail: '', clientInitials: 'A',
     serviceName: 'S', staffName: 'T', startTime: '', endTime: '', price: 10 }) as Booking;

describe('useCancelBookings', () => {
  it('reports the real partial-failure count instead of succeeding silently', async () => {
    cancelImpl = (id) =>
      id === 'b2' ? Promise.reject(new Error('boom')) : Promise.resolve({ data: {} });

    const client = makeClient();
    const { result } = renderHook(() => useCancelBookings(), { wrapper: wrapperFor(client) });

    await expect(
      act(async () => { await result.current.mutateAsync({ ids: ['b1', 'b2'] }); })
    ).rejects.toThrow('1 of 2 bookings could not be cancelled.');
  });

  it('rolls the table back when the cancel fails', async () => {
    cancelImpl = () => Promise.reject(new Error('network down'));

    const client = makeClient();
    const key = qk.bookings.list({ limit: 100 });
    client.setQueryData<Booking[]>(key, [row('b1'), row('b2')]);

    const { result } = renderHook(() => useCancelBookings(), { wrapper: wrapperFor(client) });

    await act(async () => {
      await result.current.mutateAsync({ ids: ['b1'] }).catch(() => {});
    });

    await waitFor(() => {
      // b1 must be back to 'confirmed', not stuck showing 'cancelled'.
      expect(client.getQueryData<Booking[]>(key)?.find(b => b.id === 'b1')?.status)
        .toBe('confirmed');
    });
  });

  it('applies the cancelled status optimistically on success', async () => {
    cancelImpl = () => Promise.resolve({ data: {} });

    const client = makeClient();
    const key = qk.bookings.list({ limit: 100 });
    client.setQueryData<Booking[]>(key, [row('b1'), row('b2')]);

    const { result } = renderHook(() => useCancelBookings(), { wrapper: wrapperFor(client) });
    await act(async () => { await result.current.mutateAsync({ ids: ['b1'] }); });

    expect(client.getQueryData<Booking[]>(key)?.find(b => b.id === 'b1')?.status).toBe('cancelled');
    expect(client.getQueryData<Booking[]>(key)?.find(b => b.id === 'b2')?.status).toBe('confirmed');
  });
});
