"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    Globe, Plus, Search, Eye, Trash2, Copy, BarChart2, Settings,
    RefreshCw, CheckCircle, Clock, AlertCircle, ExternalLink, FileText
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface LandingPage {
    id: string;
    name: string;
    slug: string;
    status: string;
    isPublished: boolean;
    views: number;
    conversions: number;
    conversionRate: number;
    createdAt: string;
    updatedAt: string;
    seoTitle?: string;
    metaDescription?: string;
}

const statusColor: Record<string, string> = {
    published: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
    draft: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
    archived: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
};

export default function LandingPagesPage() {
    const router = useRouter();
    const [pages, setPages] = useState<LandingPage[]>([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [creating, setCreating] = useState(false);
    const [showSeoModal, setShowSeoModal] = useState<string | null>(null);
    const [seoForm, setSeoForm] = useState({ title: '', metaDescription: '', keywords: '', ogTitle: '', ogDescription: '' });
    const [savingSeo, setSavingSeo] = useState(false);
    const [showAnalytics, setShowAnalytics] = useState<string | null>(null);
    const [analytics, setAnalytics] = useState<any>(null);

    const fetchPages = useCallback(async () => {
        try {
            setLoading(true);
            const res = await apiClient.get('/api/landing-pages');
            setPages(res.data?.data || res.data || []);
        } catch {
            toast.error('Failed to load landing pages');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchPages(); }, [fetchPages]);

    const handleCreate = async () => {
        const name = prompt('Landing page name:');
        if (!name) return;
        const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-');
        setCreating(true);
        try {
            const res = await apiClient.post('/api/landing-pages', {
                name, slug, title: name, sections: []
            });
            const newPage = res.data?.data || res.data;
            setPages(prev => [newPage, ...prev]);
            toast.success('Landing page created');
        } catch {
            toast.error('Failed to create landing page');
        } finally {
            setCreating(false);
        }
    };

    const handlePublish = async (page: LandingPage) => {
        const endpoint = page.isPublished ? 'unpublish' : 'publish';
        try {
            await apiClient.post(`/api/landing-pages/${page.id}/${endpoint}`);
            setPages(prev => prev.map(p => p.id === page.id ? { ...p, isPublished: !p.isPublished, status: !p.isPublished ? 'published' : 'draft' } : p));
            toast.success(`Page ${page.isPublished ? 'unpublished' : 'published'}`);
        } catch {
            toast.error('Failed to update page status');
        }
    };

    const handleDuplicate = async (page: LandingPage) => {
        try {
            const res = await apiClient.post(`/api/landing-pages/${page.id}/duplicate`);
            const copy = res.data?.data || res.data;
            setPages(prev => [copy, ...prev]);
            toast.success('Page duplicated');
        } catch {
            toast.error('Failed to duplicate page');
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Delete this landing page?')) return;
        try {
            await apiClient.delete(`/api/landing-pages/${id}`);
            setPages(prev => prev.filter(p => p.id !== id));
            toast.success('Page deleted');
        } catch {
            toast.error('Failed to delete page');
        }
    };

    const openSeoModal = async (page: LandingPage) => {
        setShowSeoModal(page.id);
        try {
            const res = await apiClient.get(`/api/landing-pages/${page.id}/seo`);
            const seo = res.data?.data || res.data || {};
            setSeoForm({
                title: seo.title || page.seoTitle || '',
                metaDescription: seo.metaDescription || page.metaDescription || '',
                keywords: seo.keywords || '',
                ogTitle: seo.ogTitle || '',
                ogDescription: seo.ogDescription || '',
            });
        } catch {
            setSeoForm({ title: '', metaDescription: '', keywords: '', ogTitle: '', ogDescription: '' });
        }
    };

    const saveSeo = async () => {
        if (!showSeoModal) return;
        setSavingSeo(true);
        try {
            await apiClient.put(`/api/landing-pages/${showSeoModal}/seo`, seoForm);
            toast.success('SEO settings saved');
            setShowSeoModal(null);
        } catch {
            toast.error('Failed to save SEO settings');
        } finally {
            setSavingSeo(false);
        }
    };

    const openAnalytics = async (page: LandingPage) => {
        setShowAnalytics(page.id);
        setAnalytics(null);
        try {
            const res = await apiClient.get(`/api/landing-pages/${page.id}/analytics`);
            setAnalytics(res.data?.data || res.data);
        } catch {
            setAnalytics({ error: true });
        }
    };

    const filtered = pages.filter(p =>
        !search || p.name.toLowerCase().includes(search.toLowerCase()) || p.slug.toLowerCase().includes(search.toLowerCase())
    );

    const stats = {
        total: pages.length,
        published: pages.filter(p => p.isPublished).length,
        totalViews: pages.reduce((s, p) => s + (p.views || 0), 0),
        avgConversion: pages.filter(p => p.conversionRate).length > 0
            ? (pages.reduce((s, p) => s + (p.conversionRate || 0), 0) / pages.filter(p => p.conversionRate).length).toFixed(1)
            : '0',
    };

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Landing Pages</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1">Build and optimize conversion-focused pages</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchPages} className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 dark:text-slate-400">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={handleCreate} disabled={creating} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Page
                    </Button>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                {[
                    { label: 'Total Pages', value: stats.total, icon: <FileText className="h-5 w-5 text-indigo-500" /> },
                    { label: 'Published', value: stats.published, icon: <Globe className="h-5 w-5 text-emerald-500" /> },
                    { label: 'Total Views', value: stats.totalViews.toLocaleString(), icon: <Eye className="h-5 w-5 text-blue-500" /> },
                    { label: 'Avg Conv Rate', value: `${stats.avgConversion}%`, icon: <BarChart2 className="h-5 w-5 text-amber-500" /> },
                ].map(s => (
                    <div key={s.label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 flex items-center gap-3 shadow-sm">
                        <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded-lg">{s.icon}</div>
                        <div>
                            <div className="text-xl font-bold text-slate-900 dark:text-white">{s.value}</div>
                            <div className="text-xs text-slate-500 dark:text-slate-400">{s.label}</div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Search */}
            <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 dark:text-slate-500" />
                <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search pages..." className="pl-9" />
            </div>

            {/* Pages Grid */}
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {[...Array(6)].map((_, i) => <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 animate-pulse h-40" />)}
                </div>
            ) : filtered.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                    <Globe className="h-12 w-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">No landing pages yet</h3>
                    <p className="text-slate-500 dark:text-slate-400 text-sm mt-1 mb-4">Create your first page to capture leads</p>
                    <Button onClick={handleCreate}><Plus className="h-4 w-4 mr-2" /> New Page</Button>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {filtered.map(page => (
                        <div key={page.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden hover:shadow-md transition-shadow group">
                            {/* Preview area */}
                            <div className="h-28 bg-gradient-to-br from-indigo-50 to-purple-50 dark:from-slate-800 dark:to-slate-800/50 flex items-center justify-center relative">
                                <Globe className="h-10 w-10 text-indigo-200 dark:text-slate-700" />
                                <div className="absolute top-2 left-2">
                                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${statusColor[page.status] || 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400 border-transparent'}`}>
                                        {page.status || (page.isPublished ? 'published' : 'draft')}
                                    </span>
                                </div>
                                {page.isPublished && (
                                    <div className="absolute top-2 right-2">
                                        <CheckCircle className="h-4 w-4 text-emerald-500" />
                                    </div>
                                )}
                            </div>

                            <div className="p-4">
                                <h3 className="font-semibold text-slate-900 dark:text-white mb-0.5">{page.name}</h3>
                                <p className="text-xs text-slate-400 dark:text-slate-500 mb-3 flex items-center gap-1">
                                    <Globe className="h-3 w-3" /> /{page.slug}
                                </p>

                                <div className="flex items-center gap-3 text-xs text-slate-500 dark:text-slate-400 mb-4">
                                    <span className="flex items-center gap-1"><Eye className="h-3 w-3" /> {(page.views || 0).toLocaleString()} views</span>
                                    {page.conversionRate > 0 && (
                                        <span className="flex items-center gap-1"><BarChart2 className="h-3 w-3" /> {page.conversionRate.toFixed(1)}% conv</span>
                                    )}
                                </div>

                                {/* Actions */}
                                <div className="flex gap-1.5">
                                    <button
                                        onClick={() => handlePublish(page)}
                                        className={`flex-1 py-1.5 text-xs font-medium rounded-lg border transition-colors ${
                                            page.isPublished 
                                                ? 'border-amber-200 dark:border-amber-900/40 text-amber-700 dark:text-amber-400 bg-amber-50/50 dark:bg-amber-900/10 hover:bg-amber-100 dark:hover:bg-amber-900/20' 
                                                : 'border-emerald-200 dark:border-emerald-900/40 text-emerald-700 dark:text-emerald-400 bg-emerald-50/50 dark:bg-emerald-900/10 hover:bg-emerald-100 dark:hover:bg-emerald-900/20'
                                        }`}
                                    >
                                        {page.isPublished ? 'Unpublish' : 'Publish'}
                                    </button>
                                    <button
                                        onClick={() => openSeoModal(page)}
                                        className="p-1.5 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-lg transition-colors"
                                        title="SEO Settings"
                                    >
                                        <Settings className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => openAnalytics(page)}
                                        className="p-1.5 text-slate-400 dark:text-slate-500 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded-lg transition-colors"
                                        title="Analytics"
                                    >
                                        <BarChart2 className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => handleDuplicate(page)}
                                        className="p-1.5 text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                                        title="Duplicate"
                                    >
                                        <Copy className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => handleDelete(page.id)}
                                        className="p-1.5 text-slate-400 dark:text-slate-500 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 rounded-lg transition-colors"
                                        title="Delete"
                                    >
                                        <Trash2 className="h-3.5 w-3.5" />
                                    </button>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* SEO Modal */}
            {showSeoModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
                    <div className="fixed inset-0" onClick={() => setShowSeoModal(null)} />
                    <div className="relative w-full max-w-lg bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800">
                            <h2 className="font-semibold text-slate-900 dark:text-white">SEO Settings</h2>
                            <button onClick={() => setShowSeoModal(null)} className="text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300">✕</button>
                        </div>
                        <div className="p-6 space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">SEO Title <span className="text-slate-400 dark:text-slate-500 font-normal">(60 chars max)</span></label>
                                <Input value={seoForm.title} onChange={e => setSeoForm(p => ({ ...p, title: e.target.value.slice(0, 60) }))} placeholder="Page title for search engines" />
                                <p className="text-xs text-slate-400 dark:text-slate-500 mt-0.5">{seoForm.title.length}/60</p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Meta Description <span className="text-slate-400 dark:text-slate-500 font-normal">(160 chars max)</span></label>
                                <textarea
                                    value={seoForm.metaDescription}
                                    onChange={e => setSeoForm(p => ({ ...p, metaDescription: e.target.value.slice(0, 160) }))}
                                    className="w-full border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm h-20 resize-none focus:ring-2 focus:ring-indigo-500"
                                    placeholder="Brief description for search results..."
                                />
                                <p className="text-xs text-slate-400 dark:text-slate-500 mt-0.5">{seoForm.metaDescription.length}/160</p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Keywords <span className="text-slate-400 dark:text-slate-500 font-normal">(comma-separated)</span></label>
                                <Input value={seoForm.keywords} onChange={e => setSeoForm(p => ({ ...p, keywords: e.target.value }))} placeholder="booking, salon, spa, ..." />
                            </div>
                            <div className="border-t border-slate-100 dark:border-slate-800 pt-4">
                                <p className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-3">Open Graph (Social Sharing)</p>
                                <div className="space-y-3">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">OG Title</label>
                                        <Input value={seoForm.ogTitle} onChange={e => setSeoForm(p => ({ ...p, ogTitle: e.target.value }))} placeholder="Title for social sharing" />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">OG Description</label>
                                        <Input value={seoForm.ogDescription} onChange={e => setSeoForm(p => ({ ...p, ogDescription: e.target.value }))} placeholder="Description for social sharing" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div className="flex gap-3 px-6 py-4 border-t border-slate-100 dark:border-slate-800">
                            <Button onClick={saveSeo} disabled={savingSeo} className="flex-1">
                                {savingSeo ? 'Saving...' : 'Save SEO Settings'}
                            </Button>
                            <Button variant="outline" onClick={() => setShowSeoModal(null)} className="dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700">Cancel</Button>
                        </div>
                    </div>
                </div>
            )}

            {/* Analytics Modal */}
            {showAnalytics && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
                    <div className="fixed inset-0" onClick={() => setShowAnalytics(null)} />
                    <div className="relative w-full max-w-md bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800">
                            <h2 className="font-semibold text-slate-900 dark:text-white">Page Analytics</h2>
                            <button onClick={() => setShowAnalytics(null)} className="text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300">✕</button>
                        </div>
                        <div className="p-6">
                            {!analytics ? (
                                <div className="flex items-center justify-center py-8">
                                    <RefreshCw className="h-6 w-6 animate-spin text-indigo-500" />
                                </div>
                            ) : analytics.error ? (
                                <p className="text-center text-slate-500 py-8">No analytics data available</p>
                            ) : (
                                <div className="space-y-4">
                                    {[
                                        { label: 'Total Views', value: analytics.totalViews?.toLocaleString() || '0' },
                                        { label: 'Unique Visitors', value: analytics.uniqueVisitors?.toLocaleString() || '0' },
                                        { label: 'Conversions', value: analytics.conversions?.toLocaleString() || '0' },
                                        { label: 'Conversion Rate', value: `${(analytics.conversionRate || 0).toFixed(1)}%` },
                                        { label: 'Avg Time on Page', value: analytics.avgTimeOnPage ? `${Math.round(analytics.avgTimeOnPage)}s` : '—' },
                                        { label: 'Bounce Rate', value: analytics.bounceRate ? `${(analytics.bounceRate).toFixed(1)}%` : '—' },
                                    ].map(s => (
                                        <div key={s.label} className="flex items-center justify-between py-2 border-b border-slate-50 dark:border-slate-800 last:border-0">
                                            <span className="text-sm text-slate-600 dark:text-slate-400">{s.label}</span>
                                            <span className="font-semibold text-slate-900 dark:text-white">{s.value}</span>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
