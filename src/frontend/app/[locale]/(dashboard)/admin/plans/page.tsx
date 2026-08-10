"use client";

import React, { useEffect, useState } from "react";
import { 
  CreditCard, 
  Plus, 
  Settings2, 
  Check,
  X,
  AlertCircle,
  RefreshCw
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

export default function AdminPlansPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();
  
  const [loading, setLoading] = useState(true);
  const [plans, setPlans] = useState<any[]>([]);

  useEffect(() => {
    if (isInitialized && user?.role !== 'superadmin') {
      router.push('/dashboard');
    }
  }, [user, isInitialized, router]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.superAdmin.plans();
      setPlans(res.data || []);
    } catch (error) {
      console.error("Failed to fetch plans:", error);
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

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-amber-500 to-orange-600 rounded-2xl shadow-lg shadow-amber-500/20">
              <CreditCard className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
              Subscription Tiers
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">Design and manage your platform's pricing models.</p>
        </div>
        <div className="flex items-center gap-3">
          <Button onClick={fetchData} variant="outline" size="sm">
            <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button variant="primary" size="sm">
            <Plus className="h-4 w-4 mr-2" />
            Create Plan
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-96 bg-slate-100 dark:bg-slate-800 rounded-3xl animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {plans.map((plan) => (
            <div 
              key={plan.id} 
              className="group relative bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-3xl p-8 shadow-sm hover:shadow-xl hover:shadow-indigo-500/5 transition-all duration-300"
            >
              {!plan.isActive && (
                <div className="absolute top-4 right-4">
                  <Badge variant="secondary" className="bg-slate-100 text-slate-500">Draft</Badge>
                </div>
              )}
              
              <div className="mb-6">
                <h3 className="text-2xl font-bold text-slate-900 dark:text-white mb-2" style={{ fontFamily: 'var(--font-display)' }}>{plan.name}</h3>
                <p className="text-slate-500 text-sm h-10 overflow-hidden">{plan.description}</p>
              </div>

              <div className="mb-8">
                <div className="flex items-baseline gap-1">
                  <span className="text-4xl font-black text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>${plan.monthlyPrice}</span>
                  <span className="text-slate-500 text-sm font-medium">/month</span>
                </div>
                <div className="text-emerald-600 text-xs font-semibold mt-1">
                  ${plan.annualPrice} billed annually
                </div>
              </div>

              <div className="space-y-4 mb-8">
                <div className="text-xs font-bold uppercase tracking-wider text-slate-400">Core Limits</div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="p-3 bg-slate-50 dark:bg-white/5 rounded-2xl">
                    <div className="text-xs text-slate-500">Staff</div>
                    <div className="font-bold text-slate-900 dark:text-white">{plan.features?.maxStaff === -1 ? '∞' : plan.features?.maxStaff}</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-white/5 rounded-2xl">
                    <div className="text-xs text-slate-500">Locations</div>
                    <div className="font-bold text-slate-900 dark:text-white">{plan.features?.maxLocations === -1 ? '∞' : plan.features?.maxLocations}</div>
                  </div>
                </div>
              </div>

              <div className="space-y-3 mb-8">
                {plan.features?.onlineBooking && <FeatureItem label="Online Booking" enabled />}
                {plan.features?.whiteLabelDomain && <FeatureItem label="White Labeling" enabled />}
                {plan.features?.aiFeatures && <FeatureItem label="AI Smart Tools" enabled />}
                {plan.features?.apiAccess ? <FeatureItem label="API Access" enabled /> : <FeatureItem label="API Access" enabled={false} />}
              </div>

              <Button variant="outline" className="w-full rounded-2xl group-hover:bg-slate-900 group-hover:text-white dark:group-hover:bg-white dark:group-hover:text-slate-900 transition-colors">
                <Settings2 className="h-4 w-4 mr-2" />
                Edit Plan
              </Button>
            </div>
          ))}
        </div>
      )}
      
      {plans.length === 0 && !loading && (
        <div className="text-center py-20 bg-white dark:bg-slate-900 rounded-3xl border border-dashed border-slate-300 dark:border-slate-700">
          <AlertCircle className="h-12 w-12 text-slate-300 mx-auto mb-4" />
          <h3 className="text-lg font-bold text-slate-900 dark:text-white mb-2">No plans defined</h3>
          <p className="text-slate-500 mb-6">Create your first subscription tier to start accepting tenants.</p>
          <Button variant="primary">
            <Plus className="h-4 w-4 mr-2" />
            Create Plan
          </Button>
        </div>
      )}
    </div>
  );
}

function FeatureItem({ label, enabled }: { label: string; enabled: boolean }) {
  return (
    <div className="flex items-center gap-2 text-sm">
      {enabled ? (
        <div className="h-5 w-5 rounded-full bg-emerald-100 dark:bg-emerald-500/10 flex items-center justify-center">
          <Check className="h-3 w-3 text-emerald-600 dark:text-emerald-400" />
        </div>
      ) : (
        <div className="h-5 w-5 rounded-full bg-slate-100 dark:bg-white/5 flex items-center justify-center">
          <X className="h-3 w-3 text-slate-400" />
        </div>
      )}
      <span className={enabled ? 'text-slate-700 dark:text-slate-300' : 'text-slate-400 line-through'}>{label}</span>
    </div>
  );
}
