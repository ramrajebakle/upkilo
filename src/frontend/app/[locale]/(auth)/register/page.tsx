"use client";

import React, { useState } from "react";
import { useRouter, Link } from "@/navigation";
import { UserPlus, Sparkles, Building2, ShieldCheck } from "lucide-react";

function getPasswordStrength(pw: string): { score: 0 | 1 | 2 | 3 | 4; label: string; color: string } {
  let score = 0;
  if (pw.length >= 8) score++;
  if (/[A-Z]/.test(pw)) score++;
  if (/[0-9]/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;
  const levels = [
    { label: '', color: 'bg-gray-200' },
    { label: 'Weak', color: 'bg-red-500' },
    { label: 'Fair', color: 'bg-amber-500' },
    { label: 'Good', color: 'bg-blue-500' },
    { label: 'Strong', color: 'bg-emerald-500' },
  ];
  return { score: score as 0|1|2|3|4, ...levels[score] };
}
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { api } from "@/lib/api";

export default function RegisterPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    company: "",
    password: ""
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
        companyName: formData.company // Note: DB schema maps it to companyName
      });
      // Redirect to login with next=/onboarding so after sign-in the user goes through setup
      const message = response.data?.message || "Account created! Sign in to get started.";
      router.push(`/login?registered=true&next=/onboarding&message=${encodeURIComponent(message)}`);
    } catch (err: any) {
      // The API often returns 400 Bad Request with Validation errors or a raw error message
      setError(err.response?.data?.message || err.response?.data?.detail || "Registration failed. Please verify your fields and try again.");
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
                  Password <span className="text-red-500" aria-hidden="true">*</span>
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
                {formData.password.length > 0 && (() => {
                  const strength = getPasswordStrength(formData.password);
                  return (
                    <div id="password-strength" className="mt-2" aria-live="polite">
                      <div className="flex gap-1 mb-1">
                        {[1, 2, 3, 4].map((i) => (
                          <div
                            key={i}
                            className={`h-1 flex-1 rounded-full transition-colors duration-300 ${i <= strength.score ? strength.color : 'bg-gray-200'}`}
                          />
                        ))}
                      </div>
                      {strength.label && (
                        <p className={`text-xs font-medium ${strength.score >= 3 ? 'text-emerald-600' : strength.score === 2 ? 'text-blue-600' : 'text-amber-600'}`}>
                          <ShieldCheck className="inline w-3 h-3 mr-1" aria-hidden="true" />
                          {strength.label}
                        </p>
                      )}
                    </div>
                  );
                })()}
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
