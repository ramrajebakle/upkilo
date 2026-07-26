"use client";

import React, { useState, useEffect, useCallback } from "react";
import { UserCheck, UserX, Clock, Users, TrendingUp, Loader2, RefreshCw, Calendar } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface AttendanceRecord {
  staffId: string;
  staffName: string;
  date: string;
  clockInTime?: string;
  clockOutTime?: string;
  totalHours?: number;
  status: "Present" | "Absent" | "Late" | "Active";
  ipAddress?: string;
}

interface AttendanceStats {
  presentToday: number;
  absentToday: number;
  avgHoursPerDay: number;
  totalStaff: number;
}

const STATUS_CONFIG: Record<string, { color: string; bg: string }> = {
  Present: { color: "text-green-600", bg: "bg-green-50" },
  Active:  { color: "text-blue-600",  bg: "bg-blue-50" },
  Late:    { color: "text-amber-600", bg: "bg-amber-50" },
  Absent:  { color: "text-red-600",   bg: "bg-red-50" },
};

export default function StaffAttendancePage() {
  const { error: toastError } = useToast();
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [stats, setStats] = useState<AttendanceStats>({ presentToday: 0, absentToday: 0, avgHoursPerDay: 0, totalStaff: 0 });
  const [loading, setLoading] = useState(true);
  const [date, setDate] = useState(() => new Date().toISOString().split("T")[0]);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [recRes, statsRes] = await Promise.all([
        apiClient.get("/api/v1/attendance", { params: { date } }).catch(() => ({ data: [] })),
        apiClient.get("/api/v1/attendance/stats").catch(() => ({ data: {} })),
      ]);
      const d: AttendanceRecord[] = Array.isArray(recRes.data) ? recRes.data : recRes.data?.data ?? [];
      setRecords(d);
      const s = statsRes.data?.data ?? statsRes.data ?? {};
      setStats({
        presentToday: s.presentToday ?? d.filter((r) => r.status !== "Absent").length,
        absentToday: s.absentToday ?? d.filter((r) => r.status === "Absent").length,
        avgHoursPerDay: s.avgHoursPerDay ?? 0,
        totalStaff: s.totalStaff ?? d.length,
      });
    } catch { toastError("Failed to load attendance"); }
    finally { setLoading(false); }
  }, [date]);

  useEffect(() => { fetch(); }, [fetch]);

  const fmt = (iso?: string) => iso ? new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "—";

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Staff Attendance <UserCheck className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Daily attendance tracking and clock-in status.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Present today", value: stats.presentToday, icon: UserCheck, color: "text-green-500" },
          { label: "Absent today", value: stats.absentToday, icon: UserX, color: "text-red-500" },
          { label: "Avg hours/day", value: stats.avgHoursPerDay ? `${stats.avgHoursPerDay.toFixed(1)}h` : "—", icon: Clock, color: "text-blue-500" },
          { label: "Total staff", value: stats.totalStaff, icon: Users, color: "text-purple-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-xs font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent><p className={`text-2xl font-bold ${s.color}`}>{s.value}</p></CardContent>
          </Card>
        ))}
      </div>

      <div className="flex items-center gap-3">
        <Calendar className="h-4 w-4 text-text-tertiary" />
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        <span className="text-sm text-text-secondary">
          {new Date(date).toLocaleDateString([], { weekday: "long", month: "long", day: "numeric" })}
        </span>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><TrendingUp className="h-4 w-4" /> Attendance Log</CardTitle>
          <CardDescription>{records.length} records</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : records.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <UserCheck className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No attendance records for this date</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Staff", "Status", "Clock In", "Clock Out", "Hours", "IP"].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {records.map((r, i) => {
                    const cfg = STATUS_CONFIG[r.status] ?? STATUS_CONFIG.Absent;
                    return (
                      <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                        <td className="py-3 px-3 font-medium text-text-primary">{r.staffName}</td>
                        <td className="py-3 px-3"><span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{r.status}</span></td>
                        <td className="py-3 px-3 text-text-primary font-mono text-xs">{fmt(r.clockInTime)}</td>
                        <td className="py-3 px-3 text-text-primary font-mono text-xs">{fmt(r.clockOutTime)}</td>
                        <td className="py-3 px-3 font-medium text-text-primary">{r.totalHours ? `${r.totalHours.toFixed(1)}h` : "—"}</td>
                        <td className="py-3 px-3 text-text-tertiary text-xs font-mono">{r.ipAddress ?? "—"}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
        </CardContent>
      </Card>
    </div>
  );
}
