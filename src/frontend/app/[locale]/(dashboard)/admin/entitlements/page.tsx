"use client";

import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  ShieldCheck,
  Search,
  Lock,
  Unlock,
  Trash2,
  AlertTriangle,
  RefreshCw,
  Clock,
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { UNLIMITED } from "@/lib/featureKeys";

/**
 * Customer entitlement control centre.
 *
 * The backend could already grant, revoke, expire and audit customer-specific overrides, but
 * nothing drove it — which in practice means the capability does not exist, because the people
 * who need it (support, sales) do not have database access. This is the surface that makes the
 * override table usable, and the inspector that answers "why can this customer do that?".
 *
 * Read-only until a tenant is chosen; every mutation is confirmed and immediately re-read, so
 * what the operator sees is always the resolver's own answer rather than an optimistic guess.
 */

type Feature = {
  key: string;
  effective: boolean;
  limit: number;
  planValue: boolean;
  source: string;
  reason: string;
  overrideReason?: string | null;
  expiresAt?: string | null;
};

type Effective = {
  tenantId: string;
  tenantName: string;
  planName: string;
  subscriptionStatus: string;
  isServiceEntitled: boolean;
  currentPeriodEnd?: string | null;
  features: Feature[];
};

type CatalogEntry = {
  key: string;
  name: string;
  description: string;
  isNumeric: boolean;
  missingFromDatabase: boolean;
};

const SOURCE_TONE: Record<string, string> = {
  Override: "bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300",
  Plan: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300",
  PlanExcluded: "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400",
  SubscriptionInactive: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300",
  NoSubscription: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300",
};

function formatLimit(limit: number): string {
  if (limit === UNLIMITED) return "Unlimited";
  return String(limit);
}

