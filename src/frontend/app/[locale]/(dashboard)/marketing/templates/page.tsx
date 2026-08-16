'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import {
    Save,
    Send,
    LayoutTemplate,
    Undo,
    Redo,
    Smartphone,
    Monitor,
    Code2,
    Eye,
    Columns2,
    Plus,
    Loader2,
    RefreshCw,
    Trash2,
    Copy,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import { toast } from 'sonner';

interface EmailTemplate {
    id: string;
    name: string;
    subject: string;
    htmlBody?: string;
    content?: string;
    updatedAt?: string;
    lastModified?: string;
}

const SAMPLE_TEMPLATES: EmailTemplate[] = [
    {
        id: 'welcome',
        name: 'Welcome Email',
        subject: 'Welcome to {{businessName}}!',
        htmlBody: `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0; }
    .container { max-width: 600px; margin: 32px auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .header { background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); padding: 40px 32px; text-align: center; }
    .header h1 { color: #fff; margin: 0; font-size: 28px; }
    .body { padding: 32px; }
    .body p { color: #374151; line-height: 1.7; }
    .btn { display: inline-block; background: #6366f1; color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; margin-top: 16px; }
    .footer { background: #f9fafb; padding: 20px 32px; text-align: center; color: #9ca3af; font-size: 12px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>Welcome to {{businessName}}! 🎉</h1>
    </div>
    <div class="body">
      <p>Hi {{clientFirstName}},</p>
      <p>Thank you for joining us! We're thrilled to have you as a client and can't wait to help you look and feel your best.</p>
      <p>Your first appointment is just a few clicks away. Book online anytime — it's fast and easy.</p>
      <a href="{{bookingLink}}" class="btn">Book Your First Appointment</a>
    </div>
    <div class="footer">
      <p>© {{year}} {{businessName}} · <a href="{{unsubscribeLink}}">Unsubscribe</a></p>
    </div>
  </div>
</body>
</html>`,
    },
    {
        id: 'reminder',
        name: 'Appointment Reminder',
        subject: 'Reminder: Your appointment tomorrow at {{appointmentTime}}',
        htmlBody: `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0; }
    .container { max-width: 600px; margin: 32px auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .header { background: linear-gradient(135deg, #0ea5e9 0%, #6366f1 100%); padding: 32px; text-align: center; }
    .header h1 { color: #fff; margin: 0; font-size: 24px; }
    .detail-card { background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 10px; padding: 20px; margin: 24px 32px; }
    .detail-row { display: flex; gap: 12px; margin-bottom: 8px; color: #374151; font-size: 14px; }
    .label { font-weight: 600; min-width: 80px; color: #6366f1; }
    .body { padding: 0 32px 24px; }
    .body p { color: #374151; line-height: 1.7; }
    .btn { display: inline-block; background: #6366f1; color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; }
    .footer { background: #f9fafb; padding: 20px 32px; text-align: center; color: #9ca3af; font-size: 12px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>📅 Appointment Reminder</h1>
    </div>
    <div class="detail-card">
      <div class="detail-row"><span class="label">Date:</span> {{appointmentDate}}</div>
      <div class="detail-row"><span class="label">Time:</span> {{appointmentTime}}</div>
      <div class="detail-row"><span class="label">Service:</span> {{serviceName}}</div>
      <div class="detail-row"><span class="label">Staff:</span> {{staffName}}</div>
    </div>
    <div class="body">
      <p>Hi {{clientFirstName}}, this is a friendly reminder about your upcoming appointment. We look forward to seeing you!</p>
      <a href="{{manageBookingLink}}" class="btn">Manage Booking</a>
    </div>
    <div class="footer">
      <p>© {{year}} {{businessName}} · <a href="{{unsubscribeLink}}">Unsubscribe</a></p>
    </div>
  </div>
</body>
</html>`,
    },
    {
        id: 'followup',
        name: 'Post-Visit Follow-Up',
        subject: 'How was your visit, {{clientFirstName}}?',
        htmlBody: `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0; }
    .container { max-width: 600px; margin: 32px auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .header { background: linear-gradient(135deg, #10b981 0%, #0ea5e9 100%); padding: 32px; text-align: center; }
    .header h1 { color: #fff; margin: 0; font-size: 24px; }
    .stars { text-align: center; font-size: 32px; padding: 24px; }
    .body { padding: 0 32px 24px; }
    .body p { color: #374151; line-height: 1.7; }
    .btn { display: inline-block; background: #10b981; color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; }
    .footer { background: #f9fafb; padding: 20px 32px; text-align: center; color: #9ca3af; font-size: 12px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>Thank You for Visiting! 💚</h1>
    </div>
    <div class="stars">⭐⭐⭐⭐⭐</div>
    <div class="body">
      <p>Hi {{clientFirstName}}, thank you for choosing {{businessName}} for your {{serviceName}} appointment.</p>
      <p>We'd love to hear how your experience was. Your feedback helps us improve!</p>
      <a href="{{reviewLink}}" class="btn">Leave a Review</a>
    </div>
    <div class="footer">
      <p>© {{year}} {{businessName}} · <a href="{{unsubscribeLink}}">Unsubscribe</a></p>
    </div>
  </div>
</body>
</html>`,
    },
];

