/**
 * M6: Analytics dashboard in mobile.
 * Shows key metrics: revenue, bookings, client growth, top services, and AI weekly summary.
 */
import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  RefreshControl,
  StyleSheet,
  ActivityIndicator,
  Dimensions,
} from 'react-native';
import { apiClient } from '../api/apiClient';

interface AnalyticsSummary {
  revenue: { current: number; previous: number; currency: string };
  bookings: { current: number; previous: number };
  newClients: { current: number; previous: number };
  topServices: Array<{ name: string; bookings: number; revenue: number }>;
  aiNarrative?: string;
  period: string;
}

const { width } = Dimensions.get('window');

function DeltaBadge({ current, previous }: { current: number; previous: number }) {
  if (previous === 0) return null;
  const pct = ((current - previous) / previous) * 100;
  const isUp = pct >= 0;
  return (
    <Text style={[styles.delta, isUp ? styles.deltaUp : styles.deltaDown]}>
      {isUp ? '▲' : '▼'} {Math.abs(pct).toFixed(1)}%
    </Text>
  );
}

function MetricCard({ label, value, previous, format = 'number' }: {
  label: string; value: number; previous: number; format?: 'number' | 'currency';
}) {
  const display = format === 'currency' ? `$${value.toLocaleString(undefined, { maximumFractionDigits: 0 })}` : value.toLocaleString();
  return (
    <View style={styles.metricCard}>
      <Text style={styles.metricLabel}>{label}</Text>
      <Text style={styles.metricValue}>{display}</Text>
      <DeltaBadge current={value} previous={previous} />
    </View>
  );
}

export default function AnalyticsScreen() {
  const [data, setData] = useState<AnalyticsSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      // There is no /reports/summary endpoint — compose the summary from the Analytics
      // controller, which already computes each figure. Every call degrades independently
      // so one failure (e.g. the plan-gated AI summary) cannot blank the screen.
      const [revenueRes, bookingsRes, clientsRes, servicesRes, settingsRes, weeklyRes] =
        await Promise.all([
          apiClient.get('/analytics/revenue?period=month').catch(() => null),
          apiClient.get('/analytics/bookings?period=month').catch(() => null),
          apiClient.get('/analytics/clients?period=month').catch(() => null),
          apiClient.get('/analytics/services?period=month').catch(() => null),
          apiClient.get('/settings/business').catch(() => null),
          apiClient.get('/aidashboard/weekly-summary').catch(() => null),
        ]);

      const revenue = revenueRes?.data ?? {};
      const bookings = bookingsRes?.data ?? {};
      const clients = clientsRes?.data ?? {};
      const services = servicesRes?.data ?? {};
      const weekly = weeklyRes?.data;

      setData({
        revenue: {
          current: Number(revenue.totalRevenue) || 0,
          previous: Number(revenue.previousPeriodRevenue) || 0,
          currency: settingsRes?.data?.currency ?? 'USD',
        },
        bookings: { current: Number(bookings.totalBookings) || 0, previous: 0 },
        newClients: { current: Number(clients.newClients) || 0, previous: 0 },
        topServices: Array.isArray(services.topServices) ? services.topServices : [],
        aiNarrative: weekly?.narrative,
        period: 'Last 30 days',
      });
    } catch {
      // Silent fail — show empty state
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <ActivityIndicator style={styles.loader} size="large" color="#3B82F6" />;

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      <View style={styles.header}>
        <Text style={styles.title}>Analytics</Text>
        <Text style={styles.period}>{data?.period ?? 'Last 30 days'}</Text>
      </View>

      {/* AI Summary */}
      {data?.aiNarrative && (
        <View style={styles.aiCard}>
          <Text style={styles.aiLabel}>✨ AI Weekly Summary</Text>
          <Text style={styles.aiText}>{data.aiNarrative}</Text>
        </View>
      )}

      {/* Key Metrics Grid */}
      <View style={styles.metricsGrid}>
        <MetricCard label="Revenue" value={data?.revenue.current ?? 0} previous={data?.revenue.previous ?? 0} format="currency" />
        <MetricCard label="Bookings" value={data?.bookings.current ?? 0} previous={data?.bookings.previous ?? 0} />
        <MetricCard label="New Clients" value={data?.newClients.current ?? 0} previous={data?.newClients.previous ?? 0} />
      </View>

      {/* Top Services */}
      {(data?.topServices?.length ?? 0) > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Top Services</Text>
          {data!.topServices.slice(0, 5).map((svc, i) => (
            <View key={svc.name} style={styles.serviceRow}>
              <Text style={styles.serviceRank}>#{i + 1}</Text>
              <View style={styles.serviceInfo}>
                <Text style={styles.serviceName}>{svc.name}</Text>
                <Text style={styles.serviceDetail}>{svc.bookings} bookings</Text>
              </View>
              <Text style={styles.serviceRevenue}>${svc.revenue.toLocaleString(undefined, { maximumFractionDigits: 0 })}</Text>
            </View>
          ))}
        </View>
      )}

      {!data && (
        <View style={styles.empty}>
          <Text style={styles.emptyIcon}>📊</Text>
          <Text style={styles.emptyText}>No data yet</Text>
          <Text style={styles.emptySubtext}>Analytics will appear once you have completed bookings.</Text>
        </View>
      )}

      <View style={styles.footer} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  loader: { flex: 1 },
  header: { padding: 16, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#E5E7EB' },
  title: { fontSize: 20, fontWeight: '700', color: '#111827' },
  period: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  aiCard: { margin: 16, padding: 16, backgroundColor: '#EFF6FF', borderRadius: 12, borderLeftWidth: 3, borderLeftColor: '#3B82F6' },
  aiLabel: { fontSize: 12, fontWeight: '700', color: '#1D4ED8', marginBottom: 6 },
  aiText: { fontSize: 14, color: '#1E40AF', lineHeight: 20 },
  metricsGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, padding: 16 },
  metricCard: { flex: 1, minWidth: (width - 56) / 2, backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  metricLabel: { fontSize: 12, color: '#6B7280', fontWeight: '500', marginBottom: 4 },
  metricValue: { fontSize: 22, fontWeight: '700', color: '#111827' },
  delta: { fontSize: 12, fontWeight: '600', marginTop: 4 },
  deltaUp: { color: '#10B981' },
  deltaDown: { color: '#EF4444' },
  section: { margin: 16, marginTop: 0, backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  sectionTitle: { fontSize: 16, fontWeight: '700', color: '#111827', marginBottom: 12 },
  serviceRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#F3F4F6' },
  serviceRank: { width: 28, fontSize: 14, fontWeight: '700', color: '#9CA3AF' },
  serviceInfo: { flex: 1 },
  serviceName: { fontSize: 14, fontWeight: '600', color: '#111827' },
  serviceDetail: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  serviceRevenue: { fontSize: 14, fontWeight: '700', color: '#111827' },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 48 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 18, fontWeight: '600', color: '#374151', marginBottom: 8 },
  emptySubtext: { fontSize: 14, color: '#6B7280', textAlign: 'center' },
  footer: { height: 32 },
});
