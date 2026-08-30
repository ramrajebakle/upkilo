"use client";

import React, { useState, useEffect } from "react";
import { Settings, Save, Loader2, RefreshCw, Clock, CreditCard, Bell, Shield } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface BookingPolicies {
  minAdvanceNoticeHours: number;
  maxAdvanceDays: number;
  cancellationWindowHours: number;
  noShowFeePercent: number;
  requireDepositPercent: number;
  depositRefundWindowHours: number;
  allowOnlineRescheduling: boolean;
  allowOnlineCancellation: boolean;
  sendReminderHoursBefore: number;
  requireClientConfirmation: boolean;
  autoConfirmBookings: boolean;
  bufferBetweenAppointmentsMinutes: number;
}

const DEFAULT: BookingPolicies = {
  minAdvanceNoticeHours: 2,
  maxAdvanceDays: 60,
  cancellationWindowHours: 24,
  noShowFeePercent: 0,
  requireDepositPercent: 0,
  depositRefundWindowHours: 48,
  allowOnlineRescheduling: true,
  allowOnlineCancellation: true,
  sendReminderHoursBefore: 24,
  requireClientConfirmation: false,
  autoConfirmBookings: true,
  bufferBetweenAppointmentsMinutes: 0,
};

export default function BookingPoliciesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [policies, setPolicies] = useState<BookingPolicies>(DEFAULT);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    apiClient.get("/api/v1/settings/booking").then((r) => {
      setPolicies({ ...DEFAULT, ...(r.data?.data ?? r.data ?? {}) });
    }).catch(() => {}).finally(() => setLoading(false));
  }, []);

  const setNum = (k: keyof BookingPolicies, v: string) => setPolicies((p) => ({ ...p, [k]: parseFloat(v) || 0 }));
  const setBool = (k: keyof BookingPolicies, v: boolean) => setPolicies((p) => ({ ...p, [k]: v }));

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiClient.put("/api/v1/settings/booking", policies);
      toastSuccess("Booking policies saved");
    } catch { toastError("Failed to save policies"); }
    finally { setSaving(false); }
  };

  const NumberField = ({ label, field, min = 0, unit = "" }: { label: string; field: keyof BookingPolicies; min?: number; unit?: string }) => (
    <div>
      <label className="block text-sm font-medium text-text-primary mb-1">{label}</label>
      <div className="flex items-center gap-2">
        <input type="number" min={min} value={policies[field] as number} onChange={(e) => setNum(field, e.target.value)}
          className="w-28 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        {unit && <span className="text-sm text-text-tertiary">{unit}</span>}
      </div>
    </div>
  );

  const Toggle = ({ label, desc, field }: { label: string; desc: string; field: keyof BookingPolicies }) => (
    <label className="flex items-start gap-3 cursor-pointer">
      <div className="mt-0.5">
        <input type="checkbox" checked={policies[field] as boolean} onChange={(e) => setBool(field, e.target.checked)} className="sr-only" />
        <div onClick={() => setBool(field, !(policies[field] as boolean))}
          className={`w-10 h-5 rounded-full transition-colors cursor-pointer ${policies[field] ? "bg-ai-500" : "bg-surface-300"} relative`}>
          <div className={`absolute top-0.5 w-4 h-4 bg-control-thumb rounded-full shadow transition-transform ${policies[field] ? "translate-x-5" : "translate-x-0.5"}`} />
        </div>
      </div>
      <div>
        <p className="text-sm font-medium text-text-primary">{label}</p>
        <p className="text-xs text-text-tertiary">{desc}</p>
      </div>
    </label>
  );

  if (loading) return <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>;

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Booking Policies <Settings className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Set rules for how clients can book, cancel, and reschedule.</p>
        </div>
      </header>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Clock className="h-4 w-4" /> Timing Rules</CardTitle></CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <NumberField label="Min. advance notice" field="minAdvanceNoticeHours" unit="hours" />
          <NumberField label="Max. days ahead" field="maxAdvanceDays" unit="days" />
          <NumberField label="Cancellation window" field="cancellationWindowHours" unit="hours" />
          <NumberField label="Buffer between appointments" field="bufferBetweenAppointmentsMinutes" unit="min" />
          <NumberField label="Reminder before" field="sendReminderHoursBefore" unit="hours" />
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><CreditCard className="h-4 w-4" /> Deposits & Fees</CardTitle></CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <NumberField label="Required deposit" field="requireDepositPercent" unit="% of total" />
          <NumberField label="Deposit refund window" field="depositRefundWindowHours" unit="hours" />
          <NumberField label="No-show fee" field="noShowFeePercent" unit="% of total" />
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Shield className="h-4 w-4" /> Permissions</CardTitle></CardHeader>
        <CardContent className="space-y-5">
          <Toggle label="Allow online rescheduling" desc="Clients can reschedule via the booking portal" field="allowOnlineRescheduling" />
          <Toggle label="Allow online cancellation" desc="Clients can cancel without calling" field="allowOnlineCancellation" />
          <Toggle label="Auto-confirm bookings" desc="No manual review required for new bookings" field="autoConfirmBookings" />
          <Toggle label="Require client confirmation" desc="Client must confirm booking via email/SMS" field="requireClientConfirmation" />
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
          onClick={handleSave} disabled={saving}>
          {saving ? "Saving…" : "Save Policies"}
        </Button>
      </div>
    </div>
  );
}
