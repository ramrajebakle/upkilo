"use client";

import React, { useState, useEffect } from "react";
import { DollarSign, TrendingUp, TrendingDown, BarChart3, AlertCircle, Loader2, RefreshCw, ArrowUpRight, ArrowDownRight } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

interface FinancialInsight { category: string; insight: string; impact: number; recommendation: string; severity: "positive" | "warning" | "critical"; }
interface CashFlow { month: string; revenue: number; expenses: number; netCashFlow: number; }
interface FinancialSummary { currentMonthRevenue: number; revenueGrowth: number; averageTransactionValue: number; revenuePerClient: number; churnImpact: number; forecastNextMonth: number; }

export default function FinancialIntelligencePage() {
  const [summary, setSummary] = useState<FinancialSummary | null>(null);
  const [insights, setInsights] = useState<FinancialInsight[]>([]);
  const [cashFlow, setCashFlow] = useState<CashFlow[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      // FinancialIntelligenceController exposes predict-revenue / churn-risk /
      // cashflow-forecast — there are no /summary, /insights or /cashflow routes, so those
      // three calls always 404'd and the page rendered empty. Compose the summary from the
      // analytics endpoints that actually hold these figures.
      const [revRes, bookRes, cliRes, forecastRes, churnRes, cfRes] = await Promise.all([
        apiClient.get("/api/v1/analytics/revenue?period=month").catch(() => ({ data: null })),
        apiClient.get("/api/v1/analytics/bookings?period=month").catch(() => ({ data: null })),
        apiClient.get("/api/v1/analytics/clients?period=month").catch(() => ({ data: null })),
        apiClient.get("/api/v1/financialintelligence/predict-revenue?months=1").catch(() => ({ data: null })),
        apiClient.get("/api/v1/financialintelligence/churn-risk").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/financialintelligence/cashflow-forecast").catch(() => ({ data: [] })),
      ]);

      const rev = revRes.data ?? {};
      const book = bookRes.data ?? {};
      const cli = cliRes.data ?? {};
      const totalRevenue = Number(rev.totalRevenue) || 0;
      const clients = Number(cli.totalClients) || 0;
      const projected = forecastRes.data?.projectedRevenue;

      setSummary({
        currentMonthRevenue: totalRevenue,
        revenueGrowth: Number(rev.growthRate) || 0,
        averageTransactionValue: Number(book.averageValue) || 0,
        revenuePerClient: clients > 0 ? totalRevenue / clients : 0,
        churnImpact: 0,
        forecastNextMonth: Number(Array.isArray(projected) ? projected[0] : projected) || 0,
      });

      // Churn risks are the only server-side "insight" feed; map them onto the view model.
      const risks = Array.isArray(churnRes.data) ? churnRes.data : churnRes.data?.data ?? [];
      setInsights(
        risks.slice(0, 5).map((r: any) => ({
          category: "Churn risk",
          insight: r.clientName ? `${r.clientName} is at risk of churning` : "Client at risk of churning",
          impact: Number(r.lifetimeValue ?? r.impact) || 0,
          recommendation: r.recommendation ?? "Reach out with a re-engagement offer.",
          severity: (Number(r.riskScore) || 0) > 0.7 ? "critical" : "warning",
        }))
      );

      setCashFlow(Array.isArray(cfRes.data) ? cfRes.data : cfRes.data?.data ?? []);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const SEV: Record<string, string> = {
    positive: "text-green-600 bg-green-50 border-green-200",
    warning: "text-amber-600 bg-amber-50 border-amber-200",
    critical: "text-red-600 bg-red-50 border-red-200",
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Financial Intelligence <DollarSign className="text-green-500" size={22} /></h1>
          <p className="text-text-secondary mt-1">AI-powered financial analysis, cash flow forecasting, and revenue insights.</p>
        </div>
        <Button variant="outline" leftIcon={loading ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />} onClick={load} disabled={loading}>Refresh</Button>
      </header>

      {loading ? <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          {summary && (
            <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
              {[
                { label: "This Month Revenue", value: `$${(summary.currentMonthRevenue ?? 0).toLocaleString()}`, change: summary.revenueGrowth, icon: <DollarSign className="h-5 w-5 text-green-400" /> },
                { label: "Avg Transaction Value", value: `$${(summary.averageTransactionValue ?? 0).toFixed(0)}`, icon: <BarChart3 className="h-5 w-5 text-blue-400" /> },
                { label: "Revenue per Client", value: `$${(summary.revenuePerClient ?? 0).toFixed(0)}`, icon: <TrendingUp className="h-5 w-5 text-ai-400" /> },
                { label: "Churn Revenue Impact", value: `-$${(summary.churnImpact ?? 0).toLocaleString()}`, negative: true, icon: <TrendingDown className="h-5 w-5 text-red-400" /> },
                { label: "Next Month Forecast", value: `$${(summary.forecastNextMonth ?? 0).toLocaleString()}`, icon: <ArrowUpRight className="h-5 w-5 text-purple-400" /> },
                { label: "MoM Growth", value: `${(summary.revenueGrowth ?? 0) >= 0 ? "+" : ""}${(summary.revenueGrowth ?? 0).toFixed(1)}%`, positive: (summary.revenueGrowth ?? 0) >= 0, icon: <TrendingUp className="h-5 w-5 text-text-tertiary" /> },
              ].map((s, i) => (
                <Card key={i}>
                  <CardContent className="pt-5">
                    <div className="flex items-center justify-between mb-1">{s.icon}</div>
                    <p className="text-xs text-text-secondary">{s.label}</p>
                    <p className={`text-xl font-bold mt-0.5 ${s.negative ? "text-red-600" : s.positive === false ? "text-red-500" : "text-text-primary"}`}>{s.value}</p>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader><CardTitle>AI Insights</CardTitle><CardDescription>Actionable recommendations based on your financial patterns</CardDescription></CardHeader>
              <CardContent className="space-y-3">
                {insights.length === 0 ? <p className="text-sm text-text-tertiary text-center py-6">No insights available</p> : insights.map((ins, i) => (
                  <div key={i} className={`p-3 rounded-xl border ${SEV[ins.severity] ?? SEV.warning}`}>
                    <div className="flex items-start gap-2">
                      {ins.severity === "critical" ? <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" /> : ins.severity === "positive" ? <ArrowUpRight className="h-4 w-4 flex-shrink-0 mt-0.5" /> : <TrendingDown className="h-4 w-4 flex-shrink-0 mt-0.5" />}
                      <div>
                        <p className="text-xs font-semibold uppercase mb-0.5">{ins.category}</p>
                        <p className="text-sm">{ins.insight}</p>
                        {ins.recommendation && <p className="text-xs mt-1 opacity-75 italic">{ins.recommendation}</p>}
                        {ins.impact !== undefined && <p className="text-xs font-semibold mt-1">Impact: ${Math.abs(ins.impact).toLocaleString()}</p>}
                      </div>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card>
              <CardHeader><CardTitle>Cash Flow (12 months)</CardTitle><CardDescription>Monthly revenue vs expenses and net cash position</CardDescription></CardHeader>
              <CardContent>
                {cashFlow.length === 0 ? <p className="text-sm text-text-tertiary text-center py-6">No cash flow data available</p> : (
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-surface-200">
                      {["Month", "Revenue", "Expenses", "Net"].map((h) => (
                        <th key={h} className="text-left py-2 px-2 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                      ))}
                    </tr></thead>
                    <tbody>
                      {cashFlow.slice(-12).map((cf, i) => (
                        <tr key={i} className="border-b border-surface-100 hover:bg-surface-50">
                          <td className="py-1.5 px-2 text-xs font-medium text-text-primary">{cf.month}</td>
                          <td className="py-1.5 px-2 text-xs text-green-600">${(cf.revenue ?? 0).toLocaleString()}</td>
                          <td className="py-1.5 px-2 text-xs text-red-500">${(cf.expenses ?? 0).toLocaleString()}</td>
                          <td className={`py-1.5 px-2 text-xs font-semibold ${(cf.netCashFlow ?? 0) >= 0 ? "text-green-600" : "text-red-500"}`}>
                            {(cf.netCashFlow ?? 0) >= 0 ? "+" : ""}${(cf.netCashFlow ?? 0).toLocaleString()}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
