"use client";

import React, { useState, useEffect } from "react";
import { Fingerprint, CheckCircle2, AlertCircle, Loader2, Trash2, Plus, Shield } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface BiometricCredential { id: string; name: string; deviceType?: string; createdAt: string; lastUsedAt?: string; }

export default function BiometricsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [credentials, setCredentials] = useState<BiometricCredential[]>([]);
  const [loading, setLoading] = useState(true);
  const [registering, setRegistering] = useState(false);
  const [deleting, setDeleting] = useState<string | null>(null);
  const [credName, setCredName] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/auth/biometrics").catch(() => ({ data: [] }));
      setCredentials(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const register = async () => {
    if (!credName.trim()) return;
    setRegistering(true);
    try {
      const optRes = await apiClient.post("/api/v1/auth/biometrics/register/options", { name: credName });
      const options = optRes.data?.data ?? optRes.data;
      if (typeof window !== "undefined" && "PublicKeyCredential" in window && options) {
        try {
          const credential = await (navigator.credentials as any).create({ publicKey: options });
          await apiClient.post("/api/v1/auth/biometrics/register/verify", { name: credName, credential });
          toastSuccess("Biometric credential registered"); setCredName(""); load();
        } catch (webauthnErr) { toastError("WebAuthn registration cancelled or failed"); }
      } else {
        await apiClient.post("/api/v1/auth/biometrics/register", { name: credName });
        toastSuccess("Biometric credential registered"); setCredName(""); load();
      }
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Registration failed"); }
    finally { setRegistering(false); }
  };

  const remove = async (id: string) => {
    setDeleting(id);
    try {
      await apiClient.delete(`/api/v1/auth/biometrics/${id}`);
      toastSuccess("Credential removed"); setCredentials((c) => c.filter((x) => x.id !== id));
    } catch { toastError("Remove failed"); }
    finally { setDeleting(null); }
  };

  const webAuthnSupported = typeof window !== "undefined" && "PublicKeyCredential" in window;

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Biometric Authentication <Fingerprint className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Register passkeys and biometric credentials for passwordless sign-in.</p>
      </header>

      {!webAuthnSupported && (
        <div className="flex items-start gap-3 p-4 rounded-xl bg-amber-50 border border-amber-200">
          <AlertCircle className="h-5 w-5 text-amber-600 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-amber-800">Your browser doesn't support WebAuthn/Passkeys. Use Chrome, Edge, Safari, or Firefox on a modern device.</p>
        </div>
      )}

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Plus className="h-4 w-4" /> Register New Credential</CardTitle>
          <CardDescription>Add your fingerprint, Face ID, or security key as a login method</CardDescription></CardHeader>
        <CardContent className="space-y-3">
          <div className="flex gap-2">
            <input value={credName} onChange={(e) => setCredName(e.target.value)} placeholder="Credential name (e.g. MacBook Touch ID)"
              className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            <Button variant="primary" leftIcon={registering ? <Loader2 size={14} className="animate-spin" /> : <Fingerprint size={14} />}
              onClick={register} disabled={!credName.trim() || registering || !webAuthnSupported}>
              {registering ? "Registering…" : "Register"}
            </Button>
          </div>
          <p className="text-xs text-text-tertiary">You'll be prompted by your device to scan your fingerprint or face, or to insert your security key.</p>
        </CardContent>
      </Card>

      <div>
        <h2 className="text-sm font-semibold text-text-primary mb-3">Registered Credentials ({credentials.length})</h2>
        {loading ? <div className="flex justify-center py-6"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
          : credentials.length === 0 ? (
            <Card><CardContent className="text-center py-10 text-text-tertiary">
              <Fingerprint className="h-10 w-10 mx-auto mb-3 opacity-20" />
              <p className="text-sm">No biometric credentials registered</p>
            </CardContent></Card>
          ) : (
            <div className="space-y-2">
              {credentials.map((c) => (
                <Card key={c.id}>
                  <CardContent className="pt-3 pb-3 flex items-center gap-3">
                    <div className="w-9 h-9 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                      <Fingerprint className="h-4 w-4 text-ai-500" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-text-primary">{c.name}</p>
                      <div className="flex items-center gap-3 mt-0.5">
                        {c.deviceType && <span className="text-xs text-text-tertiary">{c.deviceType}</span>}
                        <span className="text-xs text-text-tertiary">Added {new Date(c.createdAt).toLocaleDateString()}</span>
                        {c.lastUsedAt && <span className="text-xs text-green-600">Last used {new Date(c.lastUsedAt).toLocaleDateString()}</span>}
                      </div>
                    </div>
                    <Button variant="outline" size="sm" leftIcon={deleting === c.id ? <Loader2 size={12} className="animate-spin" /> : <Trash2 size={12} className="text-red-500" />}
                      onClick={() => remove(c.id)} disabled={!!deleting}>Remove</Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
      </div>
    </div>
  );
}
