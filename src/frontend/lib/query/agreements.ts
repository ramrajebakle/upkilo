import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { unwrapList } from './unwrap';
import { HOOK_MANAGED } from './config';

export type AgreementStatus = 'NotSigned' | 'Signed' | 'Expired' | 'Terminated';
export type AgreementType = 'HipaaBaa' | 'Sla';

export interface BaaRecord {
  status: AgreementStatus;
  documentVersion: string | null;
  signatoryName: string | null;
  signatoryTitle: string | null;
  signedAt: string | null;
  signedFromIp: string | null;
  effectiveFrom: string | null;
  expiresAt: string | null;
  notes: string | null;
}

export interface SlaRecord {
  status: AgreementStatus;
  uptimeTargetPercent: number | null;
  effectiveFrom: string | null;
  expiresAt: string | null;
  notes: string | null;
}

export interface TenantAgreements {
  tenantId: string;
  tenantName: string;
  /** Null means this tenant has no BAA on record at all — not that it is unsigned. */
  baa: BaaRecord | null;
  sla: SlaRecord | null;
}

const qkAgreements = {
  all: ['tenant-agreements'] as const,
  list: () => [...qkAgreements.all, 'list'] as const,
};

/**
 * Every tenant with its BAA and SLA state.
 *
 * Returns tenants that have neither, deliberately: the question this view exists
 * to answer is "who still owes us an agreement", and a list of only signed ones
 * cannot answer it.
 */
export function useAgreements() {
  return useQuery({
    queryKey: qkAgreements.list(),
    queryFn: () => api.agreements.list(undefined, HOOK_MANAGED).then(unwrapList<TenantAgreements>),
    staleTime: 30_000,
  });
}

export interface AgreementUpsert {
  status: AgreementStatus;
  documentVersion?: string | null;
  signatoryName?: string | null;
  signatoryTitle?: string | null;
  effectiveFrom?: string | null;
  expiresAt?: string | null;
  uptimeTargetPercent?: number | null;
  notes?: string | null;
}

/**
 * Records or amends one tenant's agreement.
 *
 * No optimistic update here, unlike the lead pipeline. This writes a legal record,
 * and showing "Signed" a moment before the server has accepted it is the wrong
 * trade for a compliance view — the list refetches once the write lands.
 */
export function useUpsertAgreement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      tenantId,
      type,
      data,
    }: {
      tenantId: string;
      type: AgreementType;
      data: AgreementUpsert;
    }) => api.agreements.upsert(tenantId, type, data as unknown as Record<string, unknown>),
    onSuccess: () => qc.invalidateQueries({ queryKey: qkAgreements.all }),
  });
}
