"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
  Plus, Trash2, Send, CheckCircle, XCircle, Clock, AlertTriangle,
  RefreshCw, Eye, EyeOff, Copy, ExternalLink, Zap, Activity, Settings,
  ShieldCheck, Terminal, Globe, Share2, Play, Loader2, History
} from 'lucide-react';
import { apiClient as api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';
import { motion, AnimatePresence } from 'framer-motion';

interface WebhookEndpoint {
  id: string;
  name: string;
  url: string;
  events: string[];
  isActive: boolean;
  secret?: string;
  createdAt: string;
  deliveryCount?: number;
  successCount?: number;
  failureCount?: number;
  lastDeliveredAt?: string;
}

interface WebhookDelivery {
  id: string;
  webhookEndpointId: string;
  eventType: string;
  responseStatusCode?: number;
  responseBody?: string;
  requestBody?: string;
  attemptCount: number;
  status: string;
  deliveredAt?: string;
  createdAt: string;
  errorMessage?: string;
}

const EVENT_CATEGORIES: { category: string; events: string[] }[] = [
  { category: 'Booking', events: ['booking.created', 'booking.updated', 'booking.cancelled', 'booking.completed'] },
  { category: 'Client', events: ['client.created', 'client.updated'] },
  { category: 'Payment', events: ['payment.received', 'payment.failed'] },
  { category: 'Invoice', events: ['invoice.created'] },
  { category: 'Staff', events: ['staff.created'] },
  { category: 'Service', events: ['service.created'] },
  { category: 'Reminder', events: ['appointment.reminder'] },
];

const ALL_EVENTS = EVENT_CATEGORIES.flatMap(c => c.events);

function statusColor(status: string) {
  if (status === 'delivered' || status === 'success') return 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20';
  if (status === 'failed') return 'text-red-500 bg-red-500/10 border-red-500/20';
  if (status === 'pending') return 'text-amber-500 bg-amber-500/10 border-amber-500/20';
  return 'text-slate-500 bg-slate-500/10 border-slate-500/20';
}

export default function WebhooksIntegrationsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [endpoints, setEndpoints] = useState<WebhookEndpoint[]>([]);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [showDeliveries, setShowDeliveries] = useState<string | null>(null);
  const [showSecret, setShowSecret] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [testLoading, setTestLoading] = useState<string | null>(null);
  const [form, setForm] = useState({ name: '', url: '', events: [] as string[] });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const fetchEndpoints = useCallback(async () => {
    try {
      setLoading(true);
      const res = await api.get('/api/v1/webhooks/endpoints');
      setEndpoints(res.data?.data || res.data || []);
    } catch {
      toastError('Failed to load webhook endpoints');
    } finally {
      setLoading(false);
    }
  }, [toastError]);

  const fetchDeliveries = useCallback(async (endpointId?: string) => {
    try {
      const url = endpointId
        ? `/api/v1/webhooks/deliveries?endpointId=${endpointId}`
        : '/api/v1/webhooks/deliveries';
      const res = await api.get(url);
      setDeliveries(res.data?.data || res.data || []);
    } catch {
      toastError('Failed to load delivery logs');
    }
  }, [toastError]);

  useEffect(() => { fetchEndpoints(); }, [fetchEndpoints]);

  const validate = () => {
    const errors: Record<string, string> = {};
    if (!form.name.trim()) errors.name = 'Required';
    if (!form.url.trim()) errors.url = 'Required';
    else if (!form.url.startsWith('https://') && !form.url.startsWith('http://')) {
      errors.url = 'Must start with http:// or https://';
    }
    if (form.events.length === 0) errors.events = 'Select at least one event';
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async () => {
    if (!validate()) return;
    try {
      const res = await api.post('/api/v1/webhooks/endpoints', form);
      setEndpoints(prev => [...prev, res.data?.data || res.data]);
      setShowForm(false);
      setForm({ name: '', url: '', events: [] });
      toastSuccess('Endpoint initialized');
    } catch {
      toastError('Failed to create webhook endpoint');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Abort this webhook endpoint?')) return;
    try {
      await api.delete(`/api/v1/webhooks/endpoints/${id}`);
      setEndpoints(prev => prev.filter(e => e.id !== id));
      if (showDeliveries === id) setShowDeliveries(null);
      toastSuccess('Endpoint terminated');
    } catch {
      toastError('Failed to delete endpoint');
    }
  };

  const handleTest = async (endpoint: WebhookEndpoint) => {
    setTestLoading(endpoint.id);
    try {
      await api.post(`/api/v1/webhooks/endpoints/${endpoint.id}/test`);
      toastSuccess(`Test pulse sent to ${endpoint.name}`);
      if (showDeliveries === endpoint.id) {
        await fetchDeliveries(endpoint.id);
      }
    } catch {
      toastError('Test pulse failed');
    } finally {
      setTestLoading(null);
    }
  };

  const handleViewDeliveries = async (endpointId: string) => {
    if (showDeliveries === endpointId) {
      setShowDeliveries(null);
      return;
    }
    setShowDeliveries(endpointId);
    await fetchDeliveries(endpointId);
  };

  const handleResend = async (deliveryId: string) => {
    try {
      await api.post(`/api/v1/webhooks/deliveries/${deliveryId}/resend`);
      toastSuccess('Packet re-transmitted');
      if (showDeliveries) await fetchDeliveries(showDeliveries);
    } catch {
      toastError('Failed to resend webhook');
    }
  };

  const toggleEvent = (event: string) => {
    setForm(prev => ({
      ...prev,
      events: prev.events.includes(event)
        ? prev.events.filter(e => e !== event)
        : [...prev.events, event]
    }));
  };

  const toggleAllEvents = () => {
    setForm(prev => ({
      ...prev,
      events: prev.events.length === ALL_EVENTS.length ? [] : [...ALL_EVENTS]
    }));
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toastSuccess('Cloned to clipboard');
  };

  if (loading && endpoints.length === 0) return (
      <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
          <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
          <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Webhook Nodes...</p>
      </div>
  );

  return (
    <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
      {/* Header Bundle */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
        <div className="flex items-center gap-6">
            <div className="p-4 bg-gradient-to-br from-primary-600 to-indigo-900 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                <Share2 className="h-8 w-8 text-white" />
            </div>
            <div>
                <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Integration Nexus</h1>
                <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Real-time Event Broadcasting Matrix</p>
            </div>
        </div>
        {!showForm && (
            <Button 
              onClick={() => { setShowForm(true); }} 
              className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-3"
            >
              <Plus className="h-4 w-4" /> Initialise Endpoint
            </Button>
        )}
      </div>

      {/* Stats Spectrum */}
      <AnimatePresence>
        {!showForm && endpoints.length > 0 && (
            <motion.div 
                initial={{ opacity: 0, y: -20 }}
                animate={{ opacity: 1, y: 0 }}
                className="grid grid-cols-1 md:grid-cols-3 gap-8"
            >
                {[
                    { label: 'Total Endpoints', value: endpoints.length, icon: Globe, color: 'text-indigo-500', bg: 'bg-indigo-500/10' },
                    { label: 'Active Channels', value: endpoints.filter(e => e.isActive).length, icon: Zap, color: 'text-emerald-500', bg: 'bg-emerald-500/10' },
                    { label: 'Packet Volume', value: endpoints.reduce((sum, e) => sum + (e.deliveryCount || 0), 0).toLocaleString(), icon: Activity, color: 'text-primary-500', bg: 'bg-primary-500/10' }
                ].map((stat, i) => (
                    <div key={i} className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-4 group overflow-hidden relative">
                        <div className="relative z-10 flex items-center justify-between">
                            <div className={cn("p-4 rounded-2xl border", stat.bg, "border-transparent dark:border-white/5")}>
                                <stat.icon className={cn("h-6 w-6", stat.color)} />
                            </div>
                            <span className="text-4xl font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{stat.value}</span>
                        </div>
                        <p className="relative z-10 text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">{stat.label}</p>
                    </div>
                ))}
            </motion.div>
        )}
      </AnimatePresence>

      {/* Initialize Form Corridor */}
      <AnimatePresence>
        {showForm && (
            <motion.div 
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.95 }}
                className="p-10 bg-white dark:bg-slate-900 border border-primary-500/20 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-primary-500/5 dark:shadow-none space-y-12 relative overflow-hidden"
            >
                <div className="flex items-center justify-between relative z-10">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                        <h2 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Neural Endpoint Initialisation</h2>
                    </div>
                    <button onClick={() => setShowForm(false)} className="h-10 w-10 flex items-center justify-center rounded-xl bg-slate-50 dark:bg-slate-950 text-slate-400 hover:text-slate-900 transition-colors">✕</button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-10 relative z-10">
                    <div className="space-y-4">
                        <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Endpoint Alias</label>
                        <Input
                            placeholder="PROD-ENV-LISTENER"
                            value={form.name}
                            onChange={e => setForm(prev => ({ ...prev, name: e.target.value }))}
                            className={cn(
                                "h-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white",
                                formErrors.name && "ring-2 ring-red-500/50"
                            )}
                        />
                    </div>
                    <div className="space-y-4">
                        <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Secure Destination URL</label>
                        <Input
                            placeholder="HTTPS://API.INTEGRATION.ROOT/WEBHOOKS"
                            value={form.url}
                            onChange={e => setForm(prev => ({ ...prev, url: e.target.value }))}
                            className={cn(
                                "h-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white",
                                formErrors.url && "ring-2 ring-red-500/50"
                            )}
                        />
                    </div>
                </div>

                <div className="space-y-8 relative z-10 pt-4">
                    <div className="flex items-center justify-between">
                        <label className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest ml-1">Event Subscription Matrix</label>
                        <button
                            onClick={toggleAllEvents}
                            className="text-[10px] font-black text-primary-500 hover:text-primary-600 uppercase tracking-widest"
                        >
                            {form.events.length === ALL_EVENTS.length ? 'PURGE SELECTION' : 'MAP ALL EVENTS'}
                        </button>
                    </div>
                    
                    <div className="space-y-8 max-h-[400px] overflow-y-auto pr-4 scrollbar-premium">
                        {EVENT_CATEGORIES.map(cat => (
                            <div key={cat.category} className="space-y-4">
                                <p className="text-[9px] font-black text-slate-300 dark:text-slate-700 uppercase tracking-[0.3em] border-b border-slate-50 dark:border-slate-850 pb-2">{cat.category} PROTOCOLS</p>
                                <div className="flex flex-wrap gap-3">
                                    {cat.events.map(event => (
                                        <button
                                            key={event}
                                            onClick={() => toggleEvent(event)}
                                            className={cn(
                                                "px-4 py-2 rounded-xl text-[9px] font-black uppercase tracking-widest transition-all",
                                                form.events.includes(event)
                                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/30'
                                                    : 'bg-white dark:bg-slate-950 text-slate-400 dark:text-slate-700 border border-slate-100 dark:border-slate-850 hover:border-primary-500/40'
                                            )}
                                        >
                                            {event}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="flex gap-6 pt-10 border-t border-slate-50 dark:border-slate-850 relative z-10">
                    <Button onClick={handleCreate} className="h-16 flex-1 rounded-[24px] font-black uppercase tracking-[0.2em] text-[10px] shadow-2xl shadow-primary-500/20 active:scale-95 transition-all">initialise commitment</Button>
                    <Button variant="outline" onClick={() => setShowForm(false)} className="h-16 px-12 rounded-[24px] font-black uppercase tracking-widest text-[10px] text-slate-400">Abort</Button>
                </div>
                
                <Zap className="absolute -bottom-10 -right-10 h-64 w-64 text-primary-500/[0.03] -rotate-12 pointer-events-none" />
            </motion.div>
        )}
      </AnimatePresence>

      {/* Endpoints Matrix Corridor */}
      <div className="space-y-8">
        {endpoints.length === 0 && !showForm ? (
            <div className="p-20 bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/40 dark:shadow-none text-center">
                <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-full inline-block mb-8 shadow-inner">
                    <Zap className="h-12 w-12 text-slate-200 dark:text-slate-800" />
                </div>
                <h3 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Zero Webhook Nodes Detected</h3>
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mt-2 mb-10">No active real-time integration conduits found in the matrix.</p>
                <Button onClick={() => setShowForm(true)} className="h-14 px-12 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30"><Plus className="h-4 w-4 mr-3" /> Initialize Primary Node</Button>
            </div>
        ) : (
            <div className="space-y-8">
                {endpoints.map(endpoint => (
                    <div key={endpoint.id} className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none overflow-hidden group">
                        <div className="p-10">
                            <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-10">
                                <div className="flex-1 min-w-0 space-y-6">
                                    <div className="flex items-center gap-6 flex-wrap">
                                        <div className="p-4 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-transparent dark:border-slate-850 shadow-inner">
                                            <Globe className="h-6 w-6 text-primary-500" />
                                        </div>
                                        <div>
                                            <h3 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">{endpoint.name}</h3>
                                            <span className={cn(
                                                "inline-flex items-center gap-2 px-3 py-1 mt-2 rounded-lg text-[9px] font-black uppercase tracking-widest border",
                                                endpoint.isActive 
                                                    ? 'bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20' 
                                                    : 'bg-slate-100 dark:bg-slate-800 text-slate-400 dark:text-slate-600 border-slate-200 dark:border-slate-700'
                                            )}>
                                                {endpoint.isActive ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                                                {endpoint.isActive ? 'Neural Link Active' : 'Node Offline'}
                                            </span>
                                        </div>
                                    </div>
                                    
                                    <div className="flex items-center gap-4 bg-slate-50 dark:bg-slate-950 p-4 rounded-2xl border border-transparent dark:border-slate-850 shadow-inner group-hover:bg-white dark:group-hover:bg-slate-900 transition-colors">
                                        <code className="text-[10px] font-black text-slate-400 dark:text-slate-600 tracking-tighter truncate max-w-lg uppercase">{endpoint.url}</code>
                                        <button onClick={() => copyToClipboard(endpoint.url)} className="p-2 transition-colors text-slate-300 hover:text-primary-500">
                                            <Copy className="h-4 w-4" />
                                        </button>
                                    </div>

                                    {/* Events Spectrum */}
                                    <div className="flex flex-wrap gap-2">
                                        {(endpoint.events || []).slice(0, 10).map(event => (
                                            <span key={event} className="px-3 py-1 rounded-lg text-[8px] font-black uppercase tracking-widest bg-slate-100 dark:bg-slate-850 text-slate-500 dark:text-slate-500 border border-transparent dark:border-slate-800">{event}</span>
                                        ))}
                                        {(endpoint.events || []).length > 10 && (
                                            <span className="px-3 py-1 rounded-lg text-[8px] font-black uppercase tracking-widest bg-primary-500 text-white shadow-lg shadow-primary-500/20">+{(endpoint.events || []).length - 10} MORE</span>
                                        )}
                                    </div>

                                    {/* Hidden Secret Matrix */}
                                    {endpoint.secret && (
                                        <div className="flex items-center gap-4 p-4 bg-slate-50/50 dark:bg-slate-950/20 rounded-2xl border border-transparent dark:border-slate-850">
                                            <ShieldCheck className="h-4 w-4 text-emerald-500" />
                                            <span className="text-[9px] font-black text-slate-400 uppercase tracking-widest">Secret Hash:</span>
                                            <code className="text-[10px] font-black font-mono text-slate-900 dark:text-white tracking-widest">
                                                {showSecret === endpoint.id ? endpoint.secret : '••••••••••••••••••••••••'}
                                            </code>
                                            <div className="flex gap-2 ml-auto">
                                                <button onClick={() => setShowSecret(showSecret === endpoint.id ? null : endpoint.id)} className="p-2 text-slate-400 hover:text-primary-500 transition-colors">
                                                    {showSecret === endpoint.id ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                                </button>
                                                <button onClick={() => copyToClipboard(endpoint.secret!)} className="p-2 text-slate-400 hover:text-primary-500 transition-colors">
                                                    <Copy className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </div>
                                    )}

                                    {/* Telemetry Stats */}
                                    {endpoint.deliveryCount !== undefined && (
                                        <div className="flex flex-wrap items-center gap-8 pt-4 border-t border-slate-50 dark:border-slate-850">
                                            <div className="flex items-center gap-3">
                                                <Activity className="h-4 w-4 text-primary-500" />
                                                <span className="text-[10px] font-black text-slate-400 uppercase tracking-widest tabular-nums">{endpoint.deliveryCount} DELIVERIES</span>
                                            </div>
                                            {endpoint.successCount !== undefined && (
                                                <div className="flex items-center gap-3">
                                                    <div className="w-2 h-2 rounded-full bg-emerald-500" />
                                                    <span className="text-[10px] font-black text-emerald-500 uppercase tracking-widest tabular-nums">{endpoint.successCount} SYNCED</span>
                                                </div>
                                            )}
                                            {endpoint.failureCount !== undefined && endpoint.failureCount > 0 && (
                                                <div className="flex items-center gap-3">
                                                    <div className="w-2 h-2 rounded-full bg-red-500" />
                                                    <span className="text-[10px] font-black text-red-500 uppercase tracking-widest tabular-nums">{endpoint.failureCount} DROPPED</span>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>

                                <div className="flex flex-row lg:flex-col gap-4 self-stretch lg:self-center border-t lg:border-t-0 lg:border-l border-slate-50 dark:border-slate-850 pt-8 lg:pt-0 lg:pl-10">
                                    <Button
                                        onClick={() => handleTest(endpoint)}
                                        disabled={testLoading === endpoint.id}
                                        className="h-14 flex-1 lg:w-40 rounded-2xl bg-white dark:bg-slate-900 text-primary-600 dark:text-primary-400 border border-slate-100 dark:border-slate-800 shadow-xl hover:bg-primary-50 transition-all disabled:opacity-50 font-black uppercase tracking-widest text-[10px]"
                                    >
                                        {testLoading === endpoint.id ? (
                                            <RefreshCw className="h-4 w-4 animate-spin" />
                                        ) : (
                                            <Zap className="h-4 w-4 mr-2" />
                                        )}
                                        TEST PULSE
                                    </Button>
                                    <Button
                                        onClick={() => handleViewDeliveries(endpoint.id)}
                                        className="h-14 flex-1 lg:w-40 rounded-2xl bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border border-slate-100 dark:border-slate-800 shadow-xl hover:bg-slate-50 transition-all font-black uppercase tracking-widest text-[10px]"
                                    >
                                        <History className="h-4 w-4 mr-2" /> RECENT LOGS
                                    </Button>
                                    <Button
                                        onClick={() => handleDelete(endpoint.id)}
                                        className="h-14 w-14 rounded-2xl bg-red-50 dark:bg-red-950/20 text-red-500 hover:bg-red-500 hover:text-white transition-all shadow-xl"
                                        variant="ghost"
                                    >
                                        <Trash2 className="h-5 w-5" />
                                    </Button>
                                </div>
                            </div>
                        </div>

                        {/* Logs Corridor */}
                        <AnimatePresence>
                            {showDeliveries === endpoint.id && (
                                <motion.div 
                                    initial={{ opacity: 0, height: 0 }}
                                    animate={{ opacity: 1, height: 'auto' }}
                                    exit={{ opacity: 0, height: 0 }}
                                    className="border-t border-slate-50 dark:border-slate-850 bg-slate-50/50 dark:bg-slate-950/50 p-10 overflow-hidden"
                                >
                                    <div className="flex items-center justify-between mb-8">
                                        <div className="flex items-center gap-4">
                                            <div className="p-3 bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-slate-100 dark:border-slate-800">
                                                <Activity className="h-4 w-4 text-primary-500" />
                                            </div>
                                            <h4 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Temporal Packet Flux</h4>
                                        </div>
                                        <button onClick={() => fetchDeliveries(endpoint.id)} className="p-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 text-slate-400 hover:text-primary-500 transition-all">
                                            <RefreshCw className="h-4 w-4" />
                                        </button>
                                    </div>

                                    {deliveries.filter(d => d.webhookEndpointId === endpoint.id).length === 0 ? (
                                        <div className="text-center py-20 bg-white/50 dark:bg-slate-900/50 rounded-[32px] border border-dashed border-slate-200 dark:border-slate-800">
                                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Atmosphere clear. No packet collisions detected.</p>
                                        </div>
                                    ) : (
                                        <div className="space-y-4 max-h-[500px] overflow-y-auto pr-4 scrollbar-premium">
                                            {deliveries.filter(d => d.webhookEndpointId === endpoint.id).map(delivery => (
                                                <div key={delivery.id} className="group/delivery bg-white dark:bg-slate-900 rounded-[28px] border border-slate-100 dark:border-slate-800 p-8 hover:shadow-xl transition-all relative overflow-hidden">
                                                    <div className="flex flex-col md:flex-row items-center justify-between gap-6 relative z-10">
                                                        <div className="flex items-center gap-6">
                                                            <div className={cn(
                                                                "h-12 px-6 rounded-xl flex items-center justify-center font-black text-[9px] uppercase tracking-[0.2em] border",
                                                                statusColor(delivery.status)
                                                            )}>
                                                                {delivery.status}
                                                            </div>
                                                            <div>
                                                                <p className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-widest mb-1.5">{delivery.eventType}</p>
                                                                <div className="flex items-center gap-4">
                                                                    {delivery.responseStatusCode && (
                                                                        <span className={cn(
                                                                            "text-[9px] font-black tabular-nums tracking-widest",
                                                                            delivery.responseStatusCode < 300 ? 'text-emerald-500' : 'text-red-500'
                                                                        )}>
                                                                            HTTP {delivery.responseStatusCode}
                                                                        </span>
                                                                    )}
                                                                    <span className="text-[8px] font-black text-slate-400 uppercase tracking-widest">Attempt {delivery.attemptCount} :: {new Date(delivery.createdAt).toLocaleTimeString()}</span>
                                                                </div>
                                                            </div>
                                                        </div>
                                                        
                                                        {delivery.status === 'failed' && (
                                                            <button
                                                                onClick={() => handleResend(delivery.id)}
                                                                className="h-10 px-6 rounded-xl bg-primary-500 text-white text-[9px] font-black uppercase tracking-widest shadow-lg shadow-primary-500/20 hover:scale-105 active:scale-95 transition-all flex items-center gap-2"
                                                            >
                                                                <RefreshCw className="h-3.5 w-3.5" /> RE-TRANSMIT
                                                            </button>
                                                        )}
                                                    </div>
                                                    {delivery.errorMessage && (
                                                        <div className="mt-6 p-6 bg-red-50 dark:bg-red-950/20 rounded-2xl border border-red-100 dark:border-red-900/30">
                                                            <p className="text-[10px] font-mono font-bold text-red-600 dark:text-red-400 tracking-tight leading-relaxed">{delivery.errorMessage}</p>
                                                        </div>
                                                    )}
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </motion.div>
                            )}
                        </AnimatePresence>
                    </div>
                ))}
            </div>
        )}
      </div>

      {/* Protocol Documentation Nexus */}
      <div className="p-10 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl relative overflow-hidden group">
        <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
            <div className="p-6 bg-slate-800 rounded-[32px] border border-slate-700 shadow-inner group-hover:rotate-12 transition-transform duration-1000">
                <Terminal className="h-10 w-10 text-primary-400" />
            </div>
            <div className="flex-1 space-y-4">
                <h3 className="text-xl font-black text-white uppercase tracking-tight">Security Protocol: VER-SIG-V1</h3>
                <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest leading-relaxed">
                    Verify all inbound neural bursts via the <code className="bg-white/5 border border-white/10 rounded-lg px-2 py-0.5 text-primary-400">X-UPKILO-SIGNATURE</code> header. Prevent replay-collisions by validating the HMAC-SHA256 digest.
                </p>
                <div className="flex flex-wrap gap-4 pt-4">
                    <Button variant="outline" className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-primary-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700">
                        <Terminal className="h-4 w-4 mr-2" /> Source Node Example
                    </Button>
                    <Button variant="outline" className="h-12 px-8 rounded-xl bg-slate-800 border-slate-700 text-slate-400 font-black uppercase tracking-widest text-[9px] hover:bg-slate-700">
                        <ExternalLink className="h-4 w-4 mr-2" /> Global API Specs
                    </Button>
                </div>
            </div>
        </div>
        <div className="absolute top-0 right-0 w-80 h-80 bg-primary-500/5 blur-3xl rounded-full" />
      </div>
    </div>
  );
}

