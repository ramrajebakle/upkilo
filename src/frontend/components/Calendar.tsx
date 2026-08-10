'use client';

import { useState, useEffect, useCallback } from 'react';
import { ChevronLeft, ChevronRight, Plus, RefreshCw, Eye, Clock, User } from 'lucide-react';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { useSignalR } from '@/contexts/SignalRContext';
import { useToast } from '@/components/ui/Toast';
import { apiClient } from '@/lib/api';

interface CalendarEvent {
    id: string;
    title: string;
    clientName: string;
    startTime: string;
    endTime: string;
    date: string; // YYYY-MM-DD
    color: string;
    status: string;
    staffName?: string;
    serviceName?: string;
}

// Deterministic color palette for services/staff
const COLOR_PALETTE = [
    '#3B82F6', '#10B981', '#8B5CF6', '#F59E0B', '#EF4444',
    '#06B6D4', '#EC4899', '#14B8A6', '#F97316', '#6366F1',
];

function getColorForKey(key: string, map: Map<string, string>): string {
    if (map.has(key)) return map.get(key)!;
    const color = COLOR_PALETTE[map.size % COLOR_PALETTE.length];
    map.set(key, color);
    return color;
}

type ColorMode = 'service' | 'staff' | 'status';

const STATUS_COLORS: Record<string, string> = {
    confirmed: '#3B82F6',
    pending: '#F59E0B',
    completed: '#10B981',
    cancelled: '#EF4444',
    no_show: '#6B7280',
};

