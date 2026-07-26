"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Shield, Monitor, Smartphone, Globe, Trash2, LogOut, Loader2, RefreshCw, Clock, MapPin } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Session { id: string; device?: string; browser?: string; ipAddress?: string; location?: string; lastActiveAt: string; isCurrent: boolean; }
interface LoginEntry { id: string; ipAddress?: string; userAgent?: string; location?: string; success: boolean; createdAt: string; }

export default function SessionsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [history, setHistory] = useState<LoginEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [revoking, setRevoking] = useState<string | null>(null);
  const [revokingAll, setRevokingAll] = useState(false);
  const [tab, setTab] = useState<"sessions" | "history">("sessions");
  const [timeout, setSessionTimeout] = useState(60);
  const [savingTimeout, setSavingTimeout] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [sessRes, histRes] = await Promise.all([
        apiClient.get("/api/v1/sessions").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/login-history").catch(() => ({ data: [] })),
      ]);
      setSessions(Array.isArray(sessRes.data) ? sessRes.data : sessRes.data?.data ?? []);
      setHistory(Array.isArray(histRes.data) ? histRes.data : histRes.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const revokeSession = async (id: string) => {
    setRevoking(id);
    try { await apiClient.delete(`/api/v1/sessions/${id}`); toastSuccess("Session revoked"); setSessions((s) => s.filter((x) => x.id !== id)); }
    catch { toastError("Failed to revoke session"); }
    finally { setRevoking(null); }
  };

  const revokeAll = async () => {
    setRevokingAll(true);
    try { await apiClient.delete("/api/v1/sessions/revoke-all"); toastSuccess("All other sessions revoked"); load(); }
    catch { toastError("Failed to revoke sessions"); }
    finally { setRevokingAll(false); }
  };

  const saveTimeout = async () => {
    setSavingTimeout(true);
    try { await apiClient.put("/api/v1/sessions/timeout", { timeoutMinutes: timeout }); toastSuccess("Session timeout updated"); }
    catch { toastError("Failed to update timeout"); }
    finally { setSavingTimeout(false); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Sessions & Login History <Shield className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Manage active sessions and review login history.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      <div className="flex gap-1 p-1 bg-surface-100 rounded-xl max-w-xs">
        {[
          { key: "sessions" as const, label: `Sessions (${sessions.length})` },
          { key: "history" as const, label: `Login History (${history.length})` },
        ].map((t) => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.key ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
            {t.label}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "sessions" && (
            <>
              <div className="flex items-center gap-4 p-4 bg-surface-50 rounded-xl border border-surface-200">
                <div className="flex items-center gap-2">
                  <label className="text-sm font-medium text-text-primary">Session timeout:</label>
                  <input type="number" min={5} max={1440} value={timeout} onChange={(e) => setSessionTimeout(parseInt(e.target.value) || 60)}
                    className="w-20 px-2 py-1.5 text-sm rounded-lg border border-surface-200 bg-white text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                  <span className="text-sm text-text-tertiary">minutes</span>
                </div>
                <Button size="sm" variant="outline" leftIcon={savingTimeout ? <Loader2 size={12} className="animate-spin" /> : undefined}
                  onClick={saveTimeout} disabled={savingTimeout}>Save</Button>
                <div className="ml-auto">
                  <Button variant="outline" size="sm" leftIcon={revokingAll ? <Loader2 size={12} className="animate-spin" /> : <LogOut size={12} className="text-red-500" />}
                    onClick={revokeAll} disabled={revokingAll}>Revoke All Others</Button>
                </div>
              </div>
              <div className="space-y-3">
                {sessions.map((s) => (
                  <Card key={s.id} className={s.isCurrent ? "border-ai-300" : ""}>
                    <CardContent className="pt-4 pb-4 flex items-center justify-between gap-3">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                          {s.device?.toLowerCase().includes("mobile") ? <Smartphone className="h-4 w-4 text-text-tertiary" /> : <Monitor className="h-4 w-4 text-text-tertiary" />}
                        </div>
                        <div>
                          <p className="text-sm font-medium text-text-primary flex items-center gap-1.5">
                            {s.device ?? s.browser ?? "Unknown device"}
                            {s.isCurrent && <span className="text-xs text-ai-600 bg-ai-50 px-1.5 py-0.5 rounded-full">Current</span>}
                          </p>
                          <div className="flex items-center gap-3 mt-0.5">
                            {s.ipAddress && <span className="text-xs text-text-tertiary font-mono flex items-center gap-1"><Globe className="h-3 w-3" />{s.ipAddress}</span>}
                            {s.location && <span className="text-xs text-text-tertiary flex items-center gap-1"><MapPin className="h-3 w-3" />{s.location}</span>}
                            <span className="text-xs text-text-tertiary flex items-center gap-1"><Clock className="h-3 w-3" />Active {new Date(s.lastActiveAt).toLocaleDateString()}</span>
                          </div>
                        </div>
                      </div>
                      {!s.isCurrent && (
                        <Button variant="outline" size="sm" leftIcon={revoking === s.id ? <Loader2 size={12} className="animate-spin" /> : <Trash2 size={12} className="text-red-500" />}
                          onClick={() => revokeSession(s.id)} disabled={!!revoking}>Revoke</Button>
                      )}
                    </CardContent>
                  </Card>
                ))}
                {sessions.length === 0 && <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No active sessions</p></CardContent></Card>}
              </div>
            </>
          )}

          {tab === "history" && (
            <Card>
              <CardContent className="p-0">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Result", "IP Address", "Location", "User Agent", "Time"].map((h) => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {history.map((e) => (
                      <tr key={e.id} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-4">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${e.success ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"}`}>
                            {e.success ? "Success" : "Failed"}
                          </span>
                        </td>
                        <td className="py-3 px-4 text-xs text-text-tertiary font-mono">{e.ipAddress ?? "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-secondary">{e.location ?? "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-tertiary max-w-xs truncate">{e.userAgent ?? "—"}</td>
                        <td className="py-3 px-4 text-xs text-text-tertiary">{new Date(e.createdAt).toLocaleString()}</td>
                      </tr>
                    ))}
                    {history.length === 0 && <tr><td colSpan={5} className="text-center py-10 text-text-tertiary">No login history</td></tr>}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
