"use client";

import React, { useState, useEffect, useCallback } from "react";
import { CheckCircle2, Search, Clock, User, Loader2, RefreshCw, Calendar } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Booking { id: string; clientName: string; serviceName: string; startTime: string; staffName?: string; status: string; checkedInAt?: string; }

export default function CheckInPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [checkingIn, setCheckingIn] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const today = new Date().toISOString().split("T")[0];
      const r = await apiClient.get(`/api/v1/bookings?date=${today}&status=Confirmed,CheckedIn`).catch(() => ({ data: [] }));
      setBookings(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const checkIn = async (id: string) => {
    setCheckingIn(id);
    try {
      await apiClient.put(`/api/v1/bookings/${id}/check-in`);
      toastSuccess("Client checked in"); setBookings((b) => b.map((x) => x.id === id ? { ...x, status: "CheckedIn", checkedInAt: new Date().toISOString() } : x));
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Check-in failed"); }
    finally { setCheckingIn(null); }
  };

  const filtered = bookings.filter((b) => !search || b.clientName?.toLowerCase().includes(search.toLowerCase()) || b.serviceName?.toLowerCase().includes(search.toLowerCase()));
  const pending = filtered.filter((b) => b.status !== "CheckedIn");
  const done = filtered.filter((b) => b.status === "CheckedIn");

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Front Desk Check-In <CheckCircle2 className="text-success-fg" size={22} /></h1>
          <p className="text-text-secondary mt-1">Today's appointments — mark clients as arrived.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Today's Appointments", value: bookings.length, color: "text-text-primary" },
          { label: "Awaiting Check-in", value: pending.length, color: "text-warning-fg" },
          { label: "Checked In", value: done.length, color: "text-success-fg" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search client or service…"
          className="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {pending.length > 0 && (
            <div>
              <h2 className="text-sm font-semibold text-text-primary mb-3">Awaiting Arrival</h2>
              <div className="space-y-2">
                {pending.map((b) => (
                  <Card key={b.id}>
                    <CardContent className="pt-3 pb-3 flex items-center gap-4">
                      <div className="w-10 h-10 rounded-full bg-surface-100 flex items-center justify-center flex-shrink-0">
                        <User className="h-4 w-4 text-text-tertiary" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-text-primary truncate">{b.clientName}</p>
                        <div className="flex items-center gap-3 mt-0.5">
                          <span className="text-xs text-text-secondary">{b.serviceName}</span>
                          {b.staffName && <span className="text-xs text-text-tertiary">with {b.staffName}</span>}
                          <span className="text-xs text-text-tertiary flex items-center gap-1"><Clock className="h-3 w-3" />{new Date(b.startTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</span>
                        </div>
                      </div>
                      <Button variant="primary" size="sm" leftIcon={checkingIn === b.id ? <Loader2 size={12} className="animate-spin" /> : <CheckCircle2 size={12} />}
                        onClick={() => checkIn(b.id)} disabled={!!checkingIn}>
                        {checkingIn === b.id ? "…" : "Check In"}
                      </Button>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          )}

          {done.length > 0 && (
            <div>
              <h2 className="text-sm font-semibold text-text-primary mb-3">Checked In</h2>
              <div className="space-y-2">
                {done.map((b) => (
                  <Card key={b.id} className="border-green-200 bg-green-50/30">
                    <CardContent className="pt-3 pb-3 flex items-center gap-4">
                      <div className="w-10 h-10 rounded-full bg-green-100 flex items-center justify-center flex-shrink-0">
                        <CheckCircle2 className="h-4 w-4 text-success-fg" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-text-primary truncate">{b.clientName}</p>
                        <div className="flex items-center gap-3 mt-0.5">
                          <span className="text-xs text-text-secondary">{b.serviceName}</span>
                          {b.checkedInAt && <span className="text-xs text-success-fg">Arrived {new Date(b.checkedInAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</span>}
                        </div>
                      </div>
                      <span className="text-xs font-medium text-green-600 bg-green-50 border border-green-200 px-2 py-0.5 rounded-full">Arrived</span>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          )}

          {filtered.length === 0 && (
            <Card><CardContent className="text-center py-12 text-text-tertiary">
              <Calendar className="h-10 w-10 mx-auto mb-3 opacity-20" />
              <p className="font-medium">No appointments found for today</p>
            </CardContent></Card>
          )}
        </>
      )}
    </div>
  );
}
