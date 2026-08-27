import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { qk } from './keys';
import { unwrapList } from './unwrap';
import { HOOK_MANAGED } from './config';

export interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  status: string;
  loyaltyPoints: number;
  totalSpend: number;
  lastVisitAt: string | null;
  tags: string[];
  createdAt: string;
}

/**
 * The API returns clients under several historical field names. Normalising
 * here rather than in the component keeps the shape in one place, so a page
 * that renders clients cannot disagree with another page about what a client
 * is.
 */
function normalizeClient(c: Record<string, unknown>): Client {
  const str = (v: unknown) => (typeof v === 'string' ? v : '');
  const num = (v: unknown) => (typeof v === 'number' ? v : 0);
  return {
    id: str(c.id),
    firstName: str(c.firstName),
    lastName: str(c.lastName),
    email: str(c.email),
    phone: str(c.phone) || str(c.phoneNumber),
    status: str(c.status) || (c.isActive ? 'Active' : 'Inactive'),
    loyaltyPoints: num(c.loyaltyPoints),
    totalSpend: num(c.totalSpend) || num(c.lifetimeValue),
    lastVisitAt: str(c.lastVisitAt) || str(c.lastVisit) || null,
    tags: Array.isArray(c.tags) ? (c.tags as string[]) : [],
    createdAt: str(c.createdAt),
  };
}

/**
 * Client list, searched server-side.
 *
 * The previous effect read `searchQuery` but declared `[]` as its dependency
 * list, so it captured the initial empty string and never re-ran: the `search`
 * parameter was always undefined and the server never actually searched.
 * Filtering happened client-side over whatever the first response contained,
 * so any client outside that page was simply unfindable. Putting the term in
 * the query key makes the request re-issue when it changes, which is the
 * behaviour the code already looked like it had.
 */
export function useClients(search?: string) {
  const term = search?.trim() || undefined;
  return useQuery({
    queryKey: qk.clients.list({ search: term }),
    queryFn: () =>
      api.clients
        .list({ search: term }, HOOK_MANAGED)
        .then((res) => unwrapList<Record<string, unknown>>(res).map(normalizeClient)),
    placeholderData: (previous) => previous,
    staleTime: 30_000,
  });
}

/** Single delete. Invalidation refetches the list so counts and paging stay true. */
export function useDeleteClient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.clients.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.clients.all }),
  });
}

/**
 * Bulk delete.
 *
 * `Promise.all` rejected on the first failure, so a partial failure reported
 * "Failed to perform bulk deletion" while some clients really had been
 * deleted, and the table still listed them. Reporting how many succeeded, and
 * always invalidating, keeps the message and the table honest.
 */
export function useDeleteClients() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (ids: string[]) => {
      const results = await Promise.allSettled(ids.map((id) => api.clients.delete(id)));
      const deleted = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - deleted;
      if (failed > 0) {
        const err = new Error(
          deleted === 0
            ? `Could not delete ${failed === 1 ? 'the client' : 'any of the clients'}.`
            : `Deleted ${deleted}, but ${failed} could not be removed.`,
        );
        (err as Error & { deleted: number }).deleted = deleted;
        throw err;
      }
      return deleted;
    },
    onSettled: () => qc.invalidateQueries({ queryKey: qk.clients.all }),
  });
}
