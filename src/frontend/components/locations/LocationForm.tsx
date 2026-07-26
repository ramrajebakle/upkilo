'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Loader2, Save, ArrowLeft, Clock } from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';
import { LocationRequest } from '@/lib/api.locations';

const locationSchema = z.object({
    name: z.string().min(1, 'Name is required'),
    addressLine1: z.string().optional(),
    addressLine2: z.string().optional(),
    city: z.string().optional(),
    state: z.string().optional(),
    postalCode: z.string().optional(),
    country: z.string().optional(),
    phone: z.string().optional(),
    email: z.string().email('Invalid email').optional().or(z.literal('')),
    timezone: z.string().min(1, 'Timezone is required'),
    isActive: z.boolean().default(true),
});

type LocationFormData = z.infer<typeof locationSchema>;

interface LocationFormProps {
    initialData?: LocationRequest & { id?: string };
    onSubmit: (data: LocationRequest) => Promise<void>;
    isEditing?: boolean;
}

interface BusinessHour {
    day: number; // 0-6
    isOpen: boolean;
    open: string;
    close: string;
}

const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const DEFAULT_HOURS: BusinessHour[] = DAYS.map((_, i) => ({
    day: i,
    isOpen: i !== 0 && i !== 6, // Closed weekends by default
    open: '09:00',
    close: '17:00'
}));

