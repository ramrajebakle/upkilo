"use client";

import React, { useState } from "react";
import { useRouter } from "@/navigation";
import { UserPlus, Shield, AlertCircle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { api } from "@/lib/api";
import { useToast } from "@/components/ui/Toast";

export default function AdminRegisterPage() {
  const router = useRouter();
  const { success, error: toastError } = useToast();
  
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Form State
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) {
      setError("Passkeys do not match.");
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      await api.superAdmin.register({
        email,
        password,
        firstName,
        lastName
      });
      
      success("Owner account created. Proceed to security enrollment.");
      
      // Redirect to login to start the 2FA flow
      setTimeout(() => {
        router.push("/admin/login");
      }, 2000);
    } catch (err: any) {
      const message = typeof err.response?.data === 'string' 
        ? err.response.data 
        : err.response?.data?.message || err.message || "Registration failed.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card className="bg-neutral-900 border-neutral-800 shadow-2xl">
      <CardHeader className="text-center pb-2">
        <CardTitle className="text-xl text-white font-mono uppercase tracking-widest flex items-center justify-center gap-2">
          <Shield className="w-5 h-5 text-red-500" /> Platform Initialization
        </CardTitle>
        <CardDescription className="text-neutral-500">
          Create the primary system administrator account.
        </CardDescription>
      </CardHeader>
      
      <CardContent className="pt-4">
        {error && (
          <div className="mb-4 flex items-center gap-2 p-3 bg-red-950/30 border border-red-500/30 text-red-400 rounded-lg text-sm">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleRegister} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">First Name</label>
              <Input 
                type="text" 
                value={firstName} 
                onChange={(e) => setFirstName(e.target.value)} 
                className="bg-black border-neutral-800 text-white h-10"
                required
                disabled={isLoading}
              />
            </div>
            <div className="space-y-1">
              <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">Last Name</label>
              <Input 
                type="text" 
                value={lastName} 
                onChange={(e) => setLastName(e.target.value)} 
                className="bg-black border-neutral-800 text-white h-10"
                required
                disabled={isLoading}
              />
            </div>
          </div>
          
          <div className="space-y-1">
            <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">System Email</label>
            <Input 
              type="email" 
              value={email} 
              onChange={(e) => setEmail(e.target.value)} 
              className="bg-black border-neutral-800 text-white h-10"
              required
              disabled={isLoading}
            />
          </div>

          <div className="space-y-1">
            <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">Master Password</label>
            <Input 
              type="password" 
              value={password} 
              onChange={(e) => setPassword(e.target.value)} 
              className="bg-black border-neutral-800 text-white h-10"
              required
              disabled={isLoading}
            />
          </div>

          <div className="space-y-1">
            <label className="text-[10px] uppercase tracking-wider text-neutral-400 ml-1">Confirm Identity</label>
            <Input 
              type="password" 
              value={confirmPassword} 
              onChange={(e) => setConfirmPassword(e.target.value)} 
              className="bg-black border-neutral-800 text-white h-10"
              required
              disabled={isLoading}
            />
          </div>

          <Button type="submit" className="w-full h-12 bg-card hover:bg-neutral-200 text-black font-bold uppercase tracking-widest mt-4" disabled={isLoading}>
            {isLoading ? "Provisioning..." : <><UserPlus className="w-4 h-4 mr-2" /> Initialize Platform</>}
          </Button>
        </form>

        <div className="mt-6 text-center">
          <p className="text-[10px] text-neutral-600">
            Note: This setup is only available once. After initialization, further admins must be invited.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
