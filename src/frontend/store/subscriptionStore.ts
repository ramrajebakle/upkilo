import { create } from 'zustand';
import { api } from '@/lib/api';

export interface UsageMetric {
  used: number;
  limit: number;
}

export interface StorageMetric {
  usedBytes: number;
  limitBytes: number;
}

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

interface SubscriptionState {
  usage: UsageSummary | null;
  isLoading: boolean;
  error: string | null;
  hasLoaded: boolean;
  
  fetchUsage: () => Promise<void>;
  clear: () => void;
}

export const useSubscriptionStore = create<SubscriptionState>((set) => ({
  usage: null,
  isLoading: false,
  error: null,
  hasLoaded: false,

  fetchUsage: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await api.billing.getUsage();
      set({ 
        usage: response.data.usage, 
        isLoading: false,
        hasLoaded: true 
      });
    } catch (err: any) {
      console.error('Failed to fetch subscription usage:', err);
      set({ 
        isLoading: false, 
        error: err?.response?.data?.message || 'Failed to load subscription usage' 
      });
    }
  },

  clear: () => set({ usage: null, hasLoaded: false, error: null })
}));
