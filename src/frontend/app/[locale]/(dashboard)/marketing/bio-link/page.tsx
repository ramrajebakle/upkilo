"use client";

import React, { useState, useEffect } from "react";
import { Share2, Link2, ExternalLink, Loader2, Save, RefreshCw, Copy } from "lucide-react";
import { FaInstagram } from "react-icons/fa6";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface BioLink { id: string; slug: string; title: string; description?: string; bookingUrl?: string; isActive: boolean; views: number; clicks: number; }

export default function BioLinkPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [link, setLink] = useState<BioLink | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ slug: "", title: "", description: "" });

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/social-booking/bio-link").catch(() => ({ data: null }));
      const data = r.data?.data ?? r.data ?? null;
      if (data) { setLink(data); setForm({ slug: data.slug ?? "", title: data.title ?? "", description: data.description ?? "" }); }
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    try {
      const r = await (link?.id
        ? apiClient.put(`/api/v1/social-booking/bio-link/${link.id}`, form)
        : apiClient.post("/api/v1/social-booking/bio-link", form));
      toastSuccess("Bio link saved"); setLink(r.data?.data ?? r.data);
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to save"); }
    finally { setSaving(false); }
  };

  const publicUrl = link ? `https://upkilo.app/b/${link.slug}` : "";

  return (
    <div className="max-w-2xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Social Bio Link <Share2 className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Create a link-in-bio page that directs social media followers to book with you.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {link && (
            <div className="grid grid-cols-3 gap-4">
              {[
                { label: "Views", value: link.views ?? 0, color: "text-text-primary" },
                { label: "Clicks", value: link.clicks ?? 0, color: "text-ai-600" },
                { label: "Click Rate", value: `${link.views ? ((link.clicks / link.views) * 100).toFixed(1) : "0"}%`, color: "text-green-600" },
              ].map((s) => (
                <Card key={s.label}><CardContent className="pt-5"><p className="text-xs text-text-secondary">{s.label}</p><p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p></CardContent></Card>
              ))}
            </div>
          )}

          {link && (
            <Card className="border-green-200 bg-green-50/30">
              <CardContent className="pt-4 pb-4 flex items-center gap-3">
                <Link2 className="h-4 w-4 text-green-600 flex-shrink-0" />
                <span className="text-sm font-medium text-green-800 flex-1 truncate">{publicUrl}</span>
                <Button variant="outline" size="sm" leftIcon={<Copy size={12} />} onClick={() => { navigator.clipboard.writeText(publicUrl); toastSuccess("Link copied"); }}>Copy</Button>
                <Button variant="outline" size="sm" leftIcon={<ExternalLink size={12} />} onClick={() => window.open(publicUrl, "_blank")}>Open</Button>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><FaInstagram className="h-4 w-4" /> Bio Link Settings</CardTitle>
              <CardDescription>Customize what people see when they visit your link-in-bio</CardDescription></CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Slug (URL path) *</label>
                <div className="flex items-center gap-0">
                  <span className="px-3 py-2 text-sm bg-surface-100 border border-surface-200 border-r-0 rounded-l-lg text-text-tertiary">upkilo.app/b/</span>
                  <input value={form.slug} onChange={(e) => setForm((p) => ({ ...p, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, "") }))}
                    placeholder="your-business-name"
                    className="flex-1 px-3 py-2 text-sm rounded-r-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Page Title *</label>
                <input value={form.title} onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} placeholder="Book with [Business Name]"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Description</label>
                <textarea value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} rows={3}
                  placeholder="Short bio that appears under your name on the booking page…"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
              </div>
              <div className="flex justify-end">
                <Button variant="primary" leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                  onClick={save} disabled={!form.slug || !form.title || saving}>{saving ? "Saving…" : "Save Bio Link"}</Button>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
