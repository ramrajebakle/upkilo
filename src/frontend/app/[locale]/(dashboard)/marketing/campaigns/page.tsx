"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  Plus, Search, Megaphone, Send, Clock, BarChart2, Mail, MessageSquare,
  Play, Pause, Trash2, Eye, TrendingUp, MousePointer, ArrowRight,
  CheckCircle, AlertCircle, RefreshCw, ExternalLink, Activity, Globe,
  Target, Zap, Layers, ChevronRight, ShieldCheck, PieChart, Loader2
} from "lucide-react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";
import { useToast } from "@/components/ui/Toast";
import { motion, AnimatePresence } from "framer-motion";

interface Campaign {
  id: string;
  name: string;
  type: 'Email' | 'SMS' | 'Push' | 'WhatsApp';
  status: 'draft' | 'scheduled' | 'sent' | 'active' | 'paused' | 'failed';
  sentAt?: string;
  scheduledAt?: string;
  recipients: number;
  delivered: number;
  opened: number;
  clicked: number;
  replied: number;
  unsubscribed: number;
  openRate: number;
  clickRate: number;
  replyRate: number;
  subject?: string;
  createdAt: string;
}

interface CampaignStats {
  totalSent: number;
  avgOpenRate: number;
  avgClickRate: number;
  totalRevenue: number;
}

