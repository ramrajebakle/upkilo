/**
 * Covers the two clients-page defects:
 *  - the fetch effect read `searchQuery` but declared `[]` deps, so it captured
 *    the initial empty string and the `search` parameter was never sent;
 *  - bulk delete used Promise.all, reporting total failure on a partial one.
 */
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

let listCalls: Array<{ search?: string }> = [];
let deleteImpl: (id: string) => Promise<unknown> = async () => ({ data: {} });

vi.mock('@/lib/api', () => ({
  api: {
    clients: {
      list: (params: { search?: string }) => {
        listCalls.push(params);
        return Promise.resolve({ data: { data: [{ id: 'c1', firstName: 'Ada' }] } });
      },
      delete: (id: string) => deleteImpl(id),
    },
  },
  apiClient: {},
}));

import { useClients, useDeleteClients } from '@/lib/query/clients';

function wrap() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  return { client, Wrapper };
}

describe('useClients', () => {
  it('sends the search term to the server when it changes', async () => {
    listCalls = [];
    const { Wrapper } = wrap();
    const { result, rerender } = renderHook(({ q }: { q: string }) => useClients(q), {
      wrapper: Wrapper,
      initialProps: { q: '' },
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    rerender({ q: 'ada' });
    await waitFor(() => expect(listCalls.some(c => c.search === 'ada')).toBe(true));
  });

  it('normalises legacy field names', async () => {
    listCalls = [];
    const { Wrapper } = wrap();
    const { result } = renderHook(() => useClients(), { wrapper: Wrapper });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.[0]).toMatchObject({ id: 'c1', firstName: 'Ada', tags: [] });
  });
});

describe('useDeleteClients', () => {
  it('reports how many were deleted on a partial failure', async () => {
    deleteImpl = (id) => (id === 'c2' ? Promise.reject(new Error('nope')) : Promise.resolve({}));
    const { Wrapper } = wrap();
    const { result } = renderHook(() => useDeleteClients(), { wrapper: Wrapper });

    let message = '';
    await act(async () => {
      await result.current.mutateAsync(['c1', 'c2']).catch((e: Error) => { message = e.message; });
    });
    expect(message).toBe('Deleted 1, but 1 could not be removed.');
  });
});
