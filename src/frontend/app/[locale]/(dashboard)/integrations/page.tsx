"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Search, Loader2 } from "lucide-react";
import { Input } from "@/components/ui/Input";
import { useToast } from "@/components/ui/Toast";
import { apiClient } from "@/lib/api";
import { IntegrationCard, IntegrationWrapper, IntegrationItem } from "@/components/integrations/IntegrationCard";
import { IntegrationCredentialModal } from "@/components/integrations/IntegrationCredentialModal";

const CATEGORIES = ["all", "payment", "email", "sms", "calendar", "storage", "analytics", "crm", "notifications", "automation"];

export default function IntegrationsPage() {
  const { success, error: toastError } = useToast();

  const [loading, setLoading] = useState(true);
  const [integrations, setIntegrations] = useState<IntegrationWrapper[]>([]);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("all");

  // Per-card loading states
  const [connectingId, setConnectingId] = useState<string | null>(null);
  const [testingId, setTestingId] = useState<string | null>(null);

  // Credential modal state
  const [modalTarget, setModalTarget] = useState<IntegrationItem | null>(null);

  const fetchIntegrations = useCallback(async () => {
    try {
      const res = await apiClient.get("/api/v1/integrations");
      if (res.data?.data) setIntegrations(res.data.data as IntegrationWrapper[]);
    } catch {
      toastError("Failed to load integrations.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchIntegrations(); }, [fetchIntegrations]);

  const handleConnect = (id: string) => {
    const wrapper = integrations.find((i) => i.item.id === id);
    if (!wrapper) return;
    // OAuth integrations redirect; all others open the credential modal
    if (wrapper.item.authType === "oauth") {
      window.location.href = `/api/v1/auth/oauth/${id}`;
      return;
    }
    setModalTarget(wrapper.item);
  };

  const handleSaveCredentials = async (id: string, credentials: Record<string, string>) => {
    setConnectingId(id);
    try {
      await apiClient.post(`/api/v1/integrations/${id}/connect`, { credentials });
      success(`${id} connected. Run a test to verify.`);
      await fetchIntegrations();
    } finally {
      setConnectingId(null);
    }
  };

  const handleDisconnect = async (id: string) => {
    setConnectingId(id);
    try {
      await apiClient.post(`/api/v1/integrations/${id}/disconnect`);
      success("Integration disconnected.");
      await fetchIntegrations();
    } catch {
      toastError("Failed to disconnect.");
    } finally {
      setConnectingId(null);
    }
  };

  const handleTest = async (id: string) => {
    setTestingId(id);
    try {
      const res = await apiClient.post(`/api/v1/integrations/${id}/test`);
      if (res.data?.success) {
        success(res.data.message ?? "Connection verified.");
      } else {
        toastError(res.data?.message ?? "Verification failed.");
      }
      await fetchIntegrations();
    } catch {
      toastError("Test request failed.");
    } finally {
      setTestingId(null);
    }
  };

  const handleManage = (id: string) => {
    const wrapper = integrations.find((i) => i.item.id === id);
    if (wrapper) setModalTarget(wrapper.item);
  };

  const filtered = integrations.filter((w) => {
    const matchSearch =
      w.item.name.toLowerCase().includes(search.toLowerCase()) ||
      w.item.category.toLowerCase().includes(search.toLowerCase()) ||
      w.item.description.toLowerCase().includes(search.toLowerCase());
    const matchCat = category === "all" || w.item.category === category;
    return matchSearch && matchCat;
  });

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center py-20">
        <Loader2 className="h-10 w-10 text-primary animate-spin mb-4" />
        <p className="text-foreground-secondary">Loading integrations…</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-6xl">
      {/* Header */}
      <div className="flex justify-between items-start flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Integration Center</h1>
          <p className="text-muted-foreground">
            Connect Upkilo with the tools you already use. All credentials are encrypted at rest.
          </p>
        </div>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-foreground-muted" />
          <Input
            placeholder="Search integrations…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 w-64"
          />
        </div>
      </div>

      {/* Category filter */}
      <div className="flex gap-2 flex-wrap">
        {CATEGORIES.map((cat) => (
          <button
            key={cat}
            onClick={() => setCategory(cat)}
            className={`text-xs px-3 py-1.5 rounded-full font-medium transition-colors ${
              category === cat
                ? "bg-gray-900 text-white"
                : "bg-muted text-foreground-secondary hover:bg-gray-200"
            }`}
          >
            {cat.charAt(0).toUpperCase() + cat.slice(1)}
          </button>
        ))}
      </div>

      {/* Stats bar */}
      <div className="flex gap-6 text-sm text-foreground-secondary border-b pb-4">
        <span><strong className="text-foreground">{integrations.filter(i => i.isConnected).length}</strong> connected</span>
        <span><strong className="text-foreground">{integrations.filter(i => i.isVerified).length}</strong> verified</span>
        <span><strong className="text-foreground">{filtered.length}</strong> shown</span>
      </div>

      {/* Grid */}
      {filtered.length === 0 ? (
        <div className="text-center py-12 text-foreground-muted">
          <p className="text-lg font-medium">No integrations found</p>
          <p className="text-sm">Try a different search or category.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filtered.map((wrapper) => (
            <IntegrationCard
              key={wrapper.item.id}
              integration={wrapper}
              onConnect={handleConnect}
              onDisconnect={handleDisconnect}
              onTest={handleTest}
              onManage={handleManage}
              loading={connectingId === wrapper.item.id}
              testing={testingId === wrapper.item.id}
            />
          ))}
        </div>
      )}

      {/* Credential modal */}
      <IntegrationCredentialModal
        integration={modalTarget}
        isOpen={!!modalTarget}
        onClose={() => setModalTarget(null)}
        onSave={handleSaveCredentials}
      />
    </div>
  );
}
