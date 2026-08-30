"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Truck, Plus, Pencil, Trash2, Loader2, RefreshCw, Search, Mail, Phone, Globe, Package } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Supplier {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  website?: string;
  contactPerson?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

function SupplierForm({ initial, onSave, onCancel, saving }: {
  initial?: Supplier | null;
  onSave: (d: Omit<Supplier, "id">) => Promise<void>;
  onCancel: () => void;
  saving: boolean;
}) {
  const [form, setForm] = useState({
    name: initial?.name ?? "",
    email: initial?.email ?? "",
    phone: initial?.phone ?? "",
    website: initial?.website ?? "",
    contactPerson: initial?.contactPerson ?? "",
    address: initial?.address ?? "",
    notes: initial?.notes ?? "",
    isActive: initial?.isActive ?? true,
  });
  const set = (k: string, v: string | boolean) => setForm((f) => ({ ...f, [k]: v }));

  return (
    <div className="p-5 bg-surface-50 rounded-xl border border-surface-200 space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {[
          { key: "name", label: "Supplier Name *", placeholder: "e.g. Dermalogica India" },
          { key: "contactPerson", label: "Contact Person", placeholder: "e.g. Priya Sharma" },
          { key: "email", label: "Email", placeholder: "supplier@example.com", type: "email" },
          { key: "phone", label: "Phone", placeholder: "+91 98765 43210" },
          { key: "website", label: "Website", placeholder: "https://supplier.com" },
          { key: "address", label: "Address", placeholder: "Street, City" },
        ].map((f) => (
          <div key={f.key}>
            <label className="block text-xs font-medium text-text-secondary mb-1">{f.label}</label>
            <input type={f.type ?? "text"} value={(form as any)[f.key]} onChange={(e) => set(f.key, e.target.value)} placeholder={f.placeholder}
              className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
          </div>
        ))}
        <div className="sm:col-span-2">
          <label className="block text-xs font-medium text-text-secondary mb-1">Notes</label>
          <textarea value={form.notes} onChange={(e) => set("notes", e.target.value)} rows={2} placeholder="Payment terms, lead times, etc."
            className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
        </div>
      </div>
      <div className="flex justify-end gap-2">
        <Button variant="outline" size="sm" onClick={onCancel} disabled={saving}>Cancel</Button>
        <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : undefined}
          onClick={() => onSave({ ...form })} disabled={!form.name.trim() || saving}>
          {saving ? "Saving…" : initial ? "Update" : "Create"}
        </Button>
      </div>
    </div>
  );
}

export default function SuppliersPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/suppliers");
      const d = r.data?.data ?? r.data ?? [];
      setSuppliers(Array.isArray(d) ? d : []);
    } catch { toastError("Failed to load suppliers"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const handleSave = async (data: Omit<Supplier, "id">) => {
    setSaving(true);
    try {
      if (editing) {
        await apiClient.put(`/api/v1/suppliers/${editing.id}`, data); toastSuccess("Supplier updated");
      } else {
        await apiClient.post("/api/v1/suppliers", data); toastSuccess("Supplier created");
      }
      setShowForm(false); setEditing(null); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    try {
      await apiClient.delete(`/api/v1/suppliers/${id}`); toastSuccess("Deleted");
      setSuppliers((s) => s.filter((x) => x.id !== id));
    } catch { toastError("Failed to delete"); }
    finally { setDeletingId(null); }
  };

  const filtered = suppliers.filter((s) =>
    !search || s.name.toLowerCase().includes(search.toLowerCase()) ||
    s.contactPerson?.toLowerCase().includes(search.toLowerCase()) ||
    s.email?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Suppliers <Truck className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Manage your product and inventory suppliers.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => { setEditing(null); setShowForm(true); }}>Add Supplier</Button>
        </div>
      </header>

      {(showForm || editing) && (
        <SupplierForm initial={editing} onSave={handleSave} onCancel={() => { setShowForm(false); setEditing(null); }} saving={saving} />
      )}

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input type="text" placeholder="Search suppliers…" value={search} onChange={(e) => setSearch(e.target.value)}
          className="w-full sm:w-80 pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-16 text-text-tertiary">
            <Truck className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">{search ? "No suppliers match your search" : "No suppliers yet"}</p>
            {!search && <Button variant="primary" className="mt-4" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Add first supplier</Button>}
          </CardContent></Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {filtered.map((s) => (
              <Card key={s.id}>
                <CardContent className="pt-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3 flex-1 min-w-0">
                      <div className="w-10 h-10 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                        <Truck className="h-5 w-5 text-text-tertiary" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="font-semibold text-text-primary truncate">{s.name}</p>
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${s.isActive ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"}`}>
                            {s.isActive ? "Active" : "Inactive"}
                          </span>
                        </div>
                        {s.contactPerson && <p className="text-sm text-text-secondary">{s.contactPerson}</p>}
                        <div className="flex flex-wrap gap-3 mt-1.5 text-xs text-text-tertiary">
                          {s.email && <span className="flex items-center gap-1"><Mail className="h-3 w-3" />{s.email}</span>}
                          {s.phone && <span className="flex items-center gap-1"><Phone className="h-3 w-3" />{s.phone}</span>}
                          {s.website && <a href={s.website} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1 text-ai hover:underline"><Globe className="h-3 w-3" />Website</a>}
                        </div>
                        {s.notes && <p className="text-xs text-text-tertiary mt-1.5 line-clamp-1">{s.notes}</p>}
                      </div>
                    </div>
                    <div className="flex gap-1 flex-shrink-0">
                      <Button variant="outline" size="sm" leftIcon={<Pencil size={12} />} onClick={() => setEditing(s)}>Edit</Button>
                      <Button variant="outline" size="sm" className="text-danger-fg border-red-200 hover:bg-red-50"
                        leftIcon={deletingId === s.id ? <Loader2 size={12} className="animate-spin" /> : <Trash2 size={12} />}
                        onClick={() => handleDelete(s.id)} disabled={deletingId === s.id} />
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
