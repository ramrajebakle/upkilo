'use client';

import { LucideIcon, ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ReactNode, memo } from 'react';

interface StatItem {
    label: string;
    value: string | number;
    icon: LucideIcon;
    color: 'emerald' | 'blue' | 'violet' | 'amber' | 'rose' | 'cyan' | 'orange' | 'slate';
    trend?: string;
    trendUp?: boolean;
    extra?: ReactNode;
}

interface StatsGridProps {
    stats: StatItem[];
    loading?: boolean;
    columns?: 2 | 3 | 4;
}

// Premium gradient palette matching the dashboard's visual language.
const gradientMap: Record<string, { gradient: string; shadow: string }> = {
    emerald: { gradient: 'from-emerald-500 to-emerald-700', shadow: 'shadow-emerald-500/25' },
    blue:    { gradient: 'from-blue-500 to-indigo-600',     shadow: 'shadow-blue-500/25' },
    violet:  { gradient: 'from-violet-500 to-violet-700',   shadow: 'shadow-violet-500/25' },
    amber:   { gradient: 'from-amber-500 to-orange-600',    shadow: 'shadow-amber-500/25' },
    rose:    { gradient: 'from-rose-500 to-pink-600',       shadow: 'shadow-rose-500/25' },
    cyan:    { gradient: 'from-cyan-500 to-cyan-700',       shadow: 'shadow-cyan-500/25' },
    orange:  { gradient: 'from-orange-500 to-red-600',      shadow: 'shadow-orange-500/25' },
    slate:   { gradient: 'from-slate-500 to-slate-700',     shadow: 'shadow-slate-500/25' },
};

export const StatsGrid = memo(function StatsGrid({ stats, loading = false, columns = 4 }: StatsGridProps) {
    const colClass = columns === 2 ? 'lg:grid-cols-2' : columns === 3 ? 'lg:grid-cols-3' : 'lg:grid-cols-4';

    if (loading) {
        return (
            <div className={cn('grid grid-cols-2 gap-4', colClass)}>
                {[...Array(columns)].map((_, i) => (
                    <div key={i} className="card-elevated p-5">
                        <div className="flex items-start justify-between mb-3">
                            <div className="space-y-2 flex-1">
                                <div className="h-3 w-20 bg-muted rounded animate-pulse" />
                                <div className="h-7 w-24 bg-muted rounded animate-pulse" />
                            </div>
                            <div className="h-11 w-11 bg-muted rounded-xl animate-pulse" />
                        </div>
                    </div>
                ))}
            </div>
        );
    }

    return (
        <div className={cn('grid grid-cols-2 gap-4', colClass)}>
            {stats.map((stat, i) => {
                const palette = gradientMap[stat.color] || gradientMap.slate;
                const Icon = stat.icon;
                const positive = stat.trendUp !== false;
                return (
                    <div
                        key={stat.label}
                        className="card-elevated p-5 animate-fade-in-up hover:-translate-y-0.5 transition-transform dark:bg-slate-900 dark:border-slate-800"
                        style={{ animationDelay: `${(i + 1) * 80}ms` }}
                    >
                        <div className="flex items-start justify-between mb-2">
                            <div className="min-w-0 flex-1">
                                <p className="text-sm font-medium text-slate-500 dark:text-slate-400 mb-1 truncate">{stat.label}</p>
                                <p
                                    className="text-2xl font-bold text-slate-900 dark:text-white tracking-tight truncate"
                                    style={{ fontFamily: 'var(--font-display)' }}
                                >
                                    {stat.value}
                                </p>
                            </div>
                            <div className={cn('p-2.5 rounded-xl bg-gradient-to-br shadow-lg flex-shrink-0', palette.gradient, palette.shadow)}>
                                <Icon className="h-5 w-5 text-white" />
                            </div>
                        </div>
                        {stat.trend && (
                            <div className="flex items-center gap-1.5 text-xs">
                                <span
                                    className={cn(
                                        'inline-flex items-center gap-0.5 font-semibold px-1.5 py-0.5 rounded-md',
                                        positive 
                                            ? 'text-emerald-700 bg-emerald-50 dark:text-emerald-400 dark:bg-emerald-500/10' 
                                            : 'text-rose-700 bg-rose-50 dark:text-rose-400 dark:bg-rose-500/10'
                                    )}
                                >
                                    {positive ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                                    {stat.trend}
                                </span>
                            </div>
                        )}
                        {stat.extra}
                    </div>
                );
            })}
        </div>
    );
});
