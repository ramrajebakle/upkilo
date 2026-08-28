import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { unwrapList } from './unwrap';
import { HOOK_MANAGED } from './config';

export const LEAD_STATUSES = ['New', 'Contacted', 'Qualified', 'Closed'] as const;
export type LeadStatus = (typeof LEAD_STATUSES)[number];

export interface EnterpriseLead {
  id: string;
  companyName: string;
  contactName: string | null;
  email: string;
  phone: string | null;
  teamSize: string | null;
  currentPlatform: string | null;
  useCase: string | null;
  message: string | null;
  status: LeadStatus;
  createdAt: string;
}

const qkLeads = {
  all: ['enterprise-leads'] as const,
  list: () => [...qkLeads.all, 'list'] as const,
};

/**
 * Enterprise leads.
 *
 * The capture side has always worked — the form writes an EnterpriseLead and
 * emails sales — but nothing in the product ever displayed the table, so the
 * only usable copy of a lead was whatever landed in an inbox. A missed or
 * filtered email meant a lost enterprise prospect with no second record.
 */
export function useLeads() {
  return useQuery({
    queryKey: qkLeads.list(),
    queryFn: () =>
      api.enterprise
        .leads({ page: 1, pageSize: 100 }, HOOK_MANAGED)
        .then(unwrapList<EnterpriseLead>),
    staleTime: 30_000,
  });
}

/** Moves a lead through New -> Contacted -> Qualified -> Closed, with rollback on failure. */
export function useUpdateLeadStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: LeadStatus }) =>
      api.enterprise.updateLeadStatus(id, status),

    onMutate: async ({ id, status }) => {
      await qc.cancelQueries({ queryKey: qkLeads.all });
      const previous = qc.getQueryData<EnterpriseLead[]>(qkLeads.list());
      qc.setQueryData<EnterpriseLead[]>(qkLeads.list(), (rows) =>
        rows?.map((l) => (l.id === id ? { ...l, status } : l)),
      );
      return { previous };
    },

    // Without this the row would keep showing a stage the server rejected.
    onError: (_e, _v, ctx) => qc.setQueryData(qkLeads.list(), ctx?.previous),
    onSettled: () => qc.invalidateQueries({ queryKey: qkLeads.all }),
  });
}
