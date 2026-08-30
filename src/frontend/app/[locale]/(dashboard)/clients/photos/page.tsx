"use client";

import React, { useState, useEffect, useCallback, useRef } from "react";
import { Camera, Upload, Search, Trash2, Star, Loader2, Image as ImageIcon, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface Client { id: string; fullName: string; }
interface ClientPhoto {
  id: string;
  type: string;
  fileUrl: string;
  caption?: string;
  fileName: string;
  fileSizeBytes: number;
  isPublic: boolean;
  createdAt: string;
}

const PHOTO_TYPES = ["Profile", "Before", "After", "Progress", "Other"];

export default function ClientPhotosPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [clients, setClients] = useState<Client[]>([]);
  const [selectedClient, setSelectedClient] = useState<Client | null>(null);
  const [clientSearch, setClientSearch] = useState("");
  const [photos, setPhotos] = useState<ClientPhoto[]>([]);
  const [loading, setLoading] = useState(false);
  const [typeFilter, setTypeFilter] = useState<string>("All");
  const [uploading, setUploading] = useState(false);
  const [uploadType, setUploadType] = useState("Other");
  const [caption, setCaption] = useState("");
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiClient.get("/api/v1/clients?limit=100").catch(() => ({ data: [] })).then((r) => {
      setClients(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    });
  }, []);

  const filteredClients = clients.filter((c) =>
    c.fullName.toLowerCase().includes(clientSearch.toLowerCase())
  );

  const loadPhotos = useCallback(async (clientId: string) => {
    setLoading(true);
    try {
      const url = typeFilter === "All"
        ? `/api/clients/${clientId}/photos`
        : `/api/clients/${clientId}/photos?type=${typeFilter}`;
      const r = await apiClient.get(url).catch(() => ({ data: [] }));
      setPhotos(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  }, [typeFilter]);

  useEffect(() => {
    if (selectedClient) loadPhotos(selectedClient.id);
  }, [selectedClient, loadPhotos]);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !selectedClient) return;
    setUploading(true);
    try {
      const form = new FormData();
      form.append("file", file);
      form.append("type", uploadType);
      if (caption) form.append("caption", caption);
      await apiClient.post(`/api/clients/${selectedClient.id}/photos`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      toastSuccess("Photo uploaded");
      setCaption(""); loadPhotos(selectedClient.id);
    } catch { toastError("Upload failed"); }
    finally { setUploading(false); if (fileRef.current) fileRef.current.value = ""; }
  };

  const handleDelete = async (photoId: string) => {
    if (!selectedClient) return;
    try {
      await apiClient.delete(`/api/clients/${selectedClient.id}/photos/${photoId}`);
      toastSuccess("Photo deleted");
      setPhotos((p) => p.filter((ph) => ph.id !== photoId));
    } catch { toastError("Delete failed"); }
  };

  const setAsProfile = async (photoId: string) => {
    if (!selectedClient) return;
    try {
      await apiClient.put(`/api/clients/${selectedClient.id}/photos/${photoId}/set-profile`);
      toastSuccess("Profile photo updated");
    } catch { toastError("Failed to set profile photo"); }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Client Photos <Camera className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Before/after photos and progress documentation for clients.</p>
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Client list sidebar */}
        <Card className="lg:col-span-1">
          <CardHeader><CardTitle className="text-sm">Select Client</CardTitle></CardHeader>
          <CardContent className="p-0">
            <div className="px-4 pb-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-text-tertiary" />
                <input value={clientSearch} onChange={(e) => setClientSearch(e.target.value)} placeholder="Search clients…"
                  className="w-full pl-8 pr-3 py-1.5 text-xs rounded-md border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500" />
              </div>
            </div>
            <div className="max-h-80 overflow-y-auto">
              {filteredClients.map((c) => (
                <button key={c.id} onClick={() => { setSelectedClient(c); setClientSearch(""); }}
                  className={cn("w-full text-left px-4 py-2.5 text-sm transition-colors hover:bg-surface-50",
                    selectedClient?.id === c.id ? "bg-ai-subtle text-ai font-medium" : "text-text-primary")}>
                  {c.fullName}
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Photos area */}
        <div className="lg:col-span-3 space-y-4">
          {selectedClient ? (
            <>
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <p className="font-semibold text-text-primary">{selectedClient.fullName}</p>
                  <span className="text-text-tertiary text-sm">· {photos.length} photo{photos.length !== 1 ? "s" : ""}</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="flex gap-1">
                    {["All", ...PHOTO_TYPES].map((t) => (
                      <button key={t} onClick={() => setTypeFilter(t)}
                        className={cn("px-2.5 py-1 text-xs rounded-full font-medium transition-colors",
                          typeFilter === t ? "bg-ai-500 text-white" : "bg-surface-100 text-text-secondary hover:bg-surface-200")}>
                        {t}
                      </button>
                    ))}
                  </div>
                  <Button variant="outline" size="sm" leftIcon={<RefreshCw size={12} />} onClick={() => loadPhotos(selectedClient.id)} disabled={loading} />
                </div>
              </div>

              {/* Upload panel */}
              <div className="flex flex-wrap items-center gap-3 p-3 bg-surface-50 rounded-xl border border-surface-200">
                <select value={uploadType} onChange={(e) => setUploadType(e.target.value)}
                  className="px-2.5 py-1.5 text-xs rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500">
                  {PHOTO_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
                <input value={caption} onChange={(e) => setCaption(e.target.value)} placeholder="Caption (optional)"
                  className="flex-1 min-w-32 px-2.5 py-1.5 text-xs rounded-lg border border-surface-200 bg-card text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500" />
                <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleUpload} />
                <Button variant="primary" size="sm" leftIcon={uploading ? <Loader2 size={12} className="animate-spin" /> : <Upload size={12} />}
                  onClick={() => fileRef.current?.click()} disabled={uploading}>
                  {uploading ? "Uploading…" : "Upload Photo"}
                </Button>
              </div>

              {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
                : photos.length === 0 ? (
                  <Card><CardContent className="text-center py-12 text-text-tertiary">
                    <ImageIcon className="h-10 w-10 mx-auto mb-3 opacity-20" />
                    <p className="font-medium">No photos yet for this client</p>
                  </CardContent></Card>
                ) : (
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                    {photos.map((ph) => (
                      <div key={ph.id} className="relative group rounded-xl overflow-hidden border border-surface-200 bg-surface-50">
                        <img src={ph.fileUrl} alt={ph.caption ?? ph.fileName} className="w-full h-40 object-cover" />
                        <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-2 gap-1.5">
                          <button onClick={() => setAsProfile(ph.id)}
                            className="flex items-center gap-1 text-xs bg-white/90 text-foreground px-2 py-1 rounded-lg hover:bg-card">
                            <Star size={11} />Profile
                          </button>
                          <button onClick={() => handleDelete(ph.id)}
                            className="flex items-center gap-1 text-xs bg-red-500/90 text-white px-2 py-1 rounded-lg hover:bg-red-600">
                            <Trash2 size={11} />
                          </button>
                        </div>
                        <div className="px-2 py-1.5">
                          <span className="text-xs text-ai font-medium">{ph.type}</span>
                          {ph.caption && <p className="text-xs text-text-secondary truncate">{ph.caption}</p>}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
            </>
          ) : (
            <Card><CardContent className="text-center py-16 text-text-tertiary">
              <Camera className="h-12 w-12 mx-auto mb-3 opacity-20" />
              <p className="font-medium">Select a client to view their photos</p>
            </CardContent></Card>
          )}
        </div>
      </div>
    </div>
  );
}
