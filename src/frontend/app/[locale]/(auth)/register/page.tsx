"use client";

import React, { useState, useMemo, Suspense } from "react";
import { useRouter, Link } from "@/navigation";
import { useSearchParams } from "next/navigation";
import { UserPlus, Sparkles, Building2, ShieldCheck, Check, X } from "lucide-react";

/**
 * Mirrors RegisterRequestValidator on the server, rule for rule.
 *
 * It used to score length, uppercase, digit and symbol — but not lowercase, which the server
 * requires. So "PASSWORD1!" was rated "Strong" in green and then rejected on submit, which is a
 * poor thing to do to someone on their first screen. The unmet rules are now listed rather than
 * summarised into a single bar, so a rejection is never a surprise.
 */
const PASSWORD_RULES: { id: string; label: string; test: (pw: string) => boolean }[] = [
  { id: "length", label: "At least 8 characters", test: (pw) => pw.length >= 8 },
  { id: "upper", label: "One uppercase letter", test: (pw) => /[A-Z]/.test(pw) },
  { id: "lower", label: "One lowercase letter", test: (pw) => /[a-z]/.test(pw) },
  { id: "number", label: "One number", test: (pw) => /[0-9]/.test(pw) },
  { id: "special", label: "One special character", test: (pw) => /[^a-zA-Z0-9]/.test(pw) },
];

const STRENGTH_LEVELS = [
  { label: "", color: "bg-gray-200" },
  { label: "Very weak", color: "bg-red-500" },
  { label: "Weak", color: "bg-red-500" },
  { label: "Fair", color: "bg-amber-500" },
  { label: "Good", color: "bg-blue-500" },
  { label: "Strong", color: "bg-emerald-500" },
];

function getPasswordStrength(pw: string) {
  const passed = PASSWORD_RULES.filter((r) => r.test(pw));
  return {
    score: passed.length,
    total: PASSWORD_RULES.length,
    meetsServerRules: passed.length === PASSWORD_RULES.length,
    ...STRENGTH_LEVELS[passed.length],
  };
}

/**
 * Attribution keys the server accepts (AuthService.AllowedAttributionKeys). The marketing site
 * links here with these already set — /register?plan=starter&locale=en-AU from the country
 * pages, ?vertical=medical-spa from the vertical pages, and a full utm_* chain from the
 * Powered-by-Upkilo widget — but this page read no query parameters whatsoever, so all of it was
 * dropped at the final step of the funnel.
 */
const ATTRIBUTION_KEYS = [
  "utm_source",
  "utm_medium",
  "utm_campaign",
  "utm_content",
  "utm_term",
  "vertical",
] as const;

import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { api } from "@/lib/api";

