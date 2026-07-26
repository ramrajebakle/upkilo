'use client';

import React, { useEffect, useState } from 'react';
import { Link } from '@/navigation';
import { 
    Plus, 
    Search, 
    Filter, 
    MoreVertical, 
    Edit2, 
    Trash2, 
    Clock, 
    DollarSign, 
    Users,
    CheckCircle2,
    XCircle,
    Package,
    ChevronRight,
    Loader2
} from 'lucide-react';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';

interface Service {
    id: string;
    name: string;
    description: string;
    durationMinutes: number;
    price: number;
    currency: string;
    color: string;
    isActive: boolean;
    maxAttendees: number;
}

export default function ServicesPage() {
    const [services, setServices] = useState<Service[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');
    const { error } = useToast();

    useEffect(() => {
        fetchServices();
    }, []);

    const fetchServices = async () => {
        try {
            setIsLoading(true);
            const response = await api.services.list();
            setServices(response.data.data || []);
        } catch (err) {
            console.error('Error fetching services:', err);
            error('Failed to load services');
        } finally {
            setIsLoading(false);
        }
    };

    const filteredServices = services.filter(service => 
        service.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        service.description?.toLowerCase().includes(searchQuery.toLowerCase())
    );

    return (
        <div className="space-y-8 animate-fade-in">
            {/* Header section */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 px-1">
                <div>
                    <h1 className="text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>Services</h1>
                    <p className="text-slate-500 dark:text-slate-400 mt-1 font-medium">Manage your appointment types and class offerings</p>
                </div>
                <Link 
                    href="/services/new" 
                    className="btn btn-primary px-8 py-3 rounded-2xl shadow-xl shadow-primary-500/25 transition-all hover:-translate-y-0.5 active:scale-95"
                >
                    <Plus className="w-5 h-5" />
                    <span className="font-bold uppercase tracking-widest text-xs">Create Service</span>
                </Link>
            </div>

            {/* Filters & Search */}
            <div className="card-elevated p-4 md:p-5 flex flex-col md:flex-row gap-4 items-center dark:bg-slate-900 dark:border-slate-800 shadow-lg">
                <div className="relative flex-1 group w-full">
                    <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400 group-focus-within:text-primary-500 transition-colors" />
                    <input 
                        type="text"
                        placeholder="Search services..."
                        className="input pl-12 bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 dark:text-white dark:placeholder-slate-600 rounded-xl"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                    />
                </div>
                <div className="flex gap-3 w-full md:w-auto">
                    <button className="btn btn-secondary flex-1 md:flex-none dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 rounded-xl">
                        <Filter className="w-4 h-4" />
                        <span className="font-bold uppercase tracking-widest text-[10px]">Filter</span>
                    </button>
                    <div className="w-px h-10 bg-slate-200 dark:bg-slate-800 hidden md:block" />
                    <div className="flex bg-slate-100 dark:bg-slate-800 p-1 rounded-xl shadow-inner border border-slate-200 dark:border-slate-700">
                        <button className="px-5 py-1.5 text-[10px] font-bold uppercase tracking-widest rounded-lg bg-white dark:bg-slate-700 shadow-md text-primary-600 dark:text-white">All</button>
                        <button className="px-5 py-1.5 text-[10px] font-bold uppercase tracking-widest rounded-lg text-slate-500 dark:text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-colors">Active</button>
                        <button className="px-5 py-1.5 text-[10px] font-bold uppercase tracking-widest rounded-lg text-slate-500 dark:text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-colors">Inactive</button>
                    </div>
                </div>
            </div>

            {/* List View */}
            {isLoading ? (
                <div className="flex flex-col items-center justify-center py-24 gap-6">
                    <div className="relative">
                        <Loader2 className="w-12 h-12 text-primary-500 animate-spin" />
                        <div className="absolute inset-0 blur-xl bg-primary-500/20" />
                    </div>
                    <p className="text-slate-500 dark:text-slate-400 font-bold uppercase tracking-widest text-xs">Discovering your catalog...</p>
                </div>
            ) : filteredServices.length > 0 ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {filteredServices.map((service, index) => (
                        <div 
                            key={service.id} 
                            className="group card-elevated hover-premium flex flex-col stagger-1 dark:bg-slate-900 dark:border-slate-800 shadow-xl overflow-hidden"
                            style={{ animationDelay: `${index * 50}ms` }}
                        >
                            <div className="p-7 flex-1 space-y-6">
                                <div className="flex items-start justify-between">
                                    <div 
                                        className="w-16 h-16 rounded-2xl flex items-center justify-center shadow-lg transform transition-transform group-hover:scale-110 group-hover:rotate-3"
                                        style={{ backgroundColor: `${service.color}15`, color: service.color, border: `1px solid ${service.color}33` }}
                                    >
                                        <Package className="w-8 h-8" />
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <div className={cn(
                                            "px-3 py-1 rounded-lg text-[10px] font-bold uppercase tracking-widest flex items-center gap-1.5 shadow-sm border",
                                            service.isActive 
                                                ? "bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-800/50" 
                                                : "bg-rose-50 dark:bg-rose-900/20 text-rose-600 dark:text-rose-400 border-rose-100 dark:border-rose-800/50"
                                        )}>
                                            {service.isActive ? (
                                                <CheckCircle2 className="w-3 h-3" />
                                            ) : (
                                                <XCircle className="w-3 h-3" />
                                            )}
                                            {service.isActive ? 'Active' : 'Inactive'}
                                        </div>
                                        <button className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-all text-slate-400 hover:text-slate-600 active:scale-90">
                                            <MoreVertical className="w-4 h-4" />
                                        </button>
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <h3 className="text-xl font-bold text-slate-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                                        {service.name}
                                    </h3>
                                    <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-2 leading-relaxed h-10 font-medium">
                                        {service.description || 'Elevate your experience with our premium service catalog featuring world-class quality.'}
                                    </p>
                                </div>

                                <div className="grid grid-cols-2 gap-4 pt-4 border-t border-slate-100 dark:border-slate-800/50">
                                    <div className="flex flex-col gap-1">
                                        <span className="text-[10px] font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500">Duration</span>
                                        <div className="flex items-center gap-2 text-slate-700 dark:text-slate-300">
                                            <Clock className="w-4 h-4 text-primary-500" />
                                            <span className="text-sm font-bold">{service.durationMinutes} min</span>
                                        </div>
                                    </div>
                                    <div className="flex flex-col gap-1">
                                        <span className="text-[10px] font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500">Pricing</span>
                                        <div className="flex items-center gap-1.5 text-slate-900 dark:text-white">
                                            <div className="p-1 bg-indigo-50 dark:bg-indigo-900/20 rounded-md">
                                                <DollarSign className="w-3.5 h-3.5 text-indigo-600 dark:text-indigo-400" />
                                            </div>
                                            <span className="text-sm font-black whitespace-nowrap">
                                                {service.price} <span className="text-[10px] text-slate-400 font-bold">{service.currency}</span>
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="px-7 py-5 bg-slate-50/50 dark:bg-slate-950/50 border-t border-slate-100 dark:border-slate-800 rounded-b-2xl flex items-center justify-between group-hover:bg-slate-100 dark:group-hover:bg-slate-900 transition-colors">
                                <Link 
                                    href={`/services/${service.id}`}
                                    className="text-primary-600 dark:text-primary-400 font-bold text-xs uppercase tracking-widest hover:text-primary-700 dark:hover:text-primary-300 transition-all flex items-center gap-2 group/link"
                                >
                                    View Service
                                    <ChevronRight className="w-4 h-4 group-hover/link:translate-x-1 transition-transform" />
                                </Link>
                                <div className="flex gap-2">
                                    <button className="p-2.5 hover:bg-red-50 dark:hover:bg-red-900/40 hover:text-red-600 text-slate-400 dark:text-slate-500 rounded-xl transition-all active:scale-90 border border-transparent hover:border-red-100 dark:hover:border-red-900/50" title="Delete Service">
                                        <Trash2 className="w-4 h-4" />
                                    </button>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="card-elevated p-16 flex flex-col items-center text-center gap-8 animate-scale-in dark:bg-slate-900 dark:border-slate-800 shadow-xl">
                    <div className="relative">
                        <div className="w-24 h-24 rounded-full bg-slate-50 dark:bg-slate-800 flex items-center justify-center text-slate-300 dark:text-slate-600">
                            <Package className="w-12 h-12" />
                        </div>
                        <div className="absolute -inset-4 blur-2xl bg-indigo-500/5 rounded-full" />
                    </div>
                    <div className="max-w-md space-y-2">
                        <h3 className="text-2xl font-black text-slate-900 dark:text-white tracking-tight">No services found</h3>
                        <p className="text-slate-500 dark:text-slate-400 font-medium leading-relaxed">
                            {searchQuery 
                                ? `No results for "${searchQuery}". Maybe check your spelling or try another keyword?`
                                : "Your business catalog is currently empty. Scale your business by adding your premier services."}
                        </p>
                    </div>
                    {!searchQuery && (
                        <Link href="/services/new" className="btn btn-primary px-10 py-3.5 rounded-2xl shadow-xl shadow-primary-500/25">
                            <Plus className="w-5 h-5" />
                            <span className="font-bold uppercase tracking-widest text-xs">Add First Service</span>
                        </Link>
                    )}
                </div>
            )}
        </div>
    );
}
