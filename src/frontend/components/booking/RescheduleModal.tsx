'use client';

import React, { useState, useEffect } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Calendar as CalendarIcon, Clock, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react';
import { apiClient } from '@/lib/api';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';

interface RescheduleModalProps {
    isOpen: boolean;
    onClose: () => void;
    booking: {
        id: string;
        tenantSlug: string;
        serviceId: string;
        staffId: string;
        service: string;
        staff: string;
        duration: number;
        date: string;
        time: string;
    } | null;
    confirmationCode?: string;
    onSuccess: (newDate: string, newTime: string) => void;
}

export function RescheduleModal({ isOpen, onClose, booking, confirmationCode, onSuccess }: RescheduleModalProps) {
    const { addToast } = useToast();
    const [selectedDate, setSelectedDate] = useState<Date>(new Date());
    const [availableSlots, setAvailableSlots] = useState<string[]>([]);
    const [selectedTime, setSelectedTime] = useState<string | null>(null);
    const [loadingSlots, setLoadingSlots] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [currentMonth, setCurrentMonth] = useState(new Date());

    useEffect(() => {
        if (isOpen && booking) {
            // Default to tomorrow or current booking date if in future
            const bookingDate = new Date(booking.date);
            if (bookingDate > new Date()) {
                setSelectedDate(bookingDate);
                setCurrentMonth(new Date(bookingDate.getFullYear(), bookingDate.getMonth(), 1));
            } else {
                const tomorrow = new Date();
                tomorrow.setDate(tomorrow.getDate() + 1);
                setSelectedDate(tomorrow);
                setCurrentMonth(new Date(tomorrow.getFullYear(), tomorrow.getMonth(), 1));
            }
            setSelectedTime(null);
        }
    }, [isOpen, booking]);

    useEffect(() => {
        if (!isOpen || !booking || !selectedDate) return;

        const fetchSlots = async () => {
            setLoadingSlots(true);
            try {
                const dateStr = selectedDate.toISOString().split('T')[0];
                const res = await apiClient.get(`/api/booking/${booking.tenantSlug}/availability`, {
                    params: {
                        serviceId: booking.serviceId,
                        staffId: booking.staffId,
                        date: dateStr
                    }
                });
                setAvailableSlots(res.data || []); // Public controller returns List<DateTime> which becomes list of strings
            } catch (err) {
                console.error('Failed to load slots', err);
                setAvailableSlots([]);
            } finally {
                setLoadingSlots(false);
            }
        };

        fetchSlots();
    }, [selectedDate, booking, isOpen]);

    const handleReschedule = async () => {
        if (!booking || !selectedDate || !selectedTime) return;

        setSubmitting(true);
        try {
            const dateStr = selectedDate.toISOString().split('T')[0];
            const headers = { 'Authorization': `Bearer ${localStorage.getItem('client_token')}` };
            
            await apiClient.post(`/api/booking/${booking.tenantSlug}/reschedule/${booking.id}`, {
                date: dateStr,
                time: selectedTime,
                confirmationCode: confirmationCode
            }, { headers });

            addToast('Appointment rescheduled successfully', 'success');
            onSuccess(dateStr, selectedTime);
            onClose();
        } catch (err: any) {
            addToast(err.response?.data?.error || 'Failed to reschedule', 'error');
        } finally {
            setSubmitting(false);
        }
    };

    // Calendar Helpers
    const getDaysInMonth = (date: Date) => {
        const year = date.getFullYear();
        const month = date.getMonth();
        const days = new Date(year, month + 1, 0).getDate();
        const firstDay = new Date(year, month, 1).getDay();
        return { days, firstDay };
    };

    const { days, firstDay } = getDaysInMonth(currentMonth);
    const daysArray = Array.from({ length: days }, (_, i) => i + 1);
    const monthName = currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });

    const isToday = (day: number) => {
        const today = new Date();
        return today.getDate() === day && today.getMonth() === currentMonth.getMonth() && today.getFullYear() === currentMonth.getFullYear();
    };

    const isSelected = (day: number) => {
        return selectedDate.getDate() === day && selectedDate.getMonth() === currentMonth.getMonth() && selectedDate.getFullYear() === currentMonth.getFullYear();
    };

    const isPast = (day: number) => {
        const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
        const today = new Date();
        today.setHours(0,0,0,0);
        return date < today;
    };

    return (
        <Modal 
            isOpen={isOpen} 
            onClose={onClose} 
            title="Reschedule Appointment" 
            description={`Pick a new time for your ${booking?.service} session`}
            size="lg"
        >
            <div className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    {/* Calendar Section */}
                    <div className="space-y-4">
                        <div className="flex items-center justify-between px-2">
                            <h3 className="font-bold text-slate-900">{monthName}</h3>
                            <div className="flex gap-1">
                                <Button variant="outline" size="sm" className="h-8 w-8 p-0" onClick={() => {
                                    const d = new Date(currentMonth);
                                    d.setMonth(d.getMonth() - 1);
                                    setCurrentMonth(d);
                                }}>
                                    <ChevronLeft className="h-4 w-4" />
                                </Button>
                                <Button variant="outline" size="sm" className="h-8 w-8 p-0" onClick={() => {
                                    const d = new Date(currentMonth);
                                    d.setMonth(d.getMonth() + 1);
                                    setCurrentMonth(d);
                                }}>
                                    <ChevronRight className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>

                        <div className="grid grid-cols-7 gap-1 text-center">
                            {['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'].map(d => (
                                <span key={d} className="text-[10px] font-bold text-slate-400 uppercase py-2">{d}</span>
                            ))}
                            {Array.from({ length: firstDay }).map((_, i) => (
                                <div key={`empty-${i}`} />
                            ))}
                            {daysArray.map(day => (
                                <button
                                    key={day}
                                    disabled={isPast(day)}
                                    onClick={() => setSelectedDate(new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day))}
                                    className={cn(
                                        "h-9 w-9 rounded-lg text-sm font-semibold flex items-center justify-center transition-all",
                                        isSelected(day) ? "bg-primary text-white shadow-lg shadow-primary/30" : 
                                        isToday(day) ? "bg-slate-100 text-primary" : "text-slate-600 hover:bg-slate-50",
                                        isPast(day) && "opacity-20 cursor-not-allowed text-slate-300"
                                    )}
                                >
                                    {day}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Slots Section */}
                    <div className="space-y-4 border-l pl-8 border-slate-100">
                        <h3 className="font-bold text-slate-900 flex items-center gap-2">
                            <Clock className="h-4 w-4 text-slate-400" />
                            Available Times
                        </h3>
                        
                        <div className="h-[280px] overflow-y-auto pr-2 custom-scrollbar">
                            {loadingSlots ? (
                                <div className="flex flex-col items-center justify-center h-full text-slate-400 gap-2">
                                    <Loader2 className="h-6 w-6 animate-spin" />
                                    <span className="text-xs font-medium">Checking availability...</span>
                                </div>
                            ) : availableSlots.length > 0 ? (
                                <div className="grid grid-cols-2 gap-2 pb-4">
                                    {availableSlots.map(slot => (
                                        <button
                                            key={slot}
                                            onClick={() => setSelectedTime(slot)}
                                            className={cn(
                                                "py-2 px-3 rounded-lg text-sm font-bold border transition-all",
                                                selectedTime === slot ? "bg-primary border-primary text-white shadow-md shadow-primary/20" : "bg-white border-slate-200 text-slate-700 hover:border-primary/50 hover:bg-slate-50"
                                            )}
                                        >
                                            {slot}
                                        </button>
                                    ))}
                                </div>
                            ) : (
                                <div className="flex flex-col items-center justify-center h-full text-slate-400 text-center px-4">
                                    <CalendarIcon className="h-8 w-8 mb-2 opacity-20" />
                                    <p className="text-xs font-medium">No slots available for this date.</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-100">
                    <Button variant="outline" onClick={onClose} disabled={submitting} className="font-bold">
                        Cancel
                    </Button>
                    <Button 
                        disabled={!selectedTime || submitting} 
                        onClick={handleReschedule}
                        className="font-bold min-w-[140px]"
                    >
                        {submitting ? (
                            <Loader2 className="h-4 w-4 animate-spin mr-2" />
                        ) : 'Confirm Changes'}
                    </Button>
                </div>
            </div>
        </Modal>
    );
}
