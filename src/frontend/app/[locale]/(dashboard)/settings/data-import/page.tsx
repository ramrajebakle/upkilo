"use client";

import React, { useState, useCallback, useRef } from 'react';
import {
    Upload, Download, FileText, Users, Calendar, CheckCircle,
    XCircle, AlertCircle, Loader2, RefreshCw, ChevronDown, ChevronUp
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface ImportJob {
    jobId: string;
    status: 'pending' | 'processing' | 'completed' | 'failed';
    totalRows?: number;
    processedRows?: number;
    errorRows?: number;
    errors?: string[];
    completedAt?: string;
}

interface ExportJob {
    jobId: string;
    status: string;
    downloadUrl?: string;
    completedAt?: string;
}

type EntityType = 'clients' | 'bookings';

export default function DataImportPage() {
    const [activeTab, setActiveTab] = useState<'import' | 'export'>('import');
    const [entityType, setEntityType] = useState<EntityType>('clients');
    const [importFile, setImportFile] = useState<File | null>(null);
    const [importing, setImporting] = useState(false);
    const [exporting, setExporting] = useState(false);
    const [importJob, setImportJob] = useState<ImportJob | null>(null);
    const [exportJob, setExportJob] = useState<ExportJob | null>(null);
    const [validating, setValidating] = useState(false);
    const [validationResult, setValidationResult] = useState<any>(null);
    const [showErrors, setShowErrors] = useState(false);
    const fileRef = useRef<HTMLInputElement>(null);

    const pollJobStatus = async (jobId: string, type: 'import' | 'export') => {
        const endpoint = type === 'import' ? `/api/v1/data/import/${jobId}/status` : `/api/v1/data/export/${jobId}/status`;
        const maxAttempts = 30;
        let attempts = 0;
        while (attempts < maxAttempts) {
            await new Promise(r => setTimeout(r, 2000));
            try {
                const res = await apiClient.get(endpoint);
                const job = res.data?.data || res.data;
                if (type === 'import') {
                    setImportJob(job);
                    if (job.status === 'completed' || job.status === 'failed') break;
                } else {
                    setExportJob(job);
                    if (job.status === 'completed' || job.status === 'failed') break;
                }
            } catch { break; }
            attempts++;
        }
    };

    const handleValidate = async () => {
        if (!importFile) return;
        setValidating(true);
        setValidationResult(null);
        const formData = new FormData();
        formData.append('file', importFile);
        formData.append('entityType', entityType);
        try {
            const res = await apiClient.post('/api/v1/data/import/validate', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            setValidationResult(res.data?.data || res.data);
        } catch (e: any) {
            toast.error(e?.response?.data?.error || 'Validation failed');
        } finally {
            setValidating(false);
        }
    };

    const handleImport = async () => {
        if (!importFile) { toast.error('Please select a file'); return; }
        setImporting(true);
        setImportJob(null);
        const formData = new FormData();
        formData.append('file', importFile);
        try {
            const endpoint = entityType === 'clients' ? '/api/v1/data/import/clients' : '/api/v1/data/import/bookings';
            const res = await apiClient.post(endpoint, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            const job = res.data?.data || res.data;
            setImportJob({ ...job, status: 'processing' });
            toast.success('Import started — processing in background');
            await pollJobStatus(job.jobId, 'import');
        } catch (e: any) {
            toast.error(e?.response?.data?.error || 'Import failed');
        } finally {
            setImporting(false);
        }
    };

    const handleExport = async () => {
        setExporting(true);
        setExportJob(null);
        try {
            const endpoint = entityType === 'clients' ? '/api/v1/data/export/clients' : '/api/v1/data/export/bookings';
            const res = await apiClient.post(endpoint, {});
            const job = res.data?.data || res.data;
            setExportJob({ ...job, status: 'processing' });
            toast.success('Export started — preparing download');
            await pollJobStatus(job.jobId, 'export');
        } catch (e: any) {
            toast.error(e?.response?.data?.error || 'Export failed');
        } finally {
            setExporting(false);
        }
    };

    const handleDownload = async () => {
        if (!exportJob?.jobId) return;
        try {
            const res = await apiClient.get(`/api/v1/data/download/${exportJob.jobId}`, { responseType: 'blob' });
            const url = URL.createObjectURL(res.data);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${entityType}-export.csv`;
            a.click();
            URL.revokeObjectURL(url);
        } catch {
            toast.error('Download failed');
        }
    };

    const handleDownloadTemplate = async () => {
        try {
            const res = await apiClient.get('/api/v1/data/import/templates', { params: { entityType } });
            const data = res.data?.data || res.data;
            // Download as CSV
            const headers = Array.isArray(data) ? data.join(',') : Object.keys(data).join(',');
            const blob = new Blob([headers + '\n'], { type: 'text/csv' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${entityType}-import-template.csv`;
            a.click();
            URL.revokeObjectURL(url);
        } catch {
            toast.error('Failed to download template');
        }
    };

    return (
        <div className="p-6 max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div>
                <h1 className="text-2xl font-bold text-slate-900">Data Import / Export</h1>
                <p className="text-slate-500 mt-1">Bulk import clients/bookings from CSV, or export your data</p>
            </div>

            {/* Tab switcher */}
            <div className="flex gap-1 bg-slate-100 p-1 rounded-xl w-fit">
                {(['import', 'export'] as const).map(tab => (
                    <button
                        key={tab}
                        onClick={() => setActiveTab(tab)}
                        className={`flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-medium capitalize transition-colors ${activeTab === tab ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'}`}
                    >
                        {tab === 'import' ? <Upload className="h-4 w-4" /> : <Download className="h-4 w-4" />}
                        {tab}
                    </button>
                ))}
            </div>

            {/* Entity type selector */}
            <div className="flex gap-2">
                {(['clients', 'bookings'] as EntityType[]).map(e => (
                    <button
                        key={e}
                        onClick={() => setEntityType(e)}
                        className={`flex items-center gap-2 px-4 py-2 rounded-lg border text-sm font-medium capitalize transition-colors ${entityType === e ? 'bg-primary-600 text-white border-primary-600' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'}`}
                    >
                        {e === 'clients' ? <Users className="h-4 w-4" /> : <Calendar className="h-4 w-4" />}
                        {e}
                    </button>
                ))}
            </div>

            {/* Import tab */}
            {activeTab === 'import' && (
                <div className="space-y-5">
                    {/* Download template */}
                    <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex items-center justify-between">
                        <div>
                            <p className="text-sm font-medium text-blue-800">Need a template?</p>
                            <p className="text-xs text-blue-600 mt-0.5">Download the CSV template with the correct column headers</p>
                        </div>
                        <Button variant="outline" size="sm" onClick={handleDownloadTemplate} className="border-blue-300 text-blue-700 hover:bg-blue-100">
                            <Download className="h-4 w-4 mr-2" /> Download Template
                        </Button>
                    </div>

                    {/* File upload */}
                    <div className="bg-white border border-slate-200 rounded-xl p-6">
                        <h2 className="font-semibold text-slate-900 mb-4">Upload CSV File</h2>
                        <div
                            onClick={() => fileRef.current?.click()}
                            className={`border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors ${importFile ? 'border-primary-300 bg-primary-50' : 'border-slate-200 hover:border-primary-300 hover:bg-slate-50'}`}
                        >
                            <input
                                ref={fileRef}
                                type="file"
                                accept=".csv,.xlsx"
                                className="hidden"
                                onChange={e => {
                                    const f = e.target.files?.[0];
                                    if (f) { setImportFile(f); setValidationResult(null); setImportJob(null); }
                                }}
                            />
                            <Upload className={`h-10 w-10 mx-auto mb-3 ${importFile ? 'text-primary-500' : 'text-slate-300'}`} />
                            {importFile ? (
                                <div>
                                    <p className="font-medium text-slate-900">{importFile.name}</p>
                                    <p className="text-xs text-slate-500 mt-0.5">{(importFile.size / 1024).toFixed(1)} KB · Click to change</p>
                                </div>
                            ) : (
                                <div>
                                    <p className="font-medium text-slate-700">Drop your CSV file here</p>
                                    <p className="text-xs text-slate-400 mt-0.5">or click to browse</p>
                                </div>
                            )}
                        </div>

                        {importFile && (
                            <div className="flex gap-3 mt-4">
                                <Button variant="outline" onClick={handleValidate} disabled={validating} className="flex-1">
                                    {validating ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <CheckCircle className="h-4 w-4 mr-2" />}
                                    {validating ? 'Validating...' : 'Validate File'}
                                </Button>
                                <Button onClick={handleImport} disabled={importing} className="flex-1">
                                    {importing ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Upload className="h-4 w-4 mr-2" />}
                                    {importing ? 'Importing...' : `Import ${entityType}`}
                                </Button>
                            </div>
                        )}
                    </div>

                    {/* Validation Result */}
                    {validationResult && (
                        <div className={`bg-white border rounded-xl p-5 ${validationResult.isValid ? 'border-emerald-200' : 'border-amber-200'}`}>
                            <div className="flex items-center gap-2 mb-3">
                                {validationResult.isValid
                                    ? <CheckCircle className="h-5 w-5 text-emerald-500" />
                                    : <AlertCircle className="h-5 w-5 text-amber-500" />}
                                <h3 className="font-semibold text-slate-900">
                                    {validationResult.isValid ? 'File is valid' : 'Validation warnings'}
                                </h3>
                            </div>
                            <div className="grid grid-cols-3 gap-4 text-sm">
                                <div><span className="text-slate-500">Total rows:</span> <strong>{validationResult.totalRows || 0}</strong></div>
                                <div><span className="text-slate-500">Valid:</span> <strong className="text-emerald-600">{validationResult.validRows || 0}</strong></div>
                                <div><span className="text-slate-500">Errors:</span> <strong className="text-red-600">{validationResult.errorRows || 0}</strong></div>
                            </div>
                            {validationResult.errors?.length > 0 && (
                                <div className="mt-3">
                                    <button onClick={() => setShowErrors(!showErrors)} className="flex items-center gap-1 text-xs text-slate-500 hover:text-slate-700">
                                        {showErrors ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
                                        {validationResult.errors.length} error(s)
                                    </button>
                                    {showErrors && (
                                        <div className="mt-2 max-h-32 overflow-y-auto space-y-1">
                                            {validationResult.errors.slice(0, 20).map((e: string, i: number) => (
                                                <p key={i} className="text-xs text-red-600 font-mono">{e}</p>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    )}

                    {/* Import Job Status */}
                    {importJob && (
                        <div className="bg-white border border-slate-200 rounded-xl p-5">
                            <div className="flex items-center gap-2 mb-3">
                                {importJob.status === 'processing' && <Loader2 className="h-5 w-5 text-blue-500 animate-spin" />}
                                {importJob.status === 'completed' && <CheckCircle className="h-5 w-5 text-emerald-500" />}
                                {importJob.status === 'failed' && <XCircle className="h-5 w-5 text-red-500" />}
                                <h3 className="font-semibold text-slate-900">Import {importJob.status}</h3>
                            </div>
                            {importJob.totalRows && (
                                <div className="mb-3">
                                    <div className="flex justify-between text-xs text-slate-500 mb-1">
                                        <span>{importJob.processedRows || 0} of {importJob.totalRows} rows</span>
                                        <span>{Math.round(((importJob.processedRows || 0) / importJob.totalRows) * 100)}%</span>
                                    </div>
                                    <div className="h-2 bg-slate-100 rounded-full overflow-hidden">
                                        <div
                                            className="h-full bg-primary-500 rounded-full transition-all"
                                            style={{ width: `${Math.round(((importJob.processedRows || 0) / importJob.totalRows) * 100)}%` }}
                                        />
                                    </div>
                                </div>
                            )}
                            {importJob.errorRows && importJob.errorRows > 0 && (
                                <p className="text-xs text-amber-600">{importJob.errorRows} rows had errors and were skipped</p>
                            )}
                        </div>
                    )}
                </div>
            )}

            {/* Export tab */}
            {activeTab === 'export' && (
                <div className="space-y-5">
                    <div className="bg-white border border-slate-200 rounded-xl p-6">
                        <h2 className="font-semibold text-slate-900 mb-2">Export {entityType}</h2>
                        <p className="text-sm text-slate-500 mb-4">Download all your {entityType} data as a CSV file</p>
                        <Button onClick={handleExport} disabled={exporting} className="flex items-center gap-2">
                            {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
                            {exporting ? 'Preparing export...' : `Export ${entityType} to CSV`}
                        </Button>
                    </div>

                    {/* Export Job Status */}
                    {exportJob && (
                        <div className="bg-white border border-slate-200 rounded-xl p-5">
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    {exportJob.status === 'processing' && <Loader2 className="h-5 w-5 text-blue-500 animate-spin" />}
                                    {exportJob.status === 'completed' && <CheckCircle className="h-5 w-5 text-emerald-500" />}
                                    {exportJob.status === 'failed' && <XCircle className="h-5 w-5 text-red-500" />}
                                    <span className="font-medium text-slate-900 capitalize">{exportJob.status}</span>
                                </div>
                                {exportJob.status === 'completed' && (
                                    <Button onClick={handleDownload} className="flex items-center gap-2">
                                        <Download className="h-4 w-4" /> Download CSV
                                    </Button>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
