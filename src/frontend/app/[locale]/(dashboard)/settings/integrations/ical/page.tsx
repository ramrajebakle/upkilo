"use client";

import React, { useState, useEffect } from "react";
import { Calendar, Plus, Trash2, Copy, CheckCircle2, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface IcalToken { id: string; name: string; token: string; createdAt: string; lastUsedAt?: string; }

export default function IcalTokensPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [tokens, setTokens] = useState<IcalToken[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [copied, setCopied] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/calendarsynctokens").catch(() => ({ data: [] }));
      setTokens(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const create = async () => {
    if (!newName.trim()) return;
    setCreating(true);
    try {
      await apiClient.post("/api/v1/calendarsynctokens", { name: newName });
      toastSuccess("iCal token created"); setNewName(""); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Create failed"); }
    finally { setCreating(false); }
  };

  const remove = async (id: string) => {
    setDeleting(id);
    try { await apiClient.delete(`/api/v1/calendarsynctokens/${id}`); toastSuccess("Token revoked"); setTokens((t) => t.filter((x) => x.id !== id)); }
    catch { toastError("Revoke failed"); }
    finally { setDeleting(null); }
  };

  const copy = (text: string, key: string) => {
    navigator.clipboard.writeText(text).then(() => { setCopied(key); setTimeout(() => setCopied(null), 1500); });
  };

  const icalUrl = (token: string) =>
    typeof window !== "undefined"
      ? `${window.location.origin}/api/v1/calendarintegrations/ical/${token}.ics`
      : `/api/v1/calendarintegrations/ical/${token}.ics`;

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">iCal / Calendar Tokens <Calendar className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Generate private iCal feed URLs to sync your bookings with Google Calendar, Apple Calendar, or Outlook.</p>
      </header>

      <div className="p-4 rounded-xl bg-surface-50 border border-surface-200">
        <p className="text-sm font-medium text-text-primary mb-1">How iCal sync works</p>
        <ol className="text-xs text-text-secondary space-y-1 list-decimal ml-4">
          <li>Generate a unique token below</li>
          <li>Copy the iCal URL and add it to your calendar app ("Subscribe to calendar")</li>
          <li>Your app will automatically sync bookings — typically every 30–60 minutes</li>
        </ol>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Plus size={15} /> Generate New Token</CardTitle></CardHeader>
        <CardContent>
          <div className="flex gap-3">
            <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="Label (e.g. Google Calendar, iPhone)"
              className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
              onKeyDown={(e) => e.key === "Enter" && create()} />
            <Button variant="primary" leftIcon={creating ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
              onClick={create} disabled={!newName.trim() || creating}>{creating ? "Generating…" : "Generate"}</Button>
          </div>
        </CardContent>
      </Card>

      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold text-text-primary">Active Tokens ({tokens.length})</h2>
          <Button variant="outline" size="sm" leftIcon={<RefreshCw size={11} />} onClick={load} disabled={loading} />
        </div>

        {loading ? <div className="flex justify-center py-6"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
          : tokens.length === 0 ? (
            <Card><CardContent className="text-center py-8">
              <Calendar className="h-8 w-8 mx-auto mb-3 text-text-tertiary opacity-25" />
              <p className="text-sm text-text-tertiary">No iCal tokens yet</p>
            </CardContent></Card>
          ) : (
            <div className="space-y-3">
              {tokens.map((t) => {
                const url = icalUrl(t.token);
                return (
                  <Card key={t.id}>
                    <CardContent className="pt-4 pb-4">
                      <div className="flex items-start gap-3">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-1">
                            <p className="text-sm font-semibold text-text-primary">{t.name}</p>
                            <span className="text-xs text-text-tertiary">Created {new Date(t.createdAt).toLocaleDateString()}</span>
                            {t.lastUsedAt && <span className="text-xs text-green-600">Last synced {new Date(t.lastUsedAt).toLocaleDateString()}</span>}
                          </div>
                          <div className="flex items-center gap-2 mt-1.5">
                            <code className="text-xs bg-surface-100 text-text-secondary px-2 py-1 rounded font-mono truncate max-w-xs">{url}</code>
                            <button onClick={() => copy(url, t.id)} className="text-text-tertiary hover:text-ai-500 flex-shrink-0">
                              {copied === t.id ? <CheckCircle2 size={13} className="text-green-500" /> : <Copy size={13} />}
                            </button>
                          </div>
                        </div>
                        <Button variant="outline" size="sm"
                          leftIcon={deleting === t.id ? <Loader2 size={11} className="animate-spin" /> : <Trash2 size={11} className="text-red-500" />}
                          onClick={() => remove(t.id)} disabled={!!deleting}>Revoke</Button>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          )}
      </div>
    </div>
  );
}
