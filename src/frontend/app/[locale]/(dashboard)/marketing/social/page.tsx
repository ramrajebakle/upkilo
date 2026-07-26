"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Share2, Plus, Clock, CheckCircle2, AlertCircle, Loader2, RefreshCw, Instagram, Twitter, Facebook } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface SocialPost {
  id: string;
  content: string;
  platform: "Instagram" | "Twitter" | "Facebook" | "All";
  status: "Draft" | "Scheduled" | "Published" | "Failed";
  scheduledAt?: string;
  publishedAt?: string;
  imageUrl?: string;
}

const PLATFORM_ICON: Record<string, React.ComponentType<{ className?: string }>> = {
  Instagram: Instagram,
  Twitter: Twitter,
  Facebook: Facebook,
  All: Share2,
};

const STATUS_CFG: Record<string, { color: string; bg: string }> = {
  Draft: { color: "text-gray-600", bg: "bg-gray-50" },
  Scheduled: { color: "text-blue-600", bg: "bg-blue-50" },
  Published: { color: "text-green-600", bg: "bg-green-50" },
  Failed: { color: "text-red-600", bg: "bg-red-50" },
};

export default function SocialPostsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [posts, setPosts] = useState<SocialPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [content, setContent] = useState("");
  const [platform, setPlatform] = useState("All");
  const [scheduledAt, setScheduledAt] = useState("");
  const [saving, setSaving] = useState(false);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/socialposts").catch(() => ({ data: [] }));
      const d: SocialPost[] = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setPosts(d);
    } catch { toastError("Failed to load posts"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const handleSchedule = async () => {
    setSaving(true);
    try {
      await apiClient.post("/api/v1/socialposts/schedule-post", { content, platform, scheduledAt: scheduledAt || null });
      toastSuccess(scheduledAt ? "Post scheduled" : "Post created");
      setContent(""); setPlatform("All"); setScheduledAt(""); setShowForm(false); fetch();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Failed to create post"); }
    finally { setSaving(false); }
  };

  const stats = {
    published: posts.filter((p) => p.status === "Published").length,
    scheduled: posts.filter((p) => p.status === "Scheduled").length,
    draft: posts.filter((p) => p.status === "Draft").length,
  };

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Social Posts <Share2 className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Schedule and publish posts across social media platforms.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading} />
          <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>New Post</Button>
        </div>
      </header>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Published", value: stats.published, color: "text-green-500" },
          { label: "Scheduled", value: stats.scheduled, color: "text-blue-500" },
          { label: "Draft", value: stats.draft, color: "text-gray-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardContent className="pt-5">
              <p className="text-xs text-text-secondary font-medium">{s.label}</p>
              <p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {showForm && (
        <Card>
          <CardHeader><CardTitle>Create Post</CardTitle><CardDescription>Compose content and optionally schedule it</CardDescription></CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-primary mb-1">Content *</label>
              <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={4} placeholder="Write your post content here…"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
              <p className="text-xs text-text-tertiary mt-1">{content.length} characters</p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Platform</label>
                <select value={platform} onChange={(e) => setPlatform(e.target.value)}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500">
                  {["All", "Instagram", "Facebook", "Twitter"].map((p) => <option key={p} value={p}>{p}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-text-primary mb-1">Schedule (optional)</label>
                <input type="datetime-local" value={scheduledAt} onChange={(e) => setScheduledAt(e.target.value)}
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowForm(false)} disabled={saving}>Cancel</Button>
              <Button variant="primary" size="sm"
                leftIcon={saving ? <Loader2 size={14} className="animate-spin" /> : <Share2 size={14} />}
                onClick={handleSchedule} disabled={!content.trim() || saving}>
                {saving ? "Saving…" : scheduledAt ? "Schedule Post" : "Save Draft"}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : posts.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Share2 className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">No social posts yet</p>
            <Button variant="primary" className="mt-4" leftIcon={<Plus size={14} />} onClick={() => setShowForm(true)}>Create first post</Button>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {posts.map((p) => {
              const PIcon = PLATFORM_ICON[p.platform] ?? Share2;
              const cfg = STATUS_CFG[p.status] ?? STATUS_CFG.Draft;
              return (
                <Card key={p.id}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-start gap-3">
                      <PIcon className="h-5 w-5 text-text-tertiary mt-0.5 flex-shrink-0" />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-xs font-medium text-text-secondary">{p.platform}</span>
                          <span className={cn("text-xs font-medium px-2 py-0.5 rounded-full", cfg.color, cfg.bg)}>{p.status}</span>
                          {p.scheduledAt && (
                            <span className="text-xs text-text-tertiary flex items-center gap-1">
                              <Clock className="h-3 w-3" />{new Date(p.scheduledAt).toLocaleDateString([], { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })}
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-text-primary line-clamp-3">{p.content}</p>
                      </div>
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
