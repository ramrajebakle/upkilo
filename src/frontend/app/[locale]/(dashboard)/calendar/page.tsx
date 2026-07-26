'use client';

import { Suspense } from 'react';
import CalendarView from '@/components/Calendar';

function CalendarSkeleton() {
  return (
    <div className="flex flex-col gap-4 p-6 animate-pulse">
      <div className="h-10 w-56 bg-gray-100 rounded-xl" />
      <div className="h-6 w-80 bg-gray-100 rounded-lg" />
      <div className="grid grid-cols-7 gap-2 mt-4">
        {Array.from({ length: 35 }).map((_, i) => (
          <div key={i} className="h-24 bg-gray-100 rounded-xl" />
        ))}
      </div>
    </div>
  );
}

export default function CalendarPage() {
  return (
    <div className="flex flex-col h-full min-h-0">
      {/* Page header */}
      <div className="px-6 pt-6 pb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-gray-100">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Calendar</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            View and manage all your appointments
          </p>
        </div>
      </div>

      {/* Calendar fills remaining height */}
      <div className="flex-1 min-h-0 overflow-hidden">
        <Suspense fallback={<CalendarSkeleton />}>
          <CalendarView />
        </Suspense>
      </div>
    </div>
  );
}
