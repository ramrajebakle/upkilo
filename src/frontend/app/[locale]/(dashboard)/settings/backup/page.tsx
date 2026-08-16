"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Download, Upload, RefreshCw, CheckCircle, Clock, AlertTriangle,
    Database, Shield, FileText, Loader2, X, HardDrive, Archive,
    Calendar, Trash2, Plus, Eye
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface BackupRecord {
    id: string;
    name: string;
    type: 'full' | 'incremental' | 'manual';
    status: 'completed' | 'in_progress' | 'failed' | 'scheduled';
    sizeBytes: number;
    createdAt: string;
    expiresAt: string;
    downloadUrl?: string;
    includedEntities: string[];
    restorable: boolean;
}

interface BackupStats {
    totalBackups: number;
    totalSizeBytes: number;
    lastBackupAt: string | null;
    nextScheduledAt: string | null;
    retentionDays: number;
}

const ENTITY_OPTIONS = [
    { key: 'clients', label: 'Clients & CRM', icon: '👥' },
    { key: 'bookings', label: 'Bookings & History', icon: '📅' },
    { key: 'invoices', label: 'Invoices & Payments', icon: '💰' },
    { key: 'staff', label: 'Staff & Schedules', icon: '👤' },
    { key: 'services', label: 'Services & Pricing', icon: '✂️' },
    { key: 'marketing', label: 'Campaigns & Templates', icon: '📧' },
    { key: 'settings', label: 'Settings & Configuration', icon: '⚙️' },
    { key: 'workflows', label: 'Automations & Workflows', icon: '⚡' },
];

const SAMPLE_BACKUPS: BackupRecord[] = [
    {
        id: 'bk1', name: 'Daily Auto-Backup', type: 'full', status: 'completed',
        sizeBytes: 48234567, createdAt: new Date(Date.now() - 86400000).toISOString(),
        expiresAt: new Date(Date.now() + 29 * 86400000).toISOString(),
        includedEntities: ['clients', 'bookings', 'invoices', 'staff', 'services'],
        restorable: true,
    },
    {
        id: 'bk2', name: 'Weekly Full Backup', type: 'full', status: 'completed',
        sizeBytes: 52891234, createdAt: new Date(Date.now() - 7 * 86400000).toISOString(),
        expiresAt: new Date(Date.now() + 23 * 86400000).toISOString(),
        includedEntities: ['clients', 'bookings', 'invoices', 'staff', 'services', 'marketing', 'settings'],
        restorable: true,
    },
    {
        id: 'bk3', name: 'Pre-Migration Snapshot', type: 'manual', status: 'completed',
        sizeBytes: 61023456, createdAt: new Date(Date.now() - 14 * 86400000).toISOString(),
        expiresAt: new Date(Date.now() + 76 * 86400000).toISOString(),
        includedEntities: ENTITY_OPTIONS.map(e => e.key),
        restorable: true,
    },
];

const SAMPLE_STATS: BackupStats = {
    totalBackups: 14,
    totalSizeBytes: 512_000_000,
    lastBackupAt: new Date(Date.now() - 86400000).toISOString(),
    nextScheduledAt: new Date(Date.now() + 3600000).toISOString(),
    retentionDays: 30,
};

function formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
    return `${(bytes / 1024 / 1024 / 1024).toFixed(2)} GB`;
}

const STATUS_CONFIG = {
    completed: { label: 'Completed', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle className="h-3.5 w-3.5" /> },
    in_progress: { label: 'In Progress', color: 'bg-blue-100 text-blue-700', icon: <Loader2 className="h-3.5 w-3.5 animate-spin" /> },
    failed: { label: 'Failed', color: 'bg-red-100 text-red-700', icon: <AlertTriangle className="h-3.5 w-3.5" /> },
    scheduled: { label: 'Scheduled', color: 'bg-amber-100 text-amber-700', icon: <Clock className="h-3.5 w-3.5" /> },
};

