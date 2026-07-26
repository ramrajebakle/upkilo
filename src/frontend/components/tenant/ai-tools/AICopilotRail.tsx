'use client';

import React, { useState, useRef, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
    Bot,
    X,
    Send,
    ChevronRight,
    Sparkles,
    Loader2,
    RefreshCw,
    ThumbsUp,
    ThumbsDown,
    Copy,
    Check,
} from 'lucide-react';
import { cn } from '@/lib/utils';

interface Message {
    id: string;
    role: 'user' | 'assistant';
    content: string;
    timestamp: Date;
}

interface AICopilotRailProps {
    isOpen: boolean;
    onClose: () => void;
    contextHint?: string;
}

const SUGGESTED_PROMPTS = [
    'Summarize today\'s bookings',
    'Which clients haven\'t returned in 60 days?',
    'Draft a re-engagement SMS for inactive clients',
    'What services are most profitable this month?',
    'Create a workflow to follow up after appointments',
];

async function sendCopilotMessage(messages: Message[], signal: AbortSignal): Promise<string> {
    const res = await fetch('/api/v1/ai/copilot', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            messages: messages.map((m) => ({ role: m.role, content: m.content })),
        }),
        signal,
    });
    if (!res.ok) throw new Error('Failed to get AI response');
    const data = await res.json();
    return data.reply ?? data.content ?? '';
}

