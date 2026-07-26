'use client';

import { useState, useEffect } from 'react';
import { useRouter } from '@/navigation';
import {
  CheckCircle2, Circle, ChevronRight, Sparkles,
  Building2, Calendar, Briefcase, Users, Palette,
  CreditCard, PlusCircle, UserPlus, ArrowRight, Lock,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { api } from '@/lib/api';
import Link from 'next/link';

interface Step {
  key: string;
  label: string;
  description: string;
  icon: React.ElementType;
  href: string;
  completed: boolean;
}

const STEP_META: Record<string, { label: string; description: string; icon: React.ElementType; href: string }> = {
  business_profile: {
    label: 'Set up your business profile',
    description: 'Add your business name, logo, address, and contact details.',
    icon: Building2,
    href: '/settings/business',
  },
  working_hours: {
    label: 'Configure working hours',
    description: "Tell clients when you're open for bookings.",
    icon: Calendar,
    href: '/settings/hours',
  },
  add_services: {
    label: 'Add your first service',
    description: 'Create the services or treatments you offer.',
    icon: Briefcase,
    href: '/services',
  },
  add_staff: {
    label: 'Invite your team',
    description: 'Add staff members who take bookings.',
    icon: Users,
    href: '/staff',
  },
  booking_page: {
    label: 'Customize your booking page',
    description: 'Brand your public booking link with your colors and logo.',
    icon: Palette,
    href: '/settings/branding',
  },
  payment_setup: {
    label: 'Set up payments',
    description: 'Connect a payment gateway to collect deposits or full payments.',
    icon: CreditCard,
    href: '/payments',
  },
  first_booking: {
    label: 'Create your first booking',
    description: 'Manually add a booking or share your booking link.',
    icon: PlusCircle,
    href: '/bookings/new',
  },
  invite_client: {
    label: 'Add a client',
    description: 'Import or manually add your first client record.',
    icon: UserPlus,
    href: '/clients',
  },
};

export default function OnboardingPage() {
  const router = useRouter();
  const [steps, setSteps] = useState<Step[]>([]);
  const [loading, setLoading] = useState(true);
  const [completedCount, setCompletedCount] = useState(0);

  useEffect(() => {
    (async () => {
      try {
        const res = await api.onboarding.getChecklist();
        const items: Step[] = (res.data?.items ?? []).map((item: any) => ({
          key: item.key,
          completed: item.completed,
          ...(STEP_META[item.key] ?? {
            label: item.key,
            description: '',
            icon: Circle,
            href: '/dashboard',
          }),
        }));
        setSteps(items);
        setCompletedCount(items.filter((s) => s.completed).length);
      } catch {
        // Fallback: show static steps if API unavailable
        const fallback = Object.entries(STEP_META).map(([key, meta]) => ({
          key,
          completed: false,
          ...meta,
        }));
        setSteps(fallback);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const total = steps.length || 8;
  const pct = total > 0 ? Math.round((completedCount / total) * 100) : 0;

  if (pct === 100) {
    router.replace('/dashboard');
    return null;
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-violet-50 via-white to-purple-50 flex flex-col">
      {/* Header */}
      <header className="px-6 py-5 flex items-center justify-between border-b border-white/60 bg-white/70 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-violet-600 to-purple-700 flex items-center justify-center shadow-lg shadow-violet-500/30">
            <span className="text-white font-bold text-lg leading-none">U</span>
          </div>
          <span className="font-semibold text-gray-900 text-lg">Upkilo</span>
        </div>
        <Link href="/dashboard" className="text-sm text-gray-500 hover:text-gray-700 transition-colors">
          Skip for now →
        </Link>
      </header>

      <main className="flex-1 flex flex-col items-center justify-start px-4 py-12 max-w-2xl mx-auto w-full">
        {/* Hero text */}
        <div className="text-center mb-10">
          <div className="inline-flex items-center gap-2 bg-violet-100 text-violet-700 text-sm font-semibold px-4 py-1.5 rounded-full mb-6">
            <Sparkles size={14} />
            Welcome to Upkilo
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 leading-tight mb-3">
            Let's get you set up
          </h1>
          <p className="text-gray-500 text-lg">
            Complete these steps to start accepting bookings.
          </p>
        </div>

        {/* Progress bar */}
        {!loading && (
          <div className="w-full mb-8">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-700">
                {completedCount} of {total} complete
              </span>
              <span className="text-sm font-bold text-violet-600">{pct}%</span>
            </div>
            <div
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={pct}
              aria-label={`Onboarding progress: ${pct}%`}
              className="h-2 bg-gray-100 rounded-full overflow-hidden"
            >
              <div
                className="h-full bg-gradient-to-r from-violet-500 to-purple-600 rounded-full transition-all duration-500"
                style={{ width: `${pct}%` }}
              />
            </div>
          </div>
        )}

        {/* Steps list */}
        <div className="w-full space-y-3">
          {loading
            ? Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="h-20 rounded-2xl bg-gray-100 animate-pulse" />
              ))
            : steps.map((step, idx) => {
                const Icon = step.icon;
                // Step is locked if the immediately preceding step is not yet complete
                const isLocked = idx > 0 && !steps[idx - 1].completed && !step.completed;
                return (
                  <Link
                    key={step.key}
                    href={step.completed || isLocked ? '#' : step.href}
                    aria-disabled={isLocked}
                    aria-label={isLocked ? `${step.label} (complete step ${idx} first)` : step.label}
                    className={cn(
                      'flex items-center gap-4 p-5 rounded-2xl border transition-all duration-200 group',
                      step.completed
                        ? 'bg-white border-green-200 cursor-default'
                        : isLocked
                        ? 'bg-gray-50 border-gray-100 cursor-not-allowed opacity-60'
                        : 'bg-white border-gray-200 hover:border-violet-300 hover:shadow-md hover:shadow-violet-500/10'
                    )}
                    onClick={isLocked ? (e) => e.preventDefault() : undefined}
                  >
                    {/* Step number / check / lock */}
                    <div
                      className={cn(
                        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors',
                        step.completed
                          ? 'bg-green-100 text-green-600'
                          : isLocked
                          ? 'bg-gray-100 text-gray-400'
                          : 'bg-violet-100 text-violet-600 group-hover:bg-violet-200'
                      )}
                    >
                      {step.completed ? (
                        <CheckCircle2 size={20} />
                      ) : isLocked ? (
                        <Lock size={18} aria-hidden="true" />
                      ) : (
                        <Icon size={20} />
                      )}
                    </div>

                    {/* Label + description */}
                    <div className="flex-1 min-w-0">
                      <p
                        className={cn(
                          'font-semibold text-sm leading-tight',
                          step.completed ? 'text-gray-400 line-through' : isLocked ? 'text-gray-400' : 'text-gray-900'
                        )}
                      >
                        {step.label}
                      </p>
                      {!step.completed && (
                        <p className="text-xs mt-0.5 leading-relaxed truncate text-gray-500">
                          {isLocked ? `Complete "${steps[idx - 1].label}" first` : step.description}
                        </p>
                      )}
                    </div>

                    {/* Arrow */}
                    {!step.completed && !isLocked && (
                      <ChevronRight
                        size={18}
                        className="text-gray-400 flex-shrink-0 group-hover:text-violet-500 transition-colors"
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
            Go to dashboard
            <ArrowRight size={16} />
          </Link>
          <p className="text-xs text-gray-400 mt-3">
            You can complete these steps any time from the dashboard.
          </p>
        </div>
      </main>
    </div>
  );
}
