"use client";

import React, { useEffect, useState } from "react";
import { 
  Building2, 
  ShieldCheck, 
  Search, 
  Filter, 
  RefreshCw 
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { AdminStats } from "@/components/admin/AdminStats";
import { TenantTable } from "@/components/admin/TenantTable";
import { ServiceDonutChart, RevenueAreaChart } from "@/components/charts";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";

export default function AdminDashboardPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();
  
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState<any>(null);
  const [tenants, setTenants] = useState<any[]>([]);
  const [revenueTrend, setRevenueTrend] = useState<any[]>([]);
  const [tierDistributionData, setTierDistributionData] = useState<any[]>([]);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    if (isInitialized && user?.role !== 'superadmin') {
      router.push('/dashboard');
    }
  }, [user, isInitialized, router]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [statsRes, tenantsRes, revenueRes, tiersRes] = await Promise.all([
        api.superAdmin.analytics(),
        api.superAdmin.tenants({ page: 1, limit: 100 }),
        api.superAdmin.revenueTrend(),
        api.superAdmin.tierDistribution()
      ]);
      
      setStats(statsRes.data);
      setTenants(tenantsRes.data?.data || tenantsRes.data?.items || tenantsRes.data || []);
      setRevenueTrend(revenueRes.data?.data || revenueRes.data || []);
      setTierDistributionData(tiersRes.data?.data || tiersRes.data || []);
      
    } catch (error) {
      console.error("Failed to fetch admin data:", error);
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

  const filteredTenants = tenants.filter(t => 
    t.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
    t.slug.toLowerCase().includes(searchQuery.toLowerCase())
  );



  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-2xl shadow-lg shadow-indigo-500/20">
              <ShieldCheck className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
              Platform Administration
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">Monitor system-wide health, tenant growth, and billing.</p>
        </div>
        <Button onClick={fetchData} variant="outline" size="sm" className="w-fit">
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Refresh Data
        </Button>
      </div>

      {/* Stats Grid */}
      <AdminStats data={stats} loading={loading} />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Revenue Chart */}
        <div className="lg:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-6 shadow-sm">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'Outfit, sans-serif' }}>Platform Revenue</h2>
              <p className="text-sm text-slate-500">Gross revenue across all active tenants.</p>
            </div>
          </div>
          <RevenueAreaChart data={revenueTrend} height={300} />
        </div>

        {/* Tier Distribution */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-6 shadow-sm">
          <div className="mb-6">
            <h2 className="text-xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'Outfit, sans-serif' }}>Subscription Tiers</h2>
            <p className="text-sm text-slate-500">Breakdown of tenants by plan.</p>
          </div>
          <ServiceDonutChart data={tierDistributionData} height={200} />
        </div>
      </div>

      {/* Tenant Management */}
      <div className="space-y-4">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <h2 className="text-2xl font-bold text-slate-900 dark:text-white flex items-center gap-2" style={{ fontFamily: 'Outfit, sans-serif' }}>
            <Building2 className="h-6 w-6 text-slate-400" />
            Tenant Management
          </h2>
          <div className="flex items-center gap-3">
            <div className="relative w-full md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <Input 
                placeholder="Search by name or slug..." 
                className="pl-10"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
            <Button variant="outline" size="icon">
              <Filter className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <TenantTable tenants={filteredTenants} loading={loading} />
      </div>
    </div>
  );
}
