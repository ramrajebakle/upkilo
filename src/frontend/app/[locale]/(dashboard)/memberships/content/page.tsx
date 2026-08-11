"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
  Plus, Video, FileText, Download, Lock, Unlock, Search, Filter,
  MoreVertical, Eye, Edit, Trash2, BookOpen, Clock, Star
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Badge } from '@/components/ui/Badge';
import { toast } from 'sonner';

interface ContentItem {
  id: string;
  title: string;
  description?: string;
  contentType: 'video' | 'pdf' | 'article' | 'download';
  accessLevel: string; // membership plan name
  isGated: boolean;
  thumbnailUrl?: string;
  fileUrl?: string;
  duration?: number; // minutes for video
  fileSize?: string;
  viewCount: number;
  releaseDate?: string;
  createdAt: string;
}

const typeIcons: Record<string, React.ReactNode> = {
  video: <Video className="h-4 w-4" />,
  pdf: <FileText className="h-4 w-4" />,
  article: <BookOpen className="h-4 w-4" />,
  download: <Download className="h-4 w-4" />,
};

const typeColors: Record<string, string> = {
  video: 'bg-primary-50 text-primary-700',
  pdf: 'bg-red-50 text-red-700',
  article: 'bg-blue-50 text-blue-700',
  download: 'bg-emerald-50 text-emerald-700',
};

