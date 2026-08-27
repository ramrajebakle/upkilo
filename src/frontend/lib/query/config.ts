import type { AxiosRequestConfig } from 'axios';

/**
 * Marks a request as owned by a React Query hook.
 *
 * The response interceptor shows a toast when a read fails, because the pages
 * that have not been migrated yet swallow the error and render their empty
 * state instead. Pages driven by these hooks already render <ErrorState>, so
 * they opt out rather than report the same failure twice.
 */
export const HOOK_MANAGED: AxiosRequestConfig & { suppressErrorToast: true } = {
  suppressErrorToast: true,
};
