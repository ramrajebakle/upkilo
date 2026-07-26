'use client';

import { ReactNode } from 'react';
import { LucideIcon } from 'lucide-react';

interface PageHeaderProps {
    icon: LucideIcon;
    iconGradient: string;
    iconShadow: string;
    title: string;
    description: string;
    actions?: ReactNode;
}
import { memo } from 'react';

export const PageHeader = memo(function PageHeader({ icon: Icon, iconGradient, iconShadow, title, description, actions }: PageHeaderProps) {
    return (
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div className="animate-fade-in-up">
                <div className="flex items-center gap-3 mb-2">
                    <div className={`p-2 bg-gradient-to-br ${iconGradient} rounded-xl shadow-lg ${iconShadow}`}>
                        <Icon className="h-5 w-5 text-white" />
                    </div>
                    <h1
                        className="text-2xl lg:text-3xl font-bold text-slate-900 dark:text-white"
                        style={{ fontFamily: 'Outfit, sans-serif' }}
                    >
                        {title}
                    </h1>
                </div>
                <p className="text-slate-500 dark:text-slate-400">{description}</p>
            </div>
            {actions && (
                <div className="flex gap-3 animate-fade-in" style={{ animationDelay: '100ms' }}>
                    {actions}
                </div>
            )}
        </div>
    );
});
