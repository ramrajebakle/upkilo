"use client";

import React from "react";
import { AIInsightCard, AIInsightCardProps } from "@/components/ai/insight-card/AIInsightCard";
import { Sparkles, ArrowRight, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useInsights, RawInsight } from "@/hooks/usePlatformData";
import { motion } from "framer-motion";

export default function PlatformCommandPage() {
  const { data: insights, isLoading, isError } = useInsights();
  const currentDate = new Date().toLocaleDateString("en-US", {
    weekday: "long",
    month: "short",
    day: "numeric",
  });

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <p className="text-text-tertiary text-sm font-medium mb-1 tracking-wide uppercase">
            {currentDate}
          </p>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            Platform Command
            <Sparkles className="text-ai" size={24} />
          </h1>
        </div>
        <Button variant="outline" size="sm" rightIcon={<ArrowRight size={14} />}>
          View full analytics
        </Button>
      </header>

      <section>
        <div className="flex items-center gap-2 mb-4">
          <h2 className="text-lg font-semibold text-text-primary">AI Briefing</h2>
          <span className="bg-ai-subtle text-ai text-xs font-bold px-2 py-0.5 rounded-full">
            LIVE
          </span>
        </div>
        
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-1">
          {isLoading ? (
            Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="h-32 bg-surface-100 border border-surface-200 rounded-xl animate-pulse" />
            ))
          ) : isError ? (
            <div className="p-4 bg-danger-50 text-danger-600 rounded-lg">Failed to load AI briefing.</div>
          ) : (
            <motion.div 
              className="grid gap-4 md:grid-cols-2 lg:grid-cols-1"
              initial="hidden"
              animate="visible"
              variants={{
                hidden: { opacity: 0 },
                visible: {
                  opacity: 1,
                  transition: { staggerChildren: 0.1 }
                }
              }}
            >
              {insights?.map((insight: RawInsight) => (
                <motion.div
                  key={insight.id}
                  variants={{
                    hidden: { opacity: 0, x: 20 },
                    visible: { opacity: 1, x: 0, transition: { type: "spring", stiffness: 300, damping: 24 } }
                  }}
                >
                  <AIInsightCard
                    type={insight.type as any}
                    title={insight.title}
                    description={insight.description}
                    confidence={insight.confidence}
                    actions={insight.actions.map(a => ({
                      label: a.label,
                      primary: a.primary,
                      onClick: () => console.log(`Triggered action: ${a.id}`)
                    }))}
                  />
                </motion.div>
              ))}
            </motion.div>
          )}
        </div>
      </section>
    </div>
  );
}
