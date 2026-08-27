import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { qk } from './keys';
import { unwrapList, unwrap } from './unwrap';
import { HOOK_MANAGED } from './config';

export type BookingStatus =
  | 'confirmed' | 'pending' | 'completed' | 'cancelled' | 'no_show';

export interface Booking {
  id: string;
  clientName: string;
  clientEmail: string;
  clientInitials: string;
  serviceName: string;
  staffName: string;
  startTime: string;
  endTime: string;
  status: BookingStatus;
  price: number;
}

export interface BookingListParams {
  limit?: number;
  status?: string;
  startDate?: string;
  endDate?: string;
}

/**
 * Bookings list.
 *
 * Replaces a useEffect that refetched on every filter change with no cleanup.
 * Switching filters quickly left two requests racing, and whichever resolved
 * last won — so a slow "today" response could overwrite the "week" response
 * the user was actually looking at. Keying the cache by params makes the
 * abandoned response inert: React Query writes it to the key it was requested
 * under, which is no longer the one being rendered.
 */
export function useBookings(params: BookingListParams) {
  return useQuery({
    queryKey: qk.bookings.list(params),
    queryFn: () => api.bookings.list(params as never, HOOK_MANAGED).then(unwrapList<Booking>),
    // Filter changes should not blank the table — keeping the previous page's
    // rows visible while the next set loads avoids a layout collapse and the
    // spinner flash that made filtering feel slower than it is.
    placeholderData: (previous) => previous,
    staleTime: 30_000,
  });
}

export function useBooking(id: string) {
  return useQuery({
    queryKey: qk.bookings.detail(id),
    queryFn: () => api.bookings.get(id, HOOK_MANAGED).then(unwrap<Booking>),
    enabled: Boolean(id),
  });
}

/**
 * Bulk cancel, with an optimistic table update.
 *
 * Uses the real `/cancel` endpoint. The page previously offered two bulk
 * actions — "Cancel Selected" (PUT status=cancelled) and "Delete Selected"
 * (POST /cancel) — but the API exposes no delete at all, so the delete action
 * cancelled the booking, removed the row locally and reported "deleted". The
 * record came back on the next load, still cancelled. One honest action
 * replaces both.
 *
 * `Promise.allSettled` + an explicit throw replaces `Promise.all` with
 * `.catch(() => null)` on each request, which swallowed every rejection and so
 * reported success even when all of them failed.
 */
export function useCancelBookings() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: async ({ ids, reason }: { ids: string[]; reason?: string }) => {
      const results = await Promise.allSettled(
        ids.map((id) => api.bookings.cancel(id, reason)),
      );
      const failed = results.filter((r) => r.status === 'rejected').length;
      if (failed > 0) {
        throw new Error(
          failed === ids.length
            ? `Could not cancel ${failed === 1 ? 'the booking' : 'any of the bookings'}.`
            : `${failed} of ${ids.length} bookings could not be cancelled.`,
        );
      }
      return ids.length;
    },

    onMutate: async ({ ids }) => {
      await qc.cancelQueries({ queryKey: qk.bookings.lists() });
      const snapshot = qc.getQueriesData<Booking[]>({ queryKey: qk.bookings.lists() });
      const target = new Set(ids);
      for (const [key, rows] of snapshot) {
        if (!rows) continue;
        qc.setQueryData<Booking[]>(
          key,
          rows.map((b) => (target.has(b.id) ? { ...b, status: 'cancelled' as const } : b)),
        );
      }
      return { snapshot };
    },

    onError: (_err, _vars, ctx) => {
      // Restore every list exactly as it was before the optimistic write, so a
      // failed cancel never leaves a row falsely showing "cancelled".
      ctx?.snapshot.forEach(([key, rows]) => qc.setQueryData(key, rows));
    },

    onSettled: () => {
      qc.invalidateQueries({ queryKey: qk.bookings.all });
    },
  });
}
