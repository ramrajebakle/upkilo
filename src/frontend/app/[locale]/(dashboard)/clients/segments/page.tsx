"use client";

import React, { useState, useCallback } from "react";
import {
  Users, Filter, Play, Save, Search, RefreshCw, ChevronRight,
  DollarSign, Calendar, Tag, Crown, Loader2, Download,
} from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

interface SegmentFilters {
  minSpend: string;
  minDaysSinceLastVisit: string;
  maxDaysSinceLastVisit: string;
  tags: string;
  loyaltyTier: string;
}

interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  totalSpend?: number;
  lastVisitDate?: string;
  loyaltyTier?: string;
  tags?: string[];
}

const PRESET_SEGMENTS = [
  {
    name: "At Risk (Lapsed)",
    description: "Haven't visited in 30–90 days",
    filters: { minSpend: "", minDaysSinceLastVisit: "30", maxDaysSinceLastVisit: "90", tags: "", loyaltyTier: "" },
    color: "text-danger-fg",
    bg: "bg-red-50",
  },
  {
    name: "High Value",
    description: "Lifetime spend over $500",
    filters: { minSpend: "500", minDaysSinceLastVisit: "", maxDaysSinceLastVisit: "", tags: "", loyaltyTier: "" },
    color: "text-success-fg",
    bg: "bg-green-50",
  },
  {
    name: "VIP Members",
    description: "Gold or Platinum loyalty tier",
    filters: { minSpend: "", minDaysSinceLastVisit: "", maxDaysSinceLastVisit: "", tags: "", loyaltyTier: "Gold" },
    color: "text-warning-fg",
    bg: "bg-amber-50",
  },
  {
    name: "Win-Back",
    description: "Not visited in over 90 days",
    filters: { minSpend: "", minDaysSinceLastVisit: "90", maxDaysSinceLastVisit: "", tags: "", loyaltyTier: "" },
    color: "text-purple-600",
    bg: "bg-purple-50",
  },
];

const LOYALTY_TIERS = ["Bronze", "Silver", "Gold", "Platinum"];

