import React from "react";
import { 
  Table, 
  TableHeader, 
  TableBody, 
  TableHead, 
  TableRow, 
  TableCell 
} from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { ExternalLink, Users, Calendar } from "lucide-react";

interface Tenant {
  id: string;
  name: string;
  slug: string;
  status: string;
  subscriptionTier: string;
  userCount: number;
  bookingCount: number;
  aiUsed: number;
  aiBudget: number;
  createdAt: string;
}

interface TenantTableProps {
  tenants: Tenant[];
  loading: boolean;
}

export function TenantTable({ tenants, loading }: TenantTableProps) {
  if (loading) {
    return (
      <div className="space-y-4 animate-pulse">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-16 bg-slate-100 dark:bg-slate-800 rounded-xl" />
        ))}
      </div>
    );
  }

  if (tenants.length === 0) {
    return (
      <div className="text-center py-12 bg-white dark:bg-slate-900 rounded-2xl border border-dashed">
        <p className="text-foreground-secondary">No tenants found matching your criteria.</p>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-white/5 overflow-hidden shadow-sm">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Tenant Name</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Tier</TableHead>
            <TableHead>Usage</TableHead>
            <TableHead>AI Credits</TableHead>
            <TableHead>Created</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tenants.map((tenant) => (
            <TableRow key={tenant.id} className="group hover:bg-slate-50 dark:hover:bg-white/5 transition-colors">
              <TableCell>
                <div>
                  <div className="font-semibold text-slate-900 dark:text-white">{tenant.name}</div>
                  <div className="text-xs text-slate-500 dark:text-slate-400 font-mono">/{tenant.slug}</div>
                </div>
              </TableCell>
              <TableCell>
                <Badge 
                  variant={tenant.status.toLowerCase() === 'active' ? 'success' : 'secondary'}
                >
                  {tenant.status}
                </Badge>
              </TableCell>
              <TableCell>
                <Badge 
                  variant="outline"
                  className={getColorForTier(tenant.subscriptionTier)}
                >
                  {tenant.subscriptionTier}
                </Badge>
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-4 text-xs text-foreground-secondary">
                  <span className="flex items-center gap-1">
                    <Users className="h-3 w-3" /> {tenant.userCount}
                  </span>
                  <span className="flex items-center gap-1">
                    <Calendar className="h-3 w-3" /> {tenant.bookingCount}
                  </span>
                </div>
              </TableCell>
              <TableCell>
                <div className="space-y-1.5 min-w-[120px]">
                  <div className="flex justify-between text-[10px] font-medium">
                    <span className="text-foreground-secondary">${tenant.aiUsed?.toFixed(2) || '0.00'}</span>
                    <span className="text-foreground-muted">/ ${tenant.aiBudget?.toFixed(0) || '0'}</span>
                  </div>
                  <div className="h-1.5 w-full bg-slate-100 dark:bg-white/5 rounded-full overflow-hidden">
                    <div 
                      className={`h-full transition-all duration-500 rounded-full ${
                        (tenant.aiUsed / tenant.aiBudget) > 0.9 ? 'bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.4)]' : 
                        (tenant.aiUsed / tenant.aiBudget) > 0.7 ? 'bg-amber-500' : 'bg-emerald-500'
                      }`}
                      style={{ width: `${Math.min((tenant.aiUsed / (tenant.aiBudget || 1)) * 100, 100)}%` }}
                    />
                  </div>
                </div>
              </TableCell>
              <TableCell className="text-xs text-foreground-secondary">
                {new Date(tenant.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell className="text-right">
                <Button variant="ghost" size="sm" className="opacity-0 group-hover:opacity-100 transition-opacity">
                  <ExternalLink className="h-4 w-4 mr-2" />
                  Details
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function getColorForTier(tier: string) {
  switch (tier.toLowerCase()) {
    case 'enterprise': return 'border-primary-200 bg-primary-50 text-primary-700 dark:bg-primary-900/20 dark:text-primary-400';
    case 'business': return 'border-blue-200 bg-blue-50 text-blue-700 dark:bg-blue-900/20 dark:text-blue-400';
    case 'professional': return 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:bg-emerald-900/20 dark:text-emerald-400';
    default: return 'border-border bg-muted text-foreground';
  }
}
