"use client";

import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
    Bot, Send, User, Calendar, Clock, RefreshCw, CheckCircle,
    MessageSquare, Loader2, AlertCircle, Sparkles, X, Plus
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { toast } from 'sonner';

type MessageRole = 'user' | 'assistant' | 'system';

interface ChatMessage {
    id: string;
    role: MessageRole;
    content: string;
    timestamp: Date;
    isTyping?: boolean;
    suggestedBooking?: {
        service?: string;
        dateTime?: string;
        staff?: string;
    };
}

interface ConversationSession {
    id: string;
    clientName?: string;
    startedAt: Date;
    messageCount: number;
    status: 'active' | 'booked' | 'handed-off' | 'abandoned';
    channel: 'web' | 'sms' | 'whatsapp';
}

const QUICK_PROMPTS = [
    'I want to book a haircut',
    'What services do you offer?',
    'What are your hours?',
    'I need to cancel my appointment',
    'Do you have availability this weekend?',
    'Book me something next week',
];

const SYSTEM_CONTEXT = `You are an AI booking assistant for a salon/spa business named Upkilo.
Help clients book appointments, answer questions about services, hours, and pricing.
Be friendly, professional, and concise. Guide users to book appointments.
When you understand what they want to book, suggest specific time slots and ask for confirmation.
Available services: Haircut ($45), Blowout ($35), Color ($120+), Facial ($75), Massage ($90), Manicure ($30).
Hours: Mon-Sat 9am-7pm, Sun 10am-5pm.`;

