"use client";

import React, { useState, useEffect } from "react";
import {
  Users,
  Search,
  Clock,
  UserCheck,
  UserX,
  Calendar,
  ChevronRight,
  Mail,
  Phone,
  Star,
  Coffee,
  Loader2,
  Shield,
  Sparkles,
  TrendingUp,
  Activity,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import api from "@/lib/api";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { Link } from "@/navigation";

interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  role: string;
  color?: string;
  isActive: boolean;
  avatar?: string;
  specializations?: string[];
  isOnShift?: boolean;
  currentShift?: {
    start: string;
    end: string;
  };
}

type ShiftFilter = "all" | "on-shift" | "off-shift";

export default function TenantTeamPage() {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [shiftFilter, setShiftFilter] = useState<ShiftFilter>("all");

  useEffect(() => {
    fetchStaff();
  }, []);

  const fetchStaff = async () => {
    try {
      setLoading(true);
      const res = await api.staff.list();
      const rawData = res.data?.data || res.data || [];
      const data = (Array.isArray(rawData) ? rawData : []).map((s: any) => ({
        id: s.id,
        firstName: s.firstName || '',
        lastName: s.lastName || '',
        email: s.email || '',
        phone: s.phone || '',
        role: s.role || 'Staff',
        color: s.color || '',
        isActive: s.isActive !== undefined ? s.isActive : (s.employmentStatus ? s.employmentStatus === 'Active' : true),
        specializations: s.specialties || s.specializations || [],
        isOnShift: s.isOnShift ?? s.clockedIn ?? (s.isActive === true),
        currentShift: s.currentShift || undefined,
      }));
      setStaff(data);
    } catch (err) {
      console.error("Failed to fetch team:", err);
      toast.error("Failed to load team members");
      setStaff([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredStaff = staff.filter((s) => {
    const name = `${s.firstName} ${s.lastName}`.toLowerCase();
    const matchesSearch =
      !searchQuery ||
      name.includes(searchQuery.toLowerCase()) ||
      s.email?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      s.role?.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesShift =
      shiftFilter === "all" ||
      (shiftFilter === "on-shift" && s.isOnShift) ||
      (shiftFilter === "off-shift" && !s.isOnShift);
    return matchesSearch && matchesShift;
  });

  const totalStaff = staff.length;
  const onShiftCount = staff.filter((s) => s.isOnShift).length;
  const offShiftCount = totalStaff - onShiftCount;

  const getInitials = (first: string, last: string) => {
    return `${(first || "?")[0]}${(last || "?")[0]}`.toUpperCase();
  };

  const getRoleBadgeColor = (role: string) => {
    const r = (role || "").toLowerCase();
    if (r.includes("admin") || r.includes("owner") || r.includes("manager"))
      return "bg-primary-100 text-primary-700";
    if (r.includes("senior") || r.includes("lead"))
      return "bg-amber-100 text-amber-700";
    if (r.includes("therapist") || r.includes("stylist") || r.includes("specialist"))
      return "bg-violet-100 text-violet-700";
    return "bg-neutral-100 text-neutral-600";
  };

  const getShiftStatus = (member: StaffMember) => {
    if (member.isOnShift) {
      return {
        label: "On Shift",
        dotClass: "bg-success-500",
        textClass: "text-success-700",
        bgClass: "bg-success-50",
      };
    }
    return {
      label: "Off Shift",
      dotClass: "bg-neutral-400",
      textClass: "text-neutral-500",
      bgClass: "bg-neutral-50",
    };
  };

  const formatShiftTime = (time: string) => {
    return new Date(time).toLocaleTimeString("en-US", {
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
    });
  };

  const currentDay = new Date().toLocaleDateString("en-US", {
    weekday: "long",
    month: "long",
    day: "numeric",
  });

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <header className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <p className="text-text-tertiary text-sm font-medium tracking-wide uppercase mb-1">
            {currentDay}
          </p>
          <h1 className="text-3xl font-bold text-text-primary">Team Overview</h1>
        </div>
        <Link href="/staff">
          <Button variant="primary" leftIcon={<Users size={16} />}>
            Manage Staff
          </Button>
        </Link>
      </header>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card className="bg-surface-0 border-t-[4px] border-t-primary-500">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">Total Staff</span>
              <Users size={18} className="text-primary-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">{totalStaff}</span>
          </CardContent>
        </Card>

        <Card className="bg-surface-0 border-t-[4px] border-t-success-500">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">On Shift</span>
              <UserCheck size={18} className="text-success-500" />
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-bold text-text-primary">{onShiftCount}</span>
              <span className="text-sm text-text-tertiary">/ {totalStaff}</span>
            </div>
          </CardContent>
        </Card>

        <Card className="bg-ai-50 border-ai-200">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">Off Shift</span>
              <Coffee size={18} className="text-ai-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">{offShiftCount}</span>
          </CardContent>
        </Card>
      </div>

      {/* Search & Filter */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary" />
          <input
            type="text"
            placeholder="Search by name, email, or role..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-lg border border-neutral-200 bg-surface-0 text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-primary-500/30 focus:border-primary-500 transition-all text-sm"
          />
        </div>
        <div className="flex gap-2">
          {([
            { key: "all" as const, label: "All" },
            { key: "on-shift" as const, label: "On Shift" },
            { key: "off-shift" as const, label: "Off Shift" },
          ]).map(({ key, label }) => (
            <button
              key={key}
              onClick={() => setShiftFilter(key)}
              className={cn(
                "px-4 py-2 rounded-lg text-sm font-medium transition-all",
                shiftFilter === key
                  ? "bg-primary-500 text-white shadow-sm"
                  : "bg-surface-100 text-text-secondary hover:bg-surface-200"
              )}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {/* Team List */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className="bg-surface-0 rounded-xl border border-neutral-200 p-5 animate-pulse"
            >
              <div className="flex items-start gap-4">
                <div className="w-12 h-12 rounded-full bg-neutral-200" />
                <div className="flex-1 space-y-2">
                  <div className="h-4 bg-neutral-200 rounded w-2/3" />
                  <div className="h-3 bg-neutral-200 rounded w-1/2" />
                  <div className="h-3 bg-neutral-200 rounded w-1/3" />
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : filteredStaff.length === 0 ? (
        <Card className="bg-surface-0">
          <CardContent className="p-12 text-center">
            <div className="w-16 h-16 rounded-full bg-neutral-100 flex items-center justify-center mx-auto mb-4">
              <Users size={28} className="text-neutral-400" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-1">
              {searchQuery ? "No team members found" : "No team members yet"}
            </h3>
            <p className="text-text-secondary text-sm mb-4">
              {searchQuery
                ? "Try adjusting your search or filters"
                : "Start building your team by adding staff members"}
            </p>
            {!searchQuery && (
              <Link href="/staff">
                <Button variant="primary" leftIcon={<Users size={16} />}>
                  Add Team Member
                </Button>
              </Link>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filteredStaff.map((member) => {
            const shift = getShiftStatus(member);

            return (
              <Link
                key={member.id}
                href={`/staff/${member.id}`}
                className="block"
              >
                <div className="group bg-surface-0 rounded-xl border border-neutral-200 p-5 hover:border-primary-300 hover:shadow-md transition-all cursor-pointer">
                  <div className="flex items-start gap-4">
                    {/* Avatar */}
                    <div className="relative shrink-0">
                      <div className="w-12 h-12 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold text-sm">
                        {getInitials(member.firstName, member.lastName)}
                      </div>
                      {/* Shift indicator dot */}
                      <div
                        className={cn(
                          "absolute -bottom-0.5 -right-0.5 w-3.5 h-3.5 rounded-full border-2 border-white",
                          shift.dotClass
                        )}
                      />
                    </div>

                    {/* Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="font-semibold text-text-primary truncate">
                          {member.firstName} {member.lastName}
                        </span>
                        <span
                          className={cn(
                            "text-xs font-medium px-2 py-0.5 rounded-full capitalize shrink-0",
                            getRoleBadgeColor(member.role)
                          )}
                        >
                          {member.role || "Staff"}
                        </span>
                      </div>

                      {/* Contact */}
                      <div className="flex items-center gap-3 text-sm text-text-secondary mb-2">
                        {member.email && (
                          <span className="flex items-center gap-1 truncate">
                            <Mail size={12} />
                            <span className="truncate">{member.email}</span>
                          </span>
                        )}
                        {member.phone && (
                          <span className="flex items-center gap-1 shrink-0">
                            <Phone size={12} />
                            {member.phone}
                          </span>
                        )}
                      </div>

                      {/* Shift & Schedule Info */}
                      <div className="flex items-center gap-3 flex-wrap">
                        <span
                          className={cn(
                            "inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full",
                            shift.bgClass,
                            shift.textClass
                          )}
                        >
                          <span
                            className={cn(
                              "w-1.5 h-1.5 rounded-full",
                              shift.dotClass
                            )}
                          />
                          {shift.label}
                        </span>

                        {member.currentShift && (
                          <span className="text-xs text-text-tertiary flex items-center gap-1">
                            <Clock size={12} />
                            {formatShiftTime(member.currentShift.start)} –{" "}
                            {formatShiftTime(member.currentShift.end)}
                          </span>
                        )}

                        {member.specializations && member.specializations.length > 0 && (
                          <span className="text-xs text-text-tertiary flex items-center gap-1">
                            <Star size={12} />
                            {member.specializations.slice(0, 2).join(", ")}
                          </span>
                        )}
                      </div>
                    </div>

                    <ChevronRight
                      size={18}
                      className="text-neutral-300 group-hover:text-primary-500 transition-colors mt-2 shrink-0"
                    />
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}

      {/* Footer Count */}
      {!loading && filteredStaff.length > 0 && (
        <div className="text-center text-sm text-text-tertiary pt-2">
          Showing {filteredStaff.length} of {totalStaff} team members
        </div>
      )}
    </div>
  );
}