export default function CalendarView() {
    const [currentDate, setCurrentDate] = useState(new Date());
    const [view, setView] = useState<'week' | 'day'>('week');
    const [events, setEvents] = useState<CalendarEvent[]>([]);
    const [loading, setLoading] = useState(true);
    const [colorMode, setColorMode] = useState<ColorMode>('service');
    const [colorMap] = useState(new Map<string, string>());
    const { connection } = useSignalR();
    const { info } = useToast();

    const hours = Array.from({ length: 12 }, (_, i) => i + 8); // 8 AM to 7 PM
    const weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    const getWeekDates = useCallback(() => {
        const dates: Date[] = [];
        const startOfWeek = new Date(currentDate);
        startOfWeek.setDate(currentDate.getDate() - currentDate.getDay());
        for (let i = 0; i < 7; i++) {
            const date = new Date(startOfWeek);
            date.setDate(startOfWeek.getDate() + i);
            dates.push(date);
        }
        return dates;
    }, [currentDate]);

    const weekDates = getWeekDates();

    const fetchBookings = useCallback(async () => {
        setLoading(true);
        try {
            const from = weekDates[0];
            const to = weekDates[6];
            to.setHours(23, 59, 59);
            const res = await apiClient.get('/api/v1/bookings', {
                params: {
                    startDate: from.toISOString(),
                    endDate: to.toISOString(),
                    limit: 200,
                }
            });
            const bookings = res.data?.data?.items || res.data?.data || res.data?.items || res.data || [];

            // Reset color map for fresh assignment
            colorMap.clear();

            const mapped: CalendarEvent[] = (Array.isArray(bookings) ? bookings : []).map((b: any) => {
                const start = new Date(b.startTime || b.start);
                const end = new Date(b.endTime || b.end || new Date(start.getTime() + 60 * 60000));

                let colorKey: string;
                if (colorMode === 'service') colorKey = b.serviceName || b.service?.name || 'Unknown';
                else if (colorMode === 'staff') colorKey = b.staffName || b.staff?.name || 'Unknown';
                else colorKey = b.status || 'confirmed';

                const color = colorMode === 'status'
                    ? (STATUS_COLORS[b.status] || '#6B7280')
                    : getColorForKey(colorKey, colorMap);

                return {
                    id: b.id,
                    title: b.serviceName || b.service?.name || 'Booking',
                    clientName: b.clientName || b.client?.name || 'Client',
                    staffName: b.staffName || b.staff?.name,
                    serviceName: b.serviceName || b.service?.name,
                    startTime: `${start.getHours().toString().padStart(2, '0')}:${start.getMinutes().toString().padStart(2, '0')}`,
                    endTime: `${end.getHours().toString().padStart(2, '0')}:${end.getMinutes().toString().padStart(2, '0')}`,
                    date: start.toISOString().slice(0, 10),
                    color,
                    status: b.status || 'confirmed',
                };
            });

            setEvents(mapped);
        } catch (err) {
            console.error('Failed to load calendar bookings:', err);
            setEvents([]);
        } finally {
            setLoading(false);
        }
    }, [currentDate, colorMode]);

    useEffect(() => { fetchBookings(); }, [fetchBookings]);

    useEffect(() => {
        if (!connection) return;
        const handleUpdate = (notification: any) => {
            info(`Schedule Updated: ${notification.message || 'Booking change detected'}`);
            fetchBookings();
        };
        connection.on('StaffScheduleUpdated', handleUpdate);
        connection.on('BookingCreated', handleUpdate);
        connection.on('BookingUpdated', handleUpdate);
        return () => {
            connection.off('StaffScheduleUpdated', handleUpdate);
            connection.off('BookingCreated', handleUpdate);
            connection.off('BookingUpdated', handleUpdate);
        };
    }, [connection, info, fetchBookings]);

    const isToday = (date: Date) => date.toDateString() === new Date().toDateString();

    const getEventPosition = (startTime: string) => {
        const [h, m] = startTime.split(':').map(Number);
        return ((h - 8) + m / 60) * 60;
    };

    const getEventHeight = (startTime: string, endTime: string) => {
        const [sh, sm] = startTime.split(':').map(Number);
        const [eh, em] = endTime.split(':').map(Number);
        return Math.max(((eh - sh) + (em - sm) / 60) * 60, 20);
    };

    const getEventsForDate = (date: Date) => {
        const dateStr = date.toISOString().slice(0, 10);
        return events.filter(e => e.date === dateStr);
    };

    // Build legend
    const legend = colorMode === 'status'
        ? Object.entries(STATUS_COLORS).map(([k, v]) => ({ label: k, color: v }))
        : Array.from(colorMap.entries()).map(([k, v]) => ({ label: k, color: v }));

    return (
        <div className="h-[calc(100vh-200px)] flex flex-col gap-4">
            {/* Header */}
            <div className="flex items-center justify-between flex-wrap gap-4 px-1">
                <div className="flex items-center gap-4">
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>Calendar</h1>
                    <div className="flex items-center gap-1.5 bg-slate-100 dark:bg-slate-800 p-1 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm">
                        <button 
                            onClick={() => { const d = new Date(currentDate); d.setDate(d.getDate() - 7); setCurrentDate(d); }} 
                            className="p-1.5 hover:bg-white dark:hover:bg-slate-700 rounded-lg transition-all text-slate-600 dark:text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400"
                        >
                            <ChevronLeft className="h-4.5 w-4.5" />
                        </button>
                        <span className="font-bold min-w-[200px] text-center text-xs uppercase tracking-widest text-slate-700 dark:text-slate-300 px-2">
                            {weekDates[0].toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} – {weekDates[6].toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                        </span>
                        <button 
                            onClick={() => { const d = new Date(currentDate); d.setDate(d.getDate() + 7); setCurrentDate(d); }} 
                            className="p-1.5 hover:bg-white dark:hover:bg-slate-700 rounded-lg transition-all text-slate-600 dark:text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400"
                        >
                            <ChevronRight className="h-4.5 w-4.5" />
                        </button>
                    </div>
                    <button 
                        onClick={() => setCurrentDate(new Date())} 
                        className="px-4 py-2 text-xs font-bold uppercase tracking-widest bg-indigo-50 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 hover:bg-indigo-100 dark:hover:bg-indigo-900/60 rounded-xl transition-all border border-indigo-100 dark:border-indigo-800 shadow-sm active:scale-95"
                    >
                        Today
                    </button>
                </div>
                <div className="flex items-center gap-3">
                    {/* Color mode selector */}
                    <div className="flex bg-slate-100 dark:bg-slate-800 rounded-xl p-1 border border-slate-200 dark:border-slate-700 shadow-sm">
                        {(['service', 'staff', 'status'] as ColorMode[]).map(m => (
                            <button 
                                key={m} 
                                onClick={() => setColorMode(m)} 
                                className={cn(
                                    'px-3 py-1.5 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all', 
                                    colorMode === m 
                                        ? 'bg-white dark:bg-slate-700 shadow-sm text-indigo-600 dark:text-white' 
                                        : 'text-slate-500 dark:text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
                                )}
                            >
                                {m}
                            </button>
                        ))}
                    </div>
                    <div className="flex bg-slate-100 dark:bg-slate-800 rounded-xl p-1 border border-slate-200 dark:border-slate-700 shadow-sm">
                        {(['day', 'week'] as const).map(v => (
                            <button 
                                key={v} 
                                onClick={() => setView(v)} 
                                className={cn(
                                    'px-4 py-1.5 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all', 
                                    view === v 
                                        ? 'bg-white dark:bg-slate-700 shadow-sm text-indigo-600 dark:text-white' 
                                        : 'text-slate-500 dark:text-slate-500 hover:text-slate-900 dark:hover:text-slate-300'
                                )}
                            >
                                {v}
                            </button>
                        ))}
                    </div>
                    <button onClick={fetchBookings} className="p-2.5 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-all text-slate-500 dark:text-slate-400 border border-transparent hover:border-slate-200 dark:hover:border-slate-700">
                        <RefreshCw className={cn('h-4 w-4', loading && 'animate-spin')} />
                    </button>
                    <Link href="/bookings/new" className="bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white px-5 py-2.5 rounded-xl font-bold flex items-center gap-2 text-xs uppercase tracking-widest shadow-lg shadow-indigo-500/25 transition-all hover:-translate-y-0.5 active:scale-95">
                        <Plus className="h-4 w-4" /> New Booking
                    </Link>
                </div>
            </div>

            {/* Legend */}
            {legend.length > 0 && (
                <div className="flex gap-4 flex-wrap px-1">
                    {legend.slice(0, 8).map(l => (
                        <div key={l.label} className="flex items-center gap-2 group cursor-default">
                            <div className="w-2.5 h-2.5 rounded-full shadow-sm ring-1 ring-black/5" style={{ backgroundColor: l.color }} />
                            <span className="text-[10px] font-bold uppercase tracking-widest text-slate-500 dark:text-slate-500 group-hover:text-slate-900 dark:group-hover:text-slate-300 transition-colors">{l.label}</span>
                        </div>
                    ))}
                </div>
            )}

            {/* Calendar grid */}
            <div className="flex-1 bg-white dark:bg-slate-900 rounded-2xl shadow-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                {/* Day headers */}
                <div className="grid grid-cols-8 border-b border-slate-200 dark:border-slate-800">
                    <div className="p-4 border-r border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/50" />
                    {weekDates.map((date, i) => (
                        <div key={i} className={cn('p-3 text-center border-r border-slate-200 dark:border-slate-800 last:border-r-0 transition-colors', isToday(date) ? 'bg-indigo-50/50 dark:bg-indigo-900/10' : 'bg-white dark:bg-slate-900')}>
                            <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500 mb-1">{weekDays[date.getDay()]}</p>
                            <div className="flex items-center justify-center">
                                <span className={cn(
                                    'w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all',
                                    isToday(date) 
                                        ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-500/30' 
                                        : 'text-slate-900 dark:text-white'
                                )}>
                                    {date.getDate()}
                                </span>
                            </div>
                        </div>
                    ))}
                </div>

                {/* Time grid */}
                <div className="overflow-y-auto scrollbar-thin dark:scrollbar-thumb-slate-800" style={{ height: 'calc(100% - 76px)' }}>
                    <div className="grid grid-cols-8 relative h-full">
                        {/* Time labels */}
                        <div className="border-r border-slate-200 dark:border-slate-800 bg-slate-50/30 dark:bg-slate-950/30">
                            {hours.map(hour => (
                                <div key={hour} className="h-[60px] border-b border-slate-100 dark:border-slate-800/50 px-3 py-2 text-[10px] font-bold text-slate-400 dark:text-slate-600 uppercase tracking-tighter text-right">
                                    {hour > 12 ? `${hour - 12}pm` : hour === 12 ? '12pm' : `${hour}am`}
                                </div>
                            ))}
                        </div>

                        {/* Day columns */}
                        {weekDates.map((date, dayIndex) => (
                            <div key={dayIndex} className={cn('relative border-r border-slate-200 dark:border-slate-800 last:border-r-0 transition-colors', isToday(date) && 'bg-indigo-50/20 dark:bg-indigo-900/5')}>
                                {hours.map(hour => (
                                    <div key={hour} className="h-[60px] border-b border-slate-100 dark:border-slate-800/50 hover:bg-slate-50/50 dark:hover:bg-slate-800/20 transition-colors" />
                                ))}

                                {getEventsForDate(date).map(event => (
                                    <Link
                                        key={event.id}
                                        href={`/bookings/${event.id}`}
                                        className="absolute left-1 right-1 rounded-xl px-2.5 py-1.5 text-xs text-white overflow-hidden cursor-pointer hover:shadow-xl hover:-translate-y-0.5 active:scale-[0.98] transition-all group z-10 border border-white/20 dark:border-black/20"
                                        style={{
                                            backgroundColor: event.color,
                                            boxShadow: `0 4px 12px ${event.color}33`,
                                            top: getEventPosition(event.startTime),
                                            height: Math.max(getEventHeight(event.startTime, event.endTime) - 4, 24),
                                        }}
                                    >
                                        <p className="font-bold truncate leading-tight tracking-tight drop-shadow-sm">{event.title}</p>
                                        <p className="font-medium opacity-90 truncate text-[10px] drop-shadow-sm">{event.clientName}</p>
                                        {getEventHeight(event.startTime, event.endTime) > 40 && (
                                            <div className="flex items-center gap-1.5 mt-1 opacity-80 text-[10px] font-bold">
                                                <Clock className="w-2.5 h-2.5" />
                                                <span>{event.startTime}</span>
                                            </div>
                                        )}
                                        {event.staffName && getEventHeight(event.startTime, event.endTime) > 65 && (
                                            <div className="flex items-center gap-1.5 mt-1 opacity-80 text-[10px] truncate font-bold">
                                                <User className="w-2.5 h-2.5" />
                                                <span className="truncate">{event.staffName}</span>
                                            </div>
                                        )}
                                        {/* Subtle inner highlight */}
                                        <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity" />
                                    </Link>
                                ))}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
