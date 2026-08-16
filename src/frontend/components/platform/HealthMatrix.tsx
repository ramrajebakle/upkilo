"use client";

import React, { useState } from "react";
import { Card } from "@/components/ui/Card";
import { Info, TrendingUp, TrendingDown, Minus, Sparkles } from "lucide-react";
import { motion } from "framer-motion";

export interface TenantData {
  id: string;
  name: string;
  revenueScore: number; // 0-100 (X axis)
  healthScore: number; // 0-100 (Y axis)
  seatCount: number; // For dot size
  trend: "up" | "flat" | "down";
  mrr: string;
}

interface HealthMatrixProps {
  tenants: TenantData[];
}

export const HealthMatrix: React.FC<HealthMatrixProps> = ({ tenants }) => {
  const [selectedTenant, setSelectedTenant] = useState<TenantData | null>(null);

  const getTrendIcon = (trend: string) => {
    switch (trend) {
      case "up":
        return <TrendingUp size={14} className="text-success-500" />;
      case "down":
        return <TrendingDown size={14} className="text-danger-500" />;
      default:
        return <Minus size={14} className="text-neutral-400" />;
    }
  };

  const getDotColor = (trend: string, isSelected: boolean) => {
    if (isSelected) return "bg-platform-500 ring-2 ring-platform-500 ring-offset-2 ring-offset-surface-base";
    switch (trend) {
      case "up":
        return "bg-success-500/80 hover:bg-success-500";
      case "down":
        return "bg-danger-500/80 hover:bg-danger-500";
      default:
        return "bg-neutral-400/80 hover:bg-neutral-400";
    }
  };

  const getDotSize = (seatCount: number) => {
    // Min 8px, Max 28px based on typical seat counts (e.g. 1 to 1000)
    const minSize = 8;
    const maxSize = 28;
    const size = Math.max(minSize, Math.min(maxSize, 8 + Math.sqrt(seatCount) * 1.5));
    return `${size}px`;
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-text-primary">Tenant Health Matrix</h3>
        <div className="flex gap-4 text-xs text-text-secondary">
          <div className="flex items-center gap-1.5">
            <div className="w-2.5 h-2.5 rounded-full bg-success-500/80" /> Growing
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-2.5 h-2.5 rounded-full bg-neutral-400/80" /> Flat
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-2.5 h-2.5 rounded-full bg-danger-500/80" /> Declining
          </div>
          <div className="flex items-center gap-1.5 ml-2">
            <Info size={14} /> Size = Seats
          </div>
        </div>
      </div>

      <Card className="p-6 h-[400px] flex flex-col relative bg-gradient-to-tr from-surface-base to-surface-50 border border-neutral-200">
        
        {/* Y-Axis Label */}
        <div className="absolute left-2 top-1/2 -translate-y-1/2 -rotate-90 text-xs font-semibold text-text-tertiary tracking-widest uppercase">
          Health Score
        </div>
        
        {/* X-Axis Label */}
        <div className="absolute bottom-2 left-1/2 -translate-x-1/2 text-xs font-semibold text-text-tertiary tracking-widest uppercase">
          Revenue Tier
        </div>

        {/* Matrix Grid Lines */}
        <div className="absolute inset-8 border-l border-b border-neutral-200">
          <div className="absolute inset-0 bg-[linear-gradient(to_right,var(--color-neutral-100)_1px,transparent_1px),linear-gradient(to_bottom,var(--color-neutral-100)_1px,transparent_1px)] bg-[size:4rem_4rem] [mask-image:linear-gradient(to_bottom_right,white,transparent,transparent)]" />
          
          <div className="absolute left-0 bottom-0 -ml-6 -mb-6 text-[10px] text-text-tertiary">LOW</div>
          <div className="absolute left-0 top-0 -ml-6 text-[10px] text-text-tertiary">HIGH</div>
          <div className="absolute right-0 bottom-0 -mb-6 text-[10px] text-text-tertiary">HIGH</div>

          {/* Quadrant overlays (optional subtle colors) */}
          <div className="absolute top-0 right-0 w-1/2 h-1/2 bg-success-500/5 rounded-tr-lg" />
          <div className="absolute bottom-0 left-0 w-1/2 h-1/2 bg-danger-500/5 rounded-bl-lg" />

          {/* Tenant Dots */}
          {tenants.map((tenant, index) => {
            const isSelected = selectedTenant?.id === tenant.id;
            // Invert Y axis because CSS top is from top, but high health should be visually higher
            return (
              <motion.div
                key={tenant.id}
                // scale starts at 0.9, not 0. Scaling up from nothing has no physical
                // analogue — real objects do not materialise from a point — and it reads as
                // a pop rather than an arrival. 0.9 plus the opacity fade gives the same
                // sense of appearing without the rubber-band feel.
                initial={{ opacity: 0, scale: 0.9, x: "-50%", y: "-50%" }}
                animate={{ opacity: 1, scale: 1, x: "-50%", y: "-50%" }}
                transition={{
                  // Apple-style spring params: easier to reason about than
                  // stiffness/damping, and bounce 0.2 keeps it subtle. The previous
                  // stiffness 260 / damping 20 was under-damped enough to visibly overshoot
                  // on every dot, which reads as playful in a health-monitoring view that
                  // should read as precise.
                  type: "spring",
                  duration: 0.5,
                  bounce: 0.2,
                  // 50ms between items, within the 30-80ms stagger window. With enough
                  // tenants this tail gets long, so it is capped rather than unbounded.
                  delay: Math.min(index * 0.05, 0.4),
                }}
                className="absolute cursor-pointer"
                style={{
                  left: `${tenant.revenueScore}%`,
                  top: `${100 - tenant.healthScore}%`,
                  zIndex: isSelected ? 20 : 10,
                }}
                onClick={() => setSelectedTenant(isSelected ? null : tenant)}
              >
                <div
                  className={`rounded-full shadow-sm transition-transform ${getDotColor(
                    tenant.trend,
                    isSelected
                  )} ${isSelected ? "scale-110" : "hover:scale-125"}`}
                  style={{
                    width: getDotSize(tenant.seatCount),
                    height: getDotSize(tenant.seatCount),
                  }}
                />

                {/* Selected Tooltip */}
                {isSelected && (
                  <div className="absolute bottom-full mb-2 left-1/2 -translate-x-1/2 bg-surface-base border border-neutral-200 shadow-xl rounded-lg p-3 w-48 z-30 animate-fade-in-up">
                    <div className="flex items-start justify-between mb-2">
                      <span className="font-semibold text-text-primary text-sm truncate pr-2">
                        {tenant.name}
                      </span>
                      {getTrendIcon(tenant.trend)}
                    </div>
                    <div className="space-y-1 text-xs text-text-secondary">
                      <div className="flex justify-between">
                        <span>MRR:</span>
                        <span className="font-medium text-text-primary">{tenant.mrr}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Health:</span>
                        <span
                          className={`font-medium ${
                            tenant.healthScore > 70
                              ? "text-success-500"
                              : tenant.healthScore < 40
                              ? "text-danger-500"
                              : "text-warning-500"
                          }`}
                        >
                          {tenant.healthScore}/100
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span>Seats:</span>
                        <span className="font-medium text-text-primary">{tenant.seatCount}</span>
                      </div>
                    </div>
                  </div>
                )}
              </motion.div>
            );
          })}
        </div>
      </Card>
      
      {/* AI Contextual Annotation */}
      <div className="bg-ai-50 border border-ai-100 rounded-lg p-3 flex items-start gap-3">
        <Sparkles className="text-ai-500 shrink-0 mt-0.5" size={16} />
        <p className="text-sm text-text-secondary">
          <strong className="text-text-primary">Insight:</strong> 3 tenants in the bottom-left danger zone this week. 
          The top-right cluster grew 18% MoM. 
          <button className="text-platform-600 font-medium ml-1 hover:underline">Identify pattern?</button>
        </p>
      </div>
    </div>
  );
};

// Extracted Sparkles since we imported it from lucide
// wait, already imported.
