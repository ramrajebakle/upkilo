import { useQuery } from '@tanstack/react-query';
import { TenantData } from '@/components/platform/HealthMatrix';
import { AIInsightCardProps } from '@/components/ai/insight-card/AIInsightCard';

// Fetch Tenants
export const useTenants = () => {
  return useQuery<TenantData[]>({
    queryKey: ['tenants'],
    queryFn: async () => {
      const res = await fetch('/api/platform/tenants');
      if (!res.ok) throw new Error('Failed to fetch tenants');
      return res.json();
    },
  });
};

// Insights have a slightly different structure in the API vs the Component props (specifically the actions)
// We will type the raw API response and transform it if necessary, but for now we assume they match enough
export interface RawInsight extends Omit<AIInsightCardProps, 'actions' | 'onDismiss'> {
  id: string;
  actions: { id: string; label: string; primary: boolean }[];
}

export const useInsights = () => {
  return useQuery<RawInsight[]>({
    queryKey: ['insights'],
    queryFn: async () => {
      const res = await fetch('/api/platform/insights');
      if (!res.ok) throw new Error('Failed to fetch insights');
      return res.json();
    },
  });
};