export default function MembershipContentPage() {
  const [content, setContent] = useState<ContentItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    title: '', description: '', contentType: 'article' as const, accessLevel: 'basic',
    isGated: true, fileUrl: '', duration: '', releaseDate: ''
  });

  const fetchContent = useCallback(async () => {
    try {
      setLoading(true);
      const res = await apiClient.get('/api/v1/memberships/content');
      setContent(res.data?.data || res.data || []);
    } catch {
      toast.error('Failed to load content library');
      setContent([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchContent(); }, [fetchContent]);

  const handleCreate = async () => {
    if (!form.title) { toast.error('Title is required'); return; }
    try {
      const res = await apiClient.post('/api/v1/memberships/content', form);
      setContent(prev => [res.data?.data || res.data, ...prev]);
      setShowForm(false);
      setForm({ title: '', description: '', contentType: 'article', accessLevel: 'basic', isGated: true, fileUrl: '', duration: '', releaseDate: '' });
      toast.success('Content added');
    } catch {
      toast.error('Failed to add content');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this content item?')) return;
    try {
      await apiClient.delete(`/api/v1/memberships/content/${id}`);
      setContent(prev => prev.filter(c => c.id !== id));
      toast.success('Content deleted');
    } catch {
      toast.error('Failed to delete content');
    }
  };

  const filtered = content.filter(c => {
    const matchSearch = !search || c.title.toLowerCase().includes(search.toLowerCase());
    const matchType = typeFilter === 'all' || c.contentType === typeFilter;
    return matchSearch && matchType;
  });

  const stats = {
    total: content.length,
    gated: content.filter(c => c.isGated).length,
    totalViews: content.reduce((sum, c) => sum + (c.viewCount || 0), 0),
  };

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Content Library</h1>
          <p className="text-slate-500 mt-1">Manage gated content for your membership tiers</p>
        </div>
        <Button onClick={() => setShowForm(true)} className="flex items-center gap-2">
          <Plus className="h-4 w-4" /> Add Content
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Total Content', value: stats.total, icon: <BookOpen className="h-5 w-5 text-primary-500" /> },
          { label: 'Gated Content', value: stats.gated, icon: <Lock className="h-5 w-5 text-amber-500" /> },
          { label: 'Total Views', value: stats.totalViews.toLocaleString(), icon: <Eye className="h-5 w-5 text-emerald-500" /> },
        ].map(stat => (
          <div key={stat.label} className="bg-white border border-slate-200 rounded-xl p-4 flex items-center gap-3">
            <div className="p-2 rounded-lg bg-slate-50">{stat.icon}</div>
            <div>
              <div className="text-2xl font-bold text-slate-900">{stat.value}</div>
              <div className="text-xs text-slate-500">{stat.label}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Create Form */}
      {showForm && (
        <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-slate-900">Add Content</h2>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-600">✕</button>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-1">Title</label>
              <Input value={form.title} onChange={e => setForm(p => ({ ...p, title: e.target.value }))} placeholder="e.g. Introduction to Yoga Meditation" />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Content Type</label>
              <select
                value={form.contentType}
                onChange={e => setForm(p => ({ ...p, contentType: e.target.value as 'article' }))}
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
              >
                <option value="article">Article</option>
                <option value="video">Video</option>
                <option value="pdf">PDF</option>
                <option value="download">Download</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Access Level</label>
              <select
                value={form.accessLevel}
                onChange={e => setForm(p => ({ ...p, accessLevel: e.target.value }))}
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
              >
                <option value="free">Free</option>
                <option value="basic">Basic</option>
                <option value="premium">Premium</option>
                <option value="vip">VIP</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">File URL</label>
              <Input value={form.fileUrl} onChange={e => setForm(p => ({ ...p, fileUrl: e.target.value }))} placeholder="https://..." />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Release Date (optional)</label>
              <Input type="date" value={form.releaseDate} onChange={e => setForm(p => ({ ...p, releaseDate: e.target.value }))} />
            </div>
            <div className="col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
              <textarea
                value={form.description}
                onChange={e => setForm(p => ({ ...p, description: e.target.value }))}
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm h-20 resize-none"
                placeholder="Brief description..."
              />
            </div>
            <div className="col-span-2 flex items-center gap-3">
              <input
                type="checkbox"
                id="gated"
                checked={form.isGated}
                onChange={e => setForm(p => ({ ...p, isGated: e.target.checked }))}
                className="h-4 w-4 rounded border-slate-300 text-primary-600"
              />
              <label htmlFor="gated" className="text-sm text-slate-700 flex items-center gap-1.5">
                <Lock className="h-3.5 w-3.5 text-amber-500" /> Gated content (requires membership)
              </label>
            </div>
          </div>
          <div className="flex gap-3 pt-2 border-t border-slate-100">
            <Button onClick={handleCreate} className="flex-1">Add Content</Button>
            <Button variant="outline" onClick={() => setShowForm(false)}>Cancel</Button>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search content..." className="pl-9" />
        </div>
        <div className="flex gap-1">
          {['all', 'video', 'pdf', 'article', 'download'].map(type => (
            <button
              key={type}
              onClick={() => setTypeFilter(type)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium capitalize transition-colors ${typeFilter === type ? 'bg-primary-600 text-white' : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50'}`}
            >
              {type}
            </button>
          ))}
        </div>
      </div>

      {/* Content Grid */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="bg-white border border-slate-200 rounded-xl p-4 animate-pulse">
              <div className="h-32 bg-slate-200 rounded-lg mb-3" />
              <div className="h-5 bg-slate-200 rounded w-3/4 mb-2" />
              <div className="h-4 bg-slate-100 rounded w-1/2" />
            </div>
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-slate-200">
          <BookOpen className="h-12 w-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-slate-700">No content yet</h3>
          <p className="text-slate-500 text-sm mt-1 mb-4">Add your first piece of content to the library</p>
          <Button onClick={() => setShowForm(true)}><Plus className="h-4 w-4 mr-2" /> Add Content</Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(item => (
            <div key={item.id} className="bg-white border border-slate-200 rounded-xl overflow-hidden hover:shadow-md transition-shadow group">
              {/* Thumbnail / Placeholder */}
              <div className="h-32 bg-gradient-to-br from-slate-100 to-slate-200 flex items-center justify-center relative">
                {item.thumbnailUrl ? (
                  <img src={item.thumbnailUrl} alt={item.title} className="w-full h-full object-cover" />
                ) : (
                  <div className={`p-4 rounded-xl ${typeColors[item.contentType]}`}>
                    {typeIcons[item.contentType]}
                  </div>
                )}
                {item.isGated && (
                  <div className="absolute top-2 right-2 bg-amber-500 text-white rounded-full p-1">
                    <Lock className="h-3 w-3" />
                  </div>
                )}
                {item.duration && (
                  <div className="absolute bottom-2 right-2 bg-black/60 text-white text-xs rounded px-1.5 py-0.5 flex items-center gap-1">
                    <Clock className="h-3 w-3" /> {item.duration}m
                  </div>
                )}
              </div>

              <div className="p-4">
                <div className="flex items-start justify-between gap-2 mb-2">
                  <h3 className="font-semibold text-slate-900 text-sm line-clamp-2">{item.title}</h3>
                  <div className="relative shrink-0">
                    <button className="p-1 text-slate-400 hover:text-slate-600 rounded">
                      <MoreVertical className="h-4 w-4" />
                    </button>
                  </div>
                </div>
                {item.description && <p className="text-xs text-slate-500 line-clamp-2 mb-3">{item.description}</p>}

                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${typeColors[item.contentType]}`}>
                      {item.contentType}
                    </span>
                    <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 capitalize">
                      {item.accessLevel}
                    </span>
                  </div>
                  <div className="flex items-center gap-1 text-xs text-slate-400">
                    <Eye className="h-3 w-3" /> {item.viewCount}
                  </div>
                </div>

                {item.releaseDate && new Date(item.releaseDate) > new Date() && (
                  <div className="mt-2 text-xs text-amber-600 flex items-center gap-1">
                    <Clock className="h-3 w-3" /> Releases {new Date(item.releaseDate).toLocaleDateString()}
                  </div>
                )}

                <div className="flex gap-2 mt-3 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button onClick={() => handleDelete(item.id)} className="flex-1 py-1.5 text-xs text-red-600 border border-red-200 rounded-lg hover:bg-red-50 transition-colors flex items-center justify-center gap-1">
                    <Trash2 className="h-3 w-3" /> Delete
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
