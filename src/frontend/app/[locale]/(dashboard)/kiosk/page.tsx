"use client";

import { useState, useEffect } from 'react';
import { Monitor, QrCode, ClipboardList, Settings, Play, X, Search, CheckCircle2, User } from 'lucide-react';
import { apiClient } from '@/lib/api';
import { useAuthStore } from '@/store/authStore';
import { useTranslations } from 'next-intl';
import { IService } from '@/types';
import { toast } from 'sonner';

interface IKioskSearchResult {
    id: string;
    clientId: string;
    startTime: string;
    client?: { firstName: string, lastName: string };
    service?: { name: string };
}

export default function KioskPage() {
    const t = useTranslations('Kiosk');
    const { user } = useAuthStore();
    const [stats, setStats] = useState({ activeToday: 0, waiversSigned: 0 });
    const [isKioskOpen, setIsKioskOpen] = useState(false);

    // Kiosk State
    const [kioskTab, setKioskTab] = useState<'checkin' | 'walkin'>('checkin');
    const [searchQuery, setSearchQuery] = useState('');
    const [searchResults, setSearchResults] = useState<IKioskSearchResult[]>([]);
    const [searching, setSearching] = useState(false);
    const [availableServices, setAvailableServices] = useState<IService[]>([]);

    const [walkInForm, setWalkInForm] = useState({
        firstName: '',
        lastName: '',
        phone: '',
        email: '',
        serviceId: '',
        groupSize: '1'
    });

    useEffect(() => {
        const init = async () => {
            try {
                const res = await apiClient.get('/api/v1/kiosk/stats', {
                    params: { tenantId: user?.tenantId }
                });
                setStats({
                    activeToday: res.data?.activeToday ?? res.data?.checkInsToday ?? 0,
                    waiversSigned: res.data?.waiversSigned ?? res.data?.waiverCount ?? 0,
                });
            } catch (err) {
                console.error('Failed to load kiosk stats:', err);
                setStats({ activeToday: 0, waiversSigned: 0 });
            }
        };
        if (user?.tenantId) init();
    }, [user?.tenantId]);

    const fetchServices = async () => {
        if (!user?.tenantId) return;
        try {
            const res = await apiClient.get('/api/v1/kiosk/services', { params: { tenantId: user.tenantId } });
            setAvailableServices(res.data?.services || []);
        } catch (err) {
            console.error('Failed to fetch services:', err);
        }
    };

    const handleOpenKiosk = () => {
        setIsKioskOpen(true);
        fetchServices();
    };

    const handleSearch = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!searchQuery.trim() || !user?.tenantId) return;
        
        try {
            setSearching(true);
            const isEmail = searchQuery.includes('@');
            const isDigits = /^\d+$/.test(searchQuery.replace(/\D/g, ''));
            const params: Record<string, string> = { tenantId: user.tenantId };
            
            if (isEmail) params.email = searchQuery;
            else if (isDigits) params.phone = searchQuery;
            else params.name = searchQuery;

            const res = await apiClient.get('/api/v1/kiosk/search', { params });
            setSearchResults(res.data?.results || []);
        } catch (err) {
            console.error('Search failed:', err);
            setSearchResults([]);
        } finally {
            setSearching(false);
        }
    };

    const handleCheckIn = async (bookingId: string) => {
        if (!user?.tenantId) return;
        try {
            await apiClient.post(`/api/v1/kiosk/check-in/${bookingId}?tenantId=${user.tenantId}`);
            toast.success(t('checkInSuccess'));
            setSearchResults(prev => prev.filter(b => b.id !== bookingId));
        } catch (err: any) {
            console.error('Check in failed:', err);
            toast.error(err.response?.data?.error || t('checkInError'));
        }
    };

    const handleWalkIn = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!user?.tenantId) return;
        
        try {
            await apiClient.post('/api/v1/kiosk/walk-in', {
                tenantId: user.tenantId,
                serviceId: walkInForm.serviceId,
                firstName: walkInForm.firstName,
                lastName: walkInForm.lastName,
                phone: walkInForm.phone,
                email: walkInForm.email,
                groupSize: parseInt(walkInForm.groupSize, 10)
            });
            toast.success(t('walkInSuccess'));
            setWalkInForm({ firstName: '', lastName: '', phone: '', email: '', serviceId: '', groupSize: '1' });
            setKioskTab('checkin');
        } catch (err: any) {
            console.error('Walk-in failed:', err);
            toast.error(err.response?.data?.error || t('walkInError'));
        }
    };

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-bold text-foreground" style={{ fontFamily: 'var(--font-display)' }}>
                    {t('title')}
                </h1>
                <p className="text-sm text-foreground-secondary">{t('description')}</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Launch Kiosk Card */}
                <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                    <div className="h-16 w-16 bg-brand-subtle rounded-2xl flex items-center justify-center text-primary mb-4">
                        <Monitor className="h-8 w-8" />
                    </div>
                    <h3 className="text-lg font-bold text-foreground mb-2">{t('selfServiceTitle')}</h3>
                    <p className="text-sm text-foreground-secondary mb-6">{t('selfServiceDesc')}</p>
                    <button onClick={handleOpenKiosk} className="btn btn-primary w-full shadow-lg shadow-primary-500/25">
                        <Play className="h-4 w-4 mr-2" />
                        {t('launchBtn')}
                    </button>
                </div>

                {/* QR Code Booking */}
                <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                    <div className="h-16 w-16 bg-emerald-50 rounded-2xl flex items-center justify-center text-emerald-600 mb-4">
                        <QrCode className="h-8 w-8" />
                    </div>
                    <h3 className="text-lg font-bold text-foreground mb-2">{t('qrTitle')}</h3>
                    <p className="text-sm text-foreground-secondary mb-6">{t('qrDesc')}</p>
                    <button className="btn bg-card border border-border text-foreground hover:bg-accent w-full">
                        {t('downloadQrBtn')}
                    </button>
                </div>

                {/* Digital Waivers */}
                <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                    <div className="h-16 w-16 bg-blue-50 rounded-2xl flex items-center justify-center text-blue-600 mb-4">
                        <ClipboardList className="h-8 w-8" />
                    </div>
                    <h3 className="text-lg font-bold text-foreground mb-2">{t('waiversTitle')}</h3>
                    <p className="text-sm text-foreground-secondary mb-6">{t('waiversDesc')}</p>
                    <button className="btn bg-card border border-border text-foreground hover:bg-accent w-full">
                        <Settings className="h-4 w-4 mr-2" />
                        {t('configureWaiversBtn')}
                    </button>
                </div>
            </div>

            {/* Kiosk Simulator Modal */}
            {isKioskOpen && (
                <div className="fixed inset-0 z-[100] bg-slate-900 flex flex-col">
                    <div className="flex justify-between items-center p-6 border-b border-slate-800 bg-slate-900 text-white">
                        <h2 className="text-2xl font-bold" style={{ fontFamily: 'var(--font-display)' }}>{t('welcomeTitle')}</h2>
                        <button onClick={() => setIsKioskOpen(false)} className="px-4 py-2 bg-slate-800 hover:bg-slate-700 rounded-lg font-medium transition-colors flex items-center gap-2">
                            <X className="h-4 w-4" /> {t('exitKiosk')}
                        </button>
                    </div>
                    
                    <div className="flex-1 overflow-y-auto bg-muted flex items-center justify-center p-6">
                        <div className="bg-card rounded-3xl shadow-xl w-full max-w-3xl overflow-hidden min-h-[600px] flex flex-col">
                            <div className="flex border-b border-border-subtle">
                                <button 
                                    className={`flex-1 py-5 text-center font-bold text-lg transition-colors ${kioskTab === 'checkin' ? 'bg-brand-subtle text-primary border-b-2 border-primary-600' : 'text-foreground-secondary hover:bg-accent'}`}
                                    onClick={() => setKioskTab('checkin')}
                                >
                                    {t('tabCheckIn')}
                                </button>
                                <button 
                                    className={`flex-1 py-5 text-center font-bold text-lg transition-colors ${kioskTab === 'walkin' ? 'bg-brand-subtle text-primary border-b-2 border-primary-600' : 'text-foreground-secondary hover:bg-accent'}`}
                                    onClick={() => setKioskTab('walkin')}
                                >
                                    {t('tabWalkIn')}
                                </button>
                            </div>

                            <div className="p-8 flex-1 flex flex-col">
                                {kioskTab === 'checkin' ? (
                                    <div className="max-w-xl mx-auto w-full flex-1 flex flex-col">
                                        <div className="text-center mb-8">
                                            <h3 className="text-3xl font-bold text-foreground mb-3" style={{ fontFamily: 'var(--font-display)' }}>{t('checkInHeader')}</h3>
                                            <p className="text-foreground-secondary text-lg">{t('checkInDesc')}</p>
                                        </div>
                                        
                                        <form onSubmit={handleSearch} className="relative mb-8">
                                            <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-6 w-6 text-foreground-muted" />
                                            <input 
                                                type="text" 
                                                placeholder={t('searchPlaceholder')} 
                                                className="w-full pl-14 pr-6 py-4 text-xl bg-muted border border-border rounded-2xl focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-all"
                                                value={searchQuery}
                                                onChange={(e) => setSearchQuery(e.target.value)}
                                            />
                                            <button type="submit" disabled={searching || !searchQuery} className="absolute right-3 top-1/2 -translate-y-1/2 btn btn-primary py-2 hidden sm:flex">
                                                {searching ? t('searchingBtn') : t('searchBtn')}
                                            </button>
                                        </form>

                                        <div className="flex-1 space-y-4 overflow-y-auto">
                                            {searchResults.length > 0 ? (
                                                searchResults.map(b => (
                                                    <div key={b.id} className="border border-border rounded-2xl p-6 flex flex-col sm:flex-row items-center justify-between gap-4 bg-card hover:border-primary-300 transition-colors">
                                                        <div>
                                                            <h4 className="font-bold text-xl text-foreground">{b.client?.firstName} {b.client?.lastName}</h4>
                                                            <p className="text-foreground-secondary">{b.service?.name} at {new Date(b.startTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</p>
                                                        </div>
                                                        <button 
                                                            onClick={() => handleCheckIn(b.id)}
                                                            className="btn btn-primary w-full sm:w-auto px-8 py-3 text-lg"
                                                        >
                                                            {t('checkInNowBtn')}
                                                        </button>
                                                    </div>
                                                ))
                                            ) : (
                                                searchQuery && !searching && (
                                                    <div className="text-center py-12 text-foreground-secondary flex flex-col items-center">
                                                        <User className="h-12 w-12 text-slate-300 mb-4" />
                                                        <p className="text-lg">{t('noAppointments')}</p>
                                                        <button onClick={() => setKioskTab('walkin')} className="text-primary font-medium mt-2 hover:underline">
                                                            {t('registerWalkInLink')}
                                                        </button>
                                                    </div>
                                                )
                                            )}
                                        </div>
                                    </div>
                                ) : (
                                    <div className="max-w-2xl mx-auto w-full">
                                        <div className="text-center mb-8">
                                            <h3 className="text-3xl font-bold text-foreground mb-3" style={{ fontFamily: 'var(--font-display)' }}>{t('walkInHeader')}</h3>
                                            <p className="text-foreground-secondary text-lg">{t('walkInDesc')}</p>
                                        </div>

                                        <form onSubmit={handleWalkIn} className="space-y-6 bg-card border border-border-subtle p-8 rounded-3xl shadow-sm">
                                            <div>
                                                <label className="block text-sm font-bold text-foreground mb-2 uppercase tracking-wide">{t('formService')}</label>
                                                <select 
                                                    required 
                                                    className="w-full p-4 bg-muted border border-border rounded-xl text-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                                                    value={walkInForm.serviceId}
                                                    onChange={e => setWalkInForm(prev => ({...prev, serviceId: e.target.value}))}
                                                >
                                                    <option value="" disabled>{t('formServicePlaceholder')}</option>
                                                    {availableServices.map(s => (
                                                        <option key={s.id} value={s.id}>{s.name} ({s.durationMinutes} min)</option>
                                                    ))}
                                                </select>
                                            </div>

                                            <div className="grid grid-cols-2 gap-6">
                                                <div>
                                                    <label className="block text-sm font-bold text-foreground mb-2 uppercase tracking-wide">{t('formFirstName')}</label>
                                                    <input 
                                                        type="text" required 
                                                        className="w-full p-4 bg-muted border border-border rounded-xl text-lg"
                                                        value={walkInForm.firstName}
                                                        onChange={e => setWalkInForm(prev => ({...prev, firstName: e.target.value}))}
                                                    />
                                                </div>
                                                <div>
                                                    <label className="block text-sm font-bold text-foreground mb-2 uppercase tracking-wide">{t('formLastName')}</label>
                                                    <input 
                                                        type="text" 
                                                        className="w-full p-4 bg-muted border border-border rounded-xl text-lg"
                                                        value={walkInForm.lastName}
                                                        onChange={e => setWalkInForm(prev => ({...prev, lastName: e.target.value}))}
                                                    />
                                                </div>
                                            </div>

                                            <div className="grid grid-cols-2 gap-6">
                                                <div>
                                                    <label className="block text-sm font-bold text-foreground mb-2 uppercase tracking-wide">{t('formPhone')}</label>
                                                    <input 
                                                        type="tel" required 
                                                        className="w-full p-4 bg-muted border border-border rounded-xl text-lg"
                                                        value={walkInForm.phone}
                                                        onChange={e => setWalkInForm(prev => ({...prev, phone: e.target.value}))}
                                                    />
                                                </div>
                                                <div>
                                                    <label className="block text-sm font-bold text-foreground mb-2 uppercase tracking-wide">{t('formEmail')}</label>
                                                    <input 
                                                        type="email" 
                                                        className="w-full p-4 bg-muted border border-border rounded-xl text-lg"
                                                        value={walkInForm.email}
                                                        onChange={e => setWalkInForm(prev => ({...prev, email: e.target.value}))}
                                                    />
                                                </div>
                                            </div>

                                            <button type="submit" className="w-full btn btn-primary py-5 text-xl rounded-xl mt-4">
                                                <CheckCircle2 className="h-6 w-6 mr-2" />
                                                {t('registerBtn')}
                                            </button>
                                        </form>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