function TagInput({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const [input, setInput] = useState("");
  const tags = value ? value.split(",").filter(Boolean) : [];

  const add = () => {
    const trimmed = input.trim();
    if (trimmed && !tags.includes(trimmed)) {
      onChange([...tags, trimmed].join(","));
    }
    setInput("");
  };

  const remove = (tag: string) => {
    onChange(tags.filter((t) => t !== tag).join(","));
  };

  return (
    <div className="border border-surface-200 rounded-lg p-2 bg-surface-50 flex flex-wrap gap-1.5 min-h-[42px]">
      {tags.map((t) => (
        <span key={t} className="inline-flex items-center gap-1 bg-ai-subtle text-ai text-xs px-2 py-0.5 rounded-full">
          {t}
          <button onClick={() => remove(t)} className="hover:text-ai-900 font-bold">×</button>
        </span>
      ))}
      <input
        type="text"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onKeyDown={(e) => e.key === "Enter" && (e.preventDefault(), add())}
        placeholder="Add tag…"
        className="flex-1 min-w-20 bg-transparent text-sm text-text-primary focus:outline-none placeholder:text-text-tertiary"
      />
    </div>
  );
}

export default function ClientSegmentsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [filters, setFilters] = useState<SegmentFilters>({
    minSpend: "",
    minDaysSinceLastVisit: "",
    maxDaysSinceLastVisit: "",
    tags: "",
    loyaltyTier: "",
  });
  const [results, setResults] = useState<Client[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [clientSearch, setClientSearch] = useState("");

  const setFilter = (key: keyof SegmentFilters, value: string) =>
    setFilters((f) => ({ ...f, [key]: value }));

  const applyPreset = (preset: (typeof PRESET_SEGMENTS)[0]) => {
    setFilters(preset.filters);
    setResults(null);
  };

  const runSegment = useCallback(async () => {
    setLoading(true);
    try {
      const body: Record<string, unknown> = {};
      if (filters.minSpend) body.minSpend = parseFloat(filters.minSpend);
      if (filters.minDaysSinceLastVisit) body.minDaysSinceLastVisit = parseInt(filters.minDaysSinceLastVisit);
      if (filters.maxDaysSinceLastVisit) body.maxDaysSinceLastVisit = parseInt(filters.maxDaysSinceLastVisit);
      if (filters.tags) body.tags = filters.tags.split(",").filter(Boolean);
      if (filters.loyaltyTier) body.loyaltyTier = filters.loyaltyTier;

      const res = await apiClient.post("/api/v1/clients/segment", body);
      const data = res.data?.data ?? res.data ?? [];
      setResults(Array.isArray(data) ? data : []);
      toastSuccess(`Found ${Array.isArray(data) ? data.length : 0} matching clients`);
    } catch {
      toastError("Failed to run segment query");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  const filtered = results?.filter((c) => {
    if (!clientSearch) return true;
    const name = `${c.firstName} ${c.lastName}`.toLowerCase();
    return name.includes(clientSearch.toLowerCase()) || c.email?.includes(clientSearch.toLowerCase());
  });

  const hasFilters = Object.values(filters).some(Boolean);

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">
          Client Segments
          <Users className="text-ai" size={24} />
        </h1>
        <p className="text-text-secondary mt-1">
          Filter and group clients for targeted marketing campaigns.
        </p>
      </header>

      {/* Presets */}
      <section>
        <p className="text-xs font-semibold uppercase tracking-wider text-text-tertiary mb-3">Quick Presets</p>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {PRESET_SEGMENTS.map((p) => (
            <button
              key={p.name}
              onClick={() => applyPreset(p)}
              className={cn(
                "text-left p-4 rounded-xl border border-surface-200 hover:shadow-sm transition-all group",
                p.bg
              )}
            >
              <p className={cn("font-semibold text-sm", p.color)}>{p.name}</p>
              <p className="text-xs text-text-secondary mt-0.5">{p.description}</p>
            </button>
          ))}
        </div>
      </section>

      {/* Filter builder */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Filter className="h-4 w-4" /> Segment Filters
            </CardTitle>
            <CardDescription>Combine filters — all conditions apply (AND logic)</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Min spend */}
            <div>
              <label className="flex items-center gap-1.5 text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                <DollarSign className="h-3 w-3" /> Min Lifetime Spend
              </label>
              <div className="relative">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary text-sm">$</span>
                <input
                  type="number"
                  min="0"
                  value={filters.minSpend}
                  onChange={(e) => setFilter("minSpend", e.target.value)}
                  placeholder="e.g. 500"
                  className="w-full pl-7 pr-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
                />
              </div>
            </div>

            {/* Days since last visit */}
            <div>
              <label className="flex items-center gap-1.5 text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                <Calendar className="h-3 w-3" /> Days Since Last Visit
              </label>
              <div className="flex gap-2 items-center">
                <input
                  type="number"
                  min="0"
                  value={filters.minDaysSinceLastVisit}
                  onChange={(e) => setFilter("minDaysSinceLastVisit", e.target.value)}
                  placeholder="Min"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
                />
                <span className="text-text-tertiary text-xs flex-shrink-0">to</span>
                <input
                  type="number"
                  min="0"
                  value={filters.maxDaysSinceLastVisit}
                  onChange={(e) => setFilter("maxDaysSinceLastVisit", e.target.value)}
                  placeholder="Max"
                  className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
                />
              </div>
            </div>

            {/* Tags */}
            <div>
              <label className="flex items-center gap-1.5 text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                <Tag className="h-3 w-3" /> Client Tags
              </label>
              <TagInput
                value={filters.tags}
                onChange={(v) => setFilter("tags", v)}
              />
              <p className="text-xs text-text-tertiary mt-1">Press Enter to add a tag</p>
            </div>

            {/* Loyalty tier */}
            <div>
              <label className="flex items-center gap-1.5 text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                <Crown className="h-3 w-3" /> Loyalty Tier
              </label>
              <select
                value={filters.loyaltyTier}
                onChange={(e) => setFilter("loyaltyTier", e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500"
              >
                <option value="">Any tier</option>
                {LOYALTY_TIERS.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>

            <div className="flex gap-2 pt-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => { setFilters({ minSpend: "", minDaysSinceLastVisit: "", maxDaysSinceLastVisit: "", tags: "", loyaltyTier: "" }); setResults(null); }}
                disabled={!hasFilters}
              >
                <RefreshCw className="h-3.5 w-3.5" />
              </Button>
              <Button
                variant="primary"
                className="flex-1"
                leftIcon={loading ? <Loader2 size={15} className="animate-spin" /> : <Play size={15} />}
                onClick={runSegment}
                disabled={loading || !hasFilters}
              >
                {loading ? "Running…" : "Run Segment"}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Results */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  {results === null ? "Results" : `${results.length} Clients`}
                </CardTitle>
                <CardDescription>
                  {results === null ? "Run a segment to see matching clients" : "Matching your current filters"}
                </CardDescription>
              </div>
              {results && results.length > 0 && (
                <Button variant="outline" size="sm" leftIcon={<Download size={14} />}>
                  Export
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            {results === null ? (
              <div className="text-center py-16 text-text-tertiary">
                <Users className="h-12 w-12 mx-auto mb-3 opacity-20" />
                <p className="font-medium">Set filters and click Run Segment</p>
                <p className="text-sm mt-1">Your matched clients will appear here</p>
              </div>
            ) : results.length === 0 ? (
              <div className="text-center py-16 text-text-tertiary">
                <Search className="h-12 w-12 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No clients match these filters</p>
                <p className="text-sm mt-1">Try relaxing the criteria</p>
              </div>
            ) : (
              <>
                <div className="relative mb-3">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
                  <input
                    type="text"
                    placeholder="Filter results…"
                    value={clientSearch}
                    onChange={(e) => setClientSearch(e.target.value)}
                    className="w-full pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 focus:outline-none focus:ring-2 focus:ring-ai-500"
                  />
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-surface-200">
                        <th className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">Client</th>
                        <th className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">Tier</th>
                        <th className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">Last Visit</th>
                        <th className="text-left py-2.5 px-3 text-xs font-semibold text-text-tertiary uppercase">Spend</th>
                        <th className="py-2.5 px-3" />
                      </tr>
                    </thead>
                    <tbody>
                      {(filtered ?? results).slice(0, 50).map((c) => (
                        <tr key={c.id} className="border-b border-surface-100 hover:bg-surface-50 transition-colors">
                          <td className="py-2.5 px-3">
                            <p className="font-medium text-text-primary">{c.firstName} {c.lastName}</p>
                            <p className="text-xs text-text-tertiary">{c.email ?? c.phone}</p>
                          </td>
                          <td className="py-2.5 px-3">
                            {c.loyaltyTier ? (
                              <span className="text-xs font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full">
                                {c.loyaltyTier}
                              </span>
                            ) : <span className="text-text-tertiary">—</span>}
                          </td>
                          <td className="py-2.5 px-3 text-text-secondary text-xs">
                            {c.lastVisitDate
                              ? new Date(c.lastVisitDate).toLocaleDateString([], { month: "short", day: "numeric" })
                              : "—"}
                          </td>
                          <td className="py-2.5 px-3 font-medium text-text-primary">
                            {c.totalSpend !== undefined ? `$${c.totalSpend.toFixed(0)}` : "—"}
                          </td>
                          <td className="py-2.5 px-3">
                            <a href={`/clients/${c.id}`} className="text-ai hover:text-ai">
                              <ChevronRight className="h-4 w-4" />
                            </a>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  {(filtered ?? results).length > 50 && (
                    <p className="text-center text-xs text-text-tertiary py-3">
                      Showing first 50 of {(filtered ?? results).length} results
                    </p>
                  )}
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
