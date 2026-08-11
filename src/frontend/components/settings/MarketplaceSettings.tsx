'use client';

import { useState, useEffect } from 'react';
import { 
  ShoppingBag, 
  Search, 
  TrendingUp, 
  Award, 
  CheckCircle2, 
  ExternalLink, 
  DollarSign, 
  Users, 
  BarChart3,
  Loader2,
  AlertCircle,
  Zap,
  Globe
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn, formatCurrency } from '@/lib/utils';

export function MarketplaceSettings() {
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [leadFees, setLeadFees] = useState<any>(null);
    const [listing, setListing] = useState<any>(null);
    const [apps, setApps] = useState<any[]>([]);

    useEffect(() => {
        fetchMarketplaceData();
    }, []);

    const fetchMarketplaceData = async () => {
        setLoading(true);
        try {
            const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null;
            if (!tenantId) return;

            const [feesRes, appsRes] = await Promise.all([
                api.marketplace.getLeadFees(tenantId),
                api.marketplace.getApps()
            ]);

            setLeadFees(feesRes.data);
            setApps(appsRes.data || []);
            
            const bizRes = await api.settings.getBusiness();
            setListing(bizRes.data);
        } catch (err) {
            console.error('Failed to fetch marketplace data:', err);
        } finally {
            setLoading(false);
        }
    };

    const handlePurchaseBadge = async () => {
        const tenantId = typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null;
        if (!tenantId) return;

        setActionLoading('badge');
        try {
            await api.marketplace.purchasePremiumBadge(tenantId);
            await fetchMarketplaceData();
        } catch (err) {
            console.error('Badge purchase error:', err);
        } finally {
            setActionLoading(null);
        }
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center py-24 gap-6">
                <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">Syncing Commerce Nexus...</p>
            </div>
        );
    }

    return (
        <div className="space-y-10 animate-fade-in">
            {/* Visibility Score Card */}
            <div className="grid md:grid-cols-2 gap-8">
                <div className="p-8 bg-gradient-to-br from-primary-600 via-primary-700 to-primary-900 rounded-[32px] text-white shadow-2xl shadow-primary-500/20 overflow-hidden relative group">
                    <div className="relative z-10">
                        <div className="flex items-center gap-4 mb-6">
                            <div className="p-3 bg-white/10 rounded-2xl backdrop-blur-md border border-white/20 shadow-inner group-hover:scale-110 transition-transform">
                                <TrendingUp className="h-6 w-6 text-white" />
                            </div>
                            <div>
                                <h3 className="font-black text-xl uppercase tracking-tight">Marketplace Rank</h3>
                                <p className="text-[10px] font-bold text-primary-100 uppercase tracking-widest mt-0.5">Algorithm Visibility</p>
                            </div>
                        </div>
                        <div className="flex items-baseline gap-3 mb-4">
                            <span className="text-6xl font-black tracking-tighter">{listing?.premiumScore || 0}</span>
                            <span className="text-primary-100 text-xs font-bold uppercase tracking-[0.2em]">Power Index</span>
                        </div>
                        <p className="text-primary-100/80 text-[11px] font-medium leading-relaxed max-w-[240px]">
                            Advanced telemetry indicates your ranking position relative to regional competitors.
                        </p>
                        <div className="mt-8 flex gap-3">
                            <Button 
                                className="bg-white text-primary-700 hover:bg-slate-50 border-none h-11 px-6 rounded-xl font-black uppercase tracking-widest text-[9px] shadow-xl shadow-black/10 active:scale-95 transition-all"
                                onClick={handlePurchaseBadge}
                                loading={actionLoading === 'badge'}
                            >
                                <Award className="h-4 w-4 mr-2" />
                                Premium Badge
                            </Button>
                            <Button className="bg-primary-500/20 text-white hover:bg-primary-500/30 border-white/20 backdrop-blur-md h-11 px-6 rounded-xl font-black uppercase tracking-widest text-[9px] active:scale-95 transition-all">
                                Global Grid
                            </Button>
                        </div>
                    </div>
                    {/* Decorative elements */}
                    <div className="absolute top-0 right-0 -mr-16 -mt-16 w-64 h-64 bg-white/5 rounded-full blur-3xl" />
                    <div className="absolute bottom-0 left-0 -ml-8 -mb-8 w-32 h-32 bg-primary-500/20 rounded-full blur-2xl" />
                    <TrendingUp className="absolute bottom-4 right-4 h-32 w-32 text-white/5 -rotate-12 group-hover:rotate-0 transition-transform duration-700" />
                </div>

                <div className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none flex flex-col justify-between group">
                    <div>
                        <div className="flex items-center justify-between mb-8">
                            <div className="flex items-center gap-4">
                                <div className="p-3 bg-amber-50 dark:bg-amber-400/10 rounded-2xl border border-amber-100 dark:border-amber-400/20 group-hover:scale-110 transition-transform">
                                    <DollarSign className="h-6 w-6 text-amber-600 dark:text-amber-400" />
                                </div>
                                <div>
                                    <h3 className="font-black text-xl text-slate-900 dark:text-white uppercase tracking-tight">Acquisition Fees</h3>
                                    <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-0.5">Pay-per-performance</p>
                                </div>
                            </div>
                            <div className="px-3 py-1.5 bg-amber-50 dark:bg-amber-400/10 text-amber-700 dark:text-amber-400 text-[9px] font-black rounded-lg border border-amber-100 dark:border-amber-400/20 uppercase tracking-widest">
                                Cycle: Live
                            </div>
                        </div>
                        <div className="flex items-baseline gap-3">
                            <span className="text-4xl font-black text-slate-900 dark:text-white tracking-tight">{formatCurrency(leadFees?.totalOwed || 0)}</span>
                            <span className="text-slate-400 dark:text-slate-500 text-[10px] font-black uppercase tracking-widest">Aggregate Owed</span>
                        </div>
                        <p className="mt-4 text-slate-500 dark:text-slate-400 text-[11px] font-medium leading-relaxed">
                            Structured rate of <span className="text-amber-600 dark:text-amber-400 font-bold">${leadFees?.feePerLead || 2.50}</span> per confirmed reservation originating from the global discovery matrix.
                        </p>
                    </div>
                    <div className="mt-8 pt-6 border-t border-slate-50 dark:border-slate-850 flex justify-between items-center">
                        <span className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">Next Settlement: April 1, 2026</span>
                        <button className="h-9 px-4 rounded-lg bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 text-[10px] font-black uppercase tracking-widest hover:bg-primary-100 dark:hover:bg-primary-900/50 transition-all flex items-center">
                            Ledger <ExternalLink className="h-3 w-3 ml-2" />
                        </button>
                    </div>
                </div>
            </div>

            {/* Marketplace Directory Status */}
            <div className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-10">
                    <div>
                        <h3 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Ecosystem Presence</h3>
                        <p className="text-[11px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Directory Visibility Control</p>
                    </div>
                    <div className={cn(
                        "flex items-center gap-3 px-5 py-2.5 rounded-2xl text-[10px] font-black border uppercase tracking-widest shadow-sm",
                        listing?.isActive 
                            ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-700 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20" 
                            : "bg-slate-50 dark:bg-slate-950 text-slate-400 dark:text-slate-600 border-slate-100 dark:border-slate-800"
                    )}>
                        <div className={cn("w-2 h-2 rounded-full animate-pulse", listing?.isActive ? "bg-emerald-500" : "bg-slate-400")} />
                        {listing?.isActive ? "Broadcast: ACTIVE" : "Broadcast: OFFLINE"}
                    </div>
                </div>

                <div className="grid md:grid-cols-3 gap-8">
                    <div className="p-6 rounded-3xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 group hover:border-primary-500/30 transition-all">
                        <div className="flex items-center gap-3 mb-4">
                            <div className="p-2 bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-slate-100 dark:border-slate-800">
                                <Globe className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                            </div>
                            <span className="text-[10px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-[0.2em]">Index Protocol</span>
                        </div>
                        <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">Successfully Indexed</p>
                        <div className="mt-4 flex items-center gap-2 text-[9px] text-emerald-600 dark:text-emerald-400 font-black bg-emerald-50 dark:bg-emerald-400/10 w-fit px-3 py-1 rounded-lg border border-emerald-100 dark:border-emerald-400/20 uppercase tracking-widest">
                            <CheckCircle2 className="h-3.5 w-3.5" /> High Performance
                        </div>
                    </div>

                    <div className="p-6 rounded-3xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 group hover:border-primary-500/30 transition-all">
                        <div className="flex items-center gap-3 mb-4">
                            <div className="p-2 bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-slate-100 dark:border-slate-800">
                                <Search className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                            </div>
                            <span className="text-[10px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-[0.2em]">Semantics</span>
                        </div>
                        <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight truncate">{listing?.category || 'General Utility'}</p>
                        <div className="mt-4 flex flex-wrap gap-2">
                          {['Lifestyle', 'Wellness', 'Precision'].map(tag => (
                            <span key={tag} className="px-2 py-1 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400 text-[9px] font-bold rounded-lg uppercase tracking-tight">#{tag}</span>
                          ))}
                        </div>
                    </div>

                    <div className="p-6 rounded-3xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-850 group hover:border-primary-500/30 transition-all">
                        <div className="flex items-center gap-3 mb-4">
                            <div className="p-2 bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-slate-100 dark:border-slate-800">
                                <BarChart3 className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                            </div>
                            <span className="text-[10px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-[0.2em]">CTR Telemetry</span>
                        </div>
                        <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">4.8% Click Velocity</p>
                        <div className="mt-4 text-[9px] font-black uppercase tracking-[0.15em] text-slate-400 dark:text-slate-600">
                            1.2k impressions / 30 day cycle
                        </div>
                    </div>
                </div>
                
                <div className="mt-12 flex justify-end gap-4">
                    <Button variant="outline" className="h-12 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] dark:border-slate-800 dark:text-slate-400">Preview Uplink</Button>
                    <Button variant="primary" className="h-12 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 active:scale-95 transition-all">Configure Profile</Button>
                </div>
            </div>

            {/* Marketplace Apps */}
            <div className="pt-6">
                <div className="flex items-center justify-between mb-8">
                    <div>
                        <h3 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Extension Matrix</h3>
                        <p className="text-[11px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Connect third-party operative systems</p>
                    </div>
                    <Button variant="outline" className="h-10 px-6 rounded-xl font-black uppercase tracking-widest text-[10px] dark:border-slate-800 dark:text-slate-400">Library</Button>
                </div>

                <div className="grid md:grid-cols-2 gap-6">
                    {apps.map((app: any) => (
                        <div key={app.id} className="p-6 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl shadow-xl shadow-slate-200/40 dark:shadow-none flex items-center gap-6 hover:border-primary-500/50 transition-all cursor-pointer group active:scale-[0.98]">
                            <div className="w-16 h-16 bg-slate-50 dark:bg-slate-950 rounded-2xl flex items-center justify-center border border-slate-100 dark:border-slate-850 text-slate-400 group-hover:bg-primary-50 dark:group-hover:bg-primary-900/30 group-hover:border-primary-100 dark:group-hover:border-primary-500/20 group-hover:text-primary-500 transition-all shadow-inner">
                                <ShoppingBag className="h-8 w-8" />
                            </div>
                            <div className="flex-1">
                                <h4 className="font-black text-slate-900 dark:text-white text-sm uppercase tracking-tight group-hover:text-primary-600 transition-colors">{app.name}</h4>
                                <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1 leading-relaxed">{app.description}</p>
                            </div>
                            <Button variant="outline" className="h-10 px-6 rounded-xl font-black shadow-sm uppercase tracking-widest text-[9px] dark:border-slate-800 dark:group-hover:border-primary-500/50 transition-all">INITIALIZE</Button>
                        </div>
                    ))}
                </div>
            </div>

            {/* Premium Upsell */}
            {!listing?.isFeatured && (
                <div className="p-10 bg-gradient-to-br from-amber-400 via-amber-500 to-orange-600 rounded-[40px] border border-amber-300 shadow-2xl shadow-amber-500/20 flex flex-wrap items-center justify-between gap-10 relative overflow-hidden group">
                    <div className="max-w-md relative z-10">
                        <div className="flex items-center gap-3 mb-4">
                            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-md shadow-inner border border-white/30">
                                <Zap className="h-6 w-6 text-white fill-white animate-pulse" />
                            </div>
                            <h3 className="text-2xl font-black text-white uppercase tracking-tight">Nexus Priority Status</h3>
                        </div>
                        <p className="text-white/80 text-xs font-bold leading-relaxed uppercase tracking-widest">
                            Achieve absolute domination in city-level index results. Verified featured status accelerates conversion rates by up to <span className="text-white font-black text-sm">250%</span> through algorithm prioritization.
                        </p>
                    </div>
                    <div className="flex flex-col items-center gap-4 relative z-10">
                        <div className="text-center group-hover:scale-110 transition-transform">
                            <span className="text-4xl font-black text-white tracking-tighter">$149</span>
                            <span className="text-white/70 text-sm font-black uppercase tracking-widest ml-1">/mo</span>
                        </div>
                        <Button className="bg-white text-amber-600 hover:bg-slate-50 border-none h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[11px] shadow-2xl shadow-black/10 active:scale-95 transition-all">
                            Enable Featured Uplink
                        </Button>
                    </div>
                    {/* Decorative Zap */}
                    <Zap className="absolute -bottom-10 -right-10 h-64 w-64 text-white/5 rotate-12 group-hover:rotate-45 transition-transform duration-1000" />
                </div>
            )}
        </div>
    );
}

