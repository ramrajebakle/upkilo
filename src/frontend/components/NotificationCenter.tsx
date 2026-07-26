'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { Bell, X, Check, CheckCheck, ExternalLink } from 'lucide-react';
import { cn, formatDate } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import Link from 'next/link';

interface Notification {
    id: string;
    type: string;
    title: string;
    message: string;
    actionUrl?: string;
    isRead: boolean;
    createdAt: string;
    priority: 'low' | 'normal' | 'high' | 'urgent';
}

export function NotificationCenter() {
    const [isOpen, setIsOpen] = useState(false);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [unreadCount, setUnreadCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const dropdownRef = useRef<HTMLDivElement>(null);

    const fetchNotifications = useCallback(async () => {
        try {
            const res = await apiClient.get('/api/v1/notifications');
            const data = res.data.data || res.data.notifications || res.data || [];
            setNotifications(data);
            setUnreadCount(data.filter((n: Notification) => !n.isRead).length);
        } catch (err) {
            // Quieter log for non-critical dashboard features
            console.debug('Failed to fetch notifications:', err);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchNotifications();
        // Poll for new notifications every 30 seconds
        const interval = setInterval(fetchNotifications, 30000);
        return () => clearInterval(interval);
    }, [fetchNotifications]);

    // Close on click outside
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const markAsRead = async (id: string) => {
        setNotifications((prev) =>
            prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
        );
        setUnreadCount((prev) => Math.max(0, prev - 1));
        try {
            await apiClient.patch(`/api/v1/notifications/${id}/read`);
        } catch (err) {
            console.error('Failed to mark notification as read:', err);
        }
    };

    const markAllAsRead = async () => {
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
        setUnreadCount(0);
        try {
            await apiClient.patch('/api/v1/notifications/read-all');
        } catch (err) {
            console.error('Failed to mark all notifications as read:', err);
        }
    };

    const getTypeIcon = (type: string) => {
        switch (type) {
            case 'booking_confirmed':
                return '📅';
            case 'payment_received':
                return '💰';
            case 'reminder':
                return '⏰';
            case 'system':
                return 'ℹ️';
            default:
                return '🔔';
        }
    };

    const formatTimeAgo = (date: string) => {
        const seconds = Math.floor((Date.now() - new Date(date).getTime()) / 1000);

        if (seconds < 60) return 'Just now';
        if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
        if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
        return formatDate(date);
    };

    return (
        <div className="relative" ref={dropdownRef}>
            {/* Bell button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                aria-haspopup="true"
                aria-expanded={isOpen}
                aria-label={`Notifications, ${unreadCount} unread`}
                className="relative p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors"
            >
                <Bell className="h-5 w-5 text-slate-600 dark:text-slate-400" aria-hidden="true" />
                {unreadCount > 0 && (
                    <span className="absolute top-1 right-1 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center" aria-hidden="true">
                        {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                )}
            </button>

            {/* Dropdown */}
            {isOpen && (
                <div className="absolute right-0 mt-2 w-80 bg-white dark:bg-slate-900 rounded-xl shadow-xl border border-slate-200 dark:border-white/10 z-50">
                    {/* Header */}
                    <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100 dark:border-white/5">
                        <h3 className="font-semibold text-slate-900 dark:text-white">Notifications</h3>
                        {unreadCount > 0 && (
                            <button
                                onClick={markAllAsRead}
                                className="text-sm text-primary-500 hover:text-primary-600 flex items-center gap-1"
                            >
                                <CheckCheck className="h-4 w-4" />
                                Mark all read
                            </button>
                        )}
                    </div>

                    {/* Notifications list */}
                    <div className="max-h-96 overflow-y-auto" role="list">
                        {loading ? (
                            <div className="p-4 text-center text-slate-500 dark:text-slate-400" role="status">Loading...</div>
                        ) : notifications.length === 0 ? (
                            <div className="p-8 text-center" role="status">
                                <Bell className="h-12 w-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" aria-hidden="true" />
                                <p className="text-slate-500 dark:text-slate-400">No notifications</p>
                            </div>
                        ) : (
                            notifications.map((notification) => (
                                <div
                                    key={notification.id}
                                    role="listitem"
                                    className={cn(
                                        'p-4 border-b border-slate-50 dark:border-white/5 hover:bg-slate-50 dark:hover:bg-white/5 transition-colors cursor-pointer',
                                        !notification.isRead && 'bg-primary-50/50 dark:bg-primary-500/10'
                                    )}
                                    onClick={() => markAsRead(notification.id)}
                                >
                                    <div className="flex gap-3">
                                        <span className="text-xl flex-shrink-0">
                                            {getTypeIcon(notification.type)}
                                        </span>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-start justify-between gap-2">
                                                <p
                                                    className={cn(
                                                        'text-sm',
                                                        notification.isRead ? 'text-slate-600 dark:text-slate-400' : 'text-slate-900 dark:text-white font-medium'
                                                    )}
                                                >
                                                    {notification.title}
                                                </p>
                                                <span className="text-xs text-slate-400 dark:text-slate-500 flex-shrink-0">
                                                    {formatTimeAgo(notification.createdAt)}
                                                </span>
                                            </div>
                                            <p className="text-sm text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-2">
                                                {notification.message}
                                            </p>
                                            {notification.actionUrl && (
                                                <Link
                                                    href={notification.actionUrl}
                                                    onClick={(e) => e.stopPropagation()}
                                                    className="inline-flex items-center gap-1 text-xs text-primary-500 hover:text-primary-600 mt-2"
                                                >
                                                    View <ExternalLink className="h-3 w-3" />
                                                </Link>
                                            )}
                                        </div>
                                        {!notification.isRead && (
                                            <div className="w-2 h-2 bg-primary-500 rounded-full flex-shrink-0 mt-1.5" />
                                        )}
                                    </div>
                                </div>
                            ))
                        )}
                    </div>

                    {/* Footer */}
                    <div className="px-4 py-3 border-t border-slate-100 dark:border-white/5">
                        <Link
                            href="/settings#notifications"
                            className="text-sm text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-300"
                        >
                            Notification settings
                        </Link>
                    </div>
                </div>
            )}
        </div>
    );
}
