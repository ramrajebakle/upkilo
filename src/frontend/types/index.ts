/** 
 * Upkilo Core Domain Interfaces
 * Authoritative types for frontend-backend communication.
 */

export type DiscountType = 'Percentage' | 'Fixed';
export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';
export type WaitlistStatus = 'Pending' | 'Waiting' | 'Notified' | 'Booked' | 'Converted' | 'Expired' | 'Cancelled';

export interface IBaseEntity {
    id: string;
    createdAt: string;
    updatedAt: string;
    isDeleted: boolean;
    tenantId: string;
    rowVersion?: string; // Base64 encoded rowversion from backend
    version?: number;    // Sequential version number
}

export interface IService extends IBaseEntity {
    name: string;
    description: string | null;
    durationMinutes: number;
    price: number;
    category: string | null;
    isActive: boolean;
}


export interface ICoupon extends IBaseEntity {
    code: string;
    discountType: DiscountType;
    discountValue: number;
    usageLimit: number | null;
    timesUsed: number;
    validFrom: string | null;
    expiresAt: string | null;
    minimumOrderAmount: number;
    isActive: boolean;
    isExpired: boolean;
    clientSpecificId?: string | null;
    firstTimeOnly: boolean;
}

export interface IServicePackage extends IBaseEntity {
    name: string;
    description: string | null;
    price: number;
    originalPrice: number | null;
    savings: number;
    serviceIds: string; // JSON array of { serviceId, quantity }
    sessionCount: number;
    sessionsUsed: number;
    sessionsRemaining: number;
    validityDays: number;
    isActive: boolean;
}

export interface IWaitlistEntry extends IBaseEntity {
    serviceId: string;
    clientId?: string;
    email: string;
    firstName: string;
    lastName: string;
    phone?: string;
    status: WaitlistStatus;
    preferredDate: string;
    preferredTimeRange?: string;
    notes?: string;
    isConverted: boolean;
    staffId?: string;
    priority: number;
    requestedDate: string;
}

export interface IClient extends IBaseEntity {
    firstName: string;
    lastName: string;
    email: string;
    phone: string | null;
    totalSpend: number;
    visitCount: number;
    lastVisitAt: string | null;
    isActive: boolean;
}

export interface IBooking extends IBaseEntity {
    clientId: string;
    serviceId: string;
    staffId: string;
    startTime: string;
    endTime: string;
    status: BookingStatus;
    totalAmount: number;
    currency: string;
    notes: string | null;
}

export interface IApiResponse<T> {
    data: T;
    success: boolean;
    message?: string;
}

export interface IPaginatedResponse<T> {
    data: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}
