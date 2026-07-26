'use client';

import { useState, useEffect, useCallback } from 'react';
import { 
    Shield, 
    Search, 
    Filter, 
    Download, 
    ChevronDown, 
    User, 
    Clock, 
    Database, 
    Activity,
    AlertCircle,
    ArrowUpDown,
    CheckCircle2
} from 'lucide-react';
import api from '@/lib/api';
import { cn, formatDate } from '@/lib/utils';
import { motion, AnimatePresence } from 'framer-motion';

interface AuditEntry {
    id: string;
    entityType: string;
    entityId: string;
    action: string;
    userId: string | null;
    userName: string | null;
    ipAddress: string | null;
    userAgent: string | null;
    timestamp: string;
    details: string | null;
    oldValues: string | null;
    newValues: string | null;
    changedFields: string | null;
}

interface AuditSummary {
    totalLogs: number;
    createActions: number;
    updateActions: number;
    deleteActions: number;
    byEntityType: Record<string, number>;
    byUser: Record<string, number>;
}

export default function AuditLogsPage() {
    const [logs, setLogs] = useState<AuditEntry[]>([]);
    const [summary, setSummary] = useState<AuditSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState({
        entityType: '',
        entityId: '',
        userId: '',
        from: '',
        to: '',
        limit: 50
    });
    const [isExporting, setIsExporting] = useState(false);

    const fetchLogs = useCallback(async () => {
        setLoading(true);
        try {
            const [logsRes, summaryRes] = await Promise.all([
                api.audit.getLogs(filters),
                api.audit.getSummary({ from: filters.from, to: filters.to })
            ]);
            setLogs(logsRes.data);
            setSummary(summaryRes.data);
        } catch (err) {
            console.error('Failed to fetch audit logs:', err);
        } finally {
            setLoading(false);
        }
    }, [filters]);

    useEffect(() => {
        fetchLogs();
    }, [fetchLogs]);

    const handleExport = async (format: 'csv' | 'json') => {
        setIsExporting(true);
        try {
            const res = format === 'csv' 
                ? await api.audit.exportCsv(filters)
                : await api.audit.exportJson(filters);
                
            const url = window.URL.createObjectURL(new Blob([res.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', `audit-logs-${new Date().toISOString()}.${format}`);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (err) {
            console.error(`Failed to export ${format}:`, err);
        } finally {
            setIsExporting(false);
        }
    };

    const getActionBadge = (action: string) => {
        const a = (action || '').toLowerCase();
        if (a === 'added' || a === 'create' || a === 'add')
            return 'bg-emerald-100 text-emerald-700 border-emerald-200';
        if (a === 'modified' || a === 'update' || a === 'modify')
            return 'bg-blue-100 text-blue-700 border-blue-200';
        if (a === 'deleted' || a === 'delete')
            return 'bg-red-100 text-red-700 border-red-200';
        return 'bg-slate-100 text-slate-700 border-slate-200';
    };

    return (
        <div className="space-y-8 animate-fade-in">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-bold text-slate-900 tracking-tight flex items-center gap-3">
                        <Shield className="w-8 h-8 text-primary-500" />
                        Audit Logs
                    </h1>
                    <p className="text-slate-500 mt-1">
                        Comprehensive trail of all system changes and administrative actions.
                    </p>
                </div>
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => handleExport('csv')}
                        disabled={isExporting}
                        className="px-4 py-2 bg-white border border-slate-200 text-slate-700 rounded-xl hover:bg-slate-50 flex items-center gap-2 transition-colors disabled:opacity-50"
                    >
                        <Download className="w-4 h-4" />
                        Export CSV
                    </button>
                    <button
                        onClick={() => handleExport('json')}
                        disabled={isExporting}
                        className="px-4 py-2 bg-white border border-slate-200 text-slate-700 rounded-xl hover:bg-slate-50 flex items-center gap-2 transition-colors disabled:opacity-50"
                    >
                        <Download className="w-4 h-4" />
                        JSON
                    </button>
                </div>
            </div>

            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-primary-50 flex items-center justify-center text-primary-600">
                            <Activity className="w-6 h-6" />
                        </div>
                        <div>
                            <p className="text-sm font-medium text-slate-500">Total Activities</p>
                            <p className="text-2xl font-bold text-slate-900">{summary?.totalLogs || 0}</p>
                        </div>
                    </div>
                </div>

                <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-600">
                            <CheckCircle2 className="w-6 h-6" />
                        </div>
                        <div>
                            <p className="text-sm font-medium text-slate-500">Creations</p>
                            <p className="text-2xl font-bold text-slate-900">{summary?.createActions || 0}</p>
                        </div>
                    </div>
                </div>

                <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600">
                            <Filter className="w-6 h-6" />
                        </div>
                        <div>
                            <p className="text-sm font-medium text-slate-500">Updates</p>
                            <p className="text-2xl font-bold text-slate-900">{summary?.updateActions || 0}</p>
                        </div>
                    </div>
                </div>

                <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-red-50 flex items-center justify-center text-red-600">
                            <AlertCircle className="w-6 h-6" />
                        </div>
                        <div>
                            <p className="text-sm font-medium text-slate-500">Deletions</p>
                            <p className="text-2xl font-bold text-slate-900">{summary?.deleteActions || 0}</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Filters */}
            <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200">
                <div className="flex flex-wrap items-center gap-6">
                    <div className="flex-1 min-w-[200px]">
                        <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 block">Entity Type</label>
                        <input
                            type="text"
                            placeholder="e.g. Service, StaffMember"
                            value={filters.entityType}
                            onChange={(e) => setFilters(prev => ({ ...prev, entityType: e.target.value }))}
                            className="w-full px-4 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20"
                        />
                    </div>
                    <div className="flex-1 min-w-[200px]">
                        <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 block">Results</label>
                        <select
                            value={filters.limit}
                            onChange={(e) => setFilters(prev => ({ ...prev, limit: Number(e.target.value) }))}
                            className="w-full px-4 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20"
                        >
                            <option value={50}>50 Results</option>
                            <option value={100}>100 Results</option>
                            <option value={500}>500 Results</option>
                        </select>
                    </div>
                    <div className="flex-1 min-w-[200px]">
                        <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 block">From Date</label>
                        <input
                            type="date"
                            value={filters.from}
                            onChange={(e) => setFilters(prev => ({ ...prev, from: e.target.value }))}
                            className="w-full px-4 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20"
                        />
                    </div>
                    <div className="flex-1 min-w-[200px]">
                        <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 block">To Date</label>
                        <input
                            type="date"
                            value={filters.to}
                            onChange={(e) => setFilters(prev => ({ ...prev, to: e.target.value }))}
                            className="w-full px-4 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20"
                        />
                    </div>
                </div>
            </div>

            {/* Logs Table */}
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50 border-b border-slate-200">
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Date & Time</th>
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">User</th>
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Action</th>
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Resource</th>
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">ID</th>
                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Context</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {loading ? (
                                Array.from({ length: 5 }).map((_, i) => (
                                    <tr key={i} className="animate-pulse">
                                        <td colSpan={6} className="px-6 py-8 h-12 bg-white" />
                                    </tr>
                                ))
                            ) : logs.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center text-slate-500">
                                        No audit logs found matching your criteria.
                                    </td>
                                </tr>
                            ) : (
                                logs.map((log) => (
                                    <tr key={log.id} className="hover:bg-slate-50 transition-colors">
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="flex items-center gap-2 text-sm text-slate-900 font-medium">
                                                <Clock className="w-4 h-4 text-slate-400" />
                                                {formatDate(log.timestamp, 'PPpp')}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="flex items-center gap-2">
                                                <div className="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center text-primary-700 text-xs font-bold">
                                                    {log.userName?.[0] || <User className="w-4 h-4" />}
                                                </div>
                                                <div className="text-sm">
                                                    <p className="text-slate-900 font-medium">{log.userName || 'System'}</p>
                                                    <p className="text-xs text-slate-500">{log.ipAddress || '---'}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <span className={cn(
                                                "px-2.5 py-1 rounded-full text-xs font-bold border capitalize",
                                                getActionBadge(log.action)
                                            )}>
                                                {log.action}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="flex items-center gap-2 text-sm text-slate-900 font-medium">
                                                <Database className="w-4 h-4 text-slate-400" />
                                                {log.entityType}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 font-mono">
                                            {log.entityId.substring(0, 8)}...
                                        </td>
                                        <td className="px-6 py-4 max-w-xs">
                                            <p className="text-sm text-slate-600 truncate" title={log.userAgent || ''}>
                                                {log.userAgent || 'No context available'}
                                            </p>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
