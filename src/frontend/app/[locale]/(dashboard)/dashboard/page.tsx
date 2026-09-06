"use client";

import React, { useEffect, useState } from "react";
import {
  Users,
  DollarSign,
  Calendar,
  TrendingUp,
  ArrowUpRight,
  ArrowDownRight,
  Activity,
  BarChart3,
} from "lucide-react";
import { cn } from "@/lib/utils";
import api from "@/lib/api";
import { FunnelChart } from "@/components/charts";

type DashboardData = {
  todayRevenue: number;
  todayBookings: number;
  activeClients: number;
  pendingBookings: number;
  upcomingToday: number;
  completedToday: number;
  revenueChange: number;
  bookingsChange: number;
};

type FunnelStep = { step: string; count: number; dropoff: number };
type FunnelData = { period: string; steps: FunnelStep[]; overallConversion: number };

type ActivityItem = {
  id?: string;
  type?: string;
  title?: string;
  description?: string;
  timestamp?: string;
  metadata?: Record<string, unknown>;
};

const currency = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0,
});

const compactNumber = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1,
});

type Stat = {
  label: string;
  value: string;
  delta: number;
  deltaLabel: string;
  icon: React.ComponentType<{ className?: string }>;
  gradient: string;
  shadow: string;
};

/**
 * Coerces a possibly-absent numeric field to 0.
 *
 * Every stat below indexes straight into the API payload, so a response missing any one of these
 * six fields threw inside Intl formatting and took the WHOLE dashboard to its error boundary —
 * "Something went wrong" in place of the page, over one absent number. A partial payload should
 * cost you that tile's value, not the screen.
 */
const num = (v: unknown): number => (typeof v === 'number' && Number.isFinite(v) ? v : 0);

function buildStats(d: DashboardData, conversion: number): Stat[] {
  return [
    {
      label: "Today's Revenue",
      value: currency.format(num(d.todayRevenue)),
      delta: num(d.revenueChange),
      deltaLabel: "vs yesterday",
      icon: DollarSign,
      gradient: "from-emerald-500 to-emerald-700",
      shadow: "shadow-emerald-500/25",
    },
    {
      label: "Active Clients",
      value: compactNumber.format(num(d.activeClients)),
      delta: 0,
      deltaLabel: "total",
      icon: Users,
      gradient: "from-primary-500 to-cyan-600",
      shadow: "shadow-cyan-500/25",
    },
    {
      label: "Upcoming Today",
      value: num(d.upcomingToday).toString(),
      delta: num(d.bookingsChange),
      deltaLabel: `${num(d.pendingBookings)} pending`,
      icon: Calendar,
      gradient: "from-primary-500 to-primary-700",
      shadow: "shadow-primary-500/25",
    },
    {
      label: "Conversion Rate",
      value: `${conversion.toFixed(1)}%`,
      delta: 0,
      deltaLabel: "from funnel",
      icon: TrendingUp,
      gradient: "from-amber-500 to-orange-600",
      shadow: "shadow-orange-500/25",
    },
  ];
}

function StatSkeleton() {
  return (
    <div className="card-elevated p-6 bg-white dark:bg-slate-900 border-slate-200 dark:border-white/5">
      <div className="flex items-start justify-between mb-4">
        <div className="space-y-2 flex-1">
          <div className="h-3 w-24 bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
          <div className="h-8 w-32 bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
        </div>
        <div className="h-12 w-12 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
      </div>
      <div className="h-3 w-28 bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
    </div>
  );
}

const ACTIVITY_DOT_COLORS: Record<string, string> = {
  booking: "bg-cyan-500",
  payment: "bg-emerald-500",
  campaign: "bg-orange-500",
  review: "bg-primary-500",
  client: "bg-primary-500",
  default: "bg-slate-400",
};

function dotFor(type?: string) {
  if (!type) return ACTIVITY_DOT_COLORS.default;
  const key = Object.keys(ACTIVITY_DOT_COLORS).find((k) =>
    type.toLowerCase().includes(k)
  );
  return key ? ACTIVITY_DOT_COLORS[key] : ACTIVITY_DOT_COLORS.default;
}

function relativeTime(iso?: string): string {
  if (!iso) return "";
  const diff = Date.now() - new Date(iso).getTime();
  const min = Math.floor(diff / 60000);
  if (min < 1) return "now";
  if (min < 60) return `${min}m ago`;
  const hr = Math.floor(min / 60);
  if (hr < 24) return `${hr}h ago`;
  return `${Math.floor(hr / 24)}d ago`;
}

