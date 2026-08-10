'use client';

import React, { useState, useEffect } from 'react';
import { useSignalR, BookingNotification } from '@/contexts/SignalRContext';
import { Calendar, CheckCircle, XCircle, Clock, Info, ChevronRight } from 'lucide-react';
import { cn, formatRelativeTime } from '@/lib/utils';

export function LiveBookingFeed() {
    const { connection } = useSignalR();
    const [feed, setFeed] = useState<BookingNotification[]>([]);

    useEffect(() => {
        if (!connection) return;

        const handleNewBooking = (notification: BookingNotification) => {
            setFeed(prev => [notification, ...prev].slice(0, 15));
        };

        const handleUpdate = (notification: BookingNotification) => {
            setFeed(prev => [notification, ...prev].slice(0, 15));
        };

        const handleCancel = (notification: BookingNotification) => {
            setFeed(prev => [notification, ...prev].slice(0, 15));
        };

        connection.on('BookingCreated', handleNewBooking);
        connection.on('BookingUpdated', handleUpdate);
        connection.on('BookingCancelled', handleCancel);

        return () => {
            connection.off('BookingCreated', handleNewBooking);
            connection.off('BookingUpdated', handleUpdate);
            connection.off('BookingCancelled', handleCancel);
        };
    }, [connection]);

    const getIcon = (status: string) => {
        switch (status.toLowerCase()) {
            case 'confirmed': return <CheckCircle className="h-4 w-4 text-emerald-500" />;
            case 'cancelled': return <XCircle className="h-4 w-4 text-red-500" />;
            case 'completed': return <Award className="h-4 w-4 text-primary-500" />;
            case 'inprogress': return <Clock className="h-4 w-4 text-blue-500" />;
            default: return <Info className="h-4 w-4 text-slate-400" />;
        }
    };

    const getBgColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'confirmed': return 'bg-emerald-50 border-emerald-100';
            case 'cancelled': return 'bg-red-50 border-red-100';
            case 'completed': return 'bg-primary-50 border-primary-100';
            case 'inprogress': return 'bg-blue-50 border-blue-100';
            default: return 'bg-slate-50 border-slate-100';
        }
    };

    return (
        <div className="card-elevated overflow-hidden animate-fade-in-up" style={{ animationDelay: '450ms' }}>
            <div className="p-6 border-b border-slate-100 flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <div className="p-2 bg-emerald-50 rounded-lg">
                        <Clock className="h-5 w-5 text-emerald-500" />
                    </div>
                    <div>
                        <h2 className="text-lg font-semibold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>
                            Live Activity
                        </h2>
                        <p className="text-sm text-slate-500 font-medium flex items-center gap-1.5">
                            <span className="relative flex h-2 w-2">
                                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                            </span>
                            Running now
                        </p>
                    </div>
                </div>
            </div>

            <div className="max-h-[400px] overflow-y-auto divide-y divide-slate-100 scrollbar-hide">
                {feed.length === 0 ? (
                    <div className="p-12 text-center">
                        <div className="w-12 h-12 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-3">
                            <Activity className="h-6 w-6 text-slate-300" />
                        </div>
                        <p className="text-slate-400 text-sm">No live activity yet.</p>
                    </div>
                ) : (
                    feed.map((item, idx) => (
                        <div 
                            key={item.bookingId + idx} 
                            className="p-4 hover:bg-slate-50 transition-all duration-300 animate-in slide-in-from-top-2 fade-in"
                        >
                            <div className="flex items-start gap-4">
                                <div className={cn("p-2 rounded-lg border", getBgColor(item.status))}>
                                    {getIcon(item.status)}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center justify-between mb-0.5">
                                        <p className="font-semibold text-slate-900 text-sm truncate">
                                            {item.clientName}
                                        </p>
                                        <span className="text-[10px] font-medium text-slate-400 uppercase tracking-wider">
                                            {item.status}
                                        </span>
                                    </div>
                                    <p className="text-xs text-slate-600 mb-1">
                                        {item.message} - <span className="font-medium">{item.serviceName}</span>
                                    </p>
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-1.5 text-[10px] text-slate-400">
                                            <Calendar className="h-3 w-3" />
                                            {item.startTime ? formatRelativeTime(item.startTime) : ''}
                                        </div>
                                        <div className="flex items-center gap-1 text-[10px] text-primary-500 font-semibold">
                                            {item.staffName && <span>with {item.staffName.split(' ')[0]}</span>}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))
                )}
            </div>

            <div className="p-4 bg-slate-50 border-t border-slate-100 text-center">
                <button className="text-xs font-semibold text-slate-500 hover:text-primary-600 transition-colors flex items-center gap-1 justify-center mx-auto">
                    View Activity Log
                    <ChevronRight className="h-3 w-3" />
                </button>
            </div>
        </div>
    );
}

// Support components for types/icons if needed or use Lucide directly
const Award = ({ className }: { className: string }) => (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}><circle cx="12" cy="8" r="6"/><path d="M15.477 12.89L17 22l-5-3-5 3 1.523-9.11"/></svg>
);

const Activity = ({ className }: { className: string }) => (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
);
