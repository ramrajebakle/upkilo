'use client';

import React, { use, useState, useEffect } from 'react';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Input } from '@/components/ui/Input';
import { Calendar as CalendarIcon, Clock, CheckCircle2, AlertCircle, Loader2, MapPin, ArrowLeft } from 'lucide-react';
import { RescheduleModal } from '@/components/booking/RescheduleModal';
import { useToast } from '@/components/ui/Toast';
import Link from 'next/link';

export default function PublicReschedulePage({ params }: { params: Promise<{ locale: string, slug: string, id: string }> }) {
    const { slug, id } = use(params);
    const { addToast } = useToast();
    const [booking, setBooking] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [confirmationCode, setConfirmationCode] = useState('');
    const [isVerified, setIsVerified] = useState(false);
    const [isRescheduleOpen, setIsRescheduleOpen] = useState(false);
    const [successData, setSuccessData] = useState<any>(null);

    useEffect(() => {
        const fetchBooking = async () => {
            try {
                const res = await apiClient.get(`/api/booking/${slug}/status/${id}`);
                setBooking(res.data);
            } catch (err: any) {
                setError(err.response?.data?.error || 'Booking not found');
            } finally {
                setLoading(false);
            }
        };
        fetchBooking();
    }, [slug, id]);

    const handleVerifyCode = () => {
        // The expected code we validated in backend is booking.Id.Substring(0,8).ToUpper()
        const expectedCode = id.split('-')[0].toUpperCase();
        if (confirmationCode.trim().toUpperCase().includes(expectedCode)) {
            setIsVerified(true);
            addToast('Code verified', 'success');
        } else {
            addToast('Invalid confirmation code. Please check your email.', 'error');
        }
    };

    const handleRescheduleSuccess = (newDate: string, newTime: string) => {
        setSuccessData({ newDate, newTime });
        addToast('Appointment rescheduled successfully!', 'success');
    };

    if (loading) return (
        <div className="min-h-screen flex items-center justify-center bg-muted">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
    );

    if (error) return (
        <div className="min-h-screen flex items-center justify-center bg-muted p-4">
            <Card className="max-w-md w-full p-8 text-center space-y-4 shadow-2xl border-none">
                <AlertCircle className="h-12 w-12 text-danger-fg mx-auto" />
                <h1 className="text-xl font-bold text-foreground">Oops! {error}</h1>
                <p className="text-foreground-secondary text-sm">We couldn't find the booking you're looking for. Please check the link in your email.</p>
                <Button onClick={() => window.location.href = `/book/${slug}`} className="w-full">
                    Back to Booking
                </Button>
            </Card>
        </div>
    );

    if (successData) return (
        <div className="min-h-screen flex flex-col items-center justify-center bg-muted p-4">
            <Card className="max-w-md w-full p-8 text-center space-y-6 shadow-2xl border-none animate-in fade-in zoom-in duration-300">
                <div className="w-20 h-20 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-2">
                    <CheckCircle2 className="h-10 w-10 text-success-fg" />
                </div>
                <div>
                    <h1 className="text-2xl font-black text-foreground tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>Rescheduled!</h1>
                    <p className="text-foreground-secondary mt-2 font-medium">Your appointment has been successfully updated.</p>
                </div>
                <div className="bg-muted rounded-xl p-5 text-left border border-border-subtle space-y-3">
                    <div className="flex items-center gap-3">
                        <CalendarIcon className="h-4 w-4 text-primary" />
                        <span className="text-sm font-bold text-foreground">
                            {new Date(successData.newDate).toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
                        </span>
                    </div>
                    <div className="flex items-center gap-3">
                        <Clock className="h-4 w-4 text-primary" />
                        <span className="text-sm font-bold text-foreground">{successData.newTime}</span>
                    </div>
                </div>
                <div className="bg-primary-50/50 p-3 rounded-lg flex items-start gap-2 text-left">
                    <AlertCircle className="h-4 w-4 text-primary shrink-0 mt-0.5" />
                    <p className="text-[11px] text-primary font-medium">A new confirmation email with these details has been sent to your inbox.</p>
                </div>
                <Button onClick={() => window.location.href = `/book/${slug}`} className="w-full font-bold h-12 shadow-lg shadow-primary/20">
                    Done
                </Button>
            </Card>
        </div>
    );

    return (
        <div className="min-h-screen bg-muted flex flex-col">
            <header className="bg-card border-b border-border-subtle sticky top-0 z-10 shadow-sm">
                <div className="max-w-4xl mx-auto px-4 h-16 flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <Link href={`/book/${slug}`} className="p-2 hover:bg-accent rounded-full transition-colors text-foreground-muted">
                            <ArrowLeft className="h-5 w-5" />
                        </Link>
                        <h1 className="font-bold text-lg tracking-tight text-foreground">{booking.location}</h1>
                    </div>
                    <Badge variant="outline" className="bg-brand-subtle text-primary border-primary/25 px-3 py-1 font-bold">
                        SELF-SERVICE
                    </Badge>
                </div>
            </header>

            <main className="flex-1 max-w-2xl mx-auto w-full px-4 pt-8 md:pt-16 pb-12 space-y-8">
                <div className="text-center space-y-3">
                    <h2 className="text-4xl font-black text-foreground tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                        Reschedule Session
                    </h2>
                    <p className="text-foreground-secondary font-bold tracking-widest text-xs uppercase opacity-60">Booking ID: {booking.confirmationNumber}</p>
                </div>

                {!isVerified ? (
                    <Card className="p-10 shadow-2xl shadow-slate-200/50 border-none space-y-8 animate-in slide-in-from-bottom-4 duration-500">
                        <div className="space-y-3 text-center">
                            <h3 className="text-xl font-bold text-foreground">Security Check</h3>
                            <p className="text-sm text-foreground-secondary max-w-xs mx-auto leading-relaxed">
                                Enter the 8-character confirmation code (e.g. BK-1A2B3C4D) from your email to unlock rescheduling.
                            </p>
                        </div>
                        <div className="space-y-6 max-w-xs mx-auto">
                            <Input 
                                placeholder="BK-XXXXXX" 
                                value={confirmationCode}
                                onChange={(e) => setConfirmationCode(e.target.value.toUpperCase())}
                                className="text-center font-black tracking-[0.2em] text-2xl h-14 border-2 border-border-subtle focus:border-primary focus:ring-primary/5 rounded-xl transition-all"
                            />
                            <Button onClick={handleVerifyCode} className="w-full font-bold h-12 shadow-xl shadow-primary/25 rounded-xl text-base">
                                Verify Appointment
                            </Button>
                        </div>
                    </Card>
                ) : (
                    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
                        <Card className="p-0 overflow-hidden shadow-2xl shadow-slate-200/50 border-none">
                            <div className="p-8 border-b border-slate-50 space-y-6">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h3 className="text-2xl font-black text-foreground mb-1">{booking.service}</h3>
                                        <p className="text-foreground-secondary font-medium flex items-center gap-1.5">
                                            with <span className="text-foreground font-bold">{booking.staff}</span>
                                        </p>
                                    </div>
                                    <Badge className="bg-primary/5 text-primary border-none font-black px-3 py-1 text-[11px] tracking-widest">
                                        {booking.status.toUpperCase()}
                                    </Badge>
                                </div>
                                
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 bg-muted rounded-2xl p-6">
                                    <div className="space-y-4">
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-lg bg-card shadow-sm flex items-center justify-center text-primary">
                                                <CalendarIcon className="h-4 w-4" />
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-[10px] font-bold text-foreground-muted uppercase tracking-wider">Current Date</span>
                                                <span className="text-sm font-bold text-foreground">{new Date(booking.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}</span>
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-lg bg-card shadow-sm flex items-center justify-center text-primary">
                                                <Clock className="h-4 w-4" />
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-[10px] font-bold text-foreground-muted uppercase tracking-wider">Current Time</span>
                                                <span className="text-sm font-bold text-foreground">{booking.time}</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="space-y-4">
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-lg bg-card shadow-sm flex items-center justify-center text-primary">
                                                <MapPin className="h-4 w-4" />
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-[10px] font-bold text-foreground-muted uppercase tracking-wider">Location</span>
                                                <span className="text-sm font-bold text-foreground truncate">{booking.address || booking.location}</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div className="px-8 py-6 bg-card flex flex-col sm:flex-row items-center justify-between gap-6">
                                <div className="flex items-center gap-2">
                                    <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                                    <p className="text-xs text-foreground-secondary font-bold">New slots are currently available</p>
                                </div>
                                <Button 
                                    onClick={() => setIsRescheduleOpen(true)} 
                                    disabled={!booking.canReschedule}
                                    className="font-bold w-full sm:w-auto px-10 h-12 shadow-lg shadow-primary/20 rounded-xl"
                                >
                                    Select New Time
                                </Button>
                            </div>
                        </Card>
                    </div>
                )}

                <p className="text-center text-[11px] text-foreground-muted max-w-sm mx-auto font-medium leading-relaxed">
                    By rescheduling, you agree to our <span className="underline decoration-slate-200 underline-offset-2">Cancellation Policy</span>. 
                    Need help? Contact {booking.location} support.
                </p>
            </main>

            <footer className="mt-auto py-8 text-center border-t border-border-subtle bg-card">
                <p className="text-xs text-foreground-muted font-bold tracking-widest uppercase">
                    Secure Booking Powered by <span className="text-primary tracking-normal font-black">UPKILO</span>
                </p>
            </footer>

            <RescheduleModal 
                isOpen={isRescheduleOpen}
                onClose={() => setIsRescheduleOpen(false)}
                booking={{
                    id: id,
                    tenantSlug: slug,
                    serviceId: booking.serviceId,
                    staffId: booking.staffId,
                    service: booking.service,
                    staff: booking.staff,
                    duration: booking.duration || 30,
                    date: booking.date,
                    time: booking.time
                }}
                confirmationCode={confirmationCode}
                onSuccess={handleRescheduleSuccess}
            />
        </div>
    );
}
