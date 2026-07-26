import React from "react";
import { Users, Building2, DollarSign, Activity, ShieldCheck } from "lucide-react";
import { StatsGrid } from "@/components/ui/StatsGrid";

interface AdminStatsData {
  totalTenants: number;
  activeTenants: number;
  totalUsers: number;
  totalRevenue: number;
  revenueChange?: number;
  totalBookings: number;
  activeSubscriptions: number;
}

interface AdminStatsProps {
  data: AdminStatsData | null;
  loading: boolean;
}

export function AdminStats({ data, loading }: AdminStatsProps) {
  const stats = [
    {
      label: "Total Tenants",
      value: data?.totalTenants ?? 0,
      icon: Building2,
      color: "blue" as const,
    },
    {
      label: "Active Tenants",
      value: data?.activeTenants ?? 0,
      trend: data?.totalTenants ? `${((data.activeTenants / data.totalTenants) * 100).toFixed(0)}%` : undefined,
      trendUp: true,
      icon: Activity,
      color: "emerald" as const,
    },
    {
      label: "Platform Users",
      value: data?.totalUsers ?? 0,
      icon: Users,
      color: "violet" as const,
    },
    {
      label: "Total Revenue",
      value: data?.totalRevenue ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(data.totalRevenue) : "$0",
      trend: data?.revenueChange ? `${Math.abs(data.revenueChange)}%` : undefined,
      trendUp: (data?.revenueChange ?? 0) >= 0,
      icon: DollarSign,
      color: "amber" as const,
    },
    {
        label: "Platform Bookings",
        value: data?.totalBookings ?? 0,
        icon: Activity,
        color: "cyan" as const,
    },
    {
        label: "Active Subs",
        value: data?.activeSubscriptions ?? 0,
        icon: ShieldCheck,
        color: "orange" as const,
    }
  ];

  return <StatsGrid stats={stats} loading={loading} columns={3} />;
}
