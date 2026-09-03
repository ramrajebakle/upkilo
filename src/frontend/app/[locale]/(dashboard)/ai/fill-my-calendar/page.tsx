"use client";

import React, { useState, useEffect } from "react";
import { Calendar, Zap, Users, Clock, Loader2, RefreshCw, Send } from "lucide-react";
import { apiClient } from "@/lib/api";
import { apiErrorMessage } from "@/lib/apiError";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Suggestion { clientId: string; clientName: string; service: string; suggestedSlot: string; reason: string; confidence: number; }
interface Slot { date: string; time: string; staffId?: string; staffName?: string; }

export default function FillMyCalendarPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [emptySlots, setEmptySlots] = useState<Slot[]>([]);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState<string | null>(null);
  const [daysAhead, setDaysAhead] = useState(7);

  const load = async () => {
    setLoading(true);
    try {
      const [sugRes, slotsRes] = await Promise.all([
        apiClient.get(`/api/v1/ai/fill-my-calendar?daysAhead=${daysAhead}`).catch(() => ({ data: {} })),
        apiClient.get(`/api/v1/calendar/empty-slots?daysAhead=${daysAhead}`).catch(() => ({ data: [] })),
      ]);
      const payload = sugRes.data?.data ?? sugRes.data ?? {};
      setSuggestions(Array.isArray(payload.suggestions) ? payload.suggestions : Array.isArray(payload) ? payload : []);
      setEmptySlots(Array.isArray(slotsRes.data) ? slotsRes.data : slotsRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [daysAhead]);

  /**
   * Two calls, because there is no single "invite" endpoint and never was — this posted to
   * /api/v1/ai/fill-my-calendar/invite, which 404s. The backend splits the job: generate-sms
   * writes the personalised message, send dispatches it.
   *
   * Only the client id and the message go to the server. The destination number is resolved
   * there from the tenant's own client record, so the browser neither supplies nor needs it.
   */
  const sendInvite = async (suggestion: Suggestion) => {
    setSending(suggestion.clientId);
    try {
      const generated = await apiClient.post("/api/v1/ai/fill-my-calendar/generate-sms", {
        clientId: suggestion.clientId,
        clientName: suggestion.clientName,
        lastServiceName: suggestion.service,
        daysSinceLastVisit: 0,
        slotStart: suggestion.suggestedSlot,
      });

      const message = generated.data?.message;
      if (!message) throw new Error("No message was generated");

      const res = await apiClient.post("/api/v1/ai/fill-my-calendar/send", {
        items: [{ clientId: suggestion.clientId, message }],
      });

      // A skipped item is not a success: it means the client had no phone number on file.
      if ((res.data?.sent ?? 0) < 1) {
        toastError(
          (res.data?.skipped ?? 0) > 0
            ? "No phone number on file for this client"
            : "Message could not be sent",
        );
        return;
      }

      toastSuccess("Re-engagement message sent");
      setSuggestions((s) => s.filter((x) => x.clientId !== suggestion.clientId));
    } catch (e: unknown) {
      toastError(apiErrorMessage(e, "Failed to send invite"));
    } finally {
      setSending(null);
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Fill My Calendar <Calendar className="text-ai" size={22} /></h1>
          <p className="text-text-secondary mt-1">AI identifies gaps in your calendar and suggests clients to re-engage.</p>
        </div>
        <div className="flex items-center gap-2">
          <select value={daysAhead} onChange={(e) => setDaysAhead(parseInt(e.target.value))}
            className="px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
            {[7, 14, 30].map((d) => <option key={d} value={d}>Next {d} days</option>)}
          </select>
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Open Slots", value: emptySlots.length, color: "text-warning-fg", icon: <Clock className="h-5 w-5 text-amber-400" /> },
          { label: "AI Suggestions", value: suggestions.length, color: "text-ai", icon: <Zap className="h-5 w-5 text-ai-400" /> },
          { label: "Clients to Contact", value: new Set(suggestions.map((s) => s.clientId)).size, color: "text-text-primary", icon: <Users className="h-5 w-5 text-text-tertiary" /> },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5 flex items-center gap-3">{s.icon}<div><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-0.5 ${s.color}`}>{s.value}</p></div></CardContent></Card>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div>
            <h2 className="text-sm font-semibold text-text-primary mb-3 flex items-center gap-1.5"><Zap size={14} className="text-ai" /> AI Re-Engagement Suggestions</h2>
            {suggestions.length === 0 ? (
              <Card><CardContent className="text-center py-10 text-text-tertiary">
                <Zap className="h-8 w-8 mx-auto mb-2 opacity-20" />
                <p className="text-sm">No suggestions — your calendar looks full!</p>
              </CardContent></Card>
            ) : (
              <div className="space-y-3">
                {suggestions.map((s) => (
                  <Card key={s.clientId}>
                    <CardContent className="pt-4 pb-4">
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex-1">
                          <p className="text-sm font-semibold text-text-primary">{s.clientName}</p>
                          <p className="text-xs text-text-secondary mt-0.5">{s.service}</p>
                          <p className="text-xs text-text-tertiary mt-1 italic">{s.reason}</p>
                          <div className="flex items-center gap-2 mt-2">
                            <Clock className="h-3 w-3 text-text-tertiary" />
                            <span className="text-xs text-text-secondary">{new Date(s.suggestedSlot).toLocaleString()}</span>
                            <span className={`text-xs font-medium px-1.5 py-0.5 rounded-full ${s.confidence > 80 ? "text-green-600 bg-green-50" : "text-amber-600 bg-amber-50"}`}>{s.confidence}% match</span>
                          </div>
                        </div>
                        <Button variant="primary" size="sm" leftIcon={sending === s.clientId ? <Loader2 size={12} className="animate-spin" /> : <Send size={12} />}
                          onClick={() => sendInvite(s)} disabled={!!sending}>
                          {sending === s.clientId ? "Sending…" : "Invite"}
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </div>

          <div>
            <h2 className="text-sm font-semibold text-text-primary mb-3 flex items-center gap-1.5"><Clock size={14} className="text-warning-fg" /> Open Time Slots</h2>
            {emptySlots.length === 0 ? (
              <Card><CardContent className="text-center py-10 text-text-tertiary">
                <Calendar className="h-8 w-8 mx-auto mb-2 opacity-20" />
                <p className="text-sm">No gaps found in your calendar</p>
              </CardContent></Card>
            ) : (
              <Card>
                <CardContent className="p-0">
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Date", "Time", "Staff"].map((h) => (
                        <th key={h} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {emptySlots.map((sl, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-2 px-3 text-xs text-text-primary">{new Date(sl.date).toLocaleDateString()}</td>
                          <td className="py-2 px-3 text-xs text-text-secondary">{sl.time}</td>
                          <td className="py-2 px-3 text-xs text-text-tertiary">{sl.staffName ?? "Any"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
