"use client";

import React, { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  UserPlus, Search, Clock, Briefcase, UserCog, CheckCircle2,
  Loader2, ChevronRight, ArrowLeft, Users, Zap,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Service {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
  category?: string;
}

interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  role?: string;
}

interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
}

type Step = "service" | "staff" | "client" | "confirm";

export default function WalkInPage() {
  const router = useRouter();
  const { success: toastSuccess, error: toastError } = useToast();

  const [step, setStep] = useState<Step>("service");
  const [services, setServices] = useState<Service[]>([]);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [loadingServices, setLoadingServices] = useState(true);
  const [loadingStaff, setLoadingStaff] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [selectedStaff, setSelectedStaff] = useState<StaffMember | null>(null);
  const [selectedClient, setSelectedClient] = useState<Client | null>(null);
  const [clientSearch, setClientSearch] = useState("");
  const [serviceSearch, setServiceSearch] = useState("");
  const [notes, setNotes] = useState("");
  const [groupSize, setGroupSize] = useState(1);
  const [autoAssign, setAutoAssign] = useState(true);

  useEffect(() => {
    apiClient
      .get("/api/v1/services")
      .then((r) => {
        const d = r.data?.data ?? r.data ?? [];
        setServices(Array.isArray(d) ? d : []);
      })
      .catch(() => {})
      .finally(() => setLoadingServices(false));
  }, []);

  useEffect(() => {
    if (step === "staff" && !autoAssign) {
      setLoadingStaff(true);
      apiClient
        .get("/api/v1/staff")
        .then((r) => {
          const d = r.data?.data ?? r.data ?? [];
          setStaff(Array.isArray(d) ? d : []);
        })
        .catch(() => {})
        .finally(() => setLoadingStaff(false));
    }
  }, [step, autoAssign]);

  const searchClients = useCallback(async (q: string) => {
    if (q.length < 2) { setClients([]); return; }
    try {
      const r = await apiClient.get("/api/v1/clients", { params: { search: q, limit: 8 } });
      const d = r.data?.data ?? r.data ?? [];
      setClients(Array.isArray(d) ? d : []);
    } catch { setClients([]); }
  }, []);

  useEffect(() => {
    const t = setTimeout(() => searchClients(clientSearch), 300);
    return () => clearTimeout(t);
  }, [clientSearch, searchClients]);

  const handleSubmit = async () => {
    if (!selectedService) return;
    setSubmitting(true);
    try {
      const payload = {
        serviceId: selectedService.id,
        staffId: autoAssign ? null : selectedStaff?.id ?? null,
        clientId: selectedClient?.id ?? null,
        groupSize,
        notes: notes || null,
        startTime: new Date().toISOString(),
      };
      await apiClient.post("/api/v1/bookings/walk-in", payload);
      toastSuccess("Walk-in booking created");
      router.push("/bookings");
    } catch (err: any) {
      toastError(err?.response?.data?.error ?? "Failed to create walk-in booking");
    } finally {
      setSubmitting(false);
    }
  };

  const filteredServices = services.filter((s) =>
    !serviceSearch || s.name.toLowerCase().includes(serviceSearch.toLowerCase()) ||
    s.category?.toLowerCase().includes(serviceSearch.toLowerCase())
  );

  const STEPS: { key: Step; label: string }[] = [
    { key: "service", label: "Service" },
    { key: "staff", label: "Staff" },
    { key: "client", label: "Client" },
    { key: "confirm", label: "Confirm" },
  ];

  const stepIndex = STEPS.findIndex((s) => s.key === step);

  return (
    <div className="max-w-2xl mx-auto space-y-6 animate-fade-in">
      <header className="flex items-center gap-3 border-b border-surface-200 pb-6">
        <button
          onClick={() => router.push("/bookings")}
          className="p-2 rounded-lg hover:bg-surface-100 transition-colors text-text-tertiary"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-text-primary flex items-center gap-2">
            Walk-In Booking
            <Zap className="h-5 w-5 text-warning-fg" />
          </h1>
          <p className="text-sm text-text-secondary mt-0.5">Create an instant booking for a walk-in client.</p>
        </div>
      </header>

      {/* Step progress */}
      <div className="flex items-center gap-2">
        {STEPS.map((s, i) => (
          <React.Fragment key={s.key}>
            <div className={cn(
              "flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold transition-colors",
              i < stepIndex ? "bg-green-100 text-green-700" :
              i === stepIndex ? "bg-ai-subtle text-ai" :
              "bg-surface-100 text-text-tertiary"
            )}>
              {i < stepIndex && <CheckCircle2 className="h-3 w-3" />}
              {s.label}
            </div>
            {i < STEPS.length - 1 && (
              <ChevronRight className="h-3.5 w-3.5 text-text-tertiary flex-shrink-0" />
            )}
          </React.Fragment>
        ))}
      </div>

      {/* Step: Service */}
      {step === "service" && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Briefcase className="h-4 w-4" /> Select Service
            </CardTitle>
            <CardDescription>Which service does the client want?</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
              <input
                type="text"
                placeholder="Search services…"
                value={serviceSearch}
                onChange={(e) => setServiceSearch(e.target.value)}
                className="w-full pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 focus:outline-none focus:ring-2 focus:ring-ai-500"
              />
            </div>
            {loadingServices ? (
              <div className="flex justify-center py-8"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            ) : (
              <div className="space-y-2 max-h-80 overflow-y-auto pr-1">
                {filteredServices.map((svc) => (
                  <button
                    key={svc.id}
                    onClick={() => { setSelectedService(svc); setStep("staff"); }}
                    className={cn(
                      "w-full text-left p-3 rounded-xl border transition-all hover:shadow-sm",
                      selectedService?.id === svc.id
                        ? "border-ai-400 bg-ai-subtle"
                        : "border-surface-200 hover:border-surface-300 bg-surface-50"
                    )}
                  >
                    <div className="flex justify-between items-center">
                      <div>
                        <p className="font-medium text-text-primary text-sm">{svc.name}</p>
                        <p className="text-xs text-text-tertiary mt-0.5">
                          <Clock className="inline h-3 w-3 me-1" />{svc.durationMinutes} min
                          {svc.category && <span className="ms-2 opacity-60">· {svc.category}</span>}
                        </p>
                      </div>
                      <span className="font-semibold text-text-primary text-sm">${svc.price}</span>
                    </div>
                  </button>
                ))}
                {filteredServices.length === 0 && (
                  <p className="text-center py-8 text-text-tertiary text-sm">No services found</p>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Step: Staff */}
      {step === "staff" && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <UserCog className="h-4 w-4" /> Assign Staff
            </CardTitle>
            <CardDescription>For service: <strong>{selectedService?.name}</strong></CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <button
              onClick={() => { setAutoAssign(true); setSelectedStaff(null); setStep("client"); }}
              className={cn(
                "w-full text-left p-4 rounded-xl border-2 transition-all",
                autoAssign ? "border-ai-400 bg-ai-subtle" : "border-surface-200 hover:border-ai/25"
              )}
            >
              <div className="flex items-center gap-3">
                <Zap className="h-5 w-5 text-ai" />
                <div>
                  <p className="font-semibold text-text-primary text-sm">Auto-assign</p>
                  <p className="text-xs text-text-secondary">Assign the least busy available staff member</p>
                </div>
              </div>
            </button>

            <p className="text-xs font-semibold text-text-tertiary uppercase tracking-wider text-center">
              or choose manually
            </p>

            {loadingStaff ? (
              <div className="flex justify-center py-4"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            ) : (
              <div className="space-y-2 max-h-64 overflow-y-auto pr-1">
                {staff.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => { setAutoAssign(false); setSelectedStaff(s); setStep("client"); }}
                    className="w-full text-left p-3 rounded-xl border border-surface-200 hover:border-surface-300 bg-surface-50 transition-all hover:shadow-sm"
                  >
                    <p className="font-medium text-text-primary text-sm">{s.firstName} {s.lastName}</p>
                    {s.role && <p className="text-xs text-text-tertiary">{s.role}</p>}
                  </button>
                ))}
              </div>
            )}

            <div className="flex justify-between pt-2">
              <Button variant="outline" size="sm" onClick={() => setStep("service")}>Back</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step: Client */}
      {step === "client" && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-4 w-4" /> Client
            </CardTitle>
            <CardDescription>Existing client or anonymous walk-in</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
              <input
                type="text"
                placeholder="Search client by name, email, or phone…"
                value={clientSearch}
                onChange={(e) => setClientSearch(e.target.value)}
                className="w-full pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 focus:outline-none focus:ring-2 focus:ring-ai-500"
              />
            </div>

            {clients.length > 0 && (
              <div className="space-y-1.5 max-h-48 overflow-y-auto pr-1">
                {clients.map((c) => (
                  <button
                    key={c.id}
                    onClick={() => { setSelectedClient(c); setClientSearch(`${c.firstName} ${c.lastName}`); }}
                    className={cn(
                      "w-full text-left p-3 rounded-xl border transition-all text-sm",
                      selectedClient?.id === c.id
                        ? "border-ai-400 bg-ai-subtle"
                        : "border-surface-200 hover:border-surface-300"
                    )}
                  >
                    <p className="font-medium text-text-primary">{c.firstName} {c.lastName}</p>
                    <p className="text-xs text-text-tertiary">{c.email ?? c.phone}</p>
                  </button>
                ))}
              </div>
            )}

            <div className="border-t border-surface-200 pt-3">
              <p className="text-xs text-text-tertiary mb-2">Group size</p>
              <div className="flex items-center gap-3">
                <button
                  onClick={() => setGroupSize(Math.max(1, groupSize - 1))}
                  className="w-8 h-8 rounded-full border border-surface-200 flex items-center justify-center text-text-primary hover:bg-surface-100"
                >—</button>
                <span className="text-lg font-bold text-text-primary w-6 text-center">{groupSize}</span>
                <button
                  onClick={() => setGroupSize(groupSize + 1)}
                  className="w-8 h-8 rounded-full border border-surface-200 flex items-center justify-center text-text-primary hover:bg-surface-100"
                >+</button>
              </div>
            </div>

            <div className="flex justify-between pt-2">
              <Button variant="outline" size="sm" onClick={() => setStep("staff")}>Back</Button>
              <Button variant="primary" size="sm" onClick={() => setStep("confirm")}>
                Continue
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step: Confirm */}
      {step === "confirm" && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CheckCircle2 className="h-4 w-4 text-success-fg" /> Confirm Walk-In
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="bg-surface-50 rounded-xl p-4 space-y-3 text-sm">
              {[
                { label: "Service", value: selectedService?.name },
                { label: "Duration", value: `${selectedService?.durationMinutes} min` },
                { label: "Price", value: `$${selectedService?.price}` },
                { label: "Staff", value: autoAssign ? "Auto-assign" : `${selectedStaff?.firstName} ${selectedStaff?.lastName}` },
                { label: "Client", value: selectedClient ? `${selectedClient.firstName} ${selectedClient.lastName}` : "Anonymous walk-in" },
                { label: "Group size", value: groupSize },
                { label: "Start time", value: "Now (" + new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) + ")" },
              ].map(({ label, value }) => (
                <div key={label} className="flex justify-between">
                  <span className="text-text-secondary">{label}</span>
                  <span className="font-medium text-text-primary">{value}</span>
                </div>
              ))}
            </div>

            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Notes (optional)</label>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={2}
                placeholder="Any special requests or notes…"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none"
              />
            </div>

            <div className="flex justify-between pt-2">
              <Button variant="outline" onClick={() => setStep("client")}>Back</Button>
              <Button
                variant="primary"
                leftIcon={submitting ? <Loader2 size={15} className="animate-spin" /> : <Zap size={15} />}
                onClick={handleSubmit}
                disabled={submitting}
              >
                {submitting ? "Creating…" : "Create Walk-In"}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
