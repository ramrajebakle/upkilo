"use client";

import React, { useState, useEffect, useCallback } from "react";
import { AlertCircle, RefreshCw, RotateCcw, Trash2, Loader2, Activity } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface DlqMessage {
  id: string;
  queue: string;
  payload: string;
  error?: string;
  retryCount: number;
  createdAt: string;
  lastRetryAt?: string;
}

export default function DlqDashboardPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [messages, setMessages] = useState<DlqMessage[]>([]);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [actioning, setActioning] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/admin/dlq/messages").catch(() => ({ data: [] }));
      setMessages(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const retry = async (id: string) => {
    setActioning(id);
    try { await apiClient.post(`/api/admin/dlq/messages/${id}/retry`); toastSuccess("Message requeued"); load(); }
    catch { toastError("Retry failed"); }
    finally { setActioning(null); }
  };

  const remove = async (id: string) => {
    setActioning(id);
    try { await apiClient.delete(`/api/admin/dlq/messages/${id}`); toastSuccess("Message deleted"); setMessages((m) => m.filter((msg) => msg.id !== id)); }
    catch { toastError("Delete failed"); }
    finally { setActioning(null); }
  };

  const queues = [...new Set(messages.map((m) => m.queue))];

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Dead Letter Queue <AlertCircle className="text-red-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">Failed background messages awaiting retry or deletion.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="pt-5">
            <p className="text-xs text-text-secondary">Total Messages</p>
            <p className="text-2xl font-bold text-red-500 mt-1">{messages.length}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5">
            <p className="text-xs text-text-secondary">Queues Affected</p>
            <p className="text-2xl font-bold text-amber-500 mt-1">{queues.length}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-5">
            <p className="text-xs text-text-secondary">Avg Retry Count</p>
            <p className="text-2xl font-bold text-blue-500 mt-1">
              {messages.length ? Math.round(messages.reduce((a, m) => a + m.retryCount, 0) / messages.length) : 0}
            </p>
          </CardContent>
        </Card>
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : messages.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Activity className="h-10 w-10 mx-auto mb-3 text-green-400 opacity-60" />
            <p className="font-medium text-green-600">No messages in DLQ — all queues healthy</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {messages.map((m) => (
              <Card key={m.id} className={m.retryCount >= 5 ? "border-red-200" : ""}>
                <CardContent className="pt-4 pb-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-xs font-medium bg-surface-100 text-text-secondary px-2 py-0.5 rounded-full font-mono">{m.queue}</span>
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${m.retryCount >= 5 ? "text-red-600 bg-red-50" : "text-amber-600 bg-amber-50"}`}>
                          {m.retryCount} retries
                        </span>
                        <span className="text-xs text-text-tertiary">{new Date(m.createdAt).toLocaleDateString()}</span>
                      </div>
                      {m.error && <p className="text-xs text-red-600 font-mono mb-1 line-clamp-1">{m.error}</p>}
                      <button onClick={() => setExpanded(expanded === m.id ? null : m.id)}
                        className="text-xs text-ai-500 hover:text-ai-700">
                        {expanded === m.id ? "Hide payload" : "Show payload"}
                      </button>
                      {expanded === m.id && (
                        <pre className="mt-2 p-2 bg-slate-900 text-green-400 text-xs rounded-lg overflow-x-auto max-h-32 font-mono">
                          {(() => { try { return JSON.stringify(JSON.parse(m.payload), null, 2); } catch { return m.payload; } })()}
                        </pre>
                      )}
                    </div>
                    <div className="flex gap-1.5 flex-shrink-0">
                      <Button variant="outline" size="sm"
                        leftIcon={actioning === m.id ? <Loader2 size={12} className="animate-spin" /> : <RotateCcw size={12} />}
                        onClick={() => retry(m.id)} disabled={!!actioning}>Retry</Button>
                      <Button variant="outline" size="sm"
                        leftIcon={<Trash2 size={12} className="text-red-500" />}
                        onClick={() => remove(m.id)} disabled={!!actioning} />
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
