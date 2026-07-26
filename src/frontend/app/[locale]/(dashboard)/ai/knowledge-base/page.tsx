"use client";

import React, { useState, useEffect } from "react";
import { BookOpen, Plus, Trash2, Edit2, Save, X, Loader2, Search } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface KbEntry { id: string; question: string; answer: string; category?: string; tags?: string[]; createdAt: string; }

export default function KnowledgeBasePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [entries, setEntries] = useState<KbEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ question: "", answer: "", category: "", tags: "" });
  const [editForm, setEditForm] = useState({ question: "", answer: "", category: "", tags: "" });

  const load = async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/knowledge-base/entries").catch(() => ({ data: [] }));
      setEntries(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const create = async () => {
    if (!form.question.trim() || !form.answer.trim()) return;
    setSaving(true);
    try {
      await apiClient.post("/api/v1/knowledge-base/entries", {
        ...form, tags: form.tags ? form.tags.split(",").map((t) => t.trim()) : [],
      });
      toastSuccess("Entry added"); setShowNew(false); setForm({ question: "", answer: "", category: "", tags: "" }); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Save failed"); }
    finally { setSaving(false); }
  };

  const update = async (id: string) => {
    setSaving(true);
    try {
      await apiClient.put(`/api/v1/knowledge-base/entries/${id}`, {
        ...editForm, tags: editForm.tags ? editForm.tags.split(",").map((t) => t.trim()) : [],
      });
      toastSuccess("Entry updated"); setEditingId(null); load();
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Update failed"); }
    finally { setSaving(false); }
  };

  const remove = async (id: string) => {
    try {
      await apiClient.delete(`/api/v1/knowledge-base/entries/${id}`);
      toastSuccess("Entry deleted"); setEntries((e) => e.filter((x) => x.id !== id));
    } catch { toastError("Delete failed"); }
  };

  const startEdit = (entry: KbEntry) => {
    setEditingId(entry.id);
    setEditForm({ question: entry.question, answer: entry.answer, category: entry.category ?? "", tags: (entry.tags ?? []).join(", ") });
  };

  const filtered = entries.filter((e) =>
    !search || e.question.toLowerCase().includes(search.toLowerCase()) || e.answer.toLowerCase().includes(search.toLowerCase()) || (e.category ?? "").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Knowledge Base <BookOpen className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Train your AI chatbot with Q&A pairs, FAQs, and business information.</p>
      </header>

      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search entries…"
            className="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
        </div>
        <Button variant="primary" leftIcon={<Plus size={14} />} onClick={() => setShowNew(true)}>Add Entry</Button>
      </div>

      {showNew && (
        <Card>
          <CardHeader><CardTitle>New Knowledge Entry</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-text-primary mb-1">Question / Topic *</label>
              <input value={form.question} onChange={(e) => setForm((p) => ({ ...p, question: e.target.value }))}
                placeholder="e.g. What are your cancellation policies?"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-text-primary mb-1">Answer *</label>
              <textarea value={form.answer} onChange={(e) => setForm((p) => ({ ...p, answer: e.target.value }))} rows={4}
                placeholder="Provide a detailed, accurate answer the AI should give…"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Category</label>
                <input value={form.category} onChange={(e) => setForm((p) => ({ ...p, category: e.target.value }))} placeholder="e.g. Policies, Services"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-primary mb-1">Tags (comma-separated)</label>
                <input value={form.tags} onChange={(e) => setForm((p) => ({ ...p, tags: e.target.value }))} placeholder="cancellation, refund"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" leftIcon={<X size={12} />} onClick={() => setShowNew(false)}>Cancel</Button>
              <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={12} className="animate-spin" /> : <Save size={12} />}
                onClick={create} disabled={!form.question.trim() || !form.answer.trim() || saving}>{saving ? "Saving…" : "Save Entry"}</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? <div className="flex justify-center py-6"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12">
            <BookOpen className="h-10 w-10 mx-auto mb-3 text-text-tertiary opacity-25" />
            <p className="text-sm text-text-tertiary">{search ? "No entries match your search" : "No knowledge base entries yet. Add your first Q&A pair."}</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            <p className="text-xs text-text-tertiary">{filtered.length} {filtered.length === 1 ? "entry" : "entries"}</p>
            {filtered.map((entry) => (
              <Card key={entry.id}>
                <CardContent className="pt-4 pb-4">
                  {editingId === entry.id ? (
                    <div className="space-y-3">
                      <input value={editForm.question} onChange={(e) => setEditForm((p) => ({ ...p, question: e.target.value }))}
                        className="w-full px-3 py-2 text-sm font-medium rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                      <textarea value={editForm.answer} onChange={(e) => setEditForm((p) => ({ ...p, answer: e.target.value }))} rows={3}
                        className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500 resize-none" />
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" leftIcon={<X size={11} />} onClick={() => setEditingId(null)}>Cancel</Button>
                        <Button variant="primary" size="sm" leftIcon={saving ? <Loader2 size={11} className="animate-spin" /> : <Save size={11} />}
                          onClick={() => update(entry.id)} disabled={saving}>Save</Button>
                      </div>
                    </div>
                  ) : (
                    <div className="flex items-start gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          {entry.category && <span className="text-xs font-medium text-ai-500 bg-ai-50 px-1.5 py-0.5 rounded">{entry.category}</span>}
                          <span className="text-xs text-text-tertiary">{new Date(entry.createdAt).toLocaleDateString()}</span>
                        </div>
                        <p className="text-sm font-semibold text-text-primary mb-1">{entry.question}</p>
                        <p className="text-sm text-text-secondary line-clamp-3">{entry.answer}</p>
                        {(entry.tags ?? []).length > 0 && (
                          <div className="flex flex-wrap gap-1 mt-2">
                            {entry.tags!.map((t) => <span key={t} className="text-xs text-text-tertiary bg-surface-100 px-1.5 py-0.5 rounded">#{t}</span>)}
                          </div>
                        )}
                      </div>
                      <div className="flex gap-1 flex-shrink-0">
                        <Button variant="outline" size="sm" leftIcon={<Edit2 size={11} />} onClick={() => startEdit(entry)}>Edit</Button>
                        <Button variant="outline" size="sm" leftIcon={<Trash2 size={11} className="text-red-500" />} onClick={() => remove(entry.id)} />
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
