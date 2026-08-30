"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Play, Pause, Trash2, BarChart3, Zap, Search, MoreVertical, Copy, Clock, CheckCircle, XCircle, AlertTriangle, Filter } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Badge } from '@/components/ui/Badge';
import { toast } from 'sonner';

interface WorkflowStep {
  id: string;
  stepOrder: number;
  actionType: string;
  config: Record<string, unknown>;
}

interface Workflow {
  id: string;
  name: string;
  description?: string;
  status: 'draft' | 'active' | 'paused' | 'archived';
  triggerType: string;
  version: number;
  executionCount: number;
  successCount: number;
  failureCount: number;
  lastExecutedAt?: string;
  createdAt: string;
  updatedAt: string;
  steps?: WorkflowStep[];
}

const statusColors: Record<string, string> = {
  draft: 'bg-muted text-foreground',
  active: 'bg-emerald-100 text-emerald-700',
  paused: 'bg-amber-100 text-amber-700',
  archived: 'bg-muted text-foreground-secondary',
};

const statusIcons: Record<string, React.ReactNode> = {
  draft: <AlertTriangle className="h-3 w-3" />,
  active: <CheckCircle className="h-3 w-3" />,
  paused: <Pause className="h-3 w-3" />,
  archived: <XCircle className="h-3 w-3" />,
};

const triggerLabels: Record<string, string> = {
  ManualTrigger: 'Manual',
  BookingCreated: 'Booking Created',
  BookingCancelled: 'Booking Cancelled',
  BookingCompleted: 'Booking Completed',
  ClientCreated: 'New Client',
  PaymentReceived: 'Payment Received',
  PaymentFailed: 'Payment Failed',
  FormSubmitted: 'Form Submitted',
  ReviewReceived: 'Review Received',
  ScheduledTrigger: 'Scheduled',
  TagAdded: 'Tag Added',
  StageChanged: 'Stage Changed',
};

