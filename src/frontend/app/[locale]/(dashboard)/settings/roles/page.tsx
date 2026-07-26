"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  Shield, Plus, Pencil, Trash2, CheckCircle2, XCircle,
  Loader2, RefreshCw, ChevronDown, ChevronUp, Lock,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface CustomRole {
  id: string;
  name: string;
  description?: string;
  isSystem: boolean;
  isActive: boolean;
  permissions: Record<string, boolean>;
  createdAt: string;
}

const PERMISSION_GROUPS: { group: string; permissions: { key: string; label: string }[] }[] = [
  {
    group: "Bookings",
    permissions: [
      { key: "bookings.view", label: "View bookings" },
      { key: "bookings.create", label: "Create bookings" },
      { key: "bookings.edit", label: "Edit bookings" },
      { key: "bookings.delete", label: "Cancel/delete bookings" },
    ],
  },
  {
    group: "Clients",
    permissions: [
      { key: "clients.view", label: "View clients" },
      { key: "clients.create", label: "Create clients" },
      { key: "clients.edit", label: "Edit clients" },
      { key: "clients.delete", label: "Delete clients" },
      { key: "clients.export", label: "Export clients" },
    ],
  },
  {
    group: "Payments",
    permissions: [
      { key: "payments.view", label: "View payments" },
      { key: "payments.refund", label: "Issue refunds" },
      { key: "payments.reports", label: "View financial reports" },
    ],
  },
  {
    group: "Staff",
    permissions: [
      { key: "staff.view", label: "View staff" },
      { key: "staff.manage", label: "Manage staff" },
      { key: "staff.schedule", label: "Edit schedules" },
    ],
  },
  {
    group: "Settings",
    permissions: [
      { key: "settings.view", label: "View settings" },
      { key: "settings.edit", label: "Edit settings" },
      { key: "settings.billing", label: "Manage billing" },
    ],
  },
  {
    group: "Marketing",
    permissions: [
      { key: "marketing.view", label: "View campaigns" },
      { key: "marketing.send", label: "Send campaigns" },
      { key: "marketing.edit", label: "Edit templates" },
    ],
  },
];

function allPermissions(): Record<string, boolean> {
  return Object.fromEntries(
    PERMISSION_GROUPS.flatMap((g) => g.permissions.map((p) => [p.key, false]))
  );
}

interface RoleFormProps {
  initial?: CustomRole | null;
  onSave: (data: { name: string; description: string; permissions: Record<string, boolean> }) => Promise<void>;
  onCancel: () => void;
  saving: boolean;
}

