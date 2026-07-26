"use client";

import React from "react";
import { HealthMatrix } from "@/components/platform/HealthMatrix";
import { Button } from "@/components/ui/Button";
import { Plus, Download, Loader2 } from "lucide-react";
import { useTenants } from "@/hooks/usePlatformData";

export default function TenantsPage() {
  const { data: tenants, isLoading, isError } = useTenants();
  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary">
            Tenants
          </h1>
          <p className="text-text-secondary mt-1">
            Overview of all active tenants and their health status.
          </p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" leftIcon={<Download size={16} />}>
            Export
          </Button>
          <Button variant="primary" leftIcon={<Plus size={16} />}>
            Provision Tenant
          </Button>
        </div>
      </header>

      <section>
        {isLoading ? (
          <div className="h-[480px] bg-surface-base border border-surface-200 rounded-xl flex flex-col items-center justify-center text-text-tertiary animate-pulse">
            <Loader2 size={32} className="animate-spin mb-4" />
            <p>Loading tenant health data...</p>
          </div>
        ) : isError ? (
          <div className="h-[480px] bg-danger-50 border border-danger-200 rounded-xl flex items-center justify-center text-danger-600">
            Failed to load tenant data.
          </div>
        ) : (
          <HealthMatrix tenants={tenants || []} />
        )}
      </section>

      {/* Placeholder for list view or other tenant management features */}
      <section className="pt-8">
        <h3 className="text-lg font-semibold text-text-primary mb-4">Recent Activity</h3>
        <div className="text-text-secondary text-sm bg-surface-0 border border-surface-200 rounded-lg p-8 text-center">
          Activity log would go here. We prioritize the health matrix above the fold.
        </div>
      </section>
    </div>
  );
}
