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
    UserPlus,
    Sparkles
} from 'lucide-react';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';
import { useTranslations } from 'next-intl';

const iconMap: Record<string, any> = {
    business_profile: Building2,
    working_hours: Calendar,
    add_services: Briefcase,
    add_staff: Users,
    booking_page: Palette,
    payment_setup: CreditCard,
    first_booking: PlusCircle,
    invite_client: UserPlus,
    // Was missing, so the AI step fell through to the generic CheckCircle2 default and
    // looked identical to a completed tick.
    ai_copilot_quickwin: Sparkles,
};

/** One row of GET /onboarding/checklist. Mirrors OnboardingController's step shape. */
type OnboardingStep = {
    id: string;
    title: string;
    description: string;
    order: number;
    completed: boolean;
    completedAt: string | null;
    route: string;
};

export function OnboardingWizard() {
    // Step titles and descriptions arrive from OnboardingController as hardcoded English.
    // The app routes 15 locales, so 14 of them rendered this panel untranslated. The API
    // stays the source of truth for WHICH steps exist (id, order, route, completion) while
    // the display text is looked up here by id, falling back to whatever the API sent when
    // a key is absent — so an id added server-side still renders instead of breaking.
    const t = useTranslations('Onboarding');
    const stepText = (id: string, field: 'title' | 'description', fallback: string) => {
        const key = `steps.${id}.${field}`;
        return t.has(key) ? t(key) : fallback;
    };

    const [status, setStatus] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [isExpanded, setIsExpanded] = useState(true);
    const [isDismissed, setIsDismissed] = useState(false);
    const [confirmingDismiss, setConfirmingDismiss] = useState(false);

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

    const steps: OnboardingStep[] = status.steps ?? [];
    const completedCount = steps.filter((s) => s.completed).length;
    // The one card that gets visual priority. Nine equally-weighted tiles gave the user no
    // idea where to begin; exactly one "Start here" does.
    const nextStepIndex = steps.findIndex((s) => !s.completed);

    return (
        <div className="mb-8 animate-fade-in">
            <div className={cn(
                "card-elevated border border-slate-200 dark:border-white/5 bg-white dark:bg-gradient-to-br dark:from-slate-900 dark:to-slate-950 transition-all duration-500",
                // Clamp ONLY while collapsed. Previously max-h-[800px] applied even when
                // expanded, and combined with overflow-hidden it silently cut off the last
                // rows — on mobile (single column, nine cards) several steps were simply
                // unreachable.
                isExpanded ? "rounded-2xl" : "max-h-[100px] overflow-hidden rounded-2xl"
            )}>
                {/* Header */}
                <div className="p-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-100 dark:border-white/5">
                    <div className="flex items-center gap-4">
                        <div className="w-12 h-12 shrink-0 rounded-2xl bg-primary-50 dark:bg-primary-500/20 flex items-center justify-center border border-primary-200 dark:border-primary-500/30">
                            <Rocket className="h-6 w-6 text-primary-600 dark:text-primary-400" aria-hidden="true" />
                        </div>
                        <div>
                            {/* Was gradient-clipped in dark mode only, fading white to
                                slate-400 — so the greeting was legible in light mode and
                                progressively less so in dark. Solid in both. */}
                            <h2 className="text-xl font-bold text-slate-900 dark:text-white">
                                {t('title')}
                            </h2>
                            <p className="text-slate-600 dark:text-slate-400 text-sm">{t('subtitle')}</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-4 sm:justify-end">
                        {/* Progress used to be hidden below md, so phone users saw no progress
                            at all — the most motivating element in a checklist. It is also
                            labelled by COUNT, because "0% Complete" over an empty bar reads as
                            a broken component, whereas "0 of 9 done" reads as a starting line. */}
                        <div className="flex-1 sm:flex-none sm:text-right">
                            <p className="text-sm font-medium text-slate-900 dark:text-white">
                                {t('progress', { completed: completedCount, total: steps.length })}
                                <span className="text-slate-500 dark:text-slate-400 font-normal"> · {status.completionPercentage}%</span>
                            </p>
                            <div
                                className="w-full sm:w-32 h-1.5 bg-slate-200 dark:bg-white/10 rounded-full mt-1 overflow-hidden"
                                role="progressbar"
                                aria-valuenow={status.completionPercentage}
                                aria-valuemin={0}
                                aria-valuemax={100}
                                aria-label={t('progressLabel')}
                            >
                                <motion.div
                                    initial={{ width: 0 }}
                                    animate={{ width: `${status.completionPercentage}%` }}
                                    className="h-full bg-primary-500 shadow-[0_0_10px_rgba(6,182,212,0.5)]"
                                />
                            </div>
                        </div>

                        {/* Two-stage. A single click used to POST the dismissal immediately and
                            permanently remove the guided setup, with no visible way back. */}
                        {confirmingDismiss ? (
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={handleDismiss}
                                    className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-900 text-white dark:bg-white dark:text-slate-900 hover:opacity-90 transition-opacity"
                                >
                                    {t('hide')}
                                </button>
                                <button
                                    onClick={() => setConfirmingDismiss(false)}
                                    className="px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/10 transition-colors"
                                >
                                    {t('keep')}
                                </button>
                            </div>
                        ) : (
                            <button
                                onClick={() => setConfirmingDismiss(true)}
                                className="p-2 shrink-0 rounded-lg hover:bg-slate-100 dark:hover:bg-white/10 text-slate-500 dark:text-slate-400 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
                                aria-label={t('hideAria')}
                                title={t('hideAria')}
                            >
                                <X className="h-5 w-5" aria-hidden="true" />
                            </button>
                        )}
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
                            {/* Three columns, not four: nine steps over four columns left a
                                single orphaned card on the last row, which reads as a layout
                                bug. 3x3 is exact, and the wider card fits the descriptions. */}
                            <ul className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 list-none p-0 m-0">
                                {steps.map((step, index) => {
                                    const Icon = iconMap[step.id] || CheckCircle2;
                                    const isNext = index === nextStepIndex;
                                    return (
                                        <li key={step.id} className="flex">
                                            <Link
                                                href={step.route}
                                                aria-label={t(
                                                    step.completed ? 'stepAriaCompleted' : 'stepAria',
                                                    { current: index + 1, total: steps.length, title: stepText(step.id, 'title', step.title) }
                                                )}
                                                className={cn(
                                                    "group relative flex w-full flex-col p-4 rounded-2xl border transition-all duration-300",
                                                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500",
                                                    step.completed
                                                        ? "bg-emerald-500/5 border-emerald-500/20"
                                                        : isNext
                                                            ? "bg-primary-50 dark:bg-primary-500/10 border-primary-300 dark:border-primary-500/40 hover:border-primary-500"
                                                            : "bg-slate-50 dark:bg-white/5 border-slate-200 dark:border-white/10 hover:border-primary-500/50 dark:hover:bg-white/[0.08]"
                                                )}
                                            >
                                                <div className="flex items-start justify-between mb-3">
                                                    <div className={cn(
                                                        "w-10 h-10 rounded-xl flex items-center justify-center transition-colors",
                                                        step.completed
                                                            ? "bg-emerald-500/20 text-emerald-600 dark:text-emerald-400"
                                                            : "bg-white dark:bg-slate-800 text-slate-500 dark:text-slate-400 group-hover:bg-primary-500/20 group-hover:text-primary-600 dark:group-hover:text-primary-400"
                                                    )}>
                                                        <Icon className="h-5 w-5" aria-hidden="true" />
                                                    </div>

                                                    <div className="flex items-center gap-2">
                                                        {/* Order is returned by the API and was never surfaced,
                                                            leaving nine tiles with no sense of sequence. */}
                                                        <span className="text-[11px] font-semibold tabular-nums text-slate-500 dark:text-slate-400">
                                                            {index + 1}/{steps.length}
                                                        </span>
                                                        {step.completed ? (
                                                            <CheckCircle2 className="h-5 w-5 text-emerald-600 dark:text-emerald-400" aria-hidden="true" />
                                                        ) : (
                                                            // Was slate-700 on a near-black card — roughly 2:1, effectively
                                                            // invisible. Decorative only; state is in the aria-label.
                                                            <Circle className="h-5 w-5 text-slate-400 dark:text-slate-500 group-hover:text-primary-500" aria-hidden="true" />
                                                        )}
                                                    </div>
                                                </div>

                                                {isNext && (
                                                    <span className="mb-2 inline-flex w-fit items-center rounded-full bg-primary-500/15 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-primary-700 dark:text-primary-300">
                                                        {t('startHere')}
                                                    </span>
                                                )}

                                                <h3 className={cn(
                                                    "font-semibold text-sm mb-1 transition-colors",
                                                    step.completed
                                                        ? "text-emerald-700 dark:text-emerald-400"
                                                        : "text-slate-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400"
                                                )}>
                                                    {stepText(step.id, 'title', step.title)}
                                                </h3>
                                                {/* slate-500 at 12px sat under the 4.5:1 AA threshold on both
                                                    themes. Three lines so the longest description is not cut
                                                    mid-sentence; the grid stretches cards to match. */}
                                                <p className="text-xs text-slate-600 dark:text-slate-400 line-clamp-3 leading-relaxed">
                                                    {stepText(step.id, 'description', step.description)}
                                                </p>

                                                {!step.completed && (
                                                    // Was opacity-0 until group-hover, so on touch devices — where
                                                    // there is no hover — the only call to action never appeared at
                                                    // all, and keyboard users never saw it either. Now always
                                                    // present, and it brightens on hover or focus.
                                                    <div className="mt-3 flex items-center text-[10px] font-bold uppercase tracking-wider text-primary-600/80 dark:text-primary-400/80 group-hover:text-primary-600 dark:group-hover:text-primary-300 group-focus-visible:text-primary-600 transition-colors">
                                                        {t('completeStep')} <ChevronRight className="h-3 w-3 ml-1" aria-hidden="true" />
                                                    </div>
                                                )}
                                            </Link>
                                        </li>
                                    );
                                })}
                            </ul>
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}
