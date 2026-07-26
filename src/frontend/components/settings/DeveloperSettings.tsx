'use client';

import { useState, useEffect } from 'react';
import { Terminal, Key, Plus, Trash2, Copy, CheckCircle2, ShieldAlert, Loader2, AlertCircle } from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/components/ui/Toast';
import { cn } from '@/lib/utils';

export function DeveloperSettings() {
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [apps, setApps] = useState<any[]>([]);
    const [tokens, setTokens] = useState<any[]>([]);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const { success, error } = useToast();

    // New app form state
    const [newAppName, setNewAppName] = useState('');
    const [newAppDesc, setNewAppDesc] = useState('');
    const [newAppUris, setNewAppUris] = useState('');
    
    // Newly generated secret state
    const [newSecret, setNewSecret] = useState<{ clientId: string; clientSecret: string } | null>(null);
    const [copied, setCopied] = useState(false);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [appsRes, tokensRes] = await Promise.all([
                api.oauthApps.getApps().catch(() => ({ data: [] })),
                api.oauthApps.getActiveTokens().catch(() => ({ data: [] }))
            ]);
            setApps(Array.isArray(appsRes.data) ? appsRes.data : []);
            setTokens(Array.isArray(tokensRes.data) ? tokensRes.data : []);
        } catch (err) {
            console.error('Failed to load developer settings:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleCreateApp = async (e: React.FormEvent) => {
        e.preventDefault();
        setActionLoading('create');
        try {
            const uris = newAppUris.split(',').map(u => u.trim()).filter(Boolean);
            const res = await api.oauthApps.registerApp({
                name: newAppName,
                description: newAppDesc,
                redirectUris: uris,
                scopes: ['read', 'write'] // Default scopes for now
            });
            success('Application created successfully');
            setNewSecret({ clientId: res.data.clientId, clientSecret: res.data.clientSecret });
            setShowCreateModal(false);
            setNewAppName('');
            setNewAppDesc('');
            setNewAppUris('');
            fetchData();
        } catch (err) {
            error('Failed to create application');
            console.error(err);
        } finally {
            setActionLoading(null);
        }
    };

    const handleRevokeApp = async (clientId: string) => {
        if (!window.confirm('Are you sure? This will break any integrations using this app.')) return;
        setActionLoading(`revoke-${clientId}`);
        try {
            await api.oauthApps.revokeApp(clientId);
            success('Application revoked');
            fetchData();
        } catch (err) {
            error('Failed to revoke application');
        } finally {
            setActionLoading(null);
        }
    };

    const handleRevokeToken = async (tokenId: string) => {
        setActionLoading(`token-${tokenId}`);
        try {
            await api.oauthApps.revokeToken(tokenId);
            success('Token revoked');
            fetchData();
        } catch (err) {
            error('Failed to revoke token');
        } finally {
            setActionLoading(null);
        }
    };

    const copyToClipboard = (text: string) => {
        navigator.clipboard.writeText(text);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center py-20">
                <Loader2 className="h-8 w-8 text-primary-500 animate-spin mb-4" />
                <p className="text-gray-500">Loading developer settings...</p>
            </div>
        );
    }

    return (
        <div className="space-y-10">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Security Protocol</h2>
                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Authorized entity gateway</p>
                </div>
                <Button onClick={() => setShowCreateModal(true)} className="rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 hover:scale-105 active:scale-95 transition-all">
                    <Plus className="h-4 w-4 mr-2" />
                    Register App
                </Button>
            </div>

            {/* Secret Alert */}
            {newSecret && (
                <div className="p-8 bg-amber-50/50 dark:bg-amber-400/10 border border-amber-200 dark:border-amber-400/20 rounded-[32px] mb-8 shadow-xl shadow-amber-500/5 relative overflow-hidden group animate-in slide-in-from-top duration-500">
                    <div className="absolute top-0 right-0 w-64 h-64 bg-amber-500/5 blur-3xl -mr-32 -mt-32 pointer-events-none" />
                    <div className="flex items-start gap-6 relative">
                        <div className="p-4 bg-amber-100 dark:bg-amber-400/20 rounded-2xl text-amber-600 dark:text-amber-400 shadow-sm border border-amber-200 dark:border-amber-400/30">
                            <AlertCircle className="h-6 w-6" />
                        </div>
                        <div className="flex-1 space-y-4">
                            <div>
                                <h3 className="text-sm font-black text-amber-900 dark:text-amber-400 uppercase tracking-widest">Single-Point secret generation</h3>
                                <p className="text-xs font-bold text-amber-800 dark:text-amber-500/80 mt-1 uppercase tracking-widest leading-relaxed">
                                    Sensitive data detected. This cryptographic secret will not be presented again. Terminal storage required.
                                </p>
                            </div>
                            <div className="grid gap-4">
                                <div className="space-y-2">
                                    <label className="text-[9px] font-black text-amber-900/60 dark:text-amber-400/60 uppercase tracking-[0.3em]">Matrix Identifier</label>
                                    <div className="font-mono bg-white dark:bg-slate-950 p-4 rounded-xl border border-amber-100 dark:border-amber-400/20 text-xs text-slate-600 dark:text-slate-400 break-all shadow-inner">
                                        {newSecret.clientId}
                                    </div>
                                </div>
                                <div className="space-y-2">
                                    <label className="text-[9px] font-black text-amber-900/60 dark:text-amber-400/60 uppercase tracking-[0.3em]">Cryptographic Key</label>
                                    <div className="flex gap-3 mt-1">
                                        <div className="flex-1 font-mono bg-white dark:bg-slate-950 p-4 rounded-xl border border-amber-100 dark:border-amber-400/20 text-xs text-slate-900 dark:text-white break-all shadow-inner">
                                            {newSecret.clientSecret}
                                        </div>
                                        <Button variant="outline" className="h-auto px-4 rounded-xl border-amber-200 dark:border-amber-400/30 dark:bg-slate-900" onClick={() => copyToClipboard(newSecret.clientSecret)}>
                                            {copied ? <CheckCircle2 className="h-4 w-4 text-emerald-500" /> : <Copy className="h-4 w-4 text-amber-600" />}
                                        </Button>
                                    </div>
                                </div>
                            </div>
                            <Button 
                                variant="outline" 
                                className="mt-4 border-amber-300 dark:border-amber-400/30 text-amber-800 dark:text-amber-400 hover:bg-amber-100 dark:hover:bg-amber-400/10 rounded-xl font-black uppercase tracking-widest text-[10px]"
                                onClick={() => setNewSecret(null)}
                            >
                                Security confirmed: Key stored
                            </Button>
                        </div>
                    </div>
                </div>
            )}

            {/* Apps List */}
            <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                <div className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/30 dark:bg-slate-950/20">
                    <div className="flex items-center gap-4">
                        <div className="p-3 bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-700">
                            <Terminal className="h-6 w-6 text-indigo-500 dark:text-indigo-400" />
                        </div>
                        <div>
                            <h3 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight">Active Deployments</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1">Registered neural connections</p>
                        </div>
                    </div>
                </div>
                
                {apps.length === 0 ? (
                    <div className="p-20 text-center">
                        <div className="inline-block p-6 bg-slate-50 dark:bg-slate-800 rounded-3xl mb-4 border border-slate-100 dark:border-slate-700">
                            <Terminal className="h-10 w-10 text-slate-200 dark:text-slate-700" />
                        </div>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">No deployments detected in cluster</p>
                    </div>
                ) : (
                    <div className="divide-y divide-slate-100 dark:divide-slate-800/50">
                        {apps.map((app) => (
                            <div key={app.id} className="p-8 hover:bg-slate-50/50 dark:hover:bg-slate-800/10 transition-all group">
                                <div className="flex flex-col md:flex-row justify-between items-start gap-6 mb-8">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-4">
                                            <h4 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">
                                                {app.name}
                                            </h4>
                                            <span className={cn(
                                                "px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-[0.2em] border",
                                                app.isActive 
                                                    ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20 shadow-sm" 
                                                    : "bg-rose-50 dark:bg-rose-400/10 text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-400/20"
                                            )}>
                                                {app.isActive ? 'Network: Active' : 'Network: Revoked'}
                                            </span>
                                        </div>
                                        <p className="text-[11px] font-bold text-slate-500 dark:text-slate-500 uppercase tracking-widest leading-relaxed">
                                            {app.description || 'System meta description missing'}
                                        </p>
                                    </div>
                                    <Button 
                                        variant="outline" 
                                        onClick={() => handleRevokeApp(app.clientId)}
                                        disabled={!app.isActive || actionLoading === `revoke-${app.clientId}`}
                                        loading={actionLoading === `revoke-${app.clientId}`}
                                        className="rounded-xl font-black uppercase tracking-widest text-[9px] text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-400/30 hover:bg-rose-50 dark:hover:bg-rose-400/10 hover:border-rose-300 transform transition-all group-hover:scale-105"
                                    >
                                        <Trash2 className="h-3.5 w-3.5 mr-2" />
                                        Kill Access
                                    </Button>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 bg-slate-50/50 dark:bg-slate-950/30 p-6 rounded-3xl border border-slate-100/50 dark:border-slate-800/80">
                                    <div className="space-y-1">
                                        <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Matrix ID</p>
                                        <p className="font-mono text-[11px] text-slate-600 dark:text-slate-400 break-all font-bold">{app.clientId}</p>
                                    </div>
                                    <div className="space-y-1">
                                        <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Protocol Redirects</p>
                                        <p className="font-mono text-[11px] text-slate-600 dark:text-slate-400 break-words font-bold">{app.redirectUris}</p>
                                    </div>
                                    <div className="space-y-1">
                                        <p className="text-[9px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-[0.3em]">Initialization Timestamp</p>
                                        <p className="text-[11px] font-black text-slate-600 dark:text-slate-400 uppercase tracking-widest">{new Date(app.createdAt).toLocaleDateString()}</p>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Active Tokens */}
            <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                <div className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/30 dark:bg-slate-950/20">
                    <div className="flex items-center gap-4">
                        <div className="p-3 bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-700">
                            <Key className="h-6 w-6 text-emerald-500 dark:text-emerald-400" />
                        </div>
                        <div>
                            <h3 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight">Active Uplinks</h3>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1">Live third-party token grants</p>
                        </div>
                    </div>
                </div>
                
                {tokens.length === 0 ? (
                    <div className="p-20 text-center">
                        <div className="inline-block p-6 bg-slate-50 dark:bg-slate-800 rounded-3xl mb-4 border border-slate-100 dark:border-slate-700">
                            <ShieldAlert className="h-10 w-10 text-slate-200 dark:text-slate-700" />
                        </div>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest">No active matrix grants detected</p>
                    </div>
                ) : (
                    <div className="divide-y divide-slate-100 dark:divide-slate-800/50">
                        {tokens.map((token) => (
                            <div key={token.id} className="p-8 flex flex-col sm:flex-row items-center justify-between hover:bg-slate-50/50 dark:hover:bg-slate-800/10 transition-all group gap-6">
                                <div className="space-y-3">
                                    <h4 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight">{token.app?.name || 'Unknown Protocol'}</h4>
                                    <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">Host ID: {token.userId}</p>
                                    <div className="flex flex-wrap gap-3 mt-2">
                                        <span className="text-[9px] font-black bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-3 py-1 rounded-lg uppercase tracking-widest border border-slate-200 dark:border-slate-700">Permissions: {token.scope}</span>
                                        <span className="text-[9px] font-black bg-indigo-50 dark:bg-indigo-400/10 text-indigo-600 dark:text-indigo-400 px-3 py-1 rounded-lg uppercase tracking-widest border border-indigo-100 dark:border-indigo-400/20">
                                            Expiry: {new Date(token.expiresAt).toLocaleDateString()}
                                        </span>
                                    </div>
                                </div>
                                <Button 
                                    variant="outline" 
                                    onClick={() => handleRevokeToken(token.id)}
                                    disabled={actionLoading === `token-${token.id}`}
                                    loading={actionLoading === `token-${token.id}`}
                                    className="rounded-xl font-black uppercase tracking-widest text-[9px] text-amber-600 dark:text-amber-400 border-amber-100 dark:border-amber-400/30 hover:bg-amber-50 dark:hover:bg-amber-400/10 hover:border-amber-300 transform transition-all group-hover:scale-105 shrink-0"
                                >
                                    Revoke Token
                                </Button>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Create Modal */}
            {showCreateModal && (
                <div className="fixed inset-0 bg-slate-900/60 dark:bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
                    <div className="bg-white dark:bg-slate-900 rounded-[40px] w-full max-w-lg shadow-[0_20px_50px_rgba(0,0,0,0.3)] dark:shadow-none overflow-hidden border border-slate-100 dark:border-slate-800 animate-in zoom-in-95 duration-300">
                        <div className="p-8 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/20">
                            <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Register Neural Deployment</h2>
                            <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1 text-center">New authorized connection point</p>
                        </div>
                        <form onSubmit={handleCreateApp} className="p-8 space-y-8">
                            <div className="space-y-4">
                                <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">DEPLOYMENT CODENAME *</label>
                                <input 
                                    type="text" 
                                    required 
                                    className="w-full h-14 px-6 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-black uppercase tracking-widest focus:ring-4 focus:ring-indigo-500/10 focus:border-indigo-500 transition-all outline-none" 
                                    placeholder="e.g. ZYLINE_INTEGRATOR" 
                                    value={newAppName}
                                    onChange={e => setNewAppName(e.target.value)}
                                />
                            </div>
                            <div className="space-y-4">
                                <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">META DESCRIPTION</label>
                                <textarea 
                                    className="w-full p-6 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-bold uppercase tracking-widest focus:ring-4 focus:ring-indigo-500/10 focus:border-indigo-500 transition-all outline-none resize-none" 
                                    rows={3} 
                                    placeholder="Technical overview of connection..."
                                    value={newAppDesc}
                                    onChange={e => setNewAppDesc(e.target.value)}
                                />
                            </div>
                            <div className="space-y-4">
                                <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">PROTOCOL REDIR ENDPOINTS *</label>
                                <p className="text-[9px] font-black text-indigo-500 dark:text-indigo-400 uppercase tracking-widest">Matrix delimited list required</p>
                                <input 
                                    type="text" 
                                    required 
                                    className="w-full h-14 px-6 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-black uppercase tracking-widest focus:ring-4 focus:ring-indigo-500/10 focus:border-indigo-500 transition-all outline-none" 
                                    placeholder="https://uplink.io/callback" 
                                    value={newAppUris}
                                    onChange={e => setNewAppUris(e.target.value)}
                                />
                            </div>
                            <div className="flex justify-end gap-4 pt-6">
                                <Button 
                                    variant="outline" 
                                    type="button" 
                                    onClick={() => setShowCreateModal(false)}
                                    className="px-8 h-12 rounded-xl dark:border-slate-700 dark:text-slate-400 font-bold uppercase tracking-widest text-[10px]"
                                >
                                    Abort
                                </Button>
                                <Button 
                                    type="submit" 
                                    loading={actionLoading === 'create'}
                                    className="px-8 h-12 rounded-xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20"
                                >
                                    COMMIT SYSTEM REGISTER
                                </Button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
