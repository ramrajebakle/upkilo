"use client";

import React, { useState, useEffect } from "react";
import { Phone, CheckCircle2, AlertCircle, Loader2, Save, Copy } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface VoiceSetup { isConfigured: boolean; phoneNumber?: string; greeting?: string; voicemailEnabled: boolean; forwardToNumber?: string; webhookUrl?: string; }

export default function TwilioVoicePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [setup, setSetup] = useState<VoiceSetup | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [copied, setCopied] = useState(false);
  const [form, setForm] = useState({ phoneNumber: "", greeting: "", voicemailEnabled: true, forwardToNumber: "" });

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/twilio-voice/setup").catch(() => ({ data: null }));
      const s = r.data?.data ?? r.data ?? null;
      setSetup(s);
      if (s) setForm({ phoneNumber: s.phoneNumber ?? "", greeting: s.greeting ?? "", voicemailEnabled: s.voicemailEnabled ?? true, forwardToNumber: s.forwardToNumber ?? "" });
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/twilio-voice/configure", form);
      toastSuccess("Voice configuration saved"); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const copyWebhook = () => {
    if (!setup?.webhookUrl) return;
    navigator.clipboard.writeText(setup.webhookUrl).then(() => { setCopied(true); setTimeout(() => setCopied(false), 1500); });
  };

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Voice Calls (Twilio) <Phone className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Answer client calls with an AI receptionist — book appointments, answer FAQs, and take voicemails.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          <div className={`flex items-center gap-3 p-4 rounded-xl border ${setup?.isConfigured ? "bg-green-50 border-green-200" : "bg-amber-50 border-amber-200"}`}>
            {setup?.isConfigured ? <CheckCircle2 className="h-5 w-5 text-green-600 flex-shrink-0" /> : <AlertCircle className="h-5 w-5 text-amber-600 flex-shrink-0" />}
            <div>
              <p className={`text-sm font-semibold ${setup?.isConfigured ? "text-green-800" : "text-amber-800"}`}>
                {setup?.isConfigured ? `Voice active on ${setup.phoneNumber}` : "Voice not configured"}
              </p>
              <p className={`text-xs ${setup?.isConfigured ? "text-green-600" : "text-amber-600"}`}>
                {setup?.isConfigured ? "Incoming calls are handled by the AI receptionist." : "Configure your Twilio number below to enable voice."}
              </p>
            </div>
          </div>

          <Card>
            <CardHeader><CardTitle>Voice Configuration</CardTitle>
              <CardDescription>Requires a Twilio phone number with Voice capability</CardDescription></CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Twilio Phone Number</label>
                <input value={form.phoneNumber} onChange={(e) => setForm((p) => ({ ...p, phoneNumber: e.target.value }))} placeholder="+91 98765 43210"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Greeting Message</label>
                <textarea value={form.greeting} onChange={(e) => setForm((p) => ({ ...p, greeting: e.target.value }))} rows={3}
                  placeholder="Thanks for calling! I'm the virtual assistant — how can I help you today?"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Forward to Number (fallback)</label>
                <input value={form.forwardToNumber} onChange={(e) => setForm((p) => ({ ...p, forwardToNumber: e.target.value }))} placeholder="Forward when AI can't help (optional)"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 font-mono" />
              </div>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-medium text-text-primary">Voicemail</p>
                  <p className="text-xs text-text-tertiary mt-0.5">Take voicemail when calls can't be answered or forwarded</p>
                </div>
                <div onClick={() => setForm((p) => ({ ...p, voicemailEnabled: !p.voicemailEnabled }))}
                  className={`w-10 h-5 rounded-full flex-shrink-0 cursor-pointer relative transition-colors mt-0.5 ${form.voicemailEnabled ? "bg-ai-500" : "bg-surface-300"}`}>
                  <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${form.voicemailEnabled ? "translate-x-5" : "translate-x-0.5"}`} />
                </div>
              </div>
              <div className="flex justify-end">
                <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                  onClick={save} disabled={saving}>{saving ? "Saving…" : "Save Configuration"}</Button>
              </div>
            </CardContent>
          </Card>

          {setup?.webhookUrl && (
            <Card>
              <CardHeader><CardTitle className="text-base">Twilio Webhook</CardTitle>
                <CardDescription>Set this as the Voice webhook URL for your number in the Twilio console</CardDescription></CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  <code className="flex-1 text-xs bg-surface-100 text-text-secondary px-3 py-2 rounded-lg font-mono truncate">{setup.webhookUrl}</code>
                  <Button variant="outline" size="sm" leftIcon={copied ? <CheckCircle2 size={12} className="text-green-500" /> : <Copy size={12} />}
                    onClick={copyWebhook}>{copied ? "Copied" : "Copy"}</Button>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
