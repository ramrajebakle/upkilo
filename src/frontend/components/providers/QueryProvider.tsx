"use client";

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import type { AxiosError } from 'axios';

/**
 * A 4xx is an answer, not an outage: retrying a 401, 403, 404 or 422 produces
 * the same response a second time and only delays the error the user needs to
 * see. Server and transport failures are the ones worth another attempt.
 */
function shouldRetry(failureCount: number, error: unknown): boolean {
  const status = (error as AxiosError)?.response?.status;
  if (typeof status === 'number' && status >= 400 && status < 500) return false;
  return failureCount < 2;
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000, // 1 minute
            refetchOnWindowFocus: false,
            retry: shouldRetry,
          },
          mutations: {
            // A write that failed should surface immediately rather than being
            // replayed — the caller decides whether repeating it is safe.
            retry: false,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}
