'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import {
    ArrowLeft,
    Crown,
    Gift,
    Star,
    Sparkles,
    Save,
    Tag,
    Info,
    CheckCircle2,
    PlusCircle,
    BadgeDollarSign,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const loyaltyRewardSchema = z.object({
    name: z.string().min(2, 'Reward name must be at least 2 characters'),
    description: z.string().optional(),
    pointsRequired: z.number().min(1, 'Points required must be at least 1'),
    rewardType: z.string().min(1, 'Please select a reward type'),
    rewardValue: z.number().min(0, 'Value cannot be negative'),
    isActive: z.boolean(),
    expiryDays: z.number().min(1, 'Expiry days must be at least 1'),
});

type LoyaltyRewardFormData = z.infer<typeof loyaltyRewardSchema>;

export default function NewLoyaltyRewardPage() {
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
        watch,
    } = useForm<LoyaltyRewardFormData>({
        resolver: zodResolver(loyaltyRewardSchema),
        defaultValues: {
            name: '',
            description: '',
            pointsRequired: 100,
            rewardType: 'discount',
            rewardValue: 0,
            isActive: true,
            expiryDays: 30,
        },
    });

    const onSubmit = async (data: LoyaltyRewardFormData) => {
        setLoading(true);
        try {
            await api.loyalty.createReward(data);
            toastSuccess('Loyalty reward created successfully');
            router.push('/loyalty?created=true');
        } catch (error) {
            console.error('Failed to create loyalty reward', error);
            toastError('Failed to create reward');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/loyalty"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-amber-500 to-orange-600 rounded-xl shadow-lg shadow-amber-500/25">
                            <Gift className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Create New Reward
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Define a new reward for your loyalty program</p>
                </div>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Main Info */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Tag className="h-5 w-5 text-primary-500" />
                                Reward Details
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Reward Name <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        {...register('name')}
                                        type="text"
                                        className={cn("input", errors.name && "border-red-500")}
                                        placeholder="e.g. $10 Off Any Service"
                                    />
                                    {errors.name && <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Description
                                    </label>
                                    <textarea
                                        {...register('description')}
                                        className="input min-h-[100px] py-3"
                                        placeholder="Explain what this reward grants the customer..."
                                    />
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Reward Type <span className="text-red-500">*</span>
                                        </label>
                                        <select
                                            {...register('rewardType')}
                                            className={cn("input", errors.rewardType && "border-red-500")}
                                        >
                                            <option value="discount">Fix Discount ($)</option>
                                            <option value="percentage">Percentage Off (%)</option>
                                            <option value="service">Free Service</option>
                                            <option value="product">Free Product</option>
                                        </select>
                                        {errors.rewardType && <p className="text-xs text-red-500 mt-1">{errors.rewardType.message}</p>}
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 mb-2">
                                            Value
                                        </label>
                                        <div className="relative">
                                            <BadgeDollarSign className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                            <input
                                                {...register('rewardValue', { valueAsNumber: true })}
                                                type="number"
                                                className={cn("input pl-11", errors.rewardValue && "border-red-500")}
                                                placeholder="0.00"
                                            />
                                        </div>
                                        {errors.rewardValue && <p className="text-xs text-red-500 mt-1">{errors.rewardValue.message}</p>}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <CheckCircle2 className="h-5 w-5 text-emerald-500" />
                                Requirements
                            </h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Points Required <span className="text-red-500">*</span>
                                    </label>
                                    <div className="relative">
                                        <Star className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-amber-500" />
                                        <input
                                            {...register('pointsRequired', { valueAsNumber: true })}
                                            type="number"
                                            className={cn("input pl-11 font-bold text-slate-900", errors.pointsRequired && "border-red-500")}
                                        />
                                    </div>
                                    {errors.pointsRequired && <p className="text-xs text-red-500 mt-1">{errors.pointsRequired.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">
                                        Expiry (Days) <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        {...register('expiryDays', { valueAsNumber: true })}
                                        type="number"
                                        className={cn("input", errors.expiryDays && "border-red-500")}
                                    />
                                    {errors.expiryDays && <p className="text-xs text-red-500 mt-1">{errors.expiryDays.message}</p>}
                                    <p className="text-xs text-slate-400 mt-2">
                                        Voucher will expire this many days after redemption.
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Sidebar Info */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up" style={{ animationDelay: '300ms' }}>
                            <h2 className="text-lg font-semibold text-slate-900 mb-6 flex items-center gap-2">
                                <Info className="h-5 w-5 text-blue-500" />
                                Options
                            </h2>
                            <div className="space-y-4">
                                <label className="flex items-center gap-3 p-3 rounded-xl border border-slate-100 hover:bg-slate-50 transition-colors cursor-pointer">
                                    <input
                                        {...register('isActive')}
                                        type="checkbox"
                                        className="w-5 h-5 rounded border-slate-300 text-primary-500 focus:ring-primary-500"
                                    />
                                    <div>
                                        <span className="block font-medium text-slate-900">Active</span>
                                        <span className="block text-xs text-slate-500">Available for redemption</span>
                                    </div>
                                </label>
                            </div>
                        </div>

                        <div className="p-6 bg-gradient-to-br from-primary-900 to-slate-900 rounded-2xl text-white shadow-xl animate-fade-in-up" style={{ animationDelay: '400ms' }}>
                            <div className="flex items-center gap-3 mb-4">
                                <div className="p-2 bg-white/10 rounded-lg">
                                    <Crown className="h-5 w-5 text-amber-400" />
                                </div>
                                <h3 className="font-bold text-lg">VIP Experience</h3>
                            </div>
                            <p className="text-primary-200/70 text-sm leading-relaxed mb-4">
                                Create tiered rewards to encourage clients to reach higher loyalty statuses. Premium rewards drive long-term retention.
                            </p>
                            <div className="flex items-center gap-2 text-xs font-medium text-amber-400">
                                <Sparkles className="h-3 w-3" />
                                Pro Tip: Most popular is 10% off.
                            </div>
                        </div>

                        <div className="flex flex-col gap-3 pt-2">
                            <button
                                type="submit"
                                disabled={loading}
                                className="w-full btn btn-primary py-4 shadow-xl shadow-primary-500/25"
                            >
                                {loading ? (
                                    <>
                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                        Creating Reward...
                                    </>
                                ) : (
                                    <>
                                        <Save className="h-5 w-5" />
                                        Save Reward
                                    </>
                                )}
                            </button>
                            <Link
                                href="/loyalty"
                                className="w-full btn btn-secondary text-center py-4"
                            >
                                Cancel
                            </Link>
                        </div>
                    </div>
                </div>
            </form>
        </div>
    );
}
