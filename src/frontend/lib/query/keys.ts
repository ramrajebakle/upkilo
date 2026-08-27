/**
 * Query key factory.
 *
 * One hierarchical namespace per domain so invalidation can be as broad or as
 * narrow as the mutation actually warrants:
 *
 *   qk.bookings.all          -> invalidates every booking query
 *   qk.bookings.lists()      -> every list, leaving loaded detail pages alone
 *   qk.bookings.detail(id)   -> exactly one record
 *
 * Keys are built here rather than inline at call sites so a rename cannot
 * silently orphan a cache entry: a typo becomes a type error instead of a
 * query that never invalidates.
 */

export const qk = {
  bookings: {
    all: ['bookings'] as const,
    lists: () => [...qk.bookings.all, 'list'] as const,
    list: (params?: unknown) => [...qk.bookings.lists(), params ?? {}] as const,
    details: () => [...qk.bookings.all, 'detail'] as const,
    detail: (id: string) => [...qk.bookings.details(), id] as const,
  },

  clients: {
    all: ['clients'] as const,
    lists: () => [...qk.clients.all, 'list'] as const,
    list: (params?: unknown) => [...qk.clients.lists(), params ?? {}] as const,
    details: () => [...qk.clients.all, 'detail'] as const,
    detail: (id: string) => [...qk.clients.details(), id] as const,
    notes: (id: string) => [...qk.clients.detail(id), 'notes'] as const,
  },

  staff: {
    all: ['staff'] as const,
    lists: () => [...qk.staff.all, 'list'] as const,
    list: (params?: unknown) => [...qk.staff.lists(), params ?? {}] as const,
    detail: (id: string) => [...qk.staff.all, 'detail', id] as const,
  },

  services: {
    all: ['services'] as const,
    lists: () => [...qk.services.all, 'list'] as const,
    list: (params?: unknown) => [...qk.services.lists(), params ?? {}] as const,
    detail: (id: string) => [...qk.services.all, 'detail', id] as const,
  },

  dashboard: {
    all: ['dashboard'] as const,
    stats: () => [...qk.dashboard.all, 'stats'] as const,
  },
} as const;
