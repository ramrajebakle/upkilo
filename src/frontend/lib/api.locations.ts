import { apiClient } from './api';

export interface Location {
    id: string;
    name: string;
    description?: string;
    addressLine1?: string;
    addressLine2?: string;
    city?: string;
    state?: string;
    postalCode?: string;
    country?: string;
    phone?: string;
    email?: string;
    timezone: string;
    isPrimary: boolean;
    isActive: boolean;
    businessHours?: string; // JSON string
    holidays?: string; // JSON string
    createdAt?: string;
    updatedAt?: string;
}

export interface LocationRequest {
    name: string;
    description?: string;
    addressLine1?: string;
    addressLine2?: string;
    city?: string;
    state?: string;
    country?: string;
    postalCode?: string;
    phone?: string;
    email?: string;
    timezone?: string;
    businessHours?: string;
    holidays?: string;
    isActive?: boolean;
}

export const locationsApi = {
    getAll: () => apiClient.get<Location[]>('/api/v1/locations'),
    get: (id: string) => apiClient.get<Location>(`/api/v1/locations/${id}`),
    create: (data: LocationRequest) => apiClient.post<Location>('/api/v1/locations', data),
    update: (id: string, data: LocationRequest) => apiClient.put<Location>(`/api/v1/locations/${id}`, data),
    delete: (id: string) => apiClient.delete(`/api/v1/locations/${id}`),
    setPrimary: (id: string) => apiClient.post(`/api/v1/locations/${id}/primary`),
};
