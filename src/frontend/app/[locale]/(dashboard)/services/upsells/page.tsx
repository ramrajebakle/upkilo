"use client";

import React, { useState, useEffect, useCallback } from "react";
import { TrendingUp, Search, Zap, ArrowUpRight, Star, Package, Loader2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Service { id: string; name: string; price: number; durationMinutes: number; }
interface UpsellItem { id: string; name: string; description?: string; price: number; duration?: number; suggestedReason: string; priceDifference?: number; }

type TabKey = "addons" | "upgrades" | "recommendations";

export default function UpsellsPage() {
  const { error: toastError } = useToast();
  const [services, setServices] = useState<Service[]>([]);
  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [serviceSearch, setServiceSearch] = useState("");
  const [addons, setAddons] = useState<UpsellItem[]>([]);
  const [upgrades, setUpgrades] = useState<UpsellItem[]>([]);
  const [recommendations, setRecommendations] = useState<UpsellItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<TabKey>("addons");

  useEffect(() => {
    apiClient.get("/api/v1/services").catch(() => ({ data: [] })).then((r) => {
      setServices(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
    });
  }, []);

  const filteredServices = services.filter((s) =>
    s.name.toLowerCase().includes(serviceSearch.toLowerCase())
  );

  const loadUpsells = useCallback(async (service: Service) => {
    setLoading(true);
    try {
      const [addonRes, upgradeRes] = await Promise.all([
        apiClient.get(`/api/v1/upsell/service/${service.id}/addons`).catch(() => ({ data: { data: [] } })),
        apiClient.get(`/api/v1/upsell/booking/${service.id}/upgrades`).catch(() => ({ data: { upgrades: [] } })),
      ]);
      setAddons(addonRes.data?.data ?? []);
      setUpgrades(upgradeRes.data?.upgrades ?? upgradeRes.data?.data ?? []);
    } catch { toastError("Failed to load upsell data"); }
    finally { setLoading(false); }
  }, []);

  const handleSelectService = (s: Service) => {
    setSelectedService(s);
    setServiceSearch("");
    loadUpsells(s);
  };

  const TABS: { key: TabKey; label: string; icon: React.ReactNode; items: UpsellItem[] }[] = [
    { key: "addons", label: "Add-ons", icon: <Package size={14} />, items: addons },
    { key: "upgrades", label: "Upgrades", icon: <ArrowUpRight size={14} />, items: upgrades },
    { key: "recommendations", label: "Popular", icon: <Star size={14} />, items: recommendations },
  ];

  const UpsellCard = ({ item }: { item: UpsellItem }) => (
    <div className="p-4 rounded-xl border border-surface-200 hover:border-ai/25 hover:bg-ai-50/30 transition-all group">
      <div className="flex items-start justify-between gap-2 mb-1">
        <p className="font-medium text-text-primary group-hover:text-ai transition-colors">{item.name}</p>
        <span className="text-sm font-bold text-success-fg flex-shrink-0">${item.price}</span>
      </div>
      {item.description && <p className="text-xs text-text-secondary mb-2 line-clamp-2">{item.description}</p>}
      <div className="flex items-center justify-between">
        <span className="text-xs text-ai bg-ai-subtle px-2 py-0.5 rounded-full">{item.suggestedReason}</span>
        {item.duration && <span className="text-xs text-text-tertiary">{item.duration} min</span>}
        {item.priceDifference !== undefined && item.priceDifference > 0 && (
          <span className="text-xs text-warning-fg">+${item.priceDifference}</span>
        )}
      </div>
    </div>
  );

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Upsell Engine <TrendingUp className="text-text-tertiary" size={22} /></h1>
        <p className="text-text-secondary mt-1">Smart add-ons, upgrades, and recommendations to increase revenue per visit.</p>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Service selector */}
        <Card className="lg:col-span-1">
          <CardHeader><CardTitle className="text-sm">Select Service</CardTitle></CardHeader>
          <CardContent className="p-0">
            <div className="px-4 pb-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-text-tertiary" />
                <input value={serviceSearch} onChange={(e) => setServiceSearch(e.target.value)} placeholder="Search services…"
                  className="w-full pl-8 pr-3 py-1.5 text-xs rounded-md border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-1 focus:ring-ai-500" />
              </div>
            </div>
            <div className="max-h-96 overflow-y-auto">
              {filteredServices.map((s) => (
                <button key={s.id} onClick={() => handleSelectService(s)}
                  className={`w-full text-left px-4 py-2.5 transition-colors hover:bg-surface-50 ${selectedService?.id === s.id ? "bg-ai-subtle text-ai font-medium" : "text-text-primary"}`}>
                  <p className="text-sm">{s.name}</p>
                  <p className="text-xs text-text-tertiary">${s.price} · {s.durationMinutes} min</p>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Upsell content */}
        <div className="lg:col-span-3">
          {selectedService ? (
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <div>
                  <p className="font-semibold text-text-primary">{selectedService.name}</p>
                  <p className="text-xs text-text-tertiary">${selectedService.price} · {selectedService.durationMinutes} min</p>
                </div>
              </div>

              {/* Tabs */}
              <div className="flex gap-1 p-1 bg-surface-100 rounded-xl">
                {TABS.map((t) => (
                  <button key={t.key} onClick={() => setActiveTab(t.key)}
                    className={`flex-1 flex items-center justify-center gap-1.5 py-2 text-xs font-medium rounded-lg transition-colors ${activeTab === t.key ? "bg-card text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
                    {t.icon}{t.label}
                    <span className={`ml-0.5 text-xs px-1.5 py-0.5 rounded-full ${activeTab === t.key ? "bg-ai-subtle text-ai" : "bg-surface-200 text-text-tertiary"}`}>{t.items.length}</span>
                  </button>
                ))}
              </div>

              {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div>
                : (() => {
                  const current = TABS.find((t) => t.key === activeTab)!;
                  return current.items.length === 0 ? (
                    <Card><CardContent className="text-center py-10 text-text-tertiary">
                      <TrendingUp className="h-8 w-8 mx-auto mb-2 opacity-20" />
                      <p className="text-sm font-medium">No {current.label.toLowerCase()} found for this service</p>
                    </CardContent></Card>
                  ) : (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      {current.items.map((item) => <UpsellCard key={item.id} item={item} />)}
                    </div>
                  );
                })()}
            </div>
          ) : (
            <Card><CardContent className="text-center py-16 text-text-tertiary">
              <Zap className="h-12 w-12 mx-auto mb-3 opacity-20" />
              <p className="font-medium">Select a service to see upsell opportunities</p>
            </CardContent></Card>
          )}
        </div>
      </div>
    </div>
  );
}
