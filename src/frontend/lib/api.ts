import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig, AxiosRequestConfig } from 'axios';
import Cookies from 'js-cookie';
import {
  ICoupon,
  IServicePackage,
  IWaitlistEntry,
  IClient,
  IBooking,
  IApiResponse,
  IPaginatedResponse
} from '../types';

// Shared in-flight refresh promise — prevents N concurrent 401s each triggering an independent refresh
let _refreshPromise: Promise<void> | null = null;

// ── Circuit breaker — opens after 5 consecutive 5xx failures, auto-resets after 30s ──────────────
let _cbFailures = 0;
let _cbOpen = false;
const CB_THRESHOLD = 5;
const CB_RESET_MS = 30_000;

function cbRecordSuccess() {
  _cbFailures = 0;
  _cbOpen = false;
}

function cbRecordFailure() {
  _cbFailures += 1;
  if (!_cbOpen && _cbFailures >= CB_THRESHOLD) {
    _cbOpen = true;
    setTimeout(() => { _cbOpen = false; _cbFailures = 0; }, CB_RESET_MS);
  }
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

// Create axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add auth token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    let token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
    
    // Fallback to cookie if localStorage is empty or we are in a transition
    if (!token && typeof document !== 'undefined') {
      const match = document.cookie.match(/token=([^;]+)/);
      if (match) token = match[1];
    }

    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    // Add tenant header if available
    const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null;
    if (tenantId && config.headers) {
      config.headers['X-Tenant-Id'] = tenantId;
    }

    // Add timezone header if available from cookie
    if (typeof document !== 'undefined') {
      const tzMatch = document.cookie.match(/timezone=([^;]+)/);
      if (tzMatch && config.headers) {
        config.headers['X-Timezone'] = decodeURIComponent(tzMatch[1]);
      }
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Global read-failure notification ──────────────────────────────────────────────────────────
// 164 dashboard pages still `catch { setRows([]) }`, which renders their empty state: a failed
// request is presented to the customer as "you have no bookings / clients / revenue". Migrating
// each page to render a real error branch is the proper fix and is under way, but until then a
// silent failure is the single most damaging thing this UI does.
//
// This is the safety net, not the fix. It only fires for GETs — a failed read is what turns into
// a lying empty state; writes overwhelmingly have their own toast at the call site. Requests the
// query hooks make opt out, because those pages already render <ErrorState> and would otherwise
// say it twice.
const READ_FAILURE_WINDOW_MS = 4000;
let _lastReadFailureAt = 0;

function notifyReadFailure(error: AxiosError): void {
  if (typeof window === 'undefined') return;

  // A dashboard can fire half a dozen parallel reads; one outage should not stack six toasts.
  const now = Date.now();
  if (now - _lastReadFailureAt < READ_FAILURE_WINDOW_MS) return;
  _lastReadFailureAt = now;

  const status = error.response?.status;
  const message =
    status === 403 ? "You don't have access to this."
    : status === 404 ? "That isn't available."
    : status !== undefined && status >= 500 ? "Couldn't load this — the server is having trouble."
    : "Couldn't load this. Check your connection.";

  void import('sonner').then(({ toast }) => {
    toast.error(message, { description: 'The page may be showing incomplete data.' });
  }).catch(() => { /* toast is a courtesy; never let it mask the original failure */ });
}

function shouldNotifyReadFailure(error: AxiosError): boolean {
  const config = error.config as (InternalAxiosRequestConfig & { suppressErrorToast?: boolean }) | undefined;
  if (!config || config.suppressErrorToast) return false;
  // Only reads. GET is the default when a method is not set.
  if ((config.method ?? 'get').toLowerCase() !== 'get') return false;
  const status = error.response?.status;
  // 401 redirects to login on its own; 429 is retried above and may still succeed.
  if (status === 401 || status === 429) return false;
  return true;
}

// Response interceptor - handle circuit breaker, 401 refresh, and 429 rate-limit retry
apiClient.interceptors.response.use(
  (response) => { cbRecordSuccess(); return response; },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const status = error.response?.status;

    // ── Circuit breaker: reject immediately when open (avoids pile-up during outages) ────────────
    if (_cbOpen && status !== 401 && status !== 429) {
      return Promise.reject(new Error('Service temporarily unavailable. Please try again shortly.'));
    }

    // Record 5xx failures toward the circuit breaker threshold
    if (status !== undefined && status >= 500) {
      cbRecordFailure();
    }

    // ── 401: attempt a single token refresh, then retry ─────────────────────────────────────────
    // Skip refresh for login/2fa endpoints — a 401 there means bad credentials, not token expiry.
    const isAuthEndpoint = originalRequest.url?.includes('/auth/login')
      || originalRequest.url?.includes('/super-admin/login')
      || originalRequest.url?.includes('/super-admin/verify-2fa');

    if (status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      originalRequest._retry = true;

      // Deduplicate concurrent 401s — all waiters share one in-flight refresh.
      // NextAuth owns the refresh token (server-side, in the encrypted session JWT).
      // Reading the session triggers NextAuth's jwt() callback, which refreshes the
      // access token when it has expired, then hands us the fresh token.
      if (!_refreshPromise) {
        _refreshPromise = (async () => {
          const { getSession } = await import('next-auth/react');
          const session = await getSession();
          const newToken = session?.user?.accessToken;
          if (newToken && typeof window !== 'undefined') {
            localStorage.setItem('token', newToken);
            apiClient.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
          } else {
            // Session could not produce a valid token — treat as unauthenticated.
            throw new Error('session refresh failed');
          }
        })().finally(() => { _refreshPromise = null; });
      }

      try {
        await _refreshPromise;
        const newToken = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
        if (newToken && originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
        }
        cbRecordSuccess();
        return apiClient(originalRequest);
      } catch {
        // Refresh failed — clear local tokens and redirect to login
        if (typeof window !== 'undefined') {
          localStorage.removeItem('token');
          localStorage.removeItem('tenantId');
          Cookies.remove('token', { path: '/' });
          const localeMatch = window.location.pathname.match(/^\/([a-z]{2})(\/|$)/);
          const currentLocale = localeMatch ? localeMatch[1] : 'en';
          const pathWithoutLocale = window.location.pathname.replace(/^\/([a-z]{2})(\/|$)/, '/');
          const isAdminPath = pathWithoutLocale.startsWith('/admin');
          window.location.href = isAdminPath
            ? `/${currentLocale}/admin/login?returnTo=${encodeURIComponent(pathWithoutLocale)}`
            : `/${currentLocale}/login?returnTo=${encodeURIComponent(pathWithoutLocale)}`;
        }
        return Promise.reject(new Error('Session expired. Please log in again.'));
      }
    }

    // ── 429: respect Retry-After, up to 3 retries ───────────────────────────────────────────────
    if (status === 429) {
      const config = error.config;
      if (!config) return Promise.reject(error);

      const retryCount = (config as any).__retryCount ?? 0;
      if (retryCount >= 3) {
        return Promise.reject(error);
      }
      (config as any).__retryCount = retryCount + 1;

      const retryAfter = parseInt((error.response as any)?.headers?.['retry-after'] ?? '5', 10);
      const delayMs = Math.min(retryAfter * 1000, 60_000);
      await new Promise(resolve => setTimeout(resolve, delayMs));
      return apiClient(config);
    }

    if (shouldNotifyReadFailure(error)) {
      notifyReadFailure(error);
    }

    return Promise.reject(error);
  }
);

// API methods
export const api = {
  // Auth
  auth: {
    login: (email: string, password: string) =>
      apiClient.post('/api/v1/auth/login', { email, password }),
    register: (data: { email: string; password: string; firstName: string; lastName: string; companyName?: string; planId?: string }) =>
      apiClient.post('/api/v1/auth/register', data),
    logout: (refreshToken: string) => apiClient.post('/api/v1/auth/logout', { refreshToken }),
    me: () => apiClient.get('/api/v1/auth/me'),
    refresh: (refreshToken: string) => apiClient.post('/api/v1/auth/refresh', { refreshToken }),
    verify2fa: (data: { email: string; code: string; isBackupCode: boolean }) =>
      apiClient.post('/api/v1/auth/verify-2fa', data),
    // 2FA management lives on the Profile controller, not /auth — the former /auth/2fa/*
    // paths all 404'd. The real flow is: setup() issues the secret + QR + backup codes,
    // then verify(code) confirms and flips TwoFactorEnabled. Status comes from GET /profile.
    twoFactor: {
      status: () => apiClient.get('/api/v1/profile'),
      setup: () => apiClient.post('/api/v1/profile/2fa/enable'),
      verify: (code: string) => apiClient.post('/api/v1/profile/2fa/verify', { code }),
      disable: (code: string) => apiClient.post('/api/v1/profile/2fa/disable', { code }),
    },
    verifyEmail: (token: string, tenantId?: string) => apiClient.post('/api/v1/auth/verify-email', { token, tenantId }),
    resendVerification: () => apiClient.post('/api/v1/auth/resend-verification'),
    forgotPassword: (email: string) => apiClient.post('/api/v1/auth/forgot-password', { email }),
    resetPassword: (data: { token: string; newPassword: string; tenantId?: string }) => apiClient.post('/api/v1/auth/reset-password', data),
  },
  
  // Search
  search: {
    global: (query: string) => apiClient.get('/api/v1/search', { params: { q: query } }),
    getRecent: (limit: number = 5) => apiClient.get('/api/v1/search/recent', { params: { limit } }),
    getSaved: () => apiClient.get('/api/v1/search/saved'),
    saveFilter: (data: { name: string; query: string; searchType: string; filtersJson?: string }) => 
      apiClient.post('/api/v1/search/saved', data),
    deleteSaved: (id: string) => apiClient.delete(`/api/v1/search/saved/${id}`),
  },

  // Credits
  credits: {
    getBalance: (clientId?: string) => apiClient.get('/api/v1/credits/balance', { params: { clientId } }),
    getHistory: (clientId?: string, page: number = 1, pageSize: number = 20) => 
      apiClient.get('/api/v1/credits/history', { params: { clientId, page, pageSize } }),
    addCredit: (data: { amount: number; type: string; description?: string; clientId?: string }) => 
      apiClient.post('/api/v1/credits/add', data),
    createCheckout: (data: { amount: number }) => 
      apiClient.post('/api/v1/credits/checkout', data),
  },

  // OAuth Apps
  oauthApps: {
    getApps: () => apiClient.get('/api/v1/oauth-apps'),
    registerApp: (data: { name: string; description?: string; redirectUris: string[]; scopes: string[] }) => 
      apiClient.post('/api/v1/oauth-apps', data),
    revokeApp: (clientId: string) => apiClient.delete(`/api/v1/oauth-apps/${clientId}`),
    getActiveTokens: () => apiClient.get('/api/v1/oauth-apps/tokens'),
    revokeToken: (tokenId: string) => apiClient.delete(`/api/v1/oauth-apps/tokens/${tokenId}`),
  },

  // Split Payments
  splitPayments: {
    getByBooking: (bookingId: string) => apiClient.get(`/api/v1/split-payments/booking/${bookingId}`),
    createDeposit: (data: { bookingId: string; totalAmount: number; currency: string; depositPercentage?: number }) => 
      apiClient.post('/api/v1/split-payments/deposit', data),
    payInstallment: (paymentId: string, referenceId: string) => 
      apiClient.post(`/api/v1/split-payments/${paymentId}/pay`, { referenceId }),
  },
  
  // Bookings
  bookings: {
    list: (params?: { page?: number; limit?: number; status?: string }, config?: AxiosRequestConfig) =>
      apiClient.get('/api/v1/bookings', { ...config, params }), // Versioned
    get: (id: string, config?: AxiosRequestConfig) => apiClient.get(`/api/v1/bookings/${id}`, config),
    create: (data: any) => apiClient.post('/api/v1/bookings', data),
    createRecurring: (data: any) => apiClient.post('/api/v1/bookings/recurring', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/bookings/${id}`, data),
    cancel: (id: string, reason?: string) =>
      apiClient.post(`/api/v1/bookings/${id}/cancel`, { reason }),
  },
  
  // Clients
  clients: {
    list: (params?: { page?: number; limit?: number; search?: string }, config?: AxiosRequestConfig) =>
      apiClient.get('/api/v1/clients', { ...config, params }),
    advancedSearch: (params?: any) =>
      apiClient.get('/api/v1/clients/advanced-search', { params }),
    get: (id: string) => apiClient.get(`/api/v1/clients/${id}`),
    create: (data: any) => apiClient.post('/api/v1/clients', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/clients/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/clients/${id}`),
    notes: (id: string) => apiClient.get(`/api/v1/clients/${id}/notes`),
    addNote: (id: string, data: { content: string; isPrivate: boolean; category?: string }) =>
      apiClient.post(`/api/v1/clients/${id}/notes`, data),
    communications: (id: string) => apiClient.get(`/api/v1/clients/${id}/communications`),
    segment: (data: any) => apiClient.post('/api/v1/clients/segment', data),
    loyalty: (id: string) => apiClient.get(`/api/v1/clients/${id}/loyalty`),
    adjustLoyalty: (id: string, data: { points: number; reason: string }) => 
      apiClient.post(`/api/v1/clients/${id}/loyalty/adjust`, data),
    referrals: (id: string) => apiClient.get(`/api/v1/clients/${id}/referrals`),
    createReferral: (id: string, data: { referredClientId: string }) =>
      apiClient.post(`/api/v1/clients/${id}/referrals`, data),
  },

  communications: {
    sendSms: (clientId: string, message: string) => 
      apiClient.post('/api/v1/communications/sms', { clientId, message }),
  },

  // Products
  products: {
    list: (params?: any) => apiClient.get('/api/v1/products', { params }),
    get: (id: string) => apiClient.get(`/api/v1/products/${id}`),
    create: (data: any) => apiClient.post('/api/v1/products', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/products/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/products/${id}`),
  },

  // Reviews — full management: import, respond, request, stats
  reviews: {
    list: (params?: any) => apiClient.get('/api/v1/reviews', { params }),
    get: (id: string) => apiClient.get(`/api/v1/reviews/${id}`),
    stats: () => apiClient.get('/api/v1/reviews/stats'),
    add: (data: any) => apiClient.post('/api/v1/reviews', data),
    respond: (id: string, data: { responseText: string }) => apiClient.patch(`/api/v1/reviews/${id}/respond`, data),
    reply: (id: string, data: { response: string }) => apiClient.post(`/api/v1/reviews/${id}/reply`, data),
    report: (id: string) => apiClient.post(`/api/v1/reviews/${id}/report`),
    // Review requests
    requests: (params?: any) => apiClient.get('/api/v1/reviews/requests', { params }),
    sendRequest: (data: any) => apiClient.post('/api/v1/reviews/requests', data),
    completeRequest: (id: string) => apiClient.patch(`/api/v1/reviews/requests/${id}/complete`),
  },

  // Blog — SEO content management for tenants
  blog: {
    list: (params?: any) => apiClient.get('/api/v1/blog', { params }),
    get: (id: string) => apiClient.get(`/api/v1/blog/${id}`),
    create: (data: any) => apiClient.post('/api/v1/blog', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/blog/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/blog/${id}`),
    publish: (id: string) => apiClient.post(`/api/v1/blog/${id}/publish`),
  },

  // SEO — audit, keyword suggestions
  seoTools: {
    audit: () => apiClient.get('/api/seo/audit'),
    keywords: () => apiClient.get('/api/seo/keywords'),
  },

  // Notifications
  notifications: {
    list: (params?: any) => apiClient.get('/api/v1/notifications', { params }),
    markAsRead: (id: string) => apiClient.patch(`/api/v1/notifications/${id}/read`),
    markAllAsRead: () => apiClient.patch('/api/v1/notifications/read-all'),
  },

  // Services
  services: {
    list: (config?: AxiosRequestConfig) => apiClient.get('/api/v1/services', config),
    get: (id: string) => apiClient.get(`/api/v1/services/${id}`),
    create: (data: any) => apiClient.post('/api/v1/services', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/services/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/services/${id}`),
  },
  
  // Payments
  payments: {
    list: (params?: any) => apiClient.get('/api/v1/payments', { params }),
    get: (id: string) => apiClient.get(`/api/v1/payments/${id}`),
    create: (data: any) => apiClient.post('/api/v1/payments', data),
    split: (bookingId: string, splits: any[]) => apiClient.post('/api/v1/payments/split', { bookingId, splits }),
  },

  // Settings
  settings: {
    getBusiness: () => apiClient.get('/api/v1/settings/business'),
    updateBusiness: (data: any) => apiClient.put('/api/v1/settings/business', data),
    getNotifications: () => apiClient.get('/api/v1/settings/notifications'),
    updateNotifications: (data: any) => apiClient.put('/api/v1/settings/notifications', data),
    getAppearance: () => apiClient.get('/api/v1/settings/appearance'),
    updateAppearance: (data: any) => apiClient.put('/api/v1/settings/appearance', data),
  },

  // Profile (New)
  profile: {
    get: () => apiClient.get('/api/v1/profile'),
    update: (data: any) => apiClient.put('/api/v1/profile', data),
    uploadAvatar: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return apiClient.post('/api/v1/profile/avatar', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
    },
    deleteAvatar: () => apiClient.delete('/api/v1/profile/avatar'),
    changePassword: (data: any) => apiClient.post('/api/v1/profile/change-password', data),
  },
  
  // Staff
  staff: {
    list: (config?: AxiosRequestConfig) => apiClient.get('/api/v1/staff', config),
    get: (id: string) => apiClient.get(`/api/v1/staff/${id}`),
    update: (id: string, data: any) => apiClient.put(`/api/v1/staff/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/staff/${id}`),
    availability: (id: string, date: string) =>
      apiClient.get(`/api/v1/staff/${id}/availability`, { params: { date } }),
    shifts: (id: string) => apiClient.get(`/api/v1/staff/${id}/shifts`),
    createShift: (id: string, data: { locationId: string; startTime: string; endTime: string }) =>
      apiClient.post(`/api/v1/staff/${id}/shifts`, data),
    clockIn: (id: string, data: { shiftId?: string; latLong?: string }) =>
      apiClient.post(`/api/v1/staff/${id}/clock-in`, data),
    clockOut: (id: string) => apiClient.post(`/api/v1/staff/${id}/clock-out`),
    commissions: (id: string) => apiClient.get(`/api/v1/staff/${id}/commissions`),
    stats: () => apiClient.get('/api/v1/staff/stats'),
    ranking: (params?: { start?: string; end?: string; top?: number }) =>
      apiClient.get('/api/v1/staff/ranking', { params }),
    utilization: (params?: { start?: string; end?: string }) =>
      apiClient.get('/api/v1/staff/utilization', { params }),
    requestSwap: (data: any) => apiClient.post('/api/v1/staff/shifts/swap-request', data),
    acceptSwap: (swapId: string) => apiClient.post(`/api/v1/staff/shifts/swap/${swapId}/accept`),
    approveSwap: (swapId: string) => apiClient.post(`/api/v1/staff/shifts/swap/${swapId}/approve`),
  },
  
  // Dashboard
  dashboard: {
    stats: () => apiClient.get('/api/v1/dashboard/stats'),
    recentBookings: () => apiClient.get('/api/v1/dashboard/recent-bookings'),
    revenue: (period: string) => apiClient.get('/api/v1/dashboard/revenue', { params: { period } }),
  },
  
  // Analytics
  analytics: {
    dashboard: () => apiClient.get('/api/v1/analytics/dashboard'),
    bookings: (period?: string) => apiClient.get('/api/v1/analytics/bookings', { params: { period } }),
    revenue: (period?: string) => apiClient.get('/api/v1/analytics/revenue', { params: { period } }),
    clients: (period?: string) => apiClient.get('/api/v1/analytics/clients', { params: { period } }),
    services: (period?: string) => apiClient.get('/api/v1/analytics/services', { params: { period } }),
    staff: (period?: string) => apiClient.get('/api/v1/analytics/staff', { params: { period } }),
    funnel: (period?: string) => apiClient.get('/api/v1/analytics/funnel', { params: { period } }),
    activity: (limit?: number) => apiClient.get('/api/v1/analytics/activity', { params: { limit } }),
  },
  
  // Inventory
  inventory: {
    list: (params?: { page?: number; pageSize?: number; category?: string; lowStock?: boolean }) => apiClient.get('/api/v1/inventory', { params }),
    get: (id: string) => apiClient.get(`/api/v1/inventory/${id}`),
    create: (data: any) => apiClient.post('/api/v1/inventory', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/inventory/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/inventory/${id}`),
    adjust: (id: string, data: any) => apiClient.post(`/api/v1/inventory/${id}/adjust`, data),
    bulkAdjust: (data: { adjustments: any[] }) => apiClient.post('/api/v1/inventory/bulk-adjust', data),
    lowStock: () => apiClient.get('/api/v1/inventory/low-stock'),
    value: () => apiClient.get('/api/v1/inventory/value'),
    sendAlerts: (itemIds: string[]) => apiClient.post('/api/v1/inventory/alerts/send', itemIds),
    transactions: (id: string, params?: { page?: number; pageSize?: number }) => 
      apiClient.get(`/api/v1/inventory/${id}/transactions`, { params }),
  },

  // Loyalty
  loyalty: {
    members: (params?: any) => apiClient.get('/api/v1/loyalty/members', { params }),
    stats: () => apiClient.get('/api/v1/loyalty/stats'),
    rewards: () => apiClient.get('/api/v1/loyalty/rewards'),
    getReward: (id: string) => apiClient.get(`/api/v1/loyalty/rewards/${id}`),
    createReward: (data: any) => apiClient.post('/api/v1/loyalty/rewards', data),
    updateReward: (id: string, data: any) => apiClient.put(`/api/v1/loyalty/rewards/${id}`, data),
    deleteReward: (id: string) => apiClient.delete(`/api/v1/loyalty/rewards/${id}`),
    adjustPoints: (memberId: string, data: { points: number; reason: string }) => apiClient.post(`/api/v1/loyalty/members/${memberId}/adjust`, data),
  },

  // Memberships
  memberships: {
    plans: {
      list: () => apiClient.get('/api/v1/memberships/plans'),
      get: (id: string) => apiClient.get(`/api/v1/memberships/plans/${id}`),
      create: (data: any) => apiClient.post('/api/v1/memberships/plans', data),
      update: (id: string, data: any) => apiClient.put(`/api/v1/memberships/plans/${id}`, data),
      delete: (id: string) => apiClient.delete(`/api/v1/memberships/plans/${id}`),
    },
    subscriptions: {
      list: (params?: any) => apiClient.get('/api/v1/memberships/subscriptions', { params }),
      get: (id: string) => apiClient.get(`/api/v1/memberships/subscriptions/${id}`),
      create: (data: any) => apiClient.post('/api/v1/memberships/subscriptions', data),
      cancel: (id: string, immediately: boolean = false) => apiClient.post(`/api/v1/memberships/subscriptions/${id}/cancel`, { immediately }),
      pause: (id: string, resumeDate?: string) => apiClient.post(`/api/v1/memberships/subscriptions/${id}/pause`, { resumeDate }),
      resume: (id: string) => apiClient.post(`/api/v1/memberships/subscriptions/${id}/resume`),
      useService: (id: string, serviceId: string) => apiClient.post(`/api/v1/memberships/subscriptions/${id}/use-service`, { serviceId }),
    },
    stats: () => apiClient.get('/api/v1/memberships/stats'),
    analytics: () => apiClient.get('/api/v1/memberships/analytics'),
  },

  // Packages
  packages: {
    list: (params?: any) => apiClient.get<IPaginatedResponse<IServicePackage>>('/api/v1/packages', { params }),
    get: (id: string) => apiClient.get<IApiResponse<IServicePackage>>(`/api/v1/packages/${id}`),
    create: (data: Partial<IServicePackage>) => apiClient.post<IApiResponse<IServicePackage>>('/api/v1/packages', data),
    update: (id: string, data: Partial<IServicePackage>) => apiClient.put<IApiResponse<IServicePackage>>(`/api/v1/packages/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/packages/${id}`),
    redeem: (id: string) => apiClient.post<IApiResponse<any>>(`/api/v1/packages/${id}/redeem`),
    analytics: () => apiClient.get('/api/v1/packages/analytics'),
  },

  // Gift Cards
  giftCards: {
    list: (params?: any) => apiClient.get('/api/v1/giftcards', { params }),
    get: (id: string) => apiClient.get(`/api/v1/giftcards/${id}`),
    create: (data: any) => apiClient.post('/api/v1/giftcards', data),
    check: (code: string) => apiClient.get(`/api/v1/giftcards/check/${code}`),
    redeem: (id: string, data: any) => apiClient.post(`/api/v1/giftcards/${id}/redeem`, data),
    refund: (id: string, data: any) => apiClient.post(`/api/v1/giftcards/${id}/refund`, data),
    reload: (id: string, data: any) => apiClient.post(`/api/v1/giftcards/${id}/reload`, data),
    void: (id: string, data: any) => apiClient.post(`/api/v1/giftcards/${id}/void`, data),
  },

  // Coupons
  coupons: {
    list: (params?: any) => apiClient.get<IPaginatedResponse<ICoupon>>('/api/v1/coupons', { params }),
    get: (id: string) => apiClient.get<IApiResponse<ICoupon>>(`/api/v1/coupons/${id}`),
    create: (data: Partial<ICoupon>) => apiClient.post<IApiResponse<ICoupon>>('/api/v1/coupons', data),
    createBatch: (data: any) => apiClient.post('/api/v1/coupons/batch', data),
    update: (id: string, data: Partial<ICoupon>) => apiClient.put<IApiResponse<ICoupon>>(`/api/v1/coupons/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/coupons/${id}`),
    validate: (data: { code: string; clientId?: string; orderAmount?: number }) => 
      apiClient.post<IApiResponse<ICoupon>>('/api/v1/coupons/validate', data),
    apply: (id: string, data: { orderAmount: number; clientId?: string }) => 
      apiClient.post<IApiResponse<ICoupon>>(`/api/v1/coupons/${id}/apply`, data),
    deactivate: (id: string) => apiClient.post(`/api/v1/coupons/${id}/deactivate`),
    duplicate: (id: string) => apiClient.post(`/api/v1/coupons/${id}/duplicate`),
    analytics: () => apiClient.get('/api/v1/coupons/analytics'),
  },

  // Waitlist
  waitlist: {
    list: (params?: any) => apiClient.get<IPaginatedResponse<IWaitlistEntry>>('/api/v1/waitlist', { params }),
    get: (id: string) => apiClient.get<IApiResponse<IWaitlistEntry>>(`/api/v1/waitlist/${id}`),
    add: (data: Partial<IWaitlistEntry>) => apiClient.post<IApiResponse<IWaitlistEntry>>('/api/v1/waitlist', data),
    update: (id: string, data: Partial<IWaitlistEntry>) => apiClient.put<IApiResponse<IWaitlistEntry>>(`/api/v1/waitlist/${id}`, data),
    notify: (id: string) => apiClient.post(`/api/v1/waitlist/${id}/notify`),
    remove: (id: string) => apiClient.delete(`/api/v1/waitlist/${id}`),
    convert: (id: string) => apiClient.post<IApiResponse<IBooking>>(`/api/v1/waitlist/${id}/book`),
    stats: () => apiClient.get('/api/v1/waitlist/stats'),
    export: () => apiClient.get('/api/v1/waitlist/export', { responseType: 'blob' }),
    bulkPriority: (updates: any[]) => apiClient.post('/api/v1/waitlist/bulk-priority', updates),
    position: (id: string) => apiClient.get<IApiResponse<{ position: number }>>(`/api/v1/waitlist/${id}/position`),
  },

  // Resources
  resources: {
    list: () => apiClient.get('/api/v1/resources'),
    get: (id: string) => apiClient.get(`/api/v1/resources/${id}`),
    create: (data: any) => apiClient.post('/api/v1/resources', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/resources/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/resources/${id}`),
  },

  // Forms
  forms: {
    list: (params?: any) => apiClient.get('/api/v1/forms', { params }),
    get: (id: string) => apiClient.get(`/api/v1/forms/${id}`),
    create: (data: any) => apiClient.post('/api/v1/forms', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/forms/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/forms/${id}`),
    responses: (id: string) => apiClient.get(`/api/v1/forms/${id}/responses`),
  },
  
  // Health
  health: {
    check: () => apiClient.get('/api/v1/health'),
    ready: () => apiClient.get('/api/v1/health/ready'),
  },

  // Calendar Integration
  calendar: {
    getAuthUrl: (provider: string, staffId: string) => 
      apiClient.get('/api/v1/calendar/auth-url', { params: { provider, staffId } }),
    connect: (data: { provider: string; staffId: string; code: string }) =>
      apiClient.post('/api/v1/calendar/connect', data),
    sync: (staffId: string) => apiClient.post(`/api/v1/calendar/sync/${staffId}`),
    connections: (staffId: string) => apiClient.get(`/api/v1/calendar/connections/${staffId}`),
  },

  // Marketing Campaigns
  campaigns: {
    list: (params?: { page?: number; pageSize?: number; status?: string; type?: string }) =>
      apiClient.get('/api/v1/campaigns', { params }),
    get: (id: string) => apiClient.get(`/api/v1/campaigns/${id}`),
    create: (data: any) => apiClient.post('/api/v1/campaigns', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/campaigns/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/campaigns/${id}`),
    send: (id: string) => apiClient.post(`/api/v1/campaigns/${id}/send`),
    schedule: (id: string, scheduledAt: string) => 
      apiClient.post(`/api/v1/campaigns/${id}/schedule`, { scheduledAt }),
    analytics: (id: string) => apiClient.get(`/api/v1/campaigns/${id}/analytics`),
    timeline: (id: string, params?: { start?: string; end?: string }) =>
      apiClient.get(`/api/v1/campaigns/${id}/timeline`, { params }),
    performanceAggregate: (params?: { start?: string; end?: string }) =>
      apiClient.get('/api/v1/campaigns/performance-aggregate', { params }),
    autoResponders: () => apiClient.get('/api/v1/campaigns/auto-responders'),
    saveAutoResponder: (data: any) => apiClient.post('/api/v1/campaigns/auto-responders', data),
    segments: {
      list: (params?: any) => apiClient.get('/api/v1/clients/segments', { params })
    },
  },

  // Public Booking
  publicBooking: {
    getLanding: (slug: string) => apiClient.get(`/api/v1/booking/${slug}`),
    getServices: (slug: string) => apiClient.get(`/api/v1/booking/${slug}/services`),
    getStaff: (slug: string, serviceId?: string) => 
      apiClient.get(`/api/v1/booking/${slug}/staff`, { params: { serviceId } }),
    getAvailability: (slug: string, serviceId: string, staffId: string | null, date: string) =>
      apiClient.get(`/api/v1/booking/${slug}/availability`, { params: { serviceId, staffId, date } }),
    book: (slug: string, data: any) => apiClient.post(`/api/v1/booking/${slug}/book`, data),
    getStatus: (slug: string, id: string) => apiClient.get(`/api/v1/booking/${slug}/status/${id}`),
  },

  // Invitations
  invitations: {
    list: () => apiClient.get('/api/v1/invitation'),
    create: (data: { email: string; role: string }) => apiClient.post('/api/v1/invitation', data),
    delete: (id: string) => apiClient.delete(`/api/v1/invitation/${id}`),
    getPublic: (token: string) => apiClient.get(`/api/v1/public/publicinvitation/${token}`),
    accept: (data: { token: string; firstName: string; lastName: string; password: string }) => 
      apiClient.post('/api/v1/public/publicinvitation/accept', data),
  },

  // Billing & Subscriptions (New)
  billing: {
    getSubscription: () => apiClient.get('/api/v1/billing/subscription'),
    getPlans: () => apiClient.get('/api/v1/billing/plans'),
    createCheckout: (data: { planId: string; isAnnual: boolean; promoCode?: string }) =>
      apiClient.post('/api/v1/billing/checkout', data),
    createPortalSession: (returnUrl: string) =>
      apiClient.post('/api/v1/billing/portal', { returnUrl }),
    getInvoices: (params?: { page?: number; pageSize?: number }) =>
      apiClient.get('/api/v1/billing/invoices', { params }),
    downloadInvoice: (id: string) => 
      apiClient.get(`/api/v1/billing/invoices/${id}/pdf`, { responseType: 'blob' }),
    getUsage: () => apiClient.get('/api/v1/billing/usage'),
    applyPromoCode: (code: string) => apiClient.post('/api/v1/billing/promo-code', { code }),
    cancelSubscription: (reason?: string) => apiClient.post('/api/v1/billing/cancel', { reason }),
    updateInvoiceSettings: (data: { prefix: string; nextNumber: number }) =>
      apiClient.post('/api/v1/billing/settings/invoice', data),
    updateAiBudget: (budget: number) => apiClient.put('/api/v1/subscriptions/ai-budget', { budget }),
  },  

  // Usage Dashboard (Historical/Charts)
  usageDashboard: {
    get: () => apiClient.get('/api/v1/usagedashboard'),
    getHistory: (params?: { from?: string; to?: string; granularity?: string }) =>
      apiClient.get('/api/v1/usagedashboard/history', { params }),
    getAiUsage: (params?: { from?: string; to?: string }) =>
      apiClient.get('/api/v1/usagedashboard/ai', { params }),
    exportCsv: (params?: { from?: string; to?: string }) =>
      apiClient.get('/api/v1/usagedashboard/export', { 
        params,
        responseType: 'blob'
      }),
  },

  // Bulk Data Operations
  import: {
    analyze: (file: File, entityType: string = 'clients') => {
      const formData = new FormData();
      formData.append('file', file);
      return apiClient.post(`/api/v1/import/analyze?entityType=${entityType}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
    },
    start: (file: File, entityType: string, mapping: any) => {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('entityType', entityType);
      formData.append('mappingJson', JSON.stringify(mapping));
      return apiClient.post('/api/v1/import/start', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
    },
    getStatus: (jobId: string) => apiClient.get(`/api/v1/import/status/${jobId}`),
    getHistory: (limit: number = 10) => apiClient.get('/api/v1/import/history', { params: { limit } }),
    getTemplate: (entityType: string) => apiClient.get(`/api/v1/import/template/${entityType}`, { responseType: 'blob' }),
  },

  // Data Export
  export: {
    clients: () => apiClient.get('/api/v1/export/clients', { responseType: 'blob' }),
    bookings: () => apiClient.get('/api/v1/export/bookings', { responseType: 'blob' }),
  },

  // Data Migration Wizard
  migration: {
    getOverview: (data: { provider: string; apiKey: string; extraCredentials?: string }) =>
      apiClient.post('/api/v1/migration/overview', data),
    start: (data: { provider: string; apiKey: string; extraCredentials?: string; importServices: boolean; importStaff: boolean; importBookings: boolean }) =>
      apiClient.post('/api/v1/migration/start', data),
  },

  // White-Label Domains
  domains: {
    list: () => apiClient.get('/api/v1/domain'),
    add: (hostname: string) => apiClient.post('/api/v1/domain', { hostname }),
    verify: (id: string) => apiClient.post(`/api/v1/domain/${id}/verify`),
    delete: (id: string) => apiClient.delete(`/api/v1/domain/${id}`),
  },
  
  // Super Admin
  agreements: {
    list: (params?: { status?: string }, config?: AxiosRequestConfig) =>
      apiClient.get('/api/v1/admin/agreements', { ...config, params }),
    upsert: (tenantId: string, type: 'HipaaBaa' | 'Sla', data: Record<string, unknown>) =>
      apiClient.put(`/api/v1/admin/agreements/${tenantId}/${type}`, data),
  },

  enterprise: {
    leads: (params?: { page?: number; pageSize?: number }, config?: AxiosRequestConfig) =>
      apiClient.get('/api/v1/enterprise/leads', { ...config, params }),
    updateLeadStatus: (id: string, status: string) =>
      apiClient.patch(`/api/v1/enterprise/leads/${id}/status`, { status }),
  },

  superAdmin: {
    // Auth
    login: (email: string, password: string) => 
      apiClient.post('/api/v1/super-admin/login', { email, password }),
    register: (data: { email: string; password: string; firstName: string; lastName: string }) =>
      apiClient.post('/api/v1/super-admin/register', data),
    setup2fa: (data: { email: string; password: string }) =>
      apiClient.post('/api/v1/super-admin/setup-2fa', data),
    verify2fa: (data: { email: string; code: string }) =>
      apiClient.post('/api/v1/super-admin/verify-2fa', data),

    // Management
    tenants: (params?: any) => apiClient.get('/api/v1/super-admin/tenants', { params }),
    resetUser2fa: (userId: string) => apiClient.post(`/api/v1/super-admin/users/${userId}/reset-2fa`),
    analytics: () => apiClient.get('/api/v1/super-admin/analytics'),
    revenueTrend: () => apiClient.get('/api/v1/super-admin/analytics/revenue-trend'),
    tierDistribution: () => apiClient.get('/api/v1/super-admin/analytics/tier-distribution'),
    plans: () => apiClient.get('/api/v1/super-admin/plans'),
    health: () => apiClient.get('/api/v1/super-admin/health'),
    auditLogs: (params?: any) => apiClient.get('/api/v1/super-admin/audit-logs', { params }),
    getSettings: () => apiClient.get('/api/v1/super-admin/settings'),
    updateSettings: (data: any) => apiClient.put('/api/v1/super-admin/settings', data),
    aiOverview: (days?: number) => apiClient.get('/api/v1/super-admin/ai/overview', { params: { days } }),
    aiTenantUsage: (id: string, days?: number) => apiClient.get(`/api/v1/super-admin/ai/tenants/${id}`, { params: { days } }),
    securityOverview: (days?: number) => apiClient.get('/api/v1/super-admin/security/overview', { params: { days } }),
  },

  /**
   * Customer-specific entitlement overrides (SuperAdmin only).
   *
   * Note the route prefix: these live under /api/admin/entitlements, NOT /api/v1/... —
   * EntitlementsAdminController is routed outside the versioned group, matching the other
   * /api/admin/* controllers.
   */
  entitlementsAdmin: {
    catalog: () => apiClient.get('/api/admin/entitlements/catalog'),
    effective: (tenantId: string) => apiClient.get(`/api/admin/entitlements/${tenantId}`),
    overrides: (tenantId: string) => apiClient.get(`/api/admin/entitlements/${tenantId}/overrides`),
    upsertOverride: (
      tenantId: string,
      featureKey: string,
      data: { isEnabled: boolean; numericLimit?: number | null; startsAt?: string | null; expiresAt?: string | null; reason?: string | null },
    ) => apiClient.put(`/api/admin/entitlements/${tenantId}/overrides/${featureKey}`, data),
    deleteOverride: (tenantId: string, featureKey: string) =>
      apiClient.delete(`/api/admin/entitlements/${tenantId}/overrides/${featureKey}`),
    unboundedGrants: () => apiClient.get('/api/admin/entitlements/audit/unbounded-grants'),
    invalidateAllCaches: () => apiClient.post('/api/admin/entitlements/cache/invalidate-all'),
  },
  
  // Performance & Commissions
  performance: {
    staff: (start?: string, end?: string) => 
      apiClient.get('/api/v1/performance/staff', { params: { start, end } }),
    commissions: (start?: string, end?: string) => 
      apiClient.get('/api/v1/performance/commissions', { params: { start, end } }),
  },

  // AI & Chatbot (New)
  ai: {
    generateText: (prompt: string, context?: any) =>
      apiClient.post('/api/v1/ai/generate', { prompt, context }),
    generateImage: (prompt: string) =>
      apiClient.post('/api/v1/ai/generate-image', { prompt }),
    analyzeSentiment: (text: string) =>
      apiClient.post('/api/v1/ai/analyze-sentiment', { text }),
  },

  // AI Dashboard — gated behind the `ai_insights` plan feature (403 when not entitled)
  aiDashboard: {
    metrics: () => apiClient.get('/api/v1/aidashboard/metrics'),
    recommendations: () => apiClient.get('/api/v1/aidashboard/recommendations'),
    forecast: () => apiClient.get('/api/v1/aidashboard/forecast'),
    weeklySummary: () => apiClient.get('/api/v1/aidashboard/weekly-summary'),
  },

  chatbot: {
    getSettings: () => apiClient.get('/api/v1/aichatbot/settings'),
    updateSettings: (data: any) => apiClient.put('/api/v1/aichatbot/settings', data),
    getKnowledgeBase: () => apiClient.get('/api/v1/aichatbot/kb'),
    addKnowledgeBase: (data: { category: string; question: string; answer: string }) =>
      apiClient.post('/api/v1/aichatbot/train', data),
    deleteKnowledgeBase: (id: string) => apiClient.delete(`/api/v1/aichatbot/kb/${id}`),
    getConversations: (params?: any) => apiClient.get('/api/v1/aichatbot/conversations', { params }),
    getStats: () => apiClient.get('/api/v1/aichatbot/stats'),
  },

  // Marketing Funnels
  funnels: {
    list: (params?: { status?: string; page?: number; pageSize?: number }) =>
      apiClient.get('/api/v1/marketingfunnels', { params }),
    get: (id: string) => apiClient.get(`/api/v1/marketingfunnels/${id}`),
    create: (data: any) => apiClient.post('/api/v1/marketingfunnels', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/marketingfunnels/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/marketingfunnels/${id}`),
    activate: (id: string) => apiClient.post(`/api/v1/marketingfunnels/${id}/activate`),
    pause: (id: string) => apiClient.post(`/api/v1/marketingfunnels/${id}/pause`),
    analytics: (id: string) => apiClient.get(`/api/v1/marketingfunnels/${id}/analytics`),
    addStep: (id: string, data: any) => apiClient.post(`/api/v1/marketingfunnels/${id}/steps`, data),
    updateStep: (id: string, stepId: string, data: any) =>
      apiClient.put(`/api/v1/marketingfunnels/${id}/steps/${stepId}`, data),
    deleteStep: (id: string, stepId: string) =>
      apiClient.delete(`/api/v1/marketingfunnels/${id}/steps/${stepId}`),
  },

  // SSO / SAML
  sso: {
    getConfig: () => apiClient.get('/api/v1/sso/config'),
    updateConfig: (data: any) => apiClient.put('/api/v1/sso/config', data),
    testConnection: () => apiClient.post('/api/v1/sso/test'),
    deleteConfig: () => apiClient.delete('/api/v1/sso/config'),
    getProviders: () => apiClient.get('/api/v1/sso/providers'),
  },

  // Resource Scheduling (extends existing resources)
  resourceScheduling: {
    getAvailability: (id: string, date: string, days: number = 1) =>
      apiClient.get(`/api/v1/resources/${id}/availability`, { params: { date, days } }),
    book: (id: string, data: { title?: string; startTime: string; endTime: string; bookingId?: string; notes?: string }) =>
      apiClient.post(`/api/v1/resources/${id}/book`, data),
    getBookings: (id: string, from?: string, to?: string) =>
      apiClient.get(`/api/v1/resources/${id}/bookings`, { params: { from, to } }),
    cancelBooking: (resourceId: string, bookingId: string) =>
      apiClient.delete(`/api/v1/resources/${resourceId}/bookings/${bookingId}`),
    getSchedule: (date?: string, resourceIds?: string) =>
      apiClient.get('/api/v1/resources/schedule', { params: { date, resourceIds } }),
  },

  // Deals / Sales Pipeline
  deals: {
    list: (params?: any) => apiClient.get('/api/v1/salespipeline/deals', { params }),
    get: (id: string) => apiClient.get(`/api/v1/salespipeline/deals/${id}`),
    create: (data: any) => apiClient.post('/api/v1/salespipeline/deals', data),
    update: (id: string, data: any) => apiClient.put(`/api/v1/salespipeline/deals/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/salespipeline/deals/${id}`),
    move: (id: string, stageId: string) => apiClient.post(`/api/v1/salespipeline/deals/${id}/move`, { stageId }),
    // Stages are served by PipelineStagesController at /pipelinestages — there is no
    // /sales-pipeline/stages route, so these previously 404'd and the Deals board stayed empty.
    stages: () => apiClient.get('/api/v1/pipelinestages'),
    createStage: (data: any) => apiClient.post('/api/v1/pipelinestages', data),
  },

  // Users
  users: {
    list: () => apiClient.get('/api/v1/users'),
  },

  // Webhooks
  webhooks: {
    getEndpoints: () => apiClient.get('/api/v1/webhooks/endpoints'),
    createEndpoint: (data: { name: string; url: string; events: string[] }) => 
      apiClient.post('/api/v1/webhooks/endpoints', data),
    updateEndpoint: (id: string, data: { name?: string; url?: string; events?: string[]; isActive?: boolean }) => 
      apiClient.put(`/api/v1/webhooks/endpoints/${id}`, data),
    deleteEndpoint: (id: string) => apiClient.delete(`/api/v1/webhooks/endpoints/${id}`),
    getDeliveries: (params?: { endpointId?: string; limit?: number }) => 
      apiClient.get('/api/v1/webhooks/deliveries', { params }),
    resendDelivery: (id: string) => apiClient.post(`/api/v1/webhooks/deliveries/${id}/resend`),
    clearDeliveries: (endpointId: string) => apiClient.delete(`/api/v1/webhooks/endpoints/${endpointId}/deliveries`),
    testEndpoint: (id: string) => apiClient.post(`/api/v1/webhooks/endpoints/${id}/test`),
    getEventTypes: () => apiClient.get('/api/v1/webhooks/events'),
  },

  // Onboarding
  onboarding: {
    getChecklist: () => apiClient.get('/api/v1/onboarding/checklist'),
    completeStep: (stepId: string) => apiClient.post(`/api/v1/onboarding/checklist/${stepId}/complete`),
    dismiss: () => apiClient.post('/api/v1/onboarding/dismiss'),
    getSampleData: () => apiClient.get('/api/v1/onboarding/sample-data'),
    seedSampleData: (templateId: string) => apiClient.post(`/api/v1/onboarding/sample-data/${templateId}`),
  },

  // Audit
  audit: {
    getLogs: (params: any) => apiClient.get('/api/v1/audit', { params }),
    getSummary: (params: any) => apiClient.get('/api/v1/audit/summary', { params }),
    exportCsv: (params: any) => apiClient.get('/api/v1/audit/export/csv', { params, responseType: 'blob' }),
    exportJson: (params: any) => apiClient.get('/api/v1/audit/export/json', { params, responseType: 'blob' }),
    enqueueExport: (data: any) => apiClient.post('/api/v1/audit/export/enqueue', data),
  },
  // White-label & Agency
  whitelabel: {
    getConfig: () => apiClient.get('/api/v1/whitelabel'),
    updateConfig: (data: any) => apiClient.put('/api/v1/whitelabel', data),
    verifyDomain: () => apiClient.post('/api/v1/whitelabel/verify-domain'),
    verifyEmailDomain: () => apiClient.post('/api/v1/whitelabel/verify-email-domain'),
    getSubAccounts: () => apiClient.get('/api/v1/whitelabel/sub-accounts'),
    createSubAccount: (data: { businessName: string; slug: string; sector: string }) => 
      apiClient.post('/api/v1/whitelabel/sub-accounts', data),
    getAgencyBilling: () => apiClient.get('/api/v1/whitelabel/billing'),
  },

  // Marketplace (New)
  marketplace: {
    getFeaturedListings: (params?: { city?: string; category?: string; search?: string }) =>
      apiClient.get('/api/v1/marketplace/featured-listings', { params }),
    getLeadFees: (tenantId: string) =>
      apiClient.get(`/api/v1/marketplace/tenants/${tenantId}/lead-fees`),
    purchasePremiumBadge: (tenantId: string) =>
      apiClient.post(`/api/v1/marketplace/tenants/${tenantId}/premium-badge`),
    getAdRevenueShare: () =>
      apiClient.get('/api/v1/marketplace/ads/revenue-share'),
    getApps: () => apiClient.get('/api/v1/marketplace/apps'),
  },
  
  // Marketing Automation (Phase 5.4)
  marketingAutomation: {
    dashboard: () => apiClient.get('/api/v1/marketingautomation/dashboard'),
    forecasts: (horizonDays: number = 30) => 
      apiClient.get('/api/v1/marketingautomation/forecasts', { params: { horizonDays } }),
    actions: (count: number = 20) => 
      apiClient.get('/api/v1/marketingautomation/actions', { params: { count } }),
    getIntegrations: () => 
      apiClient.get('/api/v1/marketingautomation/integrations'),
    connectIntegration: (platform: string) => 
      apiClient.post('/api/v1/marketingautomation/integrations/connect', { platform }),
    toggle: (isEnabled: boolean) => 
      apiClient.post('/api/v1/marketingautomation/toggle-autonomous', { isEnabled }),
    onboard: (data: { businessUrl: string; primaryGoal: string; targetRegions?: string }) => 
      apiClient.post('/api/v1/marketingautomation/onboard', data),
  },
};


export { locationsApi } from './api.locations';
export { analyticsApi } from './api.analytics';
export default api;

