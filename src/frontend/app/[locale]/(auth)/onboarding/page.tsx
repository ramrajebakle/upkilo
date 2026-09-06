'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from '@/navigation';
import {
  CheckCircle2, Circle, ChevronRight, Sparkles,
  Building2, Calendar, Briefcase, Users, Palette,
  CreditCard, PlusCircle, UserPlus, ArrowRight, RefreshCw, AlertTriangle,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { api } from '@/lib/api';
import Link from 'next/link';
import { useTranslations } from 'next-intl';

/**
 * One row of GET /onboarding/checklist, matching OnboardingController's step shape.
 *
 * This page used to read `res.data.items` with an `item.key` — the API returns `steps` with an
 * `id`. Nothing threw, so the catch-fallback never fired: the map simply ran over an empty array
 * and the page rendered a progress bar over nothing. Every registration is routed straight here,
 * so the first screen after signup was an empty checklist.
 */
type OnboardingStep = {
  id: string;
  title: string;
  description: string;
  order: number;
  completed: boolean;
  completedAt: string | null;
  route: string;
};

/**
 * Icons only. This page previously kept a local STEP_META map holding each step's label,
 * description AND href, duplicating what the API already sends — and the two drifted, as
 * duplicated data does: it pointed working_hours at /settings/hours and booking_page at
 * /settings/branding while the API named two different routes, and it had no entry at all for
 * the ninth step, ai_copilot_quickwin. The API is now the single source of which steps exist and
 * where they go; display text comes from the shared Onboarding.steps.* messages, which already
 * cover all nine steps in every locale. This page rendered hardcoded English across 15 locales.
 */
const ICONS: Record<string, React.ElementType> = {
  business_profile: Building2,
  working_hours: Calendar,
  add_services: Briefcase,
  add_staff: Users,
  booking_page: Palette,
  payment_setup: CreditCard,
  first_booking: PlusCircle,
  invite_client: UserPlus,
  ai_copilot_quickwin: Sparkles,
};

export default function OnboardingPage() {
  const router = useRouter();
  const t = useTranslations('Onboarding');
  const [steps, setSteps] = useState<OnboardingStep[]>([]);
  const [completionPercentage, setCompletionPercentage] = useState(0);
  const [loading, setLoading] = useState(true);
  const [failed, setFailed] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setFailed(false);
    try {
      const res = await api.onboarding.getChecklist();
      const items: OnboardingStep[] = res.data?.steps ?? [];
      setSteps(items);
      setCompletionPercentage(
        res.data?.completionPercentage ??
          (items.length ? Math.round((items.filter((s) => s.completed).length / items.length) * 100) : 0)
      );
    } catch {
      // An explicit failure state, not a silent stand-in. The previous fallback rendered a
      // plausible-looking static checklist from local data, which is how a page showing nothing
      // real went unnoticed.
      setFailed(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  // In an effect, not in the render body. Calling router.replace during render updates the router
  // while React is rendering this component, which React warns about and which can fire twice.
  useEffect(() => {
    if (!loading && !failed && steps.length > 0 && completionPercentage === 100) {
      router.replace('/dashboard');
    }
  }, [loading, failed, steps.length, completionPercentage, router]);

  const stepText = (id: string, field: 'title' | 'description', fallback: string) => {
    const key = `steps.${id}.${field}`;
    return t.has(key) ? t(key) : fallback;
  };

  const completedCount = steps.filter((s) => s.completed).length;
  const total = steps.length;
  // The first incomplete step is the one to point at. This replaces a hard lock that disabled
  // every step until its predecessor was done — which turned any single undetectable step into a
  // dead end for the whole list, and did exactly that when business_profile could not complete.
  // The dashboard wizard has never locked steps; now neither does this.
  const nextStepIndex = steps.findIndex((s) => !s.completed);

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 via-white to-primary-100 flex flex-col">
      {/* Header */}
      <header className="px-6 py-5 flex items-center justify-between border-b border-border bg-background/70 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary-600 to-primary-700 flex items-center justify-center shadow-lg shadow-primary-500/30">
            <span className="text-white font-bold text-lg leading-none">U</span>
          </div>
          <span className="font-semibold text-foreground text-lg">Upkilo</span>
        </div>
        <Link href="/dashboard" className="text-sm text-foreground-secondary hover:text-foreground transition-colors">
          {t('skipForNow')}
        </Link>
      </header>

      <main className="flex-1 flex flex-col items-center justify-start px-4 py-12 max-w-2xl mx-auto w-full">
        {/* Hero text */}
        <div className="text-center mb-10">
          <div className="inline-flex items-center gap-2 bg-brand-subtle text-primary text-sm font-semibold px-4 py-1.5 rounded-full mb-6">
            <Sparkles size={14} aria-hidden="true" />
            {t('title')}
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-foreground leading-tight mb-3">
            {t('pageHeading')}
          </h1>
          <p className="text-foreground-secondary text-lg">{t('subtitle')}</p>
        </div>

        {failed && (
          <div
            role="alert"
            className="w-full mb-8 rounded-2xl border border-amber-300 bg-amber-50 p-5 flex items-start gap-4"
          >
            <AlertTriangle className="h-5 w-5 text-amber-600 flex-shrink-0 mt-0.5" aria-hidden="true" />
            <div className="flex-1">
              <p className="font-semibold text-sm text-amber-900">{t('loadFailedTitle')}</p>
              <p className="text-xs text-amber-800 mt-0.5">{t('loadFailedBody')}</p>
            </div>
            <button
              onClick={load}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-amber-900 text-white hover:bg-amber-800 transition-colors"
            >
              <RefreshCw size={12} aria-hidden="true" />
              {t('retry')}
            </button>
          </div>
        )}

        {/* Progress bar */}
        {!loading && !failed && total > 0 && (
          <div className="w-full mb-8">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-foreground">
                {t('progress', { completed: completedCount, total })}
              </span>
              <span className="text-sm font-bold text-primary">{completionPercentage}%</span>
            </div>
            <div
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={completionPercentage}
              aria-label={t('progressLabel')}
              className="h-2 bg-muted rounded-full overflow-hidden"
            >
              <div
                className="h-full bg-gradient-to-r from-primary-500 to-primary-600 rounded-full transition-all duration-500"
                style={{ width: `${completionPercentage}%` }}
              />
            </div>
          </div>
        )}

        {/* Steps list */}
        <div className="w-full space-y-3">
          {loading
            ? Array.from({ length: 9 }).map((_, i) => (
                <div key={i} className="h-20 rounded-2xl bg-muted animate-pulse" />
              ))
            : steps.map((step, idx) => {
                const Icon = ICONS[step.id] ?? Circle;
                const isNext = idx === nextStepIndex;
                const title = stepText(step.id, 'title', step.title);
                return (
                  <Link
                    key={step.id}
                    href={step.route}
                    aria-label={t(step.completed ? 'stepAriaCompleted' : 'stepAria', {
                      current: idx + 1,
                      total,
                      title,
                    })}
                    className={cn(
                      'flex items-center gap-4 p-5 rounded-2xl border transition-all duration-200 group',
                      step.completed
                        ? 'bg-card border-green-200'
                        : isNext
                        ? 'bg-card border-primary-300 shadow-md shadow-primary-500/10'
                        : 'bg-card border-border hover:border-primary-300 hover:shadow-md hover:shadow-primary-500/10'
                    )}
                  >
                    {/* Step icon / check */}
                    <div
                      className={cn(
                        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors',
                        step.completed
                          ? 'bg-green-100 text-green-600'
                          : 'bg-brand-subtle text-primary group-hover:bg-primary-200'
                      )}
                    >
                      {step.completed ? <CheckCircle2 size={20} aria-hidden="true" /> : <Icon size={20} aria-hidden="true" />}
                    </div>

                    {/* Label + description */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p
                          className={cn(
                            'font-semibold text-sm leading-tight',
                            step.completed ? 'text-foreground-muted line-through' : 'text-foreground'
                          )}
                        >
                          {title}
                        </p>
                        {isNext && (
                          <span className="inline-flex w-fit items-center rounded-full bg-primary-500/15 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-primary-700">
                            {t('startHere')}
                          </span>
                        )}
                      </div>
                      {!step.completed && (
                        <p className="text-xs mt-0.5 leading-relaxed truncate text-foreground-secondary">
                          {stepText(step.id, 'description', step.description)}
                        </p>
                      )}
                    </div>

                    {!step.completed && (
                      <ChevronRight
                        size={18}
                        className="text-foreground-muted flex-shrink-0 group-hover:text-primary transition-colors"
                        aria-hidden="true"
                      />
                    )}
                  </Link>
                );
              })}
        </div>

        {/* Go to dashboard CTA */}
        <div className="mt-10 text-center">
          <Link
            href="/dashboard"
            className="inline-flex items-center gap-2 px-6 py-3 bg-gray-900 text-white font-semibold rounded-xl hover:bg-gray-800 transition-colors text-sm"
          >
            {t('goToDashboard')}
            <ArrowRight size={16} aria-hidden="true" />
          </Link>
          <p className="text-xs text-foreground-muted mt-3">{t('completeAnyTime')}</p>
        </div>
      </main>
    </div>
  );
}