export function AICopilotRail({ isOpen, onClose, contextHint }: AICopilotRailProps) {
    const [messages, setMessages] = useState<Message[]>([]);
    const [input, setInput] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [copiedId, setCopiedId] = useState<string | null>(null);
    const abortRef = useRef<AbortController | null>(null);
    const bottomRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLTextAreaElement>(null);

    useEffect(() => {
        if (isOpen) {
            setTimeout(() => inputRef.current?.focus(), 300);
        }
    }, [isOpen]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const send = async (text: string) => {
        const trimmed = text.trim();
        if (!trimmed || isLoading) return;

        const userMsg: Message = {
            id: crypto.randomUUID(),
            role: 'user',
            content: trimmed,
            timestamp: new Date(),
        };

        setMessages((prev) => [...prev, userMsg]);
        setInput('');
        setIsLoading(true);

        abortRef.current = new AbortController();

        try {
            const reply = await sendCopilotMessage([...messages, userMsg], abortRef.current.signal);
            const assistantMsg: Message = {
                id: crypto.randomUUID(),
                role: 'assistant',
                content: reply,
                timestamp: new Date(),
            };
            setMessages((prev) => [...prev, assistantMsg]);
        } catch (err: any) {
            if (err.name !== 'AbortError') {
                const errMsg: Message = {
                    id: crypto.randomUUID(),
                    role: 'assistant',
                    content: 'Sorry, I ran into an issue. Please try again.',
                    timestamp: new Date(),
                };
                setMessages((prev) => [...prev, errMsg]);
            }
        } finally {
            setIsLoading(false);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            send(input);
        }
    };

    const copyMessage = async (msg: Message) => {
        await navigator.clipboard.writeText(msg.content);
        setCopiedId(msg.id);
        setTimeout(() => setCopiedId(null), 2000);
    };

    const clearChat = () => {
        abortRef.current?.abort();
        setMessages([]);
        setIsLoading(false);
    };

    return (
        <AnimatePresence>
            {isOpen && (
                <motion.aside
                    key="copilot-rail"
                    initial={{ x: '100%', opacity: 0 }}
                    animate={{ x: 0, opacity: 1 }}
                    exit={{ x: '100%', opacity: 0 }}
                    transition={{ type: 'spring', stiffness: 300, damping: 30 }}
                    className="fixed right-0 top-0 h-full w-[360px] z-40 flex flex-col bg-white border-l border-surface-200 shadow-2xl"
                >
                    {/* Header */}
                    <div className="flex items-center justify-between px-4 py-3 border-b border-surface-200 bg-gradient-to-r from-indigo-50 to-purple-50">
                        <div className="flex items-center gap-2">
                            <div className="p-1.5 bg-indigo-600 rounded-lg">
                                <Bot className="h-4 w-4 text-white" />
                            </div>
                            <div>
                                <p className="text-sm font-semibold text-text-primary">AI Copilot</p>
                                <p className="text-xs text-text-tertiary flex items-center gap-1">
                                    <Sparkles className="h-3 w-3 text-indigo-400" />
                                    Context-aware assistant
                                </p>
                            </div>
                        </div>
                        <div className="flex items-center gap-1">
                            <button
                                onClick={clearChat}
                                title="Clear chat"
                                className="p-1.5 rounded-md text-text-tertiary hover:text-text-primary hover:bg-surface-100 transition-colors"
                            >
                                <RefreshCw className="h-4 w-4" />
                            </button>
                            <button
                                onClick={onClose}
                                className="p-1.5 rounded-md text-text-tertiary hover:text-text-primary hover:bg-surface-100 transition-colors"
                            >
                                <X className="h-4 w-4" />
                            </button>
                        </div>
                    </div>

                    {/* Context hint banner */}
                    {contextHint && (
                        <div className="px-4 py-2 bg-indigo-50 border-b border-indigo-100 text-xs text-indigo-700 flex items-center gap-1.5">
                            <ChevronRight className="h-3 w-3" />
                            Context: {contextHint}
                        </div>
                    )}

                    {/* Messages */}
                    <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
                        {messages.length === 0 && (
                            <div className="space-y-4">
                                <div className="text-center py-6">
                                    <div className="mx-auto mb-3 w-12 h-12 bg-indigo-50 rounded-2xl flex items-center justify-center">
                                        <Sparkles className="h-6 w-6 text-indigo-500" />
                                    </div>
                                    <p className="text-sm font-medium text-text-primary">How can I help?</p>
                                    <p className="text-xs text-text-tertiary mt-1">Ask me anything about your business</p>
                                </div>

                                <div className="space-y-2">
                                    <p className="text-xs font-medium text-text-tertiary uppercase tracking-wide">Suggestions</p>
                                    {SUGGESTED_PROMPTS.map((prompt) => (
                                        <button
                                            key={prompt}
                                            onClick={() => send(prompt)}
                                            className="w-full text-left text-sm px-3 py-2.5 rounded-xl border border-surface-200 text-text-secondary hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700 transition-all"
                                        >
                                            {prompt}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        )}

                        {messages.map((msg) => (
                            <div
                                key={msg.id}
                                className={cn('flex gap-2', msg.role === 'user' ? 'justify-end' : 'justify-start')}
                            >
                                {msg.role === 'assistant' && (
                                    <div className="shrink-0 w-7 h-7 bg-indigo-600 rounded-full flex items-center justify-center mt-0.5">
                                        <Bot className="h-3.5 w-3.5 text-white" />
                                    </div>
                                )}

                                <div className={cn('group max-w-[80%]', msg.role === 'user' ? 'items-end' : 'items-start', 'flex flex-col gap-1')}>
                                    <div
                                        className={cn(
                                            'rounded-2xl px-3.5 py-2.5 text-sm leading-relaxed whitespace-pre-wrap',
                                            msg.role === 'user'
                                                ? 'bg-indigo-600 text-white rounded-tr-sm'
                                                : 'bg-surface-100 text-text-primary rounded-tl-sm'
                                        )}
                                    >
                                        {msg.content}
                                    </div>

                                    {msg.role === 'assistant' && (
                                        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                            <button
                                                onClick={() => copyMessage(msg)}
                                                className="p-1 rounded text-text-tertiary hover:text-text-primary transition-colors"
                                                title="Copy"
                                            >
                                                {copiedId === msg.id ? <Check className="h-3 w-3 text-green-500" /> : <Copy className="h-3 w-3" />}
                                            </button>
                                            <button className="p-1 rounded text-text-tertiary hover:text-green-500 transition-colors" title="Helpful">
                                                <ThumbsUp className="h-3 w-3" />
                                            </button>
                                            <button className="p-1 rounded text-text-tertiary hover:text-red-500 transition-colors" title="Not helpful">
                                                <ThumbsDown className="h-3 w-3" />
                                            </button>
                                        </div>
                                    )}
                                </div>
                            </div>
                        ))}

                        {isLoading && (
                            <div className="flex gap-2 justify-start">
                                <div className="shrink-0 w-7 h-7 bg-indigo-600 rounded-full flex items-center justify-center mt-0.5">
                                    <Bot className="h-3.5 w-3.5 text-white" />
                                </div>
                                <div className="bg-surface-100 rounded-2xl rounded-tl-sm px-3.5 py-2.5 flex items-center gap-1.5">
                                    <Loader2 className="h-3.5 w-3.5 text-indigo-500 animate-spin" />
                                    <span className="text-sm text-text-secondary">Thinking…</span>
                                </div>
                            </div>
                        )}

                        <div ref={bottomRef} />
                    </div>

                    {/* Input */}
                    <div className="px-4 py-3 border-t border-surface-200 bg-surface-50">
                        <div className="flex items-end gap-2 bg-white rounded-2xl border border-surface-200 px-3 py-2 focus-within:border-indigo-400 focus-within:ring-2 focus-within:ring-indigo-100 transition-all">
                            <textarea
                                ref={inputRef}
                                rows={1}
                                value={input}
                                onChange={(e) => setInput(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Ask your AI Copilot…"
                                className="flex-1 resize-none bg-transparent text-sm text-text-primary placeholder:text-text-tertiary outline-none max-h-32 min-h-[1.5rem]"
                            />
                            <button
                                onClick={() => send(input)}
                                disabled={!input.trim() || isLoading}
                                className={cn(
                                    'shrink-0 p-1.5 rounded-xl transition-all',
                                    input.trim() && !isLoading
                                        ? 'bg-indigo-600 text-white hover:bg-indigo-700'
                                        : 'bg-surface-200 text-text-tertiary cursor-not-allowed'
                                )}
                            >
                                <Send className="h-3.5 w-3.5" />
                            </button>
                        </div>
                        <p className="text-center text-xs text-text-tertiary mt-2">
                            AI can make mistakes. Verify important details.
                        </p>
                    </div>
                </motion.aside>
            )}
        </AnimatePresence>
    );
}
