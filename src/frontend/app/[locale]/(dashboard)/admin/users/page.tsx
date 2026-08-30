"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Users, Search, UserCheck, UserX, Loader2, RefreshCw, Activity } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface User {
  id: string;
  email: string;
  fullName?: string;
  role: string;
  tenantName?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export default function AdminUsersPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [actioning, setActioning] = useState<string | null>(null);
  const [roleFilter, setRoleFilter] = useState("All");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/users").catch(() => ({ data: [] }));
      setUsers(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const setStatus = async (id: string, action: "activate" | "deactivate") => {
    setActioning(id);
    try {
      await apiClient.post(`/api/v1/users/${id}/${action}`);
      toastSuccess(`User ${action}d`);
      setUsers((prev) => prev.map((u) => u.id === id ? { ...u, isActive: action === "activate" } : u));
    } catch { toastError(`Failed to ${action} user`); }
    finally { setActioning(null); }
  };

  const roles = ["All", ...Array.from(new Set(users.map((u) => u.role)))];
  const filtered = users.filter((u) => {
    const matchSearch = !search || u.email.toLowerCase().includes(search.toLowerCase()) || u.fullName?.toLowerCase().includes(search.toLowerCase());
    const matchRole = roleFilter === "All" || u.role === roleFilter;
    return matchSearch && matchRole;
  });

  const stats = {
    total: users.length,
    active: users.filter((u) => u.isActive).length,
    inactive: users.filter((u) => !u.isActive).length,
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">User Management <Users className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">View, activate, and deactivate platform users.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Users", value: stats.total, color: "text-text-primary" },
          { label: "Active", value: stats.active, color: "text-success-fg" },
          { label: "Inactive", value: stats.inactive, color: "text-foreground-muted" },
        ].map((s) => (
          <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search users…"
            className="pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 w-64" />
        </div>
        <div className="flex gap-1 p-1 bg-surface-100 rounded-lg">
          {roles.map((r) => (
            <button key={r} onClick={() => setRoleFilter(r)}
              className={cn("px-3 py-1 text-xs font-medium rounded-md transition-colors",
                roleFilter === r ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary")}>
              {r}
            </button>
          ))}
        </div>
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : (
          <Card>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["User", "Role", "Tenant", "Status", "Last Login", "Actions"].map((h) => (
                    <th key={h} className="text-left py-3 px-4 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {filtered.map((u) => (
                    <tr key={u.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-4">
                        <p className="font-medium text-text-primary">{u.fullName ?? u.email}</p>
                        <p className="text-xs text-text-tertiary">{u.fullName ? u.email : ""}</p>
                      </td>
                      <td className="py-3 px-4 text-xs text-text-secondary">{u.role}</td>
                      <td className="py-3 px-4 text-xs text-text-secondary">{u.tenantName ?? "—"}</td>
                      <td className="py-3 px-4">
                        <span className={cn("text-xs font-medium px-2 py-0.5 rounded-full",
                          u.isActive ? "text-green-600 bg-green-50" : "text-foreground-secondary bg-muted")}>
                          {u.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-xs text-text-tertiary">{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : "Never"}</td>
                      <td className="py-3 px-4">
                        <Button variant="outline" size="sm"
                          leftIcon={actioning === u.id ? <Loader2 size={12} className="animate-spin" /> : u.isActive ? <UserX size={12} className="text-danger-fg" /> : <UserCheck size={12} className="text-success-fg" />}
                          onClick={() => setStatus(u.id, u.isActive ? "deactivate" : "activate")} disabled={!!actioning}>
                          {u.isActive ? "Deactivate" : "Activate"}
                        </Button>
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr><td colSpan={6} className="text-center py-10 text-text-tertiary">No users found</td></tr>
                  )}
                </tbody>
              </table>
            </CardContent>
          </Card>
        )}
    </div>
  );
}
