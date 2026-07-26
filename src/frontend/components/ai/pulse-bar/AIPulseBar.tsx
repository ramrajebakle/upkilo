"use client";

import React, { useEffect, useState } from "react";
import { useAIStore } from "@/stores/ai";
import { useSession } from "next-auth/react";
import { AlertCircle, TrendingUp, Zap, Sparkles } from "lucide-react";
import api from "@/lib/api";

interface Signal {
  id: number;
  type: string;
  text: string;
  icon: typeof Zap;
}

export const AIPulseBar = () => {
  const { data: session } = useSession();
  const { toggleCopilot } = useAIStore();
  const role = session?.user?.role;
  const isPlatform = role === "platform_owner" || role === "platform_admin";

  const [signals, setSignals] = useState<Signal[]>([]);

  // Real signals only. Platform-wide metrics have no tenant-safe source here, so the bar shows
  // the tenant's own figures; anything that fails to load is simply omitted rather than faked.
  useEffect(() => {
    if (isPlatform) {
      setSignals([]);
      return;
    }
    let active = true;

    Promise.allSettled([
      api.analytics.dashboard(),
      api.health.check(),
    ]).then(([dashRes, healthRes]) => {
      if (!active) return;
      const next: Signal[] = [];

      if (healthRes.status === "fulfilled") {
        next.push({ id: 1, type: "success", text: "All systems operational", icon: Zap });
      }

      if (dashRes.status === "fulfilled") {
        const d = dashRes.value.data ?? {};
        const today = Number(d.todayBookings) || 0;
        const pending = Number(d.pendingBookings) || 0;
        next.push({
          id: 2,
          type: "info",
          text: `${today} booking${today === 1 ? "" : "s"} today`,
          icon: TrendingUp,
        });
        if (pending > 0) {
          next.push({
            id: 3,
            type: "warning",
            text: `${pending} pending confirmation${pending === 1 ? "" : "s"}`,
            icon: AlertCircle,
          });
        }
      }

      setSignals(next);
    });

    return () => { active = false; };
  }, [isPlatform]);

  const getColorClass = (type: string) => {
    switch (type) {
      case "success": return "text-success-500";
      case "warning": return "text-warning-500";
      case "danger": return "text-danger-500";
      default: return "text-info-500";
    }
  };

  return (
    <div className="h-[var(--pulse-bar-height,40px)] w-full bg-surface-base border-b border-neutral-200 flex items-center px-6 z-[var(--z-pulse-bar)] fixed top-0 right-0 left-0">
      <div className="flex items-center gap-6 ml-auto text-sm">
        {signals.map((signal) => {
          const Icon = signal.icon;
          return (
            <div
              key={signal.id}
              className="flex items-center gap-2 cursor-pointer hover:bg-neutral-50 px-2 py-1 rounded transition-colors"
            >
              <Icon size={14} className={getColorClass(signal.type)} />
              <span className="text-text-secondary">{signal.text}</span>
            </div>
          );
        })}
        
        <div className="w-px h-4 bg-neutral-200 mx-1" />

        {/* User Profile Mock */}
        <div className="h-7 w-7 rounded-full bg-primary-100 flex items-center justify-center text-primary-700 text-xs font-bold border border-primary-200">
          {isPlatform ? "PO" : "TO"}
        </div>
        
        <button 
          onClick={toggleCopilot}
          className="flex items-center gap-1.5 px-2 py-1 rounded bg-ai-50 text-ai-600 hover:bg-ai-100 transition-colors font-medium cursor-pointer"
        >
          <Sparkles size={14} />
          Copilot
        </button>
      </div>
    </div>
  );
};
