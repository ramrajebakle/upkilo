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
                <span className="text-sm font-medium text-gray-700">{label}</span>
                <span className="text-xs text-gray-500">
                    <span className={cn(
                        "font-semibold",
                        isHigh ? "text-red-600" : isWarning ? "text-amber-600" : "text-gray-900"
                    )}>
                        {format === 'currency' ? `$${used.toFixed(2)}` : used.toLocaleString()}
                    </span>
                    {" / "}
                    {format === 'currency' ? `$${limit.toFixed(2)}` : `${limit.toLocaleString()} ${unit}`}
                </span>
            </div>

            <div className="h-2 w-full bg-gray-100 rounded-full overflow-hidden">
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
                    isHigh ? "text-red-500" : "text-amber-500"
                )}>
                    {isHigh ? "Limit reached soon" : "Approaching limit"}
                </p>
            )}
        </div>
    );
}
