'use client';

export default function DashboardLoading() {
    return (
        <div className="space-y-6 animate-pulse p-1">
            {/* Header skeleton */}
            <div className="flex items-center justify-between">
                <div className="space-y-2">
                    <div className="h-8 w-48 bg-slate-200 rounded-lg" />
                    <div className="h-4 w-64 bg-muted rounded-lg" />
                </div>
                <div className="flex gap-2">
                    <div className="h-10 w-24 bg-slate-200 rounded-lg" />
                    <div className="h-10 w-24 bg-muted rounded-lg" />
                </div>
            </div>

            {/* Stats grid skeleton */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="bg-card rounded-2xl border border-border-subtle p-5 space-y-3">
                        <div className="flex items-center justify-between">
                            <div className="h-10 w-10 bg-muted rounded-xl" />
                            <div className="h-5 w-16 bg-muted rounded-full" />
                        </div>
                        <div className="h-8 w-24 bg-slate-200 rounded-lg" />
                        <div className="h-3 w-32 bg-muted rounded-full" />
                    </div>
                ))}
            </div>

            {/* Main content skeleton */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div className="lg:col-span-2 bg-card rounded-2xl border border-border-subtle p-6">
                    <div className="h-6 w-40 bg-slate-200 rounded-lg mb-6" />
                    <div className="h-64 bg-muted rounded-xl" />
                </div>
                <div className="bg-card rounded-2xl border border-border-subtle p-6 space-y-4">
                    <div className="h-6 w-32 bg-slate-200 rounded-lg" />
                    {[1, 2, 3, 4, 5].map((i) => (
                        <div key={i} className="flex items-center gap-3">
                            <div className="h-8 w-8 bg-muted rounded-full" />
                            <div className="flex-1 space-y-1">
                                <div className="h-4 w-full bg-muted rounded" />
                                <div className="h-3 w-2/3 bg-muted rounded" />
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
