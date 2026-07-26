'use client';

import { useState, useEffect } from 'react';
import { 
    CheckCircle2, 
    Circle, 
    ChevronRight, 
    Rocket, 
    X,
    Building2,
    Calendar,
    Briefcase,
    Users,
    Palette,
    CreditCard,
    PlusCircle,
    UserPlus
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';

const iconMap: Record<string, any> = {
    business_profile: Building2,
    working_hours: Calendar,
    add_services: Briefcase,
    add_staff: Users,
    booking_page: Palette,
    payment_setup: CreditCard,
    first_booking: PlusCircle,
    invite_client: UserPlus,
};

export function OnboardingWizard() {
    const [status, setStatus] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [isExpanded, setIsExpanded] = useState(true);
    const [isDismissed, setIsDismissed] = useState(false);

    useEffect(() => {
        fetchStatus();
    }, []);

    const fetchStatus = async () => {
        try {
            const res = await api.onboarding.getChecklist();
            setStatus(res.data);
            if (res.data.isDismissed) {
                setIsDismissed(true);
            }
        } catch (err) {
            console.error('Failed to fetch onboarding status', err);
        } finally {
            setLoading(false);
        }
    };

    const handleDismiss = async () => {
        // Track drop-off: which step % user abandoned at
        if (typeof window !== 'undefined' && (window as any).gtag) {
            (window as any).gtag('event', 'onboarding_dismissed', {
                completion_pct: status?.completionPercentage ?? 0,
                completed_steps: status?.steps?.filter((s: any) => s.completed).length ?? 0,
            });
        }
        try {
            await api.onboarding.dismiss();
            setIsDismissed(true);
        } catch (err) {
            console.error('Failed to dismiss onboarding', err);
        }
    };

    if (loading || isDismissed || !status || status.completionPercentage === 100) {
        return null;
    }

    return (
        <div className="mb-8 animate-fade-in">
            <div className={cn(
                "card-elevated overflow-hidden border border-slate-200 dark:border-white/5 bg-white dark:bg-gradient-to-br dark:from-slate-900 dark:to-slate-950 transition-all duration-500",
                isExpanded ? "max-h-[800px]" : "max-h-[100px]"
            )}>
                {/* Header */}
                <div className="p-6 flex items-center justify-between border-b border-slate-100 dark:border-white/5">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-2xl bg-primary-50 dark:bg-primary-500/20 flex items-center justify-center border border-primary-200 dark:border-primary-500/30">
                            <Rocket className="h-6 w-6 text-primary-600 dark:text-primary-400" />
                        </div>
                        <div>
                            <h2 className="text-xl font-bold text-slate-900 dark:bg-gradient-to-r dark:from-white dark:to-slate-400 dark:bg-clip-text dark:text-transparent">
                                Welcome to Upkilo!
                            </h2>
                            <p className="text-slate-500 dark:text-slate-400 text-sm">Let's get your business ready for bookings</p>
                        </div>
                    </div>
                    
                    <div className="flex items-center gap-4">
                        <div className="hidden md:block text-right">
                            <p className="text-sm font-medium text-slate-900 dark:text-white">{status.completionPercentage}% Complete</p>
                            <div className="w-32 h-1.5 bg-slate-100 dark:bg-white/10 rounded-full mt-1 overflow-hidden">
                                <motion.div 
                                    initial={{ width: 0 }}
                                    animate={{ width: `${status.completionPercentage}%` }}
                                    className="h-full bg-primary-500 shadow-[0_0_10px_rgba(6,182,212,0.5)]"
                                />
                            </div>
                        </div>
                        <button 
                            onClick={handleDismiss}
                            className="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-white/10 text-slate-400 transition-colors"
                            title="Dismiss for now"
                        >
                            <X className="h-5 w-5" />
                        </button>
                    </div>
                </div>

                <AnimatePresence>
                    {isExpanded && (
                        <motion.div 
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            exit={{ opacity: 0 }}
                            className="p-6"
                        >
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                                {status.steps.map((step: any, index: number) => {
                                    const Icon = iconMap[step.id] || CheckCircle2;
                                    return (
                                        <Link 
                                            key={step.id} 
                                            href={step.route}
                                            className={cn(
                                                "group relative p-4 rounded-2xl border transition-all duration-300",
                                                step.completed 
                                                    ? "bg-emerald-500/5 border-emerald-500/20" 
                                                    : "bg-slate-50 dark:bg-white/5 border-slate-100 dark:border-white/10 hover:border-primary-500/50 dark:hover:bg-white/[0.08]"
                                            )}
                                        >
                                            <div className="flex items-start justify-between mb-3">
                                                <div className={cn(
                                                    "w-10 h-10 rounded-xl flex items-center justify-center transition-colors",
                                                    step.completed 
                                                        ? "bg-emerald-500/20 text-emerald-600 dark:text-emerald-400" 
                                                        : "bg-white dark:bg-slate-800 text-slate-400 group-hover:bg-primary-500/20 group-hover:text-primary-600 dark:group-hover:text-primary-400"
                                                )}>
                                                    <Icon className="h-5 w-5" />
                                                </div>
                                                {step.completed ? (
                                                    <CheckCircle2 className="h-5 w-5 text-emerald-400" />
                                                ) : (
                                                    <Circle className="h-5 w-5 text-slate-700 group-hover:text-primary-500/50" />
                                                )}
                                            </div>
                                            
                                            <h3 className={cn(
                                                "font-semibold text-sm mb-1 transition-colors",
                                                step.completed 
                                                    ? "text-emerald-600 dark:text-emerald-400" 
                                                    : "text-slate-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400"
                                            )}>
                                                {step.title}
                                            </h3>
                                            <p className="text-xs text-slate-500 line-clamp-2 leading-relaxed">
                                                {step.description}
                                            </p>
                                            
                                            {!step.completed && (
                                                <div className="mt-3 flex items-center text-[10px] font-bold uppercase tracking-wider text-primary-400 opacity-0 group-hover:opacity-100 transition-opacity">
                                                    Complete Step <ChevronRight className="h-3 w-3 ml-1" />
                                                </div>
                                            )}
                                        </Link>
                                    );
                                })}
                            </div>
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}
