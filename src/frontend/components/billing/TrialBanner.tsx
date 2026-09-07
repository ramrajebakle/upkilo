'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Sparkles, Clock, AlertTriangle, ArrowRight, X, MailCheck } from 'lucide-react';
import { cn } from '@/lib/utils';
import { api } from '@/lib/api';

/**
 * Trial countdown and upgrade prompt.
 *
 * Every piece this depends on already existed and was inert: Tenant.TrialEndsAt,
 * SubscriptionStatus.Trialing, PricingPlan.TrialDays, UpsellTriggerService's "trial_ending"
 * Critical trigger, and GET /billing/upsell-triggers — whose own doc comment says "Frontend reads
 * this on page load and shows toast/banner notifications". Nothing ever did, and nothing ever set
 * TrialEndsAt, so there was no countdown to show in the first place.
 */
type TrialStatus = {
  isTrialing: boolean;
  hasExpired: boolean;
  /** No trial yet because the address is unproven. The trial is what verifying earns. */
  needsVerification?: boolean;
  trialDays?: number;
  trialPlanName?: string;
  planName?: string;
  trialEndsAt?: string | null;
  daysLeft?: number;
  intendedPlan?: string | null;
  upgradeUrl?: string;
};

/** Urgency rises as the trial runs out. The last three days get the loudest treatment. */
function urgencyOf(daysLeft: number): 'calm' | 'warning' | 'critical' {
  if (daysLeft <= 3) return 'critical';
  if (daysLeft <= 7) return 'warning';
  return 'calm';
}

const DISMISS_KEY = 'upkilo.trialBanner.dismissedForDay';

