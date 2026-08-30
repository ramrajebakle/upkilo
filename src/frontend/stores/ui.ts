// NOTE: theme does NOT live here. This store used to carry `theme` and `setTheme` beside
// ThemeProvider's own state — two sources of truth for one setting, with nothing keeping them
// in step. Nothing read the store's copy, so it was silently stale from the moment the app
// loaded; the danger was the next component to reach for it and get an answer that had never
// been true. Theme comes from useTheme() only.
import { create } from 'zustand';

interface UIState {
  orbitCollapsed: boolean;
  activeOrbitItem: string | null;
  density: 'comfortable' | 'compact';
  commandOpen: boolean;
  toggleOrbit: () => void;
  setOrbitCollapsed: (collapsed: boolean) => void;
  setActiveOrbitItem: (item: string | null) => void;
  setDensity: (density: 'comfortable' | 'compact') => void;
  setCommandOpen: (open: boolean) => void;
}

export const useUIStore = create<UIState>((set) => ({
  orbitCollapsed: false,
  activeOrbitItem: null,
  density: 'comfortable',
  commandOpen: false,
  toggleOrbit: () => set((state) => ({ orbitCollapsed: !state.orbitCollapsed })),
  setOrbitCollapsed: (collapsed) => set({ orbitCollapsed: collapsed }),
  setActiveOrbitItem: (item) => set({ activeOrbitItem: item }),
  setDensity: (density) => set({ density }),
  setCommandOpen: (open) => set({ commandOpen: open }),
}));
