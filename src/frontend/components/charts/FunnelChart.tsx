'use client';

import { cn } from '@/lib/utils';

interface FunnelStep {
    name: string;
    value: number;
    color?: string;
}

interface FunnelChartProps {
    steps: FunnelStep[];
}

const STEP_COLORS = [
    { bg: 'bg-cyan-500', text: 'text-cyan-600', bar: '#06b6d4' },
    { bg: 'bg-violet-500', text: 'text-violet-600', bar: '#8b5cf6' },
    { bg: 'bg-emerald-500', text: 'text-emerald-600', bar: '#10b981' },
    { bg: 'bg-amber-500', text: 'text-amber-600', bar: '#f59e0b' },
    { bg: 'bg-rose-500', text: 'text-rose-600', bar: '#f43f5e' },
];

export function FunnelChart({ steps }: FunnelChartProps) {
    if (!steps || steps.length === 0) {
        return (
            <div className="flex items-center justify-center h-[200px] text-slate-400 text-sm">
                No data available
            </div>
        );
    }

    const max = Math.max(...steps.map(s => s.value), 1);

    return (
        <div className="space-y-3">
            {steps.map((step, i) => {
                const pct = (step.value / max) * 100;
                const convPct = i > 0 ? ((step.value / steps[i - 1].value) * 100).toFixed(0) : null;
                const color = STEP_COLORS[i % STEP_COLORS.length];

                return (
                    <div key={step.name}>
                        <div className="flex items-center justify-between text-xs mb-1.5">
                            <div className="flex items-center gap-2">
                                <span
                                    className={`w-5 h-5 rounded-md flex items-center justify-center text-white font-bold text-[10px] flex-shrink-0`}
                                    style={{ background: color.bar }}
                                >
                                    {i + 1}
                                </span>
                                <span className="font-medium text-slate-700">{step.name}</span>
                            </div>
                            <div className="flex items-center gap-2">
                                {convPct && (
                                    <span className="text-slate-400 text-[10px]">↓{convPct}%</span>
                                )}
                                <span className={cn('font-bold', color.text)}>{step.value.toLocaleString()}</span>
                            </div>
                        </div>
                        <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                            <div
                                className="h-full rounded-full transition-all duration-700"
                                style={{ width: `${pct}%`, background: color.bar }}
                            />
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
