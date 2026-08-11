'use client';

import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { 
    Clock, 
    DollarSign, 
    Info, 
    Settings, 
    Layout, 
    Users, 
    ShieldCheck, 
    Palette,
    History
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { currencySymbol, currencyStep, getCurrency } from '@/lib/currency';

const serviceSchema = z.object({
    name: z.string().min(1, 'Service name is required'),
    description: z.string().optional(),
    durationMinutes: z.number().min(1, 'Duration must be at least 1 minute'),
    price: z.number().min(0, 'Price cannot be negative'),
    currency: z.string().default('USD'),
    color: z.string().default('#3B82F6'),
    bufferBefore: z.number().min(0).default(0),
    bufferAfter: z.number().min(0).default(0),
    maxAttendees: z.number().min(1).default(1),
    requiresPayment: z.boolean().default(false),
    depositAmount: z.number().min(0).optional(),
    isActive: z.boolean().default(true),
});

type ServiceFormData = z.infer<typeof serviceSchema>;

interface ServiceFormProps {
    initialData?: Partial<ServiceFormData>;
    onSubmit: (data: ServiceFormData) => void;
    isLoading?: boolean;
}

export default function ServiceForm({ initialData, onSubmit, isLoading }: ServiceFormProps) {
    const {
        register,
        handleSubmit,
        watch,
        formState: { errors },
    } = useForm<ServiceFormData>({
        resolver: zodResolver(serviceSchema),
        defaultValues: {
            name: '',
            description: '',
            durationMinutes: 30,
            price: 0,
            currency: 'USD',
            color: '#3B82F6',
            bufferBefore: 0,
            bufferAfter: 0,
            maxAttendees: 1,
            requiresPayment: false,
            isActive: true,
            ...initialData,
        },
    });

    const requiresPayment = watch('requiresPayment');
    const selectedCurrency = watch('currency') || 'USD';
    const selectedDecimals = getCurrency(selectedCurrency).decimals;

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-8 animate-fade-in">
            {/* Basic Information Section */}
            <div className="card p-6 md:p-8 space-y-6">
                <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
                    <div className="w-10 h-10 rounded-xl bg-primary-50 flex items-center justify-center text-primary-600">
                        <Info className="w-5 h-5" />
                    </div>
                    <div>
                        <h3 className="text-lg font-semibold text-slate-900">Basic Information</h3>
                        <p className="text-sm text-slate-500">How your service appears to clients</p>
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="space-y-2">
                        <label className="text-sm font-medium text-slate-700 ml-1">Service Name</label>
                        <input
                            {...register('name')}
                            className={cn("input", errors.name && "border-red-500")}
                            placeholder="e.g. Initial Consultation"
                        />
                        {errors.name && <p className="text-xs text-red-500 ml-1">{errors.name.message}</p>}
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-slate-700 ml-1">Service Color</label>
                        <div className="flex gap-3">
                            <input
                                {...register('color')}
                                type="color"
                                className="h-10 w-20 rounded-lg cursor-pointer bg-transparent"
                            />
                            <div className="flex-1 flex items-center gap-2 px-3 border border-slate-200 rounded-lg text-sm text-slate-500 bg-slate-50">
                                <Palette className="w-4 h-4" />
                                {watch('color')}
                            </div>
                        </div>
                    </div>

                    <div className="md:col-span-2 space-y-2">
                        <label className="text-sm font-medium text-slate-700 ml-1">Description</label>
                        <textarea
                            {...register('description')}
                            className="input min-h-[100px] py-3"
                            placeholder="Tell your clients what this service is about..."
                        />
                    </div>
                </div>
            </div>

            {/* Pricing & Duration Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                <div className="card p-6 md:p-8 space-y-6">
                    <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
                        <div className="w-10 h-10 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-600">
                            <DollarSign className="w-5 h-5" />
                        </div>
                        <div>
                            <h3 className="text-lg font-semibold text-slate-900">Pricing</h3>
                            <p className="text-sm text-slate-500">Set your service rates</p>
                        </div>
                    </div>

                    <div className="space-y-6">
                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <label className="text-sm font-medium text-slate-700 ml-1">Price</label>
                                <div className="relative">
                                    {/* Symbol and step follow the selected currency. Both were
                                        hardcoded to dollars, so a tenant pricing in yen saw "$"
                                        beside the field and could enter fractional yen. */}
                                    <div className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
                                        {currencySymbol(selectedCurrency)}
                                    </div>
                                    <input
                                        {...register('price', { valueAsNumber: true })}
                                        type="number"
                                        step={currencyStep(selectedCurrency)}
                                        className={cn("input pl-7", errors.price && "border-red-500")}
                                        placeholder={selectedDecimals === 0 ? '0' : '0.00'}
                                    />
                                </div>
                                {errors.price && <p className="text-xs text-red-500 ml-1">{errors.price.message}</p>}
                            </div>
                            <div className="space-y-2">
                                <label className="text-sm font-medium text-slate-700 ml-1">Currency</label>
                                {/* Read-only, and deliberately so. Currency is a property of the
                                    Stripe account the business settles through — the account's
                                    country fixes it — so offering a choice here only lets a tenant
                                    pick one their account would have to convert out of. It also
                                    kept prices within a business in a single currency, which the
                                    revenue totals depend on. */}
                                <div
                                    className="input bg-slate-50 text-slate-600 flex items-center justify-between cursor-not-allowed"
                                    aria-readonly="true"
                                >
                                    <span>{selectedCurrency} ({currencySymbol(selectedCurrency)})</span>
                                    <span className="text-xs text-slate-400">from Stripe</span>
                                </div>
                                <input type="hidden" {...register('currency')} />
                                <p className="text-xs text-slate-500 ml-1">
                                    Set by your connected Stripe account.
                                </p>
                            </div>
                        </div>

                        <div className="space-y-4 pt-2">
                            <label className="flex items-center gap-3 cursor-pointer group">
                                <input
                                    type="checkbox"
                                    {...register('requiresPayment')}
                                    className="w-5 h-5 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                                />
                                <span className="text-sm font-medium text-slate-700 group-hover:text-slate-900 transition-colors">
                                    Requires payment at booking
                                </span>
                            </label>

                            {requiresPayment && (
                                <div className="pl-8 animate-fade-in">
                                    <div className="space-y-2 max-w-[200px]">
                                        <label className="text-xs font-medium text-slate-500">Deposit Amount (Optional)</label>
                                        <div className="relative">
                                            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">$</div>
                                            <input
                                                {...register('depositAmount', { valueAsNumber: true })}
                                                type="number"
                                                step="0.01"
                                                className="input pl-7 py-2"
                                                placeholder="e.g. 20.00"
                                            />
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div className="card p-6 md:p-8 space-y-6">
                    <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
                        <div className="w-10 h-10 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600">
                            <Clock className="w-5 h-5" />
                        </div>
                        <div>
                            <h3 className="text-lg font-semibold text-slate-900">Duration & Scheduling</h3>
                            <p className="text-sm text-slate-500">Manage your time effectively</p>
                        </div>
                    </div>

                    <div className="space-y-6">
                        <div className="space-y-2">
                            <label className="text-sm font-medium text-slate-700 ml-1">Service Duration (Minutes)</label>
                            <div className="grid grid-cols-4 gap-2">
                                {[15, 30, 45, 60].map((val) => (
                                    <button
                                        key={val}
                                        type="button"
                                        onClick={() => {}} // Handle quick select if needed
                                        className="py-2 text-xs font-medium border border-slate-200 rounded-lg hover:border-primary-500 hover:text-primary-600 transition-all"
                                    >
                                        {val}m
                                    </button>
                                ))}
                            </div>
                            <input
                                {...register('durationMinutes', { valueAsNumber: true })}
                                type="number"
                                className={cn("input mt-2", errors.durationMinutes && "border-red-500")}
                            />
                            {errors.durationMinutes && <p className="text-xs text-red-500 ml-1">{errors.durationMinutes.message}</p>}
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <label className="text-xs font-medium text-slate-500 ml-1">Buffer Before (m)</label>
                                <input
                                    {...register('bufferBefore', { valueAsNumber: true })}
                                    type="number"
                                    className="input"
                                />
                            </div>
                            <div className="space-y-2">
                                <label className="text-xs font-medium text-slate-500 ml-1">Buffer After (m)</label>
                                <input
                                    {...register('bufferAfter', { valueAsNumber: true })}
                                    type="number"
                                    className="input"
                                />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Advanced Settings */}
            <div className="card p-6 md:p-8 space-y-6">
                <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
                    <div className="w-10 h-10 rounded-xl bg-primary-50 flex items-center justify-center text-primary-600">
                        <Settings className="w-5 h-5" />
                    </div>
                    <div>
                        <h3 className="text-lg font-semibold text-slate-900">Advanced Settings</h3>
                        <p className="text-sm text-slate-500">Capacity and status control</p>
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    <div className="space-y-4">
                        <div className="flex items-center justify-between p-4 bg-slate-50 rounded-xl border border-slate-100">
                            <div className="flex items-center gap-3">
                                <Users className="w-5 h-5 text-slate-400" />
                                <div>
                                    <p className="text-sm font-medium text-slate-700">Maximum Attendees</p>
                                    <p className="text-xs text-slate-500">Set for group sessions</p>
                                </div>
                            </div>
                            <input
                                {...register('maxAttendees', { valueAsNumber: true })}
                                type="number"
                                className="input w-20 text-center"
                            />
                        </div>
                    </div>

                    <div className="space-y-4">
                        <div className="flex items-center justify-between p-4 bg-slate-50 rounded-xl border border-slate-100">
                            <div className="flex items-center gap-3">
                                <ShieldCheck className="w-5 h-5 text-slate-400" />
                                <div>
                                    <p className="text-sm font-medium text-slate-700">Service Status</p>
                                    <p className="text-xs text-slate-500">Currently accepting bookings</p>
                                </div>
                            </div>
                            <label className="relative inline-flex items-center cursor-pointer">
                                <input
                                    type="checkbox"
                                    {...register('isActive')}
                                    className="sr-only peer"
                                />
                                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-100 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-500"></div>
                            </label>
                        </div>
                    </div>
                </div>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-end gap-3 pt-4">
                <button
                    type="button"
                    className="btn btn-secondary px-8"
                    disabled={isLoading}
                >
                    Cancel
                </button>
                <button
                    type="submit"
                    className="btn btn-primary px-12 shadow-primary-500/25"
                    disabled={isLoading}
                >
                    {isLoading ? (
                        <>
                            <div className="spinner h-4 w-4 border-white/30 border-t-white" />
                            Saving...
                        </>
                    ) : (
                        'Save Service'
                    )}
                </button>
            </div>
        </form>
    );
}
