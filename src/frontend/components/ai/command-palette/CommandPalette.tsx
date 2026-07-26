"use client";

import React, { useEffect, useState } from "react";
import { useUIStore } from "@/stores/ui";
import { Search, Sparkles, X } from "lucide-react";

export const CommandPalette = () => {
  const { commandOpen, setCommandOpen } = useUIStore();
  const [query, setQuery] = useState("");

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setCommandOpen(!commandOpen);
      }
      if (e.key === "Escape" && commandOpen) {
        setCommandOpen(false);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [commandOpen, setCommandOpen]);

  if (!commandOpen) return null;

  return (
    <div className="fixed inset-0 z-[var(--z-command)] bg-neutral-900/40 backdrop-blur-sm flex items-start justify-center pt-[15vh]">
      <div 
        className="w-full max-w-2xl bg-surface-base rounded-xl shadow-xl border border-neutral-200 overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center px-4 py-3 border-b border-neutral-100">
          <Search size={20} className="text-neutral-400 mr-3" />
          <input
            autoFocus
            type="text"
            placeholder="What do you want to do?"
            className="flex-1 bg-transparent outline-none text-text-primary text-lg"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <button 
            onClick={() => setCommandOpen(false)}
            className="p-1 rounded hover:bg-neutral-100 text-neutral-400"
          >
            <X size={18} />
          </button>
        </div>
        
        <div className="p-2">
          {query.length === 0 && (
            <>
              <div className="px-3 py-2 text-xs font-semibold text-text-tertiary uppercase tracking-wider">
                Recent
              </div>
              <div className="px-2 py-2 rounded-lg hover:bg-neutral-50 cursor-pointer text-text-secondary flex items-center gap-3">
                <Search size={16} className="text-neutral-400" />
                Review tenant health
              </div>
              <div className="px-2 py-2 rounded-lg hover:bg-neutral-50 cursor-pointer text-text-secondary flex items-center gap-3">
                <Search size={16} className="text-neutral-400" />
                Check AI credit balance
              </div>
              
              <div className="px-3 py-2 mt-2 text-xs font-semibold text-text-tertiary uppercase tracking-wider">
                Suggested Actions
              </div>
              <div className="px-2 py-2 rounded-lg hover:bg-neutral-50 cursor-pointer text-text-secondary flex items-center gap-3">
                <Sparkles size={16} className="text-ai-500" />
                <span className="text-primary-600">3 tenants need attention</span>
              </div>
            </>
          )}
          {query.length > 0 && (
            <div className="px-3 py-4 text-center text-text-tertiary text-sm">
              Press Enter to search for &quot;{query}&quot;
            </div>
          )}
        </div>
      </div>
      {/* Click outside to close */}
      <div className="absolute inset-0 z-[-1]" onClick={() => setCommandOpen(false)} />
    </div>
  );
};
