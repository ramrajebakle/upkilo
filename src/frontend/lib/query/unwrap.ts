import type { AxiosResponse } from 'axios';

/**
 * Normalises the two response envelopes this API actually returns.
 *
 * Most endpoints put the payload at `res.data`, but a handful nest it again at
 * `res.data.data` (bookings/list and clients/list among them). Call sites
 * papered over the difference with `res.data.data ?? res.data ?? []`, repeated
 * ~180 times and easy to get subtly wrong — `?? []` also swallows a genuine
 * error into an empty list, which renders an "all clear" empty state over what
 * was really a failed request.
 *
 * Unwrapping once, here, means a hook throws on failure (so React Query can
 * surface a real error state) and call sites receive the payload directly.
 */

type Envelope<T> = T | { data: T };

function hasNestedData<T>(body: Envelope<T>): body is { data: T } {
  return (
    body !== null &&
    typeof body === 'object' &&
    'data' in body &&
    (body as { data: unknown }).data !== undefined
  );
}

/** Unwraps a single-object payload. */
export function unwrap<T>(res: AxiosResponse<Envelope<T>>): T {
  const body = res.data;
  return hasNestedData(body) ? body.data : (body as T);
}

/**
 * Unwraps a collection payload, guaranteeing an array.
 *
 * Unlike the old `?? []`, a non-array body is a contract violation and throws,
 * so it surfaces as an error state instead of a silent empty table.
 */
export function unwrapList<T>(res: AxiosResponse<Envelope<T[]>>): T[] {
  const payload = unwrap<T[]>(res as AxiosResponse<Envelope<T[]>>);
  if (payload === undefined || payload === null) return [];
  if (!Array.isArray(payload)) {
    throw new Error('Expected a list response but received a non-array payload.');
  }
  return payload;
}
