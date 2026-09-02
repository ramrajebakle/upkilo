/**
 * Turns an API failure into the most specific message the server actually gave us.
 *
 * The API answers a rejected write with a precise reason — FluentValidation returns an
 * ASP.NET ProblemDetails whose `errors` dictionary names each failing field, and the
 * controllers return `{ error: "..." }` for their own domain rules. Call sites were
 * discarding all of it and showing "Failed to create service. Please try again.", which tells
 * the user nothing they can act on and leaves whoever is debugging with only a bare 400 in the
 * console. That is not a hypothetical: it is exactly why a 400 on POST /api/v1/services could
 * not be diagnosed from the browser at all.
 *
 * Order matters — most specific first:
 *   1. ProblemDetails.errors  — per-field validation, the common 400
 *   2. message / error / title — a domain rule the controller rejected
 *   3. the supplied fallback   — genuinely unknown, e.g. a network failure
 */
export function apiErrorMessage(err: unknown, fallback: string): string {
  const data = (err as { response?: { data?: unknown } })?.response?.data;

  if (data && typeof data === 'object') {
    const body = data as Record<string, unknown>;

    // ASP.NET ProblemDetails: { errors: { FieldName: ["msg", ...], ... } }
    const errors = body.errors;
    if (errors && typeof errors === 'object') {
      const messages = Object.entries(errors as Record<string, unknown>)
        .flatMap(([field, list]) => {
          const items = Array.isArray(list) ? list : [list];
          return items
            .filter((m): m is string => typeof m === 'string')
            // Field name included: "Duration must be greater than 0" is far easier to act on
            // as "DurationMinutes: must be greater than 0" when the form has 20 inputs.
            .map((m) => (field && field !== '$' ? `${field}: ${m}` : m));
        });
      if (messages.length > 0) return messages.join('\n');
    }

    for (const key of ['message', 'error', 'detail', 'title'] as const) {
      const value = body[key];
      if (typeof value === 'string' && value.trim()) return value;
    }
  }

  return fallback;
}
