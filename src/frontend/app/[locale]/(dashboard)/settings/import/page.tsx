'use client';

import { useState, useRef } from 'react';

type Step = 'upload' | 'preview' | 'execute' | 'done';

interface PreviewData {
  sessionId: string;
  platform: string;
  totalParsed: number;
  toImport: number;
  duplicatesSkipped: number;
  sample: { firstName: string; lastName: string; email?: string; phone?: string }[];
}

export default function MigrationWizardPage() {
  const [step, setStep] = useState<Step>('upload');
  const [platform, setPlatform] = useState('');
  const [preview, setPreview] = useState<PreviewData | null>(null);
  const [importing, setImporting] = useState(false);
  const [importedCount, setImportedCount] = useState(0);
  const [error, setError] = useState('');
  const fileRef = useRef<HTMLInputElement>(null);

  const authHeaders = () => ({ Authorization: `Bearer ${localStorage.getItem('token')}` });

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0];
    if (!file) { setError('Please choose a CSV file.'); return; }
    setError('');

    const form = new FormData();
    form.append('file', file);
    const query = platform ? `?platform=${platform}` : '';

    const res = await fetch(`/api/v1/migration/upload${query}`, {
      method: 'POST',
      headers: authHeaders(),
      body: form
    });

    if (!res.ok) {
      const json = await res.json().catch(() => ({}));
      setError(json.message || 'Upload failed.');
      return;
    }

    const json = await res.json();
    const sessionId = json.data?.sessionId;

    const prev = await fetch(`/api/v1/migration/${sessionId}/preview`, { headers: authHeaders() });
    const prevJson = await prev.json();
    setPreview(prevJson.data);
    setStep('preview');
  };

  const handleExecute = async () => {
    if (!preview) return;
    setImporting(true);
    setError('');

    const res = await fetch(`/api/v1/migration/${preview.sessionId}/execute`, {
      method: 'POST',
      headers: authHeaders()
    });

    if (!res.ok) {
      const json = await res.json().catch(() => ({}));
      setError(json.message || 'Import failed.');
      setImporting(false);
      return;
    }

    const json = await res.json();
    setImportedCount(json.data?.imported ?? 0);
    setStep('done');
    setImporting(false);
  };

  const reset = () => {
    setStep('upload');
    setPreview(null);
    setImportedCount(0);
    setError('');
    if (fileRef.current) fileRef.current.value = '';
  };

  const steps: Step[] = ['upload', 'preview', 'execute', 'done'];
  const stepIdx = steps.indexOf(step);

  return (
    <div className="max-w-2xl mx-auto py-10 px-4">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Import Clients from Another Platform</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">Supports Mindbody, Vagaro, Acuity, or any standard CSV export.</p>
      </div>

      {/* Steps */}
      <div className="flex items-center gap-2 mb-8">
        {['Upload', 'Preview', 'Confirm', 'Done'].map((label, i) => (
          <div key={label} className="flex items-center gap-2">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-colors ${i === stepIdx ? 'bg-primary-600 text-white' : i < stepIdx ? 'bg-green-500 text-white' : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400'}`}>
              {i < stepIdx ? '✓' : i + 1}
            </div>
            <span className={`text-sm hidden sm:block ${i === stepIdx ? 'text-primary-700 dark:text-primary-400 font-semibold' : 'text-gray-400'}`}>{label}</span>
            {i < 3 && <div className="w-6 h-px bg-gray-300 dark:bg-gray-700" />}
          </div>
        ))}
      </div>

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-800 rounded-xl px-4 py-3 mb-6 text-sm">{error}</div>
      )}

      {/* Upload */}
      {step === 'upload' && (
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-8 shadow-sm">
          <h2 className="text-lg font-semibold text-gray-800 dark:text-white mb-5">Upload CSV Export</h2>

          <div className="mb-5">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Platform</label>
            <select value={platform} onChange={e => setPlatform(e.target.value)}
              className="w-full border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm">
              <option value="">Auto-detect from headers</option>
              <option value="mindbody">Mindbody</option>
              <option value="vagaro">Vagaro</option>
              <option value="acuity">Acuity Scheduling</option>
              <option value="generic">Other / Generic CSV</option>
            </select>
          </div>

          <div
            onClick={() => fileRef.current?.click()}
            className="border-2 border-dashed border-gray-300 dark:border-gray-700 rounded-xl p-10 text-center cursor-pointer hover:border-primary-400 hover:bg-primary-50/30 dark:hover:bg-primary-900/10 transition-all"
          >
            <div className="text-4xl mb-2">📁</div>
            <p className="text-sm text-gray-600 dark:text-gray-400">Click to select CSV, or drag and drop</p>
            <p className="text-xs text-foreground-muted mt-1">Max 10 MB</p>
            <input ref={fileRef} type="file" accept=".csv" className="hidden" />
          </div>

          <button onClick={handleUpload}
            className="w-full mt-6 bg-primary-600 text-white py-3 rounded-xl font-semibold hover:bg-primary-700 transition-colors">
            Upload & Analyse →
          </button>

          <div className="mt-6 bg-gray-50 dark:bg-gray-800 rounded-xl p-4">
            <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 mb-2">How to export your data:</p>
            <ul className="text-xs text-slate-300 space-y-1">
              <li><strong>Mindbody:</strong> Reports → Client List → Export</li>
              <li><strong>Vagaro:</strong> Reports → Client Report → Download CSV</li>
              <li><strong>Acuity:</strong> Business Settings → Clients → Export</li>
            </ul>
          </div>
        </div>
      )}

      {/* Preview */}
      {step === 'preview' && preview && (
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-8 shadow-sm">
          <h2 className="text-lg font-semibold text-gray-800 dark:text-white mb-5">Review Import</h2>

          <div className="grid grid-cols-3 gap-4 mb-6">
            <div className="bg-primary-50 dark:bg-primary-900/20 rounded-xl p-4 text-center">
              <p className="text-2xl font-bold text-primary-700 dark:text-primary-400">{preview.totalParsed}</p>
              <p className="text-xs text-primary-600 dark:text-primary-500 mt-1">Total in CSV</p>
            </div>
            <div className="bg-green-50 dark:bg-green-900/20 rounded-xl p-4 text-center">
              <p className="text-2xl font-bold text-green-700 dark:text-green-400">{preview.toImport}</p>
              <p className="text-xs text-green-600 dark:text-green-500 mt-1">Will import</p>
            </div>
            <div className="bg-yellow-50 dark:bg-yellow-900/20 rounded-xl p-4 text-center">
              <p className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">{preview.duplicatesSkipped}</p>
              <p className="text-xs text-yellow-600 dark:text-yellow-500 mt-1">Already exist</p>
            </div>
          </div>

          <p className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-3">Sample records</p>
          <div className="space-y-2 mb-6">
            {preview.sample.map((c, i) => (
              <div key={i} className="flex items-center gap-3 bg-gray-50 dark:bg-gray-800 rounded-lg px-4 py-2">
                <div className="w-7 h-7 rounded-full bg-primary-100 dark:bg-primary-900 text-primary-700 dark:text-primary-300 flex items-center justify-center text-xs font-bold">
                  {(c.firstName?.[0] ?? '?')}{(c.lastName?.[0] ?? '')}
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-800 dark:text-white">{c.firstName} {c.lastName}</p>
                  <p className="text-xs text-foreground-secondary">{c.email || c.phone || '—'}</p>
                </div>
              </div>
            ))}
          </div>

          <p className="text-xs text-foreground-secondary mb-4">
            Platform: <span className="font-semibold capitalize">{preview.platform}</span>.
            Duplicates matched by email and phone are skipped.
          </p>

          <div className="flex gap-3">
            <button onClick={() => setStep('upload')} className="flex-1 border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 py-3 rounded-xl font-semibold hover:bg-gray-50 dark:hover:bg-gray-800">
              ← Back
            </button>
            <button onClick={() => setStep('execute')} className="flex-1 bg-primary-600 text-white py-3 rounded-xl font-semibold hover:bg-primary-700">
              Import {preview.toImport} Clients →
            </button>
          </div>
        </div>
      )}

      {/* Confirm */}
      {step === 'execute' && preview && (
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-8 shadow-sm text-center">
          <div className="text-5xl mb-4">⚡</div>
          <h2 className="text-lg font-semibold text-gray-800 dark:text-white mb-2">Ready to import {preview.toImport} clients?</h2>
          <p className="text-gray-500 dark:text-gray-400 text-sm mb-8">You'll receive an email confirmation when done.</p>

          <button onClick={handleExecute} disabled={importing}
            className="w-full bg-green-600 text-white py-3 rounded-xl font-semibold hover:bg-green-700 disabled:opacity-50 flex items-center justify-center gap-2">
            {importing && <span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full" />}
            {importing ? 'Importing...' : 'Confirm Import'}
          </button>
        </div>
      )}

      {/* Done */}
      {step === 'done' && (
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-8 shadow-sm text-center">
          <div className="text-6xl mb-4">🎉</div>
          <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-2">Import Complete!</h2>
          <p className="text-gray-500 dark:text-gray-400 mb-8">
            {importedCount} client{importedCount !== 1 ? 's' : ''} imported. Check your email for confirmation.
          </p>
          <div className="flex gap-3 justify-center">
            <a href="/dashboard/clients" className="bg-primary-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-primary-700">
              View Clients
            </a>
            <button onClick={reset} className="border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 px-6 py-3 rounded-xl font-semibold hover:bg-gray-50 dark:hover:bg-gray-800">
              Import Another File
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
