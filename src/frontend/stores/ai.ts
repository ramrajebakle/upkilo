import { create } from 'zustand';

interface AIState {
  copilotOpen: boolean;
  setCopilotOpen: (open: boolean) => void;
  toggleCopilot: () => void;
  
  // Later we can add active context, conversation history, etc.
  activeContext: string | null;
  setActiveContext: (context: string | null) => void;
}

export const useAIStore = create<AIState>((set) => ({
  copilotOpen: false,
  setCopilotOpen: (open) => set({ copilotOpen: open }),
  toggleCopilot: () => set((state) => ({ copilotOpen: !state.copilotOpen })),
  
  activeContext: null,
  setActiveContext: (context) => set({ activeContext: context }),
}));
