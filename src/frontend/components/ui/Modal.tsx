'use client';

import { ReactNode, useEffect } from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ModalProps {
    isOpen: boolean;
    onClose: () => void;
    title?: string;
    description?: string;
    children: ReactNode;
    size?: 'sm' | 'md' | 'lg' | 'xl';
    showCloseButton?: boolean;
}

export function Modal({
    isOpen,
    onClose,
    title,
    description,
    children,
    size = 'md',
    showCloseButton = true,
}: ModalProps) {
    // Close on escape key
    useEffect(() => {
        const handleEscape = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose();
        };

        if (isOpen) {
            document.addEventListener('keydown', handleEscape);
            document.body.style.overflow = 'hidden';
        }

        return () => {
            document.removeEventListener('keydown', handleEscape);
            document.body.style.overflow = 'unset';
        };
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    const sizes = {
        sm: 'max-w-sm',
        md: 'max-w-md',
        lg: 'max-w-lg',
        xl: 'max-w-xl',
    };

    return (
        <div className="fixed inset-0 z-50">
            {/* Backdrop */}
            <div
                className="absolute inset-0 bg-black/50 backdrop-blur-sm"
                onClick={onClose}
            />

            {/* Modal */}
            <div className="flex items-center justify-center min-h-full p-4">
                <div
                    className={cn(
                        'relative bg-white dark:bg-slate-900 rounded-xl shadow-xl w-full animate-modal-in border border-gray-100 dark:border-slate-800',
                        sizes[size]
                    )}
                >
                    {/* Header */}
                    {(title || showCloseButton) && (
                        <div className="flex items-start justify-between p-6 border-b border-gray-100 dark:border-slate-800">
                            <div>
                                {title && (
                                    <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{title}</h2>
                                )}
                                {description && (
                                    <p className="text-sm text-gray-500 dark:text-slate-400 mt-1">{description}</p>
                                )}
                            </div>
                            {showCloseButton && (
                                <button
                                    onClick={onClose}
                                    className="p-1 hover:bg-gray-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
                                >
                                    <X className="h-5 w-5 text-gray-400 dark:text-slate-500" />
                                </button>
                            )}
                        </div>
                    )}

                    {/* Content */}
                    <div className="p-6">{children}</div>
                </div>
            </div>
        </div>
    );
}

interface ConfirmModalProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title: string;
    description: string;
    confirmText?: string;
    cancelText?: string;
    variant?: 'danger' | 'warning' | 'default';
    loading?: boolean;
}

export function ConfirmModal({
    isOpen,
    onClose,
    onConfirm,
    title,
    description,
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    variant = 'default',
    loading = false,
}: ConfirmModalProps) {
    const buttonStyles = {
        danger: 'bg-red-500 hover:bg-red-600 text-white',
        warning: 'bg-yellow-500 hover:bg-yellow-600 text-white',
        default: 'bg-primary-500 hover:bg-primary-600 text-white',
    };

    return (
        <Modal isOpen={isOpen} onClose={onClose} size="sm" showCloseButton={false}>
            <div className="text-center">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">{title}</h3>
                <p className="text-gray-500 dark:text-slate-400 mb-6">{description}</p>

                <div className="flex gap-3">
                    <button
                        onClick={onClose}
                        disabled={loading}
                        className="flex-1 px-4 py-2 border border-gray-300 dark:border-slate-700 text-gray-700 dark:text-slate-300 rounded-lg hover:bg-gray-50 dark:hover:bg-slate-800 font-medium disabled:opacity-50"
                    >
                        {cancelText}
                    </button>
                    <button
                        onClick={onConfirm}
                        disabled={loading}
                        className={cn(
                            'flex-1 px-4 py-2 rounded-lg font-medium disabled:opacity-50 flex items-center justify-center',
                            buttonStyles[variant]
                        )}
                    >
                        {loading ? (
                            <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        ) : (
                            confirmText
                        )}
                    </button>
                </div>
            </div>
        </Modal>
    );
}

// Add to globals.css:
// @keyframes modal-in { from { transform: scale(0.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }
// .animate-modal-in { animation: modal-in 0.2s ease-out; }