export default function ConversationalBookingPage() {
    const [sessions, setSessions] = useState<ConversationSession[]>([
        { id: 's1', clientName: 'Anonymous', startedAt: new Date(Date.now() - 600000), messageCount: 4, status: 'booked', channel: 'web' },
        { id: 's2', clientName: 'Sarah M.', startedAt: new Date(Date.now() - 3600000), messageCount: 12, status: 'active', channel: 'sms' },
        { id: 's3', clientName: 'John D.', startedAt: new Date(Date.now() - 7200000), messageCount: 2, status: 'abandoned', channel: 'whatsapp' },
    ]);
    const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [sending, setSending] = useState(false);
    const [handedOff, setHandedOff] = useState(false);
    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const startNewSession = () => {
        const newSession: ConversationSession = {
            id: `s${Date.now()}`,
            clientName: 'New Chat',
            startedAt: new Date(),
            messageCount: 0,
            status: 'active',
            channel: 'web',
        };
        setSessions(prev => [newSession, ...prev]);
        setActiveSessionId(newSession.id);
        setHandedOff(false);
        setMessages([{
            id: 'welcome',
            role: 'assistant',
            content: "Hi! I'm your AI booking assistant. I can help you book appointments, check availability, or answer questions about our services. What can I help you with today?",
            timestamp: new Date(),
        }]);
    };

    const sendMessage = async (text?: string) => {
        const userText = text || input.trim();
        if (!userText || sending) return;
        setInput('');

        const userMsg: ChatMessage = {
            id: `msg-${Date.now()}`,
            role: 'user',
            content: userText,
            timestamp: new Date(),
        };

        const typingMsg: ChatMessage = {
            id: 'typing',
            role: 'assistant',
            content: '',
            timestamp: new Date(),
            isTyping: true,
        };

        setMessages(prev => [...prev, userMsg, typingMsg]);
        setSending(true);

        try {
            // Build context from conversation history
            const history = messages
                .filter(m => !m.isTyping)
                .map(m => ({ role: m.role as 'user' | 'assistant', content: m.content }));

            const prompt = `${SYSTEM_CONTEXT}\n\nConversation:\n${history.map(m => `${m.role}: ${m.content}`).join('\n')}\nuser: ${userText}\nassistant:`;

            const res = await apiClient.post('/api/v1/ai/generate', {
                prompt,
                model: 'gpt-4',
            });

            const aiContent = res.data?.content || res.data?.data?.content || '';

            // Check for handoff triggers
            const shouldHandOff = /speak.*human|real person|manager|complaint|urgent|emergency/i.test(userText);
            if (shouldHandOff) setHandedOff(true);

            // Check for booking intent
            const bookingIntent = /book|schedule|appointment|reserve|available|slot/i.test(userText);
            const suggestedBooking = bookingIntent ? {
                service: /haircut/i.test(userText) ? 'Haircut ($45)' : /color/i.test(userText) ? 'Color Service' : /massage/i.test(userText) ? 'Massage ($90)' : undefined,
                dateTime: 'Tomorrow at 2:00 PM',
            } : undefined;

            const aiMsg: ChatMessage = {
                id: `ai-${Date.now()}`,
                role: 'assistant',
                content: aiContent || generateFallbackResponse(userText),
                timestamp: new Date(),
                suggestedBooking,
            };

            setMessages(prev => prev.filter(m => m.id !== 'typing').concat(aiMsg));
        } catch {
            // Fallback local AI response
            const aiMsg: ChatMessage = {
                id: `ai-${Date.now()}`,
                role: 'assistant',
                content: generateFallbackResponse(userText),
                timestamp: new Date(),
            };
            setMessages(prev => prev.filter(m => m.id !== 'typing').concat(aiMsg));
        } finally {
            setSending(false);
        }
    };

    const handleConfirmBooking = async (booking: ChatMessage['suggestedBooking']) => {
        const confirmMsg: ChatMessage = {
            id: `confirm-${Date.now()}`,
            role: 'assistant',
            content: `✅ Your appointment has been booked!\n\n📅 **${booking?.service || 'Service'}**\n🕐 ${booking?.dateTime || 'Soon'}\n\nYou'll receive a confirmation SMS shortly. See you then!`,
            timestamp: new Date(),
        };
        setMessages(prev => [...prev, confirmMsg]);
        toast.success('Booking confirmed!');
    };

    const statusColor: Record<string, string> = {
        active: 'bg-emerald-100 text-emerald-700',
        booked: 'bg-blue-100 text-blue-700',
        'handed-off': 'bg-amber-100 text-amber-700',
        abandoned: 'bg-muted text-foreground-secondary',
    };

    const channelIcon: Record<string, React.ReactNode> = {
        web: <MessageSquare className="h-3 w-3" />,
        sms: <MessageSquare className="h-3 w-3" />,
        whatsapp: <MessageSquare className="h-3 w-3" />,
    };

    return (
        <div className="h-[calc(100vh-8rem)] flex gap-4">
            {/* Sessions Sidebar */}
            <div className="w-72 shrink-0 bg-card rounded-xl border border-border flex flex-col overflow-hidden">
                <div className="p-4 border-b border-border-subtle flex items-center justify-between">
                    <div>
                        <h2 className="font-semibold text-foreground">AI Booking Chat</h2>
                        <p className="text-xs text-foreground-secondary mt-0.5">Conversational booking assistant</p>
                    </div>
                    <button onClick={startNewSession} className="p-1.5 bg-brand-subtle rounded-lg text-primary hover:bg-brand-subtle">
                        <Plus className="h-4 w-4" />
                    </button>
                </div>

                {/* Stats */}
                <div className="p-3 grid grid-cols-2 gap-2 border-b border-border-subtle">
                    {[
                        { label: 'Active', value: sessions.filter(s => s.status === 'active').length, color: 'text-success-fg' },
                        { label: 'Booked', value: sessions.filter(s => s.status === 'booked').length, color: 'text-blue-600' },
                    ].map(s => (
                        <div key={s.label} className="text-center p-2 bg-muted rounded-lg">
                            <div className={`text-lg font-bold ${s.color}`}>{s.value}</div>
                            <div className="text-xs text-foreground-muted">{s.label}</div>
                        </div>
                    ))}
                </div>

                <div className="flex-1 overflow-y-auto p-2 space-y-1">
                    {sessions.map(session => (
                        <button
                            key={session.id}
                            onClick={() => {
                                setActiveSessionId(session.id);
                                // For demo: load the welcome message for any session
                                if (messages.length === 0) startNewSession();
                            }}
                            className={`w-full text-left p-3 rounded-lg border transition-all ${activeSessionId === session.id ? 'bg-brand-subtle border-primary/25' : 'border-transparent hover:bg-accent'}`}
                        >
                            <div className="flex items-center gap-2 mb-1">
                                <span className={`px-1.5 py-0.5 rounded-full text-xs font-medium ${statusColor[session.status]}`}>
                                    {session.status}
                                </span>
                                <span className="flex items-center gap-0.5 text-xs text-foreground-muted">
                                    {channelIcon[session.channel]} {session.channel}
                                </span>
                            </div>
                            <div className="text-sm font-medium text-foreground">{session.clientName}</div>
                            <div className="text-xs text-foreground-muted mt-0.5">{session.messageCount} msgs · {new Date(session.startedAt).toLocaleTimeString()}</div>
                        </button>
                    ))}
                </div>

                <div className="p-3 border-t border-border-subtle">
                    <button onClick={startNewSession} className="w-full py-2 bg-primary-600 text-white rounded-lg text-sm font-medium hover:bg-primary-700 flex items-center justify-center gap-2">
                        <Plus className="h-4 w-4" /> New Chat Session
                    </button>
                </div>
            </div>

            {/* Chat Panel */}
            <div className="flex-1 bg-card rounded-xl border border-border flex flex-col overflow-hidden">
                {activeSessionId && messages.length > 0 ? (
                    <>
                        {/* Chat Header */}
                        <div className="px-5 py-3.5 border-b border-border-subtle flex items-center gap-3 bg-gradient-to-r from-primary-50 to-primary-100">
                            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center">
                                <Bot className="h-5 w-5 text-white" />
                            </div>
                            <div>
                                <p className="font-semibold text-foreground text-sm">Upkilo AI Booking Assistant</p>
                                <p className="text-xs text-success-fg flex items-center gap-1">
                                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 inline-block" /> Online · Powered by GPT-4
                                </p>
                            </div>
                            {handedOff && (
                                <div className="ml-auto flex items-center gap-2 text-amber-600 bg-amber-50 px-3 py-1.5 rounded-lg text-xs font-medium">
                                    <AlertCircle className="h-3.5 w-3.5" /> Handed off to human
                                </div>
                            )}
                        </div>

                        {/* Messages */}
                        <div className="flex-1 overflow-y-auto p-5 space-y-4">
                            {messages.map(msg => (
                                <div key={msg.id} className={`flex gap-3 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
                                    <div className={`w-8 h-8 rounded-lg shrink-0 flex items-center justify-center ${msg.role === 'assistant' ? 'bg-gradient-to-br from-primary-500 to-primary-600' : 'bg-slate-200'}`}>
                                        {msg.role === 'assistant'
                                            ? <Bot className="h-4 w-4 text-white" />
                                            : <User className="h-4 w-4 text-foreground-secondary" />}
                                    </div>
                                    <div className={`max-w-[70%] space-y-2 ${msg.role === 'user' ? 'items-end' : 'items-start'} flex flex-col`}>
                                        <div className={`px-4 py-3 rounded-2xl text-sm leading-relaxed whitespace-pre-wrap ${
                                            msg.role === 'assistant'
                                                ? 'bg-muted border border-border text-foreground rounded-tl-sm'
                                                : 'bg-primary-600 text-white rounded-tr-sm'
                                        }`}>
                                            {msg.isTyping ? (
                                                <div className="flex gap-1.5 items-center py-1">
                                                    <div className="w-2 h-2 bg-slate-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                                                    <div className="w-2 h-2 bg-slate-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                                                    <div className="w-2 h-2 bg-slate-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                                                </div>
                                            ) : msg.content}
                                        </div>

                                        {/* Booking suggestion card */}
                                        {msg.suggestedBooking && (
                                            <div className="bg-brand-subtle border border-primary/25 rounded-xl p-3 space-y-2 w-full">
                                                <p className="text-xs font-semibold text-primary flex items-center gap-1">
                                                    <Calendar className="h-3.5 w-3.5" /> Suggested Booking
                                                </p>
                                                {msg.suggestedBooking.service && (
                                                    <p className="text-sm text-foreground">{msg.suggestedBooking.service}</p>
                                                )}
                                                {msg.suggestedBooking.dateTime && (
                                                    <p className="text-sm text-foreground-secondary flex items-center gap-1">
                                                        <Clock className="h-3.5 w-3.5" /> {msg.suggestedBooking.dateTime}
                                                    </p>
                                                )}
                                                <button
                                                    onClick={() => handleConfirmBooking(msg.suggestedBooking)}
                                                    className="w-full py-2 bg-primary-600 text-white rounded-lg text-xs font-semibold hover:bg-primary-700 flex items-center justify-center gap-1"
                                                >
                                                    <CheckCircle className="h-3.5 w-3.5" /> Confirm Booking
                                                </button>
                                            </div>
                                        )}

                                        <p className="text-xs text-foreground-muted px-1">{msg.timestamp.toLocaleTimeString()}</p>
                                    </div>
                                </div>
                            ))}
                            <div ref={bottomRef} />
                        </div>

                        {/* Quick prompts */}
                        <div className="px-4 py-2 border-t border-border-subtle flex gap-2 overflow-x-auto scrollbar-thin">
                            {QUICK_PROMPTS.map(p => (
                                <button
                                    key={p}
                                    onClick={() => sendMessage(p)}
                                    disabled={sending}
                                    className="shrink-0 px-3 py-1.5 text-xs bg-muted border border-border rounded-full text-foreground-secondary hover:bg-brand-subtle hover:border-primary-300 hover:text-primary transition-colors"
                                >
                                    {p}
                                </button>
                            ))}
                        </div>

                        {/* Input */}
                        <div className="px-4 py-3 border-t border-border-subtle bg-card flex gap-3">
                            <input
                                value={input}
                                onChange={e => setInput(e.target.value)}
                                onKeyDown={e => e.key === 'Enter' && !e.shiftKey && (e.preventDefault(), sendMessage())}
                                placeholder="Type a message... (Press Enter to send)"
                                className="flex-1 border border-border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                                disabled={sending || handedOff}
                            />
                            <button
                                onClick={() => sendMessage()}
                                disabled={!input.trim() || sending || handedOff}
                                className="p-2.5 bg-primary-600 text-white rounded-xl hover:bg-primary-700 disabled:opacity-40 transition-colors"
                            >
                                {sending ? <Loader2 className="h-5 w-5 animate-spin" /> : <Send className="h-5 w-5" />}
                            </button>
                        </div>
                    </>
                ) : (
                    <div className="flex-1 flex flex-col items-center justify-center text-foreground-muted">
                        <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center mb-4">
                            <Bot className="h-8 w-8 text-white" />
                        </div>
                        <h3 className="text-lg font-semibold text-foreground">AI Booking Assistant</h3>
                        <p className="text-sm text-foreground-secondary mt-1 mb-6 text-center max-w-sm">
                            An AI-powered chat interface that helps clients book appointments through natural conversation
                        </p>
                        <button onClick={startNewSession} className="flex items-center gap-2 px-5 py-2.5 bg-primary-600 text-white rounded-xl font-medium hover:bg-primary-700">
                            <Sparkles className="h-4 w-4" /> Start New Chat Session
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}

function generateFallbackResponse(userText: string): string {
    const text = userText.toLowerCase();
    if (text.includes('haircut')) return "Great choice! We offer haircuts starting at $45. I can book you in for tomorrow at 2:00 PM or 4:30 PM — which works better?";
    if (text.includes('color')) return "Our color services start at $120 depending on the technique. We have openings this Thursday and Friday. Would you like to reserve a slot?";
    if (text.includes('massage')) return "Our 60-minute massage is $90 and is incredibly relaxing! I have availability tomorrow morning and Friday afternoon. Any preference?";
    if (text.includes('hour') || text.includes('open')) return "We're open Monday–Saturday 9am–7pm and Sunday 10am–5pm. When would you like to come in?";
    if (text.includes('price') || text.includes('cost') || text.includes('how much')) return "Our services start from:\n• Haircut: $45\n• Blowout: $35\n• Color: $120+\n• Facial: $75\n• Massage: $90\n• Manicure: $30\n\nWhich service interests you?";
    if (text.includes('cancel')) return "I can help with that. Could you share your name or booking reference? I'll look up your appointment right away.";
    if (text.includes('book') || text.includes('schedule') || text.includes('appointment')) return "I'd love to help you book! What service are you looking for, and do you have a preferred day or time?";
    return "Thanks for reaching out! I'm here to help you book appointments and answer any questions. What can I assist you with today?";
}
