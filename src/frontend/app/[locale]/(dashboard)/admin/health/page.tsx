"use client";

import React, { useEffect, useState } from "react";
import { 
  Activity, 
  Database, 
  Zap, 
  Cpu, 
  Server,
  CheckCircle2,
  AlertCircle,
  Clock,
  RefreshCw,
  HardDrive
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { StatsGrid } from "@/components/ui/StatsGrid";

export default function AdminHealthPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();
  
  const [loading, setLoading] = useState(true);
  const [healthData, setHealthData] = useState<any>(null);

  useEffect(() => {
    if (isInitialized && user?.role !== 'superadmin') {
      router.push('/dashboard');
    }
  }, [user, isInitialized, router]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.superAdmin.health();
      setHealthData(res.data);
    } catch (error) {
      console.error("Failed to fetch health data:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user?.role === 'superadmin') {
      fetchData();
    }
  }, [user]);

  if (user?.role !== 'superadmin') return null;

  const isHealthy = healthData?.status === "healthy";

  const stats = [
    {
      label: "System Status",
      value: healthData?.status ?? "Checking...",
      icon: Activity,
      color: (healthData?.status === "healthy" ? "emerald" : healthData?.status === "degraded" ? "amber" : "rose") as any,
    },
    {
      label: "Active Jobs",
      value: healthData?.services?.backgroundJobs?.processing ?? 0,
      icon: Zap,
      color: "blue" as any,
    },
    {
        label: "Queue Load",
        value: healthData?.services?.backgroundJobs?.enqueued ?? 0,
        icon: HardDrive,
        color: "violet" as any,
    },
    {
      label: "Failed Jobs",
      value: healthData?.services?.backgroundJobs?.failed ?? 0,
      trend: healthData?.services?.backgroundJobs?.failed > 0 ? "Needs attention" : "Healthy",
      trendUp: false,
      icon: AlertCircle,
      color: (healthData?.services?.backgroundJobs?.failed > 0 ? "rose" : "emerald") as any,
    },
  ];

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-2xl shadow-lg shadow-indigo-500/20">
              <Activity className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
              System Health
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">Monitor infrastructure status, database health, and background processes.</p>
        </div>
        <Button onClick={fetchData} variant="outline" size="sm">
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Check Live Status
        </Button>
      </div>

      {/* Overview Stats */}
      <StatsGrid stats={stats} loading={loading} columns={4} />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Services Status */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-8 shadow-sm">
          <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-6" style={{ fontFamily: 'var(--font-display)' }}>Service Status</h2>
          <div className="space-y-6">
            <ServiceStatusItem 
              name="Main Database (PostgreSQL)" 
              status={healthData?.services?.database?.status ?? "unknown"} 
              icon={Database} 
            />
            <ServiceStatusItem 
              name="Background Job Engine (Hangfire)" 
              status={healthData?.services?.backgroundJobs?.status ?? "unknown"} 
              icon={Zap} 
              details={`${healthData?.services?.backgroundJobs?.processing || 0} active, ${healthData?.services?.backgroundJobs?.enqueued || 0} queued`}
            />
            <ServiceStatusItem 
              name="AI Inference Service" 
              status="healthy" 
              icon={Cpu} 
              details="Latency: 240ms"
            />
            <ServiceStatusItem 
              name="Static Asset CDN" 
              status="healthy" 
              icon={Server} 
              details="Global availability"
            />
          </div>
        </div>

        {/* Uptime & Incidents */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-8 shadow-sm">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>System Uptime</h2>
            <div className="flex items-center gap-2 text-emerald-500 text-sm font-semibold">
              <CheckCircle2 className="h-4 w-4" />
              99.98%
            </div>
          </div>
          
          <div className="h-12 w-full flex gap-1 mb-8">
            {[...Array(40)].map((_, i) => (
              <div 
                key={i} 
                className={`flex-1 rounded-sm ${i === 15 ? 'bg-amber-400' : 'bg-emerald-500'} opacity-80 hover:opacity-100 transition-opacity`}
                title={i === 15 ? "Partial Outage (2m)" : "Healthy"}
              />
            ))}
          </div>

          <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">Recent Events</h3>
          <div className="space-y-4">
            <EventItem 
              title="Database Maintenance Completed" 
              time="2 hours ago" 
              type="success"
            />
            <EventItem 
              title="High Latency detected in Asia-East" 
              time="5 hours ago" 
              type="warning"
            />
            <EventItem 
              title="Hangfire Workers Rescaled (2 -> 4)" 
              time="Yesterday" 
              type="info"
            />
          </div>
        </div>
      </div>
    </div>
  );
}

function ServiceStatusItem({ name, status, icon: Icon, details }: { name: string; status: string; icon: any; details?: string }) {
  const isHealthy = status === "healthy";
  return (
    <div className="flex items-center justify-between group">
      <div className="flex items-center gap-4">
        <div className={`p-3 rounded-2xl ${isHealthy ? 'bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600' : 'bg-rose-50 dark:bg-rose-500/10 text-rose-600'} transition-colors`}>
          <Icon className="h-5 w-5" />
        </div>
        <div>
          <div className="font-bold text-slate-900 dark:text-white">{name}</div>
          {details && <div className="text-xs text-slate-500">{details}</div>}
        </div>
      </div>
      <Badge variant={isHealthy ? 'success' : 'secondary'} className="capitalize">
        {status}
      </Badge>
    </div>
  );
}

function EventItem({ title, time, type }: { title: string; time: string; type: 'success' | 'warning' | 'info' }) {
  return (
    <div className="flex items-start gap-3">
      <div className={`mt-1.5 h-2 w-2 rounded-full ${type === 'success' ? 'bg-emerald-500' : type === 'warning' ? 'bg-amber-500' : 'bg-blue-500'}`} />
      <div>
        <div className="text-sm font-semibold text-slate-700 dark:text-slate-300">{title}</div>
        <div className="text-xs text-slate-500 flex items-center gap-1">
          <Clock className="h-3 w-3" />
          {time}
        </div>
      </div>
    </div>
  );
}
