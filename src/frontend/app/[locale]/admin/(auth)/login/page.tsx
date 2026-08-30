"use client";

import React, { useState } from "react";
import { useRouter } from "@/navigation";
import { LogIn, Key, QrCode, ShieldCheck, AlertCircle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/authStore";
import { useToast } from "@/components/ui/Toast";
import { QRCodeSVG } from "qrcode.react";

type AuthStage = "login" | "verify" | "setup";

export default function AdminLoginPage() {
  const router = useRouter();
  const { login: setAuthLogin } = useAuthStore();
  const { success, error: toastError } = useToast();
  
  const [stage, setStage] = useState<AuthStage>("login");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Form State
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [totpCode, setTotpCode] = useState("");
  const [setupData, setSetupData] = useState<{ qrCodeUri: string; manualEntryKey: string } | null>(null);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    try {
      const res = await api.superAdmin.login(email, password);
      const { status } = res.data;

      if (status === "SetupRequired") {
        const setupRes = await api.superAdmin.setup2fa({ email, password });
        setSetupData(setupRes.data);
        setStage("setup");
      } else if (status === "TwoFactorRequired") {
        setStage("verify");
      }
    } catch (err: any) {
      const message = typeof err.response?.data === 'string' 
        ? err.response.data 
        : err.response?.data?.message || err.message || "Access denied.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    try {
      const res = await api.superAdmin.verify2fa({ email, code: totpCode });
      const { token, user: userData } = res.data;
      
      // Use real user profile data from server response
      setAuthLogin({
        id: userData.id,
        email: userData.email,
        firstName: userData.firstName,
        lastName: userData.lastName,
        role: 'superadmin',
        tenantId: userData.tenantId || ''
      }, token);
      
      success("Security clearance granted. Welcome, Admin.");
      
      // Navigate to admin dashboard using locale-aware router
      router.push("/admin");
    } catch (err: any) {
      const message = typeof err.response?.data === 'string' 
        ? err.response.data 
        : err.response?.data?.message || err.message || "Invalid security code.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card className="bg-neutral-900 border-neutral-800 shadow-2xl">
      <CardHeader className="text-center pb-2">
        <CardTitle className="text-xl text-white font-mono uppercase tracking-widest">
          {stage === "login" && "Level 1 Auth"}
          {stage === "verify" && "Level 2 verification"}
          {stage === "setup" && "Security Enrollment"}
        </CardTitle>
        <CardDescription className="text-neutral-500">
          {stage === "login" && "Internal credentials required"}
          {stage === "verify" && "Synchronize TOTP token"}
          {stage === "setup" && "Mandatory 2FA configuration"}
        </CardDescription>
      </CardHeader>
      
      <CardContent className="pt-4">
        {error && (
          <div className="mb-4 flex items-center gap-2 p-3 bg-red-950/30 border border-red-500/30 text-red-400 rounded-lg text-sm transition-all duration-300">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {stage === "login" && (
          <form onSubmit={handleLogin} className="space-y-4">
            <div className="space-y-1">
              <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">Admin Email</label>
              <Input 
                type="email" 
                value={email} 
                onChange={(e) => setEmail(e.target.value)} 
                className="bg-black border-neutral-800 text-white h-12"
                required
              />
            </div>
            <div className="space-y-1">
              <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">Access Key</label>
              <Input 
                type="password" 
                value={password} 
                onChange={(e) => setPassword(e.target.value)} 
                className="bg-black border-neutral-800 text-white h-12"
                required
              />
            </div>
            <Button type="submit" className="w-full h-12 bg-red-600 hover:bg-red-700 text-white font-bold uppercase tracking-widest shadow-lg shadow-red-900/20" disabled={isLoading}>
              {isLoading ? "Validating..." : <><LogIn className="w-4 h-4 mr-2" /> Initializing Session</>}
            </Button>
          </form>
        )}

        {stage === "verify" && (
          <form onSubmit={handleVerify} className="space-y-4 text-center">
            <div className="flex justify-center mb-4">
              <div className="h-16 w-16 bg-neutral-800 rounded-full flex items-center justify-center border border-neutral-700 animate-pulse">
                <Key className="w-8 h-8 text-red-500" />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-[10px] uppercase tracking-wider text-neutral-400">TOTP Token Code</label>
              <Input 
                type="text" 
                placeholder="000000"
                value={totpCode} 
                onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, '').substring(0,6))} 
                className="bg-black border-neutral-800 text-white h-16 text-center text-3xl tracking-[0.2em] font-mono"
                required
                autoFocus
              />
            </div>
            <Button type="submit" className="w-full h-12 bg-green-600 hover:bg-green-700 text-white font-bold uppercase tracking-widest" disabled={isLoading || totpCode.length < 6}>
              {isLoading ? "Verifying..." : "Authorize Access"}
            </Button>
            <button type="button" onClick={() => setStage("login")} className="text-xs text-neutral-500 hover:text-white transition-colors">
              Cancel & Request Reset
            </button>
          </form>
        )}

        {stage === "setup" && setupData && (
          <div className="space-y-6 text-center animate-fade-in">
            <div className="bg-card p-4 rounded-xl mx-auto w-fit shadow-[0_0_20px_rgba(255,255,255,0.1)]">
              <div className="w-48 h-48 flex items-center justify-center bg-card rounded-lg p-2">
                <QRCodeSVG 
                  value={setupData.qrCodeUri} 
                  size={176}
                  level="H"
                  includeMargin={false}
                />
              </div>
            </div>
            
            <div className="space-y-2">
              <p className="text-xs text-neutral-400">Scan this QR in your Auth App (Google/Authy)</p>
              <div className="bg-black p-2 rounded border border-neutral-800 text-[10px] font-mono text-danger-fg select-all">
                {setupData.manualEntryKey}
              </div>
            </div>

            <form onSubmit={handleVerify} className="space-y-4">
              <Input 
                type="text" 
                placeholder="Verify Code"
                value={totpCode} 
                onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, '').substring(0,6))} 
                className="bg-black border-neutral-800 text-white h-12 text-center"
                required
              />
              <Button type="submit" className="w-full h-12 bg-red-600 hover:bg-red-700 text-white font-bold" disabled={isLoading || totpCode.length < 6}>
                Completing Setup
              </Button>
            </form>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
