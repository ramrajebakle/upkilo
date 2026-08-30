'use client';

import Link from 'next/link';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface BreadcrumbItem {
  label: string;
  href?: string;
  active?: boolean;
}

interface BreadcrumbsProps {
  items: BreadcrumbItem[];
  className?: string;
}

export function Breadcrumbs({ items, className }: BreadcrumbsProps) {
  return (
    <nav className={cn("flex items-center gap-2 text-sm text-foreground-secondary", className)} aria-label="Breadcrumb">
      <Link 
        href="/" 
        className="hover:text-primary transition-colors flex items-center justify-center"
      >
        <Home className="h-4 w-4" />
      </Link>

      {items.map((item, index) => (
        <div key={index} className="flex items-center gap-2">
          <ChevronRight className="h-3.5 w-3.5 text-gray-300 shrink-0" />
          {item.active || !item.href ? (
            <span className={cn(
              "font-bold truncate max-w-[150px]",
              item.active ? "text-foreground" : "text-foreground-muted"
            )}>
              {item.label}
            </span>
          ) : (
            <Link 
              href={item.href} 
              className="hover:text-primary transition-colors truncate max-w-[150px] font-medium"
            >
              {item.label}
            </Link>
          )}
        </div>
      ))}
    </nav>
  );
}
