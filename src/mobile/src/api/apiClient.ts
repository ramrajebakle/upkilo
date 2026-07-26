import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import * as SecureStore from '../utils/storage';
import * as Localization from 'expo-localization';

// Resolved at build time via EAS environment variables.
// Development falls back to localhost; staging/production use injected value.
const BASE_URL =
  process.env.EXPO_PUBLIC_API_URL ??
  'http://localhost:5000/api/v1';

export const apiClient = axios.create({
  baseURL: BASE_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Some backend routes are NOT under the /api/v1 prefix — e.g. the public booking widget
// (api/booking/{slug}/...). Calling those through `apiClient` produced a double prefix
// (/api/v1/api/booking/...) → 404. `publicClient` targets the API host root instead.
const PUBLIC_BASE_URL = BASE_URL.replace(/\/api\/v1\/?$/, '');

export const publicClient = axios.create({
  baseURL: PUBLIC_BASE_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ── Request interceptor: inject auth + locale + timezone ──────────────────────
async function attachRequestContext(config: InternalAxiosRequestConfig) {
  const token = await SecureStore.getItemAsync('auth_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  const timezone = Localization.getCalendars()[0]?.timeZone;
  if (timezone) config.headers['X-Timezone'] = timezone;

  const locale = Localization.getLocales()[0]?.languageTag;
  if (locale) config.headers['Accept-Language'] = locale;

  return config;
}

apiClient.interceptors.request.use(attachRequestContext);
publicClient.interceptors.request.use(attachRequestContext);

// ── Token refresh state (deduplicated — one refresh at a time) ────────────────
let _refreshPromise: Promise<string | null> | null = null;

async function attemptTokenRefresh(): Promise<string | null> {
  const refreshToken = await SecureStore.getItemAsync('refresh_token');
  if (!refreshToken) return null;

  try {
    const res = await axios.post(`${BASE_URL}/auth/refresh`, { refreshToken });
    const { token: newToken, refreshToken: newRefreshToken } = res.data ?? {};
    if (!newToken) return null;

    await SecureStore.setItemAsync('auth_token', newToken);
    if (newRefreshToken) await SecureStore.setItemAsync('refresh_token', newRefreshToken);
    return newToken;
  } catch {
    // Refresh failed — purge both tokens
    await SecureStore.deleteItemAsync('auth_token');
    await SecureStore.deleteItemAsync('refresh_token');
    return null;
  }
}

// ── Response interceptor: 401 refresh, 429 retry ──────────────────────────────
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    // ── 401: attempt token refresh (once per request) ────────────────────────
    if (error.response?.status === 401 && !originalRequest._retried) {
      originalRequest._retried = true;

      if (!_refreshPromise) {
        _refreshPromise = attemptTokenRefresh().finally(() => { _refreshPromise = null; });
      }

      const newToken = await _refreshPromise;
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      }
      // Refresh failed — caller receives the 401 to trigger logout
    }

    // ── 429: respect Retry-After header, up to 3 retries ────────────────────
    const retryCount = (originalRequest as any)._retryCount ?? 0;
    if (error.response?.status === 429 && retryCount < 3) {
      const retryAfter = parseInt(
        (error.response.headers as any)['retry-after'] ?? '5',
        10
      );
      const delayMs = Math.min(retryAfter * 1000, 30_000);
      (originalRequest as any)._retryCount = retryCount + 1;
      await new Promise((r) => setTimeout(r, delayMs));
      return apiClient(originalRequest);
    }

    return Promise.reject(error);
  }
);

/**
 * Unwrap a list response body into an array.
 *
 * Every list endpoint on this API returns `{ data: [...] }` (some also carry paging fields
 * like `total`/`page`). Screens previously did `res.data?.items ?? res.data ?? []`, but no
 * endpoint uses an `items` key — so that fell through to the wrapper *object*, and the
 * subsequent `.filter`/`.map` threw "x.filter is not a function" at runtime.
 *
 * Always returns an array, so callers can render safely.
 */
export function unwrapList<T>(body: unknown): T[] {
  if (Array.isArray(body)) return body as T[];
  if (body && typeof body === 'object') {
    const rec = body as Record<string, unknown>;
    for (const key of ['data', 'items', 'results']) {
      if (Array.isArray(rec[key])) return rec[key] as T[];
    }
  }
  return [];
}
