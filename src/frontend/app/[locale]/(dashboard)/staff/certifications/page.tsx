"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Award, Plus, Loader2, RefreshCw, CheckCircle2, AlertTriangle, Calendar } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Certification {
  id: string;
  staffId: string;
  staffName?: string;
  certificationName: string;
  issuingBody?: string;
  issueDate?: string;
  expiryDate?: string;
  certificateNumber?: string;
  isVerified: boolean;
}

interface StaffMember { id: string; firstName: string; lastName: string; }

function CertForm({ staff, onSave, onCancel, saving }: {
  staff: StaffMember[];
  onSave: (d: Omit<Certification, "id" | "staffName" | "isVerified">) => Promise<void>;
  onCancel: () => void;
  saving: boolean;
}) {
  const [form, setForm] = useState({ staffId: "", certificationName: "", issuingBody: "", issueDate: "", expiryDate: "", certificateNumber: "" });
  const set = (k: string, v: string) => setForm((f) => ({ ...f, [k]: v }));
  return (
    <div className="p-5 bg-surface-50 rounded-xl border border-surface-200 space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="sm:col-span-2">
          <label className="block text-xs font-medium text-text-secondary mb-1">Staff Member *</label>
          <select value={form.staffId} onChange={(e) => set("staffId", e.target.value)}
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
            <option value="">Select staff member…</option>
            {staff.map((s) => <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>)}
          </select>
        </div>
        {[
          { key: "certificationName", label: "Certification Name *", placeholder: "e.g. Advanced Esthetics" },
          { key: "issuingBody", label: "Issuing Body", placeholder: "e.g. CIDESCO" },
          { key: "certificateNumber", label: "Certificate No.", placeholder: "e.g. CIDC-2024-001" },
          { key: "issueDate", label: "Issue Date", type: "date" },
          { key: "expiryDate", label: "Expiry Date", type: "date" },
        ].map((f) => (
          <div key={f.key}>
            <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
            <input type={f.type ?? "text"} value={(form as any)[f.key]} onChange={(e) => set(f.key, e.target.value)} placeholder={f.placeholder}
              className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          </div>
        ))}
      </div>
      <div className="flex justify-end gap-2">
        <Button variant="outline" size="sm" onClick={onCancel} disabled={saving}>Cancel</Button>
        <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
          onClick={() => onSave(form)} disabled={!form.staffId || !form.certificationName.trim() || saving}>
          {saving ? "Saving…" : "Add Certification"}
        </Button>
      </div>
    </div>
  );
}

export default function StaffCertificationsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [certs, setCerts] = useState<Certification[]>([]);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [staffFilter, setStaffFilter] = useState("all");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [certsRes, staffRes] = await Promise.all([
        apiClient.get("/api/v1/staffcertifications").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/staff").catch(() => ({ data: [] })),
      ]);
      const d: Certification[] = Array.isArray(certsRes.data) ? certsRes.data : certsRes.data?.data ?? [];
      setCerts(d);
      const s: StaffMember[] = Array.isArray(staffRes.data) ? staffRes.data : staffRes.data?.data ?? [];
      setStaff(s);
    } catch { toastError("Failed to load certifications"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const handleSave = async (data: Omit<Certification, "id" | "staffName" | "isVerified">) => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/staffcertifications", data);
      toastSuccess("Certification added"); setShowForm(false); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  const isExpiring = (date?: string) => {
    if (!date) return false;
    const diff = new Date(date).getTime() - Date.now();
    return diff > 0 && diff < 30 * 24 * 60 * 60 * 1000;
  };
  const isExpired = (date?: string) => date ? new Date(date) < new Date() : false;

  const filtered = certs.filter((c) => staffFilter === "all" || c.staffId === staffFilter);

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Certifications <Award className="text-warning-fg" size={22} /></h1>
          <p className="text-text-secondary mt-1">Track staff qualifications and licence expiry dates.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add Certification</Button>
        </div>
      </header>

      {showForm && <CertForm staff={staff} onSave={handleSave} onCancel={() => setShowForm(false)} saving={saving} />}

      <div className="flex gap-2 flex-wrap">
        <button onClick={() => setStaffFilter("all")} className={cn("px-3 py-1.5 rounded-lg text-sm font-medium transition-colors", staffFilter === "all" ? "bg-ai-500 text-white" : "bg-surface-100 text-text-secondary hover:bg-surface-200")}>All Staff</button>
        {staff.map((s) => (
          <button key={s.id} onClick={() => setStaffFilter(s.id)}
            className={cn("px-3 py-1.5 rounded-lg text-sm font-medium transition-colors", staffFilter === s.id ? "bg-ai-500 text-white" : "bg-surface-100 text-text-secondary hover:bg-surface-200")}>
            {s.firstName} {s.lastName}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Award className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No certifications recorded</p>
            <Button variant="primary" className="mt-4" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add first certification</Button>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((c) => {
              const expired = isExpired(c.expiryDate);
              const expiring = !expired && isExpiring(c.expiryDate);
              return (
                <Card key={c.id} className={cn(expired && "border-red-200", expiring && "border-amber-200")}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex items-start gap-3">
                        <Award className={cn("h-5 w-5 mt-0.5 flex-shrink-0", expired ? "text-red-400" : expiring ? "text-amber-400" : "text-warning-fg")} />
                        <div>
                          <div className="flex items-center gap-2 flex-wrap">
                            <p className="font-semibold text-text-primary">{c.certificationName}</p>
                            {c.isVerified && <CheckCircle2 className="h-3.5 w-3.5 text-success-fg" />}
                            {expired && <span className="text-xs font-medium text-red-600 bg-red-50 px-2 py-0.5 rounded-full">Expired</span>}
                            {expiring && <span className="text-xs font-medium text-amber-600 bg-amber-50 px-2 py-0.5 rounded-full flex items-center gap-1"><AlertTriangle className="h-3 w-3" />Expiring soon</span>}
                          </div>
                          <p className="text-sm text-text-secondary">{c.staffName ?? "Staff"}{c.issuingBody && ` · ${c.issuingBody}`}</p>
                          <div className="flex gap-4 mt-1.5 text-xs text-text-tertiary">
                            {c.certificateNumber && <span>#{c.certificateNumber}</span>}
                            {c.issueDate && <span className="flex items-center gap-1"><Calendar className="h-3 w-3" />Issued {new Date(c.issueDate).toLocaleDateString([], { month: "short", year: "numeric" })}</span>}
                            {c.expiryDate && <span className={cn("flex items-center gap-1", expired && "text-danger-fg", expiring && "text-warning-fg")}>
                              <Calendar className="h-3 w-3" />Expires {new Date(c.expiryDate).toLocaleDateString([], { month: "short", year: "numeric" })}
                            </span>}
                          </div>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
    </div>
  );
}
