"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  Clock, LogIn, LogOut, Users, Calendar, Download,
  CheckCircle2, AlertCircle, Loader2, RefreshCw, Filter,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Timesheet {
  id: string;
  staffId: string;
  staffName: string;
  clockInTime: string;
  clockOutTime?: string;
  totalMinutes?: number;
  notes?: string;
}

interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
}

interface ClockStatus {
  isClockedIn: boolean;
  clockedInSince?: string;
  currentEntryId?: string;
}

function formatDuration(minutes?: number) {
  if (!minutes) return "—";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}

function formatTime(iso?: string) {
  if (!iso) return "—";
  return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString([], { month: "short", day: "numeric", year: "numeric" });
}

export default function StaffTimesheetsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [timesheets, setTimesheets] = useState<Timesheet[]>([]);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [clockStatus, setClockStatus] = useState<ClockStatus>({ isClockedIn: false });
  const [loading, setLoading] = useState(true);
  const [clockLoading, setClockLoading] = useState(false);
  const [staffFilter, setStaffFilter] = useState("all");
  const [startDate, setStartDate] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 14);
    return d.toISOString().split("T")[0];
  });
  const [endDate] = useState(() => new Date().toISOString().split("T")[0]);

  const fetchTimesheets = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string> = { startDate, endDate };
      if (staffFilter !== "all") params.staffId = staffFilter;

      const [tsRes, staffRes, statusRes] = await Promise.all([
        apiClient.get("/api/v1/stafftimesheets", { params }).catch(() => ({ data: [] })),
        apiClient.get("/api/v1/staff").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/attendance/status").catch(() => ({ data: { isClockedIn: false } })),
      ]);

      const tsData = tsRes.data?.data ?? tsRes.data ?? [];
      setTimesheets(Array.isArray(tsData) ? tsData : []);

      const staffData = staffRes.data?.data ?? staffRes.data ?? [];
      setStaff(Array.isArray(staffData) ? staffData : []);

      setClockStatus(statusRes.data?.data ?? statusRes.data ?? { isClockedIn: false });
    } catch {
      toastError("Failed to load timesheet data");
    } finally {
      setLoading(false);
    }
  }, [startDate, endDate, staffFilter]);

  useEffect(() => {
    fetchTimesheets();
  }, [fetchTimesheets]);

  const handleClockIn = async () => {
    setClockLoading(true);
    try {
      await apiClient.post("/api/v1/attendance/clock-in", {});
      toastSuccess("Clocked in successfully");
      fetchTimesheets();
    } catch (err: any) {
      toastError(err?.response?.data?.error ?? "Failed to clock in");
    } finally {
      setClockLoading(false);
    }
  };

  const handleClockOut = async () => {
    setClockLoading(true);
    try {
      await apiClient.post("/api/v1/attendance/clock-out", {});
      toastSuccess("Clocked out successfully");
      fetchTimesheets();
    } catch (err: any) {
      toastError(err?.response?.data?.error ?? "Failed to clock out");
    } finally {
      setClockLoading(false);
    }
  };

  const totalHoursThisPeriod = timesheets.reduce((sum, t) => sum + (t.totalMinutes ?? 0), 0);
  const activeSessions = timesheets.filter((t) => !t.clockOutTime).length;

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            Staff Timesheets
            <Clock className="text-text-tertiary" size={22} />
          </h1>
          <p className="text-text-secondary mt-1">
            Clock-in / clock-out management and hours tracking.
          </p>
        </div>
        <Button
          variant="outline"
          leftIcon={<RefreshCw size={15} />}
          onClick={fetchTimesheets}
          disabled={loading}
        >
          Refresh
        </Button>
      </header>

      {/* Clock-in widget */}
      <Card className={cn(
        "border-2",
        clockStatus.isClockedIn
          ? "border-green-200 bg-green-50 dark:bg-green-950/20 dark:border-green-800"
          : "border-surface-200"
      )}>
        <CardContent className="pt-6">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className={cn(
                "w-12 h-12 rounded-full flex items-center justify-center",
                clockStatus.isClockedIn ? "bg-green-100 dark:bg-green-900" : "bg-surface-100"
              )}>
                <Clock className={cn("h-6 w-6", clockStatus.isClockedIn ? "text-green-600" : "text-text-tertiary")} />
              </div>
              <div>
                <p className="font-semibold text-text-primary">
                  {clockStatus.isClockedIn ? "Currently clocked in" : "Not clocked in"}
                </p>
                {clockStatus.isClockedIn && clockStatus.clockedInSince && (
                  <p className="text-sm text-text-secondary">
                    Since {formatTime(clockStatus.clockedInSince)} · {formatDate(clockStatus.clockedInSince)}
                  </p>
                )}
              </div>
            </div>
            {clockStatus.isClockedIn ? (
              <Button
                variant="outline"
                leftIcon={clockLoading ? <Loader2 size={15} className="animate-spin" /> : <LogOut size={15} />}
                onClick={handleClockOut}
                disabled={clockLoading}
                className="border-red-200 text-red-600 hover:bg-red-50"
              >
                Clock Out
              </Button>
            ) : (
              <Button
                variant="primary"
                leftIcon={clockLoading ? <Loader2 size={15} className="animate-spin" /> : <LogIn size={15} />}
                onClick={handleClockIn}
                disabled={clockLoading}
              >
                Clock In
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Summary stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {[
          { label: "Total hours (period)", value: formatDuration(totalHoursThisPeriod), icon: Clock, color: "text-blue-500" },
          { label: "Active sessions", value: activeSessions, icon: CheckCircle2, color: "text-green-500" },
          { label: "Staff members", value: staff.length, icon: Users, color: "text-primary-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent>
              <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 items-center">
        <Filter className="h-4 w-4 text-text-tertiary" />
        <input
          type="date"
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
        />
        <span className="text-text-tertiary text-sm">to</span>
        <input
          type="date"
          value={endDate}
          readOnly
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none"
        />
        <select
          value={staffFilter}
          onChange={(e) => setStaffFilter(e.target.value)}
          className="px-3 py-1.5 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
        >
          <option value="all">All staff</option>
          {staff.map((s) => (
            <option key={s.id} value={s.id}>
              {s.firstName} {s.lastName}
            </option>
          ))}
        </select>
      </div>

      {/* Timesheets table */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calendar className="h-4 w-4" /> Timesheet Entries
          </CardTitle>
          <CardDescription>
            {timesheets.length} entries in selected period
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-6 w-6 animate-spin text-text-tertiary" />
            </div>
          ) : timesheets.length === 0 ? (
            <div className="text-center py-12 text-text-tertiary">
              <Clock className="h-10 w-10 mx-auto mb-3 opacity-30" />
              <p className="font-medium">No timesheet entries found</p>
              <p className="text-sm mt-1">Adjust the date range or staff filter</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-surface-200">
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Staff</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Date</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Clock In</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Clock Out</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Duration</th>
                    <th className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {timesheets.map((t) => (
                    <tr key={t.id} className="border-b border-surface-100 hover:bg-surface-50 transition-colors">
                      <td className="py-3 px-4 font-medium text-text-primary">{t.staffName ?? "—"}</td>
                      <td className="py-3 px-4 text-text-secondary">{formatDate(t.clockInTime)}</td>
                      <td className="py-3 px-4 text-text-primary">{formatTime(t.clockInTime)}</td>
                      <td className="py-3 px-4 text-text-primary">{formatTime(t.clockOutTime)}</td>
                      <td className="py-3 px-4 font-medium text-text-primary">{formatDuration(t.totalMinutes)}</td>
                      <td className="py-3 px-4">
                        {t.clockOutTime ? (
                          <span className="inline-flex items-center gap-1 text-xs font-medium text-green-600 bg-green-50 px-2 py-0.5 rounded-full">
                            <CheckCircle2 className="h-3 w-3" /> Complete
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-xs font-medium text-amber-600 bg-amber-50 px-2 py-0.5 rounded-full">
                            <AlertCircle className="h-3 w-3" /> Active
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
