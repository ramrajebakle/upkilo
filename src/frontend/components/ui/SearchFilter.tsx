'use client';

import { Search } from 'lucide-react';
import { cn } from '@/lib/utils';

interface SearchFilterProps {
    searchQuery: string;
    onSearchChange: (value: string) => void;
    searchPlaceholder?: string;
    filters?: {
        value: string;
        options: string[];
        onChange: (value: string) => void;
        capitalize?: boolean;
    };
    className?: string;
}

export function SearchFilter({
    searchQuery,
    onSearchChange,
    searchPlaceholder = 'Search...',
    filters,
    className,
}: SearchFilterProps) {
    return (
        <div className={cn('flex flex-col sm:flex-row gap-4 animate-fade-in-up', className)} style={{ animationDelay: '300ms' }}>
            <div className="relative flex-1">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
                <input
                    type="text"
                    placeholder={searchPlaceholder}
                    value={searchQuery}
                    onChange={(e) => onSearchChange(e.target.value)}
                    className="input pl-11"
                />
            </div>
            {filters && (
                <div className="flex gap-2 flex-wrap">
                    {filters.options.map((option) => (
                        <button
                            key={option}
                            onClick={() => filters.onChange(option)}
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-medium transition-all',
                                filters.capitalize !== false && 'capitalize',
                                filters.value === option
                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                    : 'bg-card text-foreground-secondary border border-border hover:border-primary-300'
                            )}
                        >
                            {option}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}