const EMPTY_ACTIVITY_HINT = {
  title: "No recent activity yet",
  detail: "Bookings, payments, and reviews will appear here.",
};

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [funnel, setFunnel] = useState<FunnelData | null>(null);
  const [activity, setActivity] = useState<ActivityItem[]>([]);
  const [period, setPeriod] = useState<"7d" | "30d" | "90d">("30d");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [dash, fun, act] = await Promise.all([
          api.analytics.dashboard(),
          api.analytics.funnel(period),
          api.analytics.activity(8),
        ]);
        if (cancelled) return;
        setData(dash.data);
        setFunnel(fun.data);
        setActivity(Array.isArray(act.data?.data) ? act.data.data : []);
      } catch (e) {
        console.error("Failed to load dashboard analytics", e);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [period]);

  const stats = data ? buildStats(data, funnel?.overallConversion ?? 0) : null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="animate-fade-in-up flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-gradient-to-br from-primary-500 to-cyan-600 rounded-xl shadow-lg shadow-primary-500/25">
              <BarChart3 className="h-5 w-5 text-white" />
            </div>
            <h1
              className="text-2xl lg:text-3xl font-bold text-slate-900 dark:text-white"
              style={{ fontFamily: "var(--font-display)" }}
            >
              Dashboard
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">
            Welcome back — here's what's happening with your business today.
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300 bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-lg px-3 py-2 shadow-sm">
          <Activity className="h-4 w-4 text-success-fg" />
          <span className="font-medium">Live</span>
          <span className="text-foreground-muted">·</span>
          <span>Last 30 days</span>
        </div>
      </div>

      {/* KPI grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {!stats
          ? Array.from({ length: 4 }).map((_, i) => <StatSkeleton key={i} />)
          : stats.map((s, i) => {
              const Icon = s.icon;
              const positive = s.delta >= 0;
              return (
                <div
                  key={s.label}
                  className="card-elevated p-6 animate-fade-in-up hover:-translate-y-0.5 transition-transform"
                  style={{ animationDelay: `${i * 80}ms` }}
                >
                  <div className="flex items-start justify-between mb-4">
                    <div>
                      <p className="text-sm font-medium text-slate-500 dark:text-slate-400 mb-1">
                        {s.label}
                      </p>
                      <p
                        className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight"
                        style={{ fontFamily: "var(--font-display)" }}
                      >
                        {s.value}
                      </p>
                    </div>
                    <div
                      className={cn(
                        "p-3 rounded-xl bg-gradient-to-br shadow-lg",
                        s.gradient,
                        s.shadow
                      )}
                    >
                      <Icon className="h-5 w-5 text-white" />
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <span
                      className={cn(
                        "inline-flex items-center gap-0.5 font-semibold px-1.5 py-0.5 rounded-md",
                        positive
                          ? "text-emerald-700 bg-emerald-50 dark:text-emerald-400 dark:bg-emerald-500/10"
                          : "text-rose-700 bg-rose-50 dark:text-rose-400 dark:bg-rose-500/10"
                      )}
                    >
                      {positive ? (
                        <ArrowUpRight className="h-3 w-3" />
                      ) : (
                        <ArrowDownRight className="h-3 w-3" />
                      )}
                      {Math.abs(s.delta)}%
                    </span>
                    <span className="text-foreground-secondary">{s.deltaLabel}</span>
                  </div>
                </div>
              );
            })}
      </div>

      {/* Charts row */}
      <div className="grid gap-4 lg:grid-cols-3">
        {/* Revenue funnel */}
        <div
          className="card-elevated p-6 lg:col-span-2 animate-fade-in-up"
          style={{ animationDelay: "320ms" }}
        >
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
                Revenue Funnel
              </h2>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Visitors → bookings → paid
              </p>
            </div>
            <div className="flex gap-1 text-xs">
              {(["7d", "30d", "90d"] as const).map((p) => (
                <button
                  key={p}
                  onClick={() => setPeriod(p)}
                  className={cn(
                    "px-3 py-1.5 rounded-lg font-medium transition-colors",
                    period === p
                      ? "bg-primary-500 text-white shadow-sm shadow-primary-500/25"
                      : "text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-white/5"
                  )}
                >
                  {p}
                </button>
              ))}
            </div>
          </div>

          {/* Funnel chart */}
          {!funnel ? (
            <div className="space-y-3 animate-pulse">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3">
                  <div className="w-5 h-5 bg-slate-200 rounded-md" />
                  <div className="flex-1">
                    <div className="h-2.5 bg-slate-200 rounded-full" style={{ width: `${90 - i * 15}%` }} />
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div>
              <FunnelChart
                steps={funnel.steps.map((s) => ({ name: s.step, value: s.count }))}
              />
              <div className="mt-4 pt-4 border-t border-slate-100 dark:border-white/5 flex items-center justify-between text-xs">
                <span className="text-slate-500 dark:text-slate-400">Overall conversion</span>
                <span className="font-semibold text-emerald-600 bg-emerald-50 dark:text-emerald-400 dark:bg-emerald-500/10 px-2 py-0.5 rounded-full">
                  {funnel.overallConversion.toFixed(1)}%
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Activity feed */}
        <div
          className="card-elevated p-6 animate-fade-in-up"
          style={{ animationDelay: "400ms" }}
        >
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
              Recent Activity
            </h2>
            <button className="text-xs font-medium text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300">
              View all
            </button>
          </div>
          {activity.length === 0 ? (
            <div className="py-10 text-center">
              <div className="w-12 h-12 mx-auto mb-3 rounded-full bg-slate-50 dark:bg-white/5 flex items-center justify-center">
                <Activity className="h-5 w-5 text-foreground-muted" />
              </div>
              <p className="text-sm font-medium text-slate-900 dark:text-white">
                {EMPTY_ACTIVITY_HINT.title}
              </p>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                {EMPTY_ACTIVITY_HINT.detail}
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {activity.map((a, i) => {
                const dot = dotFor(a.type);
                return (
                  <div key={a.id ?? i} className="flex items-start gap-3">
                    <div className="relative mt-1.5">
                      <div className={cn("w-2 h-2 rounded-full", dot)} />
                      <div
                        className={cn(
                          "absolute inset-0 w-2 h-2 rounded-full animate-ping opacity-40",
                          dot
                        )}
                      />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-slate-900 dark:text-white font-medium truncate">
                        {a.title ?? a.type ?? "Activity"}
                      </p>
                      {a.description && (
                        <p className="text-xs text-slate-500 dark:text-slate-400 truncate">
                          {a.description}
                        </p>
                      )}
                    </div>
                    <span className="text-xs text-foreground-muted whitespace-nowrap">
                      {relativeTime(a.timestamp)}
                    </span>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
