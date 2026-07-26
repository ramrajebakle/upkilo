import { create } from 'zustand';

export type UserRole = 'platform_owner' | 'platform_admin' | 'tenant_owner' | 'team_member' | 'customer';

interface SessionState {
  role: UserRole;
  tenantId: string | null;
  permissions: string[];
  billingContext: 'platform' | 'tenant' | null;
  setRole: (role: UserRole) => void;
  setTenantId: (tenantId: string | null) => void;
  setBillingContext: (context: 'platform' | 'tenant' | null) => void;
}

// Defaulting to platform_owner for development/demonstration purposes
export const useSessionStore = create<SessionState>((set) => ({
  role: 'platform_owner',
  tenantId: null,
  permissions: [],
  billingContext: 'platform',
  setRole: (role) => set({ role }),
  setTenantId: (tenantId) => set({ tenantId }),
  setBillingContext: (billingContext) => set({ billingContext }),
}));
