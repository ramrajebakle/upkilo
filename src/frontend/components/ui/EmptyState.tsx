'use client';

import { ReactNode } from 'react';
import { LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';

interface EmptyStateProps {
    icon: LucideIcon;
    title: string;
    description: string;
    action?: ReactNode;
    className?: string;
}
import { memo } from 'react';

export const EmptyState = memo(function EmptyState({ icon: Icon, title, description, action, className }: EmptyStateProps) {
    return (
        <div className={cn('card-elevated py-16 text-center animate-fade-in', className)}>
            <div className="mx-auto w-16 h-16 bg-slate-100 rounded-2xl flex items-center justify-center mb-4">
                <Icon className="h-8 w-8 text-slate-400" />
            </div>
            <h3 className="text-lg font-semibold text-slate-900 mb-2">{title}</h3>
            <p className="text-slate-500 mb-6 max-w-md mx-auto">{description}</p>
            {action}
        </div>
    );
});
