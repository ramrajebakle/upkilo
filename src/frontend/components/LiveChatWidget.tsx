"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { MessageCircle, X, Send, RotateCcw, Loader2 } from "lucide-react";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/authStore";

/**
 * Floating support assistant, mounted globally in the locale layout.
 *
 * This was previously a mockup: a styled panel whose input had no value, no handler and no
 * request behind it, so typing a question and pressing Enter did nothing at all. Every backend
 * chat endpoint was unreachable from the UI.
 *
 * It talks to one of two assistants depending on who is looking at it, because they have
 * different data in scope:
 *   - signed in  -> /aichatbot/message, the tenant's own assistant (its business + Upkilo)
 *   - anonymous  -> /support/chat, Upkilo's assistant (Upkilo only, no customer data anywhere)
 *
 * Which one is chosen is a display concern only. Neither endpoint trusts anything sent from here:
 * tenant, audience and conversation identity are all resolved server-side from the caller's
 * credentials, so picking the wrong one cannot widen what a visitor is allowed to see.
 */

type Message = {
    id: string;
    role: "user" | "assistant";
    text: string;
    /** Set on an assistant turn that failed, so the UI can offer a retry rather than a dead end. */
    failed?: boolean;
};

/** Bounded so a long session cannot grow the DOM without limit. */
const MAX_RENDERED = 100;

const GREETING = "Hi there! 👋 Ask me about Upkilo — what it does, plans, or getting set up.";

/** Session token for the anonymous assistant. Server-issued and opaque; we only echo it back. */
const SESSION_KEY = "upkilo.support.session";

function readSessionToken(): string | null {
    // sessionStorage throws outright in some privacy modes, so every access is guarded and a
    // failure degrades to "no session" rather than taking the widget down with it.
    try {
        return sessionStorage.getItem(SESSION_KEY);
    } catch {
        return null;
    }
}

function writeSessionToken(token: string) {
    try {
        sessionStorage.setItem(SESSION_KEY, token);
    } catch {
        /* Non-fatal: the session simply will not survive a reload. */
    }
}

let idCounter = 0;
const nextId = () => `m${++idCounter}`;

