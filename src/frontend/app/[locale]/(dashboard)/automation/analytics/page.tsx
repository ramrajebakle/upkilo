"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { BarChart3, TrendingUp, CheckCircle, XCircle, Clock, Zap, ArrowLeft, RefreshCw, Activity } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts';
import { toast } from 'sonner';

interface WorkflowAnalytics {
  totalWorkflows: number;
  activeWorkflows: number;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  avgSuccessRate: number;
  executionsByDay: { date: string; executions: number; successes: number; failures: number }[];
  topWorkflows: { id: string; name: string; executions: number; successRate: number }[];
  triggerBreakdown: { triggerType: string; count: number }[];
  recentFailures: { workflowName: string; failedAt: string; errorMessage?: string; stepName?: string }[];
}

const COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export default function WorkflowAnalyticsPage() {
  const router = useRouter();
  const [analytics, setAnalytics] = useState<WorkflowAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [period, setPeriod] = useState<'7d' | '30d' | '90d'>('30d');

  const fetchAnalytics = useCallback(async () => {
    try {
      setLoading(true);
      const res = await apiClient.get(`/api/v1/workflows/analytics?period=${period}`);
      setAnalytics(res.data?.data || res.data);
    } catch {
      toast.error('Failed to load workflow analytics');
      // Fallback mock data for display
      setAnalytics({
        totalWorkflows: 0, activeWorkflows: 0, totalExecutions: 0,
        successfulExecutions: 0, failedExecutions: 0, avgSuccessRate: 0,
        executionsByDay: [], topWorkflows: [], triggerBreakdown: [], recentFailures: [],
      });
    } finally {
      setLoading(false);
    }
  }, [period]);

  useEffect(() => { fetchAnalytics(); }, [fetchAnalytics]);

  const statCards = analytics ? [
    { label: 'Total Executions', value: analytics.totalExecutions.toLocaleString(), icon: <Activity className="h-5 w-5 text-blue-500" />, color: 'blue' },
    { label: 'Successful', value: analytics.successfulExecutions.toLocaleString(), icon: <CheckCircle className="h-5 w-5 text-emerald-500" />, color: 'emerald' },
    { label: 'Failed', value: analytics.failedExecutions.toLocaleString(), icon: <XCircle className="h-5 w-5 text-red-500" />, color: 'red' },
    { label: 'Success Rate', value: `${analytics.avgSuccessRate.toFixed(1)}%`, icon: <TrendingUp className="h-5 w-5 text-indigo-500" />, color: 'indigo' },
  ] : [];

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button onClick={() => router.back()} className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 dark:text-slate-400">
            <ArrowLeft className="h-4 w-4" />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Workflow Analytics</h1>
            <p className="text-slate-500 dark:text-slate-400 mt-0.5">Monitor automation performance and success rates</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex gap-1 bg-slate-100 dark:bg-slate-800 rounded-lg p-1">
            {(['7d', '30d', '90d'] as const).map(p => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${period === p ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-white shadow-sm' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200'}`}
              >
                {p === '7d' ? '7 days' : p === '30d' ? '30 days' : '90 days'}
              </button>
            ))}
          </div>
          <button onClick={fetchAnalytics} className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 dark:text-slate-400">
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {loading ? (
        <div className="grid grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 animate-pulse">
              <div className="h-8 bg-slate-200 dark:bg-slate-800 rounded w-16 mb-2" />
              <div className="h-4 bg-slate-100 dark:bg-slate-800/50 rounded w-24" />
            </div>
          ))}
        </div>
      ) : (
        <>
          {/* Stat Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {statCards.map(stat => (
              <div key={stat.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4">
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-slate-50 dark:bg-slate-800">{stat.icon}</div>
                  <div>
                    <div className="text-2xl font-bold text-slate-900 dark:text-white">{stat.value}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">{stat.label}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Execution Trend Chart */}
          {analytics && analytics.executionsByDay.length > 0 && (
            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
              <h2 className="text-base font-semibold text-slate-900 dark:text-white mb-4">Execution Trend</h2>
              <ResponsiveContainer width="100%" height={220}>
                <AreaChart data={analytics.executionsByDay} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
                  <defs>
                    <linearGradient id="successGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#10b981" stopOpacity={0.15} />
                      <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="failGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#ef4444" stopOpacity={0.15} />
                      <stop offset="95%" stopColor="#ef4444" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                  <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis tick={{ fontSize: 11, fill: '#64748b' }} />
                  <Tooltip
                    contentStyle={{ borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                    labelStyle={{ fontWeight: 600 }}
                  />
                  <Area type="monotone" dataKey="successes" stroke="#10b981" fill="url(#successGrad)" strokeWidth={2} name="Successful" />
                  <Area type="monotone" dataKey="failures" stroke="#ef4444" fill="url(#failGrad)" strokeWidth={2} name="Failed" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Top Workflows */}
            {analytics && analytics.topWorkflows.length > 0 && (
              <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                <h2 className="text-base font-semibold text-slate-900 dark:text-white mb-4">Top Workflows</h2>
                <div className="space-y-3">
                  {analytics.topWorkflows.slice(0, 8).map((wf, idx) => (
                    <div key={wf.id} className="flex items-center gap-3">
                      <span className="text-xs font-bold text-slate-400 w-5 text-right">{idx + 1}</span>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between mb-1">
                          <span className="text-sm font-medium text-slate-700 dark:text-slate-300 truncate">{wf.name}</span>
                          <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 shrink-0">{wf.executions} runs</span>
                        </div>
                        <div className="h-1.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                          <div
                            className={`h-full rounded-full ${wf.successRate >= 90 ? 'bg-emerald-500' : wf.successRate >= 70 ? 'bg-amber-500' : 'bg-red-400'}`}
                            style={{ width: `${wf.successRate}%` }}
                          />
                        </div>
                      </div>
                      <span className={`text-xs font-semibold w-12 text-right ${wf.successRate >= 90 ? 'text-emerald-600' : wf.successRate >= 70 ? 'text-amber-600' : 'text-red-500'}`}>
                        {wf.successRate.toFixed(0)}%
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Trigger Breakdown */}
            {analytics && analytics.triggerBreakdown.length > 0 && (
              <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
                <h2 className="text-base font-semibold text-slate-900 dark:text-white mb-4">Trigger Breakdown</h2>
                <ResponsiveContainer width="100%" height={220}>
                  <PieChart>
                    <Pie
                      data={analytics.triggerBreakdown}
                      dataKey="count"
                      nameKey="triggerType"
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      label={({ triggerType, percent }) => `${triggerType} ${(percent * 100).toFixed(0)}%`}
                      labelLine={false}
                    >
                      {analytics.triggerBreakdown.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </div>

          {/* Recent Failures */}
          {analytics && analytics.recentFailures.length > 0 && (
            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5">
              <h2 className="text-base font-semibold text-slate-900 dark:text-white mb-4 flex items-center gap-2">
                <XCircle className="h-4 w-4 text-red-500" /> Recent Failures
              </h2>
              <div className="space-y-2">
                {analytics.recentFailures.map((failure, idx) => (
                  <div key={idx} className="flex items-start gap-3 p-3 bg-red-50 dark:bg-red-500/10 rounded-lg border border-red-100 dark:border-red-900/50">
                    <XCircle className="h-4 w-4 text-red-500 shrink-0 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-slate-900 dark:text-white">{failure.workflowName}</span>
                        <span className="text-xs text-slate-500 dark:text-slate-400">{new Date(failure.failedAt).toLocaleString()}</span>
                      </div>
                      {failure.stepName && <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5">Step: {failure.stepName}</p>}
                      {failure.errorMessage && (
                        <p className="text-xs text-red-600 dark:text-red-400 mt-1 font-mono bg-red-100 dark:bg-red-900/40 px-2 py-1 rounded">{failure.errorMessage}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Empty state */}
          {analytics && analytics.totalExecutions === 0 && (
            <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
              <BarChart3 className="h-12 w-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" />
              <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">No execution data yet</h3>
              <p className="text-slate-500 dark:text-slate-400 text-sm mt-1 mb-4">Analytics will appear once your workflows start running</p>
              <Button onClick={() => router.push('/automation/workflows')}>
                <Zap className="h-4 w-4 mr-2" /> View Workflows
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
