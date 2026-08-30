"use client";

import React, { useState, useEffect } from "react";
import { 
  Code, Key, Activity, BookOpen, 
  RefreshCw, PlayCircle, ShieldCheck, Database, Loader2,
  Terminal, Shield, Zap, ExternalLink, Copy, Trash2, Plus,
  ChevronRight, Laptop, Server, Globe
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import api, { apiClient } from "@/lib/api";
import { cn } from "@/lib/utils";
import { motion, AnimatePresence } from "framer-motion";

export default function DevelopersDashboardPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [loading, setLoading] = useState(true);
  const [provisioning, setProvisioning] = useState(false);
  const [creatingKey, setCreatingKey] = useState(false);

  const [apiKeys, setApiKeys] = useState<any[]>([]);
  const [usageData, setUsageData] = useState<any>(null);

  useEffect(() => {
    fetchDeveloperData();
  }, []);

  const fetchDeveloperData = async () => {
    setLoading(true);
    try {
      const keysRes = await apiClient.get('/api/api-keys');
      const keysList = keysRes.data?.data || [];
      setApiKeys(keysList);

      if (keysList.length > 0) {
        const usageRes = await apiClient.get(`/api/api-keys/${keysList[0].id}/usage?period=7d`);
        setUsageData(usageRes.data);
      } else {
        setUsageData({ totalRequests: 0, averageLatency: 0 });
      }
    } catch (err: any) {
      console.error("Failed to load developer data", err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSandbox = async () => {
    setProvisioning(true);
    try {
      await apiClient.post('/api/v1/sandbox');
      toastSuccess("Sandbox Environment Provisioned. Deployment active.");
    } catch (err) {
      toastError("Failed to clone sandbox environment.");
    } finally {
      setProvisioning(false);
    }
  };

  const revokeKey = async (id: string) => {
    if (!confirm('Abort this access token? This action is irreversible.')) return;
    try {
      await apiClient.delete(`/api/api-keys/${id}`);
      toastSuccess("Access Token Terminated.");
      await fetchDeveloperData();
    } catch(err) {
      toastError("Failed to revoke API key.");
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toastSuccess("Cloned to clipboard.");
  };

  if (loading) {
    return (
        <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
            <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
            <p className="text-[10px] font-black uppercase tracking-[0.4em] text-foreground-secondary">Syncing Developer Nexus...</p>
        </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
      {/* Header Bundle */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
        <div className="flex items-center gap-6">
            <div className="p-4 bg-gradient-to-br from-primary-600 to-slate-900 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                <Terminal className="h-8 w-8 text-white" />
            </div>
            <div>
                <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Developer Nexus</h1>
                <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Direct System Access and API Protocol Interface</p>
            </div>
        </div>
        <div className="flex items-center gap-4">
            <div className="flex -space-x-3">
                {[1,2,3].map(i => (
                    <div key={i} className="w-10 h-10 rounded-full bg-slate-100 dark:bg-slate-800 border-2 border-white dark:border-slate-950 flex items-center justify-center shadow-lg">
                        <Shield className="h-4 w-4 text-foreground-muted" />
                    </div>
                ))}
            </div>
            <span className="text-[9px] font-black text-foreground-muted uppercase tracking-widest ml-2">Shield Status: Optimal</span>
        </div>
      </div>

      {/* Analytics Suite */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10 relative overflow-hidden">
            <div className="flex items-center justify-between relative z-10">
                <div className="flex items-center gap-4">
                    <Activity className="h-5 w-5 text-primary-500" />
                    <h2 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Temporal Protocol Telemetry</h2>
                </div>
                <span className="text-[9px] font-black text-foreground-muted uppercase tracking-widest">Period: 7 Lifecycle Cycles</span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-8 relative z-10">
                {[
                    { label: 'Total Requests', value: usageData?.totalRequests?.toLocaleString() || 0, icon: Globe, color: 'text-primary-500', trend: '+12.4%' },
                    { label: 'Avg Latency', value: `${usageData?.averageLatency || 0}ms`, icon: Zap, color: 'text-success-fg', trend: '-2ms' },
                    { label: 'Active Tokens', value: apiKeys.length, icon: Key, color: 'text-primary-500', trend: 'Stable' }
                ].map((stat, i) => (
                    <div key={i} className="p-6 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner group">
                        <div className="flex items-center justify-between mb-4">
                            <stat.icon className={cn("h-5 w-5", stat.color)} />
                            <span className="text-[9px] font-black text-success-fg">{stat.trend}</span>
                        </div>
                        <p className="text-3xl font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{stat.value}</p>
                        <p className="text-[9px] font-black text-foreground-muted uppercase tracking-widest mt-2">{stat.label}</p>
                    </div>
                ))}
            </div>
            
            <div className="absolute top-0 right-0 w-64 h-64 bg-primary-500/5 blur-3xl rounded-full" />
        </div>

        <div className="p-10 bg-gradient-to-br from-primary-900 to-slate-950 border border-slate-800 rounded-[40px] shadow-2xl space-y-8 relative overflow-hidden group">
            <div className="relative z-10 flex flex-col h-full justify-between">
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <Database className="h-5 w-5 text-primary-400" />
                        <h3 className="text-[10px] font-black text-white uppercase tracking-[0.3em]">Isolated Sandbox</h3>
                    </div>
                    <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest leading-relaxed">
                        Clone your primary environment node into an ephemeral container for secure protocol testing.
                    </p>
                </div>
                
                <Button 
                    onClick={handleCreateSandbox} 
                    loading={provisioning}
                    className="h-14 mt-8 w-full bg-white text-primary-900 rounded-2xl font-black uppercase tracking-widest text-[10px] hover:bg-slate-100 transition-all active:scale-95 shadow-xl"
                >
                    <RefreshCw className={cn("h-4 w-4 mr-3", provisioning && "animate-spin")} /> Deploy Sandbox Node
                </Button>
            </div>
            <PlayCircle className="absolute -bottom-10 -right-10 h-48 w-48 text-white/[0.03] -rotate-12 pointer-events-none" />
        </div>
      </div>

      {/* Access Tokens Matrix */}
      <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-10">
        <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
                <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg" />
                <h2 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Authorization Matrix</h2>
            </div>
            <Button variant="outline" className="h-12 px-6 rounded-2xl font-black uppercase tracking-widest text-[9px] border-slate-100 dark:border-slate-800 text-foreground-secondary hover:text-primary-500">
                <Plus className="h-4 w-4 mr-2" /> Initialize Token
            </Button>
        </div>

        <div className="space-y-6">
            {apiKeys.length === 0 ? (
                <div className="p-20 text-center border-2 border-dashed border-slate-100 dark:border-slate-850 rounded-[32px]">
                    <p className="text-[10px] font-black text-foreground-muted uppercase tracking-widest">Zero Active Tokens Detected in the Matrix</p>
                </div>
            ) : (
                apiKeys.map(key => (
                    <div key={key.id} className="group p-8 bg-slate-50 dark:bg-slate-950 rounded-[32px] border border-transparent hover:border-primary-500/20 transition-all flex flex-col md:flex-row items-center justify-between gap-8 relative overflow-hidden">
                        <div className="flex items-center gap-8 relative z-10 w-full md:w-auto">
                            <div className={cn(
                                "p-4 rounded-2xl shadow-inner border",
                                key.isActive ? "bg-white dark:bg-slate-900 border-emerald-500/10" : "bg-slate-100 dark:bg-slate-800 border-slate-200 dark:border-slate-700"
                            )}>
                                <Key className={cn("h-6 w-6", key.isActive ? "text-success-fg" : "text-foreground-muted")} />
                            </div>
                            <div className="space-y-2">
                                <div className="flex items-center gap-3">
                                    <h4 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">{key.name}</h4>
                                    <span className={cn(
                                        "px-2 py-0.5 rounded text-[8px] font-black uppercase tracking-widest border",
                                        key.isActive 
                                            ? "bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-500/20" 
                                            : "bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-400 border-red-100 dark:border-red-500/20"
                                    )}>
                                        {key.isActive ? "Active Path" : "Revoked"}
                                    </span>
                                </div>
                                <div className="flex items-center gap-3 bg-white dark:bg-slate-900 px-3 py-1.5 rounded-xl border border-slate-100 dark:border-slate-850 shadow-sm">
                                    <code className="text-[10px] font-black text-foreground-muted tracking-widest">
                                        {key.prefix}••••••••••••••••••••••••••••{key.lastFourChars}
                                    </code>
                                    <button onClick={() => copyToClipboard(`${key.prefix}...${key.lastFourChars}`)} className="p-1 hover:text-primary-500 transition-colors">
                                        <Copy className="h-3.5 w-3.5" />
                                    </button>
                                </div>
                                <p className="text-[9px] font-black text-foreground-muted uppercase tracking-widest pl-1">Born: {new Date(key.createdAt).toLocaleDateString()}</p>
                            </div>
                        </div>

                        <div className="flex items-center gap-4 relative z-10 w-full md:w-auto justify-end">
                            {key.isActive && (
                                <button 
                                    onClick={() => revokeKey(key.id)}
                                    className="h-12 px-6 rounded-xl bg-red-50 dark:bg-red-950/20 text-red-500 hover:bg-red-500 hover:text-white text-[9px] font-black uppercase tracking-widest transition-all"
                                >
                                    Terminate
                                </button>
                            )}
                            <button className="h-12 w-12 rounded-xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center hover:bg-primary-500 hover:text-white transition-all">
                                <Activity className="h-4 w-4" />
                            </button>
                        </div>
                        
                        <div className="absolute top-0 left-0 h-full w-1 bg-primary-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                    </div>
                ))
            )}
        </div>
      </div>

      {/* Protocol Resources */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {[
            { title: 'Interactive API Docs', desc: 'SWA-GER Spec v2.0 // REST Architecture', icon: Laptop, color: 'text-primary-400' },
            { title: 'Node.js Core SDK', desc: 'npm install @upkilo/node-protocol', icon: Server, color: 'text-emerald-400' }
        ].map((res, i) => (
            <button key={i} className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl hover:border-primary-500/20 transition-all text-left flex items-center justify-between group">
                <div className="flex items-center gap-6">
                    <div className="p-4 bg-slate-50 dark:bg-slate-950 rounded-[24px] border border-transparent dark:border-slate-850 shadow-inner group-hover:scale-110 transition-transform">
                        <res.icon className={cn("h-6 w-6", res.color)} />
                    </div>
                    <div>
                        <h4 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">{res.title}</h4>
                        <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-1">{res.desc}</p>
                    </div>
                </div>
                <ExternalLink className="h-4 w-4 text-slate-300 group-hover:text-primary-500 group-hover:translate-x-1 transition-all" />
            </button>
        ))}
      </div>

      {/* Bottom Footer Protocol */}
      <div className="p-8 bg-blue-50 dark:bg-primary-900/10 border border-blue-100 dark:border-primary-500/20 rounded-[32px] flex items-center gap-8 group">
          <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-lg border border-blue-100 dark:border-primary-500/30 group-hover:scale-110 transition-transform">
              <ShieldCheck className="h-8 w-8 text-blue-600 dark:text-primary-400" />
          </div>
          <div>
              <h3 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.4em]">Developer Security Shield</h3>
              <p className="text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-[0.2em] mt-2 leading-loose">
                  All API requests are audited and rate-limited via our global traffic-shaping layer. <span className="text-primary-600 dark:text-primary-400 underline font-black">Audit Logs</span> are retained for 30 cycles.
              </p>
          </div>
      </div>
    </div>
  );
}