export function LiveChatWidget() {
    const [isOpen, setIsOpen] = useState(false);
    const [input, setInput] = useState("");
    const [messages, setMessages] = useState<Message[]>([
        { id: "greeting", role: "assistant", text: GREETING },
    ]);
    const [sending, setSending] = useState(false);

    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

    const scrollRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLTextAreaElement>(null);

    // Guards against a double submit racing past the `sending` state — React batches state
    // updates, so two fast Enter presses can both observe sending === false.
    const inFlight = useRef(false);

    // Keeps the newest turn visible. Depends on length rather than the array so an in-place
    // status change (a failed turn being retried) does not yank the view.
    useEffect(() => {
        const el = scrollRef.current;
        if (!el) return;
        // scrollTo is absent in some environments (jsdom, older embedded webviews). Assigning
        // scrollTop is universally supported, so the fallback keeps the transcript pinned to the
        // newest turn instead of throwing out of the effect and killing the render.
        if (typeof el.scrollTo === "function") {
            el.scrollTo({ top: el.scrollHeight, behavior: "smooth" });
        } else {
            el.scrollTop = el.scrollHeight;
        }
    }, [messages.length, sending]);

    useEffect(() => {
        if (isOpen) inputRef.current?.focus();
    }, [isOpen]);

    // Escape closes, matching every other dismissible surface in the product.
    useEffect(() => {
        if (!isOpen) return;
        const onKey = (e: KeyboardEvent) => {
            if (e.key === "Escape") setIsOpen(false);
        };
        document.addEventListener("keydown", onKey);
        return () => document.removeEventListener("keydown", onKey);
    }, [isOpen]);

    const send = useCallback(
        async (text: string) => {
            if (inFlight.current) return;

            const trimmed = text.trim();
            if (!trimmed) return; // whitespace-only is not a message

            inFlight.current = true;
            setSending(true);

            setMessages((prev) => [
                ...prev.slice(-MAX_RENDERED),
                { id: nextId(), role: "user", text: trimmed },
            ]);

            try {
                let reply: string | undefined;

                if (isAuthenticated) {
                    const res = await api.chatbot.sendMessage(trimmed);
                    reply = res.data?.response;
                } else {
                    const res = await api.support.chat(trimmed, readSessionToken());
                    if (res.data?.sessionToken) writeSessionToken(res.data.sessionToken);
                    reply = res.data?.reply;
                }

                setMessages((prev) => [
                    ...prev,
                    {
                        id: nextId(),
                        role: "assistant",
                        // A 200 with an empty body is still a failed turn from the visitor's point
                        // of view, so it gets the retry affordance rather than an empty bubble.
                        text: reply || "I didn't catch that. Could you try rephrasing?",
                        failed: !reply,
                    },
                ]);
            } catch {
                // Deliberately does not surface the error object. It can carry backend detail,
                // and none of it helps the visitor.
                setMessages((prev) => [
                    ...prev,
                    {
                        id: nextId(),
                        role: "assistant",
                        text: "Something went wrong reaching the assistant. Please try again.",
                        failed: true,
                    },
                ]);
            } finally {
                inFlight.current = false;
                setSending(false);
                inputRef.current?.focus();
            }
        },
        [isAuthenticated]
    );

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const text = input;
        setInput(""); // cleared first so the box never holds a message already in flight
        void send(text);
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        // Enter sends; Shift+Enter is a newline. IME composition must not be treated as a send,
        // or the first Enter confirming a Japanese/Chinese candidate would fire the message.
        if (e.key === "Enter" && !e.shiftKey && !(e.nativeEvent as any).isComposing) {
            e.preventDefault();
            const text = input;
            setInput("");
            void send(text);
        }
    };

    /** Re-sends the last visitor turn after a failure, without duplicating it in the transcript. */
    const retry = () => {
        const lastUser = [...messages].reverse().find((m) => m.role === "user");
        if (!lastUser) return;
        setMessages((prev) => prev.filter((m) => !m.failed && m.id !== lastUser.id));
        void send(lastUser.text);
    };

    const lastFailed = messages[messages.length - 1]?.failed === true;

    return (
        <div className="fixed bottom-20 sm:bottom-6 right-4 sm:right-6 z-50">
            {isOpen && (
                <div
                    role="dialog"
                    aria-modal="false"
                    aria-label="Upkilo support chat"
                    // Width is capped against the viewport so the panel never overflows a narrow
                    // screen, and height against dvh so the mobile keyboard cannot push the input
                    // off-screen — the failure that makes a chat box unusable on a phone.
                    className="bg-background border border-border shadow-2xl rounded-2xl
                               w-[min(22rem,calc(100vw-2rem))] h-[min(28rem,calc(100dvh-8rem))]
                               mb-4 flex flex-col overflow-hidden"
                >
                    <div className="bg-primary text-primary-foreground p-4 flex justify-between items-center shrink-0">
                        <span className="font-semibold">Upkilo Support</span>
                        <button
                            type="button"
                            onClick={() => setIsOpen(false)}
                            className="hover:bg-primary/90 rounded-full p-1 transition-colors
                                       focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
                            aria-label="Close chat"
                        >
                            <X size={18} aria-hidden="true" />
                        </button>
                    </div>

                    {/* role=log + aria-live announces new turns to a screen reader without
                        stealing focus from the input. */}
                    <div
                        ref={scrollRef}
                        role="log"
                        aria-live="polite"
                        aria-label="Conversation"
                        className="flex-1 p-4 overflow-y-auto space-y-3 bg-muted/20"
                    >
                        {messages.slice(-MAX_RENDERED).map((m) => (
                            <div
                                key={m.id}
                                className={
                                    m.role === "user"
                                        ? "ml-auto bg-primary text-primary-foreground p-3 rounded-lg rounded-br-sm max-w-[85%] text-sm whitespace-pre-wrap break-words"
                                        : "bg-muted text-foreground p-3 rounded-lg rounded-bl-sm max-w-[85%] text-sm whitespace-pre-wrap break-words"
                                }
                            >
                                {/* Rendered as text, never as HTML. The assistant's output is
                                    untrusted by construction, so there is no markdown or HTML
                                    parser here for an injected payload to reach. */}
                                {m.text}
                            </div>
                        ))}

                        {sending && (
                            <div className="flex items-center gap-2 text-sm text-foreground-secondary">
                                <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                                <span>Thinking…</span>
                            </div>
                        )}

                        {lastFailed && !sending && (
                            <button
                                type="button"
                                onClick={retry}
                                className="flex items-center gap-1.5 text-sm text-primary hover:underline
                                           focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded"
                            >
                                <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
                                Try again
                            </button>
                        )}
                    </div>

                    <form onSubmit={handleSubmit} className="p-3 border-t border-border bg-background shrink-0">
                        <div className="flex items-end gap-2">
                            <label htmlFor="upkilo-support-input" className="sr-only">
                                Type your message
                            </label>
                            <textarea
                                id="upkilo-support-input"
                                ref={inputRef}
                                rows={1}
                                value={input}
                                onChange={(e) => setInput(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Type a message…"
                                maxLength={1000}
                                className="flex-1 resize-none text-sm outline-none px-3 py-2 border border-border
                                           rounded-2xl bg-muted/50 focus:bg-background focus:ring-1 focus:ring-primary
                                           transition-all max-h-24 text-foreground"
                            />
                            <button
                                type="submit"
                                // Disabled while a turn is in flight and for an empty box, so the
                                // send button cannot queue a duplicate or an empty message.
                                disabled={sending || input.trim().length === 0}
                                aria-label="Send message"
                                className="p-2 rounded-full bg-primary text-primary-foreground shrink-0
                                           disabled:opacity-40 disabled:cursor-not-allowed
                                           focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                            >
                                <Send size={16} aria-hidden="true" />
                            </button>
                        </div>
                    </form>
                </div>
            )}

            <button
                type="button"
                onClick={() => setIsOpen((o) => !o)}
                className="bg-primary text-primary-foreground p-4 rounded-full shadow-xl hover:scale-105
                           transition-transform flex items-center justify-center ml-auto
                           focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                aria-label={isOpen ? "Close support chat" : "Open support chat"}
                aria-expanded={isOpen}
            >
                {isOpen ? <X size={24} aria-hidden="true" /> : <MessageCircle size={24} aria-hidden="true" />}
            </button>
        </div>
    );
}
