import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api';
import { AxiosResponse } from 'axios';

const fetchMetrics = (url: string) => apiClient.get(url).then((res: AxiosResponse) => res.data);

export interface AnalyticsMetrics {
  totalClients: number;
  activeSubscriptions: number;
  monthlyRevenue: number;
  pendingAppointments: number;
  growth: {
    clients: number;
    revenue: number;
  };
}

const METRICS_FALLBACK: AnalyticsMetrics = {
  totalClients: 0,
  activeSubscriptions: 0,
  monthlyRevenue: 0,
  pendingAppointments: 0,
  growth: { clients: 0, revenue: 0 },
};

export function useMetrics() {
  const { data, error, isLoading, refetch } = useQuery<AnalyticsMetrics>({
    queryKey: ['analytics', 'dashboard'],
    queryFn: () => fetchMetrics('/api/v1/analytics/dashboard'),
    placeholderData: METRICS_FALLBACK,
    staleTime: 60_000,       // treat fresh for 1 min
    refetchInterval: 300_000, // background refresh every 5 min
    refetchOnWindowFocus: true,
  });

  return {
    metrics: data ?? METRICS_FALLBACK,
    isLoading,
    isError: !!error,
    refreshMetrics: refetch,
  };
}
