import { apiClient } from './api';

export interface AnalyticsPeriod {
    from?: string;
    to?: string;
    period?: string; // "7d", "30d", etc.
}

export interface RevenueAnalytics {
    period: string;
    totalRevenue: number;
    previousPeriodRevenue: number;
    growthRate: number; // percentage
    averageDaily: number;
    data: Array<{ date: string; revenue: number }>;
}

export interface BookingAnalytics {
    period: string;
    totalBookings: number;
    completionRate: number;
    averageValue: number;
    peakHours: Array<{ hour: string; bookings: number }>;
    byStatus: Record<string, number>;
}

export interface ClientAnalytics {
    period: string;
    totalClients: number;
    newClients: number;
    returningClients: number;
    averageLifetimeValue: number;
}

export interface StaffAnalytics {
    period: string;
    topPerformers: Array<{ name: string; bookings: number; revenue: number }>;
}

export interface ServiceAnalytics {
    period: string;
    topServices: Array<{ name: string; bookings: number; revenue: number }>;
}

export interface MarketingAnalytics {
    period: string;
    channels: Array<{ channel: string; conversions: number; revenue: number }>;
}

export const analyticsApi = {
    getRevenue: (period: string = '30d') => 
        apiClient.get<RevenueAnalytics>('/api/v1/analytics/revenue', { params: { period } }),
    
    getBookings: (period: string = '30d') => 
        apiClient.get<BookingAnalytics>('/api/v1/analytics/bookings', { params: { period } }),
        
    getClients: (period: string = '30d') => 
        apiClient.get<ClientAnalytics>('/api/v1/analytics/clients', { params: { period } }),
        
    getStaff: (period: string = '30d') => 
        apiClient.get<StaffAnalytics>('/api/v1/analytics/staff', { params: { period } }),
        
    getServices: (period: string = '30d') => 
        apiClient.get<ServiceAnalytics>('/api/v1/analytics/services', { params: { period } }),
        
    getMarketing: (period: string = '30d') => 
        apiClient.get<MarketingAnalytics>('/api/v1/analytics/marketing', { params: { period } }),

    getActivity: (limit: number = 20) =>
        apiClient.get('/api/v1/analytics/activity', { params: { limit } })
};
