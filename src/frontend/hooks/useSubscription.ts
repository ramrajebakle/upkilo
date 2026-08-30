import { useEffect } from 'react';
import { useSubscriptionStore } from '@/store/subscriptionStore';
import type { FeatureKey } from '@/lib/featureKeys';
import { UNLIMITED } from '@/lib/featureKeys';

/**
 * Feature entitlements for the current tenant.
 *
 * Safe to mount anywhere, including shared layout code: it reads
 * /api/v1/subscriptions/entitlements, which any authenticated role may call. It deliberately
 * does NOT fetch billing usage — that endpoint is Owner-gated, and calling it from the layout
 * meant every non-Owner user got a 403 on every page. Use `useUsage` where consumption figures
 * are actually needed.
 */
export function useSubscription() {
  const {
    entitlements,
    entitlementsLoading: isLoading,
    entitlementsLoaded: hasLoaded,
    entitlementsError: error,
    fetchEntitlements,
  } = useSubscriptionStore();

  useEffect(() => {
    if (!hasLoaded && !isLoading) {
      fetchEntitlements();
    }
  }, [hasLoaded, isLoading, fetchEntitlements]);

  /**
   * Whether the tenant may use a feature.
   *
   * Typed to FeatureKey so a gate cannot be written against a name the catalogue does not
   * contain — the defect that made every gate in the app deny unconditionally.
   *
   * Returns false while entitlements are still loading, so callers must not treat a false here
   * as a settled denial; FeatureGate checks isLoading first for exactly that reason.
   */
  const hasFeature = (featureName: FeatureKey): boolean =>
    !!entitlements?.features?.[featureName];

  /** Effective ceiling for a numeric feature: -1 unlimited, 0 none. */
  const getLimit = (featureName: FeatureKey): number =>
    entitlements?.limits?.[featureName] ?? 0;

  return {
    entitlements,
    planName: entitlements?.planName ?? '',
    subscriptionStatus: entitlements?.subscriptionStatus ?? '',
    isServiceEntitled: entitlements?.isServiceEntitled ?? false,
    periodEnd: entitlements?.currentPeriodEnd ?? null,
    isLoading,
    hasLoaded,
    error,
    refresh: fetchEntitlements,
    hasFeature,
    getLimit,
    UNLIMITED,
  };
}

/**
 * Billing usage counters. Fetched on demand rather than by the layout, because
 * BillingController is [Authorize(Roles = "Owner")] and 403s for every other role.
 *
 * Callers must handle `usage` being null: that is the normal result for a non-Owner, not an
 * error state worth surfacing to the user.
 */
export function useUsage() {
  const {
    usage,
    usageLoading: isLoading,
    usageLoaded: hasLoaded,
    usageError: error,
    fetchUsage,
  } = useSubscriptionStore();

  useEffect(() => {
    if (!hasLoaded && !isLoading && !error) {
      fetchUsage();
    }
  }, [hasLoaded, isLoading, error, fetchUsage]);

  return { usage, isLoading, hasLoaded, error, refresh: fetchUsage };
}