export default function CampaignsPage() {
  const router = useRouter();
  const { success: toastSuccess, error: toastError } = useToast();
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [stats, setStats] = useState<CampaignStats>({ totalSent: 0, avgOpenRate: 0, avgClickRate: 0, totalRevenue: 0 });
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");

  const fetchCampaigns = useCallback(async () => {
    try {
      setLoading(true);
      const [campsRes, statsRes] = await Promise.all([
        apiClient.get('/api/v1/campaigns').catch(() => ({ data: { data: [] } })),
        apiClient.get('/api/v1/campaigns/stats').catch(() => ({ data: {} })),
      ]);

      const campsData = campsRes.data?.data || campsRes.data || [];
      setCampaigns(Array.isArray(campsData) ? campsData : []);

      const s = statsRes.data?.data || statsRes.data || {};
      setStats({
        totalSent: s.totalSent || 0,
        avgOpenRate: s.avgOpenRate || 0,
        avgClickRate: s.avgClickRate || 0,
        totalRevenue: s.totalRevenue || 0,
      });
    } catch {
      toastError('Failed to load campaign telemetry');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchCampaigns(); }, [fetchCampaigns]);

  const handleToggleStatus = async (campaign: Campaign) => {
    const newStatus = campaign.status === 'active' ? 'paused' : 'active';
    try {
      await apiClient.patch(`/api/v1/campaigns/${campaign.id}/status`, { status: newStatus });
      setCampaigns(prev => prev.map(c => c.id === campaign.id ? { ...c, status: newStatus as Campaign['status'] } : c));
      toastSuccess(`Campaign protocol ${newStatus}`);
    } catch {
      toastError('Failed to update protocol status');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Abort this campaign protocol?')) return;
    try {
      await apiClient.delete(`/api/v1/campaigns/${id}`);
      setCampaigns(prev => prev.filter(c => c.id !== id));
      toastSuccess('Campaign protocol purged.');
    } catch {
      toastError('Purge failed.');
    }
  };

  const filtered = campaigns.filter(c => {
    const matchSearch = !search || (c.name || '').toLowerCase().includes(search.toLowerCase()) || (c.subject || '').toLowerCase().includes(search.toLowerCase());
    const matchStatus = statusFilter === 'all' || c.status === statusFilter;
    const matchType = typeFilter === 'all' || c.type === typeFilter;
    return matchSearch && matchStatus && matchType;
  });

  const getStatusStyles = (status: string) => {
    switch (status) {
      case 'active': return 'bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-500/20';
      case 'sent': return 'bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-100 dark:border-blue-500/20';
      case 'scheduled': return 'bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-100 dark:border-amber-500/20';
      case 'paused': return 'bg-orange-50 dark:bg-orange-500/10 text-orange-600 dark:text-orange-400 border-orange-100 dark:border-orange-500/20';
      case 'failed': return 'bg-rose-50 dark:bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-500/20';
      default: return 'bg-slate-50 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-200 dark:border-slate-850';
    }
  };

  const getTypeIcon = (type: string) => {
      switch(type) {
          case 'Email': return <Mail className="h-4 w-4" />;
          case 'SMS': return <MessageSquare className="h-4 w-4" />;
          case 'Push': return <Send className="h-4 w-4" />;
          default: return <Megaphone className="h-4 w-4" />;
      }
  };

  if (loading && campaigns.length === 0) return (
    <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
        <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
        <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Broadcast Tunnels...</p>
    </div>
  );

  return (
    <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
      {/* Header Bundle */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
        <div className="flex items-center gap-6">
            <div className="p-4 bg-gradient-to-br from-primary-600 to-primary-950 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                <Megaphone className="h-8 w-8 text-white" />
            </div>
            <div>
                <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Broadcast Nexus</h1>
                <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Multi-Channel Audience Engagement and Protocol Broadcast</p>
            </div>
        </div>
        <div className="flex items-center gap-4">
            <button onClick={fetchCampaigns} className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-900 border border-transparent dark:border-slate-850 text-slate-400 hover:text-primary-500 transition-all shadow-inner">
                <RefreshCw className={cn("h-5 w-5", loading && "animate-spin")} />
            </button>
            <Button onClick={() => router.push('/marketing/campaigns/new')} className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-3 bg-primary-600 hover:bg-primary-700">
                <Plus className="h-4 w-4" /> Initialize Campaign
            </Button>
        </div>
      </div>

      {/* Campaign Telemetry Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
          {[
              { label: 'Cumulative Reach', value: stats.totalSent?.toLocaleString() || '0', icon: Send, color: 'text-blue-500', trend: '+18.5%' },
              { label: 'Density Yield (Open)', value: `${stats.avgOpenRate?.toFixed(1) || '0.0'}%`, icon: Eye, color: 'text-emerald-500', trend: 'Optimal' },
              { label: 'Interaction Delta', value: `${stats.avgClickRate?.toFixed(1) || '0.0'}%`, icon: MousePointer, color: 'text-primary-500', trend: 'Stable' },
              { label: 'Scheduled Nodes', value: campaigns.filter(c => c.status === 'scheduled').length, icon: Clock, color: 'text-amber-500', trend: 'Ready' }
          ].map((stat, i) => (
              <div key={i} className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-4 group overflow-hidden relative">
                  <div className="relative z-10 flex items-center justify-between">
                      <div className="p-4 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-transparent dark:border-slate-850 shadow-inner group-hover:scale-110 transition-transform">
                          <stat.icon className={cn("h-6 w-6", stat.color)} />
                      </div>
                      <span className="text-[9px] font-black text-emerald-500 uppercase tracking-widest">{stat.trend}</span>
                  </div>
                  <div className="relative z-10 space-y-1">
                      <p className="text-3xl font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{stat.value}</p>
                      <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">{stat.label}</p>
                  </div>
              </div>
          ))}
      </div>

      {/* Matrix Overlays (Filters) */}
      <div className="flex flex-col lg:flex-row gap-6 items-center">
          <div className="relative flex-1 w-full group">
              <Search className="absolute left-6 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-300 dark:text-slate-700 group-focus-within:text-primary-500 transition-colors" />
              <input
                  type="text"
                  placeholder="SEARCH CAMPAIGN NODES OR SUBJECT HASHES..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="w-full h-16 pl-16 pr-6 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[28px] text-xs font-black uppercase tracking-widest dark:text-white outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all shadow-xl"
              />
          </div>
          <div className="flex gap-4 w-full lg:w-auto overflow-x-auto pb-4 lg:pb-0">
              <select
                value={statusFilter}
                onChange={e => setStatusFilter(e.target.value)}
                className="h-16 px-6 bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 rounded-[24px] text-[10px] font-black uppercase tracking-widest dark:text-white outline-none focus:border-primary-500"
              >
                  <option value="all">ALL STATUS</option>
                  <option value="draft">DRAFT</option>
                  <option value="scheduled">SCHEDULED</option>
                  <option value="sent">SENT</option>
                  <option value="active">ACTIVE</option>
                  <option value="paused">PAUSED</option>
              </select>
              <select
                value={typeFilter}
                onChange={e => setTypeFilter(e.target.value)}
                className="h-16 px-6 bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 rounded-[24px] text-[10px] font-black uppercase tracking-widest dark:text-white outline-none focus:border-primary-500"
              >
                  <option value="all">ALL CHANNELS</option>
                  <option value="Email">EMAIL TUNNEL</option>
                  <option value="SMS">SMS PROTOCOL</option>
                  <option value="Push">PUSH OVERLAY</option>
              </select>
          </div>
      </div>

      {/* Campaign Ledger (The List) */}
      <div className="space-y-6">
          {filtered.length === 0 ? (
              <div className="p-20 text-center bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl">
                  <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-full inline-block mb-8">
                      <Megaphone className="h-14 w-14 text-slate-200 dark:text-slate-800" />
                  </div>
                  <h3 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Zero Active Broadcasts</h3>
                  <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mt-4">The broadcast nexus is currently silent. Initialize a new campaign vector to engage your audience.</p>
              </div>
          ) : (
              filtered.map((campaign, i) => (
                  <div key={campaign.id} className="group p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none transition-all hover:border-primary-500/20 relative overflow-hidden">
                      <div className="grid grid-cols-1 xl:grid-cols-12 gap-10 items-center relative z-10">
                          {/* Profile / Basic Info */}
                          <div className="xl:col-span-4 flex items-center gap-8">
                              <div className={cn(
                                  "w-16 h-16 rounded-[24px] flex items-center justify-center text-white shadow-xl transition-all group-hover:scale-110",
                                  campaign.type === 'Email' ? 'bg-primary-600' : campaign.type === 'SMS' ? 'bg-primary-600' : 'bg-blue-600'
                              )}>
                                  {getTypeIcon(campaign.type)}
                              </div>
                              <div className="space-y-1">
                                  <h4 
                                    onClick={() => router.push(`/marketing/campaigns/${campaign.id}/analytics`)}
                                    className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight cursor-pointer hover:text-primary-500 transition-colors"
                                  >
                                      {campaign.name}
                                  </h4>
                                  <div className="flex items-center gap-3">
                                      <span className={cn("px-2.5 py-0.5 rounded-lg text-[8px] font-black uppercase tracking-widest border", getStatusStyles(campaign.status))}>
                                          {campaign.status}
                                      </span>
                                      <p className="text-[9px] font-black text-slate-400 uppercase tracking-widest">
                                          {campaign.sentAt ? `SENT ${new Date(campaign.sentAt).toLocaleDateString()}` : `CREATED ${new Date(campaign.createdAt).toLocaleDateString()}`}
                                      </p>
                                  </div>
                              </div>
                          </div>

                          {/* Engagement Matrix */}
                          <div className="xl:col-span-5 grid grid-cols-2 md:grid-cols-4 gap-4 px-6 xl:border-x border-slate-50 dark:border-slate-850">
                              {[
                                  { label: 'REACH', value: campaign.recipients || '0', icon: Send },
                                  { label: 'OPENED', value: `${campaign.openRate?.toFixed(1) || '0.0'}%`, icon: Eye },
                                  { label: 'CLOURED', value: `${campaign.clickRate?.toFixed(1) || '0.0'}%`, icon: MousePointer },
                                  { label: 'REPLIED', value: `${campaign.replyRate?.toFixed(1) || '0.0'}%`, icon: Activity }
                              ].map((m, idx) => (
                                  <div key={idx} className="text-center xl:text-left space-y-1">
                                      <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">{m.label}</p>
                                      <p className="text-sm font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{m.value}</p>
                                  </div>
                              ))}
                          </div>

                          {/* Action Hub */}
                          <div className="xl:col-span-3 flex items-center justify-end gap-3">
                              <button 
                                onClick={() => router.push(`/marketing/campaigns/${campaign.id}/analytics`)}
                                className="h-12 w-12 rounded-xl bg-slate-50 dark:bg-slate-950 flex items-center justify-center text-slate-400 hover:text-primary-500 transition-all shadow-inner border border-transparent dark:border-slate-850"
                              >
                                  <BarChart2 className="h-4 w-4" />
                              </button>
                              {(campaign.status === 'active' || campaign.status === 'paused') && (
                                  <button 
                                    onClick={() => handleToggleStatus(campaign)}
                                    className="h-12 px-6 rounded-xl bg-slate-50 dark:bg-slate-950 text-slate-400 hover:text-amber-500 transition-all font-black uppercase tracking-widest text-[9px] border border-transparent dark:border-slate-850"
                                  >
                                      {campaign.status === 'active' ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                                  </button>
                              )}
                              <button 
                                onClick={() => handleDelete(campaign.id)}
                                className="h-12 w-12 rounded-xl bg-red-50 dark:bg-red-950/20 text-red-500 hover:bg-red-500 hover:text-white transition-all shadow-xl"
                              >
                                  <Trash2 className="h-4 w-4" />
                              </button>
                              <ChevronRight className="h-5 w-5 text-slate-200 dark:text-slate-800 ml-2 group-hover:translate-x-1 transition-transform" />
                          </div>
                      </div>
                      
                      <div className="absolute top-0 left-0 h-full w-1.5 bg-primary-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </div>
              ))
          )}
      </div>

      {/* Broadcast Strategic Nexus */}
      <div className="p-10 bg-slate-900 rounded-[40px] border border-slate-800 shadow-2xl relative overflow-hidden group">
          <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
              <div className="p-6 bg-slate-800 rounded-[32px] border border-slate-700 shadow-inner group-hover:rotate-12 transition-transform duration-1000">
                  <Target className="h-10 w-10 text-emerald-400" />
              </div>
              <div className="flex-1 space-y-4">
                  <h3 className="text-xl font-black text-white uppercase tracking-tight">Campaign Intelligence Node</h3>
                  <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest leading-relaxed">
                      Audience density analysis indicates <span className="text-emerald-400">Peak Saturation</span> during the next 48 cycles. Execute <span className="text-primary-400 font-black underline cursor-pointer hover:text-white uppercase">Email Protocols</span> for maximum yield.
                  </p>
                  <div className="flex flex-wrap gap-4 pt-4">
                      <button className="h-12 px-8 rounded-xl bg-white/5 border border-white/10 text-emerald-400 font-black uppercase tracking-widest text-[9px] hover:bg-white/10 flex items-center gap-2">
                          <Activity className="h-4 w-4" /> Engagement Flux
                      </button>
                      <button className="h-12 px-8 rounded-xl bg-white/5 border border-white/10 text-slate-400 font-black uppercase tracking-widest text-[9px] hover:bg-white/10 flex items-center gap-2">
                          <Zap className="h-4 w-4" /> Auto-Scheduler Node
                      </button>
                  </div>
              </div>
          </div>
          <div className="absolute top-0 right-0 w-80 h-80 bg-primary-500/5 blur-3xl rounded-full" />
      </div>
    </div>
  );
}

