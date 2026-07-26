import { create } from 'zustand';

interface UIState {
  orbitCollapsed: boolean;
  activeOrbitItem: string | null;
  theme: 'light' | 'dark' | 'system';
  density: 'comfortable' | 'compact';
  commandOpen: boolean;
  toggleOrbit: () => void;
  setOrbitCollapsed: (collapsed: boolean) => void;
  setActiveOrbitItem: (item: string | null) => void;
  setTheme: (theme: 'light' | 'dark' | 'system') => void;
  setDensity: (density: 'comfortable' | 'compact') => void;
  setCommandOpen: (open: boolean) => void;
}

export const useUIStore = create<UIState>((set) => ({
  orbitCollapsed: false,
  activeOrbitItem: null,
  theme: 'system',
  density: 'comfortable',
  commandOpen: false,
  toggleOrbit: () => set((state) => ({ orbitCollapsed: !state.orbitCollapsed })),
  setOrbitCollapsed: (collapsed) => set({ orbitCollapsed: collapsed }),
  setActiveOrbitItem: (item) => set({ activeOrbitItem: item }),
  setTheme: (theme) => set({ theme }),
  setDensity: (density) => set({ density }),
  setCommandOpen: (open) => set({ commandOpen: open }),
}));