function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    company: "",
    password: ""
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Plan name, not id: the pricing pages link with ?plan=starter. The server resolves the name
  // case-insensitively and falls back to Free if it does not recognise it.
  const planName = searchParams?.get("plan") ?? undefined;

  const attribution = useMemo(() => {
    const collected: Record<string, string> = {};
    for (const key of ATTRIBUTION_KEYS) {
      const value = searchParams?.get(key);
      if (value) collected[key] = value;
    }
    if (typeof document !== "undefined" && document.referrer) {
      collected.referrer = document.referrer;
    }
    return Object.keys(collected).length > 0 ? collected : undefined;
  }, [searchParams]);

  const strength = getPasswordStrength(formData.password);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    try {
      const response = await api.auth.register({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        companyName: formData.company, // Note: DB schema maps it to companyName
        planName,
        attribution,
      });
      // Redirect to login with next=/onboarding so after sign-in the user goes through setup
      const message = response.data?.message || "Account created! Sign in to get started.";
      router.push(`/login?registered=true&next=/onboarding&message=${encodeURIComponent(message)}`);
    } catch (err: any) {
      // FluentValidation's auto-validation (RegisterRequestValidator) returns the ASP.NET
      // Core ValidationProblemDetails shape on 400 — { errors: { Field: ["msg", ...] } } —
      // not { message } or { detail }, so those two alone silently swallowed every
      // validation failure (weak password, missing name, ...) behind the generic fallback.
      const validationErrors = err.response?.data?.errors;
      const validationMessage = validationErrors && typeof validationErrors === "object"
        ? Object.values(validationErrors).flat().join(" ")
        : null;
      setError(validationMessage || err.response?.data?.message || err.response?.data?.detail || "Registration failed. Please verify your fields and try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const updateField = (field: keyof typeof formData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <div className="min-h-screen flex bg-background">
      {/* Left Pane - Form */}
      <div className="flex-1 flex flex-col justify-center py-12 px-4 sm:px-6 lg:flex-none lg:px-20 xl:px-24 animate-fade-in-up">
        <div className="mx-auto w-full max-w-sm lg:w-96">
          <div>
            <div className="h-10 w-10 bg-primary/10 text-primary rounded-lg flex items-center justify-center mb-6 shadow-glow">
              <Sparkles className="w-6 h-6" />
            </div>
            <h2 className="text-3xl font-extrabold text-foreground tracking-tight font-display">Create your workspace</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Already have an account? <Link href="/login" className="font-medium text-primary hover:text-primary/90">Sign in here</Link>
            </p>
          </div>

          <div className="mt-8">
            <form onSubmit={handleRegister} className="space-y-5">
              {error && (
                <div className="bg-destructive/10 border border-destructive/20 text-destructive p-3 rounded-md text-sm font-medium animate-shake">
                  {error}
                </div>
              )}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-muted-foreground mb-1">First Name</label>
                  <Input 
                    id="firstName"
                    name="firstName"
                    autoComplete="given-name"
                    required 
                    placeholder="Jane" 
                    value={formData.firstName}
                    onChange={(e) => updateField("firstName", e.target.value)}
                    disabled={isLoading}
                    className="shadow-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-muted-foreground mb-1">Last Name</label>
                  <Input 
                    id="lastName"
                    name="lastName"
                    autoComplete="family-name"
                    required 
                    placeholder="Doe" 
                    value={formData.lastName}
                    onChange={(e) => updateField("lastName", e.target.value)}
                    disabled={isLoading}
                    className="shadow-sm"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">Work Email</label>
                <Input 
                  id="email"
                  name="email"
                  autoComplete="email"
                  required 
                  type="email" 
                  placeholder="jane@company.com" 
                  value={formData.email}
                  onChange={(e) => updateField("email", e.target.value)}
                  disabled={isLoading}
                  className="shadow-sm"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">Company Name</label>
                <div className="relative rounded-md group">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Building2 className="h-4 w-4 text-muted-foreground group-focus-within:text-primary transition-colors" />
                  </div>
                  <Input 
                    id="organization"
                    name="organization"
                    autoComplete="organization"
                    required 
                    placeholder="Acme Corp" 
                    className="pl-10 shadow-sm"
                    value={formData.company}
                    onChange={(e) => updateField("company", e.target.value)}
                    disabled={isLoading}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">
                  Password <span className="text-danger-fg" aria-hidden="true">*</span>
                </label>
                <Input
                  id="password"
                  name="password"
                  autoComplete="new-password"
                  required
                  type="password"
                  placeholder="At least 8 characters"
                  value={formData.password}
                  onChange={(e) => updateField("password", e.target.value)}
                  disabled={isLoading}
                  className="shadow-sm"
                  aria-describedby="password-strength"
                />
                {formData.password.length > 0 && (
                  <div id="password-strength" className="mt-2" aria-live="polite">
                    <div className="flex gap-1 mb-1.5">
                      {PASSWORD_RULES.map((_, i) => (
                        <div
                          key={i}
                          className={`h-1 flex-1 rounded-full transition-colors duration-300 ${i < strength.score ? strength.color : 'bg-gray-200'}`}
                        />
                      ))}
                    </div>
                    {strength.label && (
                      <p className={`text-xs font-medium mb-1.5 ${strength.meetsServerRules ? 'text-success-fg' : strength.score >= 3 ? 'text-blue-600' : 'text-warning-fg'}`}>
                        <ShieldCheck className="inline w-3 h-3 mr-1" aria-hidden="true" />
                        {strength.label}
                      </p>
                    )}
                    {/* Every rule the server enforces, shown individually. A single bar could
                        read "Strong" while a required rule was still unmet. */}
                    {!strength.meetsServerRules && (
                      <ul className="space-y-0.5 list-none p-0 m-0">
                        {PASSWORD_RULES.map((rule) => {
                          const passed = rule.test(formData.password);
                          return (
                            <li
                              key={rule.id}
                              className={`flex items-center gap-1.5 text-xs ${passed ? 'text-success-fg' : 'text-muted-foreground'}`}
                            >
                              {passed
                                ? <Check className="w-3 h-3 flex-shrink-0" aria-hidden="true" />
                                : <X className="w-3 h-3 flex-shrink-0" aria-hidden="true" />}
                              <span>{rule.label}</span>
                            </li>
                          );
                        })}
                      </ul>
                    )}
                  </div>
                )}
              </div>
              
              <div className="pt-2">
                <Button type="submit" disabled={isLoading} className="w-full py-6 text-base font-semibold shadow-glow">
                  {isLoading ? (
                    "Creating Workspace..."
                  ) : (
                    <><UserPlus className="w-5 h-5 mr-2" /> Start 14-Day Free Trial</>
                  )}
                </Button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Right Pane - Feature Highlight */}
      <div className="hidden lg:block relative w-0 flex-1 bg-muted/30 border-l border-border">
        <div className="absolute inset-0 h-full w-full object-cover flex flex-col items-center justify-center p-12">
          <div className="max-w-md w-full">
            <h3 className="text-2xl font-bold text-foreground mb-6 font-display">Everything you need to scale your service business.</h3>
            <ul className="space-y-4">
              {[
                "Unlimited Staff Calendars",
                "Advanced CRM Pipelines",
                "Payment & Subscription Processing",
                "Automated Marketing Campaigns",
                "Self-Serve Client Portals"
              ].map((feature, idx) => (
                <li key={idx} className="flex items-center gap-3 text-muted-foreground animate-fade-in" style={{ animationDelay: `${idx * 100}ms` }}>
                  <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
                    <svg className="w-4 h-4 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                  {feature}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * useSearchParams opts the subtree into client-side rendering, so Next requires a Suspense
 * boundary around it or the whole route falls back to dynamic rendering at build time.
 * Same pattern as the verify-email page.
 */
export default function RegisterPage() {
  return (
    <Suspense fallback={<div className="min-h-screen bg-background" />}>
      <RegisterForm />
    </Suspense>
  );
}
