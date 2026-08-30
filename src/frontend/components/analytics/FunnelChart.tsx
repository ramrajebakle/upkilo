'use client';

import React from 'react';
import { TrendingUp, Users, MousePointer2, CreditCard, CheckCircle2, ArrowRight } from 'lucide-react';
import { cn } from '@/lib/utils';

interface FunnelStep {
  name: string;
  count: number;
  icon: React.ElementType;
  color: string;
}

const STEPS: FunnelStep[] = [
  { name: 'Website Visitors', count: 12400, icon: Users, color: 'bg-blue-500' },
  { name: 'Service Views', count: 4200, icon: MousePointer2, color: 'bg-indigo-500' },
  { name: 'Initiated Checkout', count: 1800, icon: CreditCard, color: 'bg-violet-500' },
  { name: 'Bookings Confirmed', count: 1250, icon: CheckCircle2, color: 'bg-emerald-500' },
];

export function FunnelChart() {
  const maxCount = STEPS[0].count;

  return (
    <div className="space-y-6 p-6 bg-card rounded-3xl border border-border-subtle shadow-xl shadow-gray-200/50">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h3 className="text-xl font-bold text-foreground flex items-center gap-2">
            <TrendingUp className="h-6 w-w text-success-fg" />
            Conversion Funnel
          </h3>
          <p className="text-foreground-secondary text-sm mt-1">Analyzing drop-off rates across the booking journey.</p>
        </div>
        <div className="px-4 py-2 bg-emerald-50 border border-emerald-100 rounded-2xl text-emerald-700 font-bold text-lg">
          {( (STEPS[3].count / STEPS[0].count) * 100).toFixed(1)}% Conv.
        </div>
      </div>

      <div className="space-y-4">
        {STEPS.map((step, index) => {
          const percentage = (step.count / maxCount) * 100;
          const dropOff = index > 0 ? (1 - step.count / STEPS[index - 1].count) * 100 : 0;

          return (
            <div key={step.name} className="relative group">
              {/* Drop-off line */}
              {index > 0 && (
                <div className="absolute -top-4 left-1/2 -translate-x-1/2 flex flex-col items-center z-10">
                  <div className="h-4 w-px bg-gray-200" />
                  <div className="bg-red-50 text-red-600 text-[10px] font-bold px-2 py-0.5 rounded-full border border-red-100 -mt-1 group-hover:scale-110 transition-transform">
                    -{dropOff.toFixed(0)}%
                  </div>
                </div>
              )}

              <div className="flex items-center gap-4">
                <div className={cn(
                  "h-12 w-12 rounded-2xl flex items-center justify-center text-white shadow-lg",
                  step.color
                )}>
                  <step.icon className="h-6 w-6" />
                </div>

                <div className="flex-1 space-y-1.5">
                  <div className="flex justify-between items-end">
                    <span className="text-sm font-bold text-foreground">{step.name}</span>
                    <span className="text-sm font-mono font-medium text-foreground-muted">
                      {step.count.toLocaleString()} <span className="text-[10px] uppercase">events</span>
                    </span>
                  </div>
                  
                  <div className="h-3 w-full bg-muted rounded-full overflow-hidden border border-gray-100/50">
                    <div 
                      className={cn("h-full rounded-full transition-all duration-1000 ease-out", step.color)}
                      style={{ width: `${percentage}%` }}
                    />
                  </div>
                </div>

                <div className="w-12 text-right">
                  <div className="text-xs font-bold text-foreground-muted">{percentage.toFixed(0)}%</div>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="mt-8 pt-6 border-t border-gray-50 flex items-center justify-center gap-2 text-primary font-bold text-sm cursor-pointer hover:underline group">
        View Detailed Attribution
        <ArrowRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  );
}
