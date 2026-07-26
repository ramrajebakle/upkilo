'use client';

import { useEffect, useState } from 'react';

interface RevenueForecast {
  last30DayRevenue: number;
  momGrowthPercent: number;
  confirmedNextMonthRevenue: number;
  membershipMonthlyRevenue: number;
  forecast30Days: number;
  forecast60Days: number;
  forecast90Days: number;
  activeMemberCount: number;
  aiRecommendations: string[];
  generatedAt: string;
}

interface AIMetrics {
  decisionsToday: number;
  autoApproved: number;
  pendingReview: number;
  accuracy: number;
}

function MetricCard({ label, value, sub, trend }: { label: string; value: string; sub?: string; trend?: 'up' | 'down' | 'neutral' }) {
  const trendColor = trend === 'up' ? 'text-green-600' : trend === 'down' ? 'text-red-500' : 'text-gray-500';
  return (
    <div className="bg-white rounded-xl border border-gray-100 p-5 shadow-sm">
      <div className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">{label}</div>
      <div className="text-2xl font-bold text-gray-900 mb-1">{value}</div>
      {sub && <div className={`text-sm ${trendColor}`}>{sub}</div>}
    </div>
  );
}

function ForecastBar({ label, value, max }: { label: string; value: number; max: number }) {
  const pct = max > 0 ? Math.min((value / max) * 100, 100) : 0;
  return (
    <div>
      <div className="flex justify-between text-sm mb-1">
        <span className="text-gray-600">{label}</span>
        <span className="font-semibold text-gray-900">${value.toLocaleString(undefined, { maximumFractionDigits: 0 })}</span>
      </div>
      <div className="w-full bg-gray-100 rounded-full h-2">
        <div className="bg-indigo-500 h-2 rounded-full transition-all duration-700" style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

export default function AIDashboardPage() {
  const [forecast, setForecast] = useState<RevenueForecast | null>(null);
  const [metrics, setMetrics] = useState<AIMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [fcRes, mRes] = await Promise.all([
          fetch('/api/v1/aidashboard/forecast'),
          fetch('/api/v1/aidashboard/metrics'),
        ]);
        if (fcRes.ok) setForecast(await fcRes.json());
        if (mRes.ok) setMetrics(await mRes.json());
      } catch {
        setError('Failed to load AI dashboard data');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600" />
    </div>
  );

  if (error) return (
    <div className="p-6 text-red-600">{error}</div>
  );

  const maxForecast = forecast ? Math.max(forecast.forecast30Days, forecast.forecast60Days, forecast.forecast90Days) : 1;

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">AI Business Intelligence</h1>
          <p className="text-gray-500 text-sm mt-1">
            {forecast ? `Updated ${new Date(forecast.generatedAt).toLocaleTimeString()}` : 'Revenue forecasts and AI recommendations'}
          </p>
        </div>
        <div className="bg-indigo-50 text-indigo-700 px-3 py-1 rounded-full text-sm font-medium flex items-center gap-2">
          <span className="inline-block w-2 h-2 rounded-full bg-indigo-500 animate-pulse" />
          AI Active
        </div>
      </div>

      {/* KPI Row */}
      {forecast && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <MetricCard
            label="Last 30 Days"
            value={`$${forecast.last30DayRevenue.toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
            sub={`${forecast.momGrowthPercent >= 0 ? '+' : ''}${forecast.momGrowthPercent.toFixed(1)}% MoM`}
            trend={forecast.momGrowthPercent >= 0 ? 'up' : 'down'}
          />
          <MetricCard
            label="Confirmed (Next 30d)"
            value={`$${forecast.confirmedNextMonthRevenue.toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
            sub="Booked & confirmed"
            trend="neutral"
          />
          <MetricCard
            label="Membership MRR"
            value={`$${forecast.membershipMonthlyRevenue.toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
            sub={`${forecast.activeMemberCount} active members`}
            trend="up"
          />
          <MetricCard
            label="90-Day Forecast"
            value={`$${forecast.forecast90Days.toLocaleString(undefined, { maximumFractionDigits: 0 })}`}
            sub="AI projection"
            trend={forecast.momGrowthPercent >= 0 ? 'up' : 'down'}
          />
        </div>
      )}

      {/* Revenue Forecast + AI Recs side by side */}
      <div className="grid md:grid-cols-2 gap-6">
        {/* Forecast Bars */}
        {forecast && (
          <div className="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Revenue Projections</h2>
            <div className="space-y-4">
              <ForecastBar label="30-Day Forecast" value={forecast.forecast30Days} max={maxForecast} />
              <ForecastBar label="60-Day Forecast" value={forecast.forecast60Days} max={maxForecast} />
              <ForecastBar label="90-Day Forecast" value={forecast.forecast90Days} max={maxForecast} />
            </div>
            <p className="text-xs text-gray-400 mt-4">
              Based on booking velocity, confirmed appointments, and membership recurring revenue.
            </p>
          </div>
        )}

        {/* AI Recommendations */}
        {forecast && forecast.aiRecommendations.length > 0 && (
          <div className="bg-gradient-to-br from-indigo-50 to-purple-50 rounded-xl border border-indigo-100 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <span>🧠</span> AI Recommendations
            </h2>
            <div className="space-y-3">
              {forecast.aiRecommendations.map((rec, i) => (
                <div key={i} className="flex gap-3">
                  <div className="w-6 h-6 rounded-full bg-indigo-500 text-white text-xs flex items-center justify-center flex-shrink-0 font-bold">
                    {i + 1}
                  </div>
                  <p className="text-sm text-gray-700">{rec}</p>
                </div>
              ))}
            </div>
            <p className="text-xs text-gray-400 mt-4">Powered by AI — updated daily based on your business data.</p>
          </div>
        )}
      </div>

      {/* AI Decision Metrics */}
      {metrics && (
        <div className="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">AI Decision Engine (Today)</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            <div className="text-center">
              <div className="text-3xl font-bold text-gray-900">{metrics.decisionsToday ?? '—'}</div>
              <div className="text-sm text-gray-500 mt-1">Decisions Made</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-green-600">{metrics.autoApproved ?? '—'}</div>
              <div className="text-sm text-gray-500 mt-1">Auto-Approved</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-amber-500">{metrics.pendingReview ?? '—'}</div>
              <div className="text-sm text-gray-500 mt-1">Pending Review</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-indigo-600">{metrics.accuracy ? `${metrics.accuracy}%` : '—'}</div>
              <div className="text-sm text-gray-500 mt-1">Accuracy Score</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
