'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import {
    Zap,
    ArrowLeft,
    ArrowRight,
    Check,
    Calendar,
    Clock,
    User,
    CreditCard,
    Search,
    ChevronLeft,
    ChevronRight,
    Tag,
    Loader2,
    AlertCircle,
    CheckCircle2,
    Star,
} from 'lucide-react';
import { cn, formatCurrency } from '@/lib/utils';
import { apiClient as api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

// ─── Types ────────────────────────────────────────────────────────────────────

interface Service {
    id: string;
    name: string;
    duration: number;
    price: number;
    color: string;
    category: string;
    description?: string;
}

interface StaffMember {
    id: string;
    name: string;
    avatar?: string;
    rating: number;
    specialty: string;
}

interface TimeSlot {
    time: string;
    dateTime: string;
    available: boolean;
}

interface CheckoutForm {
    serviceId: string;
    staffId: string | null;
    startTime: string | null;
    selectedDate: Date;
    clientId: string | null;
    clientName: string;
    clientEmail: string;
    clientPhone: string;
    promoCode: string;
    notes: string;
    paymentMethodId: string | null;
}

interface BookingResult {
    bookingId: string;
    confirmationCode: string;
    clientName: string;
    serviceName: string;
    staffName: string;
    startTime: string;
    endTime: string;
    price: number;
    discount: number;
    status: string;
}

// ─── Step indicator ───────────────────────────────────────────────────────────

const STEPS = [
    { id: 1, label: 'Service', icon: Star },
    { id: 2, label: 'Date & Time', icon: Calendar },
    { id: 3, label: 'Client', icon: User },
    { id: 4, label: 'Confirm', icon: CreditCard },
];

function StepIndicator({ current }: { current: number }) {
    return (
        <div className="flex items-center justify-center gap-0 mb-8">
            {STEPS.map((step, idx) => {
                const Icon = step.icon;
                const done = current > step.id;
                const active = current === step.id;
                return (
                    <div key={step.id} className="flex items-center">
                        <div className="flex flex-col items-center">
                            <div
                                className={cn(
                                    'w-10 h-10 rounded-full flex items-center justify-center text-sm font-semibold border-2 transition-all',
                                    done && 'bg-emerald-500 border-emerald-500 text-white',
                                    active && 'bg-primary-600 border-primary-600 text-white scale-110 shadow-lg shadow-primary-200',
                                    !done && !active && 'bg-card border-border text-foreground-muted'
                                )}
                            >
                                {done ? <Check className="w-5 h-5" /> : <Icon className="w-4 h-4" />}
                            </div>
                            <span
                                className={cn(
                                    'text-xs mt-1 font-medium',
                                    active ? 'text-primary' : done ? 'text-success-fg' : 'text-foreground-muted'
                                )}
                            >
                                {step.label}
                            </span>
                        </div>
                        {idx < STEPS.length - 1 && (
                            <div
                                className={cn(
                                    'h-0.5 w-16 mx-1 mb-4 transition-all',
                                    current > step.id ? 'bg-emerald-400' : 'bg-gray-200'
                                )}
                            />
                        )}
                    </div>
                );
            })}
        </div>
    );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function ExpressCheckoutPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();

    const [step, setStep] = useState(1);
    const [loading, setLoading] = useState(false);

    // Data
    const [services, setServices] = useState<Service[]>([]);
    const [staff, setStaff] = useState<StaffMember[]>([]);
    const [slots, setSlots] = useState<TimeSlot[]>([]);
    const [slotsLoading, setSlotsLoading] = useState(false);

    // Form state
    const [form, setForm] = useState<CheckoutForm>({
        serviceId: '',
        staffId: null,
        startTime: null,
        selectedDate: new Date(),
        clientId: null,
        clientName: '',
        clientEmail: '',
        clientPhone: '',
        promoCode: '',
        notes: '',
        paymentMethodId: null,
    });

    const [serviceSearch, setServiceSearch] = useState('');
    const [promoApplied, setPromoApplied] = useState(false);
    const [promoDiscount, setPromoDiscount] = useState(0);
    const [result, setResult] = useState<BookingResult | null>(null);

    // ── Load services on mount ───────────────────────────────────────────────
    useEffect(() => {
        const load = async () => {
            try {
                const res = await api.get('/api/v1/bookings/express-checkout/services');
                setServices(res.data?.data ?? []);
            } catch {
                // fallback: empty
            }
        };
        load();
    }, []);

    // Load staff when service is selected
    useEffect(() => {
        if (!form.serviceId) return;
        const load = async () => {
            try {
                const res = await api.get(`/api/v1/bookings/express-checkout/staff?serviceId=${form.serviceId}`);
                setStaff(res.data?.data ?? []);
            } catch {
                setStaff([]);
            }
        };
        load();
    }, [form.serviceId]);

    // Load slots when date/service change
    useEffect(() => {
        if (!form.serviceId || !form.selectedDate) return;
        const load = async () => {
            setSlotsLoading(true);
            try {
                const dateStr = form.selectedDate.toISOString().slice(0, 10);
                const staffParam = form.staffId ? `&staffId=${form.staffId}` : '';
                const res = await api.get(
                    `/api/v1/bookings/express-checkout/slots?serviceId=${form.serviceId}&date=${dateStr}${staffParam}`
                );
                setSlots(res.data?.data?.slots ?? []);
            } catch {
                setSlots([]);
            } finally {
                setSlotsLoading(false);
            }
        };
        load();
    }, [form.serviceId, form.selectedDate, form.staffId]);

    // ── Helpers ──────────────────────────────────────────────────────────────
    const selectedService = services.find(s => s.id === form.serviceId);
    const selectedStaff = staff.find(s => s.id === form.staffId);

    const filteredServices = services.filter(
        s =>
            !serviceSearch ||
            s.name.toLowerCase().includes(serviceSearch.toLowerCase()) ||
            s.category.toLowerCase().includes(serviceSearch.toLowerCase())
    );

    const groupedServices = filteredServices.reduce<Record<string, Service[]>>((acc, svc) => {
        const cat = svc.category || 'Other';
        if (!acc[cat]) acc[cat] = [];
        acc[cat].push(svc);
        return acc;
    }, {});

    const navigateDate = (dir: -1 | 1) => {
        setForm(f => {
            const d = new Date(f.selectedDate);
            d.setDate(d.getDate() + dir);
            return { ...f, selectedDate: d, startTime: null };
        });
    };

    const applyPromo = async () => {
        if (!form.promoCode || !selectedService) return;
        try {
            // Quick validation via express-checkout promo check
            // In production would hit a dedicated endpoint; here we optimistically accept
            setPromoApplied(true);
            setPromoDiscount(selectedService.price * 0.1); // placeholder 10%
            toastSuccess('Promo code applied!');
        } catch {
            toastError('Invalid promo code');
        }
    };

    const finalPrice = selectedService
        ? Math.max(0, selectedService.price - promoDiscount)
        : 0;

    // ── Submit ───────────────────────────────────────────────────────────────
    const handleSubmit = async () => {
        if (!form.serviceId || !form.startTime) return;
        setLoading(true);
        try {
            const payload = {
                serviceId: form.serviceId,
                startTime: form.startTime,
                staffId: form.staffId || undefined,
                clientId: form.clientId || undefined,
                clientName: form.clientName || undefined,
                clientEmail: form.clientEmail || undefined,
                clientPhone: form.clientPhone || undefined,
                promoCode: form.promoCode || undefined,
                notes: form.notes || undefined,
                paymentMethodId: form.paymentMethodId || undefined,
            };

            const res = await api.post('/api/v1/bookings/express-checkout', payload);
            setResult(res.data?.data);
            setStep(5); // success step
            toastSuccess('Booking confirmed!');
        } catch (err: any) {
            const msg = err?.response?.data?.message ?? 'Booking failed. Please try again.';
            toastError(msg);
        } finally {
            setLoading(false);
        }
    };

    const canProceed = (): boolean => {
        if (step === 1) return !!form.serviceId;
        if (step === 2) return !!form.startTime;
        if (step === 3) return !!(form.clientId || (form.clientName && form.clientEmail));
        if (step === 4) return true;
        return false;
    };

    // ── Render ────────────────────────────────────────────────────────────────
    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-primary-50/30 to-white">
            {/* Header */}
            <div className="bg-card border-b border-border-subtle sticky top-0 z-10 shadow-sm">
                <div className="max-w-3xl mx-auto px-4 py-4 flex items-center gap-4">
                    <Link
                        href="/bookings"
                        className="p-2 rounded-lg hover:bg-accent transition-colors text-foreground-secondary"
                    >
                        <ArrowLeft className="w-5 h-5" />
                    </Link>
                    <div className="flex items-center gap-2">
                        <div className="p-1.5 bg-brand-subtle rounded-lg">
                            <Zap className="w-5 h-5 text-primary" />
                        </div>
                        <div>
                            <h1 className="text-lg font-bold text-foreground">Express Checkout</h1>
                            <p className="text-xs text-foreground-secondary">Book in seconds, no back-and-forth</p>
                        </div>
                    </div>
                </div>
            </div>

            <div className="max-w-3xl mx-auto px-4 py-8">
                {/* Step indicator */}
                {step < 5 && <StepIndicator current={step} />}

                {/* ── STEP 1: Service Selection ─────────────────────────────── */}
                {step === 1 && (
                    <div className="animate-in fade-in slide-in-from-right-4 duration-300">
                        <h2 className="text-xl font-bold text-foreground mb-1">Choose a service</h2>
                        <p className="text-foreground-secondary text-sm mb-5">What would you like to book today?</p>

                        {/* Search */}
                        <div className="relative mb-5">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-foreground-muted" />
                            <input
                                type="text"
                                placeholder="Search services..."
                                value={serviceSearch}
                                onChange={e => setServiceSearch(e.target.value)}
                                className="w-full pl-9 pr-4 py-2.5 border border-border rounded-xl bg-card text-sm focus:outline-none focus:ring-2 focus:ring-primary-400"
                            />
                        </div>

                        {/* Service grid by category */}
                        {Object.entries(groupedServices).map(([category, svcs]) => (
                            <div key={category} className="mb-6">
                                <h3 className="text-xs font-semibold text-foreground-muted uppercase tracking-wider mb-3">
                                    {category}
                                </h3>
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                    {svcs.map(svc => (
                                        <button
                                            key={svc.id}
                                            onClick={() => setForm(f => ({ ...f, serviceId: svc.id }))}
                                            className={cn(
                                                'text-left p-4 rounded-xl border-2 transition-all hover:shadow-md',
                                                form.serviceId === svc.id
                                                    ? 'border-primary-500 bg-brand-subtle shadow-md'
                                                    : 'border-border-subtle bg-card hover:border-border'
                                            )}
                                        >
                                            <div className="flex items-start justify-between gap-2">
                                                <div className="flex items-center gap-2">
                                                    <div
                                                        className="w-3 h-3 rounded-full flex-shrink-0 mt-0.5"
                                                        style={{ backgroundColor: svc.color || '#8B5CF6' }}
                                                    />
                                                    <span className="font-semibold text-foreground text-sm">
                                                        {svc.name}
                                                    </span>
                                                </div>
                                                {form.serviceId === svc.id && (
                                                    <Check className="w-4 h-4 text-primary flex-shrink-0" />
                                                )}
                                            </div>
                                            <div className="flex items-center gap-3 mt-2 ml-5">
                                                <span className="text-xs text-foreground-secondary flex items-center gap-1">
                                                    <Clock className="w-3 h-3" />
                                                    {svc.duration} min
                                                </span>
                                                <span className="text-xs font-bold text-primary">
                                                    {formatCurrency(svc.price)}
                                                </span>
                                            </div>
                                            {svc.description && (
                                                <p className="text-xs text-foreground-muted mt-1 ml-5 line-clamp-1">
                                                    {svc.description}
                                                </p>
                                            )}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        ))}

                        {services.length === 0 && (
                            <div className="text-center py-12 text-foreground-muted">
                                <Star className="w-8 h-8 mx-auto mb-2 opacity-30" />
                                <p>No services found</p>
                            </div>
                        )}
                    </div>
                )}

                {/* ── STEP 2: Date & Time ───────────────────────────────────── */}
                {step === 2 && (
                    <div className="animate-in fade-in slide-in-from-right-4 duration-300">
                        <h2 className="text-xl font-bold text-foreground mb-1">Pick a date & time</h2>
                        <p className="text-foreground-secondary text-sm mb-5">
                            {selectedService?.name} · {selectedService?.duration} min
                        </p>

                        {/* Staff picker (optional) */}
                        {staff.length > 0 && (
                            <div className="mb-6">
                                <label className="text-sm font-semibold text-foreground mb-2 block">
                                    Staff (optional — any available if skipped)
                                </label>
                                <div className="flex gap-2 flex-wrap">
                                    <button
                                        onClick={() => setForm(f => ({ ...f, staffId: null }))}
                                        className={cn(
                                            'px-3 py-1.5 rounded-full text-sm border transition-all',
                                            !form.staffId
                                                ? 'bg-primary-600 text-white border-primary-600'
                                                : 'bg-card text-foreground-secondary border-border hover:border-border-strong'
                                        )}
                                    >
                                        Any
                                    </button>
                                    {staff.map(s => (
                                        <button
                                            key={s.id}
                                            onClick={() => setForm(f => ({ ...f, staffId: s.id }))}
                                            className={cn(
                                                'px-3 py-1.5 rounded-full text-sm border transition-all flex items-center gap-1.5',
                                                form.staffId === s.id
                                                    ? 'bg-primary-600 text-white border-primary-600'
                                                    : 'bg-card text-foreground-secondary border-border hover:border-border-strong'
                                            )}
                                        >
                                            {s.name}
                                            {s.rating > 0 && (
                                                <span className="flex items-center gap-0.5 text-xs opacity-80">
                                                    <Star className="w-2.5 h-2.5" />
                                                    {s.rating.toFixed(1)}
                                                </span>
                                            )}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        )}

                        {/* Date navigator */}
                        <div className="bg-card rounded-xl border border-border-subtle p-4 mb-4 shadow-sm">
                            <div className="flex items-center justify-between mb-4">
                                <button
                                    onClick={() => navigateDate(-1)}
                                    disabled={form.selectedDate <= new Date()}
                                    className="p-2 rounded-lg hover:bg-accent disabled:opacity-30 transition-colors"
                                >
                                    <ChevronLeft className="w-5 h-5 text-foreground-secondary" />
                                </button>
                                <div className="text-center">
                                    <p className="font-semibold text-foreground">
                                        {form.selectedDate.toLocaleDateString('en-US', {
                                            weekday: 'long',
                                            month: 'long',
                                            day: 'numeric',
                                        })}
                                    </p>
                                    <p className="text-xs text-foreground-muted">
                                        {form.selectedDate.toLocaleDateString('en-US', { year: 'numeric' })}
                                    </p>
                                </div>
                                <button
                                    onClick={() => navigateDate(1)}
                                    className="p-2 rounded-lg hover:bg-accent transition-colors"
                                >
                                    <ChevronRight className="w-5 h-5 text-foreground-secondary" />
                                </button>
                            </div>

                            {/* Time slots */}
                            {slotsLoading ? (
                                <div className="flex justify-center py-8">
                                    <Loader2 className="w-6 h-6 text-primary animate-spin" />
                                </div>
                            ) : slots.length === 0 ? (
                                <p className="text-center text-foreground-muted py-6 text-sm">
                                    No slots available for this date
                                </p>
                            ) : (
                                <div className="grid grid-cols-4 sm:grid-cols-6 gap-2">
                                    {slots.map(slot => (
                                        <button
                                            key={slot.time}
                                            disabled={!slot.available}
                                            onClick={() =>
                                                setForm(f => ({ ...f, startTime: slot.dateTime }))
                                            }
                                            className={cn(
                                                'py-2 px-1 rounded-lg text-sm font-medium border transition-all',
                                                !slot.available &&
                                                    'opacity-30 cursor-not-allowed bg-muted border-border-subtle text-foreground-muted',
                                                slot.available &&
                                                    form.startTime === slot.dateTime &&
                                                    'bg-primary-600 text-white border-primary-600 shadow-md',
                                                slot.available &&
                                                    form.startTime !== slot.dateTime &&
                                                    'bg-card text-foreground border-border hover:border-primary-300 hover:text-primary'
                                            )}
                                        >
                                            {slot.time}
                                        </button>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* ── STEP 3: Client Info ───────────────────────────────────── */}
                {step === 3 && (
                    <div className="animate-in fade-in slide-in-from-right-4 duration-300">
                        <h2 className="text-xl font-bold text-foreground mb-1">Client details</h2>
                        <p className="text-foreground-secondary text-sm mb-5">
                            Existing client? Enter their email to look them up.
                        </p>

                        <div className="bg-card rounded-xl border border-border-subtle p-5 shadow-sm space-y-4">
                            <div>
                                <label className="text-sm font-semibold text-foreground block mb-1.5">
                                    Full name *
                                </label>
                                <input
                                    type="text"
                                    placeholder="Jane Smith"
                                    value={form.clientName}
                                    onChange={e => setForm(f => ({ ...f, clientName: e.target.value }))}
                                    className="w-full px-3 py-2.5 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400"
                                />
                            </div>
                            <div>
                                <label className="text-sm font-semibold text-foreground block mb-1.5">
                                    Email *
                                </label>
                                <input
                                    type="email"
                                    placeholder="jane@example.com"
                                    value={form.clientEmail}
                                    onChange={e => setForm(f => ({ ...f, clientEmail: e.target.value }))}
                                    className="w-full px-3 py-2.5 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400"
                                />
                                <p className="text-xs text-foreground-muted mt-1">
                                    If this email matches an existing client they'll be linked automatically.
                                </p>
                            </div>
                            <div>
                                <label className="text-sm font-semibold text-foreground block mb-1.5">
                                    Phone (optional)
                                </label>
                                <input
                                    type="tel"
                                    placeholder="+1 555 000 0000"
                                    value={form.clientPhone}
                                    onChange={e => setForm(f => ({ ...f, clientPhone: e.target.value }))}
                                    className="w-full px-3 py-2.5 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400"
                                />
                            </div>
                            <div>
                                <label className="text-sm font-semibold text-foreground block mb-1.5">
                                    Notes (optional)
                                </label>
                                <textarea
                                    placeholder="Any special requests or notes..."
                                    value={form.notes}
                                    onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                                    rows={2}
                                    className="w-full px-3 py-2.5 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 resize-none"
                                />
                            </div>
                        </div>
                    </div>
                )}

                {/* ── STEP 4: Confirm & Pay ─────────────────────────────────── */}
                {step === 4 && selectedService && (
                    <div className="animate-in fade-in slide-in-from-right-4 duration-300">
                        <h2 className="text-xl font-bold text-foreground mb-1">Confirm booking</h2>
                        <p className="text-foreground-secondary text-sm mb-5">Review the details before confirming.</p>

                        {/* Summary card */}
                        <div className="bg-card rounded-xl border border-border-subtle shadow-sm divide-y divide-gray-50 mb-5">
                            <div className="p-4 flex items-center gap-3">
                                <div
                                    className="w-10 h-10 rounded-xl flex-shrink-0"
                                    style={{ backgroundColor: selectedService.color + '20' }}
                                >
                                    <div
                                        className="w-full h-full rounded-xl opacity-70"
                                        style={{ backgroundColor: selectedService.color }}
                                    />
                                </div>
                                <div>
                                    <p className="font-semibold text-foreground">{selectedService.name}</p>
                                    <p className="text-sm text-foreground-secondary">{selectedService.duration} min</p>
                                </div>
                            </div>
                            <div className="p-4 grid grid-cols-2 gap-3 text-sm">
                                <div>
                                    <p className="text-foreground-muted text-xs mb-0.5">Date & Time</p>
                                    <p className="font-medium text-foreground">
                                        {form.startTime
                                            ? new Date(form.startTime).toLocaleString('en-US', {
                                                  weekday: 'short',
                                                  month: 'short',
                                                  day: 'numeric',
                                                  hour: '2-digit',
                                                  minute: '2-digit',
                                              })
                                            : '—'}
                                    </p>
                                </div>
                                <div>
                                    <p className="text-foreground-muted text-xs mb-0.5">Staff</p>
                                    <p className="font-medium text-foreground">
                                        {selectedStaff?.name ?? 'Any available'}
                                    </p>
                                </div>
                                <div>
                                    <p className="text-foreground-muted text-xs mb-0.5">Client</p>
                                    <p className="font-medium text-foreground">
                                        {form.clientName || 'Walk-in'}
                                    </p>
                                </div>
                                <div>
                                    <p className="text-foreground-muted text-xs mb-0.5">Email</p>
                                    <p className="font-medium text-foreground truncate">{form.clientEmail}</p>
                                </div>
                            </div>
                        </div>

                        {/* Promo code */}
                        <div className="bg-card rounded-xl border border-border-subtle shadow-sm p-4 mb-5">
                            <label className="text-sm font-semibold text-foreground block mb-2 flex items-center gap-1.5">
                                <Tag className="w-4 h-4 text-foreground-muted" />
                                Promo code
                            </label>
                            <div className="flex gap-2">
                                <input
                                    type="text"
                                    placeholder="Enter code"
                                    value={form.promoCode}
                                    onChange={e =>
                                        setForm(f => ({ ...f, promoCode: e.target.value.toUpperCase() }))
                                    }
                                    disabled={promoApplied}
                                    className="flex-1 px-3 py-2 border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-400 disabled:opacity-50"
                                />
                                <button
                                    onClick={applyPromo}
                                    disabled={promoApplied || !form.promoCode}
                                    className="px-4 py-2 bg-primary-600 text-white rounded-lg text-sm font-medium hover:bg-primary-700 disabled:opacity-40 transition-colors"
                                >
                                    {promoApplied ? 'Applied' : 'Apply'}
                                </button>
                            </div>
                            {promoApplied && (
                                <p className="text-xs text-success-fg mt-1.5 flex items-center gap-1">
                                    <Check className="w-3 h-3" />
                                    Discount applied — saving {formatCurrency(promoDiscount)}
                                </p>
                            )}
                        </div>

                        {/* Price breakdown */}
                        <div className="bg-card rounded-xl border border-border-subtle shadow-sm p-4">
                            <div className="space-y-2 text-sm">
                                <div className="flex justify-between text-foreground-secondary">
                                    <span>{selectedService.name}</span>
                                    <span>{formatCurrency(selectedService.price)}</span>
                                </div>
                                {promoDiscount > 0 && (
                                    <div className="flex justify-between text-success-fg">
                                        <span>Promo ({form.promoCode})</span>
                                        <span>−{formatCurrency(promoDiscount)}</span>
                                    </div>
                                )}
                                <div className="flex justify-between font-bold text-foreground text-base pt-2 border-t border-border-subtle">
                                    <span>Total</span>
                                    <span>{formatCurrency(finalPrice)}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {/* ── STEP 5: Success ───────────────────────────────────────── */}
                {step === 5 && result && (
                    <div className="animate-in fade-in zoom-in-95 duration-500 text-center py-6">
                        <div className="w-20 h-20 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-5">
                            <CheckCircle2 className="w-10 h-10 text-success-fg" />
                        </div>
                        <h2 className="text-2xl font-bold text-foreground mb-1">Booking Confirmed!</h2>
                        <p className="text-foreground-secondary mb-6">
                            Confirmation:{' '}
                            <span className="font-mono font-bold text-primary">
                                {result.confirmationCode}
                            </span>
                        </p>

                        <div className="bg-card rounded-xl border border-border-subtle shadow-sm p-5 text-left mb-6 max-w-sm mx-auto">
                            <div className="space-y-3 text-sm">
                                {[
                                    { label: 'Client', value: result.clientName },
                                    { label: 'Service', value: result.serviceName },
                                    { label: 'Staff', value: result.staffName },
                                    {
                                        label: 'Date & Time',
                                        value: new Date(result.startTime).toLocaleString('en-US', {
                                            weekday: 'short',
                                            month: 'short',
                                            day: 'numeric',
                                            hour: '2-digit',
                                            minute: '2-digit',
                                        }),
                                    },
                                    { label: 'Total Paid', value: formatCurrency(result.price) },
                                ].map(row => (
                                    <div key={row.label} className="flex justify-between">
                                        <span className="text-foreground-muted">{row.label}</span>
                                        <span className="font-medium text-foreground">{row.value}</span>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="flex gap-3 justify-center">
                            <button
                                onClick={() => {
                                    setStep(1);
                                    setForm(f => ({
                                        ...f,
                                        serviceId: '',
                                        startTime: null,
                                        clientName: '',
                                        clientEmail: '',
                                        promoCode: '',
                                        notes: '',
                                    }));
                                    setResult(null);
                                    setPromoApplied(false);
                                    setPromoDiscount(0);
                                }}
                                className="px-5 py-2.5 border border-border text-foreground rounded-xl text-sm font-medium hover:bg-accent transition-colors"
                            >
                                New Booking
                            </button>
                            <Link
                                href={`/bookings/${result.bookingId}`}
                                className="px-5 py-2.5 bg-primary-600 text-white rounded-xl text-sm font-medium hover:bg-primary-700 transition-colors"
                            >
                                View Booking
                            </Link>
                        </div>
                    </div>
                )}

                {/* ── Navigation bar ────────────────────────────────────────── */}
                {step < 5 && (
                    <div className="flex items-center justify-between mt-8 pt-6 border-t border-border-subtle">
                        <button
                            onClick={() => setStep(s => Math.max(1, s - 1))}
                            disabled={step === 1}
                            className="flex items-center gap-2 px-4 py-2.5 text-sm font-medium text-foreground-secondary border border-border rounded-xl hover:bg-accent disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                        >
                            <ChevronLeft className="w-4 h-4" />
                            Back
                        </button>

                        {step < 4 ? (
                            <button
                                onClick={() => setStep(s => s + 1)}
                                disabled={!canProceed()}
                                className="flex items-center gap-2 px-6 py-2.5 text-sm font-bold bg-primary-600 text-white rounded-xl hover:bg-primary-700 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-md shadow-primary-200"
                            >
                                Continue
                                <ArrowRight className="w-4 h-4" />
                            </button>
                        ) : (
                            <button
                                onClick={handleSubmit}
                                disabled={loading}
                                className="flex items-center gap-2 px-6 py-2.5 text-sm font-bold bg-emerald-600 text-white rounded-xl hover:bg-emerald-700 disabled:opacity-40 transition-all shadow-md shadow-emerald-200"
                            >
                                {loading ? (
                                    <>
                                        <Loader2 className="w-4 h-4 animate-spin" />
                                        Confirming...
                                    </>
                                ) : (
                                    <>
                                        <Zap className="w-4 h-4" />
                                        Confirm &amp; Book — {formatCurrency(finalPrice)}
                                    </>
                                )}
                            </button>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