export default function LocationForm({ initialData, onSubmit, isEditing = false }: LocationFormProps) {
    const router = useRouter();
    const [submitting, setSubmitting] = useState(false);
    const [businessHours, setBusinessHours] = useState<BusinessHour[]>(DEFAULT_HOURS);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue
    } = useForm<LocationFormData>({
        resolver: zodResolver(locationSchema),
        defaultValues: {
            name: '',
            timezone: 'UTC', // Should fetch from browser or tenant settings
            isActive: true,
            ...initialData
        }
    });

    useEffect(() => {
        if (initialData?.businessHours) {
            try {
                const parsed = JSON.parse(initialData.businessHours);
                if (Array.isArray(parsed)) {
                    setBusinessHours(parsed);
                }
            } catch (e) {
                console.error('Failed to parse business hours', e);
            }
        }
    }, [initialData]);

    const handleFormSubmit = async (data: LocationFormData) => {
        try {
            setSubmitting(true);
            await onSubmit({
                ...data,
                businessHours: JSON.stringify(businessHours)
            });
        } catch (error) {
            console.error(error);
        } finally {
            setSubmitting(false);
        }
    };

    const toggleDay = (index: number) => {
        const newHours = [...businessHours];
        newHours[index].isOpen = !newHours[index].isOpen;
        setBusinessHours(newHours);
    };

    const updateTime = (index: number, field: 'open' | 'close', value: string) => {
        const newHours = [...businessHours];
        newHours[index][field] = value;
        setBusinessHours(newHours);
    };

    return (
        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-8">
            {/* Header Actions */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Link
                        href="/dashboard/locations"
                        className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                    >
                        <ArrowLeft className="h-5 w-5 text-gray-500" />
                    </Link>
                    <div>
                        <h1 className="text-2xl font-bold text-gray-900">
                            {isEditing ? 'Edit Location' : 'Add Location'}
                        </h1>
                        <p className="text-gray-500">
                            {isEditing ? 'Update location details' : 'Add a new business location'}
                        </p>
                    </div>
                </div>
                <button
                    type="submit"
                    disabled={submitting}
                    className="btn btn-primary min-w-[120px]"
                >
                    {submitting ? (
                        <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                        <Save className="h-4 w-4 mr-2" />
                    )}
                    {isEditing ? 'Save Changes' : 'Create Location'}
                </button>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Main Info */}
                <div className="lg:col-span-2 space-y-6">
                    <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Location Details</h3>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="col-span-2">
                                <label className="label">Location Name</label>
                                <input
                                    {...register('name')}
                                    className="input"
                                    placeholder="e.g. Main Office, Downtown Branch"
                                />
                                {errors.name && (
                                    <p className="text-red-500 text-sm mt-1">{errors.name.message}</p>
                                )}
                            </div>

                            <div className="col-span-2">
                                <label className="label">Address Line 1</label>
                                <input
                                    {...register('addressLine1')}
                                    className="input"
                                    placeholder="Address"
                                />
                            </div>

                            <div className="col-span-2">
                                <label className="label">Address Line 2 (Optional)</label>
                                <input
                                    {...register('addressLine2')}
                                    className="input"
                                    placeholder="Suite, Floor, etc."
                                />
                            </div>

                            <div>
                                <label className="label">City</label>
                                <input
                                    {...register('city')}
                                    className="input"
                                    placeholder="City"
                                />
                            </div>

                            <div>
                                <label className="label">State / Province</label>
                                <input
                                    {...register('state')}
                                    className="input"
                                    placeholder="State"
                                />
                            </div>

                            <div>
                                <label className="label">Postal Code</label>
                                <input
                                    {...register('postalCode')}
                                    className="input"
                                    placeholder="ZIP / Postal Code"
                                />
                            </div>

                            <div>
                                <label className="label">Country</label>
                                <input
                                    {...register('country')}
                                    className="input"
                                    placeholder="Country"
                                />
                            </div>
                        </div>
                    </div>

                    <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Contact Information</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div>
                                <label className="label">Phone Number</label>
                                <input
                                    {...register('phone')}
                                    className="input"
                                    placeholder="+1 (555) 000-0000"
                                />
                            </div>

                            <div>
                                <label className="label">Email Address</label>
                                <input
                                    {...register('email')}
                                    className="input"
                                    placeholder="location@business.com"
                                />
                                {errors.email && (
                                    <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Sidebar - Hours & Settings */}
                <div className="space-y-6">
                    <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
                                <Clock className="h-5 w-5 text-gray-400" />
                                Business Hours
                            </h3>
                        </div>

                        <div className="space-y-4">
                            {DEFAULT_HOURS.map((_, index) => (
                                <div key={index} className="flex items-center justify-between group">
                                    <div className="flex items-center gap-3 w-32">
                                        <input
                                            type="checkbox"
                                            checked={businessHours[index].isOpen}
                                            onChange={() => toggleDay(index)}
                                            className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                                        />
                                        <span className={`text-sm ${businessHours[index].isOpen ? 'text-gray-900 font-medium' : 'text-gray-400'}`}>
                                            {DAYS[index]}
                                        </span>
                                    </div>

                                    {businessHours[index].isOpen ? (
                                        <div className="flex items-center gap-2 text-sm">
                                            <input
                                                type="time"
                                                value={businessHours[index].open}
                                                onChange={(e) => updateTime(index, 'open', e.target.value)}
                                                className="border border-gray-200 rounded px-1 py-0.5 w-20 text-center text-gray-900 focus:outline-none focus:border-primary-500"
                                            />
                                            <span className="text-gray-400">-</span>
                                            <input
                                                type="time"
                                                value={businessHours[index].close}
                                                onChange={(e) => updateTime(index, 'close', e.target.value)}
                                                className="border border-gray-200 rounded px-1 py-0.5 w-20 text-center text-gray-900 focus:outline-none focus:border-primary-500"
                                            />
                                        </div>
                                    ) : (
                                        <span className="text-sm text-gray-400 italic px-4">Closed</span>
                                    )}
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
                        <h3 className="text-lg font-semibold text-gray-900 mb-4">Settings</h3>

                        <div className="space-y-4">
                            <div>
                                <label className="label">Timezone</label>
                                <select {...register('timezone')} className="input">
                                    <option value="UTC">UTC</option>
                                    <option value="America/New_York">Eastern Time (US & Canada)</option>
                                    <option value="America/Chicago">Central Time (US & Canada)</option>
                                    <option value="America/Denver">Mountain Time (US & Canada)</option>
                                    <option value="America/Los_Angeles">Pacific Time (US & Canada)</option>
                                    <option value="Europe/London">London</option>
                                    <option value="Asia/Tokyo">Tokyo</option>
                                    {/* Add more timezones as needed */}
                                </select>
                            </div>

                            <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                                <div>
                                    <p className="font-medium text-gray-900">Active Status</p>
                                    <p className="text-xs text-gray-500">Enable this location</p>
                                </div>
                                <label className="relative inline-flex items-center cursor-pointer">
                                    <input
                                        type="checkbox"
                                        {...register('isActive')}
                                        className="sr-only peer"
                                    />
                                    <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    );
}
