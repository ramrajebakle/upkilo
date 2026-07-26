"use client";

import React, { useState } from "react";
import { Sparkles, Activity, Network, Database } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { AIToolsOverview } from "@/components/tenant/ai-tools/AIToolsOverview";
import { KnowledgeEngine } from "@/components/tenant/ai-tools/KnowledgeEngine";
import { WorkflowBuilder } from "@/components/tenant/ai-tools/WorkflowBuilder";

type Tab = "overview" | "automations" | "knowledge";

export default function TenantAIToolsPage() {
  const [activeTab, setActiveTab] = useState<Tab>("overview");

  const tabs = [
    { id: "overview", label: "Overview", icon: <Activity size={18} /> },
    { id: "automations", label: "Automations", icon: <Network size={18} /> },
    { id: "knowledge", label: "Knowledge Engine", icon: <Database size={18} /> },
  ];

  return (
    <div className="space-y-8 max-w-6xl mx-auto pb-24">
      <header className="flex flex-col gap-6 border-b border-surface-200 pb-2">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            AI Tools & Workflows
            <Sparkles className="text-ai-500" size={24} />
          </h1>
          <p className="text-text-secondary mt-1">
            Manage your AI assistant capabilities, custom automations, and knowledge sources.
          </p>
        </div>

        {/* Tab Navigation */}
        <div className="flex gap-6 relative">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as Tab)}
              className={`pb-4 flex items-center gap-2 text-sm font-medium transition-colors relative ${
                activeTab === tab.id ? "text-ai-600" : "text-text-tertiary hover:text-text-primary"
              }`}
            >
              {tab.icon}
              {tab.label}
              {activeTab === tab.id && (
                <motion.div
                  layoutId="activeTabIndicator"
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-ai-600 rounded-t-full"
                  transition={{ type: "spring", stiffness: 300, damping: 30 }}
                />
              )}
            </button>
          ))}
        </div>
      </header>

      {/* Tab Content */}
      <div className="relative">
        <AnimatePresence mode="wait">
          {activeTab === "overview" && (
            <motion.div
              key="overview"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{ duration: 0.2 }}
            >
              <AIToolsOverview />
            </motion.div>
          )}

          {activeTab === "automations" && (
            <motion.div
              key="automations"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{ duration: 0.2 }}
            >
              <WorkflowBuilder />
            </motion.div>
          )}

          {activeTab === "knowledge" && (
            <motion.div
              key="knowledge"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{ duration: 0.2 }}
            >
              <KnowledgeEngine />
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}
