'use client';

import { createContext, useContext, useState, useCallback, ReactNode } from 'react';
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';

type ToastType = 'success' | 'error' | 'info' | 'warning';

interface Toast {
    id: string;
    message: string;
    type: ToastType;
    duration?: number;
}

interface ToastContextType {
    toasts: Toast[];
    addToast: (message: string, type?: ToastType, duration?: number) => void;
    removeToast: (id: string) => void;
    success: (message: string) => void;
    error: (message: string) => void;
    info: (message: string) => void;
    warning: (message: string) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export function useToast() {
    const context = useContext(ToastContext);
    if (!context) {
        throw new Error('useToast must be used within a ToastProvider');
    }
    return context;
}

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);

    const removeToast = useCallback((id: string) => {
        setToasts((prev) => prev.filter((toast) => toast.id !== id));
    }, []);

    const addToast = useCallback(
        (message: string, type: ToastType = 'info', duration: number = 5000) => {
            const id = Math.random().toString(36).slice(2);
            const toast: Toast = { id, message, type, duration };

            setToasts((prev) => [...prev, toast]);

            if (duration > 0) {
                setTimeout(() => removeToast(id), duration);
            }
        },
        [removeToast]
    );

    const success = useCallback((message: string) => addToast(message, 'success'), [addToast]);
    const error = useCallback((message: string) => addToast(message, 'error'), [addToast]);
    const info = useCallback((message: string) => addToast(message, 'info'), [addToast]);
    const warning = useCallback((message: string) => addToast(message, 'warning'), [addToast]);

    return (
        <ToastContext.Provider value={{ toasts, addToast, removeToast, success, error, info, warning }}>
            {children}
            <ToastContainer toasts={toasts} removeToast={removeToast} />
        </ToastContext.Provider>
    );
}

function ToastContainer({ toasts, removeToast }: { toasts: Toast[]; removeToast: (id: string) => void }) {
    return (
        <div className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm">
            {toasts.map((toast) => (
                <ToastItem key={toast.id} toast={toast} onClose={() => removeToast(toast.id)} />
            ))}
        </div>
    );
}

function ToastItem({ toast, onClose }: { toast: Toast; onClose: () => void }) {
    const icons = {
        success: CheckCircle,
        error: AlertCircle,
        info: Info,
        warning: AlertTriangle,
    };

    const styles = {
        // The light 50-tints were the only definition, so every toast was a pale card —
        // on a dark page, a bright rectangle in the corner. The *-surface/-border/-fg
        // triples resolve per theme, so this is one set of classes for both.
        //
        // The tint is applied to a ::before layer rather than to the element itself. In dark
        // mode --status-*-surface is a translucent wash (correct for an alert panel sitting
        // on a card, where the card shows through as intended) but a toast FLOATS over
        // arbitrary page content — as a direct background it would let the text underneath
        // read through it. Stacking the wash over an opaque --surface-popover base keeps one
        // token set working for both jobs.
        success: 'border-success-border text-success-fg before:bg-success-surface',
        error: 'border-danger-border text-danger-fg before:bg-danger-surface',
        info: 'border-info-border text-info-fg before:bg-info-surface',
        warning: 'border-warning-border text-warning-fg before:bg-warning-surface',
    };

    const iconStyles = {
        success: 'text-success-fg',
        error: 'text-danger-fg',
        info: 'text-info-fg',
        warning: 'text-warning-fg',
    };

    const Icon = icons[toast.type];

    return (
        <div
            className={cn(
                'relative isolate flex items-start gap-3 p-4 rounded-lg border animate-slide-in',
                'bg-popover shadow-[var(--shadow-popover)]',
                'before:absolute before:inset-0 before:-z-10 before:rounded-[inherit]',
                styles[toast.type]
            )}
        >
            <Icon className={cn('h-5 w-5 flex-shrink-0', iconStyles[toast.type])} />
            <p className="flex-1 text-sm font-medium">{toast.message}</p>
            <button onClick={onClose} className="flex-shrink-0 hover:opacity-70">
                <X className="h-4 w-4" />
            </button>
        </div>
    );
}

// Add to globals.css: @keyframes slide-in { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
// .animate-slide-in { animation: slide-in 0.3s ease-out; }