function RoleForm({ initial, onSave, onCancel, saving }: RoleFormProps) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [permissions, setPermissions] = useState<Record<string, boolean>>(
    initial?.permissions ?? allPermissions()
  );
  const [expanded, setExpanded] = useState<string | null>(PERMISSION_GROUPS[0].group);

  const toggle = (key: string) =>
    setPermissions((p) => ({ ...p, [key]: !p[key] }));

  const toggleGroup = (group: (typeof PERMISSION_GROUPS)[0]) => {
    const keys = group.permissions.map((p) => p.key);
    const allOn = keys.every((k) => permissions[k]);
    setPermissions((p) => ({ ...p, ...Object.fromEntries(keys.map((k) => [k, !allOn])) }));
  };

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-text-primary mb-1">Role Name *</label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Receptionist"
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-primary mb-1">Description</label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Brief description of this role"
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
          />
        </div>
      </div>

      <div>
        <p className="text-sm font-medium text-text-primary mb-2">Permissions</p>
        <div className="space-y-2">
          {PERMISSION_GROUPS.map((group) => {
            const keys = group.permissions.map((p) => p.key);
            const enabledCount = keys.filter((k) => permissions[k]).length;
            const isOpen = expanded === group.group;
            return (
              <div key={group.group} className="border border-surface-200 rounded-xl overflow-hidden">
                <button
                  onClick={() => setExpanded(isOpen ? null : group.group)}
                  className="w-full flex items-center justify-between px-4 py-3 bg-surface-50 hover:bg-surface-100 transition-colors text-sm"
                >
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={enabledCount === keys.length}
                      ref={(el) => { if (el) el.indeterminate = enabledCount > 0 && enabledCount < keys.length; }}
                      onChange={() => toggleGroup(group)}
                      onClick={(e) => e.stopPropagation()}
                      className="accent-ai-500 h-4 w-4"
                    />
                    <span className="font-medium text-text-primary">{group.group}</span>
                    <span className="text-xs text-text-tertiary">({enabledCount}/{keys.length})</span>
                  </div>
                  {isOpen ? <ChevronUp className="h-4 w-4 text-text-tertiary" /> : <ChevronDown className="h-4 w-4 text-text-tertiary" />}
                </button>
                {isOpen && (
                  <div className="p-4 grid grid-cols-1 sm:grid-cols-2 gap-2">
                    {group.permissions.map((p) => (
                      <label key={p.key} className="flex items-center gap-2 cursor-pointer text-sm">
                        <input
                          type="checkbox"
                          checked={!!permissions[p.key]}
                          onChange={() => toggle(p.key)}
                          className="accent-ai-500 h-4 w-4 rounded"
                        />
                        <span className="text-text-primary">{p.label}</span>
                      </label>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button variant="outline" onClick={onCancel} disabled={saving}>Cancel</Button>
        <Button
          variant="primary"
          leftIcon={saving ? <Loader2 size={15} className="animate-spin" /> : undefined}
          onClick={() => onSave({ name, description, permissions })}
          disabled={!name.trim() || saving}
        >
          {saving ? "Saving…" : initial ? "Update Role" : "Create Role"}
        </Button>
      </div>
    </div>
  );
}

export default function CustomRolesPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [roles, setRoles] = useState<CustomRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingRole, setEditingRole] = useState<CustomRole | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetchRoles = useCallback(async () => {
    setLoading(true);
    try {
      const res = await apiClient.get("/api/v1/roles");
      const data = res.data?.data ?? res.data ?? [];
      setRoles(Array.isArray(data) ? data : []);
    } catch {
      toastError("Failed to load roles");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchRoles(); }, [fetchRoles]);

  const handleSave = async (data: { name: string; description: string; permissions: Record<string, boolean> }) => {
    setSaving(true);
    try {
      if (editingRole) {
        await apiClient.put(`/api/v1/roles/${editingRole.id}`, data);
        toastSuccess("Role updated");
      } else {
        await apiClient.post("/api/v1/roles", data);
        toastSuccess("Role created");
      }
      setShowForm(false);
      setEditingRole(null);
      fetchRoles();
    } catch (err: any) {
      toastError(err?.response?.data?.error ?? "Failed to save role");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    try {
      await apiClient.delete(`/api/v1/roles/${id}`);
      toastSuccess("Role deleted");
      setRoles((r) => r.filter((role) => role.id !== id));
    } catch {
      toastError("Failed to delete role");
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
            Custom Roles
            <Shield className="text-text-tertiary" size={22} />
          </h1>
          <p className="text-text-secondary mt-1">
            Create fine-grained access control roles for your team.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={15} />} onClick={fetchRoles} disabled={loading} />
          <Button
            variant="primary"
            leftIcon={<Plus size={15} />}
            onClick={() => { setEditingRole(null); setShowForm(true); }}
          >
            New Role
          </Button>
        </div>
      </header>

      {/* Create / Edit form */}
      {(showForm || editingRole) && (
        <Card>
          <CardHeader>
            <CardTitle>{editingRole ? `Edit Role: ${editingRole.name}` : "New Custom Role"}</CardTitle>
            <CardDescription>Set a name and choose which permissions this role grants.</CardDescription>
          </CardHeader>
          <CardContent>
            <RoleForm
              initial={editingRole}
              onSave={handleSave}
              onCancel={() => { setShowForm(false); setEditingRole(null); }}
              saving={saving}
            />
          </CardContent>
        </Card>
      )}

      {/* Roles list */}
      {loading ? (
        <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
      ) : roles.length === 0 && !showForm ? (
        <Card>
          <CardContent className="text-center py-16 text-text-tertiary">
            <Shield className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No custom roles yet</p>
            <p className="text-sm mt-1">Create a role to control what your staff can access.</p>
            <Button variant="primary" className="mt-4" leftIcon={<Plus size={15} />} onClick={() => setShowForm(true)}>
              Create first role
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {roles.map((role) => {
            const enabledCount = Object.values(role.permissions).filter(Boolean).length;
            const totalCount = Object.values(role.permissions).length;
            return (
              <Card key={role.id}>
                <CardContent className="pt-5">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-3 flex-1 min-w-0">
                      <div className={cn(
                        "w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0",
                        role.isSystem ? "bg-surface-100" : "bg-ai-50"
                      )}>
                        {role.isSystem
                          ? <Lock className="h-4 w-4 text-text-tertiary" />
                          : <Shield className="h-4 w-4 text-ai-500" />}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="font-semibold text-text-primary">{role.name}</p>
                          {role.isSystem && (
                            <span className="text-xs bg-surface-100 text-text-tertiary px-2 py-0.5 rounded-full">System</span>
                          )}
                          <span className={cn(
                            "text-xs px-2 py-0.5 rounded-full font-medium",
                            role.isActive ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"
                          )}>
                            {role.isActive ? "Active" : "Inactive"}
                          </span>
                        </div>
                        {role.description && (
                          <p className="text-sm text-text-secondary mt-0.5">{role.description}</p>
                        )}
                        <p className="text-xs text-text-tertiary mt-1">
                          {enabledCount} of {totalCount} permissions enabled
                        </p>
                        <div className="flex flex-wrap gap-1 mt-2">
                          {PERMISSION_GROUPS.map((g) => {
                            const count = g.permissions.filter((p) => role.permissions[p.key]).length;
                            if (count === 0) return null;
                            return (
                              <span key={g.group} className="text-xs bg-surface-100 text-text-secondary px-2 py-0.5 rounded-full">
                                {g.group} ({count})
                              </span>
                            );
                          })}
                        </div>
                      </div>
                    </div>
                    {!role.isSystem && (
                      <div className="flex gap-2 flex-shrink-0">
                        <Button
                          variant="outline"
                          size="sm"
                          leftIcon={<Pencil size={13} />}
                          onClick={() => { setEditingRole(role); setShowForm(false); }}
                        >
                          Edit
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-red-500 border-red-200 hover:bg-red-50"
                          leftIcon={deletingId === role.id ? <Loader2 size={13} className="animate-spin" /> : <Trash2 size={13} />}
                          onClick={() => handleDelete(role.id)}
                          disabled={deletingId === role.id}
                        >
                          Delete
                        </Button>
                      </div>
                    )}
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