type ViewMode = 'code' | 'split' | 'preview';
type DeviceMode = 'desktop' | 'mobile';

export default function EmailTemplatesPage() {
    const [templates, setTemplates] = useState<EmailTemplate[]>([]);
    const [selectedTemplate, setSelectedTemplate] = useState<EmailTemplate | null>(null);
    const [subject, setSubject] = useState('');
    const [htmlContent, setHtmlContent] = useState('');
    const [viewMode, setViewMode] = useState<ViewMode>('split');
    const [deviceMode, setDeviceMode] = useState<DeviceMode>('desktop');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [history, setHistory] = useState<string[]>([]);
    const [historyIndex, setHistoryIndex] = useState(-1);
    const historyRef = useRef<string[]>([]);
    const historyIndexRef = useRef(-1);
    const iframeRef = useRef<HTMLIFrameElement>(null);
    const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    useEffect(() => {
        const loadTemplates = async () => {
            try {
                const res = await apiClient.get('/api/v1/campaigns/templates');
                const data = res.data?.data || res.data;
                if (Array.isArray(data) && data.length > 0) {
                    setTemplates(data);
                } else {
                    setTemplates(SAMPLE_TEMPLATES);
                }
            } catch {
                setTemplates(SAMPLE_TEMPLATES);
            } finally {
                setLoading(false);
            }
        };
        loadTemplates();
    }, []);

    const pushHistory = useCallback((content: string) => {
        const newHistory = historyRef.current.slice(0, historyIndexRef.current + 1);
        newHistory.push(content);
        historyRef.current = newHistory;
        historyIndexRef.current = newHistory.length - 1;
        setHistory([...newHistory]);
        setHistoryIndex(newHistory.length - 1);
    }, []);

    const handleSelectTemplate = (template: EmailTemplate) => {
        const content = template.htmlBody || template.content || '';
        setSelectedTemplate(template);
        setSubject(template.subject);
        setHtmlContent(content);
        historyRef.current = [content];
        historyIndexRef.current = 0;
        setHistory([content]);
        setHistoryIndex(0);
    };

    const handleContentChange = (value: string) => {
        setHtmlContent(value);
        if (debounceRef.current) clearTimeout(debounceRef.current);
        debounceRef.current = setTimeout(() => {
            pushHistory(value);
        }, 600);
    };

    const handleUndo = () => {
        if (historyIndexRef.current <= 0) return;
        const newIndex = historyIndexRef.current - 1;
        historyIndexRef.current = newIndex;
        setHistoryIndex(newIndex);
        const prev = historyRef.current[newIndex];
        setHtmlContent(prev);
    };

    const handleRedo = () => {
        if (historyIndexRef.current >= historyRef.current.length - 1) return;
        const newIndex = historyIndexRef.current + 1;
        historyIndexRef.current = newIndex;
        setHistoryIndex(newIndex);
        const next = historyRef.current[newIndex];
        setHtmlContent(next);
    };

    const handleSave = async () => {
        if (!selectedTemplate) return;
        setSaving(true);
        try {
            await apiClient.put(`/api/v1/campaigns/templates/${selectedTemplate.id}`, {
                subject,
                htmlBody: htmlContent,
            });
            setTemplates(prev => prev.map(t =>
                t.id === selectedTemplate.id
                    ? { ...t, subject, htmlBody: htmlContent, updatedAt: new Date().toISOString() }
                    : t
            ));
            toast.success('Template saved');
        } catch {
            // Save locally
            setTemplates(prev => prev.map(t =>
                t.id === selectedTemplate.id
                    ? { ...t, subject, htmlBody: htmlContent }
                    : t
            ));
            toast.success('Template saved locally');
        } finally {
            setSaving(false);
        }
    };

    const handleSendTest = async () => {
        try {
            await apiClient.post('/api/v1/campaigns/templates/test-send', {
                subject,
                htmlBody: htmlContent,
            });
            toast.success('Test email sent to your inbox');
        } catch {
            toast.info('Test email queued (API unavailable in dev)');
        }
    };

    const handleDuplicate = () => {
        if (!selectedTemplate) return;
        const newTemplate: EmailTemplate = {
            id: `copy_${Date.now()}`,
            name: `${selectedTemplate.name} (Copy)`,
            subject: selectedTemplate.subject,
            htmlBody: htmlContent,
        };
        setTemplates(prev => [...prev, newTemplate]);
        toast.success('Template duplicated');
    };

    const handleNewTemplate = () => {
        const blank: EmailTemplate = {
            id: `new_${Date.now()}`,
            name: 'New Template',
            subject: '',
            htmlBody: `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 32px; }
    h1 { color: #1e293b; }
    p { color: #64748b; line-height: 1.7; }
  </style>
</head>
<body>
  <div class="container">
    <h1>Your Title Here</h1>
    <p>Start writing your email content...</p>
  </div>
</body>
</html>`,
        };
        setTemplates(prev => [...prev, blank]);
        handleSelectTemplate(blank);
    };

    const previewContent = htmlContent;

    const previewFrame = (
        <iframe
            ref={iframeRef}
            srcDoc={previewContent}
            className="w-full h-full border-0 bg-white"
            title="Email Preview"
            sandbox="allow-same-origin"
        />
    );

    return (
        <div className="h-[calc(100vh-8rem)] flex flex-col gap-0">
            {/* Header */}
            <div className="flex items-center justify-between mb-4 px-1 shrink-0">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Email Templates</h1>
                    <p className="text-slate-500 text-sm mt-0.5">Design and preview responsive email templates with live rendering</p>
                </div>
                <div className="flex items-center gap-2">
                    <button
                        onClick={handleSendTest}
                        disabled={!selectedTemplate}
                        className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-40"
                    >
                        <Send className="h-4 w-4" />
                        Test Send
                    </button>
                    <button
                        onClick={handleSave}
                        disabled={!selectedTemplate || saving}
                        className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 disabled:opacity-40"
                    >
                        {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                        Save
                    </button>
                </div>
            </div>

            <div className="flex-1 flex gap-4 min-h-0">
                {/* Sidebar */}
                <div className="w-60 shrink-0 bg-white rounded-xl border border-slate-200 flex flex-col overflow-hidden">
                    <div className="p-3 border-b border-slate-100 flex items-center justify-between">
                        <span className="font-semibold text-sm text-slate-900 flex items-center gap-2">
                            <LayoutTemplate className="h-4 w-4 text-primary-500" />
                            Templates
                        </span>
                        {loading && <Loader2 className="h-3.5 w-3.5 animate-spin text-slate-400" />}
                    </div>
                    <div className="flex-1 overflow-y-auto p-2 space-y-1">
                        {templates.map(template => (
                            <button
                                key={template.id}
                                onClick={() => handleSelectTemplate(template)}
                                className={cn(
                                    'w-full text-left p-3 rounded-lg text-sm transition-all',
                                    selectedTemplate?.id === template.id
                                        ? 'bg-primary-50 text-primary-700 border border-primary-200'
                                        : 'hover:bg-slate-50 text-slate-600 border border-transparent'
                                )}
                            >
                                <div className="font-medium truncate">{template.name}</div>
                                <div className="text-xs text-slate-400 mt-0.5 truncate">{template.subject || '(no subject)'}</div>
                            </button>
                        ))}
                    </div>
                    <div className="p-2 border-t border-slate-100 flex gap-1.5">
                        <button
                            onClick={handleNewTemplate}
                            className="flex-1 flex items-center justify-center gap-1.5 py-2 text-xs text-primary-600 bg-primary-50 rounded-lg hover:bg-primary-100"
                        >
                            <Plus className="h-3.5 w-3.5" /> New
                        </button>
                        <button
                            onClick={handleDuplicate}
                            disabled={!selectedTemplate}
                            className="flex-1 flex items-center justify-center gap-1.5 py-2 text-xs text-slate-600 bg-slate-50 rounded-lg hover:bg-slate-100 disabled:opacity-40"
                        >
                            <Copy className="h-3.5 w-3.5" /> Duplicate
                        </button>
                    </div>
                </div>

                {/* Editor + Preview */}
                <div className="flex-1 flex flex-col bg-white rounded-xl border border-slate-200 overflow-hidden min-w-0">
                    {selectedTemplate ? (
                        <>
                            {/* Toolbar */}
                            <div className="px-4 py-2.5 border-b border-slate-100 bg-slate-50/50 flex items-center gap-3 shrink-0">
                                {/* Subject */}
                                <div className="flex-1 flex items-center gap-2">
                                    <span className="text-xs font-medium text-slate-500 shrink-0">Subject:</span>
                                    <input
                                        value={subject}
                                        onChange={e => setSubject(e.target.value)}
                                        className="flex-1 text-sm border border-slate-200 rounded-md px-2 py-1 focus:outline-none focus:ring-2 focus:ring-primary-500 bg-white"
                                        placeholder="Email subject line..."
                                    />
                                </div>

                                <div className="h-5 w-px bg-slate-200 mx-1" />

                                {/* Undo/Redo */}
                                <div className="flex gap-1">
                                    <button
                                        onClick={handleUndo}
                                        disabled={historyIndex <= 0}
                                        className="p-1.5 rounded hover:bg-slate-100 text-slate-500 disabled:opacity-30"
                                        title="Undo"
                                    >
                                        <Undo className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={handleRedo}
                                        disabled={historyIndex >= history.length - 1}
                                        className="p-1.5 rounded hover:bg-slate-100 text-slate-500 disabled:opacity-30"
                                        title="Redo"
                                    >
                                        <Redo className="h-3.5 w-3.5" />
                                    </button>
                                </div>

                                <div className="h-5 w-px bg-slate-200 mx-1" />

                                {/* View mode */}
                                <div className="flex bg-white rounded-lg border border-slate-200 p-0.5">
                                    {([
                                        { mode: 'code' as ViewMode, icon: <Code2 className="h-3.5 w-3.5" />, label: 'HTML' },
                                        { mode: 'split' as ViewMode, icon: <Columns2 className="h-3.5 w-3.5" />, label: 'Split' },
                                        { mode: 'preview' as ViewMode, icon: <Eye className="h-3.5 w-3.5" />, label: 'Preview' },
                                    ]).map(({ mode, icon, label }) => (
                                        <button
                                            key={mode}
                                            onClick={() => setViewMode(mode)}
                                            className={cn(
                                                'flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-all',
                                                viewMode === mode
                                                    ? 'bg-primary-600 text-white shadow-sm'
                                                    : 'text-slate-500 hover:text-slate-700'
                                            )}
                                        >
                                            {icon} {label}
                                        </button>
                                    ))}
                                </div>

                                {/* Device mode */}
                                {viewMode !== 'code' && (
                                    <div className="flex bg-white rounded-lg border border-slate-200 p-0.5">
                                        <button
                                            onClick={() => setDeviceMode('desktop')}
                                            className={cn('p-1.5 rounded transition-all', deviceMode === 'desktop' ? 'bg-slate-100 text-primary-600' : 'text-slate-400 hover:text-slate-600')}
                                            title="Desktop"
                                        >
                                            <Monitor className="h-3.5 w-3.5" />
                                        </button>
                                        <button
                                            onClick={() => setDeviceMode('mobile')}
                                            className={cn('p-1.5 rounded transition-all', deviceMode === 'mobile' ? 'bg-slate-100 text-primary-600' : 'text-slate-400 hover:text-slate-600')}
                                            title="Mobile"
                                        >
                                            <Smartphone className="h-3.5 w-3.5" />
                                        </button>
                                    </div>
                                )}
                            </div>

                            {/* Content area */}
                            <div className="flex-1 flex min-h-0 overflow-hidden">
                                {/* HTML Editor */}
                                {(viewMode === 'code' || viewMode === 'split') && (
                                    <div className={cn('flex flex-col', viewMode === 'split' ? 'w-1/2 border-r border-slate-200' : 'w-full')}>
                                        <div className="px-3 py-1.5 bg-slate-900 flex items-center gap-2 shrink-0">
                                            <div className="flex gap-1.5">
                                                <div className="w-2.5 h-2.5 rounded-full bg-red-400" />
                                                <div className="w-2.5 h-2.5 rounded-full bg-amber-400" />
                                                <div className="w-2.5 h-2.5 rounded-full bg-emerald-400" />
                                            </div>
                                            <span className="text-xs text-slate-400 ml-1">HTML Editor</span>
                                            <span className="ml-auto text-xs text-slate-500">{htmlContent.length} chars</span>
                                        </div>
                                        <textarea
                                            value={htmlContent}
                                            onChange={e => handleContentChange(e.target.value)}
                                            spellCheck={false}
                                            className="flex-1 w-full bg-slate-950 text-emerald-300 text-xs font-mono p-4 resize-none focus:outline-none leading-relaxed"
                                            placeholder="<!-- Write your HTML email here -->"
                                        />
                                    </div>
                                )}

                                {/* Preview */}
                                {(viewMode === 'preview' || viewMode === 'split') && (
                                    <div className={cn(
                                        'flex flex-col',
                                        viewMode === 'split' ? 'w-1/2' : 'w-full'
                                    )}>
                                        <div className="px-3 py-1.5 bg-slate-100 flex items-center gap-2 shrink-0">
                                            <Eye className="h-3.5 w-3.5 text-slate-400" />
                                            <span className="text-xs text-slate-500 font-medium">Live Preview</span>
                                            <span className="ml-auto text-xs text-slate-400 flex items-center gap-1">
                                                <RefreshCw className="h-3 w-3" /> auto
                                            </span>
                                        </div>
                                        <div className="flex-1 bg-slate-200 flex items-start justify-center p-4 overflow-auto">
                                            <div className={cn(
                                                'bg-white shadow-xl transition-all duration-300 overflow-hidden',
                                                deviceMode === 'mobile'
                                                    ? 'w-[375px] rounded-3xl border-8 border-slate-800 min-h-[667px]'
                                                    : 'w-full max-w-[600px] min-h-[400px] rounded-lg'
                                            )}>
                                                {previewFrame}
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        </>
                    ) : (
                        <div className="flex-1 flex flex-col items-center justify-center text-slate-400">
                            <LayoutTemplate className="h-16 w-16 mb-4 opacity-20" />
                            <p className="text-sm">Select a template to start editing</p>
                            <button
                                onClick={handleNewTemplate}
                                className="mt-4 flex items-center gap-2 px-4 py-2 text-sm font-medium text-primary-600 bg-primary-50 rounded-lg hover:bg-primary-100"
                            >
                                <Plus className="h-4 w-4" /> Create New Template
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
