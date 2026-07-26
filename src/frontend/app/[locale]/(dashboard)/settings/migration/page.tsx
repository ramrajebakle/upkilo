"use client";

import React, { useState } from "react";
import { Upload, ArrowRight, CheckCircle2, AlertCircle, Loader2, Database } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

type Step = "upload" | "preview" | "execute" | "done";

interface PreviewData {
  sessionId: string; totalRows: number; mappedFields: number; sampleRows: Record<string, string>[];
  errors: string[]; warnings: string[];
}

const PLATFORMS = [
  { value: "", label: "Auto-detect" }, { value: "mindbody", label: "Mindbody" }, { value: "vagaro", label: "Vagaro" },
  { value: "booker", label: "Booker" }, { value: "acuity", label: "Acuity Scheduling" }, { value: "square", label: "Square" },
  { value: "generic-csv", label: "Generic CSV" },
];

export default function MigrationPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [step, setStep] = useState<Step>("upload");
  const [platform, setPlatform] = useState("");
  const [uploading, setUploading] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [preview, setPreview] = useState<PreviewData | null>(null);

  const upload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const fd = new FormData();
      fd.append("file", file);
      const url = `/api/v1/migration/upload${platform ? `?platform=${platform}` : ""}`;
      const uploadRes = await apiClient.post(url, fd, { headers: { "Content-Type": "multipart/form-data" } });
      const sessionId = uploadRes.data?.data?.sessionId ?? uploadRes.data?.sessionId;
      if (!sessionId) throw new Error("No session ID returned");
      const previewRes = await apiClient.get(`/api/v1/migration/${sessionId}/preview`);
      setPreview({ sessionId, ...(previewRes.data?.data ?? previewRes.data ?? {}) });
      setStep("preview");
    } catch (err: any) { toastError(err?.response?.data?.error ?? "Upload failed"); }
    finally { setUploading(false); e.target.value = ""; }
  };

  const execute = async () => {
    if (!preview?.sessionId) return;
    setExecuting(true);
    try {
      await apiClient.post(`/api/v1/migration/${preview.sessionId}/execute`);
      toastSuccess("Migration complete!"); setStep("done");
    } catch (err: any) { toastError(err?.response?.data?.error ?? "Migration failed"); }
    finally { setExecuting(false); }
  };

  const reset = () => { setStep("upload"); setPreview(null); setPlatform(""); };

  const STEPS = [
    { key: "upload", label: "Upload File" },
    { key: "preview", label: "Preview" },
    { key: "execute", label: "Import" },
    { key: "done", label: "Complete" },
  ];

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Data Migration Wizard <Database className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Import clients, bookings, and history from your previous scheduling software.</p>
      </header>

      {/* Step indicator */}
      <div className="flex items-center gap-2">
        {STEPS.map((s, i) => (
          <React.Fragment key={s.key}>
            <div className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium ${step === s.key ? "bg-ai-500 text-white" : STEPS.findIndex((x) => x.key === step) > i ? "bg-green-100 text-green-700" : "bg-surface-100 text-text-tertiary"}`}>
              {STEPS.findIndex((x) => x.key === step) > i ? <CheckCircle2 size={11} /> : <span>{i + 1}</span>}
              {s.label}
            </div>
            {i < STEPS.length - 1 && <ArrowRight size={14} className="text-text-tertiary flex-shrink-0" />}
          </React.Fragment>
        ))}
      </div>

      {step === "upload" && (
        <Card>
          <CardHeader><CardTitle>Upload Your Data File</CardTitle><CardDescription>Supports CSV and Excel exports from most scheduling platforms</CardDescription></CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Source Platform (optional)</label>
              <select value={platform} onChange={(e) => setPlatform(e.target.value)}
                className="w-64 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                {PLATFORMS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
              </select>
            </div>
            <label className={`flex flex-col items-center justify-center border-2 border-dashed rounded-xl p-10 cursor-pointer transition-colors ${uploading ? "border-ai-300 bg-ai-50" : "border-surface-200 hover:border-ai-300 hover:bg-surface-50"}`}>
              <input type="file" accept=".csv,.xlsx,.xls" onChange={upload} disabled={uploading} className="hidden" />
              {uploading ? (
                <><Loader2 className="h-8 w-8 animate-spin text-ai-500 mb-3" /><p className="text-sm text-ai-600 font-medium">Uploading & analyzing…</p></>
              ) : (
                <><Upload className="h-8 w-8 text-text-tertiary mb-3" /><p className="text-sm font-medium text-text-primary">Click to upload CSV or Excel</p><p className="text-xs text-text-tertiary mt-1">Max 50 MB</p></>
              )}
            </label>
          </CardContent>
        </Card>
      )}

      {step === "preview" && preview && (
        <>
          <div className="grid grid-cols-3 gap-4">
            {[
              { label: "Total Rows", value: preview.totalRows?.toLocaleString() ?? "—" },
              { label: "Mapped Fields", value: preview.mappedFields ?? "—" },
              { label: "Issues", value: (preview.errors?.length ?? 0) + (preview.warnings?.length ?? 0) },
            ].map((m) => (
              <Card key={m.label}><CardContent className="pt-3 pb-3 text-center">
                <p className="text-2xl font-bold text-text-primary">{m.value}</p>
                <p className="text-xs text-text-tertiary mt-1">{m.label}</p>
              </CardContent></Card>
            ))}
          </div>

          {preview.errors?.length > 0 && (
            <Card className="border-red-200">
              <CardHeader><CardTitle className="flex items-center gap-2 text-red-700"><AlertCircle size={15} /> Errors ({preview.errors.length})</CardTitle></CardHeader>
              <CardContent className="space-y-1">
                {preview.errors.map((e, i) => <p key={i} className="text-xs text-red-700 bg-red-50 px-3 py-1.5 rounded">{e}</p>)}
              </CardContent>
            </Card>
          )}

          {preview.warnings?.length > 0 && (
            <Card className="border-amber-200">
              <CardHeader><CardTitle className="flex items-center gap-2 text-amber-700"><AlertCircle size={15} /> Warnings ({preview.warnings.length})</CardTitle></CardHeader>
              <CardContent className="space-y-1">
                {preview.warnings.map((w, i) => <p key={i} className="text-xs text-amber-700 bg-amber-50 px-3 py-1.5 rounded">{w}</p>)}
              </CardContent>
            </Card>
          )}

          {preview.sampleRows?.length > 0 && (
            <Card>
              <CardHeader><CardTitle>Sample Rows (preview)</CardTitle></CardHeader>
              <CardContent className="p-0 overflow-x-auto">
                <table className="w-full text-xs">
                  <thead><tr className="border-b border-surface-200">
                    {Object.keys(preview.sampleRows[0] ?? {}).map((k) => (
                      <th key={k} className="text-left py-2 px-3 font-semibold text-text-tertiary whitespace-nowrap">{k}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {preview.sampleRows.slice(0, 5).map((row, i) => (
                      <tr key={i} className="border-b border-surface-100">
                        {Object.values(row).map((v, j) => <td key={j} className="py-2 px-3 text-text-secondary truncate max-w-[120px]">{String(v)}</td>)}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}

          <div className="flex justify-between">
            <Button variant="outline" onClick={reset}>Back</Button>
            <Button variant="primary" leftIcon={executing ? <Loader2 size={14} className="animate-spin" /> : <Upload size={14} />}
              onClick={execute} disabled={executing || (preview.errors?.length ?? 0) > 0}>
              {executing ? "Importing…" : `Import ${preview.totalRows?.toLocaleString() ?? ""} Records`}
            </Button>
          </div>
        </>
      )}

      {step === "done" && (
        <Card>
          <CardContent className="text-center py-16">
            <CheckCircle2 className="h-16 w-16 mx-auto mb-4 text-green-500" />
            <h2 className="text-xl font-bold text-text-primary">Migration Complete!</h2>
            <p className="text-text-secondary mt-2">All records have been imported. Check your Clients, Bookings, and Staff pages.</p>
            <Button variant="primary" onClick={reset} className="mt-6">Start Another Migration</Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
