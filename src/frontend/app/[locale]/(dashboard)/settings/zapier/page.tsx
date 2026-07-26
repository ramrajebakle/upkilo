"use client";

import React, { useState, useEffect } from "react";
import { Zap, ExternalLink, Key, CheckCircle2, Copy, Check, RefreshCw, Loader2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface ZapierInfo {
  apiKey?: string;
  webhookUrl?: string;
  isConnected: boolean;
  zaps?: { name: string; isEnabled: boolean; lastRun?: string }[];
}

export default function ZapierPage() {
  const { error: toastError } = useToast();
  const [info, setInfo] = useState<ZapierInfo>({ isConnected: false });
  const [loading, setLoading] = useState(true);
  const [copiedKey, setCopiedKey] = useState(false);

  useEffect(() => {
    apiClient.get("/api/v1/zapier/me").then((r) => {
      setInfo(r.data?.data ?? r.data ?? { isConnected: false });
    }).catch(() => setInfo({ isConnected: false }))
      .finally(() => setLoading(false));
  }, []);

  const copy = (text: string, setter: (v: boolean) => void) => {
    navigator.clipboard.writeText(text).then(() => { setter(true); setTimeout(() => setter(false), 2000); });
  };

  const SUPPORTED_TRIGGERS = [
    "New Booking Created", "Booking Cancelled", "New Client Added", "Payment Received",
    "Membership Started", "Campaign Sent", "Form Submitted", "Waitlist Joined",
  ];

  const SUPPORTED_ACTIONS = [
    "Create Booking", "Create Client", "Send SMS", "Apply Coupon",
    "Tag Client", "Add to Loyalty", "Create Invoice",
  ];

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Zapier Integration <Zap className="text-amber-400" size={22} /></h1>
        <p className="text-text-secondary mt-1">Connect Upkilo to 5,000+ apps via Zapier.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          <Card className={info.isConnected ? "border-green-200" : "border-surface-200"}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {info.isConnected ? <CheckCircle2 className="h-5 w-5 text-green-500" /> : <Zap className="h-5 w-5 text-amber-400" />}
                Connection Status
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3 p-4 rounded-xl bg-surface-50 border border-surface-200">
                <div className={`w-3 h-3 rounded-full ${info.isConnected ? "bg-green-500 animate-pulse" : "bg-gray-300"}`} />
                <span className="font-medium text-text-primary">{info.isConnected ? "Connected to Zapier" : "Not connected"}</span>
              </div>
              {info.apiKey && (
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">Your Zapier API Key</label>
                  <div className="flex gap-2">
                    <code className="flex-1 px-3 py-2 bg-slate-900 text-green-400 rounded-lg text-xs font-mono truncate">{info.apiKey}</code>
                    <Button variant="outline" size="sm" leftIcon={copiedKey ? <Check size={13} className="text-green-500" /> : <Copy size={13} />}
                      onClick={() => copy(info.apiKey!, setCopiedKey)}>{copiedKey ? "Copied" : "Copy"}</Button>
                  </div>
                  <p className="text-xs text-text-tertiary mt-1.5">Use this key in the Upkilo Zapier app to authenticate your Zaps.</p>
                </div>
              )}
              <Button variant="primary" leftIcon={<ExternalLink size={14} />} onClick={() => window.open("https://zapier.com/apps/upkilo", "_blank")}>
                Open Zapier
              </Button>
            </CardContent>
          </Card>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <Card>
              <CardHeader><CardTitle className="text-sm">Supported Triggers</CardTitle><CardDescription>Events in Upkilo that can start a Zap</CardDescription></CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {SUPPORTED_TRIGGERS.map((t) => (
                    <li key={t} className="flex items-center gap-2 text-sm text-text-secondary">
                      <CheckCircle2 className="h-3.5 w-3.5 text-green-500 flex-shrink-0" />{t}
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle className="text-sm">Supported Actions</CardTitle><CardDescription>Actions Zapier can perform in Upkilo</CardDescription></CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {SUPPORTED_ACTIONS.map((a) => (
                    <li key={a} className="flex items-center gap-2 text-sm text-text-secondary">
                      <Zap className="h-3.5 w-3.5 text-amber-400 flex-shrink-0" />{a}
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          </div>

          {info.zaps && info.zaps.length > 0 && (
            <Card>
              <CardHeader><CardTitle>Active Zaps</CardTitle></CardHeader>
              <CardContent>
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-surface-200">
                    {["Zap Name", "Status", "Last Run"].map((h) => (
                      <th key={h} className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {info.zaps.map((z, i) => (
                      <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-2.5 px-3 font-medium text-text-primary">{z.name}</td>
                        <td className="py-2.5 px-3"><span className={`text-xs font-medium px-2 py-0.5 rounded-full ${z.isEnabled ? "text-green-600 bg-green-50" : "text-gray-500 bg-gray-50"}`}>{z.isEnabled ? "Active" : "Paused"}</span></td>
                        <td className="py-2.5 px-3 text-xs text-text-secondary">{z.lastRun ? new Date(z.lastRun).toLocaleDateString() : "Never"}</td>
                      </tr>
                    ))}
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
