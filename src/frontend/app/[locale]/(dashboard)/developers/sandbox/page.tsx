"use client";

import React, { useState, useEffect } from "react";
import { FlaskConical, Plus, Trash2, RotateCcw, Loader2, Copy, CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Sandbox { id: string; name: string; status: "active" | "inactive"; createdAt: string; baseUrl?: string; apiKey?: string; }

export default function SandboxPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [sandboxes, setSandboxes] = useState<Sandbox[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [resetting, setResetting] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [copied, setCopied] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/sandbox").catch(() => ({ data: [] }));
      setSandboxes(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const create = async () => {
    if (!newName.trim()) return;
    setCreating(true);
    try {
      await apiClient.post("/api/v1/sandbox", { name: newName });
      toastSuccess("Sandbox created"); setNewName(""); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Create failed"); }
    finally { setCreating(false); }
  };

  const reset = async (id: string) => {
    setResetting(id);
    try { await apiClient.post(`/api/v1/sandbox/${id}/reset`); toastSuccess("Sandbox reset"); load(); }
    catch { toastError("Reset failed"); }
    finally { setResetting(null); }
  };

  const remove = async (id: string) => {
    setDeleting(id);
    try { await apiClient.delete(`/api/v1/sandbox/${id}`); toastSuccess("Sandbox deleted"); setSandboxes((s) => s.filter((x) => x.id !== id)); }
    catch { toastError("Delete failed"); }
    finally { setDeleting(null); }
  };

  const copy = (text: string, key: string) => {
    navigator.clipboard.writeText(text).then(() => { setCopied(key); setTimeout(() => setCopied(null), 1500); });
  };

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Developer Sandboxes <FlaskConical className="text-primary" size={22} /></h1>
        <p className="text-text-secondary mt-1">Isolated test environments for building and testing integrations without affecting live data.</p>
      </header>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Plus size={15} /> Create Sandbox</CardTitle></CardHeader>
        <CardContent>
          <div className="flex gap-3">
            <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="Sandbox name (e.g. integration-test)"
              className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono"
              onKeyDown={(e) => e.key === "Enter" && create()} />
            <Button variant="primary" leftIcon={creating ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
              onClick={create} disabled={!newName.trim() || creating}>{creating ? "Creating…" : "Create"}</Button>
          </div>
        </CardContent>
      </Card>

      {loading ? <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
        : sandboxes.length === 0 ? (
          <Card><CardContent className="text-center py-10">
            <FlaskConical className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
            <p className="text-sm text-text-tertiary">No sandboxes yet. Create one to start testing.</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {sandboxes.map((s) => (
              <Card key={s.id}>
                <CardContent className="pt-4 pb-4">
                  <div className="flex items-start gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="text-sm font-semibold text-text-primary">{s.name}</h3>
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${s.status === "active" ? "text-green-700 bg-green-50" : "text-foreground-secondary bg-muted"}`}>{s.status}</span>
                        <span className="text-xs text-text-tertiary">Created {new Date(s.createdAt).toLocaleDateString()}</span>
                      </div>
                      {s.baseUrl && (
                        <div className="flex items-center gap-2 mt-2">
                          <code className="text-xs bg-surface-100 text-text-secondary px-2 py-1 rounded font-mono">{s.baseUrl}</code>
                          <button onClick={() => copy(s.baseUrl!, `url-${s.id}`)} className="text-text-tertiary hover:text-text-primary">
                            {copied === `url-${s.id}` ? <CheckCircle2 size={12} className="text-success-fg" /> : <Copy size={12} />}
                          </button>
                        </div>
                      )}
                      {s.apiKey && (
                        <div className="flex items-center gap-2 mt-1">
                          <code className="text-xs bg-surface-100 text-text-secondary px-2 py-1 rounded font-mono">{s.apiKey.slice(0, 12)}…</code>
                          <button onClick={() => copy(s.apiKey!, `key-${s.id}`)} className="text-text-tertiary hover:text-text-primary">
                            {copied === `key-${s.id}` ? <CheckCircle2 size={12} className="text-success-fg" /> : <Copy size={12} />}
                          </button>
                        </div>
                      )}
                    </div>
                    <div className="flex gap-1 flex-shrink-0">
                      <Button variant="outline" size="sm"
                        leftIcon={resetting === s.id ? <Loader2 size={11} className="animate-spin" /> : <RotateCcw size={11} />}
                        onClick={() => reset(s.id)} disabled={!!resetting || !!deleting}>Reset</Button>
                      <Button variant="outline" size="sm"
                        leftIcon={deleting === s.id ? <Loader2 size={11} className="animate-spin" /> : <Trash2 size={11} className="text-danger-fg" />}
                        onClick={() => remove(s.id)} disabled={!!resetting || !!deleting} />
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
