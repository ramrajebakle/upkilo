"use client";

import React, { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  Bot, Sparkles, ShieldAlert, MessageSquare, PenTool,
  TrendingUp, Users, Calendar, DollarSign, BarChart2,
  ChevronRight, Zap, BrainCircuit, RefreshCw, Loader2,
  AlertTriangle, CheckCircle2, Activity,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface AIMetrics {
  decisionsToday: number;
  autoApproved: number;
  pendingReview: number;
  accuracy: number;
}

const AI_TOOLS = [
  {
    name: "AI Approvals",
    description: "Review and approve AI-generated actions before they execute.",
    href: "/ai/approvals",
    icon: ShieldAlert,
    color: "text-warning-fg",
    bg: "bg-amber-50 dark:bg-amber-950/30",
  },
  {
    name: "Booking Chatbot",
    description: "Configure the AI assistant that handles client booking conversations.",
    href: "/ai/chatbot",
    icon: MessageSquare,
    color: "text-blue-500",
    bg: "bg-blue-50 dark:bg-blue-950/30",
  },
  {
    name: "Copy Generator",
    description: "Generate marketing copy, email content, and social posts with AI.",
    href: "/ai/copy-gen",
    icon: PenTool,
    color: "text-purple-500",
    bg: "bg-purple-50 dark:bg-purple-950/30",
  },
];

const INTELLIGENCE_FEATURES = [
  {
    name: "Fill My Calendar",
    description: "AI identifies open slots and suggests clients most likely to book.",
    endpoint: "/api/v1/ai/fill-my-calendar",
    icon: Calendar,
    color: "text-success-fg",
    actionLabel: "Run now",
  },
  {
    name: "At-Risk Clients",
    description: "Clients showing churn signals — last visit >30 days, no upcoming booking.",
    endpoint: "/api/v1/ai/client-insights/at-risk",
    icon: Users,
    color: "text-danger-fg",
    actionLabel: "View clients",
  },
  {
    name: "Demand Forecast",
    description: "Predicted booking volume for the next 30 days by service and time slot.",
    endpoint: "/api/v1/intelligence/demand-forecast",
    icon: BarChart2,
    color: "text-indigo-500",
    actionLabel: "View forecast",
  },
  {
    name: "Price Optimization",
    description: "AI-recommended price adjustments to maximize revenue per slot.",
    endpoint: "/api/v1/intelligence/price-optimization",
    icon: DollarSign,
    color: "text-warning-fg",
    actionLabel: "View recommendations",
  },
  {
    name: "No-Show Risk",
    description: "Bookings flagged as high no-show risk — trigger early reminders.",
    endpoint: "/api/v1/intelligence/no-show-risk",
    icon: AlertTriangle,
    color: "text-orange-500",
    actionLabel: "View bookings",
  },
  {
    name: "Revenue Projections",
    description: "30/60/90-day revenue projections based on current pipeline.",
    endpoint: "/api/v1/projections",
    icon: TrendingUp,
    color: "text-success-fg",
    actionLabel: "View projections",
  },
];

function IntelligenceCard({
  feature,
}: {
  feature: (typeof INTELLIGENCE_FEATURES)[0];
}) {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<{ count?: number; summary?: string } | null>(null);
  const [error, setError] = useState(false);

  const run = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await apiClient.get(feature.endpoint);
      const data = res.data?.data ?? res.data ?? {};
      const count =
        Array.isArray(data) ? data.length :
        data.count ?? data.total ?? data.items?.length ?? null;
      const summary = data.summary ?? data.message ?? null;
      setResult({ count, summary });
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [feature.endpoint]);

  useEffect(() => { run(); }, [run]);

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-sm font-semibold text-text-primary">
          <feature.icon className={`h-4 w-4 ${feature.color}`} />
          {feature.name}
        </CardTitle>
        <CardDescription className="text-xs">{feature.description}</CardDescription>
      </CardHeader>
      <CardContent className="flex-1 flex flex-col justify-between gap-3">
        <div className="text-center py-2">
          {loading ? (
            <Loader2 className="h-5 w-5 animate-spin text-text-tertiary mx-auto" />
          ) : error ? (
            <p className="text-xs text-text-tertiary">No data available</p>
          ) : result ? (
            <div>
              {result.count !== null && result.count !== undefined && (
                <p className={`text-3xl font-bold ${feature.color}`}>{result.count}</p>
              )}
              {result.summary && (
                <p className="text-xs text-text-secondary mt-1 line-clamp-2">{result.summary}</p>
              )}
            </div>
          ) : null}
        </div>
        <Button
          variant="outline"
          size="sm"
          className="w-full text-xs"
          onClick={run}
          disabled={loading}
        >
          <RefreshCw className={cn("h-3 w-3 me-1.5", loading && "animate-spin")} />
          {feature.actionLabel}
        </Button>
      </CardContent>
    </Card>
  );
}

export default function AIToolsPage() {
  const router = useRouter();
  const [metrics, setMetrics] = useState<AIMetrics | null>(null);

  useEffect(() => {
    apiClient
      .get("/api/v1/aidashboard/metrics")
      .then((r) => setMetrics(r.data?.data ?? r.data))
      .catch(() => {});
  }, []);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
          AI Tools
          <Sparkles className="text-ai-500" size={24} />
        </h1>
        <p className="text-text-secondary mt-1">
          Automation, intelligence, and AI-powered workflows for your business.
        </p>
      </header>

      {/* Metrics bar */}
      {metrics && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { label: "Decisions today", value: metrics.decisionsToday, icon: BrainCircuit, color: "text-ai-500" },
            { label: "Auto-approved", value: metrics.autoApproved, icon: CheckCircle2, color: "text-success-fg" },
            { label: "Pending review", value: metrics.pendingReview, icon: AlertTriangle, color: "text-warning-fg" },
            { label: "Accuracy", value: `${metrics.accuracy}%`, icon: Activity, color: "text-blue-500" },
          ].map((m) => (
            <Card key={m.label}>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-xs font-medium text-text-secondary">{m.label}</CardTitle>
                <m.icon className={`h-4 w-4 ${m.color}`} />
              </CardHeader>
              <CardContent>
                <p className={`text-2xl font-bold ${m.color}`}>{m.value}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* AI Tools */}
      <section>
        <h2 className="text-sm font-semibold uppercase tracking-wider text-text-tertiary mb-3">
          Tools
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {AI_TOOLS.map((tool) => (
            <button
              key={tool.name}
              onClick={() => router.push(tool.href)}
              className={cn(
                "text-left p-5 rounded-xl border border-surface-200 hover:shadow-md transition-all group",
                tool.bg
              )}
            >
              <div className="flex items-start justify-between mb-3">
                <tool.icon className={`h-6 w-6 ${tool.color}`} />
                <ChevronRight className="h-4 w-4 text-text-tertiary group-hover:translate-x-1 transition-transform" />
              </div>
              <p className="font-semibold text-text-primary text-sm">{tool.name}</p>
              <p className="text-xs text-text-secondary mt-1">{tool.description}</p>
            </button>
          ))}
        </div>
      </section>

      {/* Intelligence */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-text-tertiary">
            Business Intelligence
          </h2>
          <a href="/ai-dashboard" className="text-xs text-ai-500 hover:underline flex items-center gap-1">
            Full AI Dashboard <ChevronRight className="h-3 w-3" />
          </a>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {INTELLIGENCE_FEATURES.map((f) => (
            <IntelligenceCard key={f.name} feature={f} />
          ))}
        </div>
      </section>
    </div>
  );
}
