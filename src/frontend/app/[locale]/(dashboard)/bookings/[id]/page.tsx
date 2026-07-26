'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    Calendar,
    Clock,
    User,
    Save,
    Trash2,
    X,
    Check,
    AlertTriangle,
    MessageSquare,
    Star,
    RefreshCw,
} from 'lucide-react';
import { cn, formatCurrency, formatDate, formatTime } from '@/lib/utils';
import api, { apiClient } from '@/lib/api';
import { toast } from 'sonner';

type BookingStatus = 'confirmed' | 'pending' | 'completed' | 'cancelled' | 'no-show';

interface BookingData {
    id: string;
    clientName: string;
    clientEmail: string;
    clientPhone: string;
    serviceName: string;
    serviceColor: string;
    servicePrice: number;
    serviceDuration: number;
    staffName: string;
    staffId: string;
    date: string;
    time: string;
    status: BookingStatus;
    notes: string;
    createdAt: string;
}

interface StaffMember {
    id: string;
    name: string;
    firstName?: string;
    lastName?: string;
}

const statusOptions: { value: BookingStatus; label: string; color: string; icon: typeof Check }[] = [
    { value: 'confirmed', label: 'Confirmed', color: 'bg-emerald-500', icon: Check },
    { value: 'pending', label: 'Pending', color: 'bg-amber-500', icon: Clock },
    { value: 'completed', label: 'Completed', color: 'bg-blue-500', icon: Star },
    { value: 'cancelled', label: 'Cancelled', color: 'bg-red-500', icon: X },
    { value: 'no-show', label: 'No Show', color: 'bg-slate-500', icon: AlertTriangle },
];

