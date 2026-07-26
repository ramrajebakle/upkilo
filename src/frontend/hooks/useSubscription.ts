import { useEffect } from 'react';
import { useSubscriptionStore } from '@/store/subscriptionStore';

export function useSubscription() {
  const { usage, isLoading, error, hasLoaded, fetchUsage } = useSubscriptionStore();

  useEffect(() => {
    if (!hasLoaded && !isLoading) {
      fetchUsage();
    }
  }, [hasLoaded, isLoading, fetchUsage]);

  const hasFeature = (featureName: string): boolean => {
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
