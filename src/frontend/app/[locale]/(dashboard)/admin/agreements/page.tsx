'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { FileCheck2, RefreshCw, ShieldAlert, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { cn, formatDate } from '@/lib/utils';
import { ErrorState, EmptyState, SkeletonTable, PageHeader } from '@/components/ui';
import { toast } from 'sonner';
import {
    useAgreements,
    useUpsertAgreement,
    type TenantAgreements,
    type AgreementType,
    type AgreementUpsert,
} from '@/lib/query/agreements';

export default function AdminAgreementsPage() {
    const { user, isInitialized } = useAuthStore();
    const { data: session, status: sessionStatus } = useSession();
    const router = useRouter();

    // Both role vocabularies — authStore is filled by the real login, the session by
    // any sign-in. Gating on one alone locks out whichever path did not populate it.
    const sessionRole = session?.user?.role;
    const isPlatformStaff =
        user?.role === 'superadmin' ||
        sessionRole === 'platform_owner' ||
        sessionRole === 'platform_admin';

    const resolved = isInitialized && sessionStatus !== 'loading';
    if (resolved && !isPlatformStaff) {
        router.push('/dashboard');
        return null;
    }

    return <AgreementsView enabled={isPlatformStaff} />;
}

function AgreementsView({ enabled }: { enabled: boolean }) {
    const { data: rows = [], isPending, isError, error, refetch, isFetching } = useAgreements();
    const upsert = useUpsertAgreement();
    const [onlyGaps, setOnlyGaps] = useState(false);
    const [editing, setEditing] = useState<{ row: TenantAgreements; type: AgreementType } | null>(null);

    const missingBaa = useMemo(
        () => rows.filter((r) => r.baa?.status !== 'Signed').length,
        [rows],
    );

    const visible = onlyGaps ? rows.filter((r) => r.baa?.status !== 'Signed') : rows;

    if (!enabled) return null;

    return (
        <div className="space-y-6 max-w-7xl mx-auto pb-12">
            <PageHeader
                title="Agreements"
                description="HIPAA BAA and uptime SLA held with each tenant."
                icon={FileCheck2}
                iconGradient="from-emerald-500 to-primary-600"
                iconShadow="shadow-emerald-500/25"
                actions={
                    <button onClick={() => refetch()} className="btn btn-secondary" disabled={isFetching}>
                        <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} aria-hidden="true" />
                        Refresh
                    </button>
                }
            />

            {!isPending && !isError && missingBaa > 0 && (
                <div
                    role="status"
                    className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-500/30 dark:bg-amber-500/10"
                >
                    <ShieldAlert className="mt-0.5 h-5 w-5 flex-shrink-0 text-warning-fg" aria-hidden="true" />
                    <div className="text-sm">
                        <p className="font-semibold text-amber-900 dark:text-amber-200">
                            {missingBaa} {missingBaa === 1 ? 'tenant has' : 'tenants have'} no signed BAA
                        </p>
                        <p className="mt-0.5 text-amber-800 dark:text-amber-300/90">
                            Medical and dental features stay locked for these tenants until a BAA is signed.
                        </p>
                    </div>
                </div>
            )}

            <label className="flex w-fit cursor-pointer items-center gap-2 text-sm text-text-secondary">
                <input
                    type="checkbox"
                    checked={onlyGaps}
                    onChange={(e) => setOnlyGaps(e.target.checked)}
                    className="h-4 w-4 rounded border-surface-300 text-primary-600 focus:ring-primary-500"
                />
                Show only tenants without a signed BAA
            </label>

            {isError ? (
                <ErrorState
                    title="Couldn&rsquo;t load agreements"
                    error={error}
                    onRetry={() => refetch()}
                    isRetrying={isFetching}
                />
            ) : isPending ? (
                <SkeletonTable rows={6} cols={4} />
            ) : visible.length === 0 ? (
                <EmptyState
                    icon={ShieldCheck}
                    title={onlyGaps ? 'Every tenant has a signed BAA' : 'No tenants yet'}
                    description={
                        onlyGaps
                            ? 'Nothing outstanding.'
                            : 'Tenants will appear here once they are created.'
                    }
                />
            ) : (
                <div className="space-y-3">
                    {visible.map((row) => (
                        <article key={row.tenantId} className="card-elevated p-5">
                            <h3 className="font-bold text-text-primary">{row.tenantName}</h3>

                            <div className="mt-4 grid gap-4 md:grid-cols-2">
                                <AgreementPanel
                                    label="HIPAA BAA"
                                    onEdit={() => setEditing({ row, type: 'HipaaBaa' })}
                                    status={row.baa?.status ?? null}
                                    lines={
                                        row.baa
                                            ? [
                                                  row.baa.signatoryName
                                                      ? `${row.baa.signatoryName}${row.baa.signatoryTitle ? ` · ${row.baa.signatoryTitle}` : ''}`
                                                      : 'No signatory recorded',
                                                  row.baa.signedAt ? `Signed ${formatDate(row.baa.signedAt)}` : null,
                                                  row.baa.documentVersion ? `Version ${row.baa.documentVersion}` : null,
                                              ]
                                            : ['No BAA on record']
                                    }
                                />
                                <AgreementPanel
                                    label="Uptime SLA"
                                    onEdit={() => setEditing({ row, type: 'Sla' })}
                                    status={row.sla?.status ?? null}
                                    lines={
                                        row.sla
                                            ? [
                                                  row.sla.uptimeTargetPercent != null
                                                      ? `${row.sla.uptimeTargetPercent}% target`
                                                      : 'No uptime target agreed',
                                                  row.sla.effectiveFrom ? `From ${formatDate(row.sla.effectiveFrom)}` : null,
                                                  row.sla.notes,
                                              ]
                                            : ['No SLA on record']
                                    }
                                />
                            </div>
                        </article>
                    ))}
                </div>
            )}

            {editing && (
                <AgreementDialog
                    row={editing.row}
                    type={editing.type}
                    saving={upsert.isPending}
                    onClose={() => setEditing(null)}
                    onSave={async (data) => {
                        try {
                            await upsert.mutateAsync({ tenantId: editing.row.tenantId, type: editing.type, data });
                            toast.success(`${editing.type === 'Sla' ? 'SLA' : 'BAA'} updated for ${editing.row.tenantName}`);
                            setEditing(null);
                        } catch (e) {
                            toast.error(e instanceof Error ? e.message : 'Could not save this agreement');
                        }
                    }}
                />
            )}
        </div>
    );
}