export default function AdminEntitlementsPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();

  const [tenants, setTenants] = useState<any[]>([]);
  const [search, setSearch] = useState("");
  const [selectedTenantId, setSelectedTenantId] = useState<string | null>(null);

  const [catalog, setCatalog] = useState<CatalogEntry[]>([]);
  const [effective, setEffective] = useState<Effective | null>(null);
  const [unbounded, setUnbounded] = useState<any | null>(null);

  const [loading, setLoading] = useState(false);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isInitialized && user?.role !== "superadmin") router.push("/dashboard");
  }, [user, isInitialized, router]);

  const loadStatics = useCallback(async () => {
    try {
      const [cat, audit, tenantList] = await Promise.all([
        api.entitlementsAdmin.catalog(),
        api.entitlementsAdmin.unboundedGrants(),
        api.superAdmin.tenants({ pageSize: 200 }),
      ]);
      setCatalog(cat.data?.features ?? []);
      setUnbounded(audit.data ?? null);
      setTenants(tenantList.data?.data ?? tenantList.data?.items ?? tenantList.data ?? []);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to load entitlement catalogue");
    }
  }, []);

  useEffect(() => {
    if (user?.role === "superadmin") loadStatics();
  }, [user, loadStatics]);

  const loadTenant = useCallback(async (tenantId: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.entitlementsAdmin.effective(tenantId);
      setEffective(res.data);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to resolve entitlements");
      setEffective(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (selectedTenantId) loadTenant(selectedTenantId);
  }, [selectedTenantId, loadTenant]);

  const catalogByKey = useMemo(
    () => Object.fromEntries(catalog.map((c) => [c.key, c])),
    [catalog],
  );

  const filteredTenants = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return tenants.slice(0, 25);
    return tenants
      .filter((t: any) =>
        (t.name ?? t.businessName ?? "").toLowerCase().includes(q) ||
        (t.slug ?? "").toLowerCase().includes(q),
      )
      .slice(0, 25);
  }, [tenants, search]);

  async function setOverride(featureKey: string, isEnabled: boolean) {
    if (!selectedTenantId) return;

    const entry = catalogByKey[featureKey];
    const verb = isEnabled ? "Grant" : "Revoke";
    const reason = window.prompt(
      `${verb} "${entry?.name ?? featureKey}" for ${effective?.tenantName}.\n\n` +
        `Reason (recorded in the audit trail):`,
    );
    // Cancel returns null; an empty string is a deliberate "no reason given".
    if (reason === null) return;

    // An unbounded grant outranks the billing status forever, so the expiry is asked for
    // explicitly rather than defaulted to "never" by omission.
    let expiresAt: string | null = null;
    if (isEnabled) {
      const days = window.prompt(
        "Expire this grant after how many days?\n\n" +
          "Leave blank for a permanent grant — note that a permanent grant keeps serving the " +
          "feature even if the customer cancels.",
        "30",
      );
      if (days === null) return;
      const parsed = parseInt(days, 10);
      if (days.trim() !== "" && (!Number.isFinite(parsed) || parsed <= 0)) {
        window.alert("Enter a positive number of days, or leave blank for permanent.");
        return;
      }
      if (days.trim() !== "") {
        expiresAt = new Date(Date.now() + parsed * 86_400_000).toISOString();
      }
    }

    let numericLimit: number | null = null;
    if (isEnabled && entry?.isNumeric) {
      const raw = window.prompt(
        `"${entry.name}" is a numeric limit.\n\n` +
          `Enter the limit, -1 for unlimited, or leave blank to inherit the plan's limit.`,
        "",
      );
      if (raw === null) return;
      if (raw.trim() !== "") {
        const n = parseInt(raw, 10);
        if (!Number.isFinite(n) || n < -1) {
          window.alert("Enter a number >= -1, or leave blank to inherit the plan limit.");
          return;
        }
        numericLimit = n;
      }
    }

    setBusyKey(featureKey);
    try {
      await api.entitlementsAdmin.upsertOverride(selectedTenantId, featureKey, {
        isEnabled,
        numericLimit,
        expiresAt,
        reason: reason || null,
      });
      await Promise.all([loadTenant(selectedTenantId), loadStatics()]);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? `Failed to ${verb.toLowerCase()} ${featureKey}`);
    } finally {
      setBusyKey(null);
    }
  }

  async function clearOverride(featureKey: string) {
    if (!selectedTenantId) return;
    if (!window.confirm(`Remove the override for "${featureKey}"? It reverts to the plan default.`)) return;

    setBusyKey(featureKey);
    try {
      await api.entitlementsAdmin.deleteOverride(selectedTenantId, featureKey);
      await Promise.all([loadTenant(selectedTenantId), loadStatics()]);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to remove override");
    } finally {
      setBusyKey(null);
    }
  }

  if (user?.role !== "superadmin") return null;

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-violet-500 to-indigo-600 rounded-2xl shadow-lg shadow-violet-500/20">
              <ShieldCheck className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-2xl font-semibold text-slate-900 dark:text-white">
              Customer Entitlements
            </h1>
          </div>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            Grant or revoke features for a single customer without repricing their plan.
          </p>
        </div>
        <Button variant="secondary" onClick={loadStatics}>
          <RefreshCw className="h-4 w-4 me-2" />
          Refresh
        </Button>
      </div>

      {error && (
        <div className="flex items-start gap-3 p-4 rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
          <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-red-700 dark:text-red-300">{error}</p>
        </div>
      )}

      {/* Revenue audit: unbounded grants with no billing behind them. */}
      {unbounded && unbounded.unbilled > 0 && (
        <div className="p-4 rounded-xl bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800">
          <div className="flex items-center gap-2 mb-2">
            <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400" />
            <h2 className="font-semibold text-amber-900 dark:text-amber-200">
              {unbounded.unbilled} permanent grant{unbounded.unbilled === 1 ? "" : "s"} with no active subscription
            </h2>
          </div>
          <p className="text-sm text-amber-800 dark:text-amber-300 mb-3">
            These customers receive a paid feature with no billing behind it. An override outranks
            the subscription status by design, so nothing will expire these automatically.
          </p>
          <div className="space-y-1">
            {unbounded.grants
              .filter((g: any) => g.unbilled)
              .slice(0, 8)
              .map((g: any, i: number) => (
                <button
                  key={`${g.tenantId}-${g.featureKey}-${i}`}
                  onClick={() => setSelectedTenantId(g.tenantId)}
                  className="w-full text-start text-sm px-3 py-2 rounded-lg bg-white/60 dark:bg-slate-900/40 hover:bg-white dark:hover:bg-slate-900 transition-colors"
                >
                  <span className="font-medium text-slate-900 dark:text-white">{g.tenantName}</span>
                  <span className="text-slate-500 dark:text-slate-400">
                    {" "}— {g.featureKey} · {g.subscriptionStatus} · {g.ageDays}d old
                  </span>
                </button>
              ))}
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Tenant picker */}
        <div className="lg:col-span-1 space-y-3">
          <div className="relative">
            <Search className="absolute start-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search customers…"
              className="w-full ps-9 pe-3 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-sm text-slate-900 dark:text-white"
            />
          </div>
          <div className="rounded-xl border border-slate-200 dark:border-slate-700 divide-y divide-slate-100 dark:divide-slate-800 max-h-[28rem] overflow-y-auto">
            {filteredTenants.length === 0 && (
              <p className="p-4 text-sm text-slate-500 dark:text-slate-400">No customers found.</p>
            )}
            {filteredTenants.map((t: any) => (
              <button
                key={t.id}
                onClick={() => setSelectedTenantId(t.id)}
                className={`w-full text-start px-4 py-3 text-sm transition-colors ${
                  selectedTenantId === t.id
                    ? "bg-violet-50 dark:bg-violet-900/20 text-violet-700 dark:text-violet-300"
                    : "hover:bg-slate-50 dark:hover:bg-slate-800/50 text-slate-700 dark:text-slate-300"
                }`}
              >
                <div className="font-medium">{t.name ?? t.businessName ?? t.slug}</div>
                {t.slug && <div className="text-xs text-slate-400">{t.slug}</div>}
              </button>
            ))}
          </div>
        </div>

        {/* Inspector */}
        <div className="lg:col-span-2">
          {!selectedTenantId && (
            <div className="rounded-xl border border-dashed border-slate-300 dark:border-slate-700 p-12 text-center">
              <ShieldCheck className="h-10 w-10 mx-auto text-foreground-muted mb-3" />
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Select a customer to inspect their effective entitlements.
              </p>
            </div>
          )}

          {loading && (
            <div className="p-12 text-center text-sm text-slate-500">Resolving entitlements…</div>
          )}

          {!loading && effective && (
            <div className="space-y-4">
              <div className="rounded-xl border border-slate-200 dark:border-slate-700 p-4">
                <div className="flex flex-wrap items-center gap-3">
                  <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
                    {effective.tenantName}
                  </h2>
                  <Badge>{effective.planName || "No plan"}</Badge>
                  <span
                    className={`text-xs px-2 py-1 rounded-full ${
                      effective.isServiceEntitled
                        ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300"
                        : "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300"
                    }`}
                  >
                    {effective.subscriptionStatus}
                    {!effective.isServiceEntitled && " — not entitled to service"}
                  </span>
                </div>
              </div>

              <div className="rounded-xl border border-slate-200 dark:border-slate-700 overflow-hidden">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-slate-50 dark:bg-slate-800/50 text-slate-500 dark:text-slate-400">
                      <tr>
                        <th className="text-start font-medium px-4 py-3">Feature</th>
                        <th className="text-start font-medium px-4 py-3">Plan</th>
                        <th className="text-start font-medium px-4 py-3">Effective</th>
                        <th className="text-start font-medium px-4 py-3">Why</th>
                        <th className="text-end font-medium px-4 py-3">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                      {effective.features.map((f) => {
                        const entry = catalogByKey[f.key];
                        const isOverride = f.source === "Override";
                        const busy = busyKey === f.key;
                        return (
                          <tr key={f.key} className="text-slate-700 dark:text-slate-300">
                            <td className="px-4 py-3">
                              <div className="font-medium text-slate-900 dark:text-white">
                                {entry?.name ?? f.key}
                              </div>
                              <div className="text-xs text-slate-400 font-mono">{f.key}</div>
                              {entry?.missingFromDatabase && (
                                <div className="text-xs text-red-500 mt-1">
                                  Not in database — this gate always denies
                                </div>
                              )}
                            </td>
                            <td className="px-4 py-3 text-slate-500">
                              {f.planValue ? "Included" : "—"}
                            </td>
                            <td className="px-4 py-3">
                              <div className="flex items-center gap-2">
                                {f.effective ? (
                                  <Unlock className="h-4 w-4 text-emerald-500" />
                                ) : (
                                  <Lock className="h-4 w-4 text-slate-400" />
                                )}
                                <span>{f.effective ? "Enabled" : "Disabled"}</span>
                                {f.effective && entry?.isNumeric && (
                                  <span className="text-xs text-slate-400">
                                    ({formatLimit(f.limit)})
                                  </span>
                                )}
                              </div>
                            </td>
                            <td className="px-4 py-3">
                              <span
                                className={`text-xs px-2 py-1 rounded-full ${
                                  SOURCE_TONE[f.source] ?? SOURCE_TONE.PlanExcluded
                                }`}
                              >
                                {f.reason}
                              </span>
                              {f.overrideReason && (
                                <div className="text-xs text-slate-400 mt-1">“{f.overrideReason}”</div>
                              )}
                              {isOverride && (
                                <div className="text-xs text-slate-400 mt-1 flex items-center gap-1">
                                  <Clock className="h-3 w-3" />
                                  {f.expiresAt
                                    ? `Expires ${new Date(f.expiresAt).toLocaleDateString()}`
                                    : "Permanent"}
                                </div>
                              )}
                            </td>
                            <td className="px-4 py-3">
                              <div className="flex items-center justify-end gap-2">
                                {!f.effective && (
                                  <Button size="sm" variant="secondary" disabled={busy}
                                    onClick={() => setOverride(f.key, true)}>
                                    Grant
                                  </Button>
                                )}
                                {f.effective && (
                                  <Button size="sm" variant="secondary" disabled={busy}
                                    onClick={() => setOverride(f.key, false)}>
                                    Revoke
                                  </Button>
                                )}
                                {isOverride && (
                                  <Button size="sm" variant="ghost" disabled={busy}
                                    title="Remove override, revert to plan default"
                                    onClick={() => clearOverride(f.key)}>
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                )}
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
