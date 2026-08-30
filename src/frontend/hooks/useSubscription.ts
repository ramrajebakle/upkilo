import { useEffect } from 'react';
import { useSubscriptionStore } from '@/store/subscriptionStore';
import type { FeatureKey } from '@/lib/featureKeys';

export function useSubscription() {
  const { usage, isLoading, error, hasLoaded, fetchUsage } = useSubscriptionStore();

  useEffect(() => {
    if (!hasLoaded && !isLoading) {
      fetchUsage();
    }
  }, [hasLoaded, isLoading, fetchUsage]);

  /**
   * Whether the tenant may use a feature, per the entitlements the API resolved.
   *
   * Typed to FeatureKey so a gate cannot be written against a name the catalogue does not
   * contain — the defect that made every gate in the app deny unconditionally.
   *
   * Returns false while usage is still loading, so callers must not treat a false here as a
   * settled denial; FeatureGate checks isLoading first for exactly that reason.
   */
  const hasFeature = (featureName: FeatureKey): boolean => {
    if (!usage) return false;
    return !!usage.enabledFeatures[featureName];
  };

  const getLimit = (metricKey: keyof Omit<typeof usage, 'periodStart' | 'periodEnd' | 'enabledFeatures' | 'storage'>): { used: number; limit: number } => {
    if (!usage) return { used: 0, limit: 0 };
    return usage[metricKey] as any;
  };

  const isLimitReached = (metricKey: keyof Omit<typeof usage, 'periodStart' | 'periodEnd' | 'enabledFeatures' | 'storage'>): boolean => {
    const { used, limit } = getLimit(metricKey);
    return limit !== -1 && used >= limit; // Assuming -1 means unlimited
  };

  return {
    usage,
    isLoading,
    error,
    hasLoaded,
    fetchUsage,
    hasFeature,
    getLimit,
    isLimitReached
  };
}
