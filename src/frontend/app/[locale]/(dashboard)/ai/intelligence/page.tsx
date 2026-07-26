"use client";

import React, { useState, useEffect } from "react";
import { Brain, TrendingUp, AlertCircle, BarChart3, Target, Users, Loader2, RefreshCw, DollarSign } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Forecast { date: string; predictedDemand: number; confidence: number; }
interface PriceOpt { serviceId: string; serviceName: string; currentPrice: number; recommendedPrice: number; expectedRevenueIncrease: number; }
interface NoShowRisk { clientId: string; clientName: string; riskScore: number; nextBookingDate?: string; }
interface CompetitorReport { competitor: string; strengths: string[]; weaknesses: string[]; updatedAt: string; }
interface Benchmark { metric: string; yourValue: number; industryAvg: number; topQuartile: number; }
interface Projection { month: string; revenue: number; bookings: number; clients: number; }

type Tab = "demand" | "pricing" | "noshow" | "competitor" | "benchmarks" | "projections";

export default function IntelligencePage() {
  const { error: toastError } = useToast();
  const [tab, setTab] = useState<Tab>("demand");
  const [loading, setLoading] = useState(false);
  const [demand, setDemand] = useState<Forecast[]>([]);
  const [priceOpts, setPriceOpts] = useState<PriceOpt[]>([]);
  const [noShow, setNoShow] = useState<NoShowRisk[]>([]);
  const [competitor, setCompetitor] = useState<CompetitorReport[]>([]);
  const [benchmarks, setBenchmarks] = useState<Benchmark[]>([]);
  const [projections, setProjections] = useState<Projection[]>([]);

  const loadTab = async (t: Tab) => {
    setLoading(true);
    try {
      switch (t) {
        case "demand": {
          const r = await apiClient.get("/api/v1/intelligence/demand-forecast").catch(() => ({ data: [] }));
          setDemand(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
        case "pricing": {
          const r = await apiClient.get("/api/v1/intelligence/price-optimization").catch(() => ({ data: [] }));
          setPriceOpts(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
        case "noshow": {
          const r = await apiClient.get("/api/v1/intelligence/no-show-risk").catch(() => ({ data: [] }));
          setNoShow(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
        case "competitor": {
          const r = await apiClient.get("/api/v1/intelligence/competitor-report").catch(() => ({ data: [] }));
          setCompetitor(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
        case "benchmarks": {
          const r = await apiClient.get("/api/v1/intelligence/benchmarks").catch(() => ({ data: [] }));
          setBenchmarks(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
        case "projections": {
          const r = await apiClient.get("/api/v1/projections").catch(() => ({ data: [] }));
          setProjections(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
          break;
        }
      }
    } catch { toastError("Failed to load data"); }
    finally { setLoading(false); }
  };

  useEffect(() => { loadTab(tab); }, [tab]);

  const TABS: { key: Tab; label: string; icon: React.ReactNode }[] = [
    { key: "demand", label: "Demand Forecast", icon: <TrendingUp size={14} /> },
    { key: "pricing", label: "Price Optimization", icon: <DollarSign size={14} /> },
    { key: "noshow", label: "No-Show Risk", icon: <AlertCircle size={14} /> },
    { key: "competitor", label: "Competitor Report", icon: <Target size={14} /> },
    { key: "benchmarks", label: "Benchmarks", icon: <BarChart3 size={14} /> },
    { key: "projections", label: "Projections", icon: <TrendingUp size={14} /> },
  ];

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Business Intelligence <Brain className="text-ai-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">AI-powered demand forecasting, price optimization, and competitive analysis.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={() => loadTab(tab)} disabled={loading}>Refresh</Button>
      </header>

      <div className="flex flex-wrap gap-1 p-1 bg-surface-100 rounded-xl">
        {TABS.map((t) => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg transition-colors ${tab === t.key ? "bg-white text-text-primary shadow-sm" : "text-text-secondary hover:text-text-primary"}`}>
            {t.icon}{t.label}
          </button>
        ))}
      </div>

      {loading ? <div className="flex justify-center py-14"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {tab === "demand" && (
            <Card>
              <CardHeader><CardTitle>Demand Forecast</CardTitle><CardDescription>Predicted booking demand over the next 30 days</CardDescription></CardHeader>
              <CardContent>
                {demand.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No forecast data available</p> : (
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Date", "Predicted Demand", "Confidence"].map((h) => (
                        <th key={h} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {demand.map((d, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-2 px-3 text-xs text-text-primary">{new Date(d.date).toLocaleDateString()}</td>
                          <td className="py-2 px-3 font-semibold text-ai-600">{d.predictedDemand} bookings</td>
                          <td className="py-2 px-3">
                            <div className="flex items-center gap-2">
                              <div className="w-20 h-1.5 bg-surface-200 rounded-full overflow-hidden">
                                <div className="h-full bg-green-500 rounded-full" style={{ width: `${d.confidence}%` }} />
                              </div>
                              <span className="text-xs text-text-tertiary">{d.confidence}%</span>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </CardContent>
            </Card>
          )}

          {tab === "pricing" && (
            <div className="space-y-3">
              {priceOpts.length === 0 ? (
                <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No price optimization recommendations</p></CardContent></Card>
              ) : priceOpts.map((p, i) => (
                <Card key={i}>
                  <CardContent className="pt-4 pb-4">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <p className="font-semibold text-text-primary">{p.serviceName}</p>
                        <div className="flex items-center gap-3 mt-1 text-sm">
                          <span className="text-text-tertiary">Current: <span className="font-medium text-text-primary">${p.currentPrice}</span></span>
                          <span className="text-ai-600 font-semibold">→ Recommended: ${p.recommendedPrice}</span>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="text-xs text-text-tertiary">Revenue Impact</p>
                        <p className="text-lg font-bold text-green-600">+${p.expectedRevenueIncrease?.toLocaleString()}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          {tab === "noshow" && (
            <Card>
              <CardHeader><CardTitle>No-Show Risk Scores</CardTitle><CardDescription>Clients with elevated likelihood of not showing up for upcoming appointments</CardDescription></CardHeader>
              <CardContent>
                {noShow.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No at-risk clients identified</p> : (
                  <div className="space-y-3">
                    {noShow.map((n, i) => (
                      <div key={i} className="flex items-center justify-between p-3 rounded-xl bg-surface-50 border border-surface-200">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-full bg-surface-200 flex items-center justify-center"><Users className="h-3.5 w-3.5 text-text-tertiary" /></div>
                          <div>
                            <p className="text-sm font-medium text-text-primary">{n.clientName}</p>
                            {n.nextBookingDate && <p className="text-xs text-text-tertiary">Next: {new Date(n.nextBookingDate).toLocaleDateString()}</p>}
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <div className="w-16 h-1.5 bg-surface-200 rounded-full overflow-hidden">
                            <div className="h-full rounded-full" style={{ width: `${n.riskScore}%`, backgroundColor: n.riskScore > 70 ? "#ef4444" : n.riskScore > 40 ? "#f59e0b" : "#22c55e" }} />
                          </div>
                          <span className={`text-xs font-bold ${n.riskScore > 70 ? "text-red-600" : n.riskScore > 40 ? "text-amber-600" : "text-green-600"}`}>{n.riskScore}%</span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {tab === "competitor" && (
            <div className="space-y-4">
              {competitor.length === 0 ? (
                <Card><CardContent className="text-center py-10 text-text-tertiary"><p>No competitor data available</p></CardContent></Card>
              ) : competitor.map((c, i) => (
                <Card key={i}>
                  <CardHeader><CardTitle className="text-base">{c.competitor}</CardTitle><CardDescription>Updated {new Date(c.updatedAt).toLocaleDateString()}</CardDescription></CardHeader>
                  <CardContent className="grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-xs font-semibold text-green-600 uppercase mb-2">Strengths</p>
                      <ul className="space-y-1">{c.strengths?.map((s, j) => <li key={j} className="text-xs text-text-secondary">• {s}</li>)}</ul>
                    </div>
                    <div>
                      <p className="text-xs font-semibold text-red-500 uppercase mb-2">Weaknesses</p>
                      <ul className="space-y-1">{c.weaknesses?.map((w, j) => <li key={j} className="text-xs text-text-secondary">• {w}</li>)}</ul>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          {tab === "benchmarks" && (
            <Card>
              <CardHeader><CardTitle>Industry Benchmarks</CardTitle><CardDescription>How your business compares to industry averages</CardDescription></CardHeader>
              <CardContent>
                {benchmarks.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No benchmark data available</p> : (
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Metric", "Your Value", "Industry Avg", "Top 25%"].map((h) => (
                        <th key={h} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {benchmarks.map((b, i) => {
                        const aboveAvg = b.yourValue >= b.industryAvg;
                        return (
                          <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                            <td className="py-2 px-3 text-xs font-medium text-text-primary">{b.metric}</td>
                            <td className={`py-2 px-3 text-xs font-bold ${aboveAvg ? "text-green-600" : "text-red-500"}`}>{b.yourValue}</td>
                            <td className="py-2 px-3 text-xs text-text-secondary">{b.industryAvg}</td>
                            <td className="py-2 px-3 text-xs text-text-tertiary">{b.topQuartile}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                )}
              </CardContent>
            </Card>
          )}

          {tab === "projections" && (
            <Card>
              <CardHeader><CardTitle>Revenue Projections</CardTitle><CardDescription>AI-generated revenue and growth forecasts</CardDescription></CardHeader>
              <CardContent>
                {projections.length === 0 ? <p className="text-sm text-text-tertiary text-center py-8">No projection data available</p> : (
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Month", "Revenue", "Bookings", "New Clients"].map((h) => (
                        <th key={h} className="text-left py-2 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {projections.map((p, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-2 px-3 text-xs font-medium text-text-primary">{p.month}</td>
                          <td className="py-2 px-3 text-xs font-semibold text-green-600">${p.revenue?.toLocaleString()}</td>
                          <td className="py-2 px-3 text-xs text-text-secondary">{p.bookings}</td>
                          <td className="py-2 px-3 text-xs text-text-secondary">{p.clients}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
