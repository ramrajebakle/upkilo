'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { Briefcase, Mail, Phone, RefreshCw, Users } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { cn, formatDate } from '@/lib/utils';
import { ErrorState, EmptyState, SkeletonTable, PageHeader } from '@/components/ui';
import { toast } from 'sonner';
import {
    useLeads,
    useUpdateLeadStatus,
    LEAD_STATUSES,
    type LeadStatus,
} from '@/lib/query/leads';

const STATUS_STYLES: Record<LeadStatus, string> = {
    New: 'bg-primary-50 text-primary-700 dark:bg-primary-500/10 dark:text-primary-300',
    Contacted: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
    Qualified: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
    Closed: 'bg-surface-100 text-text-secondary',
};

export default function AdminLeadsPage() {
    const { user, isInitialized } = useAuthStore();
    const { data: session, status: sessionStatus } = useSession();
    const router = useRouter();
    const [filter, setFilter] = useState<LeadStatus | 'All'>('All');

    // Both role vocabularies, for the same reason as the dashboard shell: authStore is
    // filled by the real login call, the session by any sign-in. Gating on one alone
    // locks out whichever path did not populate it.
    const sessionRole = session?.user?.role;
    const isPlatformStaff =
        user?.role === 'superadmin' ||
        sessionRole === 'platform_owner' ||
        sessionRole === 'platform_admin';

    // Wait for both sources to settle before deciding. Redirecting while either is
    // still loading would bounce a legitimate admin on every refresh.
    const resolved = isInitialized && sessionStatus !== 'loading';

    if (resolved && !isPlatformStaff) {
        router.push('/dashboard');
        return null;
    }

    return <LeadsView enabled={isPlatformStaff} filter={filter} setFilter={setFilter} />;
}

function LeadsView({
    enabled,
    filter,
    setFilter,
}: {
    enabled: boolean;
    filter: LeadStatus | 'All';
    setFilter: (s: LeadStatus | 'All') => void;
}) {
    const { data: leads = [], isPending, isError, error, refetch, isFetching } = useLeads();
    const updateStatus = useUpdateLeadStatus();

    const counts = useMemo(() => {
        const c: Record<string, number> = { All: leads.length };
        for (const s of LEAD_STATUSES) c[s] = leads.filter((l) => l.status === s).length;
        return c;
    }, [leads]);

    const visible = filter === 'All' ? leads : leads.filter((l) => l.status === filter);

    const move = async (id: string, status: LeadStatus, company: string) => {
        try {
            await updateStatus.mutateAsync({ id, status });
            toast.success(`${company} moved to ${status}`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Could not update this lead');
        }
    };

    if (!enabled) return null;

    return (
        <div className="space-y-6 max-w-7xl mx-auto pb-12">
            <PageHeader
                title="Enterprise leads"
                description="Submissions from the Contact sales form on /enterprise."
                icon={Briefcase}
                iconGradient="from-blue-500 to-primary-600"
                iconShadow="shadow-blue-500/25"
                actions={
                    <button onClick={() => refetch()} className="btn btn-secondary" disabled={isFetching}>
                        <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} aria-hidden="true" />
                        Refresh
                    </button>
                }
            />

            <div className="flex flex-wrap gap-2" role="group" aria-label="Filter leads by stage">
                {(['All', ...LEAD_STATUSES] as const).map((s) => (
                    <button
                        key={s}
                        onClick={() => setFilter(s)}
                        aria-pressed={filter === s}
                        className={cn(
                            'px-4 py-2 rounded-lg text-sm font-semibold transition-all',
                            filter === s
                                ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                : 'bg-surface-50 text-text-secondary border border-surface-200 hover:border-primary-300',
                        )}
                    >
                        {s}
                        <span className="ms-2 tabular-nums opacity-70">{counts[s] ?? 0}</span>
                    </button>
                ))}
            </div>

            {isError ? (
                <ErrorState
                    title="Couldn&rsquo;t load enterprise leads"
                    error={error}
                    onRetry={() => refetch()}
                    isRetrying={isFetching}
                />
            ) : isPending ? (
                <SkeletonTable rows={6} cols={5} />
            ) : visible.length === 0 ? (
                <EmptyState
                    icon={Users}
                    title={filter === 'All' ? 'No enterprise leads yet' : `No leads in ${filter}`}
                    description={
                        filter === 'All'
                            ? 'Submissions from the Contact sales form will appear here.'
                            : 'Try a different stage.'
                    }
                />
            ) : (
                <div className="space-y-3">
                    {visible.map((lead) => (
                        <article key={lead.id} className="card-elevated p-5">
                            <div className="flex flex-wrap items-start justify-between gap-4">
                                <div className="min-w-0 flex-1">
                                    <div className="flex flex-wrap items-center gap-2">
                                        <h3 className="font-bold text-text-primary">{lead.companyName}</h3>
                                        <span
                                            className={cn(
                                                'rounded-full px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider',
                                                STATUS_STYLES[lead.status] ?? STATUS_STYLES.Closed,
                                            )}
                                        >
                                            {lead.status}
                                        </span>
                                        {lead.teamSize && (
                                            <span className="text-xs text-text-tertiary">{lead.teamSize}</span>
                                        )}
                                    </div>

                                    <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-text-secondary">
                                        {lead.contactName && <span>{lead.contactName}</span>}
                                        <a
                                            href={`mailto:${lead.email}`}
                                            className="inline-flex items-center gap-1.5 hover:text-primary-600"
                                        >
                                            <Mail className="h-3.5 w-3.5" aria-hidden="true" />
                                            {lead.email}
                                        </a>
                                        {lead.phone && (
                                            <a
                                                href={`tel:${lead.phone}`}
                                                className="inline-flex items-center gap-1.5 hover:text-primary-600"
                                            >
                                                <Phone className="h-3.5 w-3.5" aria-hidden="true" />
                                                {lead.phone}
                                            </a>
                                        )}
                                    </div>

                                    {(lead.useCase || lead.currentPlatform) && (
                                        <p className="mt-2 text-sm text-text-secondary">
                                            {lead.useCase}
                                            {lead.useCase && lead.currentPlatform ? ' · ' : ''}
                                            {lead.currentPlatform && `Currently on ${lead.currentPlatform}`}
                                        </p>
                                    )}

                                    {lead.message && (
                                        <p className="mt-2 rounded-lg bg-surface-100 p-3 text-sm text-text-secondary">
                                            {lead.message}
                                        </p>
                                    )}
                                </div>

                                <div className="flex flex-col items-end gap-2">
                                    <span className="text-xs text-text-tertiary">{formatDate(lead.createdAt)}</span>
                                    <label className="sr-only" htmlFor={`status-${lead.id}`}>
                                        Stage for {lead.companyName}
                                    </label>
                                    <select
                                        id={`status-${lead.id}`}
                                        value={lead.status}
                                        disabled={updateStatus.isPending}
                                        onChange={(e) => move(lead.id, e.target.value as LeadStatus, lead.companyName)}
                                        className="input py-1.5 text-sm"
                                    >
                                        {LEAD_STATUSES.map((s) => (
                                            <option key={s} value={s}>
                                                {s}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                            </div>
                        </article>
                    ))}
                </div>
            )}
        </div>
    );
}
