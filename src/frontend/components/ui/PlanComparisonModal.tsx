'use client';

import React, { useEffect, useState } from 'react';
import { X, Check, ArrowRight, Loader2 } from 'lucide-react';
import { Button } from './Button';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import { useRouter } from 'next/navigation';
import { useToast } from '@/components/ui/Toast';

interface PlanComparisonModalProps {
    isOpen: boolean;
    onClose: () => void;
    feature?: string;
}

export function PlanComparisonModal({ isOpen, onClose, feature }: PlanComparisonModalProps) {
    const [plans, setPlans] = useState<any[]>([]);
    const [loading, setLoading] = useState(false);
    const [processingPlanId, setProcessingPlanId] = useState<string | null>(null);
    const router = useRouter();
    const { success, error } = useToast();

    useEffect(() => {
        if (!isOpen) return;
        
        const fetchPlans = async () => {
            setLoading(true);
            try {
                // api.billing.getPlans() mapping
                const res = await api.billing.getPlans();
                setPlans(res.data?.data || res.data || []);
            } catch (err) {
                console.error('Failed to fetch plans', err);
            } finally {
                setLoading(false);
            }
        };

        fetchPlans();
    }, [isOpen]);

    const handleUpgrade = async (planId: string) => {
        setProcessingPlanId(planId);
        try {
            const res = await api.billing.createCheckout({
                planId,
                isAnnual: false
            });
            if (res.data?.url) {
                window.location.href = res.data.url;
            } else {
                router.push('/dashboard/billing');
            }
        } catch (err) {
            console.error(err);
            error('Failed to initiate upgrade process.');
            router.push('/dashboard/billing');
        } finally {
            setProcessingPlanId(null);
            onClose();
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-gray-900/60 backdrop-blur-sm" onClick={onClose} />
            <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-5xl max-h-[90vh] overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200">
                
                {/* Header */}
                <div className="p-6 border-b border-gray-100 flex items-start justify-between bg-gradient-to-r from-gray-50 to-white">
                    <div>
                        <h2 className="text-2xl font-bold tracking-tight text-gray-900">
                            Upgrade to Unlock {feature ? `"${feature}"` : 'Premium Features'}
                        </h2>
                        <p className="text-gray-500 mt-1">
                            Choose the perfect plan to scale your operations without limits.
                        </p>
                    </div>
                    <button 
                        onClick={onClose}
                        className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-full transition-colors"
                    >
                        <X className="w-5 h-5" />
                    </button>
                </div>

                {/* Body */}
                <div className="p-6 overflow-y-auto bg-gray-50/50 flex-1">
                    {loading ? (
                        <div className="flex flex-col items-center justify-center py-20">
                            <Loader2 className="h-8 w-8 text-primary-500 animate-spin mb-4" />
                            <p className="text-sm text-gray-500">Loading plans...</p>
                        </div>
                    ) : (
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                            {plans.slice(0, 3).map((plan, index) => {
                                const isPopular = index === 1; // Assuming middle plan is popular
                                return (
                                    <div 
                                        key={plan.id} 
                                        className={cn(
                                            "relative flex flex-col p-6 rounded-2xl bg-white border transition-all duration-200",
                                            isPopular ? "border-primary-500 shadow-xl scale-105 z-10" : "border-gray-200 shadow-sm hover:shadow-md"
                                        )}
                                    >
                                        {isPopular && (
                                            <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-primary-500 text-white px-3 py-1 rounded-full text-xs font-bold tracking-wider uppercase shadow-sm">
                                                Most Popular
                                            </div>
                                        )}
                                        <div className="mb-6">
                                            <h3 className="text-xl font-bold text-gray-900">{plan.name}</h3>
                                            <p className="text-sm text-gray-500 mt-2 h-10">{plan.description}</p>
                                        </div>
                                        <div className="mb-6 flex-1">
                                            <div className="flex items-baseline mb-4">
                                                <span className="text-4xl font-extrabold text-gray-900">${plan.monthlyPrice || plan.price || 0}</span>
                                                <span className="text-gray-500 ml-1">/mo</span>
                                            </div>
                                            <ul className="space-y-3">
                                                {(plan.features || plan.featuresJson ? JSON.parse(plan.featuresJson || '[]') : ['Unlimited Bookings', 'Client CRM', 'Staff Management']).map((f: string, i: number) => (
                                                    <li key={i} className="flex items-start text-sm text-gray-700">
                                                        <Check className="h-4 w-4 text-green-500 mr-2 shrink-0 mt-0.5" />
                                                        <span className={feature && f.toLowerCase().includes(feature.toLowerCase()) ? "font-bold text-primary-700" : ""}>{f}</span>
                                                    </li>
                                                ))}
                                            </ul>
                                        </div>
                                        <Button
                                            variant={isPopular ? 'primary' : 'outline'}
                                            className="w-full mt-auto"
                                            disabled={processingPlanId === plan.id}
                                            onClick={() => handleUpgrade(plan.id)}
                                        >
                                            {processingPlanId === plan.id ? (
                                                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                            ) : (
                                                <ArrowRight className="w-4 h-4 mr-2" />
                                            )}
                                            {processingPlanId === plan.id ? 'Processing...' : 'Upgrade Now'}
                                        </Button>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