export default function WorkflowsPage() {
  const router = useRouter();
  const [workflows, setWorkflows] = useState<Workflow[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [openMenu, setOpenMenu] = useState<string | null>(null);

  const fetchWorkflows = useCallback(async () => {
    try {
      setLoading(true);
      const res = await apiClient.get('/api/v1/workflows');
      setWorkflows(res.data?.data || res.data || []);
    } catch {
      toast.error('Failed to load workflows');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchWorkflows();
  }, [fetchWorkflows]);

  const handleToggleStatus = async (workflow: Workflow) => {
    const newStatus = workflow.status === 'active' ? 'paused' : 'active';
    try {
      await apiClient.patch(`/api/v1/workflows/${workflow.id}/status`, { status: newStatus });
      setWorkflows(prev => prev.map(w => w.id === workflow.id ? { ...w, status: newStatus as Workflow['status'] } : w));
      toast.success(`Workflow ${newStatus === 'active' ? 'activated' : 'paused'}`);
    } catch {
      toast.error('Failed to update workflow status');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this workflow? This action cannot be undone.')) return;
    try {
      await apiClient.delete(`/api/v1/workflows/${id}`);
      setWorkflows(prev => prev.filter(w => w.id !== id));
      toast.success('Workflow deleted');
    } catch {
      toast.error('Failed to delete workflow');
    }
  };

  const handleDuplicate = async (id: string) => {
    try {
      const res = await apiClient.post(`/api/v1/workflows/${id}/duplicate`);
      const newWorkflow = res.data?.data || res.data;
      if (newWorkflow) setWorkflows(prev => [newWorkflow, ...prev]);
      toast.success('Workflow duplicated');
    } catch {
      toast.error('Failed to duplicate workflow');
    }
    setOpenMenu(null);
  };

  const filtered = workflows.filter(w => {
    const matchesSearch = !searchQuery || w.name.toLowerCase().includes(searchQuery.toLowerCase()) || w.description?.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = statusFilter === 'all' || w.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const stats = {
    total: workflows.length,
    active: workflows.filter(w => w.status === 'active').length,
    totalExecutions: workflows.reduce((sum, w) => sum + (w.executionCount || 0), 0),
    avgSuccessRate: workflows.length > 0
      ? Math.round(workflows.reduce((sum, w) => sum + ((w.executionCount || 0) > 0 ? ((w.successCount || 0) / (w.executionCount || 1)) * 100 : 100), 0) / workflows.length)
      : 0,
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Automation Workflows</h1>
          <p className="text-slate-500 dark:text-slate-400 mt-1">Automate repetitive tasks and client communications</p>
        </div>
        <Button onClick={() => router.push('/automation/workflows/new')} className="flex items-center gap-2">
          <Plus className="h-4 w-4" /> New Workflow
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total Workflows', value: stats.total, icon: <Zap className="h-5 w-5 text-primary-500" /> },
          { label: 'Active', value: stats.active, icon: <CheckCircle className="h-5 w-5 text-success-fg" /> },
          { label: 'Total Executions', value: stats.totalExecutions.toLocaleString(), icon: <BarChart3 className="h-5 w-5 text-blue-500" /> },
          { label: 'Avg Success Rate', value: `${stats.avgSuccessRate}%`, icon: <CheckCircle className="h-5 w-5 text-success-fg" /> },
        ].map(stat => (
          <div key={stat.label} className="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 p-4 flex items-center gap-3">
            <div className="p-2 rounded-lg bg-slate-50 dark:bg-slate-800">{stat.icon}</div>
            <div>
              <p className="text-2xl font-bold text-slate-900 dark:text-white">{stat.value}</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">{stat.label}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
          <Input
            placeholder="Search workflows..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
        <div className="flex gap-2">
          <Filter className="h-4 w-4 text-foreground-muted self-center" />
          {['all', 'active', 'paused', 'draft', 'archived'].map(s => (
            <button
              key={s}
              onClick={() => setStatusFilter(s)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${statusFilter === s ? 'bg-primary-600 text-white' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'}`}
            >
              {s.charAt(0).toUpperCase() + s.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Workflow List */}
      {loading ? (
        <div className="grid gap-4">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 animate-pulse">
              <div className="h-5 bg-slate-200 dark:bg-slate-800 rounded w-1/3 mb-2" />
              <div className="h-4 bg-slate-100 dark:bg-slate-800/50 rounded w-2/3" />
            </div>
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
          <Zap className="h-12 w-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">No workflows yet</h3>
          <p className="text-slate-500 dark:text-slate-400 text-sm mt-1 mb-4">Create your first automation workflow to get started</p>
          <Button onClick={() => router.push('/automation/workflows/new')}>
            <Plus className="h-4 w-4 mr-2" /> Create Workflow
          </Button>
        </div>
      ) : (
        <div className="grid gap-4">
          {filtered.map(workflow => {
            const successRate = (workflow.executionCount || 0) > 0
              ? Math.round(((workflow.successCount || 0) / (workflow.executionCount || 1)) * 100)
              : null;

            return (
              <div key={workflow.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 hover:shadow-md transition-shadow">
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 flex-wrap">
                      <h3
                        className="font-semibold text-slate-900 dark:text-white hover:text-primary-600 cursor-pointer"
                        onClick={() => router.push(`/automation/workflows/new?id=${workflow.id}`)}
                      >
                        {workflow.name}
                      </h3>
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[workflow.status]}`}>
                        {statusIcons[workflow.status]} {workflow.status}
                      </span>
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 dark:bg-blue-500/10 text-blue-700 dark:text-blue-400">
                        <Zap className="h-3 w-3" /> {triggerLabels[workflow.triggerType] || workflow.triggerType}
                      </span>
                      <span className="text-xs text-foreground-muted">v{workflow.version}</span>
                    </div>
                    {workflow.description && (
                      <p className="text-sm text-slate-500 dark:text-slate-400 mt-1 truncate">{workflow.description}</p>
                    )}
                    <div className="flex items-center gap-6 mt-3">
                      <div className="flex items-center gap-1.5 text-sm text-slate-600 dark:text-slate-400">
                        <BarChart3 className="h-4 w-4 text-foreground-muted" />
                        <span>{(workflow.executionCount || 0).toLocaleString()} runs</span>
                      </div>
                      {successRate !== null && (
                        <div className="flex items-center gap-1.5 text-sm">
                          <CheckCircle className={`h-4 w-4 ${successRate >= 90 ? 'text-success-fg' : successRate >= 70 ? 'text-warning-fg' : 'text-red-400'}`} />
                          <span className={successRate >= 90 ? 'text-emerald-600 dark:text-emerald-400' : successRate >= 70 ? 'text-amber-600 dark:text-amber-400' : 'text-red-500 dark:text-red-400'}>
                            {successRate}% success
                          </span>
                        </div>
                      )}
                      {workflow.lastExecutedAt && (
                        <div className="flex items-center gap-1.5 text-sm text-foreground-secondary">
                          <Clock className="h-4 w-4" />
                          <span>Last run {new Date(workflow.lastExecutedAt).toLocaleDateString()}</span>
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 ml-4">
                    <button
                      onClick={() => handleToggleStatus(workflow)}
                      className={`p-2 rounded-lg transition-colors ${workflow.status === 'active' ? 'text-warning-fg hover:bg-amber-50' : 'text-success-fg hover:bg-emerald-50'}`}
                      title={workflow.status === 'active' ? 'Pause' : 'Activate'}
                    >
                      {workflow.status === 'active' ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                    </button>
                    <div className="relative">
                      <button
                        onClick={() => setOpenMenu(openMenu === workflow.id ? null : workflow.id)}
                        className="p-2 rounded-lg text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
                      >
                        <MoreVertical className="h-4 w-4" />
                      </button>
                      {openMenu === workflow.id && (
                        <div className="absolute right-0 top-full mt-1 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg shadow-lg z-10 w-40 py-1">
                          <button
                            className="w-full text-left px-4 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 flex items-center gap-2"
                            onClick={() => router.push(`/automation/workflows/new?id=${workflow.id}`)}
                          >
                            <Zap className="h-4 w-4" /> Edit
                          </button>
                          <button
                            className="w-full text-left px-4 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 flex items-center gap-2"
                            onClick={() => handleDuplicate(workflow.id)}
                          >
                            <Copy className="h-4 w-4" /> Duplicate
                          </button>
                          <button
                            className="w-full text-left px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 flex items-center gap-2"
                            onClick={() => { handleDelete(workflow.id); setOpenMenu(null); }}
                          >
                            <Trash2 className="h-4 w-4" /> Delete
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                {/* Progress bar for success rate */}
                {successRate !== null && (workflow.executionCount || 0) > 0 && (
                  <div className="mt-3">
                    <div className="h-1.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all ${successRate >= 90 ? 'bg-emerald-500' : successRate >= 70 ? 'bg-amber-500' : 'bg-red-400'}`}
                        style={{ width: `${successRate}%` }}
                      />
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