export default function EditBookingPage() {
    const router = useRouter();
    const params = useParams();
    const bookingId = params.id as string;

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [booking, setBooking] = useState<BookingData | null>(null);
    const [staff, setStaff] = useState<StaffMember[]>([]);
    const [timeline, setTimeline] = useState<{ event: string; description?: string; timestamp: string; actor?: string }[]>([]);
    const [formData, setFormData] = useState({
        date: '',
        time: '',
        status: 'confirmed' as BookingStatus,
        notes: '',
        staffId: '',
    });

    useEffect(() => {
        const fetchBooking = async () => {
            setLoading(true);
            setError(null);
            try {
                const [bookingRes, staffRes] = await Promise.all([
                    api.bookings.get(bookingId),
                    api.staff.list(),
                ]);
                const b = bookingRes.data?.data || bookingRes.data;
                const staffList = (staffRes.data?.data || staffRes.data || []).map((s: any) => ({
                    id: s.id,
                    name: s.name || `${s.firstName || ''} ${s.lastName || ''}`.trim(),
                }));
                setStaff(staffList);

                const startTime = b.startTime ? new Date(b.startTime) : new Date();
                const endTime = b.endTime ? new Date(b.endTime) : new Date();
                const durationMin = Math.round((endTime.getTime() - startTime.getTime()) / 60000);

                const bookingData: BookingData = {
                    id: b.id,
                    clientName: b.clientName || `${b.client?.firstName || ''} ${b.client?.lastName || ''}`.trim() || 'Unknown',
                    clientEmail: b.clientEmail || b.client?.email || '',
                    clientPhone: b.clientPhone || b.client?.phone || b.client?.phoneNumber || '',
                    serviceName: b.serviceName || b.service?.name || 'Service',
                    serviceColor: b.serviceColor || b.service?.color || '#8B5CF6',
                    servicePrice: b.price || b.totalAmount || b.service?.price || 0,
                    serviceDuration: b.duration || durationMin || b.service?.duration || 30,
                    staffName: b.staffName || b.staff?.name || `${b.staff?.firstName || ''} ${b.staff?.lastName || ''}`.trim() || 'Unassigned',
                    staffId: b.staffId || b.staff?.id || '',
                    date: startTime.toISOString().split('T')[0],
                    time: startTime.toTimeString().slice(0, 5),
                    status: (b.status || 'pending').toLowerCase().replace('_', '-') as BookingStatus,
                    notes: b.notes || '',
                    createdAt: b.createdAt || new Date().toISOString(),
                };

                setBooking(bookingData);
                setFormData({
                    date: bookingData.date,
                    time: bookingData.time,
                    status: bookingData.status,
                    notes: bookingData.notes,
                    staffId: bookingData.staffId,
                });
            } catch (err: any) {
                console.error('Failed to fetch booking:', err);
                setError('Failed to load booking details');
                toast.error('Failed to load booking');
            } finally {
                setLoading(false);
            }
        };

        if (bookingId) fetchBooking();
    }, [bookingId]);

    useEffect(() => {
        if (!bookingId) return;
        apiClient.get(`/api/v1/bookings/${bookingId}/timeline`).catch(() => ({ data: [] })).then((r) => {
            setTimeline(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
        });
    }, [bookingId]);

    const handleSave = async () => {
        setSaving(true);
        try {
            await api.bookings.update(bookingId, {
                status: formData.status,
                staffId: formData.staffId,
                date: formData.date,
                startTime: `${formData.date}T${formData.time}:00`,
                notes: formData.notes,
            });
            toast.success('Booking updated successfully');
            router.push('/bookings');
        } catch (err: any) {
            console.error('Failed to update booking:', err);
            toast.error(err.response?.data?.message || 'Failed to update booking');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        setSaving(true);
        try {
            await api.bookings.cancel(bookingId, 'Deleted by admin');
            toast.success('Booking cancelled successfully');
            router.push('/bookings');
        } catch (err: any) {
            console.error('Failed to delete booking:', err);
            toast.error(err.response?.data?.message || 'Failed to cancel booking');
        } finally {
            setSaving(false);
        }
    };

    const timeSlots = [
        '09:00', '09:30', '10:00', '10:30', '11:00', '11:30',
        '12:00', '12:30', '13:00', '13:30', '14:00', '14:30',
        '15:00', '15:30', '16:00', '16:30', '17:00',
    ];

    if (loading) {
        return (
            <div className="max-w-3xl mx-auto animate-pulse">
                <div className="h-8 bg-slate-200 rounded w-1/3 mb-6" />
                <div className="card-elevated p-6 space-y-4">
                    <div className="h-20 bg-slate-200 rounded" />
                    <div className="h-40 bg-slate-200 rounded" />
                </div>
            </div>
        );
    }

    if (!booking) {
        return (
            <div className="text-center py-20">
                <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto mb-4" />
                <h2 className="text-xl font-semibold text-slate-900">Booking Not Found</h2>
                <p className="text-slate-500 mt-2">This booking may have been deleted.</p>
                <Link href="/bookings" className="btn btn-primary mt-6">
                    Back to Bookings
                </Link>
            </div>
        );
    }

    return (
        <div className="max-w-3xl mx-auto">
            {/* Header */}
            <div className="flex items-center justify-between gap-4 mb-8 animate-fade-in-up">
                <div className="flex items-center gap-4">
                    <Link href="/bookings" className="p-2 hover:bg-slate-100 rounded-xl transition-colors">
                        <ArrowLeft className="h-5 w-5 text-slate-600" />
                    </Link>
                    <div>
                        <div className="flex items-center gap-3 mb-1">
                            <div
                                className="p-2 rounded-xl shadow-lg"
                                style={{ backgroundColor: booking.serviceColor }}
                            >
                                <Calendar className="h-5 w-5 text-white" />
                            </div>
                            <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'Outfit, sans-serif' }}>
                                Edit Booking
                            </h1>
                        </div>
                        <p className="text-slate-500 ml-12">Booking #{bookingId.slice(0, 8)}</p>
                    </div>
                </div>
                <button
                    onClick={() => setShowDeleteModal(true)}
                    className="btn btn-secondary text-red-600 hover:bg-red-50 hover:border-red-200"
                >
                    <Trash2 className="h-4 w-4" />
                    Delete
                </button>
            </div>

            {/* Client & Service Info (Read-only) */}
            <div className="card-elevated p-6 mb-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                <div className="flex items-start gap-6">
                    <div
                        className="w-16 h-16 rounded-xl flex items-center justify-center text-white font-bold text-xl"
                        style={{ backgroundColor: booking.serviceColor }}
                    >
                        {booking.clientName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div className="flex-1">
                        <h2 className="text-lg font-semibold text-slate-900">{booking.clientName}</h2>
                        <p className="text-slate-500">{booking.clientEmail}</p>
                        <p className="text-slate-500">{booking.clientPhone}</p>
                    </div>
                    <div className="text-right">
                        <p className="font-semibold text-slate-900" style={{ color: booking.serviceColor }}>
                            {booking.serviceName}
                        </p>
                        <p className="text-slate-500">{booking.serviceDuration} min • {formatCurrency(booking.servicePrice)}</p>
                    </div>
                </div>
            </div>

            {/* Editable Fields */}
            <div className="space-y-6">
                {/* Status */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                    <h3 className="text-sm font-medium text-slate-700 mb-4">Booking Status</h3>
                    <div className="flex flex-wrap gap-2">
                        {statusOptions.map((option) => {
                            const Icon = option.icon;
                            return (
                                <button
                                    key={option.value}
                                    onClick={() => setFormData({ ...formData, status: option.value })}
                                    className={cn(
                                        'flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all',
                                        formData.status === option.value
                                            ? `${option.color} text-white shadow-lg`
                                            : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                    )}
                                >
                                    <Icon className="h-4 w-4" />
                                    {option.label}
                                </button>
                            );
                        })}
                    </div>
                </div>

                {/* Date, Time & Staff */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                    <div className="flex items-center gap-3 mb-6">
                        <RefreshCw className="h-5 w-5 text-primary-500" />
                        <h3 className="text-lg font-semibold text-slate-900">Reschedule</h3>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-2">Date</label>
                            <input
                                type="date"
                                value={formData.date}
                                onChange={(e) => setFormData({ ...formData, date: e.target.value })}
                                className="input"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-2">Time</label>
                            <select
                                value={formData.time}
                                onChange={(e) => setFormData({ ...formData, time: e.target.value })}
                                className="input"
                            >
                                {timeSlots.map((slot) => (
                                    <option key={slot} value={slot}>{slot}</option>
                                ))}
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-2">Staff</label>
                            <select
                                value={formData.staffId}
                                onChange={(e) => setFormData({ ...formData, staffId: e.target.value })}
                                className="input"
                            >
                                {staff.map((member) => (
                                    <option key={member.id} value={member.id}>{member.name}</option>
                                ))}
                            </select>
                        </div>
                    </div>
                </div>

                {/* Notes */}
                <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                    <div className="flex items-center gap-3 mb-4">
                        <MessageSquare className="h-5 w-5 text-slate-400" />
                        <h3 className="text-lg font-semibold text-slate-900">Booking Notes</h3>
                    </div>
                    <textarea
                        value={formData.notes}
                        onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                        className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-all resize-none"
                        rows={4}
                        placeholder="Add notes about this booking..."
                    />
                </div>

                {/* Timeline */}
                {timeline.length > 0 && (
                    <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '450ms' }}>
                        <div className="flex items-center gap-3 mb-4">
                            <RefreshCw className="h-5 w-5 text-slate-400" />
                            <h3 className="text-lg font-semibold text-slate-900">History</h3>
                        </div>
                        <div className="space-y-3">
                            {timeline.map((t, i) => (
                                <div key={i} className="flex items-start gap-3">
                                    <div className="w-2 h-2 rounded-full bg-primary-500 mt-1.5 flex-shrink-0" />
                                    <div className="flex-1 min-w-0">
                                        <p className="text-sm font-medium text-slate-900">{t.event}</p>
                                        {t.description && <p className="text-xs text-slate-500">{t.description}</p>}
                                        <p className="text-xs text-slate-400 mt-0.5">
                                            {new Date(t.timestamp).toLocaleString()}{t.actor ? ` · ${t.actor}` : ''}
                                        </p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* Actions */}
                <div className="flex items-center justify-between pt-4 animate-fade-in" style={{ animationDelay: '500ms' }}>
                    <Link href="/bookings" className="btn btn-secondary">
                        Cancel
                    </Link>
                    <button
                        onClick={handleSave}
                        disabled={saving}
                        className="btn btn-primary"
                    >
                        {saving ? (
                            <>
                                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                Saving...
                            </>
                        ) : (
                            <>
                                <Save className="h-4 w-4" />
                                Save Changes
                            </>
                        )}
                    </button>
                </div>
            </div>

            {/* Delete Modal */}
            {showDeleteModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 animate-fade-in">
                    <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 animate-fade-in-up">
                        <div className="flex items-center gap-4 mb-4">
                            <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center">
                                <AlertTriangle className="h-6 w-6 text-red-600" />
                            </div>
                            <div>
                                <h3 className="text-lg font-semibold text-slate-900">Delete Booking</h3>
                                <p className="text-slate-500">This action cannot be undone.</p>
                            </div>
                        </div>
                        <p className="text-slate-600 mb-6">
                            Are you sure you want to delete this booking for <strong>{booking.clientName}</strong>?
                        </p>
                        <div className="flex gap-3">
                            <button
                                onClick={() => setShowDeleteModal(false)}
                                className="btn btn-secondary flex-1"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleDelete}
                                disabled={saving}
                                className="btn bg-red-500 text-white hover:bg-red-600 flex-1"
                            >
                                {saving ? 'Deleting...' : 'Delete'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