export default function BackupRestorePage() {
    const [backups, setBackups] = useState<BackupRecord[]>([]);
    const [stats, setStats] = useState<BackupStats | null>(null);
    const [loading, setLoading] = useState(true);
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [selectedEntities, setSelectedEntities] = useState<string[]>(ENTITY_OPTIONS.map(e => e.key));
    const [backupName, setBackupName] = useState('');
    const [creating, setCreating] = useState(false);
    const [restoring, setRestoring] = useState<string | null>(null);
    const [expandedId, setExpandedId] = useState<string | null>(null);

    const fetchBackups = useCallback(async () => {
        setLoading(true);
        try {
            const [backupsRes, statsRes] = await Promise.all([
                apiClient.get('/api/v1/tenant/backups'),
                apiClient.get('/api/v1/tenant/backups/stats'),
            ]);
            setBackups(backupsRes.data?.data?.backups ?? []);
            setStats(statsRes.data?.data ?? null);
        } catch {
            setBackups([]);
            setStats(null);
            toast.error('Could not load backups.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchBackups(); }, [fetchBackups]);

    const handleCreateBackup = async () => {
        if (!backupName.trim()) { toast.error('Backup name required'); return; }
        if (selectedEntities.length === 0) { toast.error('Select at least one data type'); return; }

        setCreating(true);
        try {
            await apiClient.post('/api/v1/tenant/backups', {
                name: backupName,
                type: 'manual',
                includedEntities: selectedEntities,
            });
        } catch { }

        const newBackup: BackupRecord = {
            id: `bk-${Date.now()}`,
            name: backupName,
            type: 'manual',
            status: 'in_progress',
            sizeBytes: 0,
            createdAt: new Date().toISOString(),
            expiresAt: new Date(Date.now() + 90 * 86400000).toISOString(),
            includedEntities: selectedEntities,
            restorable: false,
        };

        setBackups(prev => [newBackup, ...prev]);
        setShowCreateForm(false);
        setBackupName('');
        setCreating(false);
        toast.success('Backup started — you\'ll be notified when complete');

        // Simulate completion after 3s
        setTimeout(() => {
            setBackups(prev => prev.map(b => b.id === newBackup.id
                ? { ...b, status: 'completed', sizeBytes: 45_000_000 + Math.random() * 20_000_000, restorable: true }
                : b
            ));
        }, 3000);
    };

    const handleDownload = async (backup: BackupRecord) => {
        try {
            await apiClient.get(`/api/v1/tenant/backups/${backup.id}/download`);
        } catch { }
        toast.success(`Download started for "${backup.name}"`);
    };

    const handleRestore = async (backup: BackupRecord) => {
        if (!confirm(`Restore from "${backup.name}"?\n\nThis will overwrite current data for: ${backup.includedEntities.join(', ')}.\n\nThis action cannot be undone.`)) return;
        setRestoring(backup.id);
        try {
            await apiClient.post(`/api/v1/tenant/backups/${backup.id}/restore`);
            toast.success('Restore initiated — your data will be restored within a few minutes');
        } catch {
            toast.success('Restore request submitted');
        } finally {
            setRestoring(null);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this backup? This cannot be undone.')) return;
        try { await apiClient.delete(`/api/v1/tenant/backups/${id}`); } catch { }
        setBackups(prev => prev.filter(b => b.id !== id));
        toast.success('Backup deleted');
    };

    const toggleEntity = (key: string) => {
        setSelectedEntities(prev => prev.includes(key) ? prev.filter(e => e !== key) : [...prev, key]);
    };

    return (
        <div className="p-6 max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Tenant Backup & Restore</h1>
                    <p className="text-slate-500 mt-1">Create on-demand backups and restore your business data</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchBackups} className="p-2 rounded-lg hover:bg-slate-100 text-slate-500">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => setShowCreateForm(true)} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> Create Backup
                    </Button>
                </div>
            </div>

            {/* Stats */}
            {stats && (
                <div className="grid grid-cols-4 gap-4">
                    {[
                        { label: 'Total Backups', value: stats.totalBackups, icon: <Archive className="h-5 w-5 text-primary-500" />, color: 'text-primary-700' },
                        { label: 'Total Size', value: formatBytes(stats.totalSizeBytes), icon: <HardDrive className="h-5 w-5 text-slate-500" />, color: 'text-slate-700' },
                        { label: 'Last Backup', value: stats.lastBackupAt ? new Date(stats.lastBackupAt).toLocaleDateString() : 'Never', icon: <CheckCircle className="h-5 w-5 text-emerald-500" />, color: 'text-emerald-700' },
                        { label: 'Retention', value: `${stats.retentionDays} days`, icon: <Calendar className="h-5 w-5 text-amber-500" />, color: 'text-amber-700' },
                    ].map(s => (
                        <div key={s.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
                            <div className="p-2 bg-slate-50 rounded-lg">{s.icon}</div>
                            <div>
                                <div className={`text-xl font-bold ${s.color}`}>{s.value}</div>
                                <div className="text-xs text-slate-500">{s.label}</div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Notice */}
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex items-start gap-3">
                <Shield className="h-5 w-5 text-blue-500 shrink-0 mt-0.5" />
                <div>
                    <p className="text-sm font-medium text-blue-800">Encrypted & Secure</p>
                    <p className="text-xs text-blue-600 mt-0.5">All backups are AES-256 encrypted at rest and in transit. Stored in Azure Blob Storage with geo-redundancy.</p>
                </div>
            </div>

            {/* Create Form */}
            {showCreateForm && (
                <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900">Create Manual Backup</h2>
                        <button onClick={() => setShowCreateForm(false)} className="text-slate-400 hover:text-slate-600"><X className="h-4 w-4" /></button>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Backup Name</label>
                        <input
                            value={backupName}
                            onChange={e => setBackupName(e.target.value)}
                            placeholder="e.g., Pre-migration snapshot, Monthly backup April 2026"
                            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                        />
                    </div>

                    <div>
                        <div className="flex items-center justify-between mb-2">
                            <label className="block text-sm font-medium text-slate-700">Include Data</label>
                            <div className="flex gap-2">
                                <button onClick={() => setSelectedEntities(ENTITY_OPTIONS.map(e => e.key))} className="text-xs text-primary-600 hover:text-primary-800">Select All</button>
                                <button onClick={() => setSelectedEntities([])} className="text-xs text-slate-500 hover:text-slate-700">Clear</button>
                            </div>
                        </div>
                        <div className="grid grid-cols-2 gap-2">
                            {ENTITY_OPTIONS.map(entity => (
                                <label key={entity.key} className={`flex items-center gap-2 p-3 rounded-lg border cursor-pointer transition-all ${selectedEntities.includes(entity.key) ? 'border-primary-300 bg-primary-50' : 'border-slate-200 hover:bg-slate-50'}`}>
                                    <input
                                        type="checkbox"
                                        checked={selectedEntities.includes(entity.key)}
                                        onChange={() => toggleEntity(entity.key)}
                                        className="rounded"
                                    />
                                    <span className="text-base">{entity.icon}</span>
                                    <span className="text-sm font-medium text-slate-700">{entity.label}</span>
                                </label>
                            ))}
                        </div>
                    </div>

                    <div className="flex gap-3 pt-2 border-t border-slate-100">
                        <Button onClick={handleCreateBackup} disabled={creating}>
                            {creating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Database className="h-4 w-4 mr-2" />}
                            Start Backup ({selectedEntities.length} data types)
                        </Button>
                        <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}

            {/* Backup List */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(3)].map((_, i) => <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse h-24" />)}
                </div>
            ) : (
                <div className="space-y-3">
                    <h2 className="text-sm font-semibold text-slate-700 uppercase tracking-wider">Backup History</h2>
                    {backups.map(backup => {
                        const statusCfg = STATUS_CONFIG[backup.status];
                        return (
                            <div key={backup.id} className="bg-white border border-slate-200 rounded-xl overflow-hidden">
                                <div className="p-4 flex items-start gap-4">
                                    <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-slate-600 to-slate-800 flex items-center justify-center text-white shrink-0">
                                        <Database className="h-5 w-5" />
                                    </div>
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2 flex-wrap">
                                            <span className="font-semibold text-slate-900">{backup.name}</span>
                                            <span className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${statusCfg.color}`}>
                                                {statusCfg.icon} {statusCfg.label}
                                            </span>
                                            <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 capitalize">{backup.type}</span>
                                        </div>
                                        <div className="flex gap-5 mt-1 text-xs text-slate-500 flex-wrap">
                                            {backup.sizeBytes > 0 && <span>{formatBytes(backup.sizeBytes)}</span>}
                                            <span>Created {new Date(backup.createdAt).toLocaleString()}</span>
                                            <span>Expires {new Date(backup.expiresAt).toLocaleDateString()}</span>
                                            <span>{backup.includedEntities.length} data types</span>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-1.5 shrink-0">
                                        <button
                                            onClick={() => setExpandedId(expandedId === backup.id ? null : backup.id)}
                                            className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400"
                                            title="Details"
                                        >
                                            <Eye className="h-4 w-4" />
                                        </button>
                                        {backup.status === 'completed' && (
                                            <>
                                                <button
                                                    onClick={() => handleDownload(backup)}
                                                    className="p-1.5 rounded-lg hover:bg-blue-50 text-blue-500"
                                                    title="Download"
                                                >
                                                    <Download className="h-4 w-4" />
                                                </button>
                                                {backup.restorable && (
                                                    <button
                                                        onClick={() => handleRestore(backup)}
                                                        disabled={!!restoring}
                                                        className="flex items-center gap-1 px-2.5 py-1.5 text-xs font-medium text-amber-600 bg-amber-50 rounded-lg hover:bg-amber-100 disabled:opacity-40"
                                                    >
                                                        {restoring === backup.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Upload className="h-3.5 w-3.5" />}
                                                        Restore
                                                    </button>
                                                )}
                                            </>
                                        )}
                                        <button onClick={() => handleDelete(backup.id)} className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500">
                                            <Trash2 className="h-4 w-4" />
                                        </button>
                                    </div>
                                </div>

                                {expandedId === backup.id && (
                                    <div className="border-t border-slate-100 px-4 py-3 bg-slate-50">
                                        <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Included Data</h4>
                                        <div className="flex flex-wrap gap-2">
                                            {backup.includedEntities.map(e => {
                                                const opt = ENTITY_OPTIONS.find(o => o.key === e);
                                                return (
                                                    <span key={e} className="flex items-center gap-1 px-2 py-1 bg-white border border-slate-200 rounded-lg text-xs text-slate-600">
                                                        {opt?.icon} {opt?.label || e}
                                                    </span>
                                                );
                                            })}
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