function AgreementPanel({
    label,
    status,
    lines,
    onEdit,
}: {
    label: string;
    status: string | null;
    lines: (string | null)[];
    onEdit: () => void;
}) {
    const tone =
        status === 'Signed'
            ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
            : status === 'Expired' || status === 'Terminated'
              ? 'bg-danger-500/10 text-danger-500'
              : 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300';

    return (
        <div className="rounded-xl border border-surface-200 p-4">
            <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                    <span className="text-sm font-semibold text-text-primary">{label}</span>
                    <span className={cn('rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider', tone)}>
                        {status ?? 'None'}
                    </span>
                </div>
                <button onClick={onEdit} className="text-xs font-semibold text-primary-600 hover:text-primary-700">
                    Edit
                </button>
            </div>
            <div className="mt-2 space-y-0.5 text-sm text-text-secondary">
                {lines.filter(Boolean).map((l, i) => (
                    <p key={i}>{l}</p>
                ))}
            </div>
        </div>
    );
}

function AgreementDialog({
    row,
    type,
    saving,
    onClose,
    onSave,
}: {
    row: TenantAgreements;
    type: AgreementType;
    saving: boolean;
    onClose: () => void;
    onSave: (data: AgreementUpsert) => void;
}) {
    const isSla = type === 'Sla';
    const existing = isSla ? row.sla : row.baa;
    const baa = row.baa;

    const [status, setStatus] = useState<AgreementUpsert['status']>(existing?.status ?? 'NotSigned');
    const [signatoryName, setSignatoryName] = useState(baa?.signatoryName ?? '');
    const [signatoryTitle, setSignatoryTitle] = useState(baa?.signatoryTitle ?? '');
    const [documentVersion, setDocumentVersion] = useState(baa?.documentVersion ?? '');
    const [uptime, setUptime] = useState(row.sla?.uptimeTargetPercent?.toString() ?? '');
    const [notes, setNotes] = useState(existing?.notes ?? '');

    // Mirrors the server rule, so the reason is visible before the request is sent
    // rather than arriving as a rejection.
    const needsSignatory = !isSla && status === 'Signed' && !signatoryName.trim();

    return (
        <div className="fixed inset-0 z-[500] flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true">
            <div className="w-full max-w-lg rounded-2xl bg-surface-50 p-6 shadow-xl">
                <h2 className="text-lg font-bold text-text-primary">
                    {isSla ? 'Uptime SLA' : 'HIPAA BAA'} — {row.tenantName}
                </h2>

                <div className="mt-4 space-y-4">
                    <div>
                        <label htmlFor="ag-status" className="mb-1 block text-sm font-medium text-text-secondary">Status</label>
                        <select
                            id="ag-status"
                            value={status}
                            onChange={(e) => setStatus(e.target.value as AgreementUpsert['status'])}
                            className="input w-full"
                        >
                            {(['NotSigned', 'Signed', 'Expired', 'Terminated'] as const).map((s) => (
                                <option key={s} value={s}>{s}</option>
                            ))}
                        </select>
                    </div>

                    {!isSla && (
                        <>
                            <div className="grid gap-3 sm:grid-cols-2">
                                <div>
                                    <label htmlFor="ag-name" className="mb-1 block text-sm font-medium text-text-secondary">
                                        Signatory name
                                    </label>
                                    <input id="ag-name" value={signatoryName} onChange={(e) => setSignatoryName(e.target.value)} className="input w-full" />
                                </div>
                                <div>
                                    <label htmlFor="ag-title" className="mb-1 block text-sm font-medium text-text-secondary">
                                        Signatory title
                                    </label>
                                    <input id="ag-title" value={signatoryTitle} onChange={(e) => setSignatoryTitle(e.target.value)} className="input w-full" />
                                </div>
                            </div>
                            <div>
                                <label htmlFor="ag-version" className="mb-1 block text-sm font-medium text-text-secondary">
                                    Document version
                                </label>
                                <input id="ag-version" value={documentVersion} onChange={(e) => setDocumentVersion(e.target.value)} placeholder="2024.1" className="input w-full" />
                            </div>
                        </>
                    )}

                    {isSla && (
                        <div>
                            <label htmlFor="ag-uptime" className="mb-1 block text-sm font-medium text-text-secondary">
                                Uptime target (%)
                            </label>
                            <input
                                id="ag-uptime"
                                type="number"
                                step="0.01"
                                min="0"
                                max="100"
                                value={uptime}
                                onChange={(e) => setUptime(e.target.value)}
                                placeholder="99.9"
                                className="input w-full"
                            />
                            <p className="mt-1 text-xs text-text-tertiary">
                                Recorded for reference. Nothing measures uptime against this yet.
                            </p>
                        </div>
                    )}

                    <div>
                        <label htmlFor="ag-notes" className="mb-1 block text-sm font-medium text-text-secondary">Notes</label>
                        <textarea id="ag-notes" rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} className="input w-full" placeholder="Contract reference, caveats, who negotiated it" />
                    </div>

                    {needsSignatory && (
                        <p role="alert" className="text-sm text-danger-500">
                            A signed BAA needs a signatory name — that is what makes it evidence.
                        </p>
                    )}
                </div>

                <div className="mt-6 flex justify-end gap-3">
                    <button onClick={onClose} className="btn btn-secondary" disabled={saving}>Cancel</button>
                    <button
                        className="btn btn-primary"
                        disabled={saving || needsSignatory}
                        onClick={() =>
                            onSave({
                                status,
                                documentVersion: documentVersion || null,
                                signatoryName: signatoryName || null,
                                signatoryTitle: signatoryTitle || null,
                                uptimeTargetPercent: isSla && uptime ? Number(uptime) : null,
                                notes: notes || null,
                            })
                        }
                    >
                        {saving ? 'Saving…' : 'Save'}
                    </button>
                </div>
            </div>
        </div>
    );
}
