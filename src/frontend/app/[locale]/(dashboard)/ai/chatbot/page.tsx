'use client';

import { useState, useEffect } from 'react';
import {
    Sparkles,
    MessageSquare,
    Settings,
    ShieldCheck,
    Bot,
    Plus,
    Trash2,
    RefreshCcw,
    AlertCircle,
    CheckCircle2,
    Users,
    Activity
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Textarea } from '@/components/ui/Textarea';
import { Badge } from '@/components/ui/Badge';
import { useToast } from '@/components/ui/Toast';

export default function ChatbotAdminPage() {
    const { success, error } = useToast();
    const [loading, setLoading] = useState(true);
    const [settings, setSettings] = useState<any>({
        isEnabled: true,
        botName: 'Upkilo Assistant',
        handoffEmail: '',
        welcomeMessage: 'Hello! How can I help you today?'
    });
    const [kbItems, setKbItems] = useState<any[]>([]);
    const [newKb, setNewKb] = useState({ category: 'General', question: '', answer: '' });
    const [stats, setStats] = useState({
        totalConversations: 0,
        resolutionRate: 0,
        activeHandoffs: 0
    });

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [settingsRes, kbRes, statsRes] = await Promise.all([
                api.chatbot.getSettings(),
                api.chatbot.getKnowledgeBase(),
                api.chatbot.getStats().catch(() => ({ data: null })),
            ]);
            setSettings(settingsRes.data);
            setKbItems(kbRes.data);
            if (statsRes.data) {
                setStats({
                    totalConversations: statsRes.data.totalConversations ?? statsRes.data.totalChats ?? 0,
                    resolutionRate: statsRes.data.resolutionRate ?? 0,
                    activeHandoffs: statsRes.data.activeHandoffs ?? statsRes.data.liveHandoffs ?? 0,
                });
            }
        } catch (err) {
            console.error('Failed to fetch chatbot data', err);
            setKbItems([]);
        } finally {
            setLoading(false);
        }
    };

    const handleToggleBot = async () => {
        const updated = { ...settings, isEnabled: !settings.isEnabled };
        setSettings(updated);
        try {
            await api.chatbot.updateSettings(updated);
            success(updated.isEnabled ? 'Chatbot enabled' : 'Chatbot disabled');
        } catch (err) {
            error('Failed to update settings');
        }
    };

    const handleAddKb = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const res = await api.chatbot.addKnowledgeBase(newKb);
            setKbItems([...kbItems, res.data]);
            setNewKb({ category: 'General', question: '', answer: '' });
            success('Knowledge base updated');
        } catch (err) {
            error('Failed to add KB entry');
        }
    };

    const handleDeleteKb = async (id: string) => {
        try {
            await api.chatbot.deleteKnowledgeBase(id);
            setKbItems(kbItems.filter(item => item.id !== id));
            success('Entry removed');
        } catch (err) {
            error('Failed to delete');
        }
    };

    if (loading) {
        return <div className="p-8 text-center">Loading AI Chatbot configuration...</div>;
    }

    return (
        <div className="space-y-8 animate-fade-in">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl shadow-lg shadow-indigo-500/25">
                            <Bot className="h-6 w-6 text-white" />
                        </div>
                        <h1 className="text-2xl lg:text-3xl font-bold text-slate-900">AI Chatbot Management</h1>
                    </div>
                    <p className="text-slate-500">Train and monitor your 24/7 AI appointment assistant</p>
                </div>
                <div className="flex items-center gap-3 bg-white p-2 rounded-xl border border-slate-200 shadow-sm">
                    <span className="text-sm font-medium text-slate-600 ml-2">Status:</span>
                    <Badge variant={settings.isEnabled ? 'success' : 'outline'}>
                        {settings.isEnabled ? 'Active' : 'Disabled'}
                    </Badge>
                    <Button variant="ghost" size="sm" onClick={handleToggleBot}>
                        <RefreshCcw className="h-4 w-4 mr-2" />
                        Toggle
                    </Button>
                </div>
            </div>

            {/* Stats Row */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <Card className="p-6 flex items-center gap-4">
                    <div className="p-3 bg-blue-50 text-blue-600 rounded-xl">
                        <MessageSquare className="h-6 w-6" />
                    </div>
                    <div>
                        <p className="text-sm text-slate-500">Total Chats</p>
                        <p className="text-2xl font-bold">{stats.totalConversations}</p>
                    </div>
                </Card>
                <Card className="p-6 flex items-center gap-4">
                    <div className="p-3 bg-emerald-50 text-emerald-600 rounded-xl">
                        <Activity className="h-6 w-6" />
                    </div>
                    <div>
                        <p className="text-sm text-slate-500">Resolution Rate</p>
                        <p className="text-2xl font-bold">{stats.resolutionRate}%</p>
                    </div>
                </Card>
                <Card className="p-6 flex items-center gap-4">
                    <div className="p-3 bg-amber-50 text-amber-600 rounded-xl">
                        <Users className="h-6 w-6" />
                    </div>
                    <div>
                        <p className="text-sm text-slate-500">Live Handoffs</p>
                        <p className="text-2xl font-bold">{stats.activeHandoffs}</p>
                    </div>
                </Card>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Configuration */}
                <Card className="lg:col-span-1 p-6 space-y-6 self-start">
                    <div className="flex items-center gap-2 mb-2">
                        <Settings className="h-5 w-5 text-indigo-500" />
                        <h2 className="text-lg font-bold">Bot Persona</h2>
                    </div>

                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Assistant Name</Label>
                            <Input
                                value={settings.botName}
                                onChange={(e) => setSettings({ ...settings, botName: e.target.value })}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>Welcome Message</Label>
                            <Textarea
                                value={settings.welcomeMessage}
                                onChange={(e) => setSettings({ ...settings, welcomeMessage: e.target.value })}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>Human Handoff Email</Label>
                            <Input
                                type="email"
                                placeholder="support@yourcompany.com"
                                value={settings.handoffEmail}
                                onChange={(e) => setSettings({ ...settings, handoffEmail: e.target.value })}
                            />
                        </div>
                        <Button className="w-full" onClick={() => success('Settings saved')}>
                            Save Configuration
                        </Button>
                    </div>
                </Card>

                {/* Knowledge Base */}
                <Card className="lg:col-span-2 p-6">
                    <div className="flex items-center justify-between mb-6">
                        <div className="flex items-center gap-2">
                            <ShieldCheck className="h-5 w-5 text-emerald-500" />
                            <h2 className="text-lg font-bold">Knowledge Base & Training</h2>
                        </div>
                    </div>

                    {/* Training Form */}
                    <form onSubmit={handleAddKb} className="bg-slate-50 p-4 rounded-xl border border-slate-100 mb-8 space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label>Question / Trigger</Label>
                                <Input
                                    placeholder="e.g. What are your opening hours?"
                                    value={newKb.question}
                                    onChange={(e) => setNewKb({ ...newKb, question: e.target.value })}
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label>Category</Label>
                                <Input
                                    placeholder="General"
                                    value={newKb.category}
                                    onChange={(e) => setNewKb({ ...newKb, category: e.target.value })}
                                />
                            </div>
                        </div>
                        <div className="space-y-2">
                            <Label>AI Response</Label>
                            <Textarea
                                placeholder="Describe the answer exactly as the AI should provide it..."
                                value={newKb.answer}
                                onChange={(e) => setNewKb({ ...newKb, answer: e.target.value })}
                                required
                            />
                        </div>
                        <Button type="submit" variant="secondary" className="w-full md:w-auto">
                            <Plus className="h-4 w-4 mr-2" />
                            Add to Knowledge Base
                        </Button>
                    </form>

                    {/* KB Grid */}
                    <div className="space-y-4">
                        <h3 className="font-semibold text-sm text-slate-500 uppercase tracking-wider">Trained Responses</h3>
                        <div className="grid gap-4">
                            {kbItems.map((item) => (
                                <div key={item.id} className="group flex items-start gap-4 p-4 bg-white border border-slate-100 rounded-xl hover:shadow-md transition-all">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2 mb-1">
                                            <Badge variant="outline" className="bg-slate-50">{item.category}</Badge>
                                            <p className="font-semibold text-slate-900">{item.question}</p>
                                        </div>
                                        <p className="text-sm text-slate-600 leading-relaxed italic">"{item.answer}"</p>
                                    </div>
                                    <button
                                        onClick={() => handleDeleteKb(item.id)}
                                        className="opacity-0 group-hover:opacity-100 p-2 text-slate-400 hover:text-red-500 transition-all"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>
                </Card>
            </div>
        </div>
    );
}
