/**
 * The lead pipeline. EnterpriseLead has carried a Status column since it was
 * introduced, but nothing could write it, so every lead sat on "New" forever.
 * These cover the write path and its rollback.
 */
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

let patchImpl: (id: string, status: string) => Promise<unknown> = async () => ({ data: {} });

vi.mock('@/lib/api', () => ({
  api: {
    enterprise: {
      leads: () => Promise.resolve({ data: { data: [
        { id: 'l1', companyName: 'Acme Clinics', email: 'ops@acme.test', status: 'New' },
      ] } }),
      updateLeadStatus: (id: string, status: string) => patchImpl(id, status),
    },
  },
  apiClient: {},
}));

import { useLeads, useUpdateLeadStatus } from '@/lib/query/leads';

function makeWrapper(client: QueryClient) {
  // Named rather than an anonymous arrow: react/display-name fires on a component returned
  // from a factory, and this was the repo's only blocking ESLint error.
  const QueryWrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client }, children);
  QueryWrapper.displayName = 'QueryWrapper';
  return QueryWrapper;
}

const newClient = () =>
  new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });

describe('enterprise leads', () => {
  it('lists captured leads', async () => {
    const client = newClient();
    const { result } = renderHook(() => useLeads(), { wrapper: makeWrapper(client) });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.[0]).toMatchObject({ id: 'l1', companyName: 'Acme Clinics', status: 'New' });
  });

  it('moves a lead to the next stage optimistically', async () => {
    patchImpl = () => Promise.resolve({ data: {} });
    const client = newClient();
    client.setQueryData(['enterprise-leads', 'list'], [{ id: 'l1', companyName: 'Acme', status: 'New' }]);

    const { result } = renderHook(() => useUpdateLeadStatus(), { wrapper: makeWrapper(client) });
    await act(async () => { await result.current.mutateAsync({ id: 'l1', status: 'Contacted' }); });

    const rows = client.getQueryData(['enterprise-leads', 'list']) as Array<{ status: string }>;
    expect(rows[0].status).toBe('Contacted');
  });

  it('rolls back when the server rejects the change', async () => {
    patchImpl = () => Promise.reject(new Error('nope'));
    const client = newClient();
    client.setQueryData(['enterprise-leads', 'list'], [{ id: 'l1', companyName: 'Acme', status: 'New' }]);

    const { result } = renderHook(() => useUpdateLeadStatus(), { wrapper: makeWrapper(client) });
    await act(async () => {
      await result.current.mutateAsync({ id: 'l1', status: 'Qualified' }).catch(() => {});
    });

    await waitFor(() => {
      const rows = client.getQueryData(['enterprise-leads', 'list']) as Array<{ status: string }>;
      // Must not keep showing a stage the server refused.
      expect(rows[0].status).toBe('New');
    });
  });
});
