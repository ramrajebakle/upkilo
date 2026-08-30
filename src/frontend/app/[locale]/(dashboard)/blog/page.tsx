'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import { FileText, Plus, Globe, Edit3, Trash2, Eye, Tag, X, BookOpen, TrendingUp, Link2, Copy, Check } from 'lucide-react';
import { cn } from '@/lib/utils';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

interface BlogPost {
    id: string;
    title: string;
    slug: string;
    status: string;
    excerpt: string | null;
    publishedAt: string | null;
    viewCount: number;
    tags: string | null;
    metaTitle: string | null;
    metaDescription: string | null;
    author: string | null;
    featuredImageUrl: string | null;
}

const STATUS_BADGE: Record<string, string> = {
    Published: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    Draft:     'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
    Archived:  'bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-400',
};

const EMPTY_FORM = { title: '', slug: '', metaTitle: '', metaDescription: '', content: '', excerpt: '', tags: '', author: '', featuredImageUrl: '', status: 'Draft' };

export default function BlogPage() {
    const [posts, setPosts] = useState<BlogPost[]>([]);
    const [loading, setLoading] = useState(true);
    const [showEditor, setShowEditor] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState({ ...EMPTY_FORM });
    const [saving, setSaving] = useState(false);
    const [filterStatus, setFilterStatus] = useState('');
    const [tenantSlug, setTenantSlug] = useState('');
    const [copied, setCopied] = useState(false);

    // fetch tenant subdomain once so we can build the full public URL
    useEffect(() => {
        api.settings.getBusiness()
            .then((res: any) => setTenantSlug(res.data?.subdomain || ''))
            .catch(() => {});
    }, []);

    const copy = async (text: string) => {
        await navigator.clipboard.writeText(text);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    const load = async () => {
        setLoading(true);
        try {
            const params: any = {};
            if (filterStatus) params.status = filterStatus;
            const res = await api.blog.list(params);
            setPosts(res.data);
        } catch { /* ignore */ }
        setLoading(false);
    };

    useEffect(() => { load(); }, [filterStatus]);

    const openNew = () => {
        setEditingId(null);
        setForm({ ...EMPTY_FORM });
        setShowEditor(true);
    };

    const openEdit = (p: BlogPost) => {
        setEditingId(p.id);
        setForm({ title: p.title, slug: p.slug, metaTitle: p.metaTitle ?? '', metaDescription: p.metaDescription ?? '', content: '', excerpt: p.excerpt ?? '', tags: p.tags ?? '', author: p.author ?? '', featuredImageUrl: p.featuredImageUrl ?? '', status: p.status });
        setShowEditor(true);
    };

    const save = async () => {
        setSaving(true);
        try {
            const data = { ...form, slug: form.slug.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '') };
            if (editingId) { await api.blog.update(editingId, data); }
            else { await api.blog.create(data); }
            setShowEditor(false);
            load();
        } catch { /* ignore */ }
        setSaving(false);
    };

    const publish = async (id: string) => {
        await api.blog.publish(id);
        load();
    };

    const archive = async (id: string) => {
        await api.blog.delete(id);
        load();
    };

    const autoSlug = (title: string) => title.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '').slice(0, 60);

    const SEO_IDEAS = [
        'Top 5 [Your Service] Tips in [City] — attract local searchers',
        'How Much Does [Service] Cost in [City]? — captures price shoppers',
        'What to Expect at Your First [Service] Appointment — FAQs rank well',
        '[Service] Before & After: Client Transformations — visual + social',
        'Why Regular [Service] Appointments Matter — educational content',
        'Best [Season] [Service] Trends in [City] — seasonal searches spike',
    ];

    return (
        <div className="space-y-8">
            {/* Header */}
            <div className="flex items-start justify-between">
                <div>
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2.5 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl shadow-lg shadow-primary-500/30">
                            <BookOpen className="h-5 w-5 text-white" />
                        </div>
                        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Blog & Content</h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400 text-sm ml-14">
                        Each blog post is a <strong>new keyword</strong> Google can rank your business for. Aim for 1 post/month.
                    </p>
                </div>
                <button
                    onClick={openNew}
                    className="flex items-center gap-2 px-4 py-2.5 bg-primary-500 hover:bg-primary-600 text-white rounded-xl font-medium text-sm shadow-lg shadow-primary-500/30 transition-all"
                >
                    <Plus className="w-4 h-4" /> New Post
                </button>
            </div>

            {/* SEO Impact Banner */}
            <div className="bg-primary-50 dark:bg-primary-900/20 border border-primary-200 dark:border-primary-800/40 rounded-2xl p-4 flex gap-3">
                <TrendingUp className="w-5 h-5 text-primary-600 dark:text-primary-400 shrink-0 mt-0.5" />
                <div className="text-sm">
                    <strong className="text-primary-800 dark:text-primary-300">Why blog posts drive local SEO:</strong>
                    <span className="text-primary-700 dark:text-primary-400"> Businesses with 1+ blog posts get 55% more website visitors. Each post targets keywords your clients search for, building long-term organic traffic without ad spend.</span>
                </div>
            </div>

            {/* Post Ideas */}
            <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm">
                <h3 className="font-semibold text-slate-900 dark:text-white mb-3 text-sm flex items-center gap-2">
                    <Tag className="w-4 h-4 text-primary-500" />
                    High-Ranking Post Ideas (click to use)
                </h3>
                <div className="grid sm:grid-cols-2 gap-2">
                    {SEO_IDEAS.map((idea, i) => (
                        <button
                            key={i}
                            onClick={() => { setEditingId(null); setForm({ ...EMPTY_FORM, title: idea.split(' — ')[0] }); setShowEditor(true); }}
                            className="text-left px-3 py-2.5 bg-slate-50 dark:bg-slate-800 hover:bg-primary-50 dark:hover:bg-primary-900/20 rounded-xl text-sm text-slate-600 dark:text-slate-300 hover:text-primary-700 dark:hover:text-primary-300 transition-colors"
                        >
                            <span className="font-medium text-xs text-foreground-muted block mb-0.5">Idea #{i+1}</span>
                            {idea}
                        </button>
                    ))}
                </div>
            </div>

            {/* Filter */}
            <div className="flex gap-3">
                {['', 'Published', 'Draft', 'Archived'].map(s => (
                    <button key={s} onClick={() => setFilterStatus(s)} className={cn('px-4 py-2 rounded-lg text-sm font-medium transition-all', filterStatus === s ? 'bg-primary-500 text-white shadow' : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700')}>
                        {s || 'All'}
                    </button>
                ))}
            </div>

            {/* Posts Grid */}
            {loading ? (
                <div className="text-center py-16 text-foreground-muted">Loading posts…</div>
            ) : posts.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800">
                    <BookOpen className="w-12 h-12 text-slate-200 mx-auto mb-3" />
                    <p className="font-medium text-slate-600 dark:text-slate-300">No posts yet</p>
                    <p className="text-sm text-foreground-muted mt-1 max-w-xs mx-auto">Write your first blog post to start ranking for local keywords and attracting new clients from Google.</p>
                    <button onClick={openNew} className="mt-4 px-4 py-2 bg-primary-500 text-white rounded-lg text-sm font-medium">Write First Post</button>
                </div>
            ) : (
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    {posts.map((p) => (
                        <div key={p.id} className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-100 dark:border-slate-800 shadow-sm flex flex-col gap-3">
                            <div className="flex items-start justify-between gap-2">
                                <span className={cn('text-xs font-semibold px-2.5 py-1 rounded-full', STATUS_BADGE[p.status] ?? STATUS_BADGE.Draft)}>{p.status}</span>
                                <span className="text-xs text-foreground-muted flex items-center gap-1"><Eye className="w-3 h-3" />{p.viewCount}</span>
                            </div>
                            <div>
                                <h3 className="font-semibold text-slate-900 dark:text-white text-sm leading-snug">{p.title}</h3>
                                {p.excerpt && <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 line-clamp-2">{p.excerpt}</p>}
                            </div>
                            {/* Public URL shown on card */}
                            {p.status === 'Published' && (
                                <a
                                    href={tenantSlug ? `${SITE_URL}/en/book/${tenantSlug}/blog/${p.slug}` : '#'}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="flex items-center gap-1.5 text-xs text-primary-500 hover:text-primary-700 font-mono truncate"
                                >
                                    <Globe className="w-3 h-3 shrink-0" />
                                    {tenantSlug ? `…/book/${tenantSlug}/blog/${p.slug}` : `…/blog/${p.slug}`}
                                </a>
                            )}
                            {p.status === 'Draft' && (
                                <p className="flex items-center gap-1.5 text-xs text-foreground-muted font-mono">
                                    <Link2 className="w-3 h-3 shrink-0" />
                                    …/blog/{p.slug} <span className="font-sans not-italic">(not public yet)</span>
                                </p>
                            )}
                            {p.tags && (
                                <div className="flex flex-wrap gap-1">
                                    {p.tags.split(',').slice(0,3).map(t => (
                                        <span key={t} className="text-xs px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 rounded-full">{t.trim()}</span>
                                    ))}
                                </div>
                            )}
                            <div className="flex gap-2 pt-1 border-t border-slate-100 dark:border-slate-800 mt-auto">
                                <button onClick={() => openEdit(p)} className="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-lg transition-colors">
                                    <Edit3 className="w-3.5 h-3.5" /> Edit
                                </button>
                                {p.status !== 'Published' && (
                                    <button onClick={() => publish(p.id)} className="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs font-medium text-emerald-600 dark:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-900/20 rounded-lg transition-colors">
                                        <Globe className="w-3.5 h-3.5" /> Publish
                                    </button>
                                )}
                                <button onClick={() => archive(p.id)} className="flex items-center justify-center gap-1 px-2 py-1.5 text-xs font-medium text-danger-fg hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors">
                                    <Trash2 className="w-3.5 h-3.5" />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Editor Modal */}
            {showEditor && (
                <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-start justify-center p-4 overflow-y-auto">
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 w-full max-w-2xl shadow-2xl border border-slate-100 dark:border-slate-800 my-8">
                        <div className="flex items-center justify-between mb-5">
                            <h3 className="font-bold text-slate-900 dark:text-white">{editingId ? 'Edit Post' : 'New Blog Post'}</h3>
                            <button onClick={() => setShowEditor(false)} className="p-1 text-foreground-muted hover:text-foreground-secondary rounded-lg"><X className="w-5 h-5" /></button>
                        </div>

                        <div className="space-y-4">
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Title *</label>
                                <input
                                    value={form.title}
                                    onChange={e => setForm(f => ({...f, title: e.target.value, slug: autoSlug(e.target.value)}))}
                                    className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white"
                                    placeholder="Top 5 Haircut Tips in London"
                                />
                            </div>

                            {/* URL Slug with full public URL preview */}
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">
                                    URL Slug
                                    <span className="ml-2 font-normal normal-case text-foreground-muted">— auto-generated from title, editable</span>
                                </label>
                                <input
                                    value={form.slug}
                                    onChange={e => setForm(f => ({...f, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-')}))}
                                    className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white font-mono"
                                    placeholder="top-5-haircut-tips-in-london"
                                />
                                {/* Full public URL preview */}
                                {form.slug && (
                                    <div className="mt-2 flex items-center gap-2 px-3 py-2 bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800/40 rounded-lg">
                                        <Link2 className="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400 shrink-0" />
                                        <p className="text-xs text-emerald-700 dark:text-emerald-300 font-mono flex-1 truncate">
                                            {tenantSlug
                                                ? `${SITE_URL}/en/book/${tenantSlug}/blog/${form.slug}`
                                                : `${SITE_URL}/en/blog/${form.slug}`}
                                        </p>
                                        <button
                                            type="button"
                                            onClick={() => copy(tenantSlug ? `${SITE_URL}/en/book/${tenantSlug}/blog/${form.slug}` : `${SITE_URL}/en/blog/${form.slug}`)}
                                            className="shrink-0 p-1 text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 transition-colors"
                                        >
                                            {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                                        </button>
                                    </div>
                                )}
                                <p className="mt-1.5 text-xs text-foreground-muted">
                                    This is the link your clients and Google will use to find this blog post. Keep it short and descriptive.
                                </p>
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Meta Title <span className="text-slate-400">(shown in Google results)</span></label>
                                <input value={form.metaTitle} onChange={e => setForm(f => ({...f, metaTitle: e.target.value}))} maxLength={60} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white" placeholder="Keep under 60 characters" />
                                <p className="text-xs text-foreground-muted mt-1">{form.metaTitle.length}/60</p>
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Meta Description <span className="text-slate-400">(shown in Google preview)</span></label>
                                <textarea value={form.metaDescription} onChange={e => setForm(f => ({...f, metaDescription: e.target.value}))} maxLength={155} rows={2} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white resize-none" placeholder="Describe the post in 120-155 characters to boost click-through rate" />
                                <p className={cn('text-xs mt-1', form.metaDescription.length > 155 ? 'text-danger-fg' : 'text-foreground-muted')}>{form.metaDescription.length}/155</p>
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Short Excerpt</label>
                                <textarea value={form.excerpt} onChange={e => setForm(f => ({...f, excerpt: e.target.value}))} rows={2} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white resize-none" placeholder="1-2 sentence summary shown on the listing card" />
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Content (HTML or plain text)</label>
                                <textarea value={form.content} onChange={e => setForm(f => ({...f, content: e.target.value}))} rows={8} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white resize-none font-mono text-xs" placeholder="Write your blog post here…" />
                            </div>

                            <div className="grid sm:grid-cols-2 gap-4">
                                <div>
                                    <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Tags <span className="text-slate-400">(comma-separated)</span></label>
                                    <input value={form.tags} onChange={e => setForm(f => ({...f, tags: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white" placeholder="haircut, london, tips" />
                                </div>
                                <div>
                                    <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Author</label>
                                    <input value={form.author} onChange={e => setForm(f => ({...f, author: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white" placeholder="Your Name" />
                                </div>
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Featured Image URL</label>
                                <input value={form.featuredImageUrl} onChange={e => setForm(f => ({...f, featuredImageUrl: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white" placeholder="https://…" />
                            </div>

                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Status</label>
                                <select value={form.status} onChange={e => setForm(f => ({...f, status: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white">
                                    <option value="Draft">Draft (save privately)</option>
                                    <option value="Published">Published (visible on booking page)</option>
                                </select>
                            </div>
                        </div>

                        <div className="flex gap-2 mt-6">
                            <button onClick={save} disabled={!form.title || saving} className="flex-1 py-2.5 bg-primary-500 text-white rounded-xl font-semibold text-sm disabled:opacity-50">
                                {saving ? 'Saving…' : editingId ? 'Save Changes' : 'Create Post'}
                            </button>
                            <button onClick={() => setShowEditor(false)} className="px-4 py-2.5 text-slate-600 dark:text-slate-300 rounded-xl font-semibold text-sm">Cancel</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}