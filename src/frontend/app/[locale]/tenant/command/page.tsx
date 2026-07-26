"use client";

import React, { useEffect, useState } from "react";
import { Sparkles, CheckCircle2, Circle, Clock, Lock } from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { CurrencyFormatter } from "@/components/ui/CurrencyFormatter";
import { useTenantCurrency } from "@/hooks/useTenantCurrency";
import api from "@/lib/api";

interface TodayBooking {
  id: string;
  clientName: string;
  serviceName: string;
  staffName: string;
  startTime: string;
  status: string;
  price: number;
}

function greetingFor(date: Date) {
  const h = date.getHours();
  if (h < 12) return "Good morning";
  if (h < 18) return "Good afternoon";
  return "Good evening";
}

function isSameDay(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

export default function TenantCommandPage() {
  const currency = useTenantCurrency();

  const [firstName, setFirstName] = useState<string>("");
  const [todayBookings, setTodayBookings] = useState<TodayBooking[]>([]);
  const [weekRevenue, setWeekRevenue] = useState<number | null>(null);
  const [insight, setInsight] = useState<string | null>(null);
  const [insightLocked, setInsightLocked] = useState(false);
  const [loading, setLoading] = useState(true);

  const now = new Date();
  const currentDate = now.toLocaleDateString("en-US", {
    weekday: "long",
    hour: "numeric",
    minute: "numeric",
  });

  useEffect(() => {
    let active = true;

    const load = async () => {
      // Each panel degrades independently — one failing endpoint must not blank the page.
      const [meRes, bookingsRes, revenueRes] = await Promise.allSettled([
        api.auth.me(),
        api.bookings.list(),
        api.analytics.revenue("week"),
      ]);

      if (!active) return;

      if (meRes.status === "fulfilled") {
        setFirstName(meRes.value.data?.firstName || "");
      }

      if (bookingsRes.status === "fulfilled") {
        const raw = bookingsRes.value.data?.data ?? bookingsRes.value.data ?? [];
        const today = (Array.isArray(raw) ? raw : [])
          .filter((b: any) => b.startTime && isSameDay(new Date(b.startTime), new Date()))
          .sort(
            (a: any, b: any) =>
              new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
          )
          .map((b: any) => ({
            id: b.id,
            clientName: b.clientName || "Unknown client",
            serviceName: b.serviceName || "",
            staffName: b.staffName || "",
            startTime: b.startTime,
            status: b.status || "",
            price: Number(b.price) || 0,
          }));
        setTodayBookings(today);
      }

      if (revenueRes.status === "fulfilled") {
        setWeekRevenue(Number(revenueRes.value.data?.totalRevenue) || 0);
      }

      setLoading(false);
    };

    load();

    // AI insights are a paid feature; a 403 here is an expected plan gate, not an error.
    api.aiDashboard
      .recommendations()
      .then((res) => {
        if (!active) return;
        const list = res.data?.recommendations ?? res.data?.data ?? res.data;
        const first = Array.isArray(list) ? list[0] : null;
        setInsight(
          typeof first === "string" ? first : first?.message || first?.title || null
        );
      })
      .catch((err) => {
        if (!active) return;
        if (err?.response?.status === 403) setInsightLocked(true);
      });

    return () => {
      active = false;
    };
  }, []);

  const upcoming = todayBookings.filter(
    (b) => new Date(b.startTime) >= now && b.status !== "Cancelled"
  );

  return (
    <div className="space-y-8 animate-fade-in max-w-4xl">
      <header className="pb-6">
        <p className="text-text-tertiary text-sm font-medium mb-1 tracking-wide uppercase">
          {currentDate}
        </p>
        <h1 className="text-3xl font-bold text-text-primary">
          {greetingFor(now)}
          {firstName ? `, ${firstName}` : ""}.
        </h1>
      </header>

      {/* Primary KPI row */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="bg-surface-0">
          <CardContent className="p-5 flex flex-col items-center justify-center text-center h-full">
            <span className="text-3xl font-bold text-text-primary mb-1">
              {loading ? "—" : upcoming.length}
            </span>
            <span className="text-sm text-text-secondary">Appointments left today</span>
          </CardContent>
        </Card>

        <Card className="bg-surface-0 border-t-[4px] border-t-tenant-500">
          <CardContent className="p-5 flex flex-col items-center justify-center text-center h-full">
            <span className="text-3xl font-bold text-text-primary mb-1">
              {weekRevenue === null ? (
                "—"
              ) : (
                <CurrencyFormatter amount={weekRevenue} currency={currency} />
              )}
            </span>
            <span className="text-sm text-text-secondary">Revenue this week</span>
          </CardContent>
        </Card>

        <Card className="bg-ai-50 border-ai-200">
          <CardContent className="p-5 flex flex-col items-center justify-center text-center h-full">
            <div className="flex items-center gap-1.5 mb-1">
              {insightLocked ? (
                <Lock size={16} className="text-ai-500" />
              ) : (
                <Sparkles size={16} className="text-ai-500" />
              )}
              <span className="text-sm font-semibold text-ai-600">AI Briefing</span>
            </div>
            <span className="text-sm text-text-secondary mt-1">
              {insightLocked
                ? "Upgrade your plan to unlock AI insights"
                : insight || "No new insights right now"}
            </span>
          </CardContent>
        </Card>
      </div>

      {/* Daily Focus View */}
      <section className="pt-4">
        <h2 className="text-lg font-semibold text-text-primary mb-4 border-b border-surface-200 pb-2">
          YOUR DAY ·{" "}
          {now.toLocaleDateString("en-US", { weekday: "long" })}
        </h2>

        <div className="space-y-3">
          {loading && (
            <p className="text-sm text-text-tertiary p-3">Loading today's schedule…</p>
          )}

          {!loading && todayBookings.length === 0 && (
            <div className="p-6 text-center">
              <p className="text-text-secondary font-medium">
                Nothing booked for today.
              </p>
              <p className="text-sm text-text-tertiary mt-1">
                New appointments will appear here as they come in.
              </p>
            </div>
          )}

          {todayBookings.map((b) => {
            const start = new Date(b.startTime);
            const past = start < now;
            const cancelled = b.status === "Cancelled";
            return (
              <div
                key={b.id}
                className="group flex items-start gap-3 p-3 rounded-lg hover:bg-surface-100 transition-colors"
              >
                {past || cancelled ? (
                  <CheckCircle2 size={18} className="text-success-500 mt-0.5" />
                ) : (
                  <Circle size={18} className="text-neutral-300 mt-0.5 group-hover:text-primary-500" />
                )}
                <div className={`flex-1 ${past || cancelled ? "opacity-60" : ""}`}>
                  <div className="flex justify-between items-start gap-3">
                    <span
                      className={`font-medium text-text-primary ${cancelled ? "line-through" : ""}`}
                    >
                      {b.serviceName ? `${b.serviceName} · ` : ""}
                      {b.clientName}
                    </span>
                    <span className="text-xs text-text-tertiary flex items-center gap-1 shrink-0">
                      <Clock size={12} />
                      {start.toLocaleTimeString("en-US", {
                        hour: "numeric",
                        minute: "2-digit",
                      })}
                    </span>
                  </div>
                  <div className="mt-1 flex items-center gap-2 text-sm text-text-secondary">
                    {b.staffName && <span>with {b.staffName}</span>}
                    {b.price > 0 && (
                      <>
                        <span className="text-text-tertiary">·</span>
                        <CurrencyFormatter amount={b.price} currency={currency} />
                      </>
                    )}
                  </div>
                </div>
              </div>
            );
          })}

          <Button
            variant="ghost"
            className="w-full justify-start text-text-tertiary mt-2"
            leftIcon={<span className="text-lg leading-none">+</span>}
            onClick={() => {
              window.location.href = "/en/bookings";
            }}
          >
            New booking
          </Button>
        </div>
      </section>
    </div>
  );
}
