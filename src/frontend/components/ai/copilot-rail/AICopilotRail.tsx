"use client";

import React, { useEffect, useState } from "react";
import { useAIStore } from "@/stores/ai";
import { X, Sparkles, ArrowRight, Zap, Loader2, Lock } from "lucide-react";
import api from "@/lib/api";

export const AICopilotRail = () => {
  const { copilotOpen, toggleCopilot } = useAIStore();
  const [isThinking, setIsThinking] = useState(false);
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [locked, setLocked] = useState(false);

  // Load real recommendations when the rail opens. A 403 is the plan gate for `ai_insights`.
  useEffect(() => {
    if (!copilotOpen) return;
    let active = true;
    setIsThinking(true);

    api.aiDashboard
      .recommendations()
      .then((res) => {
        if (!active) return;
        const list = res.data?.recommendations ?? [];
        setRecommendations(
          (Array.isArray(list) ? list : [])
            .map((r: unknown) => (typeof r === 'string' ? r : String((r as { message?: string })?.message ?? '')))
            .filter(Boolean)
        );
        setLocked(false);
      })
      .catch((err) => {
        if (!active) return;
        setRecommendations([]);
        setLocked(err?.response?.status === 403);
      })
      .finally(() => {
        if (active) setIsThinking(false);
      });

    return () => { active = false; };
  }, [copilotOpen]);

  return (
    <>
      {/* Backdrop for mobile (hidden on lg screens) */}
      {copilotOpen && (
        <div 
          className="fixed inset-0 z-[var(--z-copilot)] bg-neutral-900/20 backdrop-blur-sm lg:hidden"
          onClick={toggleCopilot}
        />
      )}

      {/* Rail Container */}
      <aside
        className={`fixed top-[var(--pulse-bar-height,40px)] right-0 bottom-0 z-[var(--z-copilot)] w-[320px] bg-surface-base border-l border-neutral-200 shadow-2xl transition-transform duration-300 ease-spring flex flex-col ${
          copilotOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {/* Header */}
        <div className="px-4 py-3 border-b border-neutral-100 flex items-center justify-between">
          <div className="flex items-center gap-2 text-ai-600">
            <Sparkles size={18} />
            <span className="font-bold tracking-tight">Copilot</span>
          </div>
          <button 
            onClick={toggleCopilot}
            className="p-1.5 text-text-tertiary hover:text-text-primary rounded-md hover:bg-neutral-100 transition-colors"
          >
            <X size={18} />
          </button>
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-6">
          
          {isThinking ? (
            <div className="flex flex-col items-center justify-center py-12 text-ai-500 animate-pulse">
              <Sparkles size={32} className="mb-4 opacity-50" />
              <div className="flex items-center gap-2">
                <Loader2 size={16} className="animate-spin" />
                <span className="text-sm font-medium">Analyzing page context...</span>
              </div>
            </div>
          ) : (
            <div className="space-y-6 animate-fade-in-up">

              <section>
                <h3 className="text-xs font-bold text-text-tertiary uppercase tracking-wider mb-2">
                  Recommended Actions
                </h3>

                {locked && (
                  <div className="bg-ai-50 border border-ai-100 rounded-lg p-3 text-sm text-text-secondary leading-relaxed flex gap-2">
                    <Lock size={16} className="text-ai-500 shrink-0 mt-0.5" />
                    <span>AI recommendations are not included in your current plan.</span>
                  </div>
                )}

                {!locked && recommendations.length === 0 && (
                  <div className="rounded-lg border border-surface-200 p-3 text-sm text-text-secondary">
                    No recommendations right now. Check back once you have more activity.
                  </div>
                )}

                {!locked && recommendations.length > 0 && (
                  <div className="space-y-2">
                    {recommendations.map((rec, i) => (
                      <div
                        key={i}
                        className="w-full text-left p-3 rounded-lg border border-surface-200 flex gap-3"
                      >
                        <Zap size={16} className="text-ai-500 shrink-0 mt-0.5" />
                        <div className="text-sm text-text-primary leading-relaxed">{rec}</div>
                      </div>
                    ))}
                  </div>
                )}
              </section>

            </div>
          )}

        </div>

        {/* Floating Input area */}
        <div className="p-4 border-t border-neutral-100 bg-surface-base">
          <div className="relative">
            <input 
              type="text" 
              placeholder="Ask Copilot..." 
              className="w-full bg-surface-50 border border-surface-200 rounded-lg py-2.5 pl-3 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-ai-500/50 focus:border-ai-500 transition-all text-text-primary placeholder:text-text-tertiary"
            />
            <button className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-ai-500 hover:bg-ai-50 rounded-md transition-colors">
              <ArrowRight size={16} />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
};
