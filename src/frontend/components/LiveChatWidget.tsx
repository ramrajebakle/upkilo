"use client";

import { useState } from "react";
import { MessageCircle, X } from "lucide-react";

export function LiveChatWidget() {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className="fixed bottom-20 sm:bottom-6 right-6 z-50">
            {isOpen && (
                <div className="bg-background border shadow-2xl rounded-2xl w-80 h-96 mb-4 flex flex-col overflow-hidden">
                    <div className="bg-primary text-primary-foreground p-4 flex justify-between items-center">
                        <span className="font-semibold">Upkilo Support</span>
                        <button
                            onClick={() => setIsOpen(false)}
                            className="hover:bg-primary/90 rounded-full p-1 transition-colors"
                            aria-label="Close chat"
                        >
                            <X size={18} aria-hidden="true" />
                        </button>
                    </div>
                    <div className="flex-1 p-4 overflow-y-auto space-y-4 bg-muted/20">
                        <div className="bg-muted p-3 rounded-lg w-3/4 text-sm text-foreground">
                            Hi there! 👋 Need help scaling your business?
                        </div>
                    </div>
                    <div className="p-3 border-t bg-background">
                        <input
                            type="text"
                            placeholder="Type a message..."
                            className="w-full text-sm outline-none px-3 py-2 border rounded-full bg-muted/50 focus:bg-background focus:ring-1 focus:ring-primary transition-all"
                        />
                    </div>
                </div>
            )}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="bg-primary text-primary-foreground p-4 rounded-full shadow-xl hover:scale-105 transition-transform flex items-center justify-center"
                aria-label={isOpen ? 'Close support chat' : 'Open support chat'}
                aria-expanded={isOpen}
            >
                {isOpen ? <X size={24} aria-hidden="true" /> : <MessageCircle size={24} aria-hidden="true" />}
            </button>
        </div>
    );
}
