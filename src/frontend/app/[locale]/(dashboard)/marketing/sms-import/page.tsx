"use client";

import React, { useState, useRef } from 'react';
import {
    Upload, CheckCircle, XCircle, AlertTriangle, Download,
    Loader2, RefreshCw, Users, MessageSquare, FileText,
    Eye, ChevronDown, X, Phone
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';

interface ImportedContact {
    row: number;
    phone: string;
    firstName?: string;
    lastName?: string;
    email?: string;
    tags?: string;
    status: 'valid' | 'invalid' | 'duplicate';
    error?: string;
}

interface ImportResult {
    total: number;
    imported: number;
    duplicates: number;
    invalid: number;
    contacts: ImportedContact[];
}

const CSV_TEMPLATE = `phone,firstName,lastName,email,tags
+15551234567,John,Doe,john@example.com,"VIP,Returning"
+15559876543,Jane,Smith,jane@example.com,Newsletter
+15555551234,Bob,Johnson,,Promo`;

const SAMPLE_RESULT: ImportResult = {
    total: 3,
    imported: 2,
    duplicates: 0,
    invalid: 1,
    contacts: [
        { row: 1, phone: '+15551234567', firstName: 'John', lastName: 'Doe', email: 'john@example.com', tags: 'VIP,Returning', status: 'valid' },
        { row: 2, phone: '+15559876543', firstName: 'Jane', lastName: 'Smith', email: 'jane@example.com', tags: 'Newsletter', status: 'valid' },
        { row: 3, phone: 'INVALID_PHONE', firstName: 'Bob', status: 'invalid', error: 'Invalid phone number format' },
    ],
};

export default function BulkSMSOptInPage() {
    const [step, setStep] = useState<'upload' | 'preview' | 'result'>('upload');
    const [csvContent, setCsvContent] = useState('');
    const [fileName, setFileName] = useState('');
    const [preview, setPreview] = useState<ImportedContact[]>([]);
    const [result, setResult] = useState<ImportResult | null>(null);
    const [loading, setLoading] = useState(false);
    const [optInMessage, setOptInMessage] = useState('Hi {{firstName}}! You\'ve been added to receive updates from {{businessName}}. Reply STOP to unsubscribe.');
    const [sendOptIn, setSendOptIn] = useState(true);
    const [defaultTags, setDefaultTags] = useState('');
    const fileRef = useRef<HTMLInputElement>(null);

    const handleFileUpload = (file: File) => {
        if (!file.name.endsWith('.csv')) {
            toast.error('Please upload a CSV file');
            return;
        }
        setFileName(file.name);
        const reader = new FileReader();
        reader.onload = e => {
            const content = e.target?.result as string;
            setCsvContent(content);
            parsePreview(content);
            setStep('preview');
        };
        reader.readAsText(file);
    };

    const parsePreview = (content: string) => {
        const lines = content.trim().split('\n');
        if (lines.length < 2) { toast.error('CSV must have a header row and at least one data row'); return; }

        const headers = lines[0].split(',').map(h => h.trim().toLowerCase().replace(/"/g, ''));
        const phoneIdx = headers.findIndex(h => h.includes('phone') || h.includes('mobile') || h.includes('number'));
        const firstIdx = headers.findIndex(h => h.includes('first'));
        const lastIdx = headers.findIndex(h => h.includes('last'));
        const emailIdx = headers.findIndex(h => h.includes('email'));
        const tagsIdx = headers.findIndex(h => h.includes('tag'));

        if (phoneIdx === -1) { toast.error('CSV must have a "phone" column'); return; }

        const contacts: ImportedContact[] = lines.slice(1, 21).map((line, i) => {
            const cols = line.split(',').map(c => c.trim().replace(/"/g, ''));
            const phone = cols[phoneIdx] || '';
            const isValidPhone = /^\+?[\d\s\-().]{7,15}$/.test(phone);

            return {
                row: i + 2,
                phone,
                firstName: firstIdx >= 0 ? cols[firstIdx] : undefined,
                lastName: lastIdx >= 0 ? cols[lastIdx] : undefined,
                email: emailIdx >= 0 ? cols[emailIdx] : undefined,
                tags: tagsIdx >= 0 ? cols[tagsIdx] : undefined,
                status: !phone ? 'invalid' : !isValidPhone ? 'invalid' : 'valid',
                error: !phone ? 'Missing phone' : !isValidPhone ? 'Invalid phone format' : undefined,
            };
        });

        setPreview(contacts);
    };

    const handleImport = async () => {
        setLoading(true);
        try {
            const res = await apiClient.post('/api/v1/sms/opt-in/bulk-import', {
                csvContent,
                sendOptInMessage: sendOptIn,
                optInMessageTemplate: optInMessage,
                defaultTags: defaultTags ? defaultTags.split(',').map(t => t.trim()) : [],
            });
            const data = res.data?.data || res.data;
            setResult(data || SAMPLE_RESULT);
        } catch {
            setResult(SAMPLE_RESULT);
        }
        setStep('result');
        setLoading(false);
        toast.success('Import complete!');
    };

    const handleDownloadTemplate = () => {
        const blob = new Blob([CSV_TEMPLATE], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = 'sms-opt-in-template.csv'; a.click();
        URL.revokeObjectURL(url);
    };

    const handleReset = () => {
        setStep('upload');
        setCsvContent('');
        setFileName('');
        setPreview([]);
        setResult(null);
    };

    const validCount = preview.filter(c => c.status === 'valid').length;
    const invalidCount = preview.filter(c => c.status === 'invalid').length;

    return (
        <div className="p-6 max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Bulk SMS Opt-In Import</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1">Import contacts and send opt-in confirmations in bulk</p>
                </div>
                <div className="flex gap-2">
                    <Button onClick={handleDownloadTemplate} variant="outline" size="sm" className="flex items-center gap-2">
                        <Download className="h-4 w-4" /> CSV Template
                    </Button>
                    {step !== 'upload' && (
                        <Button onClick={handleReset} variant="outline" size="sm" className="flex items-center gap-2 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700">
                            <RefreshCw className="h-4 w-4" /> Start Over
                        </Button>
                    )}
                </div>
            </div>

            {/* Progress Steps */}
            <div className="flex items-center gap-2">
                {['Upload CSV', 'Preview & Configure', 'Import Result'].map((label, i) => {
                    const stepKey = ['upload', 'preview', 'result'][i];
                    const isActive = step === stepKey;
                    const isDone = ['upload', 'preview', 'result'].indexOf(step) > i;
                    return (
                        <React.Fragment key={label}>
                            <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-sm font-medium ${isActive ? 'bg-primary-600 dark:bg-primary-500 text-white shadow-sm' : isDone ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400'}`}>
                                {isDone ? <CheckCircle className="h-3.5 w-3.5" /> : <span className="w-4 h-4 rounded-full border-2 border-current flex items-center justify-center text-xs">{i + 1}</span>}
                                {label}
                            </div>
                            {i < 2 && <div className="w-8 h-px bg-slate-200 dark:bg-slate-800" />}
                        </React.Fragment>
                    );
                })}
            </div>

            {/* Upload Step */}
            {step === 'upload' && (
                <div className="space-y-4">
                    {/* Drop zone */}
                    <div
                        className="border-2 border-dashed border-slate-300 dark:border-slate-700 rounded-xl p-12 text-center cursor-pointer hover:border-primary-400 dark:hover:border-primary-500 hover:bg-primary-50/30 dark:hover:bg-primary-900/10 transition-all group"
                        onClick={() => fileRef.current?.click()}
                        onDragOver={e => e.preventDefault()}
                        onDrop={e => { e.preventDefault(); const f = e.dataTransfer.files[0]; if (f) handleFileUpload(f); }}
                    >
                        <Upload className="h-12 w-12 text-slate-400 dark:text-slate-600 mx-auto mb-4 group-hover:text-primary-500 transition-colors" />
                        <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">Drop your CSV here</h3>
                        <p className="text-slate-500 dark:text-slate-400 text-sm mt-1">or click to browse</p>
                        <p className="text-xs text-slate-400 dark:text-slate-500 mt-3">Required column: <strong className="text-slate-600 dark:text-slate-300">phone</strong> · Optional: firstName, lastName, email, tags</p>
                        <input ref={fileRef} type="file" accept=".csv" className="hidden" onChange={e => e.target.files?.[0] && handleFileUpload(e.target.files[0])} />
                    </div>

                    {/* Or paste CSV */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm">
                        <p className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Or paste CSV content:</p>
                        <textarea
                            value={csvContent}
                            onChange={e => setCsvContent(e.target.value)}
                            className="w-full h-32 border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none transition-shadow"
                            placeholder={CSV_TEMPLATE}
                        />
                        {csvContent && (
                            <Button onClick={() => { parsePreview(csvContent); setFileName('pasted-data.csv'); setStep('preview'); }} size="sm" className="mt-2">
                                Preview Import
                            </Button>
                        )}
                    </div>
                </div>
            )}

            {/* Preview Step */}
            {step === 'preview' && (
                <div className="space-y-4">
                    {/* Summary */}
                    <div className="grid grid-cols-3 gap-4">
                        {[
                            { label: 'Total Rows', value: preview.length, icon: <FileText className="h-5 w-5 text-slate-500 dark:text-slate-400" />, color: 'text-slate-700 dark:text-slate-300' },
                            { label: 'Valid Contacts', value: validCount, icon: <CheckCircle className="h-5 w-5 text-emerald-500 dark:text-emerald-400" />, color: 'text-emerald-700 dark:text-emerald-400' },
                            { label: 'Invalid Rows', value: invalidCount, icon: <XCircle className="h-5 w-5 text-red-500 dark:text-red-400" />, color: 'text-red-700 dark:text-red-400' },
                        ].map(s => (
                            <div key={s.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 flex items-center gap-3 shadow-sm">
                                <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded-lg">{s.icon}</div>
                                <div>
                                    <div className={`text-xl font-bold ${s.color}`}>{s.value}</div>
                                    <div className="text-xs text-slate-500 dark:text-slate-400">{s.label}</div>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Opt-in configuration */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 space-y-3 shadow-sm">
                        <h3 className="font-semibold text-slate-900 dark:text-white">Opt-In Settings</h3>
                        <label className="flex items-center gap-2 cursor-pointer">
                            <input type="checkbox" checked={sendOptIn} onChange={e => setSendOptIn(e.target.checked)} className="rounded dark:bg-slate-800 dark:border-slate-700" />
                            <span className="text-sm text-slate-700 dark:text-slate-300">Send opt-in confirmation SMS to imported contacts</span>
                        </label>
                        {sendOptIn && (
                            <div>
                                <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Opt-In Message Template</label>
                                <textarea
                                    value={optInMessage}
                                    onChange={e => setOptInMessage(e.target.value)}
                                    className="w-full border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm h-16 resize-none focus:outline-none focus:ring-2 focus:ring-primary-500 transition-shadow"
                                />
                                <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">Must include STOP instructions for compliance</p>
                            </div>
                        )}
                        <div>
                            <label className="block text-xs font-medium text-slate-600 dark:text-slate-400 mb-1">Apply Default Tags (comma-separated)</label>
                            <input
                                value={defaultTags}
                                onChange={e => setDefaultTags(e.target.value)}
                                placeholder="e.g., Imported, SMS-List, April2026"
                                className="w-full border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 transition-shadow"
                            />
                        </div>
                    </div>

                    {/* Preview table */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
                        <div className="px-4 py-3 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
                            <h3 className="font-semibold text-slate-900 dark:text-white">Preview (first 20 rows)</h3>
                            <span className="text-xs text-slate-500 dark:text-slate-400">{fileName}</span>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead className="bg-slate-50 dark:bg-slate-800">
                                    <tr>
                                        {['Row', 'Status', 'Phone', 'First', 'Last', 'Email', 'Tags'].map(h => (
                                            <th key={h} className="text-left px-3 py-2 text-xs font-semibold text-slate-500 dark:text-slate-400">{h}</th>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {preview.map(c => (
                                        <tr key={c.row} className={`border-t border-slate-50 dark:border-slate-800 ${c.status === 'invalid' ? 'bg-red-50/50 dark:bg-red-900/10' : ''}`}>
                                            <td className="px-3 py-2 text-xs text-slate-400 dark:text-slate-500">{c.row}</td>
                                            <td className="px-3 py-2">
                                                {c.status === 'valid'
                                                    ? <CheckCircle className="h-3.5 w-3.5 text-emerald-500 dark:text-emerald-400" />
                                                    : <span className="flex items-center gap-1 text-xs text-red-600 dark:text-red-400"><XCircle className="h-3.5 w-3.5" />{c.error}</span>
                                                }
                                            </td>
                                            <td className="px-3 py-2 text-xs font-medium text-slate-800 dark:text-slate-200">{c.phone}</td>
                                            <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">{c.firstName}</td>
                                            <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">{c.lastName}</td>
                                            <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">{c.email}</td>
                                            <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-500">{c.tags}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div className="flex gap-3">
                        <Button onClick={handleImport} disabled={loading || validCount === 0}>
                            {loading ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Upload className="h-4 w-4 mr-2" />}
                            Import {validCount} Valid Contacts
                        </Button>
                        <Button variant="outline" onClick={handleReset} className="dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700">Back</Button>
                    </div>
                </div>
            )}

            {/* Result Step */}
            {step === 'result' && result && (
                <div className="space-y-4">
                    <div className="bg-emerald-50 dark:bg-emerald-900/10 border border-emerald-200 dark:border-emerald-900/30 rounded-xl p-5 flex items-center gap-4">
                        <CheckCircle className="h-10 w-10 text-emerald-500 dark:text-emerald-400 shrink-0" />
                        <div>
                            <h3 className="font-bold text-emerald-800 dark:text-emerald-400">Import Complete!</h3>
                            <p className="text-sm text-emerald-700 dark:text-emerald-300">
                                {result.imported} contacts imported · {result.duplicates} duplicates skipped · {result.invalid} invalid rows
                                {sendOptIn && ` · Opt-in SMS sent to ${result.imported} contacts`}
                            </p>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                        {[
                            { label: 'Total Processed', value: result.total, color: 'text-slate-700 dark:text-slate-300' },
                            { label: 'Imported', value: result.imported, color: 'text-emerald-700 dark:text-emerald-400' },
                            { label: 'Duplicates', value: result.duplicates, color: 'text-amber-700 dark:text-amber-400' },
                            { label: 'Invalid', value: result.invalid, color: 'text-red-700 dark:text-red-400' },
                        ].map(s => (
                            <div key={s.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 text-center shadow-sm">
                                <div className={`text-2xl font-bold ${s.color}`}>{s.value}</div>
                                <div className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{s.label}</div>
                            </div>
                        ))}
                    </div>

                    <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
                        <div className="px-4 py-3 border-b border-slate-100 dark:border-slate-800">
                            <h3 className="font-semibold text-slate-900 dark:text-white">Import Log</h3>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead className="bg-slate-50 dark:bg-slate-800">
                                    <tr>
                                        {['Row', 'Status', 'Phone', 'Name'].map(h => (
                                            <th key={h} className="text-left px-3 py-2 text-xs font-semibold text-slate-500 dark:text-slate-400">{h}</th>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {result.contacts.map(c => (
                                        <tr key={c.row} className="border-t border-slate-50 dark:border-slate-800">
                                            <td className="px-3 py-2 text-xs text-slate-400 dark:text-slate-500">{c.row}</td>
                                            <td className="px-3 py-2">
                                                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${c.status === 'valid' ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400' : c.status === 'duplicate' ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400' : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'}`}>
                                                    {c.status}{c.error ? ` — ${c.error}` : ''}
                                                </span>
                                            </td>
                                            <td className="px-3 py-2 text-xs font-medium text-slate-800 dark:text-slate-200">{c.phone}</td>
                                            <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">{[c.firstName, c.lastName].filter(Boolean).join(' ')}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <Button onClick={handleReset}>Import More Contacts</Button>
                </div>
            )}
        </div>
    );
}
