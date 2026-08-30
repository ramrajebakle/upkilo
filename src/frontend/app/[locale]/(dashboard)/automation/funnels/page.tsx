"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Play, Pause, Trash2, BarChart3, ArrowRight, Target, Users, Zap, Search, MoreVertical } from 'lucide-react';
import api from '@/lib/api';

interface FunnelStep {
  id: string;
  name: string;
  stepOrder: number;
  stepType: string;
  pageUrl?: string;
  enteredCount: number;
  completedCount: number;
  dropOffRate: number;
}

interface Funnel {
  id: string;
  name: string;
  description?: string;
  status: string;
  triggerType: string;
  conversionGoal?: string;
  isActive: boolean;
  totalEntered: number;
  totalConverted: number;
  conversionRate: number;
  activatedAt?: string;
  createdAt: string;
  steps?: FunnelStep[];
}

const statusColors: Record<string, string> = {
  draft: 'bg-muted text-foreground',
  active: 'bg-emerald-100 text-emerald-700',
  paused: 'bg-amber-100 text-amber-700',
  completed: 'bg-blue-100 text-blue-700',
  archived: 'bg-muted text-foreground-secondary',
};

const stepTypeIcons: Record<string, React.ReactNode> = {
  Page: <Target className="w-4 h-4" />,
  Form: <Users className="w-4 h-4" />,
  Payment: <Zap className="w-4 h-4" />,
  Booking: <ArrowRight className="w-4 h-4" />,
};

