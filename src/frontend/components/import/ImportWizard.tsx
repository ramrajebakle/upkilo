'use client';

import { useState, useCallback } from 'react';
import {
    Upload, FileSpreadsheet, CheckCircle2, AlertCircle,
    ArrowRight, Loader2, X, Download, Table
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

interface ImportWizardProps {
    entityType: 'clients' | 'bookings';
    isOpen: boolean;
    onClose: () => void;
    onComplete?: (job: any) => void;
}

export function ImportWizard({ entityType, isOpen, onClose, onComplete }: ImportWizardProps) {
    const [step, setStep] = useState<1 | 2 | 3>(1);
    const [file, setFile] = useState<File | null>(null);
    const [analysis, setAnalysis] = useState<any>(null);
    const [mapping, setMapping] = useState<Record<string, string>>({});
    const [loading, setLoading] = useState(false);
    const [job, setJob] = useState<any>(null);
    const [error, setError] = useState<string | null>(null);

    const targetFields = entityType === 'clients'
        ? ['FirstName', 'LastName', 'Email', 'Phone', 'Notes']
        : ['ClientEmail', 'ServiceName', 'Date', 'Time', 'Duration'];

    const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const uploadedFile = e.target.files?.[0];
        if (!uploadedFile) return;

        setFile(uploadedFile);
        setLoading(true);
        setError(null);

        try {
            const res = await api.import.analyze(uploadedFile, entityType);
            setAnalysis(res.data);

            // Auto-mapping attempt
            const newMapping: Record<string, string> = {};
            res.data.headers.forEach((header: string) => {
                const match = targetFields.find(f =>
                    f.toLowerCase() === header.toLowerCase() ||
                    f.toLowerCase().includes(header.toLowerCase())
                );
                if (match) newMapping[match] = header;
            });
            setMapping(newMapping);
            setStep(2);
        } catch (err: any) {
            setError(err.response?.data || 'Failed to analyze file');
        } finally {
            setLoading(false);
        }
    };

    const startImport = async () => {
        if (!file) return;
        setLoading(true);
        setError(null);

        try {
            const res = await api.import.start(file, entityType, mapping);
            setJob(res.data);
            setStep(3);
            pollJobStatus(res.data.id);
        } catch (err: any) {
            setError(err.response?.data || 'Failed to start import');
            setLoading(false);
        }
    };

    const pollJobStatus = async (jobId: string) => {
        const interval = setInterval(async () => {
            try {
                const res = await api.import.getStatus(jobId);
                setJob(res.data);
                if (res.data.status === 'completed' || res.data.status === 'completed_with_errors' || res.data.status === 'failed') {
                    clearInterval(interval);
                    setLoading(false);
                    if (onComplete) onComplete(res.data);
                }
            } catch (err) {
                console.error('Failed to poll job status:', err);
                clearInterval(interval);
                setLoading(false);
            }
        }, 2000);
    };

    const downloadTemplate = async () => {
        try {
            const res = await api.import.getTemplate(entityType);
            const url = window.URL.createObjectURL(new Blob([res.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', `${entityType}-template.csv`);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (err) {
            console.error('Failed to download template:', err);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
            <div className="bg-white rounded-2xl w-full max-w-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
                {/* Header */}
                <div className="p-6 border-b flex items-center justify-between bg-gray-50">
                    <div>
                        <h2 className="text-xl font-bold text-gray-900">Import {entityType === 'clients' ? 'Clients' : 'Bookings'}</h2>
                        <p className="text-sm text-gray-500">Fast bulk data migration from CSV</p>
                    </div>
                    <button onClick={onClose} className="p-2 hover:bg-gray-200 rounded-full transition-colors">
                        <X className="h-5 w-5 text-gray-500" />
                    </button>
                </div>

                {/* Content */}
                <div className="flex-1 overflow-y-auto p-6">
                    {error && (
                        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl flex items-start gap-3 text-red-800">
                            <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" />
                            <p className="text-sm">{error}</p>
                        </div>
                    )}

                    {/* Stepper */}
                    <div className="flex items-center justify-between mb-8 px-4">
                        {[1, 2, 3].map((s) => (
                            <div key={s} className="flex items-center">
                                <div className={cn(
                                    "h-8 w-8 rounded-full flex items-center justify-center text-sm font-bold transition-colors",
                                    step === s ? "bg-primary-500 text-white" :
                                        step > s ? "bg-green-500 text-white" : "bg-gray-100 text-gray-400"
                                )}>
                                    {step > s ? <CheckCircle2 className="h-5 w-5" /> : s}
                                </div>
                                {s < 3 && (
                                    <div className={cn(
                                        "w-12 h-0.5 mx-2 bg-gray-100",
                                        step > s && "bg-green-500"
                                    )} />
                                )}
                            </div>
                        ))}
                    </div>

                    {/* Step 1: Upload */}
                    {step === 1 && (
                        <div className="space-y-6">
                            <div className="border-2 border-dashed border-gray-200 rounded-2xl p-10 flex flex-col items-center justify-center bg-gray-50 hover:bg-gray-100 transition-colors cursor-pointer relative">
                                <input
                                    type="file"
                                    accept=".csv"
                                    onChange={handleFileUpload}
                                    className="absolute inset-0 opacity-0 cursor-pointer"
                                    disabled={loading}
                                />
                                <div className="h-16 w-16 bg-blue-100 text-blue-600 rounded-full flex items-center justify-center mb-4">
                                    <Upload className="h-8 w-8" />
                                </div>
                                <h3 className="text-lg font-semibold text-gray-900">Choose a CSV file</h3>
                                <p className="text-sm text-gray-500 mb-6 text-center">Drag and drop your file here or click to browse</p>
                                <Button variant="outline" size="sm">
                                    Select File
                                </Button>
                            </div>

                            <div className="p-4 bg-amber-50 rounded-xl border border-amber-100 flex items-start gap-3">
                                <Table className="h-5 w-5 text-amber-600 shrink-0 mt-0.5" />
                                <div className="text-xs text-amber-800">
                                    <p className="font-bold mb-1">Recommended Format</p>
                                    <p>Make sure your CSV has a header row. You'll be able to map columns in the next step.</p>
                                    <button onClick={downloadTemplate} className="mt-2 text-amber-900 font-bold underline flex items-center gap-1">
                                        <Download className="h-3 w-3" /> Download Sample CSV
                                    </button>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Step 2: Mapping */}
                    {step === 2 && analysis && (
                        <div className="space-y-6">
                            <div className="bg-gray-900 text-white p-4 rounded-xl overflow-x-auto">
                                <p className="text-xs font-bold text-gray-400 mb-2 uppercase tracking-widest">CSV Preview (Top 3 rows)</p>
                                <table className="w-full text-xs">
                                    <thead>
                                        <tr>
                                            {analysis.headers.map((h: string) => (
                                                <th key={h} className="text-left px-2 py-1 border-b border-gray-800 font-bold">{h}</th>
                                            ))}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {analysis.previewRows.slice(0, 3).map((row: any, i: number) => (
                                            <tr key={i}>
                                                {analysis.headers.map((h: string) => (
                                                    <td key={h} className="px-2 py-1 truncate max-w-[120px]">{row[h]}</td>
                                                ))}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-bold text-gray-900">Map your columns</h3>
                                <div className="grid gap-3">
                                    {targetFields.map(field => (
                                        <div key={field} className="flex items-center justify-between p-3 border rounded-xl bg-gray-50">
                                            <span className="text-sm font-medium text-gray-700">{field}</span>
                                            <select
                                                className="bg-white border border-gray-200 rounded-lg text-sm p-1.5 focus:ring-2 focus:ring-primary-500 outline-none"
                                                value={mapping[field] || ''}
                                                onChange={(e) => setMapping({ ...mapping, [field]: e.target.value })}
                                            >
                                                <option value="">Don't Import</option>
                                                {analysis.headers.map((h: string) => (
                                                    <option key={h} value={h}>{h}</option>
                                                ))}
                                            </select>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Step 3: Processing */}
                    {step === 3 && job && (
                        <div className="space-y-8 flex flex-col items-center py-6 text-center">
                            {job.status === 'processing' || job.status === 'pending' ? (
                                <>
                                    <div className="relative">
                                        <Loader2 className="h-20 w-20 text-primary-500 animate-spin" />
                                        <div className="absolute inset-0 flex items-center justify-center font-bold text-lg">
                                            {Math.round((job.processedRows / (job.totalRows || 1)) * 100)}%
                                        </div>
                                    </div>
                                    <div>
                                        <h3 className="text-xl font-bold text-gray-900">Importing Data...</h3>
                                        <p className="text-gray-500 mt-1">Processing {job.processedRows} of {job.totalRows} records</p>
                                    </div>
                                </>
                            ) : job.status === 'completed' || job.status === 'completed_with_errors' ? (
                                <>
                                    <div className="h-20 w-20 bg-green-100 text-green-600 rounded-full flex items-center justify-center">
                                        <CheckCircle2 className="h-12 w-12" />
                                    </div>
                                    <div>
                                        <h3 className="text-xl font-bold text-gray-900">Import Complete!</h3>
                                        <p className="text-gray-500 mt-1">Successfully imported {job.successfulRows} records.</p>
                                        {job.failedRows > 0 && (
                                            <p className="text-red-600 mt-2 font-medium flex items-center justify-center gap-1">
                                                <AlertCircle className="h-4 w-4" /> {job.failedRows} records failed to import.
                                            </p>
                                        )}
                                    </div>
                                </>
                            ) : (
                                <>
                                    <div className="h-20 w-20 bg-red-100 text-red-600 rounded-full flex items-center justify-center">
                                        <AlertCircle className="h-12 w-12" />
                                    </div>
                                    <div>
                                        <h3 className="text-xl font-bold text-gray-900">Import Failed</h3>
                                        <p className="text-gray-500 mt-1">An unexpected error occurred during processing.</p>
                                    </div>
                                </>
                            )}

                            {/* Stats Grid */}
                            <div className="grid grid-cols-2 gap-4 w-full max-w-sm mt-4">
                                <div className="p-4 bg-gray-50 rounded-2xl border">
                                    <p className="text-2xl font-bold text-gray-900">{job.totalRows}</p>
                                    <p className="text-xs text-gray-500 uppercase font-bold">Total</p>
                                </div>
                                <div className="p-4 bg-gray-50 rounded-2xl border">
                                    <p className="text-2xl font-bold text-green-600">{job.successfulRows}</p>
                                    <p className="text-xs text-gray-500 uppercase font-bold">Success</p>
                                </div>
                            </div>
                        </div>
                    )}
                </div>

                {/* Footer */}
                <div className="p-6 border-t bg-gray-50 flex items-center justify-between">
                    <div>
                        {step === 2 && (
                            <button
                                onClick={() => setStep(1)}
                                className="text-sm font-medium text-gray-500 hover:text-gray-700"
                                disabled={loading}
                            >
                                Back to Upload
                            </button>
                        )}
                    </div>
                    <div className="flex gap-3">
                        <Button variant="outline" onClick={onClose} disabled={loading}>
                            Cancel
                        </Button>
                        {step === 2 && (
                            <Button onClick={startImport} disabled={loading}>
                                {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <ArrowRight className="h-4 w-4 mr-2" />}
                                Start Import
                            </Button>
                        )}
                        {step === 3 && (job.status === 'completed' || job.status === 'completed_with_errors' || job.status === 'failed') && (
                            <Button onClick={onClose}>
                                Done
                            </Button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
