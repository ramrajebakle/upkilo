import { create } from 'zustand';

interface AppState {
  isOffline: boolean;
  theme: 'light' | 'dark';
  setOffline: (offline: boolean) => void;
  setTheme: (theme: 'light' | 'dark') => void;
}

export const useAppStore = create<AppState>((set) => ({
  isOffline: false,
  theme: 'light',
  setOffline: (isOffline) => set({ isOffline }),
  setTheme: (theme) => set({ theme }),
}));