export default function FunnelsPage() {
  const [funnels, setFunnels] = useState<Funnel[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [selectedFunnel, setSelectedFunnel] = useState<Funnel | null>(null);
  const [newFunnel, setNewFunnel] = useState({ name: '', description: '', triggerType: 'manual', conversionGoal: '' });

  const loadFunnels = useCallback(async () => {
    try {
      setLoading(true);
      const res = await api.funnels.list();
      setFunnels(res.data?.data || []);
    } catch (err) {
      console.error('Failed to load funnels:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadFunnels(); }, [loadFunnels]);

  const handleCreate = async () => {
    if (!newFunnel.name.trim()) return;
    try {
      await api.funnels.create(newFunnel);
      setShowCreate(false);
      setNewFunnel({ name: '', description: '', triggerType: 'manual', conversionGoal: '' });
      loadFunnels();
    } catch (err) {
      console.error('Failed to create funnel:', err);
    }
  };

  const handleActivate = async (id: string) => {
    try {
      await api.funnels.activate(id);
      loadFunnels();
    } catch (err: any) {
      alert(err.response?.data?.error || 'Failed to activate funnel');
    }
  };

  const handlePause = async (id: string) => {
    try {
      await api.funnels.pause(id);
      loadFunnels();
    } catch (err) {
      console.error('Failed to pause funnel:', err);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this funnel?')) return;
    try {
      await api.funnels.delete(id);
      loadFunnels();
    } catch (err) {
      console.error('Failed to delete funnel:', err);
    }
  };

  const viewFunnel = async (id: string) => {
    try {
      const res = await api.funnels.get(id);
      setSelectedFunnel(res.data);
    } catch (err) {
      console.error('Failed to load funnel:', err);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Marketing Funnels</h1>
          <p className="text-foreground-secondary mt-1">Create and manage multi-step automated marketing funnels</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-primary-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-primary-500/25 hover:shadow-primary-500/40 transition-all"
        >
          <Plus className="w-4 h-4" /> New Funnel
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {[
          { label: 'Total Funnels', value: funnels.length, icon: <Target className="w-5 h-5 text-primary" /> },
          { label: 'Active', value: funnels.filter(f => f.status === 'active').length, icon: <Play className="w-5 h-5 text-success-fg" /> },
          { label: 'Total Entered', value: funnels.reduce((s, f) => s + f.totalEntered, 0), icon: <Users className="w-5 h-5 text-blue-500" /> },
          { label: 'Total Converted', value: funnels.reduce((s, f) => s + f.totalConverted, 0), icon: <Zap className="w-5 h-5 text-warning-fg" /> },
        ].map((stat) => (
          <div key={stat.label} className="bg-card rounded-xl p-5 border border-border shadow-sm">
            <div className="flex items-center justify-between">
              <span className="text-sm text-foreground-secondary">{stat.label}</span>
              {stat.icon}
            </div>
            <p className="text-2xl font-bold mt-2 text-foreground">{stat.value.toLocaleString()}</p>
          </div>
        ))}
      </div>

      {/* Funnel List */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 border-4 border-primary-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : funnels.length === 0 ? (
        <div className="bg-card rounded-xl border border-border p-12 text-center">
          <Target className="w-12 h-12 text-slate-300 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-foreground">No funnels yet</h3>
          <p className="text-foreground-secondary mt-1">Create your first marketing funnel to automate lead conversion.</p>
          <button onClick={() => setShowCreate(true)} className="mt-4 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors">
            Create Funnel
          </button>
        </div>
      ) : (
        <div className="grid gap-4">
          {funnels.map((funnel) => (
            <div key={funnel.id} className="bg-card rounded-xl border border-border shadow-sm hover:shadow-md transition-shadow p-5">
              <div className="flex items-start justify-between">
                <div className="cursor-pointer flex-1" onClick={() => viewFunnel(funnel.id)}>
                  <div className="flex items-center gap-3">
                    <h3 className="text-lg font-semibold text-foreground">{funnel.name}</h3>
                    <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${statusColors[funnel.status] || statusColors.draft}`}>
                      {funnel.status}
                    </span>
                  </div>
                  {funnel.description && <p className="text-sm text-foreground-secondary mt-1">{funnel.description}</p>}
                  <div className="flex items-center gap-6 mt-3 text-sm text-foreground-secondary">
                    <span>Trigger: <strong className="text-foreground">{funnel.triggerType}</strong></span>
                    <span>Entered: <strong className="text-foreground">{funnel.totalEntered}</strong></span>
                    <span>Converted: <strong className="text-foreground">{funnel.totalConverted}</strong></span>
                    <span>Rate: <strong className="text-foreground">{funnel.conversionRate}%</strong></span>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {funnel.status === 'draft' || funnel.status === 'paused' ? (
                    <button onClick={() => handleActivate(funnel.id)} className="p-2 text-success-fg hover:bg-emerald-50 rounded-lg transition-colors" title="Activate">
                      <Play className="w-4 h-4" />
                    </button>
                  ) : funnel.status === 'active' ? (
                    <button onClick={() => handlePause(funnel.id)} className="p-2 text-warning-fg hover:bg-amber-50 rounded-lg transition-colors" title="Pause">
                      <Pause className="w-4 h-4" />
                    </button>
                  ) : null}
                  <button onClick={() => viewFunnel(funnel.id)} className="p-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors" title="Analytics">
                    <BarChart3 className="w-4 h-4" />
                  </button>
                  <button onClick={() => handleDelete(funnel.id)} className="p-2 text-danger-fg hover:bg-red-50 rounded-lg transition-colors" title="Delete">
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setShowCreate(false)}>
          <div className="bg-card rounded-2xl p-6 w-full max-w-lg shadow-2xl" onClick={e => e.stopPropagation()}>
            <h2 className="text-xl font-bold mb-4">Create Marketing Funnel</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Funnel Name *</label>
                <input type="text" value={newFunnel.name} onChange={e => setNewFunnel({ ...newFunnel, name: e.target.value })}
                  className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" placeholder="e.g., Welcome Series" />
              </div>
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Description</label>
                <textarea value={newFunnel.description} onChange={e => setNewFunnel({ ...newFunnel, description: e.target.value })}
                  className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none" rows={2} placeholder="Brief description..." />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Trigger Type</label>
                  <select value={newFunnel.triggerType} onChange={e => setNewFunnel({ ...newFunnel, triggerType: e.target.value })}
                    className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none">
                    <option value="manual">Manual</option>
                    <option value="form_submit">Form Submission</option>
                    <option value="tag_added">Tag Added</option>
                    <option value="client_created">Client Created</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Conversion Goal</label>
                  <select value={newFunnel.conversionGoal} onChange={e => setNewFunnel({ ...newFunnel, conversionGoal: e.target.value })}
                    className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none">
                    <option value="">Select goal</option>
                    <option value="booking_made">Booking Made</option>
                    <option value="purchase_completed">Purchase Completed</option>
                    <option value="form_submitted">Form Submitted</option>
                  </select>
                </div>
              </div>
            </div>
            <div className="flex gap-3 mt-6">
              <button onClick={() => setShowCreate(false)} className="flex-1 px-4 py-2 border border-border-strong rounded-lg text-foreground hover:bg-accent">Cancel</button>
              <button onClick={handleCreate} className="flex-1 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600">Create Funnel</button>
            </div>
          </div>
        </div>
      )}

      {/* Detail Sidebar */}
      {selectedFunnel && (
        <div className="fixed inset-0 bg-black/50 flex justify-end z-50" onClick={() => setSelectedFunnel(null)}>
          <div className="bg-card w-full max-w-lg h-full overflow-y-auto shadow-2xl" onClick={e => e.stopPropagation()}>
            <div className="p-6 border-b border-border">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-bold">{selectedFunnel.name}</h2>
                <button onClick={() => setSelectedFunnel(null)} className="text-foreground-muted hover:text-foreground-secondary text-xl">&times;</button>
              </div>
              <span className={`inline-block mt-2 text-xs font-medium px-2.5 py-0.5 rounded-full ${statusColors[selectedFunnel.status] || statusColors.draft}`}>
                {selectedFunnel.status}
              </span>
            </div>
            <div className="p-6 space-y-6">
              {/* Stats */}
              <div className="grid grid-cols-3 gap-4">
                <div className="text-center p-3 bg-muted rounded-lg">
                  <p className="text-2xl font-bold text-foreground">{selectedFunnel.totalEntered}</p>
                  <p className="text-xs text-foreground-secondary">Entered</p>
                </div>
                <div className="text-center p-3 bg-muted rounded-lg">
                  <p className="text-2xl font-bold text-foreground">{selectedFunnel.totalConverted}</p>
                  <p className="text-xs text-foreground-secondary">Converted</p>
                </div>
                <div className="text-center p-3 bg-muted rounded-lg">
                  <p className="text-2xl font-bold text-foreground">{selectedFunnel.conversionRate}%</p>
                  <p className="text-xs text-foreground-secondary">Conv. Rate</p>
                </div>
              </div>

              {/* Steps */}
              <div>
                <h3 className="font-semibold text-foreground mb-3">Funnel Steps</h3>
                {selectedFunnel.steps && selectedFunnel.steps.length > 0 ? (
                  <div className="space-y-3">
                    {selectedFunnel.steps.map((step, i) => (
                      <div key={step.id} className="flex items-center gap-3">
                        <div className="flex flex-col items-center">
                          <div className="w-8 h-8 rounded-full bg-brand-subtle flex items-center justify-center text-primary font-semibold text-sm">
                            {i + 1}
                          </div>
                          {i < (selectedFunnel.steps?.length || 0) - 1 && <div className="w-px h-6 bg-primary-200 mt-1" />}
                        </div>
                        <div className="flex-1 bg-muted rounded-lg p-3">
                          <div className="flex items-center gap-2">
                            {stepTypeIcons[step.stepType] || <ArrowRight className="w-4 h-4" />}
                            <span className="font-medium text-sm">{step.name}</span>
                            <span className="text-xs text-foreground-muted ml-auto">{step.stepType}</span>
                          </div>
                          <div className="flex gap-4 mt-1 text-xs text-foreground-secondary">
                            <span>Entered: {step.enteredCount}</span>
                            <span>Completed: {step.completedCount}</span>
                            <span>Drop-off: {step.dropOffRate}%</span>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-foreground-muted">No steps defined yet.</p>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