export function TrialBanner() {
  const [status, setStatus] = useState<TrialStatus | null>(null);
  const [dismissed, setDismissed] = useState(false);
  const [resending, setResending] = useState(false);
  const [resent, setResent] = useState(false);

  const resend = async () => {
    setResending(true);
    try {
      await api.auth.resendVerification();
      setResent(true);
    } catch {
      // Deliberately not surfaced as an error: a failed resend is retryable and an alarming
      // message on the dashboard is worse than a button that can be pressed again.
    } finally {
      setResending(false);
    }
  };

  useEffect(() => {
    (async () => {
      try {
        const res = await api.billing.getTrialStatus();
        setStatus(res.data ?? null);
      } catch {
        // Silent. A banner is an enhancement — a billing hiccup or a non-Owner role (this
        // endpoint is Owner-only) must never break the dashboard around it.
        setStatus(null);
      }
    })();
  }, []);

  useEffect(() => {
    // Dismissal lasts for the calendar day, not forever: the point of the banner is that the
    // deadline keeps approaching, and a permanently dismissed countdown stops being one. Wrapped
    // because storage throws outright in some embedded contexts.
    try {
      const stored = localStorage.getItem(DISMISS_KEY);
      if (stored === new Date().toDateString()) setDismissed(true);
    } catch {
      /* no stored preference available; show the banner */
    }
  }, []);

  const dismissForToday = () => {
    setDismissed(true);
    try {
      localStorage.setItem(DISMISS_KEY, new Date().toDateString());
    } catch {
      /* dismissal is per-session only if storage is unavailable */
    }
  };

  if (!status) return null;

  // Unverified: the trial has not started, and starting it is entirely in their hands. Framed as
  // the reward it is rather than as a chore — login is never blocked on this, so a wall here would
  // buy nothing and cost conversions.
  if (status.needsVerification) {
    return (
      <div
        role="status"
        className="mb-6 rounded-2xl border border-primary-200 bg-primary-50 dark:border-primary-500/30 dark:bg-primary-500/10 p-5 flex flex-col sm:flex-row sm:items-center gap-4"
      >
        <div className="w-10 h-10 rounded-xl bg-primary-100 dark:bg-primary-500/20 text-primary-700 dark:text-primary-300 flex items-center justify-center flex-shrink-0">
          <MailCheck className="h-5 w-5" aria-hidden="true" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-sm text-slate-900 dark:text-white">
            Verify your email to start your {status.trialDays ?? 14}-day {status.trialPlanName ?? 'Growth'} trial
          </p>
          <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5 leading-relaxed">
            We sent a link to your inbox. Until then you&apos;re on the Free plan — everything still
            works, you just don&apos;t have the premium features yet.
          </p>
          {resent && (
            <p className="text-xs font-medium text-emerald-700 dark:text-emerald-400 mt-1.5">
              Sent — check your inbox.
            </p>
          )}
        </div>
        <button
          onClick={resend}
          disabled={resending || resent}
          className="inline-flex items-center justify-center gap-2 px-4 py-2 rounded-xl bg-primary-600 text-white text-sm font-semibold hover:bg-primary-700 disabled:opacity-60 transition-colors flex-shrink-0"
        >
          {resending ? 'Sending…' : resent ? 'Email sent' : 'Resend email'}
        </button>
      </div>
    );
  }

  if (!status.isTrialing && !status.hasExpired) return null;

  const upgradeHref = status.intendedPlan
    ? `/settings/billing?upgrade=${encodeURIComponent(status.intendedPlan.toLowerCase())}`
    : (status.upgradeUrl ?? '/settings/billing?upgrade=true');

  // Expired state is never dismissible — it is a description of the account's current condition,
  // not a nag about a future one.
  if (status.hasExpired) {
    return (
      <div
        role="status"
        className="mb-6 rounded-2xl border border-slate-300 bg-slate-50 dark:border-white/10 dark:bg-white/5 p-5 flex flex-col sm:flex-row sm:items-center gap-4"
      >
        <div className="w-10 h-10 rounded-xl bg-slate-200 dark:bg-white/10 flex items-center justify-center flex-shrink-0">
          <Clock className="h-5 w-5 text-slate-600 dark:text-slate-300" aria-hidden="true" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-sm text-slate-900 dark:text-white">
            Your trial has ended — you&apos;re on the Free plan
          </p>
          <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5 leading-relaxed">
            Nothing was deleted and your booking page is still live. Upgrade to switch your premium
            features back on.
          </p>
        </div>
        <Link
          href={upgradeHref}
          className="inline-flex items-center justify-center gap-2 px-4 py-2 rounded-xl bg-slate-900 dark:bg-white text-white dark:text-slate-900 text-sm font-semibold hover:opacity-90 transition-opacity flex-shrink-0"
        >
          See plans
          <ArrowRight size={15} aria-hidden="true" />
        </Link>
      </div>
    );
  }

  if (dismissed) return null;

  const daysLeft = status.daysLeft ?? 0;
  const urgency = urgencyOf(daysLeft);
  const planName = status.planName ?? 'trial';

  const styles = {
    calm: {
      wrap: 'border-primary-200 bg-primary-50 dark:border-primary-500/30 dark:bg-primary-500/10',
      icon: 'bg-primary-100 dark:bg-primary-500/20 text-primary-700 dark:text-primary-300',
      cta: 'bg-primary-600 text-white hover:bg-primary-700',
    },
    warning: {
      wrap: 'border-amber-300 bg-amber-50 dark:border-amber-500/30 dark:bg-amber-500/10',
      icon: 'bg-amber-100 dark:bg-amber-500/20 text-amber-700 dark:text-amber-300',
      cta: 'bg-amber-600 text-white hover:bg-amber-700',
    },
    critical: {
      wrap: 'border-red-300 bg-red-50 dark:border-red-500/30 dark:bg-red-500/10',
      icon: 'bg-red-100 dark:bg-red-500/20 text-red-700 dark:text-red-300',
      cta: 'bg-red-600 text-white hover:bg-red-700',
    },
  }[urgency];

  const Icon = urgency === 'critical' ? AlertTriangle : Sparkles;

  const headline =
    daysLeft <= 0
      ? `Your ${planName} trial ends today`
      : daysLeft === 1
      ? `1 day left of your ${planName} trial`
      : `${daysLeft} days left of your ${planName} trial`;

  return (
    <div role="status" className={cn('mb-6 rounded-2xl border p-5 flex flex-col sm:flex-row sm:items-center gap-4', styles.wrap)}>
      <div className={cn('w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0', styles.icon)}>
        <Icon className="h-5 w-5" aria-hidden="true" />
      </div>

      <div className="flex-1 min-w-0">
        <p className="font-semibold text-sm text-slate-900 dark:text-white">{headline}</p>
        <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5 leading-relaxed">
          {/* States the actual consequence rather than implying data loss — the account moves to
              Free, it is not deleted, and claiming otherwise is a lie the product has to live down
              the first time somebody checks. */}
          {urgency === 'critical'
            ? 'After that your account moves to the Free plan: 1 staff member, 100 clients, and premium features switch off. Your data and bookings stay.'
            : `You have every ${planName} feature switched on. Choose a plan to keep them.`}
        </p>
      </div>

      <div className="flex items-center gap-2 flex-shrink-0">
        <Link
          href={upgradeHref}
          className={cn(
            'inline-flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold transition-colors',
            styles.cta
          )}
        >
          {status.intendedPlan ? `Continue with ${status.intendedPlan}` : 'Choose your plan'}
          <ArrowRight size={15} aria-hidden="true" />
        </Link>
        <button
          onClick={dismissForToday}
          aria-label="Hide this reminder until tomorrow"
          title="Hide until tomorrow"
          className="p-2 rounded-lg text-slate-500 dark:text-slate-400 hover:bg-black/5 dark:hover:bg-white/10 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}
