"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Calendar, Plus, Clock, Monitor, ChevronLeft, ChevronRight, X, Loader2 } from 'lucide-react';
import api from '@/lib/api';

interface ResourceInfo {
  id: string; name: string; type: string; color?: string; capacity: number;
}

interface ResourceBookingData {
  id: string; resourceId: string; title: string; startTime: string; endTime: string; status: string; bookedByUserId?: string;
}

export default function ResourceSchedulePage() {
  const [resources, setResources] = useState<ResourceInfo[]>([]);
  const [bookings, setBookings] = useState<ResourceBookingData[]>([]);
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(true);
  const [showBookModal, setShowBookModal] = useState(false);
  const [selectedResource, setSelectedResource] = useState<string>('');
  const [bookingForm, setBookingForm] = useState({ title: '', startTime: '', endTime: '', notes: '' });
  const [booking, setBooking] = useState(false);

  const hours = Array.from({ length: 12 }, (_, i) => i + 8); // 8AM - 7PM

  const loadSchedule = useCallback(async () => {
    try {
      setLoading(true);
      const res = await api.resourceScheduling.getSchedule(selectedDate);
      setResources(res.data?.resources || []);
      setBookings(res.data?.bookings || []);
    } catch (err) {
      console.error('Failed to load schedule:', err);
    } finally {
      setLoading(false);
    }
  }, [selectedDate]);

  useEffect(() => { loadSchedule(); }, [loadSchedule]);

  const changeDate = (delta: number) => {
    const d = new Date(selectedDate);
    d.setDate(d.getDate() + delta);
    setSelectedDate(d.toISOString().split('T')[0]);
  };

  const openBooking = (resourceId: string, hour: number) => {
    setSelectedResource(resourceId);
    const start = `${selectedDate}T${String(hour).padStart(2, '0')}:00:00`;
    const end = `${selectedDate}T${String(hour + 1).padStart(2, '0')}:00:00`;
    setBookingForm({ title: '', startTime: start, endTime: end, notes: '' });
    setShowBookModal(true);
  };

  const handleBook = async () => {
    if (!selectedResource) return;
    setBooking(true);
    try {
      await api.resourceScheduling.book(selectedResource, {
        title: bookingForm.title || undefined,
        startTime: bookingForm.startTime,
        endTime: bookingForm.endTime,
        notes: bookingForm.notes || undefined,
      });
      setShowBookModal(false);
      loadSchedule();
    } catch (err: any) {
      alert(err.response?.data?.error || 'Failed to book resource');
    } finally {
      setBooking(false);
    }
  };

  const cancelBooking = async (resourceId: string, bookingId: string) => {
    if (!confirm('Cancel this booking?')) return;
    try {
      await api.resourceScheduling.cancelBooking(resourceId, bookingId);
      loadSchedule();
    } catch (err) {
      console.error('Failed to cancel:', err);
    }
  };

  const getBookingsForSlot = (resourceId: string, hour: number) => {
    const slotStart = new Date(`${selectedDate}T${String(hour).padStart(2, '0')}:00:00`);
    const slotEnd = new Date(`${selectedDate}T${String(hour + 1).padStart(2, '0')}:00:00`);
    return bookings.filter(b => {
      const bStart = new Date(b.startTime);
      const bEnd = new Date(b.endTime);
      return b.resourceId === resourceId && bStart < slotEnd && bEnd > slotStart;
    });
  };

  const resourceColorMap: Record<string, string> = {};
  const defaultColors = ['bg-violet-100 border-violet-300 text-violet-800', 'bg-emerald-100 border-emerald-300 text-emerald-800', 'bg-blue-100 border-blue-300 text-blue-800', 'bg-amber-100 border-amber-300 text-amber-800', 'bg-pink-100 border-pink-300 text-pink-800', 'bg-teal-100 border-teal-300 text-teal-800'];
  resources.forEach((r, i) => { resourceColorMap[r.id] = defaultColors[i % defaultColors.length]; });

  const dateObj = new Date(selectedDate + 'T00:00:00');
  const dateLabel = dateObj.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Resource Schedule</h1>
          <p className="text-foreground-secondary mt-1">View and manage room, equipment, and vehicle reservations</p>
        </div>
        <button onClick={() => { setSelectedResource(resources[0]?.id || ''); setShowBookModal(true); }}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-blue-500 to-primary-600 text-white rounded-xl font-medium shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 transition-all">
          <Plus className="w-4 h-4" /> New Booking
        </button>
      </div>

      {/* Date Navigation */}
      <div className="flex items-center gap-4">
        <button onClick={() => changeDate(-1)} className="p-2 rounded-lg border border-border hover:bg-accent">
          <ChevronLeft className="w-5 h-5" />
        </button>
        <div className="flex items-center gap-2">
          <Calendar className="w-5 h-5 text-blue-500" />
          <span className="text-lg font-semibold text-foreground">{dateLabel}</span>
        </div>
        <button onClick={() => changeDate(1)} className="p-2 rounded-lg border border-border hover:bg-accent">
          <ChevronRight className="w-5 h-5" />
        </button>
        <button onClick={() => setSelectedDate(new Date().toISOString().split('T')[0])}
          className="ml-2 px-3 py-1.5 text-sm border border-border-strong rounded-lg hover:bg-accent text-foreground">
          Today
        </button>
      </div>

      {/* Schedule Grid */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : resources.length === 0 ? (
        <div className="bg-card rounded-xl border border-border p-12 text-center">
          <Monitor className="w-12 h-12 text-slate-300 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-foreground">No resources available</h3>
          <p className="text-foreground-secondary mt-1">Create resources first to manage their schedules.</p>
        </div>
      ) : (
        <div className="bg-card rounded-xl border border-border shadow-sm overflow-x-auto">
          <table className="w-full min-w-[800px]">
            <thead>
              <tr className="border-b border-border">
                <th className="px-4 py-3 text-left text-sm font-semibold text-foreground-secondary w-40 bg-muted sticky left-0">Resource</th>
                {hours.map(h => (
                  <th key={h} className="px-2 py-3 text-center text-xs font-medium text-foreground-secondary min-w-[80px]">
                    {h > 12 ? `${h - 12} PM` : h === 12 ? '12 PM' : `${h} AM`}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {resources.map((resource) => (
                <tr key={resource.id} className="border-b border-border-subtle hover:bg-muted/50">
                  <td className="px-4 py-3 bg-muted sticky left-0">
                    <div className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-full" style={{ backgroundColor: resource.color || '#8b5cf6' }} />
                      <div>
                        <p className="text-sm font-medium text-foreground">{resource.name}</p>
                        <p className="text-xs text-foreground-muted capitalize">{resource.type} · Cap: {resource.capacity}</p>
                      </div>
                    </div>
                  </td>
                  {hours.map(h => {
                    const slotBookings = getBookingsForSlot(resource.id, h);
                    const isBooked = slotBookings.length > 0;
                    return (
                      <td key={h} className="px-1 py-2">
                        {isBooked ? (
                          <div
                            className={`rounded-lg border px-2 py-1 text-xs font-medium cursor-pointer ${resourceColorMap[resource.id]}`}
                            onClick={() => cancelBooking(resource.id, slotBookings[0].id)}
                            title={`${slotBookings[0].title} — Click to cancel`}
                          >
                            <span className="truncate block">{slotBookings[0].title}</span>
                          </div>
                        ) : (
                          <button
                            onClick={() => openBooking(resource.id, h)}
                            className="w-full h-8 rounded-lg border border-dashed border-border hover:border-blue-400 hover:bg-blue-50/50 transition-colors"
                            title={`Book ${resource.name} at ${h}:00`}
                          />
                        )}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Legend */}
      {resources.length > 0 && (
        <div className="flex flex-wrap items-center gap-4 text-sm text-foreground-secondary">
          <span className="font-medium">Legend:</span>
          <div className="flex items-center gap-1">
            <div className="w-6 h-4 rounded border border-dashed border-border-strong" />
            <span>Available</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-6 h-4 rounded bg-brand-subtle border border-primary-300" />
            <span>Booked</span>
          </div>
        </div>
      )}

      {/* Book Modal */}
      {showBookModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setShowBookModal(false)}>
          <div className="bg-card rounded-2xl p-6 w-full max-w-md shadow-2xl" onClick={e => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-xl font-bold">Book Resource</h2>
              <button onClick={() => setShowBookModal(false)} className="text-foreground-muted hover:text-foreground-secondary">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Resource</label>
                <select value={selectedResource} onChange={e => setSelectedResource(e.target.value)}
                  className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-blue-500 outline-none">
                  {resources.map(r => <option key={r.id} value={r.id}>{r.name} ({r.type})</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Title</label>
                <input type="text" value={bookingForm.title} onChange={e => setBookingForm({ ...bookingForm, title: e.target.value })}
                  className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" placeholder="Booking title" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Start</label>
                  <input type="datetime-local" value={bookingForm.startTime.slice(0, 16)} onChange={e => setBookingForm({ ...bookingForm, startTime: e.target.value + ':00' })}
                    className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-sm" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">End</label>
                  <input type="datetime-local" value={bookingForm.endTime.slice(0, 16)} onChange={e => setBookingForm({ ...bookingForm, endTime: e.target.value + ':00' })}
                    className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-sm" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Notes</label>
                <textarea value={bookingForm.notes} onChange={e => setBookingForm({ ...bookingForm, notes: e.target.value })}
                  className="w-full px-3 py-2 border border-border-strong rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" rows={2} />
              </div>
            </div>
            <div className="flex gap-3 mt-6">
              <button onClick={() => setShowBookModal(false)} className="flex-1 px-4 py-2 border border-border-strong rounded-lg text-foreground hover:bg-accent">Cancel</button>
              <button onClick={handleBook} disabled={booking}
                className="flex-1 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 inline-flex items-center justify-center gap-2">
                {booking ? <Loader2 className="w-4 h-4 animate-spin" /> : <Clock className="w-4 h-4" />}
                Book
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
