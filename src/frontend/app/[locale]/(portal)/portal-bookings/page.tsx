'use client';

import React, { useEffect, useState } from 'react';
import {
    Calendar,
    Clock,
    MapPin,
    MoreVertical,
    XCircle,
    ChevronRight,
    Search,
    Filter,
    History
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { apiClient } from '@/lib/api';
import { formatCurrency, cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';
import { RescheduleModal } from '@/components/booking/RescheduleModal';

export default function CustomerBookingsPage() {
    const { addToast } = useToast();
    const [upcoming, setUpcoming] = useState<any[]>([]);
    const [history, setHistory] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'upcoming' | 'history'>('upcoming');

    // Reschedule State
    const [isRescheduleOpen, setIsRescheduleOpen] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState<any>(null);

    const fetchData = async () => {
        setLoading(true);
        try {
            const headers = {
                'Authorization': `Bearer ${localStorage.getItem('client_token')}`
            };

            const [upcomingRes, historyRes] = await Promise.all([
                apiClient.get('/api/client-portal/appointments/upcoming', { headers }),
                apiClient.get('/api/client-portal/appointments/history', { headers })
            ]);

            setUpcoming(upcomingRes.data.data || []);
            setHistory(historyRes.data.data || []);
        } catch (err) {
            console.error('Failed to load bookings', err);
            addToast('Failed to load bookings', 'error');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, [addToast]);

    const handleCancel = async (id: string) => {
        if (!confirm('Are you sure you want to cancel this appointment?')) return;

        try {
            const headers = { 'Authorization': `Bearer ${localStorage.getItem('client_token')}` };
            await apiClient.post(`/api/client-portal/appointments/${id}/cancel`, { reason: 'Cancelled by user' }, { headers });
            addToast('Appointment cancelled successfully', 'success');
            // Refresh
            setUpcoming(prev => prev.filter(a => a.id !== id));
        } catch (err: any) {
            addToast(err.response?.data?.error || 'Failed to cancel appointment', 'error');
        }
    };

    const handleRescheduleClick = (appt: any) => {
        setSelectedBooking(appt);
        setIsRescheduleOpen(true);
    };

    const handleRescheduleSuccess = (newDate: string, newTime: string) => {
        addToast('Refreshing your schedule...', 'info');
        fetchData();
    };

    if (loading) {
        return (
            <div className="space-y-6">
                <div className="h-10 w-48 bg-muted rounded-lg animate-pulse" />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {[1, 2].map(i => (
                        <div key={i} className="h-48 bg-muted rounded-2xl animate-pulse" />
                    ))}
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-8 max-w-4xl mx-auto">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-black text-foreground tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                        My Appointments
                    </h1>
                    <p className="text-foreground-secondary mt-1">Manage your sessions and view past visits</p>
                </div>
                <div className="flex bg-muted p-1 rounded-xl w-fit">
                    <button
                        onClick={() => setActiveTab('upcoming')}
                        className={cn(
                            "px-6 py-2 rounded-lg text-sm font-bold transition-all",
                            activeTab === 'upcoming' ? "bg-card text-primary shadow-sm" : "text-foreground-secondary hover:text-foreground"
                        )}
                    >
                        Upcoming
                    </button>
                    <button
                        onClick={() => setActiveTab('history')}
                        className={cn(
                            "px-6 py-2 rounded-lg text-sm font-bold transition-all",
                            activeTab === 'history' ? "bg-card text-primary shadow-sm" : "text-foreground-secondary hover:text-foreground"
                        )}
                    >
                        History
                    </button>
                </div>
            </div>

            {activeTab === 'upcoming' ? (
                upcoming.length > 0 ? (
                    <div className="grid grid-cols-1 gap-6">
                        {upcoming.map((appt, i) => (
                            <Card
                                key={appt.id}
                                className="p-0 overflow-hidden border-none shadow-xl shadow-slate-200/50 hover:shadow-slate-300 transition-all group animate-fade-in-up"
                                style={{ animationDelay: `${i * 100}ms` }}
                            >
                                <div className="flex flex-col md:flex-row">
                                    <div className="w-full md:w-2 bg-gradient-to-b from-primary to-primary-500" />
                                    <div className="flex-1 p-6 flex flex-col md:flex-row md:items-center justify-between gap-6">
                                        <div className="space-y-4">
                                            <div className="flex items-center gap-2">
                                                <Badge variant="outline" className="bg-primary/5 text-primary border-primary/10 px-3 py-1 font-bold">
                                                    {appt.status.toUpperCase()}
                                                </Badge>
                                                <span className="text-xs text-foreground-muted font-medium">#{appt.id.split('-')[0].toUpperCase()}</span>
                                            </div>
                                            <div>
                                                <h3 className="text-xl font-bold text-foreground mb-1">{appt.service}</h3>
                                                <p className="text-foreground-secondary font-medium flex items-center gap-1.5">
                                                    with <span className="text-foreground">{appt.staff}</span>
                                                </p>
                                            </div>
                                            <div className="flex flex-wrap items-center gap-y-2 gap-x-6">
                                                <div className="flex items-center gap-2 text-foreground-secondary">
                                                    <div className="p-1.5 bg-muted rounded-lg">
                                                        <Calendar className="h-4 w-4" />
                                                    </div>
                                                    <span className="text-sm font-semibold">{new Date(appt.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</span>
                                                </div>
                                                <div className="flex items-center gap-2 text-foreground-secondary">
                                                    <div className="p-1.5 bg-muted rounded-lg">
                                                        <Clock className="h-4 w-4" />
                                                    </div>
                                                    <span className="text-sm font-semibold">{appt.time} ({appt.duration} min)</span>
                                                </div>
                                            </div>
                                        </div>

                                        <div className="flex flex-col sm:flex-row md:flex-col lg:flex-row items-stretch sm:items-center gap-3">
                                            <Button 
                                                variant="outline" 
                                                className="border-border hover:bg-accent font-bold"
                                                onClick={() => handleRescheduleClick(appt)}
                                                disabled={!appt.canReschedule}
                                            >
                                                Reschedule
                                            </Button>
                                            <Button
                                                variant="outline"
                                                className="border-red-100 text-danger-fg hover:bg-red-50 font-bold"
                                                onClick={() => handleCancel(appt.id)}
                                                disabled={!appt.canCancel}
                                            >
                                                <XCircle className="h-4 w-4 mr-2" />
                                                Cancel
                                            </Button>
                                        </div>
                                    </div>
                                    <div className="bg-muted px-8 py-6 flex flex-col items-center justify-center border-l border-border-subtle min-w-[140px]">
                                        <p className="text-xs text-foreground-muted font-bold uppercase tracking-wider mb-1">Total</p>
                                        <p className="text-2xl font-black text-foreground">{formatCurrency(appt.price)}</p>
                                    </div>
                                </div>
                            </Card>
                        ))}
                    </div>
                ) : (
                    <Card className="p-12 text-center border-dashed border-2 bg-muted/50">
                        <div className="w-16 h-16 bg-muted rounded-full flex items-center justify-center mx-auto mb-4 text-foreground-muted">
                            <Calendar className="h-8 w-8" />
                        </div>
                        <h3 className="text-lg font-bold text-foreground">No upcoming appointments</h3>
                        <p className="text-foreground-secondary mt-2 mb-6 max-w-xs mx-auto">You don't have any bookings scheduled at the moment.</p>
                        <Button onClick={() => window.location.href = '/book/demo'}>
                            Book Now
                        </Button>
                    </Card>
                )
            ) : (
                <div className="space-y-4">
                    {history.length > 0 ? (
                        history.map((appt) => (
                            <Card key={appt.id} className="p-4 hover:shadow-md transition-shadow flex items-center justify-between gap-4">
                                <div className="flex items-center gap-4">
                                    <div className={cn(
                                        "w-12 h-12 rounded-xl flex items-center justify-center",
                                        appt.status === 'completed' ? "bg-emerald-50 text-emerald-600" : "bg-muted text-foreground-muted"
                                    )}>
                                        <History className="h-6 w-6" />
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-foreground">{appt.service}</h4>
                                        <p className="text-sm text-foreground-secondary">
                                            {new Date(appt.date).toLocaleDateString()} • {appt.time}
                                        </p>
                                    </div>
                                </div>
                                <div className="text-right">
                                    <p className="font-bold text-foreground">{formatCurrency(appt.price)}</p>
                                    <Badge className={cn(
                                        "capitalize px-2 py-0",
                                        appt.status === 'completed' ? "bg-emerald-100 text-emerald-700" : "bg-muted text-foreground-secondary"
                                    )}>
                                        {appt.status}
                                    </Badge>
                                </div>
                            </Card>
                        ))
                    ) : (
                        <Card className="p-12 text-center">
                            <p className="text-foreground-secondary">No past appointments found.</p>
                        </Card>
                    )}
                </div>
            )}

            <RescheduleModal 
                isOpen={isRescheduleOpen}
                onClose={() => setIsRescheduleOpen(false)}
                booking={selectedBooking}
                onSuccess={handleRescheduleSuccess}
            />
        </div>
    );
}
