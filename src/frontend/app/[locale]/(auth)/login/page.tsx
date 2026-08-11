"use client";

import React, { Suspense, useState } from "react";
import { signIn } from "next-auth/react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Building2, Users, Mail, Lock, Eye, EyeOff, CheckCircle2 } from "lucide-react";
import Link from "next/link";

const ROLE_ROUTES: Record<string, string> = {
  platform_owner: "platform/command",
  platform_admin: "platform/command",
  tenant_owner: "dashboard",
  team_member: "dashboard",
  customer: "dashboard",
};

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}

function LoginForm() {
  const params = useParams();
  const router = useRouter();
  const searchParams = useSearchParams();
  const locale = (params?.locale as string) ?? "en";

  // Read query params set by register page
  const nextPath = searchParams?.get("next") ?? null;
  const isRegistered = searchParams?.get("registered") === "true";
  const registrationMessage = searchParams?.get("message");

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  // Dev-only mock login
  const [mockLoading, setMockLoading] = useState<"platform" | "tenant" | null>(null);
  const isDev = process.env.NODE_ENV === "development";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setError("Please enter your email and password.");
      return;
    }
    setError("");
    setLoading(true);
    try {
      const result = await signIn("credentials", {
        email,
        password,
        redirect: false,
      });
      if (result?.error) {
        setError("Invalid email or password. Please try again.");
      } else {
        // If a next path was provided (e.g., from registration), use it; otherwise role-based redirect
        if (nextPath) {
          router.push(`/${locale}${nextPath.startsWith('/') ? nextPath : `/${nextPath}`}`);
          return;
        }
        const sessionRes = await fetch("/api/auth/session");
        const session = await sessionRes.json();
        const role: string = session?.user?.role ?? "tenant_owner";
        const route = ROLE_ROUTES[role] ?? "dashboard";
        router.push(`/${locale}/${route}`);
      }
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleMockLogin = async (role: "platform" | "tenant") => {
    setMockLoading(role);
    try {
      await signIn("credentials", {
        username: role,
        password: "password",
        callbackUrl: role === "platform" ? `/${locale}/platform/command` : `/${locale}/dashboard`,
      });
    } catch {
      setMockLoading(null);
    }
  };

  return (
    <>
      <a href="#main-content" className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-[9999] focus:px-4 focus:py-2 focus:bg-primary-600 focus:text-white focus:rounded-lg focus:shadow-lg">
        Skip to main content
      </a>
      <main id="main-content" tabIndex={-1} className="min-h-screen bg-[var(--surface-raised)] flex flex-col items-center justify-center p-4 focus:outline-none">
      <div className="w-full max-w-md space-y-8">
        <div className="text-center">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br from-[var(--color-ai-500)] to-[var(--color-platform-500)] mb-4 shadow-lg">
            <span className="text-white font-bold text-xl">U</span>
          </div>
          <h1 className="text-3xl font-bold text-[var(--text-primary)] tracking-tight">Welcome back</h1>
          <p className="text-[var(--text-secondary)] mt-2">Sign in to your Upkilo account</p>
        </div>

        {isRegistered && (
          <div role="status" className="flex items-start gap-3 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 px-4 py-3 rounded-xl text-sm">
            <CheckCircle2 size={16} className="mt-0.5 shrink-0" aria-hidden="true" />
            <span>{registrationMessage ?? "Account created! Sign in to continue."}</span>
          </div>
        )}

        <Card className="border border-[var(--color-neutral-200)] shadow-lg dark:border-white/5">
          <CardContent className="p-6 space-y-5">
            <form onSubmit={handleSubmit} className="space-y-4">
              <Input
                label="Email address"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                leftIcon={<Mail size={16} />}
                autoComplete="email"
                required
              />
              <Input
                label="Password"
                type={showPassword ? "text" : "password"}
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                leftIcon={<Lock size={16} />}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    className="text-[var(--text-tertiary)] hover:text-[var(--text-secondary)] transition-colors"
                    aria-label={showPassword ? "Hide password" : "Show password"}
                  >
                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                }
                autoComplete="current-password"
                required
              />

              {error && (
                <p role="alert" className="text-sm text-[var(--color-danger-500)] bg-red-50 dark:bg-red-950/30 px-3 py-2 rounded-lg">
                  {error}
                </p>
              )}

              <div className="flex items-center justify-between text-sm">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" className="rounded border-gray-300 text-primary-500 focus:ring-primary-500" />
                  <span className="text-[var(--text-secondary)]">Remember me</span>
                </label>
                <Link href={`/${locale}/reset-password`} className="text-[var(--color-ai-500)] hover:underline font-medium">
                  Forgot password?
                </Link>
              </div>

              <Button type="submit" fullWidth size="lg" loading={loading} disabled={loading || mockLoading !== null}>
                Sign in
              </Button>
            </form>

            <p className="text-center text-sm text-[var(--text-secondary)]">
              Don't have an account?{" "}
              <Link href={`/${locale}/register`} className="text-[var(--color-ai-500)] hover:underline font-medium">
                Start free trial
              </Link>
            </p>

            {isDev && (
              <>
                <div className="relative">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-[var(--color-neutral-200)]" />
                  </div>
                  <div className="relative flex justify-center text-xs">
                    <span className="bg-white dark:bg-[var(--color-neutral-900)] px-2 text-[var(--text-tertiary)]">
                      Dev quick-login
                    </span>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <Button
                    variant="secondary"
                    size="sm"
                    leftIcon={<Building2 size={14} />}
                    onClick={() => handleMockLogin("platform")}
                    loading={mockLoading === "platform"}
                    disabled={loading || mockLoading !== null}
                  >
                    Platform
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    leftIcon={<Users size={14} />}
                    onClick={() => handleMockLogin("tenant")}
                    loading={mockLoading === "tenant"}
                    disabled={loading || mockLoading !== null}
                  >
                    Tenant
                  </Button>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <p className="text-center text-xs text-[var(--text-tertiary)]">
          By signing in you agree to our{" "}
          <Link href={`/${locale}/terms-of-service`} className="hover:underline">Terms</Link>
          {" "}and{" "}
          <Link href={`/${locale}/privacy-policy`} className="hover:underline">Privacy Policy</Link>.
        </p>
      </div>
      </main>
    </>
  );
}
