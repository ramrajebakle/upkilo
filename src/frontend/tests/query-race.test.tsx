/**
 * Reproduces the stale-response race that the hand-rolled
 * useEffect + useState fetching pattern allows, and proves the
 * React Query data layer is structurally immune to it.
 *
 * Scenario (real, user-visible): on /bookings the user switches the
 * date filter "today" -> "week". Two requests are in flight. The
 * slower "today" request resolves LAST, so its result overwrites the
 * "week" result. The table then shows today's bookings while the
 * filter control reads "week".
 */
import { describe, it, expect } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import { useEffect, useState } from 'react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';

// Deferred responses so the test controls resolution order precisely.
const deferred: Record<string, (v: string[]) => void> = {};
function fetchBookings(filter: string): Promise<string[]> {
  return new Promise<string[]>((resolve) => {
    deferred[filter] = resolve;
  });
}

function LegacyBookings({ filter }: { filter: string }) {
  const [rows, setRows] = useState<string[]>([]);
  useEffect(() => {
    // This is the exact shape used across the dashboard today:
    // no AbortController, no ignore flag, no cleanup.
    fetchBookings(filter).then(setRows);
  }, [filter]);
  return <div data-testid="legacy">{rows.join(',')}</div>;
}

function QueryBookings({ filter }: { filter: string }) {
  const { data } = useQuery({
    queryKey: ['bookings', filter],
    queryFn: () => fetchBookings(filter),
  });
  return <div data-testid="query">{(data ?? []).join(',')}</div>;
}

function withClient(ui: React.ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return <QueryClientProvider client={client}>{ui}</QueryClientProvider>;
}

describe('stale-response race on filter change', () => {
  it('legacy useEffect pattern renders the STALE result (the bug)', async () => {
    const { rerender } = render(<LegacyBookings filter="today" />);
    rerender(<LegacyBookings filter="week" />);

    // "week" (the current filter) comes back first...
    await act(async () => { deferred['week'](['week-booking']); });
    // ...then the abandoned "today" request resolves and clobbers it.
    await act(async () => { deferred['today'](['today-booking']); });

    await waitFor(() => {
      // The UI now contradicts the filter control.
      expect(screen.getByTestId('legacy').textContent).toBe('today-booking');
    });
  });

  it('React Query discards the abandoned response (the fix)', async () => {
    const { rerender } = render(withClient(<QueryBookings filter="today" />));
    rerender(withClient(<QueryBookings filter="week" />));

    await act(async () => { deferred['week'](['week-booking']); });
    await act(async () => { deferred['today'](['today-booking']); });

    await waitFor(() => {
      expect(screen.getByTestId('query').textContent).toBe('week-booking');
    });
  });
});
