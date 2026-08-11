'use client';

import { useState } from 'react';
import {
    Database, ArrowRight, Loader2, X, AlertCircle,
    CheckCircle2, Server, Users, Calendar, Key
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

interface MigrationWizardProps {
    isOpen: boolean;
    onClose: () => void;
    onComplete?: (job: any) => void;
}

type Provider = 'calendly' | 'acuity' | null;

export function MigrationWizard({ isOpen, onClose, onComplete }: MigrationWizardProps) {
    const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
    const [provider, setProvider] = useState<Provider>(null);
    const [apiKey, setApiKey] = useState('');
    const [extraCreds, setExtraCreds] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [overview, setOverview] = useState<any>(null);
    const [job, setJob] = useState<any>(null);

    const [selection, setSelection] = useState({
        services: true,
        staff: true,
        bookings: true
    });

    const handleConnect = async () => {
        if (!provider || !apiKey) return;
        setLoading(true);
        setError(null);

        try {
            const res = await api.migration.getOverview({
                provider,
                apiKey,
                extraCredentials: extraCreds
            });
            setOverview(res.data);
            setStep(3);
        } catch (err: any) {
            setError(err.response?.data || 'Connection failed. Please check your credentials.');
        } finally {
            setLoading(false);
        }
    };

    const startMigration = async () => {
        if (!provider || !apiKey) return;
        setLoading(true);

        try {
            const res = await api.migration.start({
                provider,
                apiKey,
                extraCredentials: extraCreds,
                importServices: selection.services,
                importStaff: selection.staff,
                importBookings: selection.bookings
            });
            setJob(res.data);
            setStep(4);
            pollStatus(res.data.id);
        } catch (err: any) {
            setError(err.response?.data || 'Failed to start migration');
            setLoading(false);
        }
    };

    const pollStatus = async (jobId: string) => {
        const interval = setInterval(async () => {
            try {
                const res = await api.import.getStatus(jobId);
                setJob(res.data);
                if (res.data.status === 'completed' || res.data.status === 'failed') {
                    clearInterval(interval);
                    setLoading(false);
                    if (onComplete) onComplete(res.data);
                }
            } catch (err) {
                clearInterval(interval);
            }
        }, 2000);
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
            <div className="bg-white rounded-2xl w-full max-w-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
                {/* Header */}
                <div className="p-6 border-b flex items-center justify-between bg-gray-50">
                    <div>
                        <h2 className="text-xl font-bold text-gray-900">Migration Wizard</h2>
                        <p className="text-sm text-gray-500">Import your data from other platforms</p>
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

                    {/* Step 1: Provider Selection */}
                    {step === 1 && (
                        <div className="space-y-4">
                            <h3 className="font-semibold text-gray-900 mb-2">Select your current platform</h3>
                            <div className="grid grid-cols-2 gap-4">
                                <button
                                    onClick={() => { setProvider('calendly'); setStep(2); }}
                                    className="p-6 border-2 rounded-2xl hover:border-primary-500 hover:bg-primary-50 transition-all flex flex-col items-center gap-3 text-center"
                                >
                                    <div className="h-12 w-12 bg-blue-100 text-blue-600 rounded-xl flex items-center justify-center">
                                        <Calendar className="h-6 w-6" />
                                    </div>
                                    <span className="font-bold">Calendly</span>
                                    <span className="text-xs text-gray-500">Import event types and scheduled meetings</span>
                                </button>
                                <button
                                    onClick={() => { setProvider('acuity'); setStep(2); }}
                                    className="p-6 border-2 rounded-2xl hover:border-primary-500 hover:bg-primary-50 transition-all flex flex-col items-center gap-3 text-center"
                                >
                                    <div className="h-12 w-12 bg-primary-100 text-primary-600 rounded-xl flex items-center justify-center">
                                        <Database className="h-6 w-6" />
                                    </div>
                                    <span className="font-bold">Acuity</span>
                                    <span className="text-xs text-gray-500">Full sync of appointments, staff, and services</span>
                                </button>
                            </div>
                        </div>
                    )}

                    {/* Step 2: Credentials */}
                    {step === 2 && (
                        <div className="space-y-6">
                            <div className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl mb-4">
                                <div className="h-10 w-10 bg-white rounded-lg flex items-center justify-center shadow-sm">
                                    {provider === 'calendly' ? <Calendar className="h-5 w-5 text-blue-600" /> : <Database className="h-5 w-5 text-primary-600" />}
                                </div>
                                <div>
                                    <h4 className="font-bold capitalize">{provider} Credentials</h4>
                                    <p className="text-xs text-gray-500">Connect your account via API key</p>
                                </div>
                            </div>

                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">API Personal Token / Key</label>
                                    <div className="relative">
                                        <Key className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                                        <input
                                            type="password"
                                            value={apiKey}
                                            onChange={(e) => setApiKey(e.target.value)}
                                            placeholder="Paste your API key here"
                                            className="w-full pl-10 pr-4 py-2 border rounded-xl focus:ring-2 focus:ring-primary-500 outline-none"
                                        />
                                    </div>
                                </div>

                                {provider === 'acuity' && (
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 mb-1">User ID / Serial Number</label>
                                        <input
                                            type="text"
                                            value={extraCreds}
                                            onChange={(e) => setExtraCreds(e.target.value)}
                                            placeholder="e.g. 12345678"
                                            className="w-full px-4 py-2 border rounded-xl focus:ring-2 focus:ring-primary-500 outline-none"
                                        />
                                    </div>
                                )}
                            </div>
                        </div>
                    )}

                    {/* Step 3: Mapping & Preview */}
                    {step === 3 && overview && (
                        <div className="space-y-6">
                            <div className="p-4 bg-emerald-50 border border-emerald-100 rounded-xl flex items-center gap-3 text-emerald-800">
                                <CheckCircle2 className="h-5 w-5" />
                                <p className="text-sm font-medium">Successfully connected to {provider}!</p>
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-bold text-gray-900">What would you like to import?</h3>
                                <div className="grid gap-3">
                                    <div className="flex items-center justify-between p-4 border rounded-xl bg-gray-50">
                                        <div className="flex items-center gap-3">
                                            <Server className="h-5 w-5 text-gray-400" />
                                            <div>
                                                <p className="text-sm font-bold">Services ({overview.serviceCount})</p>
                                                <p className="text-xs text-gray-500 truncate max-w-[200px]">{overview.foundServices.join(', ')}</p>
                                            </div>
                                        </div>
                                        <input
                                            type="checkbox"
                                            checked={selection.services}
                                            onChange={() => setSelection({ ...selection, services: !selection.services })}
                                            className="h-5 w-5 accent-primary-500"
                                        />
                                    </div>

                                    <div className="flex items-center justify-between p-4 border rounded-xl bg-gray-50">
                                        <div className="flex items-center gap-3">
                                            <Users className="h-5 w-5 text-gray-400" />
                                            <div>
                                                <p className="text-sm font-bold">Staff Members ({overview.staffCount})</p>
                                                <p className="text-xs text-gray-500 truncate max-w-[200px]">{overview.foundStaff.join(', ')}</p>
                                            </div>
                                        </div>
                                        <input
                                            type="checkbox"
                                            checked={selection.staff}
                                            onChange={() => setSelection({ ...selection, staff: !selection.staff })}
                                            className="h-5 w-5 accent-primary-500"
                                        />
                                    </div>

                                    <div className="flex items-center justify-between p-4 border rounded-xl bg-gray-50">
                                        <div className="flex items-center gap-3">
                                            <Calendar className="h-5 w-5 text-gray-400" />
                                            <div>
                                                <p className="text-sm font-bold">Bookings & History ({overview.bookingCount})</p>
                                                <p className="text-xs text-gray-500">Complete historical migration</p>
                                            </div>
                                        </div>
                                        <input
                                            type="checkbox"
                                            checked={selection.bookings}
                                            onChange={() => setSelection({ ...selection, bookings: !selection.bookings })}
                                            className="h-5 w-5 accent-primary-500"
                                        />
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Step 4: Progress */}
                    {step === 4 && job && (
                        <div className="flex flex-col items-center py-10 text-center gap-6">
                            <div className="relative">
                                <div className="h-24 w-24 rounded-full border-4 border-gray-100 flex items-center justify-center">
                                    <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
                                </div>
                                <div className="absolute top-0 right-0 h-8 w-8 bg-primary-500 text-white rounded-full flex items-center justify-center font-bold text-xs shadow-lg">
                                    {Math.round((job.processedRows / (job.totalRows || 1)) * 100)}%
                                </div>
                            </div>
                            <div>
                                <h3 className="text-xl font-bold text-gray-900">Migrating your data...</h3>
                                <p className="text-sm text-gray-500 mt-2">
                                    Processing record {job.processedRows} of {job.totalRows}
                                </p>
                                {job.status === 'completed' && (
                                    <div className="mt-4 p-4 bg-green-50 text-green-700 rounded-xl flex items-center gap-2 justify-center">
                                        <CheckCircle2 className="h-5 w-5" />
                                        <span>Migration Successful!</span>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>

                {/* Footer */}
                <div className="p-6 border-t bg-gray-50 flex items-center justify-between">
                    <div>
                        {step > 1 && step < 4 && (
                            <button
                                onClick={() => setStep((step - 1) as any)}
                                className="text-sm font-medium text-gray-500 hover:text-gray-700"
                                disabled={loading}
                            >
                                Back
                            </button>
                        )}
                    </div>
                    <div className="flex gap-3">
                        <Button variant="outline" onClick={onClose} disabled={loading}>
                            Cancel
                        </Button>
                        {step === 2 && (
                            <Button onClick={handleConnect} disabled={loading || !apiKey}>
                                {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <ArrowRight className="h-4 w-4 mr-2" />}
                                Connect
                            </Button>
                        )}
                        {step === 3 && (
                            <Button onClick={startMigration} disabled={loading}>
                                {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Database className="h-4 w-4 mr-2" />}
                                Run Migration
                            </Button>
                        )}
                        {step === 4 && job?.status === 'completed' && (
                            <Button onClick={onClose}>
                                Finish
                            </Button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
