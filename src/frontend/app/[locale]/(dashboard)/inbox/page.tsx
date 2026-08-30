"use client";

import React, { useState, useEffect, useRef } from 'react';
import {
    MessageSquare, Mail, Globe, Search, Send, Filter,
    RefreshCw, CheckCheck, Circle, Loader2, Star,
    Archive, Trash2, Reply, Phone, User, ChevronDown,
    Tag, Clock, Inbox, Plus
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

type Channel = 'sms' | 'email' | 'whatsapp' | 'instagram' | 'facebook' | 'web';
type ConvStatus = 'open' | 'resolved' | 'snoozed' | 'spam';

interface Message {
    id: string;
    from: 'client' | 'agent' | 'ai';
    content: string;
    timestamp: string;
    read: boolean;
}

interface Conversation {
    id: string;
    channel: Channel;
    clientName: string;
    clientPhone?: string;
    clientEmail?: string;
    status: ConvStatus;
    unreadCount: number;
    lastMessage: string;
    lastActivity: string;
    starred: boolean;
    tags: string[];
    messages: Message[];
    assignedTo?: string;
}

const CHANNEL_CONFIG: Record<Channel, { icon: React.ReactNode; label: string; color: string; bg: string }> = {
    sms: { icon: <MessageSquare className="h-4 w-4" />, label: 'SMS', color: 'text-emerald-600 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800/50' },
    email: { icon: <Mail className="h-4 w-4" />, label: 'Email', color: 'text-blue-600 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800/50' },
    whatsapp: { icon: <MessageSquare className="h-4 w-4" />, label: 'WhatsApp', color: 'text-green-600 dark:text-green-400', bg: 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800/50' },
    instagram: { icon: <Globe className="h-4 w-4" />, label: 'Instagram', color: 'text-pink-600 dark:text-pink-400', bg: 'bg-pink-50 dark:bg-pink-900/20 border-pink-200 dark:border-pink-800/50' },
    facebook: { icon: <Globe className="h-4 w-4" />, label: 'Facebook', color: 'text-blue-700 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800/50' },
    web: { icon: <Globe className="h-4 w-4" />, label: 'Web Chat', color: 'text-indigo-600 dark:text-indigo-400', bg: 'bg-indigo-50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-800/50' },
};

const SAMPLE_CONVERSATIONS: Conversation[] = [
    {
        id: 'c1', channel: 'sms', clientName: 'Sarah Mitchell', clientPhone: '+1 (555) 234-5678',
        status: 'open', unreadCount: 3, starred: true, tags: ['VIP', 'Rebooking'],
        lastMessage: 'Can I reschedule my appointment to Friday?',
        lastActivity: new Date(Date.now() - 300000).toISOString(),
        messages: [
            { id: 'm1', from: 'client', content: 'Hi! I need to reschedule my appointment', timestamp: new Date(Date.now() - 600000).toISOString(), read: true },
            { id: 'm2', from: 'agent', content: 'Hi Sarah! Of course, what day works for you?', timestamp: new Date(Date.now() - 500000).toISOString(), read: true },
            { id: 'm3', from: 'client', content: 'Can I reschedule my appointment to Friday?', timestamp: new Date(Date.now() - 300000).toISOString(), read: false },
        ],
    },
    {
        id: 'c2', channel: 'email', clientName: 'James Rodriguez', clientEmail: 'james@example.com',
        status: 'open', unreadCount: 1, starred: false, tags: ['New Client'],
        lastMessage: 'What are your prices for balayage?',
        lastActivity: new Date(Date.now() - 1800000).toISOString(),
        messages: [
            { id: 'm4', from: 'client', content: 'Hello, I am interested in getting balayage done. What are your prices for balayage?', timestamp: new Date(Date.now() - 1800000).toISOString(), read: false },
        ],
    },
    {
        id: 'c3', channel: 'whatsapp', clientName: 'Emma Thompson', clientPhone: '+1 (555) 987-6543',
        status: 'open', unreadCount: 2, starred: false, tags: [],
        lastMessage: 'I want to book a facial and manicure together',
        lastActivity: new Date(Date.now() - 3600000).toISOString(),
        messages: [
            { id: 'm5', from: 'client', content: 'Hey! Do you offer combo packages?', timestamp: new Date(Date.now() - 4000000).toISOString(), read: true },
            { id: 'm6', from: 'ai', content: 'Hi Emma! Yes, we offer combo packages. What services are you interested in?', timestamp: new Date(Date.now() - 3900000).toISOString(), read: true },
            { id: 'm7', from: 'client', content: 'I want to book a facial and manicure together', timestamp: new Date(Date.now() - 3600000).toISOString(), read: false },
        ],
    },
    {
        id: 'c4', channel: 'instagram', clientName: 'Mia Chen',
        status: 'resolved', unreadCount: 0, starred: true, tags: ['Follow-up'],
        lastMessage: 'Thank you so much, see you Thursday!',
        lastActivity: new Date(Date.now() - 86400000).toISOString(),
        messages: [
            { id: 'm8', from: 'client', content: 'Love your Instagram! Do you take walk-ins?', timestamp: new Date(Date.now() - 90000000).toISOString(), read: true },
            { id: 'm9', from: 'agent', content: 'Thank you! We prefer appointments but can often accommodate walk-ins. Would you like to book?', timestamp: new Date(Date.now() - 89000000).toISOString(), read: true },
            { id: 'm10', from: 'client', content: 'Thank you so much, see you Thursday!', timestamp: new Date(Date.now() - 86400000).toISOString(), read: true },
        ],
    },
    {
        id: 'c5', channel: 'facebook', clientName: 'David Park',
        status: 'open', unreadCount: 0, starred: false, tags: ['Complaint'],
        lastMessage: 'I had an issue with my last visit',
        lastActivity: new Date(Date.now() - 172800000).toISOString(),
        messages: [
            { id: 'm11', from: 'client', content: 'I had an issue with my last visit and I would like to speak to a manager', timestamp: new Date(Date.now() - 172800000).toISOString(), read: true },
        ],
    },
];

export default function MultiChannelInboxPage() {
    const [conversations, setConversations] = useState<Conversation[]>(SAMPLE_CONVERSATIONS);
    const [selectedId, setSelectedId] = useState<string | null>(null);
    const [search, setSearch] = useState('');
    const [channelFilter, setChannelFilter] = useState<Channel | 'all'>('all');
    const [statusFilter, setStatusFilter] = useState<ConvStatus | 'all'>('open');
    const [replyText, setReplyText] = useState('');
    const [sending, setSending] = useState(false);
    const bottomRef = useRef<HTMLDivElement>(null);

    const selected = conversations.find(c => c.id === selectedId);

    useEffect(() => {
        if (selectedId) {
            setConversations(prev => prev.map(c =>
                c.id === selectedId ? { ...c, unreadCount: 0, messages: c.messages.map(m => ({ ...m, read: true })) } : c
            ));
        }
    }, [selectedId]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [selected?.messages.length]);

    const filteredConvs = conversations.filter(c => {
        if (channelFilter !== 'all' && c.channel !== channelFilter) return false;
        if (statusFilter !== 'all' && c.status !== statusFilter) return false;
        if (search && !c.clientName.toLowerCase().includes(search.toLowerCase()) && !c.lastMessage.toLowerCase().includes(search.toLowerCase())) return false;
        return true;
    });

    const handleSend = async () => {
        if (!replyText.trim() || !selectedId) return;
        setSending(true);
        const newMsg: Message = {
            id: `msg-${Date.now()}`,
            from: 'agent',
            content: replyText,
            timestamp: new Date().toISOString(),
            read: true,
        };
        setConversations(prev => prev.map(c =>
            c.id === selectedId
                ? { ...c, messages: [...c.messages, newMsg], lastMessage: replyText, lastActivity: new Date().toISOString() }
                : c
        ));
        setReplyText('');
        setSending(false);
        toast.success('Message sent');
    };

    const handleResolve = (id: string) => {
        setConversations(prev => prev.map(c => c.id === id ? { ...c, status: 'resolved' } : c));
        toast.success('Conversation resolved');
    };

    const handleStar = (id: string) => {
        setConversations(prev => prev.map(c => c.id === id ? { ...c, starred: !c.starred } : c));
    };

    const totalUnread = conversations.reduce((s, c) => s + c.unreadCount, 0);

    return (
        <div className="h-[calc(100vh-8rem)] flex gap-0 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-xl">
            {/* Left: Conversation list */}
            <div className="w-80 shrink-0 border-r border-slate-200 dark:border-slate-800 flex flex-col bg-white dark:bg-slate-900">
                {/* Header */}
                <div className="p-4 border-b border-slate-100 dark:border-slate-800">
                    <div className="flex items-center justify-between mb-3">
                        <h2 className="font-bold text-slate-900 dark:text-white flex items-center gap-2">
                            <Inbox className="h-5 w-5 text-primary-500" />
                            Inbox
                            {totalUnread > 0 && (
                                <span className="ml-1 bg-primary-600 text-white text-xs rounded-full px-2 py-0.5">{totalUnread}</span>
                            )}
                        </h2>
                        <button className="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-foreground-muted transition-colors">
                            <RefreshCw className="h-4 w-4" />
                        </button>
                    </div>
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-foreground-muted" />
                        <input
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                            placeholder="Search conversations..."
                            className="w-full pl-9 pr-3 py-2 text-sm border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 transition-shadow"
                        />
                    </div>
                </div>

                {/* Channel filters */}
                <div className="px-3 py-2 border-b border-slate-100 dark:border-slate-800 flex gap-1 overflow-x-auto scrollbar-hide">
                    <button
                        onClick={() => setChannelFilter('all')}
                        className={cn('shrink-0 px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider transition-all', channelFilter === 'all' ? 'bg-primary-600 text-white shadow-md' : 'text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800')}
                    >
                        All
                    </button>
                    {(Object.keys(CHANNEL_CONFIG) as Channel[]).map(ch => (
                        <button
                            key={ch}
                            onClick={() => setChannelFilter(ch)}
                            className={cn('shrink-0 flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider transition-all', channelFilter === ch ? 'bg-primary-600 text-white shadow-md' : 'text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800')}
                        >
                            <span className={cn('transition-colors', channelFilter === ch ? 'text-white' : CHANNEL_CONFIG[ch].color)}>{CHANNEL_CONFIG[ch].icon}</span> {CHANNEL_CONFIG[ch].label}
                        </button>
                    ))}
                </div>

                {/* Status tabs */}
                <div className="px-3 py-1.5 border-b border-slate-100 dark:border-slate-800 flex gap-1 bg-slate-50/50 dark:bg-slate-800/20">
                    {(['open', 'resolved', 'all'] as const).map(s => (
                        <button
                            key={s}
                            onClick={() => setStatusFilter(s)}
                            className={cn('px-3 py-1 rounded-lg text-xs font-bold uppercase transition-all', statusFilter === s ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-white shadow-sm border border-slate-200 dark:border-slate-600' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200')}
                        >
                            {s}
                        </button>
                    ))}
                </div>

                {/* List */}
                <div className="flex-1 overflow-y-auto">
                    {filteredConvs.length === 0 ? (
                        <div className="text-center py-12 text-foreground-muted">
                            <MessageSquare className="h-8 w-8 mx-auto mb-2 opacity-30" />
                            <p className="text-sm">No conversations</p>
                        </div>
                    ) : (
                        filteredConvs.map(conv => {
                            const ch = CHANNEL_CONFIG[conv.channel];
                            return (
                                <button
                                    key={conv.id}
                                    onClick={() => setSelectedId(conv.id)}
                                    className={cn(
                                        'w-full text-left px-4 py-3 border-b border-slate-50 dark:border-slate-800/50 transition-all hover:bg-slate-50 dark:hover:bg-slate-800/50',
                                        selectedId === conv.id && 'bg-primary-50/70 dark:bg-primary-900/20 border-l-2 border-l-primary-500'
                                    )}
                                >
                                    <div className="flex items-start gap-2.5">
                                        <div className={cn('p-1.5 rounded-lg border shrink-0 transition-colors', ch.bg)}>
                                            <span className={ch.color}>{ch.icon}</span>
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center justify-between">
                                                <span className={cn('text-sm font-bold truncate', conv.unreadCount > 0 ? 'text-slate-900 dark:text-white' : 'text-slate-700 dark:text-slate-300')}>
                                                    {conv.clientName}
                                                </span>
                                                <div className="flex items-center gap-1">
                                                    {conv.starred && <Star className="h-3 w-3 text-amber-400 fill-amber-400" />}
                                                    {conv.unreadCount > 0 && (
                                                        <span className="bg-primary-600 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center font-bold">
                                                            {conv.unreadCount}
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                            <p className={cn('text-xs truncate mt-0.5', conv.unreadCount > 0 ? 'text-slate-700 dark:text-slate-300 font-medium' : 'text-foreground-secondary')}>{conv.lastMessage}</p>
                                            <div className="flex items-center gap-2 mt-2">
                                                <span className="text-[10px] font-bold text-foreground-muted uppercase tracking-tighter">{formatTime(conv.lastActivity)}</span>
                                                {conv.tags.map(tag => (
                                                    <span key={tag} className="text-[10px] font-bold px-1.5 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 rounded-md border border-slate-200 dark:border-slate-700">{tag}</span>
                                                ))}
                                            </div>
                                        </div>
                                    </div>
                                </button>
                            );
                        })
                    )}
                </div>
            </div>

            {/* Right: Conversation detail */}
            {selected ? (
                <div className="flex-1 flex flex-col min-w-0 bg-white dark:bg-slate-900">
                    {/* Conversation Header */}
                    <div className="px-5 py-3 border-b border-slate-100 dark:border-slate-800 flex items-center gap-3 bg-white dark:bg-slate-900 shadow-sm z-10">
                        <div className={cn('p-2 rounded-xl border', CHANNEL_CONFIG[selected.channel].bg)}>
                            <span className={cn('transition-colors', CHANNEL_CONFIG[selected.channel].color)}>{CHANNEL_CONFIG[selected.channel].icon}</span>
                        </div>
                        <div className="flex-1">
                            <p className="font-bold text-slate-900 dark:text-white leading-tight">{selected.clientName}</p>
                            <p className="text-[10px] font-bold text-slate-300 uppercase tracking-widest mt-0.5">
                                via {CHANNEL_CONFIG[selected.channel].label}
                                {selected.clientPhone && ` · ${selected.clientPhone}`}
                                {selected.clientEmail && ` · ${selected.clientEmail}`}
                            </p>
                        </div>
                        <div className="flex items-center gap-1.5">
                            <button onClick={() => handleStar(selected.id)} className="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                                <Star className={cn('h-4 w-4 transition-colors', selected.starred ? 'text-amber-400 fill-amber-400' : 'text-foreground-muted')} />
                            </button>
                            {selected.status === 'open' && (
                                <button onClick={() => handleResolve(selected.id)} className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-900/30 border border-emerald-100 dark:border-emerald-800/50 rounded-lg hover:bg-emerald-100 dark:hover:bg-emerald-900/40 transition-all active:scale-95 shadow-sm">
                                    <CheckCheck className="h-3.5 w-3.5" /> Resolve
                                </button>
                            )}
                        </div>
                    </div>

                    {/* Messages */}
                    <div className="flex-1 overflow-y-auto p-5 space-y-4 bg-slate-50 dark:bg-slate-950/20">
                        {selected.messages.map(msg => (
                            <div key={msg.id} className={cn('flex gap-3', msg.from !== 'client' ? 'flex-row-reverse' : '')}>
                                <div className={cn('w-8 h-8 rounded-xl shrink-0 flex items-center justify-center text-[10px] font-bold shadow-sm',
                                    msg.from === 'client' ? 'bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700' :
                                    msg.from === 'ai' ? 'bg-gradient-to-br from-primary-500 to-primary-600 text-white' :
                                    'bg-primary-600 text-white'
                                )}>
                                    {msg.from === 'client' ? <User className="h-4 w-4" /> : msg.from === 'ai' ? 'AI' : 'A'}
                                </div>
                                <div className={cn('max-w-[75%] flex flex-col', msg.from !== 'client' ? 'items-end' : '')}>
                                    <div className={cn('px-4 py-2.5 rounded-2xl text-sm leading-relaxed shadow-sm',
                                        msg.from === 'client' 
                                            ? 'bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-800 dark:text-slate-200 rounded-tl-none' 
                                            : msg.from === 'ai' 
                                                ? 'bg-primary-50/80 dark:bg-primary-900/30 border border-primary-200 dark:border-primary-800/50 text-slate-800 dark:text-slate-100 rounded-tr-none' 
                                                : 'bg-primary-600 text-white rounded-tr-none'
                                    )}>
                                        {msg.from === 'ai' && <span className="text-[10px] font-bold text-primary-500 dark:text-primary-400 uppercase tracking-widest block mb-1">🤖 AI Agent</span>}
                                        {msg.content}
                                    </div>
                                    <p className="text-[10px] font-bold text-foreground-muted mt-1.5 px-1 uppercase tracking-tighter">{new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
                                </div>
                            </div>
                        ))}
                        <div ref={bottomRef} />
                    </div>

                    {/* Reply Box */}
                    {selected.status === 'open' && (
                        <div className="p-4 border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-[0_-1px_10px_rgba(0,0,0,0.05)]">
                            <div className="flex items-center gap-2 mb-3">
                                <span className={cn('text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-md', CHANNEL_CONFIG[selected.channel].bg, CHANNEL_CONFIG[selected.channel].color)}>
                                    Replying via {CHANNEL_CONFIG[selected.channel].label}
                                </span>
                            </div>
                            <div className="flex gap-3">
                                <textarea
                                    value={replyText}
                                    onChange={e => setReplyText(e.target.value)}
                                    onKeyDown={e => e.key === 'Enter' && !e.shiftKey && (e.preventDefault(), handleSend())}
                                    placeholder="Type your reply..."
                                    className="flex-1 border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none h-14 transition-all shadow-inner dark:text-white"
                                />
                                <button
                                    onClick={handleSend}
                                    disabled={!replyText.trim() || sending}
                                    className="px-5 py-2 bg-primary-600 text-white rounded-xl hover:bg-primary-700 disabled:opacity-40 flex items-center gap-2 text-sm font-bold shadow-lg shadow-primary-500/20 active:scale-95 transition-all"
                                >
                                    {sending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                                    Send
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            ) : (
                <div className="flex-1 flex flex-col items-center justify-center text-foreground-muted bg-slate-50 dark:bg-slate-950/20">
                    <div className="w-20 h-20 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl flex items-center justify-center mb-4 shadow-sm">
                        <Inbox className="h-10 w-10 opacity-20" />
                    </div>
                    <p className="text-sm font-medium tracking-tight">Select a conversation to view your message history</p>
                </div>
            )}
        </div>
    );
}

function formatTime(isoString: string): string {
    const d = new Date(isoString);
    const diff = Date.now() - d.getTime();
    if (diff < 60000) return 'Just now';
    if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;
    return d.toLocaleDateString();
}
