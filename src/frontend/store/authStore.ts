import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { apiClient } from '@/lib/api';
import Cookies from 'js-cookie';

// Backend may serialize role as enum int (0..n) or string. Normalize to lowercase string.
const ROLE_MAP: Record<number, string> = {
  0: 'owner',
  1: 'admin',
  2: 'manager',
  3: 'staff',
  4: 'superadmin',
};
function normalizeRole(role: unknown): string {
  if (typeof role === 'string') return role.toLowerCase();
  if (typeof role === 'number') return ROLE_MAP[role] ?? 'user';
  return 'user';
}

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: string;
}

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isInitialized: boolean;
  login: (user: User, token: string) => void;
  logout: () => void;
  setLoading: (loading: boolean) => void;
  setInitialized: (initialized: boolean) => void;
  checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      isInitialized: false,
      
      login: (user, token) => {
        if (typeof window !== 'undefined') {
          localStorage.setItem('token', token);
          localStorage.setItem('tenantId', user.tenantId);
          
          const isLocal = window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1";
          Cookies.set("token", token, { 
            expires: 30, // 30 days
            path: "/",
            sameSite: "Lax",
            secure: !isLocal
          });
        }
        set({ user, token, isAuthenticated: true, isLoading: false, isInitialized: true });
      },
      
      logout: () => {
        if (typeof window !== 'undefined') {
          localStorage.removeItem('token');
          localStorage.removeItem('tenantId');
          Cookies.remove('token', { path: '/' });
        }
        set({ user: null, token: null, isAuthenticated: false, isLoading: false });
      },
      
      setLoading: (loading) => set({ isLoading: loading }),
      
      setInitialized: (initialized) => set({ isInitialized: initialized }),

      checkAuth: async () => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
        if (!token) {
          set({ isInitialized: true, isLoading: false });
          return;
        }

        try {
          const { data } = await apiClient.get('/api/v1/auth/me');
          set({ 
            user: {
              id: data.id,
              email: data.email,
              firstName: data.firstName,
              lastName: data.lastName,
              role: normalizeRole(data.role),
              tenantId: data.tenantId
            }, 
            token,
            isAuthenticated: true, 
            isInitialized: true,
            isLoading: false 
          });
        } catch (error) {
          console.error('Auth verification failed:', error);
          if (typeof window !== 'undefined') {
            localStorage.removeItem('token');
            localStorage.removeItem('tenantId');
          }
          set({ user: null, token: null, isAuthenticated: false, isInitialized: true, isLoading: false });
        }
      }
    }),
    {
      name: 'upkilo-auth',
      partialize: (state) => ({ user: state.user, token: state.token, isAuthenticated: state.isAuthenticated }),
    }
  )
);
