'use client';

import { cn } from '@/lib/utils';

interface UsageProgressProps {
    label: string;
    used: number;
    limit: number;
    unit?: string;
    className?: string;
    format?: 'number' | 'currency';
}

export function UsageProgress({
    label, used, limit, unit = '', className, format = 'number'
}: UsageProgressProps) {
    const percentage = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;

    // Status colors
    const isHigh = percentage >= 90;
    const isWarning = percentage >= 80 && percentage < 90;

    return (
        <div className={cn("space-y-2", className)}>
            <div className="flex justify-between items-end">
                <span className="text-sm font-medium text-foreground">{label}</span>
                <span className="text-xs text-foreground-secondary">
                    <span className={cn(
                        "font-semibold",
                        isHigh ? "text-danger-fg" : isWarning ? "text-warning-fg" : "text-foreground"
                    )}>
                        {format === 'currency' ? `$${used.toFixed(2)}` : used.toLocaleString()}
                    </span>
                    {" / "}
                    {format === 'currency' ? `$${limit.toFixed(2)}` : `${limit.toLocaleString()} ${unit}`}
                </span>
            </div>

            <div className="h-2 w-full bg-muted rounded-full overflow-hidden">
                <div
                    className={cn(
                        "h-full transition-all duration-500 rounded-full",
                        isHigh ? "bg-red-500" : isWarning ? "bg-amber-500" : "bg-primary-500"
                    )}
                    style={{ width: `${percentage}%` }}
                />
            </div>

            {(isHigh || isWarning) && (
                <p className={cn(
                    "text-[10px] uppercase font-bold tracking-tight",
                    isHigh ? "text-danger-fg" : "text-warning-fg"
                )}>
                    {isHigh ? "Limit reached soon" : "Approaching limit"}
                </p>
            )}
        </div>
    );
}
