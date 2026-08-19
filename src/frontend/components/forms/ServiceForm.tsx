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
    // Refund policy, set per service. These are what the cancellation endpoint reads to decide
    // how much of a deposit goes back, so the same rules the API enforces are validated here —
    // catching an inverted pair at the field rather than as a 400 after save.
    fullRefundHours: z.number().int().min(0, 'Cannot be negative').default(18),
    partialRefundHours: z.number().int().min(0, 'Cannot be negative').default(12),
    partialRefundPercent: z.number().min(0, 'Must be 0–100').max(100, 'Must be 0–100').default(50),
    cancellationPolicy: z.string().optional(),
    // Rebooking + mobile. Both are opt-in and vertical-neutral: a salon sets rebookAfterDays to
    // 42 for a colour, a med spa to 120 for botox, a detailer to 150 for a full detail.
    rebookAfterDays: z.number().int().min(0).max(1095, 'Three years is the practical maximum').optional(),
    isMobile: z.boolean().default(false),
    travelBufferMinutes: z.number().int().min(0).max(480, 'Eight hours is the practical maximum').default(0),
}).refine(
    // Mirrors ValidateRefundPolicy in ServicesController. Without this a tenant can save 12/18
    // the wrong way round, which reads as "the later you cancel, the more you get back".
    (v) => v.partialRefundHours <= v.fullRefundHours,
    {
        message: 'The partial-refund window must be shorter than the full-refund window.',
        path: ['partialRefundHours'],
    }
);

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
            // Stated here as well as in the zod schema: schema .default() is applied at parse
            // time, so without these the inputs render empty on a new service and the summary
            // beneath them reads "0h", which is a policy that refunds everything. They must
            // match the Service entity's initialisers and the migration's backfill.
            fullRefundHours: 18,
            partialRefundHours: 12,
            partialRefundPercent: 50,
            cancellationPolicy: '',
            // No default rebooking interval: leaving it blank means no reminder, which is the
            // only safe default for something that messages customers.
            isMobile: false,
            travelBufferMinutes: 0,
            ...initialData,
        },
    });

    const requiresPayment = watch('requiresPayment');
    const selectedCurrency = watch('currency') || 'USD';
    // Watched so the plain-English summary under the refund fields updates as they are typed.
    const isMobile = watch('isMobile');
    const fullRefundHours = watch('fullRefundHours');
    const partialRefundHours = watch('partialRefundHours');
    const partialRefundPercent = watch('partialRefundPercent');
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
                                <div className="pl-8 animate-fade-in space-y-6">
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

                                    {/* Refund policy sits under "requires payment" because it only has
                                        meaning once money is being taken — showing it on a free service
                                        would ask the tenant a question with no consequence. */}
                                    <fieldset className="space-y-3 border-t border-slate-100 pt-5">
                                        <legend className="text-sm font-semibold text-slate-700">
                                            Refund policy for this service
                                        </legend>
                                        <p className="text-xs text-slate-500 max-w-lg">
                                            How much of the deposit is returned when a client cancels, based on how
                                            much notice they give. Set per service, because a quick consultation and
                                            a long treatment rarely warrant the same notice.
                                        </p>

                                        <div className="grid gap-4 sm:grid-cols-3 max-w-2xl">
                                            <div className="space-y-1.5">
                                                <label htmlFor="fullRefundHours" className="text-xs font-medium text-slate-600">
                                                    Full refund beyond
                                                </label>
                                                <div className="relative">
                                                    <input
                                                        id="fullRefundHours"
                                                        {...register('fullRefundHours', { valueAsNumber: true })}
                                                        type="number"
                                                        min={0}
                                                        className="input py-2 pr-12"
                                                    />
                                                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500">
                                                        hours
                                                    </span>
                                                </div>
                                            </div>

                                            <div className="space-y-1.5">
                                                <label htmlFor="partialRefundHours" className="text-xs font-medium text-slate-600">
                                                    Partial refund beyond
                                                </label>
                                                <div className="relative">
                                                    <input
                                                        id="partialRefundHours"
                                                        {...register('partialRefundHours', { valueAsNumber: true })}
                                                        type="number"
                                                        min={0}
                                                        className="input py-2 pr-12"
                                                    />
                                                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500">
                                                        hours
                                                    </span>
                                                </div>
                                            </div>

                                            <div className="space-y-1.5">
                                                <label htmlFor="partialRefundPercent" className="text-xs font-medium text-slate-600">
                                                    Partial refund amount
                                                </label>
                                                <div className="relative">
                                                    <input
                                                        id="partialRefundPercent"
                                                        {...register('partialRefundPercent', { valueAsNumber: true })}
                                                        type="number"
                                                        min={0}
                                                        max={100}
                                                        className="input py-2 pr-8"
                                                    />
                                                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500">
                                                        %
                                                    </span>
                                                </div>
                                            </div>
                                        </div>

                                        {errors.partialRefundHours && (
                                            <p className="text-sm text-red-600">{errors.partialRefundHours.message}</p>
                                        )}
                                        {errors.partialRefundPercent && (
                                            <p className="text-sm text-red-600">{errors.partialRefundPercent.message}</p>
                                        )}

                                        {/* Restates the three tiers in the tenant's own numbers. The rules are
                                            easy to enter and hard to picture; showing the outcome is what stops
                                            a policy going live that its author did not intend. */}
                                        <div className="rounded-xl bg-slate-50 p-4 text-sm text-slate-700 max-w-2xl">
                                            <p className="font-medium text-slate-900">What a client gets back</p>
                                            <ul className="mt-2 space-y-1">
                                                <li>
                                                    Cancels more than <strong>{fullRefundHours || 0}h</strong> before →{' '}
                                                    <strong>100%</strong> of the deposit
                                                </li>
                                                <li>
                                                    Between <strong>{partialRefundHours || 0}h</strong> and{' '}
                                                    <strong>{fullRefundHours || 0}h</strong> before →{' '}
                                                    <strong>{partialRefundPercent ?? 0}%</strong>
                                                </li>
                                                <li>
                                                    Less than <strong>{partialRefundHours || 0}h</strong> before →{' '}
                                                    <strong>nothing</strong>, the deposit is kept
                                                </li>
                                            </ul>
                                        </div>

                                        <div className="space-y-1.5 max-w-2xl">
                                            <label htmlFor="cancellationPolicy" className="text-xs font-medium text-slate-600">
                                                Policy note shown to clients (optional)
                                            </label>
                                            <textarea
                                                id="cancellationPolicy"
                                                {...register('cancellationPolicy')}
                                                rows={2}
                                                className="input py-2"
                                                placeholder="e.g. Please give as much notice as you can so we can offer the slot to someone else."
                                            />
                                        </div>
                                    </fieldset>
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

                        {/* Mobile / travel. Sits with the other timing settings because that is
                            what it is — travel occupies the calendar exactly like turnaround. */}
                        <div className="space-y-4 border-t border-slate-100 pt-5">
                            <label className="flex items-center gap-3 cursor-pointer group">
                                <input
                                    type="checkbox"
                                    {...register('isMobile')}
                                    className="w-5 h-5 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                                />
                                <span className="text-sm font-medium text-slate-700 group-hover:text-slate-900 transition-colors">
                                    Performed at the client&apos;s location
                                </span>
                            </label>

                            {isMobile && (
                                <div className="pl-8 animate-fade-in space-y-2 max-w-[260px]">
                                    <label htmlFor="travelBufferMinutes" className="text-xs font-medium text-slate-500">
                                        Travel time each way
                                    </label>
                                    <div className="relative">
                                        <input
                                            id="travelBufferMinutes"
                                            {...register('travelBufferMinutes', { valueAsNumber: true })}
                                            type="number"
                                            min={0}
                                            className="input py-2 pr-16"
                                        />
                                        <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500">
                                            minutes
                                        </span>
                                    </div>
                                    <p className="text-xs text-slate-500">
                                        Held either side of the appointment so two mobile jobs cannot be booked
                                        back to back with no time to drive between them.
                                    </p>
                                    {errors.travelBufferMinutes && (
                                        <p className="text-sm text-red-600">{errors.travelBufferMinutes.message}</p>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* Rebooking interval */}
                        <div className="space-y-2 border-t border-slate-100 pt-5 max-w-[320px]">
                            <label htmlFor="rebookAfterDays" className="text-sm font-medium text-slate-700">
                                Remind clients to rebook after
                            </label>
                            <div className="relative max-w-[200px]">
                                <input
                                    id="rebookAfterDays"
                                    {...register('rebookAfterDays', {
                                        // An empty field must mean "no reminder", not 0 — and
                                        // valueAsNumber turns "" into NaN, which would fail
                                        // validation rather than clearing the setting.
                                        setValueAs: (v) => (v === '' || v === null ? undefined : Number(v)),
                                    })}
                                    type="number"
                                    min={0}
                                    placeholder="e.g. 42"
                                    className="input py-2 pr-12"
                                />
                                <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-500">
                                    days
                                </span>
                            </div>
                            <p className="text-xs text-slate-500">
                                Leave blank for no reminder. When set, clients who have not rebooked by then get
                                one message — only if they have opted in to marketing, and only once per visit.
                            </p>
                            {errors.rebookAfterDays && (
                                <p className="text-sm text-red-600">{errors.rebookAfterDays.message}</p>
                            )}
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
