import * as React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
    variant?: 'default' | 'secondary' | 'outline' | 'danger' | 'success';
}

function Badge({ className, variant = 'default', ...props }: BadgeProps) {
    const variants = {
        default: 'bg-primary-500 text-white',
        secondary: 'bg-secondary-500 text-white',
        outline: 'border-2 border-gray-200 text-gray-600',
        danger: 'bg-red-500 text-white',
        success: 'bg-emerald-500 text-white',
    };

    return (
        <div
            className={cn(
                'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
                variants[variant],
                className
            )}
            {...props}
        />
    );
}

export { Badge };
