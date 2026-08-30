'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
    Calendar,
    Clock,
    User,
    DollarSign,
    ArrowLeft,
    Check,
    ArrowRight,
    Sparkles,
    Search,
    Star,
    CalendarDays,
    ChevronLeft,
    ChevronRight,
} from 'lucide-react';
import { cn, formatCurrency, formatDate, formatTime } from '@/lib/utils';

import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

interface Service {
    id: string;
    name: string;
    duration: number;
    price: number;
    color: string;
    category: string;
}

interface Staff {
    id: string;
    name: string;
    avatar?: string;
    rating: number;
    specialty: string;
}

interface Client {
    id: string;
    name: string;
    email: string;
    phone: string;
}

interface TimeSlot {
    time: string;
    available: boolean;
}

export default function NewBookingPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [step, setStep] = useState(1);
    const [loading, setLoading] = useState(false);
    const [fetchingData, setFetchingData] = useState(true);
    
    // Data states
    const [services, setServices] = useState<Service[]>([]);
    const [staff, setStaff] = useState<Staff[]>([]);
    const [clients, setClients] = useState<Client[]>([]);
    
    // Selection states
    const [selectedService, setSelectedService] = useState<string>('');
    const [selectedStaff, setSelectedStaff] = useState<string>('');
    const [selectedDate, setSelectedDate] = useState<string>(new Date().toISOString().split('T')[0]);
    const [selectedTime, setSelectedTime] = useState<string>('');
    const [clientSearch, setClientSearch] = useState('');
    const [selectedClient, setSelectedClient] = useState<string>('');
    const [notes, setNotes] = useState('');

    // Recurring states
    const [isRecurring, setIsRecurring] = useState(false);
    const [frequency, setFrequency] = useState<'Daily' | 'Weekly' | 'Monthly'>('Weekly');
    const [interval, setInterval] = useState(1);
    const [endType, setEndType] = useState<'date' | 'occurrences'>('occurrences');
    const [endDate, setEndDate] = useState<string>('');
    const [occurrences, setOccurrences] = useState(10);
    const [daysOfWeek, setDaysOfWeek] = useState<number[]>([new Date().getDay()]);

    useEffect(() => {
        const loadInitialData = async () => {
            setFetchingData(true);
            try {
                const [servicesRes, staffRes, clientsRes] = await Promise.all([
                    api.services.list(),
                    api.staff.list(),
                    api.clients.list({ limit: 100 })
                ]);
                setServices(servicesRes.data);
                setStaff(staffRes.data);
                setClients(clientsRes.data);
            } catch (error) {
                console.error('Failed to load data', error);
                toastError('Failed to load initial data');
            } finally {
                setFetchingData(false);
            }
        };
        loadInitialData();
    }, []);

    const timeSlots: TimeSlot[] = [
        { time: '09:00', available: true },
        { time: '09:30', available: true },
        { time: '10:00', available: true },
        { time: '10:30', available: false },
        { time: '11:00', available: true },
        { time: '11:30', available: true },
        { time: '12:00', available: false },
        { time: '12:30', available: false },
        { time: '13:00', available: true },
        { time: '13:30', available: true },
        { time: '14:00', available: true },
        { time: '14:30', available: true },
        { time: '15:00', available: false },
        { time: '15:30', available: true },
        { time: '16:00', available: true },
        { time: '16:30', available: true },
        { time: '17:00', available: true },
    ];

    const selectedServiceData = services.find(s => s.id === selectedService);
    const selectedStaffData = staff.find(s => s.id === selectedStaff);
    const selectedClientData = clients.find(c => c.id === selectedClient);

    const handleSubmit = async () => {
        setLoading(true);
        try {
            const bookingData: any = {
                serviceId: selectedService,
                staffId: selectedStaff === 'any' ? null : selectedStaff,
                clientId: selectedClient,
                date: selectedDate,
                time: selectedTime,
                notes
            };

            if (isRecurring) {
                await api.bookings.createRecurring({
                    ...bookingData,
                    frequency,
                    interval,
                    startDate: selectedDate,
                    startTime: selectedTime,
                    endDate: endType === 'date' ? endDate : null,
                    occurrences: endType === 'occurrences' ? occurrences : null,
                    daysOfWeek: frequency === 'Weekly' ? daysOfWeek : null
                });
            } else {
                await api.bookings.create(bookingData);
            }

            toastSuccess(`Booking${isRecurring ? 's' : ''} created successfully`);
            router.push('/bookings?created=true');
        } catch (error) {
            console.error('Failed to create booking', error);
            toastError('Failed to create booking');
        } finally {
            setLoading(false);
        }
    };

    const canProceed = () => {
        switch (step) {
            case 1: return selectedService;
            case 2: return selectedStaff;
            case 3: return selectedDate && selectedTime;
            case 4: return selectedClient;
            default: return true;
        }
    };

    // Date helpers
    const formatDateDisplay = (dateStr: string) => {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
    };

    const getNextDays = (count: number) => {
        const days = [];
        const today = new Date();
        for (let i = 0; i < count; i++) {
            const date = new Date(today);
            date.setDate(today.getDate() + i);
            days.push(date.toISOString().split('T')[0]);
        }
        return days;
    };

    const dates = getNextDays(7);

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link href="/bookings" className="p-2 hover:bg-accent rounded-xl transition-colors">
                    <ArrowLeft className="h-5 w-5 text-foreground-secondary" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-cyan-500 to-blue-600 rounded-xl shadow-lg shadow-cyan-500/25">
                            <CalendarDays className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-foreground"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            New Booking
                        </h1>
                    </div>
                    <p className="text-foreground-secondary ml-12">Schedule a new appointment</p>
                </div>
            </div>

            {/* Progress Steps */}
            <div className="mb-8 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                <div className="flex items-center justify-between relative">
                    <div className="absolute left-0 right-0 top-5 h-0.5 bg-slate-200" />
                    <div
                        className="absolute left-0 top-5 h-0.5 bg-gradient-to-r from-primary-500 to-cyan-500 transition-all duration-500"
                        style={{ width: `${((step - 1) / 4) * 100}%` }}
                    />
                    {[
                        { num: 1, label: 'Service', icon: Sparkles },
                        { num: 2, label: 'Staff', icon: User },
                        { num: 3, label: 'Date & Time', icon: Calendar },
                        { num: 4, label: 'Client', icon: User },
                        { num: 5, label: 'Confirm', icon: Check },
                    ].map((s) => {
                        const Icon = s.icon;
                        return (
                            <div key={s.num} className="relative flex flex-col items-center z-10">
                                <div className={cn(
                                    'w-10 h-10 rounded-full flex items-center justify-center transition-all',
                                    step >= s.num
                                        ? 'bg-gradient-to-br from-primary-500 to-cyan-500 text-white shadow-lg'
                                        : 'bg-card border-2 border-border text-foreground-muted'
                                )}>
                                    {step > s.num ? <Check className="h-5 w-5" /> : <Icon className="h-4 w-4" />}
                                </div>
                                <span className={cn(
                                    'text-xs mt-2 font-medium',
                                    step >= s.num ? 'text-foreground' : 'text-foreground-muted'
                                )}>
                                    {s.label}
                                </span>
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Step Content */}
            <div className="min-h-[400px]">
                {/* Step 1: Select Service */}
                {step === 1 && (
                    <div className="space-y-4 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                        <h2 className="text-lg font-semibold text-foreground mb-4">Choose a Service</h2>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {services.map((service) => (
                                <button
                                    key={service.id}
                                    onClick={() => setSelectedService(service.id)}
                                    className={cn(
                                        'p-5 rounded-xl text-left transition-all border-2',
                                        selectedService === service.id
                                            ? 'border-primary-500 bg-brand-subtle shadow-lg shadow-primary-500/10'
                                            : 'border-border bg-card hover:border-border-strong hover:shadow-md'
                                    )}
                                >
                                    <div className="flex items-start gap-4">
                                        <div
                                            className="w-12 h-12 rounded-xl flex items-center justify-center text-white"
                                            style={{ backgroundColor: service.color }}
                                        >
                                            <Sparkles className="h-6 w-6" />
                                        </div>
                                        <div className="flex-1">
                                            <h3 className="font-semibold text-foreground">{service.name}</h3>
                                            <p className="text-sm text-foreground-secondary mt-0.5">{service.category}</p>
                                            <div className="flex items-center gap-4 mt-2">
                                                <span className="flex items-center gap-1 text-sm text-foreground-secondary">
                                                    <Clock className="h-4 w-4" />
                                                    {service.duration} min
                                                </span>
                                                <span className="font-semibold" style={{ color: service.color }}>
                                                    {formatCurrency(service.price)}
                                                </span>
                                            </div>
                                        </div>
                                        {selectedService === service.id && (
                                            <div className="w-6 h-6 rounded-full bg-primary-500 flex items-center justify-center">
                                                <Check className="h-4 w-4 text-white" />
                                            </div>
                                        )}
                                    </div>
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                {/* Step 2: Select Staff */}
                {step === 2 && (
                    <div className="space-y-4 animate-fade-in-up">
                        <h2 className="text-lg font-semibold text-foreground mb-4">Choose Staff Member</h2>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <button
                                onClick={() => setSelectedStaff('any')}
                                className={cn(
                                    'p-5 rounded-xl text-left transition-all border-2',
                                    selectedStaff === 'any'
                                        ? 'border-primary-500 bg-brand-subtle shadow-lg shadow-primary-500/10'
                                        : 'border-border bg-card hover:border-border-strong hover:shadow-md'
                                )}
                            >
                                <div className="flex items-center gap-4">
                                    <div className="w-14 h-14 rounded-xl bg-gradient-to-br from-slate-400 to-slate-500 flex items-center justify-center text-white">
                                        <User className="h-7 w-7" />
                                    </div>
                                    <div className="flex-1">
                                        <h3 className="font-semibold text-foreground">No Preference</h3>
                                        <p className="text-sm text-foreground-secondary">Any available staff</p>
                                    </div>
                                    {selectedStaff === 'any' && (
                                        <div className="w-6 h-6 rounded-full bg-primary-500 flex items-center justify-center">
                                            <Check className="h-4 w-4 text-white" />
                                        </div>
                                    )}
                                </div>
                            </button>
                            {staff.map((member) => (
                                <button
                                    key={member.id}
                                    onClick={() => setSelectedStaff(member.id)}
                                    className={cn(
                                        'p-5 rounded-xl text-left transition-all border-2',
                                        selectedStaff === member.id
                                            ? 'border-primary-500 bg-brand-subtle shadow-lg shadow-primary-500/10'
                                            : 'border-border bg-card hover:border-border-strong hover:shadow-md'
                                    )}
                                >
                                    <div className="flex items-center gap-4">
                                        <div className="w-14 h-14 rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center text-white font-bold text-lg">
                                            {member.name.split(' ').map(n => n[0]).join('')}
                                        </div>
                                        <div className="flex-1">
                                            <h3 className="font-semibold text-foreground">{member.name}</h3>
                                            <p className="text-sm text-foreground-secondary">{member.specialty}</p>
                                            <div className="flex items-center gap-1 mt-1">
                                                <Star className="h-4 w-4 text-amber-400 fill-amber-400" />
                                                <span className="text-sm font-medium text-foreground">{member.rating}</span>
                                            </div>
                                        </div>
                                        {selectedStaff === member.id && (
                                            <div className="w-6 h-6 rounded-full bg-primary-500 flex items-center justify-center">
                                                <Check className="h-4 w-4 text-white" />
                                            </div>
                                        )}
                                    </div>
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                {/* Step 3: Date & Time */}
                {step === 3 && (
                    <div className="space-y-6 animate-fade-in-up">
                        <h2 className="text-lg font-semibold text-foreground">Select Date & Time</h2>

                        {/* Date Selection */}
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-3">Date</label>
                            <div className="flex gap-2 overflow-x-auto pb-2">
                                {dates.map((date, i) => {
                                    const d = new Date(date);
                                    const isToday = i === 0;
                                    return (
                                        <button
                                            key={date}
                                            onClick={() => setSelectedDate(date)}
                                            className={cn(
                                                'flex flex-col items-center min-w-[70px] p-3 rounded-xl transition-all',
                                                selectedDate === date
                                                    ? 'bg-gradient-to-br from-primary-500 to-cyan-500 text-white shadow-lg'
                                                    : 'bg-card border border-border hover:border-primary-300'
                                            )}
                                        >
                                            <span className={cn(
                                                'text-xs font-medium',
                                                selectedDate === date ? 'text-white/80' : 'text-foreground-secondary'
                                            )}>
                                                {isToday ? 'Today' : d.toLocaleDateString('en-US', { weekday: 'short' })}
                                            </span>
                                            <span className={cn(
                                                'text-xl font-bold mt-1',
                                                selectedDate === date ? 'text-white' : 'text-foreground'
                                            )}>
                                                {d.getDate()}
                                            </span>
                                            <span className={cn(
                                                'text-xs',
                                                selectedDate === date ? 'text-white/80' : 'text-foreground-secondary'
                                            )}>
                                                {d.toLocaleDateString('en-US', { month: 'short' })}
                                            </span>
                                        </button>
                                    );
                                })}
                            </div>
                        </div>

                        {/* Time Selection */}
                        <div>
                            <label className="block text-sm font-medium text-foreground mb-3">Time</label>
                            <div className="grid grid-cols-4 sm:grid-cols-6 gap-2">
                                {timeSlots.map((slot) => (
                                    <button
                                        key={slot.time}
                                        onClick={() => slot.available && setSelectedTime(slot.time)}
                                        disabled={!slot.available}
                                        className={cn(
                                            'py-3 px-2 rounded-lg text-sm font-medium transition-all',
                                            !slot.available
                                                ? 'bg-muted text-foreground-muted cursor-not-allowed line-through'
                                                : selectedTime === slot.time
                                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                                    : 'bg-card border border-border text-foreground hover:border-primary-300'
                                        )}
                                    >
                                        {slot.time}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Recurring Toggle */}
                        <div className="bg-muted rounded-2xl p-6 border border-slate-200/60 shadow-sm transition-all hover:shadow-md">
                            <div className="flex items-center justify-between mb-4">
                                <div className="flex items-center gap-3">
                                    <div className="p-2 bg-brand-subtle text-primary rounded-lg">
                                        <CalendarDays className="h-5 w-5" />
                                    </div>
                                    <div>
                                        <h3 className="font-semibold text-foreground">Make this a recurring booking</h3>
                                        <p className="text-sm text-foreground-secondary">Schedule multiple appointments at once</p>
                                    </div>
                                </div>
                                <button
                                    type="button"
                                    onClick={() => setIsRecurring(!isRecurring)}
                                    className={cn(
                                        'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
                                        isRecurring ? 'bg-primary-500' : 'bg-slate-200'
                                    )}
                                >
                                    <span className={cn(
                                        'inline-block h-4 w-4 transform rounded-full bg-control-thumb transition-transform',
                                        isRecurring ? 'translate-x-6' : 'translate-x-1'
                                    )} />
                                </button>
                            </div>

                            {isRecurring && (
                                <div className="space-y-6 pt-4 border-t border-border animate-fade-in">
                                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                        <div>
                                            <label className="block text-sm font-medium text-foreground mb-2">Frequency</label>
                                            <select
                                                value={frequency}
                                                onChange={(e) => setFrequency(e.target.value as any)}
                                                className="input"
                                            >
                                                <option value="Daily">Daily</option>
                                                <option value="Weekly">Weekly</option>
                                                <option value="Monthly">Monthly</option>
                                            </select>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-foreground mb-2">Repeat every</label>
                                            <div className="flex items-center gap-2">
                                                <input
                                                    type="number"
                                                    min="1"
                                                    value={interval}
                                                    onChange={(e) => setInterval(parseInt(e.target.value) || 1)}
                                                    className="input w-20"
                                                />
                                                <span className="text-foreground-secondary text-sm">
                                                    {frequency === 'Daily' ? 'day(s)' : frequency === 'Weekly' ? 'week(s)' : 'month(s)'}
                                                </span>
                                            </div>
                                        </div>
                                    </div>

                                    {frequency === 'Weekly' && (
                                        <div>
                                            <label className="block text-sm font-medium text-foreground mb-2">Repeat on</label>
                                            <div className="flex gap-2 flex-wrap">
                                                {['S', 'M', 'T', 'W', 'T', 'F', 'S'].map((day, i) => (
                                                    <button
                                                        key={i}
                                                        type="button"
                                                        onClick={() => {
                                                            if (daysOfWeek.includes(i)) {
                                                                if (daysOfWeek.length > 1) setDaysOfWeek(daysOfWeek.filter(d => d !== i));
                                                            } else {
                                                                setDaysOfWeek([...daysOfWeek, i]);
                                                            }
                                                        }}
                                                        className={cn(
                                                            'w-9 h-9 rounded-lg text-sm font-bold transition-all',
                                                            daysOfWeek.includes(i)
                                                                ? 'bg-primary-500 text-white shadow-md'
                                                                : 'bg-card border border-border text-foreground-secondary hover:border-primary-300'
                                                        )}
                                                    >
                                                        {day}
                                                    </button>
                                                ))}
                                            </div>
                                        </div>
                                    )}

                                    <div className="space-y-3">
                                        <label className="block text-sm font-medium text-foreground">Ends</label>
                                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                            <label className={cn(
                                                'flex items-center gap-3 p-3 rounded-xl border-2 transition-all cursor-pointer',
                                                endType === 'occurrences' ? 'border-primary-500 bg-brand-subtle' : 'border-border-subtle bg-card'
                                            )}>
                                                <input
                                                    type="radio"
                                                    checked={endType === 'occurrences'}
                                                    onChange={() => setEndType('occurrences')}
                                                    className="hidden"
                                                />
                                                <div className="flex-1">
                                                    <p className="text-sm font-semibold">After</p>
                                                    <div className="flex items-center gap-2 mt-1">
                                                        <input
                                                            type="number"
                                                            min="1"
                                                            max="100"
                                                            value={occurrences}
                                                            onChange={(e) => setOccurrences(parseInt(e.target.value) || 1)}
                                                            className="input py-1 px-2 h-8 w-16"
                                                            onClick={(e) => e.stopPropagation()}
                                                        />
                                                        <span className="text-xs text-foreground-secondary">occurrences</span>
                                                    </div>
                                                </div>
                                            </label>

                                            <label className={cn(
                                                'flex items-center gap-3 p-3 rounded-xl border-2 transition-all cursor-pointer',
                                                endType === 'date' ? 'border-primary-500 bg-brand-subtle' : 'border-border-subtle bg-card'
                                            )}>
                                                <input
                                                    type="radio"
                                                    checked={endType === 'date'}
                                                    onChange={() => setEndType('date')}
                                                    className="hidden"
                                                />
                                                <div className="flex-1">
                                                    <p className="text-sm font-semibold">On</p>
                                                    <input
                                                        type="date"
                                                        value={endDate}
                                                        onChange={(e) => setEndDate(e.target.value)}
                                                        className="input py-1 px-2 h-8 w-full mt-1 text-xs"
                                                        onClick={(e) => e.stopPropagation()}
                                                    />
                                                </div>
                                            </label>
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Step 4: Select Client */}
                {step === 4 && (
                    <div className="space-y-4 animate-fade-in-up">
                        <h2 className="text-lg font-semibold text-foreground mb-4">Select Client</h2>

                        <div className="relative mb-4">
                            <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                            <input
                                type="text"
                                value={clientSearch}
                                onChange={(e) => setClientSearch(e.target.value)}
                                className="input pl-11"
                                placeholder="Search clients..."
                            />
                        </div>

                        <div className="space-y-3">
                            {clients
                                .filter(c => c.name.toLowerCase().includes(clientSearch.toLowerCase()))
                                .map((client) => (
                                    <button
                                        key={client.id}
                                        onClick={() => setSelectedClient(client.id)}
                                        className={cn(
                                            'w-full p-4 rounded-xl text-left transition-all border-2',
                                            selectedClient === client.id
                                                ? 'border-primary-500 bg-brand-subtle'
                                                : 'border-border bg-card hover:border-border-strong'
                                        )}
                                    >
                                        <div className="flex items-center gap-4">
                                            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-emerald-400 to-teal-600 flex items-center justify-center text-white font-bold">
                                                {client.name.split(' ').map(n => n[0]).join('')}
                                            </div>
                                            <div className="flex-1">
                                                <h3 className="font-semibold text-foreground">{client.name}</h3>
                                                <p className="text-sm text-foreground-secondary">{client.email}</p>
                                            </div>
                                            {selectedClient === client.id && (
                                                <div className="w-6 h-6 rounded-full bg-primary-500 flex items-center justify-center">
                                                    <Check className="h-4 w-4 text-white" />
                                                </div>
                                            )}
                                        </div>
                                    </button>
                                ))}
                        </div>

                        <button className="w-full p-4 border-2 border-dashed border-border-strong rounded-xl text-foreground-secondary hover:border-primary-400 hover:text-primary transition-all">
                            + Add New Client
                        </button>
                    </div>
                )}

                {/* Step 5: Confirmation */}
                {step === 5 && (
                    <div className="space-y-6 animate-fade-in-up">
                        <h2 className="text-lg font-semibold text-foreground mb-4">Booking Summary</h2>

                        <div className="card-elevated p-6 space-y-4">
                            <div className="flex items-center gap-4 pb-4 border-b border-border-subtle">
                                {selectedServiceData && (
                                    <>
                                        <div
                                            className="w-14 h-14 rounded-xl flex items-center justify-center text-white"
                                            style={{ backgroundColor: selectedServiceData.color }}
                                        >
                                            <Sparkles className="h-7 w-7" />
                                        </div>
                                        <div className="flex-1">
                                            <h3 className="font-semibold text-foreground">{selectedServiceData.name}</h3>
                                            <p className="text-sm text-foreground-secondary">{selectedServiceData.duration} minutes</p>
                                        </div>
                                        <span className="text-xl font-bold" style={{ color: selectedServiceData.color }}>
                                            {formatCurrency(selectedServiceData.price)}
                                        </span>
                                    </>
                                )}
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                <div className="bg-muted rounded-xl p-4">
                                    <div className="flex items-center gap-2 text-sm text-foreground-secondary mb-1">
                                        <User className="h-4 w-4" />
                                        Staff
                                    </div>
                                    <p className="font-medium text-foreground">
                                        {selectedStaff === 'any' ? 'Any Available' : selectedStaffData?.name}
                                    </p>
                                </div>
                                <div className="bg-muted rounded-xl p-4">
                                    <div className="flex items-center gap-2 text-sm text-foreground-secondary mb-1">
                                        <Calendar className="h-4 w-4" />
                                        Date & Time
                                    </div>
                                    <p className="font-medium text-foreground">
                                        {formatDateDisplay(selectedDate)} at {selectedTime}
                                    </p>
                                </div>
                            </div>

                            <div className="bg-muted rounded-xl p-4">
                                <div className="flex items-center gap-2 text-sm text-foreground-secondary mb-1">
                                    <User className="h-4 w-4" />
                                    Client
                                </div>
                                <p className="font-medium text-foreground">{selectedClientData?.name}</p>
                                <p className="text-sm text-foreground-secondary">{selectedClientData?.email}</p>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-foreground mb-2">
                                    Booking Notes (Optional)
                                </label>
                                <textarea
                                    value={notes}
                                    onChange={(e) => setNotes(e.target.value)}
                                    className="w-full px-4 py-3 rounded-xl border border-border focus:outline-none focus:ring-2 focus:ring-primary-500 transition-all resize-none"
                                    rows={3}
                                    placeholder="Add any notes for this booking..."
                                />
                            </div>
                        </div>
                    </div>
                )}
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between pt-8 animate-fade-in" style={{ animationDelay: '300ms' }}>
                {step > 1 ? (
                    <button onClick={() => setStep(step - 1)} className="btn btn-secondary">
                        <ChevronLeft className="h-4 w-4" />
                        Back
                    </button>
                ) : (
                    <Link href="/bookings" className="btn btn-secondary">
                        Cancel
                    </Link>
                )}

                {step < 5 ? (
                    <button
                        onClick={() => setStep(step + 1)}
                        disabled={!canProceed()}
                        className="btn btn-primary"
                    >
                        Continue
                        <ChevronRight className="h-4 w-4" />
                    </button>
                ) : (
                    <button
                        onClick={handleSubmit}
                        disabled={loading}
                        className="btn btn-primary"
                    >
                        {loading ? (
                            <>
                                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                Creating...
                            </>
                        ) : (
                            <>
                                <Check className="h-4 w-4" />
                                Confirm Booking
                            </>
                        )}
                    </button>
                )}
            </div>
        </div>
    );
}
