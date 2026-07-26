"use client";

import React, { useState, useEffect } from "react";
import { Shield, Smartphone, Mail, Key, CheckCircle2, Loader2, RefreshCw, QrCode } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface TwoFAStatus { isEnabled: boolean; method?: "totp" | "sms" | "email"; phoneNumber?: string; email?: string; }

export default function TwoFAPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [status, setStatus] = useState<TwoFAStatus>({ isEnabled: false });
  const [loading, setLoading] = useState(true);
  const [qrCode, setQrCode] = useState<string | null>(null);
  const [totpCode, setTotpCode] = useState("");
  const [verifying, setVerifying] = useState(false);
  const [setupMethod, setSetupMethod] = useState<"totp" | "sms" | "email" | null>(null);
  const [disabling, setDisabling] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/auth/2fa/status").catch(() => ({ data: { isEnabled: false } }));
      setStatus(r.data?.data ?? r.data ?? { isEnabled: false });
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const startSetup = async (method: "totp" | "sms" | "email") => {
    setSetupMethod(method);
    if (method === "totp") {
      try {
        const r = await apiClient.post("/api/v1/auth/2fa/setup", { method: "totp" });
        setQrCode(r.data?.qrCode ?? r.data?.data?.qrCode ?? null);
      } catch { toastError("Failed to start TOTP setup"); }
    }
  };

  const verifyAndEnable = async () => {
    if (!totpCode) return;
    setVerifying(true);
    try {
      await apiClient.post("/api/v1/auth/2fa/verify", { code: totpCode, method: setupMethod });
      toastSuccess("Two-factor authentication enabled"); setQrCode(null); setTotpCode(""); setSetupMethod(null); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Invalid code"); }
    finally { setVerifying(false); }
  };

  const disable2FA = async () => {
    setDisabling(true);
    try {
      await apiClient.post("/api/v1/auth/2fa/disable");
      toastSuccess("Two-factor authentication disabled"); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to disable 2FA"); }
    finally { setDisabling(false); }
  };

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Two-Factor Authentication <Shield className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Add an extra layer of security to your account.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          <Card className={status.isEnabled ? "border-green-200 bg-green-50/30" : ""}>
            <CardContent className="pt-5 pb-5 flex items-center gap-4">
              <div className={`w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0 ${status.isEnabled ? "bg-green-100" : "bg-surface-100"}`}>
                {status.isEnabled ? <CheckCircle2 className="h-6 w-6 text-green-600" /> : <Shield className="h-6 w-6 text-text-tertiary" />}
              </div>
              <div className="flex-1">
                <p className={`font-semibold ${status.isEnabled ? "text-green-800" : "text-text-primary"}`}>
                  {status.isEnabled ? "2FA is Enabled" : "2FA is Disabled"}
                </p>
                <p className="text-sm text-text-secondary mt-0.5">
                  {status.isEnabled
                    ? `Active method: ${status.method?.toUpperCase() ?? "TOTP"}`
                    : "Your account is protected by password only"}
                </p>
              </div>
              {status.isEnabled && (
                <Button variant="outline" size="sm" leftIcon={disabling ? <Loader2 size={12} className="animate-spin" /> : undefined}
                  onClick={disable2FA} disabled={disabling} className="text-red-500 border-red-200 hover:bg-red-50">
                  Disable 2FA
                </Button>
              )}
            </CardContent>
          </Card>

          {!status.isEnabled && !setupMethod && (
            <div className="space-y-3">
              <p className="text-sm font-medium text-text-primary">Choose a 2FA method:</p>
              {[
                { method: "totp" as const, label: "Authenticator App", desc: "Use Google Authenticator, Authy, or similar app", icon: <Smartphone className="h-5 w-5" /> },
                { method: "sms" as const, label: "SMS Text Message", desc: "Receive a code via SMS to your phone number", icon: <Key className="h-5 w-5" /> },
                { method: "email" as const, label: "Email Code", desc: "Receive a code to your email address", icon: <Mail className="h-5 w-5" /> },
              ].map((opt) => (
                <Card key={opt.method} className="cursor-pointer hover:border-ai-300 transition-colors" onClick={() => startSetup(opt.method)}>
                  <CardContent className="pt-4 pb-4 flex items-center gap-4">
                    <div className="w-10 h-10 rounded-lg bg-surface-100 flex items-center justify-center text-text-tertiary flex-shrink-0">{opt.icon}</div>
                    <div className="flex-1">
                      <p className="text-sm font-semibold text-text-primary">{opt.label}</p>
                      <p className="text-xs text-text-tertiary">{opt.desc}</p>
                    </div>
                    <span className="text-xs text-ai-600">Set up →</span>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          {setupMethod === "totp" && (
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><QrCode className="h-4 w-4" /> Scan QR Code</CardTitle>
                <CardDescription>Scan this code with your authenticator app, then enter the 6-digit code below</CardDescription></CardHeader>
              <CardContent className="space-y-4">
                {qrCode && (
                  <div className="flex justify-center">
                    <img src={qrCode} alt="TOTP QR Code" className="w-48 h-48 rounded-xl border border-surface-200" />
                  </div>
                )}
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Verification Code</label>
                  <div className="flex gap-2">
                    <input value={totpCode} onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, "").slice(0, 6))} placeholder="000000" maxLength={6}
                      className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 tracking-widest text-center text-lg font-mono" />
                    <Button variant="primary" leftIcon={verifying ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                      onClick={verifyAndEnable} disabled={totpCode.length !== 6 || verifying}>{verifying ? "Verifying…" : "Enable"}</Button>
                  </div>
                </div>
                <Button variant="outline" size="sm" onClick={() => { setSetupMethod(null); setQrCode(null); }}>Cancel</Button>
              </CardContent>
            </Card>
          )}

          {(setupMethod === "sms" || setupMethod === "email") && (
            <Card>
              <CardHeader><CardTitle>Verify {setupMethod === "sms" ? "Phone Number" : "Email"}</CardTitle></CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-text-primary mb-1">Enter the verification code sent to your {setupMethod === "sms" ? "phone" : "email"}</label>
                  <div className="flex gap-2">
                    <input value={totpCode} onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, "").slice(0, 6))} placeholder="000000" maxLength={6}
                      className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 tracking-widest text-center text-lg font-mono" />
                    <Button variant="primary" leftIcon={verifying ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                      onClick={verifyAndEnable} disabled={totpCode.length !== 6 || verifying}>{verifying ? "Verifying…" : "Enable"}</Button>
                  </div>
                </div>
                <Button variant="outline" size="sm" onClick={() => { setSetupMethod(null); setTotpCode(""); }}>Cancel</Button>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
