"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Home, Users, Search, ChevronRight, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Household {
  id: string;
  name?: string;
  members: { clientId: string; clientName: string; relationship?: string; isPrimary: boolean }[];
  primaryClientName?: string;
  createdAt?: string;
}

export default function HouseholdsPage() {
  const { error: toastError } = useToast();
  const [households, setHouseholds] = useState<Household[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const r = await apiClient.get("/api/v1/households").catch(() => ({ data: [] }));
      const d: Household[] = Array.isArray(r.data) ? r.data : r.data?.data ?? [];
      setHouseholds(d);
    } catch { toastError("Failed to load households"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const filtered = households.filter((h) =>
    !search ||
    h.name?.toLowerCase().includes(search.toLowerCase()) ||
    h.primaryClientName?.toLowerCase().includes(search.toLowerCase()) ||
    h.members.some((m) => m.clientName.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Households <Home className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Family and household groupings of clients.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
        <input type="text" placeholder="Search households…" value={search} onChange={(e) => setSearch(e.target.value)}
          className="w-full sm:w-80 pl-9 pr-4 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
      </div>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
        : filtered.length === 0 ? (
          <Card><CardContent className="text-center py-12 text-text-tertiary">
            <Home className="h-10 w-10 mx-auto mb-3 opacity-20" />
            <p className="font-medium">{search ? "No households match your search" : "No households found"}</p>
            <p className="text-sm mt-1">Households are created when clients are linked as family members.</p>
          </CardContent></Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((h) => (
              <Card key={h.id}>
                <CardContent className="pt-4 pb-4">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 rounded-lg bg-surface-100 flex items-center justify-center flex-shrink-0">
                      <Home className="h-5 w-5 text-text-tertiary" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-text-primary">{h.name ?? `${h.primaryClientName ?? "Household"} Family`}</p>
                      <p className="text-sm text-text-secondary mb-2">{h.members.length} member{h.members.length !== 1 ? "s" : ""}</p>
                      <div className="flex flex-wrap gap-2">
                        {h.members.map((m) => (
                          <a key={m.clientId} href={`/clients/${m.clientId}`}
                            className="inline-flex items-center gap-1 text-xs bg-surface-100 hover:bg-surface-200 text-text-secondary px-2.5 py-1 rounded-full transition-colors">
                            {m.isPrimary && <span className="w-1.5 h-1.5 rounded-full bg-ai-500 flex-shrink-0" />}
                            {m.clientName}
                            {m.relationship && <span className="text-text-tertiary">· {m.relationship}</span>}
                          </a>
                        ))}
                      </div>
                    </div>
                    <ChevronRight className="h-4 w-4 text-text-tertiary flex-shrink-0 mt-1" />
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
    </div>
  );
}
