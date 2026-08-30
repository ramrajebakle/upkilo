import { create } from 'zustand';
import { api } from '@/lib/api';

/**
 * Two separate concerns, deliberately not merged.
 *
 * ENTITLEMENTS answer "may this tenant use X?" and are read by the dashboard layout on every
 * page, for every role. They come from /api/v1/subscriptions/entitlements, which is
 * [Authorize] only.
 *
 * USAGE answers "how much of X have they consumed?" and comes from /api/v1/billing/usage,
 * which sits behind a class-level [Authorize(Roles = "Owner")] on BillingController.
 *
 * These were previously one fetch against the billing endpoint. That was survivable while only
 * five Owner-facing settings pages consumed it, but once the layout began reading feature flags
 * to draw locked nav entries, every Admin, Manager and Staff user hit the Owner-only endpoint on
 * every page load and got a 403 — a console full of failures and, because the store recorded the
 * error rather than any flags, no entitlements at all for non-Owner roles.
 *
 * Splitting them means feature gating works for everyone and consumption figures stay where the
 * authorization policy already put them.
 */

export interface UsageMetric {
  used: number;
  limit: number;
}

export interface StorageMetric {
  usedBytes: number;
  limitBytes: number;
}

/** Owner-only consumption figures. */
export interface UsageSummary {
  staff: UsageMetric;
  locations: UsageMetric;
  bookings: UsageMetric;
  sms: UsageMetric;
  aiCredits: UsageMetric;
  storage: StorageMetric;
  periodStart: string;
  periodEnd: string;
  enabledFeatures: Record<string, boolean>;
}

/** Readable by any authenticated role. */
export interface Entitlements {
  planName: string;
  subscriptionStatus: string;
  isServiceEntitled: boolean;
  currentPeriodEnd: string | null;
  features: Record<string, boolean>;
  limits: Record<string, number>;
}

interface SubscriptionState {
  entitlements: Entitlements | null;
  entitlementsLoading: boolean;
  entitlementsLoaded: boolean;
  entitlementsError: string | null;

  usage: UsageSummary | null;
  usageLoading: boolean;
  usageLoaded: boolean;
  usageError: string | null;

  fetchEntitlements: () => Promise<void>;
  fetchUsage: () => Promise<void>;
  clear: () => void;
}

export const useSubscriptionStore = create<SubscriptionState>((set) => ({
  entitlements: null,
  entitlementsLoading: false,
  entitlementsLoaded: false,
  entitlementsError: null,

  usage: null,
  usageLoading: false,
  usageLoaded: false,
  usageError: null,

  fetchEntitlements: async () => {
    set({ entitlementsLoading: true, entitlementsError: null });
    try {
      const response = await api.entitlements.mine();
      set({
        entitlements: response.data,
        entitlementsLoading: false,
        entitlementsLoaded: true,
      });
    } catch (err: any) {
      // hasLoaded stays false so gates keep rendering their permissive loading state rather
      // than locking a customer out of features they pay for because one request failed.
      console.error('Failed to fetch entitlements:', err);
      set({
        entitlementsLoading: false,
        entitlementsError: err?.response?.data?.message || 'Failed to load entitlements',
      });
    }
  },

  fetchUsage: async () => {
    set({ usageLoading: true, usageError: null });
    try {
      const response = await api.billing.getUsage();
      set({ usage: response.data.usage, usageLoading: false, usageLoaded: true });
    } catch (err: any) {
      // Expected to 403 for non-Owner roles — BillingController is Owner-gated. Callers render
      // without consumption figures rather than treating it as a hard failure.
      set({
        usageLoading: false,
        usageError: err?.response?.data?.message || 'Failed to load usage',
      });
    }
  },

  clear: () =>
    set({
      entitlements: null,
      entitlementsLoaded: false,
      entitlementsError: null,
      usage: null,
      usageLoaded: false,
      usageError: null,
    }),
}));
