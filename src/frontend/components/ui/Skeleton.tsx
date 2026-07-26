import { cn } from "@/lib/utils";

type SkeletonProps = React.HTMLAttributes<HTMLDivElement>;

export function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn("animate-pulse rounded-md bg-slate-200 dark:bg-slate-800/50", className)}
      {...props}
    />
  );
}

export function SkeletonCard() {
    return (
        <div className="card-elevated p-6 space-y-4 dark:bg-slate-900 dark:border-slate-800">
            <div className="flex items-center justify-between">
                <Skeleton className="h-10 w-10 rounded-xl" />
                <Skeleton className="h-5 w-12 rounded-full" />
            </div>
            <div className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-8 w-32" />
            </div>
        </div>
    );
}

export function SkeletonBookingItem() {
    return (
        <div className="p-4 flex items-center gap-4">
            <Skeleton className="w-12 h-12 rounded-xl" />
            <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-3 w-48" />
            </div>
            <div className="text-right space-y-2">
                <Skeleton className="h-4 w-16 ml-auto" />
                <Skeleton className="h-4 w-12 ml-auto" />
            </div>
        </div>
    );
}

export function SkeletonTable({ rows = 5, cols = 4 }: { rows?: number, cols?: number }) {
    return (
        <div className="w-full space-y-4">
            <div className="flex items-center justify-between">
                <div className="flex gap-2">
                    <Skeleton className="h-10 w-64" />
                    <Skeleton className="h-10 w-32" />
                </div>
                <Skeleton className="h-10 w-32" />
            </div>
            <div className="border border-slate-100 dark:border-slate-800 rounded-2xl overflow-hidden">
                <div className="bg-slate-50 dark:bg-slate-800/50 p-4 border-b border-slate-100 dark:border-slate-800 flex gap-4">
                    {Array.from({ length: cols }).map((_, i) => (
                        <Skeleton key={i} className="h-4 flex-1" />
                    ))}
                </div>
                <div className="divide-y divide-slate-100 dark:divide-slate-800">
                    {Array.from({ length: rows }).map((_, i) => (
                        <div key={i} className="p-4 flex gap-4">
                            {Array.from({ length: cols }).map((_, j) => (
                                <Skeleton key={j} className="h-4 flex-1" />
                            ))}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
