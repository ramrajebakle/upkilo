'use client';

import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { cn } from '@/lib/utils';

interface PaginationProps {
    currentPage: number;
    totalPages: number;
    totalItems?: number;
    pageSize?: number;
    onPageChange: (page: number) => void;
    showItemCount?: boolean;
}

import { memo } from 'react';

export const Pagination = memo(function Pagination({
    currentPage,
    totalPages,
    totalItems,
    pageSize = 10,
    onPageChange,
    showItemCount = true,
}: PaginationProps) {
    if (totalPages <= 1) return null;

    const getVisiblePages = () => {
        const pages: (number | '...')[] = [];
        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) pages.push(i);
        } else {
            pages.push(1);
            if (currentPage > 3) pages.push('...');
            
            const start = Math.max(2, currentPage - 1);
            const end = Math.min(totalPages - 1, currentPage + 1);
            
            for (let i = start; i <= end; i++) pages.push(i);
            
            if (currentPage < totalPages - 2) pages.push('...');
            pages.push(totalPages);
        }
        return pages;
    };

    const startItem = (currentPage - 1) * pageSize + 1;
    const endItem = Math.min(currentPage * pageSize, totalItems || currentPage * pageSize);

    return (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 py-4">
            {showItemCount && totalItems && (
                <p className="text-sm text-foreground-secondary">
                    Showing <span className="font-medium text-foreground">{startItem}</span> to{' '}
                    <span className="font-medium text-foreground">{endItem}</span> of{' '}
                    <span className="font-medium text-foreground">{totalItems}</span> results
                </p>
            )}

            <div className="flex items-center gap-1">
                {/* First page */}
                <button
                    onClick={() => onPageChange(1)}
                    disabled={currentPage === 1}
                    className="p-2 rounded-lg hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                    aria-label="First page"
                >
                    <ChevronsLeft className="h-4 w-4 text-foreground-secondary" />
                </button>

                {/* Previous */}
                <button
                    onClick={() => onPageChange(currentPage - 1)}
                    disabled={currentPage === 1}
                    className="p-2 rounded-lg hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                    aria-label="Previous page"
                >
                    <ChevronLeft className="h-4 w-4 text-foreground-secondary" />
                </button>

                {/* Page numbers */}
                {getVisiblePages().map((page, i) => (
                    page === '...' ? (
                        <span key={`dots-${i}`} className="px-2 text-foreground-muted">…</span>
                    ) : (
                        <button
                            key={page}
                            onClick={() => onPageChange(page)}
                            className={cn(
                                'min-w-[36px] h-9 rounded-lg text-sm font-medium transition-all',
                                currentPage === page
                                    ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                    : 'text-foreground-secondary hover:bg-accent'
                            )}
                        >
                            {page}
                        </button>
                    )
                ))}

                {/* Next */}
                <button
                    onClick={() => onPageChange(currentPage + 1)}
                    disabled={currentPage === totalPages}
                    className="p-2 rounded-lg hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                    aria-label="Next page"
                >
                    <ChevronRight className="h-4 w-4 text-foreground-secondary" />
                </button>

                {/* Last page */}
                <button
                    onClick={() => onPageChange(totalPages)}
                    disabled={currentPage === totalPages}
                    className="p-2 rounded-lg hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                    aria-label="Last page"
                >
                    <ChevronsRight className="h-4 w-4 text-foreground-secondary" />
                </button>
            </div>
        </div>
    );
});
