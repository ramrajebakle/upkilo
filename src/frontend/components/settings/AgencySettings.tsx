'use client';

import { useState, useEffect } from 'react';
import { 
    Users, 
    Plus, 
    Search, 
    MoreVertical, 
    ExternalLink, 
    Shield, 
    Building2, 
    CreditCard,
    TrendingUp,
    AlertCircle,
    CheckCircle2,
    XCircle
} from 'lucide-react';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';

export function AgencySettings() {
    const [subAccounts, setSubAccounts] = useState<any[]>([]);
    const [billing, setBilling] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const { success, error } = useToast();

    // New sub-account form
    const [formData, setFormData] = useState({
        businessName: '',
        slug: '',
        sector: 'Services'
    });

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [accountsRes, billingRes] = await Promise.all([
                api.whitelabel.getSubAccounts(),
                api.whitelabel.getAgencyBilling()
            ]);
            setSubAccounts(accountsRes.data.data || []);
            setBilling(billingRes.data);
        } catch (err) {
            console.error('Failed to fetch agency data', err);
            error('Failed to load sub-accounts');
        } finally {
            setLoading(false);
        }
    };

    const handleCreateSubAccount = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        try {
            await api.whitelabel.createSubAccount(formData);
            success('Sub-account created successfully');
            setIsCreateModalOpen(false);
            setFormData({ businessName: '', slug: '', sector: 'Services' });
            fetchData();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } catch (err: any) {
            error(err.response?.data?.error || 'Failed to create sub-account');
        } finally {
            setIsSubmitting(false);
        }
    };

    const filteredAccounts = subAccounts.filter(acc => 
        acc.businessName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        acc.slug.toLowerCase().includes(searchTerm.toLowerCase())
    );

    if (loading && !billing) {
        return (
            <div className="flex items-center justify-center p-12">
                <div className="w-8 h-8 border-4 border-primary-100 border-t-primary-500 rounded-full animate-spin" />
            </div>
        );
    }

    return (
        <div className="space-y-10 animate-fade-in">
            {/* Stats Overview */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] p-8 shadow-2xl shadow-slate-200/50 dark:shadow-none hover:scale-[1.02] transition-all group">
                    <div className="flex items-center gap-5 mb-4">
                        <div className="p-3 bg-blue-50 dark:bg-blue-900/30 rounded-2xl text-blue-600 dark:text-blue-400 border border-blue-100 dark:border-blue-400/20 shadow-sm group-hover:scale-110 transition-transform">
                            <Users className="h-6 w-6" />
                        </div>
                        <div>
                            <span className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Total Entities</span>
                            <div className="text-3xl font-black text-slate-900 dark:text-white tracking-tighter">{subAccounts.length}</div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest pl-1">
                        <TrendingUp className="h-3.5 w-3.5 text-success-fg" />
                        Managed sub-tenants
                    </div>
                </div>

                <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] p-8 shadow-2xl shadow-slate-200/50 dark:shadow-none hover:scale-[1.02] transition-all group">
                    <div className="flex items-center gap-5 mb-4">
                        <div className="p-3 bg-emerald-50 dark:bg-emerald-900/30 rounded-2xl text-emerald-600 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-400/20 shadow-sm group-hover:scale-110 transition-transform">
                            <CheckCircle2 className="h-6 w-6" />
                        </div>
                        <div>
                            <span className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Live Uplinks</span>
                            <div className="text-3xl font-black text-slate-900 dark:text-white tracking-tighter">
                                {billing?.subAccounts?.activeCount || 0}
                            </div>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest pl-1">
                        <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                        Generating revenue
                    </div>
                </div>

                <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] p-8 shadow-2xl shadow-slate-200/50 dark:shadow-none hover:scale-[1.02] transition-all group">
                    <div className="flex items-center gap-5 mb-4">
                        <div className="p-3 bg-primary-50 dark:bg-primary-900/30 rounded-2xl text-primary-600 dark:text-primary-400 border border-primary-100 dark:border-primary-400/20 shadow-sm group-hover:scale-110 transition-transform">
                            <CreditCard className="h-6 w-6" />
                        </div>
                        <div>
                            <span className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Agency Overhead</span>
                            <div className="text-3xl font-black text-slate-900 dark:text-white tracking-tighter">
                                ${billing?.estimatedTotal?.toFixed(2) || '0.00'}
                            </div>
                        </div>
                    </div>
                    <div className="text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest pl-1">
                        Estimated next cycle
                    </div>
                </div>
            </div>

            {/* Sub-accounts List */}
            <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                <div className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/30 dark:bg-slate-950/20 flex flex-col xl:flex-row xl:items-center justify-between gap-6">
                    <div>
                        <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Entity Directory</h2>
                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Tenant node synchronization matrix</p>
                    </div>
                    <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-4">
                        <div className="relative group min-w-[280px]">
                            <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted group-focus-within:text-primary-500 transition-colors" />
                            <input
                                type="text"
                                placeholder="Filter nodes..."
                                className="w-full bg-white dark:bg-slate-950 border border-slate-200 dark:border-slate-850 rounded-2xl pl-12 pr-4 py-3.5 text-xs font-bold uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                        <button 
                            onClick={() => setIsCreateModalOpen(true)}
                            className="bg-primary-600 hover:bg-primary-700 text-white px-8 py-3.5 rounded-2xl text-[10px] font-black uppercase tracking-[0.2em] shadow-xl shadow-primary-600/25 transition-all active:scale-95 flex items-center justify-center gap-2"
                        >
                            <Plus className="h-4 w-4" />
                            Provision Entity
                        </button>
                    </div>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/50 dark:bg-slate-800/10">
                                <th className="px-8 py-5 text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Business Identifier</th>
                                <th className="px-8 py-5 text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Sector</th>
                                <th className="px-8 py-5 text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Status</th>
                                <th className="px-8 py-5 text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em]">Init Date</th>
                                <th className="px-8 py-5 text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] text-right">Ops</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-slate-800/50">
                            {filteredAccounts.length > 0 ? (
                                filteredAccounts.map((account) => (
                                    <tr key={account.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-800/20 transition-all group">
                                        <td className="px-8 py-6">
                                            <div className="flex items-center gap-5">
                                                <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center text-white font-black text-sm shadow-xl group-hover:scale-110 transition-transform">
                                                    {account.businessName.substring(0, 2).toUpperCase()}
                                                </div>
                                                <div className="space-y-1">
                                                    <div className="font-black text-slate-900 dark:text-white uppercase tracking-tight text-base">{account.businessName}</div>
                                                    <div className="text-[10px] text-primary-500 font-black uppercase tracking-[0.2em] flex items-center gap-1.5">
                                                        <ExternalLink className="h-3 w-3" />
                                                        {account.slug}.upkilo.com
                                                    </div>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-8 py-6">
                                            <span className="text-[11px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest">{account.sector}</span>
                                        </td>
                                        <td className="px-8 py-6">
                                            {account.status === 'Active' ? (
                                                <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-lg bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 text-[9px] font-black uppercase tracking-widest border border-emerald-100 dark:border-emerald-400/20 shadow-sm">
                                                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                                                    Syncing
                                                </span>
                                            ) : (
                                                <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-lg bg-rose-50 dark:bg-rose-400/10 text-rose-600 dark:text-rose-400 text-[9px] font-black uppercase tracking-widest border border-rose-100 dark:border-rose-400/20">
                                                    <XCircle className="h-3.5 w-3.5" />
                                                    Offline
                                                </span>
                                            )}
                                        </td>
                                        <td className="px-8 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-widest">
                                            {new Date(account.createdAt).toLocaleDateString()}
                                        </td>
                                        <td className="px-8 py-6 text-right">
                                            <div className="flex items-center justify-end gap-3 opacity-0 group-hover:opacity-100 transition-all translate-x-4 group-hover:translate-x-0">
                                                <button className="p-2.5 bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 rounded-xl text-foreground-muted hover:text-primary-600 dark:hover:text-primary-400 transition-all shadow-sm hover:scale-110">
                                                    <ExternalLink className="h-4 w-4" />
                                                </button>
                                                <button className="p-2.5 bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 rounded-xl text-foreground-muted hover:text-slate-900 dark:hover:text-white transition-all shadow-sm hover:scale-110">
                                                    <MoreVertical className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <tr>
                                    <td colSpan={5} className="px-8 py-24 text-center">
                                        <div className="flex flex-col items-center justify-center gap-6">
                                            <div className="p-6 bg-slate-50 dark:bg-slate-800 rounded-3xl border border-slate-100 dark:border-slate-700">
                                                <Building2 className="h-12 w-12 text-slate-200" />
                                            </div>
                                            <div className="text-[11px] font-black text-foreground-muted uppercase tracking-[0.4em]">Zero deployments detected matching criteria</div>
                                        </div>
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Billing Details */}
            <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden group">
                <div className="p-10 border-b border-slate-100 dark:border-slate-800 bg-primary-50/30 dark:bg-primary-400/5 flex items-start gap-8">
                    <div className="p-5 bg-primary-600 text-white rounded-3xl shadow-xl shadow-primary-600/20 group-hover:scale-110 transition-all">
                        <CreditCard className="h-8 w-8" />
                    </div>
                    <div className="flex-1">
                        <h3 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Agency Ledger</h3>
                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Financial overhead and telemetry metrics</p>
                        
                        <div className="mt-10 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12">
                            <div className="space-y-2">
                                <div className="text-[9px] font-black text-foreground-muted uppercase tracking-[0.4em]">Base Framework</div>
                                <div className="text-xl font-black text-slate-900 dark:text-white tracking-tight">${billing?.basePlanCost || '199.00'}</div>
                            </div>
                            <div className="space-y-2">
                                <div className="text-[9px] font-black text-foreground-muted uppercase tracking-[0.4em]">Entity Surcharge</div>
                                <div className="text-xl font-black text-slate-900 dark:text-white tracking-tight">${billing?.subAccounts?.totalCost?.toFixed(2) || '0.00'}</div>
                                <div className="text-[9px] font-bold text-primary-500 dark:text-primary-400 uppercase tracking-widest">${billing?.subAccounts?.costPerAccount || '29'} / unique node</div>
                            </div>
                            <div className="space-y-2">
                                <div className="text-[9px] font-black text-foreground-muted uppercase tracking-[0.4em]">Temporal Window</div>
                                <div className="text-[10px] font-black text-slate-700 dark:text-slate-300 uppercase tracking-widest">
                                    {billing?.currentCycle ? `${new Date(billing.currentCycle.startsAt).toLocaleDateString()} ➔ ${new Date(billing.currentCycle.endsAt).toLocaleDateString()}` : 'Monthly Protocol'}
                                </div>
                            </div>
                            <div className="bg-white dark:bg-slate-950 p-6 rounded-3xl border border-primary-100 dark:border-primary-400/20 shadow-xl shadow-primary-500/5 flex flex-col justify-center">
                                <div className="text-[9px] font-black text-primary-600 dark:text-primary-400 uppercase tracking-[0.4em] mb-1">Projected Total</div>
                                <div className="text-3xl font-black text-slate-900 dark:text-white tracking-tighter">${billing?.estimatedTotal?.toFixed(2) || '0.00'}</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Create Modal */}
            {isCreateModalOpen && (
                <div className="fixed inset-0 z-[100] flex items-center justify-center p-6 bg-slate-950/80 backdrop-blur-xl animate-in fade-in duration-300">
                    <div 
                        className="bg-white dark:bg-slate-900 rounded-[48px] shadow-[0_20px_50px_rgba(0,0,0,0.4)] w-full max-w-2xl overflow-hidden animate-in zoom-in-95 slide-in-from-bottom-12 duration-500 border border-slate-100 dark:border-slate-800"
                        onClick={(e) => e.stopPropagation()}
                    >
                        <div className="p-10 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/30">
                            <div className="flex items-center gap-6">
                                <div className="p-5 bg-primary-600 text-white rounded-3xl shadow-xl shadow-primary-600/20">
                                    <Plus className="h-8 w-8" />
                                </div>
                                <div>
                                    <h3 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Provision Entity</h3>
                                    <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Deploying dedicated tenant node to matrix</p>
                                </div>
                            </div>
                        </div>

                        <form onSubmit={handleCreateSubAccount} className="p-12 space-y-10">
                            <div className="grid gap-8">
                                <div className="space-y-4">
                                    <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em] pl-1">Business Designation *</label>
                                    <input
                                        required
                                        type="text"
                                        placeholder="E.g. ZENITH_PROTOCOLS"
                                        className="w-full h-16 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl px-6 text-sm font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white"
                                        value={formData.businessName}
                                        onChange={(e) => setFormData({ ...formData, businessName: e.target.value, slug: e.target.value.toLowerCase().replace(/\s+/g, '-') })}
                                    />
                                </div>
                                <div className="space-y-4">
                                    <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em] pl-1">Protocol URL Endpoint *</label>
                                    <div className="flex items-center group">
                                        <input
                                            required
                                            type="text"
                                            className="flex-1 h-16 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-l-2xl px-6 text-sm font-bold tracking-tight outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white border-r-0"
                                            value={formData.slug}
                                            onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                                        />
                                        <span className="flex items-center h-16 px-6 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-800 text-foreground-muted text-[10px] font-black uppercase tracking-widest rounded-r-2xl border-l-0">
                                            .upkilo.com
                                        </span>
                                    </div>
                                </div>
                                <div className="space-y-4">
                                    <label className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em] pl-1">Industry Sector Matrix</label>
                                    <div className="relative">
                                        <select 
                                            className="w-full h-16 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl px-6 text-sm font-black uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all dark:text-white appearance-none cursor-pointer"
                                            value={formData.sector}
                                            onChange={(e) => setFormData({ ...formData, sector: e.target.value })}
                                        >
                                            <option>Medical</option>
                                            <option>Services</option>
                                            <option>Consulting</option>
                                            <option>Beauty</option>
                                            <option>Other</option>
                                        </select>
                                        <div className="absolute right-6 top-1/2 -translate-y-1/2 pointer-events-none text-foreground-muted">
                                            <TrendingUp className="h-5 w-5" />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="flex items-start gap-5 p-6 bg-primary-50/50 dark:bg-primary-400/5 rounded-3xl border border-primary-100/50 dark:border-primary-400/10">
                                <AlertCircle className="h-6 w-6 text-primary-600 dark:text-primary-400 shrink-0 mt-0.5" />
                                <div className="space-y-1">
                                    <p className="text-[10px] font-black text-primary-700 dark:text-primary-300 uppercase tracking-widest">Protocol Overhead Reminder</p>
                                    <p className="text-[10px] text-primary-600/70 dark:text-primary-400/70 leading-relaxed font-bold uppercase tracking-widest">
                                        Initializing entity node scales overhead by <span className="text-slate-900 dark:text-white">$29.00/cycle</span>. Committed to next financial dispatch.
                                    </p>
                                </div>
                            </div>

                            <div className="flex justify-end gap-4 pt-6">
                                <button 
                                    type="button"
                                    onClick={() => setIsCreateModalOpen(false)}
                                    className="px-10 h-16 border border-slate-200 dark:border-slate-800 rounded-2xl text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800 transition-all"
                                >
                                    Abort
                                </button>
                                <button 
                                    type="submit"
                                    disabled={isSubmitting}
                                    className="bg-primary-600 hover:bg-primary-700 text-white px-12 h-16 rounded-2xl text-[10px] font-black uppercase tracking-[0.2em] shadow-2xl shadow-primary-600/25 transition-all active:scale-95 disabled:opacity-50"
                                >
                                    {isSubmitting ? 'DEPLOYING...' : 'COMMIT DEPLOYMENT'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
