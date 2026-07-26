"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
    MessageSquare, Plus, Search, Edit, Trash2, Copy, RefreshCw, Save, X
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface SmsTemplate {
    templateId: string;
    name: string;
    body: string;
    category: string;
    createdAt: string;
}

const CATEGORIES = ['All', 'Booking', 'Reminder', 'Promotional', 'Follow-up', 'Emergency'];

const VARIABLES = [
    '{{client_name}}', '{{business_name}}', '{{service_name}}',
    '{{date}}', '{{time}}', '{{staff_name}}', '{{booking_link}}',
    '{{cancel_link}}', '{{phone}}', '{{promo_code}}'
];

export default function SmsTemplatesPage() {
    const [templates, setTemplates] = useState<SmsTemplate[]>([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [category, setCategory] = useState('All');
    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState({ name: '', body: '', category: 'Booking' });
    const [saving, setSaving] = useState(false);

    const fetchTemplates = useCallback(async () => {
        try {
            setLoading(true);
            const res = await apiClient.get('/api/v1/sms/templates');
            setTemplates(res.data?.data || res.data || []);
        } catch {
            toast.error('Failed to load SMS templates');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchTemplates(); }, [fetchTemplates]);

    const handleSave = async () => {
        if (!form.name || !form.body) { toast.error('Name and body are required'); return; }
        setSaving(true);
        try {
            const res = await apiClient.post('/api/v1/sms/templates', form);
            const newTemplate = res.data?.data || res.data;
            setTemplates(prev => [newTemplate, ...prev]);
            setShowForm(false);
            setForm({ name: '', body: '', category: 'Booking' });
            toast.success('Template saved');
        } catch {
            toast.error('Failed to save template');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async (templateId: string) => {
        if (!confirm('Delete this template?')) return;
        try {
            await apiClient.delete(`/api/v1/sms/templates/${templateId}`);
            setTemplates(prev => prev.filter(t => t.templateId !== templateId));
            toast.success('Template deleted');
        } catch {
            toast.error('Failed to delete template');
        }
    };

    const insertVariable = (v: string) => {
        setForm(prev => ({ ...prev, body: prev.body + v }));
    };

    const charCount = form.body.length;
    const smsCount = Math.ceil(charCount / 160) || 1;

    const filtered = templates.filter(t => {
        const matchSearch = !search || t.name.toLowerCase().includes(search.toLowerCase()) || t.body.toLowerCase().includes(search.toLowerCase());
        const matchCategory = category === 'All' || t.category === category;
        return matchSearch && matchCategory;
    });

    return (
        <div className="p-6 max-w-5xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white">SMS Template Library</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1">Reusable SMS messages for campaigns and automations</p>
                </div>
                <div className="flex gap-2">
                    <button onClick={fetchTemplates} className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 dark:text-slate-400">
                        <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                    </button>
                    <Button onClick={() => { setShowForm(true); setEditingId(null); setForm({ name: '', body: '', category: 'Booking' }); }} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" /> New Template
                    </Button>
                </div>
            </div>

            {/* Create/Edit Form */}
            {showForm && (
                <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-6 space-y-4 shadow-sm">
                    <div className="flex items-center justify-between">
                        <h2 className="font-semibold text-slate-900 dark:text-white">{editingId ? 'Edit Template' : 'New SMS Template'}</h2>
                        <button onClick={() => setShowForm(false)} className="text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300 transition-colors"><X className="h-4 w-4" /></button>
                    </div>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Template Name</label>
                            <Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} placeholder="e.g., Booking Confirmation" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Category</label>
                            <select
                                value={form.category}
                                onChange={e => setForm(p => ({ ...p, category: e.target.value }))}
                                className="w-full border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500 transition-shadow"
                            >
                                {CATEGORIES.filter(c => c !== 'All').map(c => <option key={c} value={c}>{c}</option>)}
                            </select>
                        </div>
                    </div>

                    <div>
                        <div className="flex items-center justify-between mb-1">
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Message Body</label>
                            <span className="text-xs text-slate-400 dark:text-slate-500">{charCount} chars · {smsCount} SMS</span>
                        </div>
                        <textarea
                            value={form.body}
                            onChange={e => setForm(p => ({ ...p, body: e.target.value }))}
                            className="w-full border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg px-3 py-2 text-sm h-28 resize-none focus:ring-2 focus:ring-indigo-500 transition-shadow"
                            placeholder="Hi {{client_name}}, your appointment at {{business_name}} is confirmed for {{date}} at {{time}}. See you then!"
                        />
                        {/* Character warning */}
                        {charCount > 160 && (
                            <p className="text-xs text-amber-600 mt-0.5">This message will be split into {smsCount} SMS messages</p>
                        )}
                    </div>

                    {/* Variable inserter */}
                    <div>
                        <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mb-2">Insert Variables:</p>
                        <div className="flex flex-wrap gap-1.5">
                            {VARIABLES.map(v => (
                                <button
                                    key={v}
                                    onClick={() => insertVariable(v)}
                                    className="px-2 py-0.5 bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 rounded text-xs font-mono hover:bg-indigo-100 dark:hover:bg-indigo-900/50 transition-colors border border-indigo-100 dark:border-indigo-800"
                                >
                                    {v}
                                </button>
                            ))}
                        </div>
                    </div>

                    <div className="flex gap-3 pt-2 border-t border-slate-100 dark:border-slate-800">
                        <Button onClick={handleSave} disabled={saving}>
                            <Save className="h-4 w-4 mr-2" />
                            {saving ? 'Saving...' : 'Save Template'}
                        </Button>
                        <Button variant="outline" onClick={() => setShowForm(false)} className="dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700">Cancel</Button>
                    </div>
                </div>
            )}

            {/* Filters */}
            <div className="flex gap-3 flex-wrap">
                <div className="relative flex-1 min-w-48">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 dark:text-slate-500" />
                    <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search templates..." className="pl-9" />
                </div>
                <div className="flex gap-1">
                    {CATEGORIES.map(cat => (
                        <button
                            key={cat}
                            onClick={() => setCategory(cat)}
                            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${category === cat ? 'bg-indigo-600 dark:bg-indigo-500 text-white shadow-sm' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'}`}
                        >
                            {cat}
                        </button>
                    ))}
                </div>
            </div>

            {/* Templates */}
            {loading ? (
                <div className="space-y-3">
                    {[...Array(4)].map((_, i) => <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm rounded-xl p-4 animate-pulse h-24" />)}
                </div>
            ) : filtered.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm">
                    <MessageSquare className="h-12 w-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" />
                    <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">No templates found</h3>
                    <p className="text-slate-500 dark:text-slate-400 text-sm mt-1 mb-4">Create your first SMS template</p>
                    <Button onClick={() => setShowForm(true)}><Plus className="h-4 w-4 mr-2" /> New Template</Button>
                </div>
            ) : (
                <div className="space-y-3">
                    {filtered.map(template => (
                        <div key={template.templateId} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 hover:shadow-md transition-all shadow-sm">
                            <div className="flex items-start justify-between gap-4">
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 mb-1">
                                        <span className="font-semibold text-slate-900 dark:text-white">{template.name}</span>
                                        <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 border border-purple-100 dark:border-purple-800">
                                            {template.category}
                                        </span>
                                        <span className="text-xs text-slate-400 dark:text-slate-500">
                                            {template.body.length} chars · {Math.ceil(template.body.length / 160) || 1} SMS
                                        </span>
                                    </div>
                                    <p className="text-sm text-slate-600 dark:text-slate-400 bg-slate-50 dark:bg-slate-800/50 rounded-lg px-3 py-2 font-mono leading-relaxed border border-slate-100 dark:border-slate-800">
                                        {template.body}
                                    </p>
                                </div>
                                <div className="flex items-center gap-1 shrink-0">
                                    <button
                                        onClick={() => {
                                            navigator.clipboard.writeText(template.body);
                                            toast.success('Copied!');
                                        }}
                                        className="p-1.5 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-lg transition-colors"
                                        title="Copy body"
                                    >
                                        <Copy className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => handleDelete(template.templateId)}
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
        </div>
    );
}
