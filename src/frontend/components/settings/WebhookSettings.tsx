'use client';

import { useState, useEffect } from 'react';
import { Webhook, Plus, Trash2, Edit2, Play, RefreshCw, XCircle, CheckCircle2 } from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/components/ui/Toast';
import { Modal } from '@/components/ui/Modal';
import { cn } from '@/lib/utils';

export function WebhookSettings() {
    const [endpoints, setEndpoints] = useState<any[]>([]);
    const [eventTypes, setEventTypes] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const { success, error } = useToast();

    // Modal state
    const [showModal, setShowModal] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [formData, setFormData] = useState({ name: '', url: '', events: [] as string[] });

    // Deliveries state
    const [deliveries, setDeliveries] = useState<any[]>([]);
    const [viewingEndpointId, setViewingEndpointId] = useState<string | null>(null);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [epRes, evRes] = await Promise.all([
                api.webhooks.getEndpoints(),
                api.webhooks.getEventTypes()
            ]);
            setEndpoints(Array.isArray(epRes.data) ? epRes.data : []);
            setEventTypes(Array.isArray(evRes.data) ? evRes.data : []);
        } catch (err) {
            console.error('Failed to load webhooks', err);
            error('Failed to load webhook endpoints');
        } finally {
            setLoading(false);
        }
    };

    const loadDeliveries = async (endpointId: string) => {
        try {
            const res = await api.webhooks.getDeliveries({ endpointId, limit: 10 });
            setDeliveries(Array.isArray(res.data) ? res.data : []);
            setViewingEndpointId(endpointId);
        } catch (err) {
            error('Failed to load webhook logs');
        }
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setActionLoading('save');
        try {
            if (editingId) {
                await api.webhooks.updateEndpoint(editingId, formData);
                success('Webhook successfully updated');
            } else {
                await api.webhooks.createEndpoint(formData);
                success('Webhook successfully created');
            }
            setShowModal(false);
            fetchData();
        } catch (err) {
            error('Failed to save webhook');
        } finally {
            setActionLoading(null);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this webhook endpoint?')) return;
        try {
            await api.webhooks.deleteEndpoint(id);
            success('Webhook deleted');
            fetchData();
        } catch (err) {
            error('Failed to delete webhook');
        }
    };

    const handleTest = async (id: string) => {
        setActionLoading(`test-${id}`);
        try {
            await api.webhooks.testEndpoint(id);
            success('Test event triggered successfully');
            if (viewingEndpointId === id) {
                loadDeliveries(id);
            }
        } catch (err) {
            error('Failed to trigger test event');
        } finally {
            setActionLoading(null);
        }
    };

    const toggleEvent = (eventName: string) => {
        setFormData(prev => {
            const events = prev.events.includes(eventName)
                ? prev.events.filter(e => e !== eventName)
                : [...prev.events, eventName];
            return { ...prev, events };
        });
    };

    const openCreate = () => {
        setEditingId(null);
        setFormData({ name: '', url: '', events: ['*'] });
        setShowModal(true);
    };

    const openEdit = (ep: any) => {
        setEditingId(ep.id);
        setFormData({ name: ep.name, url: ep.url, events: ep.events || [] });
        setShowModal(true);
    };

    if (loading) {
        return <div className="p-8 text-center text-gray-500">Loading webhooks...</div>;
    }

    return (
        <div className="space-y-10">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">External Uplinks</h2>
                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Real-time event synchronization protocol</p>
                </div>
                <Button onClick={openCreate} className="rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 hover:scale-105 active:scale-95 transition-all h-12 px-6">
                    <Plus className="h-4 w-4 mr-2" />
                    Provision Endpoint
                </Button>
            </div>

            <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/50 dark:shadow-none overflow-hidden">
                {endpoints.length === 0 ? (
                    <div className="p-20 text-center">
                        <div className="inline-block p-8 bg-slate-50 dark:bg-slate-800 rounded-3xl mb-6 border border-slate-100 dark:border-slate-700">
                            <Webhook className="h-12 w-12 text-slate-200 dark:text-slate-700" />
                        </div>
                        <h3 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight">No Uplinks Configured</h3>
                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mt-1 mb-8">System is operating in isolation</p>
                        <Button onClick={openCreate} variant="outline" className="rounded-xl font-black uppercase tracking-widest text-[10px] dark:border-slate-700 dark:text-slate-400">Initialize Connection</Button>
                    </div>
                ) : (
                    <div className="divide-y divide-slate-100 dark:divide-slate-800/50">
                        {endpoints.map((ep) => (
                            <div key={ep.id} className="p-8 hover:bg-slate-50/50 dark:hover:bg-slate-800/10 transition-all group">
                                <div className="flex flex-col xl:flex-row justify-between items-start gap-8">
                                    <div className="flex gap-6 flex-1 min-w-0">
                                        <div className="p-4 bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 rounded-2xl h-fit border border-primary-100 dark:border-primary-400/20 shadow-sm">
                                            <Webhook className="h-6 w-6" />
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-4">
                                                <h3 className="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight truncate">{ep.name}</h3>
                                                <span className={cn(
                                                    "px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-[0.2em] border shadow-sm",
                                                    ep.isActive 
                                                        ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20" 
                                                        : "bg-rose-50 dark:bg-rose-400/10 text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-400/20"
                                                )}>
                                                    {ep.isActive ? 'Network: Active' : 'Network: Offline'}
                                                </span>
                                            </div>
                                            <p className="font-mono text-xs text-slate-400 dark:text-slate-500 mt-2 truncate bg-slate-50 dark:bg-slate-950 p-2 rounded-lg border border-slate-100 dark:border-slate-850">{ep.url}</p>
                                            <div className="mt-4 flex flex-wrap gap-2">
                                                {ep.events?.length === 1 && ep.events[0] === '*' ? (
                                                    <span className="bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest border border-slate-200 dark:border-slate-700">Protocol: Comprehensive</span>
                                                ) : (
                                                    ep.events?.slice(0, 3).map((e: string) => (
                                                        <span key={e} className="bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest border border-slate-200 dark:border-slate-700">{e}</span>
                                                    ))
                                                )}
                                                {ep.events?.length > 3 && <span className="bg-primary-50 dark:bg-primary-400/10 text-primary-600 dark:text-primary-400 px-3 py-1 rounded-lg text-[9px] font-black uppercase tracking-widest border border-primary-100 dark:border-primary-400/20">+{ep.events.length - 3} Overflow</span>}
                                            </div>
                                        </div>
                                    </div>
                                    <div className="flex gap-3 shrink-0">
                                        <Button 
                                            variant="outline" 
                                            onClick={() => loadDeliveries(ep.id)}
                                            className="h-10 px-4 rounded-xl font-black uppercase tracking-widest text-[9px] dark:border-slate-700 dark:text-slate-400"
                                        >
                                            System Logs
                                        </Button>
                                        <Button 
                                            variant="outline" 
                                            onClick={() => handleTest(ep.id)}
                                            loading={actionLoading === `test-${ep.id}`}
                                            className="h-10 px-4 rounded-xl font-black uppercase tracking-widest text-[9px] bg-blue-50 dark:bg-blue-400/10 text-blue-700 dark:text-blue-400 hover:bg-blue-100 border-none shadow-sm"
                                        >
                                            <Play className="h-3.5 w-3.5 mr-1.5" /> Launch Test
                                        </Button>
                                        <Button variant="outline" className="h-10 w-10 p-0 rounded-xl dark:border-slate-700 dark:text-slate-400 shadow-sm" onClick={() => openEdit(ep)}>
                                            <Edit2 className="h-4 w-4" />
                                        </Button>
                                        <Button variant="outline" className="h-10 w-10 p-0 rounded-xl text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-400/30 hover:bg-rose-50 dark:hover:bg-rose-400/10 shadow-sm" onClick={() => handleDelete(ep.id)}>
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>

                                {/* Delivery Logs Panel */}
                                {viewingEndpointId === ep.id && (
                                    <div className="mt-8 border border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/20 rounded-[32px] p-8 animate-in slide-in-from-top duration-500">
                                        <div className="flex justify-between items-center mb-6">
                                            <h4 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-widest">Diagnostic Stream</h4>
                                            <Button variant="outline" className="h-8 px-4 rounded-lg font-black uppercase tracking-widest text-[8px] dark:border-slate-700 dark:text-slate-400" onClick={() => loadDeliveries(ep.id)}>
                                                <RefreshCw className="h-3 w-3 mr-1.5" /> Sync Stream
                                            </Button>
                                        </div>
                                        {deliveries.length === 0 ? (
                                            <div className="p-8 text-center bg-white dark:bg-slate-900/50 rounded-2xl border border-slate-100 dark:border-slate-800">
                                                <p className="text-[10px] font-bold text-slate-400 dark:text-slate-600 uppercase tracking-widest">No packet transmissions detected</p>
                                            </div>
                                        ) : (
                                            <div className="space-y-3">
                                                {deliveries.map((log) => (
                                                    <div key={log.id} className="flex justify-between items-center bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 p-4 rounded-2xl shadow-sm hover:scale-[1.01] transition-transform">
                                                        <div className="flex items-center gap-4">
                                                            <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded-lg">
                                                                {log.success ? (
                                                                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                                                                ) : (
                                                                    <XCircle className="h-4 w-4 text-rose-500" />
                                                                )}
                                                            </div>
                                                            <div>
                                                                <span className="font-mono text-[10px] block text-slate-900 dark:text-white font-black uppercase tracking-tight">{log.eventType}</span>
                                                                <span className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">{new Date(log.createdAt).toLocaleString()}</span>
                                                            </div>
                                                        </div>
                                                        <div className="text-right">
                                                            <span className={cn(
                                                                "px-3 py-1 rounded text-[10px] font-black uppercase tracking-widest border",
                                                                log.success 
                                                                    ? "text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-400/10 border-emerald-100 dark:border-emerald-400/20" 
                                                                    : "text-rose-700 dark:text-rose-400 bg-rose-50 dark:bg-rose-400/10 border-rose-100 dark:border-rose-400/20"
                                                            )}>
                                                                {log.responseStatusCode || 'ERR'}
                                                            </span>
                                                            <p className="text-[10px] font-bold text-slate-300 dark:text-slate-600 mt-1 uppercase tracking-widest">{log.durationMs}ms latency</p>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                        )}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            <Modal isOpen={showModal} onClose={() => setShowModal(false)} title={editingId ? "Modify Protocol Uplink" : "Provision Protocol Uplink"}>
                <form onSubmit={handleSave} className="space-y-8 p-2">
                    <div className="space-y-4">
                        <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">UPLINK DESIGNATION *</label>
                        <input
                            required
                            type="text"
                            placeholder="e.g. ZYLINE_RECEIVER"
                            className="w-full h-14 px-6 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-black uppercase tracking-widest focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all outline-none"
                            value={formData.name}
                            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        />
                    </div>
                    <div className="space-y-4">
                        <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">ENDPOINT MATRIX URL *</label>
                        <input
                            required
                            type="url"
                            placeholder="https://uplink.io/api/v1/sync"
                            className="w-full h-14 px-6 bg-slate-50 dark:bg-slate-950 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white text-xs font-mono font-bold focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all outline-none"
                            value={formData.url}
                            onChange={(e) => setFormData({ ...formData, url: e.target.value })}
                        />
                    </div>
                    <div className="space-y-4">
                        <label className="block text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Event Synchronization Matrix</label>
                        <div className="mb-4">
                            <label className="flex items-center gap-4 text-sm p-5 border border-slate-200 dark:border-slate-800 rounded-2xl bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-900 cursor-pointer transition-all">
                                <input
                                    type="checkbox"
                                    className="h-5 w-5 rounded-lg border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                                    checked={formData.events.includes('*')}
                                    onChange={() => {
                                        if (formData.events.includes('*')) {
                                            setFormData({ ...formData, events: [] });
                                        } else {
                                            setFormData({ ...formData, events: ['*'] });
                                        }
                                    }}
                                />
                                <div className="space-y-1">
                                    <span className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight block">Comprehensive Transmission</span>
                                    <span className="text-[10px] font-bold text-slate-500 dark:text-slate-500 uppercase tracking-widest block">Deliver every packet trigger in the cluster</span>
                                </div>
                            </label>
                        </div>
                        {!formData.events.includes('*') && (
                            <div className="max-h-80 overflow-y-auto border border-slate-100 dark:border-slate-800 rounded-2xl p-4 space-y-6 bg-slate-50/50 dark:bg-slate-950/20 custom-scrollbar">
                                {eventTypes.map((category) => (
                                    <div key={category.category} className="space-y-3">
                                        <h4 className="text-[9px] font-black text-primary-500 dark:text-primary-400 uppercase tracking-[0.3em] pl-2">{category.category} Matrix</h4>
                                        <div className="grid grid-cols-1 gap-2">
                                            {category.events.map((evt: string) => (
                                                <label key={evt} className="flex items-center gap-3 text-sm p-3 hover:bg-white dark:hover:bg-slate-800 rounded-xl cursor-pointer transition-colors border border-transparent hover:border-slate-100 dark:hover:border-slate-700 group">
                                                    <input
                                                        type="checkbox"
                                                        className="h-4 w-4 rounded-md border-slate-300 dark:border-slate-700 text-primary-600 focus:ring-primary-500 bg-white dark:bg-slate-800"
                                                        checked={formData.events.includes(evt)}
                                                        onChange={() => toggleEvent(evt)}
                                                    />
                                                    <span className="font-mono text-[10px] text-slate-600 dark:text-slate-400 group-hover:text-slate-900 dark:group-hover:text-white uppercase tracking-tight">{evt}</span>
                                                </label>
                                            ))}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                    <div className="flex justify-end gap-4 pt-8 border-t border-slate-100 dark:border-slate-800">
                        <Button variant="outline" type="button" className="px-8 h-12 rounded-xl dark:border-slate-700 dark:text-slate-400 font-bold uppercase tracking-widest text-[10px]" onClick={() => setShowModal(false)}>Abort</Button>
                        <Button type="submit" loading={actionLoading === 'save'} className="px-8 h-12 rounded-xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20">Commit Sync Protocol</Button>
                    </div>
                </form>
            </Modal>
        </div>
    );
}
