"use client";

import React, { useState, useEffect, useCallback } from "react";
import { MessageSquare, Send, Zap, Users, TrendingUp, Loader2, RefreshCw, Eye } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface WinBackStats { totalAtRisk: number; contacted: number; reactivated: number; revenue: number; }
interface Trigger { id: string; name: string; description: string; isEnabled: boolean; }
interface Preview { clientId: string; clientName: string; message: string; channel: string; }

export default function ProactiveMessagingPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [stats, setStats] = useState<WinBackStats | null>(null);
  const [triggers, setTriggers] = useState<Trigger[]>([]);
  const [preview, setPreview] = useState<Preview[]>([]);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [channel, setChannel] = useState("email");
  const [triggerType, setTriggerType] = useState("win-back");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [statsRes, triggersRes] = await Promise.all([
        apiClient.get("/api/v1/proactive-messaging/win-back/stats").catch(() => ({ data: null })),
        apiClient.get("/api/v1/proactive-messaging/triggers").catch(() => ({ data: [] })),
      ]);
      setStats(statsRes.data?.data ?? statsRes.data ?? null);
      setTriggers(Array.isArray(triggersRes.data) ? triggersRes.data : triggersRes.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const generatePreview = async () => {
    setGenerating(true);
    try {
      const r = await apiClient.get(`/api/v1/proactive-messaging/preview?triggerType=${triggerType}&channel=${channel}`);
      setPreview(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } catch { toastError("Failed to generate preview"); }
    finally { setGenerating(false); }
  };

  const sendMessages = async () => {
    setSending(true);
    try {
      const r = await apiClient.post("/api/v1/proactive-messaging/send", { triggerType, channel });
      toastSuccess(`${r.data?.sent ?? 0} messages sent`); setPreview([]);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Send failed"); }
    finally { setSending(false); }
  };

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Proactive Messaging <MessageSquare className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">AI-powered outreach to re-engage at-risk and lapsed clients.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {stats && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { label: "At-Risk Clients", value: stats.totalAtRisk, color: "text-amber-500" },
            { label: "Contacted", value: stats.contacted, color: "text-blue-500" },
            { label: "Reactivated", value: stats.reactivated, color: "text-green-500" },
            { label: "Revenue Recovered", value: `$${(stats.revenue ?? 0).toLocaleString()}`, color: "text-green-600" },
          ].map((s) => (
            <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
          ))}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 space-y-4">
          <Card>
            <CardHeader><CardTitle>Send Campaign</CardTitle><CardDescription>Choose trigger and channel, then preview before sending</CardDescription></CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Trigger Type</label>
                <select value={triggerType} onChange={(e) => setTriggerType(e.target.value)}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  <option value="win-back">Win-Back (lapsed clients)</option>
                  <option value="at-risk">At-Risk (low engagement)</option>
                  <option value="birthday">Birthday Offers</option>
                  <option value="milestone">Milestone Rewards</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Channel</label>
                <div className="flex gap-2">
                  {["email", "sms", "push"].map((c) => (
                    <button key={c} onClick={() => setChannel(c)}
                      className={`flex-1 py-2 text-xs font-medium rounded-lg border transition-colors capitalize ${channel === c ? "border-ai-500 bg-ai-50 text-ai-700" : "border-surface-200 text-text-secondary hover:bg-surface-50"}`}>
                      {c}
                    </button>
                  ))}
                </div>
              </div>
              <Button variant="outline" className="w-full" leftIcon={generating ? <Loader2 size={14} className="animate-spin" /> : <Eye size={14} />}
                onClick={generatePreview} disabled={generating}>
                {generating ? "Generating…" : "Preview Messages"}
              </Button>
              <Button variant="primary" className="w-full" leftIcon={sending ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
                onClick={sendMessages} disabled={sending || preview.length === 0}>
                {sending ? "Sending…" : `Send to ${preview.length || "…"} clients`}
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-sm">Automation Triggers</CardTitle><CardDescription>Configure which events fire proactive messages</CardDescription></CardHeader>
            <CardContent className="space-y-3">
              {loading ? <Loader2 className="h-4 w-4 animate-spin text-text-tertiary mx-auto" />
                : triggers.length === 0 ? <p className="text-xs text-text-tertiary text-center">No triggers configured</p>
                : triggers.map((t) => (
                  <div key={t.id} className="flex items-start justify-between gap-2">
                    <div>
                      <p className="text-sm font-medium text-text-primary">{t.name}</p>
                      <p className="text-xs text-text-tertiary">{t.description}</p>
                    </div>
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full flex-shrink-0 ${t.isEnabled ? "text-green-600 bg-green-50" : "text-gray-500 bg-gray-50"}`}>
                      {t.isEnabled ? "On" : "Off"}
                    </span>
                  </div>
                ))}
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-2">
          {preview.length > 0 ? (
            <Card>
              <CardHeader><CardTitle>Message Preview</CardTitle><CardDescription>{preview.length} clients will receive this message</CardDescription></CardHeader>
              <CardContent className="space-y-3 max-h-[500px] overflow-y-auto">
                {preview.map((p, i) => (
                  <div key={i} className="p-3 rounded-xl border border-surface-100 bg-surface-50">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-xs font-medium text-text-primary">{p.clientName}</span>
                      <span className="text-xs bg-ai-50 text-ai-600 px-2 py-0.5 rounded-full capitalize">{p.channel}</span>
                    </div>
                    <p className="text-xs text-text-secondary">{p.message}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
          ) : (
            <Card><CardContent className="text-center py-16 text-text-tertiary">
              <MessageSquare className="h-12 w-12 mx-auto mb-3 opacity-20" />
              <p className="font-medium">Select a trigger and click Preview Messages</p>
              <p className="text-sm mt-1">AI will generate personalized messages for each eligible client.</p>
            </CardContent></Card>
          )}
        </div>
      </div>
    </div>
  );
}
