import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  ActivityIndicator,
  Alert,
  ScrollView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient } from '../api/apiClient';
import { RootStackParamList } from '../../App';
import { money, useTenantCurrency } from '../utils/currency';

type NavProp = NativeStackNavigationProp<RootStackParamList>;
type Tab = 'Revenue' | 'Bookings' | 'Clients';
type Period = 'week' | 'month' | 'year';

interface RevenueData {
  total: number;
  average: number;
  breakdown: Array<{ label: string; amount: number }>;
}

interface BookingsData {
  total: number;
  completed: number;
  noShows: number;
  cancellations: number;
}

interface ClientsData {
  newThisMonth: number;
  totalActive: number;
  retentionRate: number;
}

export function ReportsScreen() {
  // Aggregate figures belong to the tenant, so they render in the tenant's currency.
  const tenantCurrency = useTenantCurrency();
  const navigation = useNavigation<NavProp>();
  const [tab, setTab] = useState<Tab>('Revenue');
  const [period, setPeriod] = useState<Period>('month');
  const [revenueData, setRevenueData] = useState<RevenueData | null>(null);
  const [bookingsData, setBookingsData] = useState<BookingsData | null>(null);
  const [clientsData, setClientsData] = useState<ClientsData | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadData();
  }, [tab, period]);

  // These read the Analytics endpoints — there is no separate /reports/* API, and the
  // analytics controller already computes exactly these figures. Field names differ from the
  // view models below, so map explicitly rather than casting.
  const loadData = async () => {
    setLoading(true);
    try {
      if (tab === 'Revenue') {
        const res = await apiClient.get(`/analytics/revenue?period=${period}`);
        const d = res.data ?? {};
        setRevenueData({
          total: Number(d.totalRevenue) || 0,
          average: Number(d.averageDaily) || 0,
          breakdown: (Array.isArray(d.data) ? d.data : [])
            .filter((p: { revenue?: number }) => Number(p.revenue) > 0)
            .map((p: { date: string; revenue: number }) => ({
              label: new Date(p.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
              amount: Number(p.revenue) || 0,
            })),
        });
      } else if (tab === 'Bookings') {
        const res = await apiClient.get(`/analytics/bookings?period=${period}`);
        const d = res.data ?? {};
        const byStatus = d.byStatus ?? {};
        setBookingsData({
          total: Number(d.totalBookings) || 0,
          completed: Number(byStatus.completed) || 0,
          noShows: Number(byStatus.noshow ?? byStatus.noShow) || 0,
          cancellations: Number(byStatus.cancelled) || 0,
        });
      } else {
        const res = await apiClient.get(`/analytics/clients?period=${period}`);
        const d = res.data ?? {};
        const total = Number(d.totalClients) || 0;
        const returning = Number(d.returningClients) || 0;
        setClientsData({
          newThisMonth: Number(d.newClients) || 0,
          totalActive: total,
          retentionRate: total > 0 ? Math.round((returning / total) * 100) : 0,
        });
      }
    } catch {
      Alert.alert('Error', 'Failed to load report data');
    } finally {
      setLoading(false);
    }
  };

  // Bookings and Clients have server-side CSV exports; Revenue does not.
  const exportCsv = async () => {
    const exportPath = tab === 'Bookings' ? '/bookings/export' : tab === 'Clients' ? '/clients/export' : null;
    if (!exportPath) {
      Alert.alert('Not available', 'CSV export is available for Bookings and Clients reports.');
      return;
    }
    try {
      await apiClient.get(exportPath);
      Alert.alert('Export ready', 'Your export has been generated.');
    } catch {
      Alert.alert('Error', 'Failed to export report');
    }
  };

  const maxRevenue = revenueData?.breakdown?.reduce((m, i) => Math.max(m, i.amount), 1) ?? 1;

  const renderRevenue = () => (
    <ScrollView>
      <View style={styles.statRow}>
        <View style={styles.statCard}>
          <Text style={styles.statLabel}>Total Revenue</Text>
          <Text style={styles.statValue}>${(revenueData?.total ?? 0).toLocaleString()}</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statLabel}>Average</Text>
          <Text style={styles.statValue}>{money(revenueData?.average ?? 0, tenantCurrency)}</Text>
        </View>
      </View>
      <Text style={styles.chartTitle}>Breakdown</Text>
      {(revenueData?.breakdown ?? []).map(item => (
        <View key={item.label} style={styles.barRow}>
          <Text style={styles.barLabel}>{item.label}</Text>
          <View style={styles.barTrack}>
            <View style={[styles.barFill, { width: `${(item.amount / maxRevenue) * 100}%` }]} />
          </View>
          <Text style={styles.barValue}>${item.amount}</Text>
        </View>
      ))}
    </ScrollView>
  );

  const renderBookings = () => (
    <View style={styles.statsGrid}>
      {[
        { label: 'Total', value: bookingsData?.total ?? 0, color: '#007AFF' },
        { label: 'Completed', value: bookingsData?.completed ?? 0, color: '#34C759' },
        { label: 'No-Shows', value: bookingsData?.noShows ?? 0, color: '#FF9500' },
        { label: 'Cancellations', value: bookingsData?.cancellations ?? 0, color: '#FF3B30' },
      ].map(item => (
        <View key={item.label} style={[styles.gridCard, { borderLeftColor: item.color }]}>
          <Text style={[styles.gridValue, { color: item.color }]}>{item.value}</Text>
          <Text style={styles.gridLabel}>{item.label}</Text>
        </View>
      ))}
    </View>
  );

  const renderClients = () => (
    <View style={styles.statsGrid}>
      {[
        { label: 'New This Month', value: clientsData?.newThisMonth ?? 0, color: '#007AFF' },
        { label: 'Total Active', value: clientsData?.totalActive ?? 0, color: '#34C759' },
        { label: 'Retention Rate', value: `${(clientsData?.retentionRate ?? 0).toFixed(1)}%`, color: '#FF9500' },
      ].map(item => (
        <View key={item.label} style={[styles.gridCard, { borderLeftColor: item.color }]}>
          <Text style={[styles.gridValue, { color: item.color }]}>{item.value}</Text>
          <Text style={styles.gridLabel}>{item.label}</Text>
        </View>
      ))}
    </View>
  );

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Reports</Text>
        <TouchableOpacity onPress={exportCsv} style={styles.exportBtn}>
          <Text style={styles.exportBtnText}>Export CSV</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.tabs}>
        {(['Revenue', 'Bookings', 'Clients'] as Tab[]).map(t => (
          <TouchableOpacity key={t} style={[styles.tab, tab === t && styles.tabActive]} onPress={() => setTab(t)}>
            <Text style={[styles.tabText, tab === t && styles.tabTextActive]}>{t}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <View style={styles.periodRow}>
        {([['week', 'This Week'], ['month', 'This Month'], ['year', 'This Year']] as [Period, string][]).map(([p, label]) => (
          <TouchableOpacity
            key={p}
            style={[styles.periodBtn, period === p && styles.periodBtnActive]}
            onPress={() => setPeriod(p)}
          >
            <Text style={[styles.periodBtnText, period === p && styles.periodBtnTextActive]}>{label}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>
      ) : (
        <View style={{ flex: 1, padding: 16 }}>
          {tab === 'Revenue' && renderRevenue()}
          {tab === 'Bookings' && renderBookings()}
          {tab === 'Clients' && renderClients()}
        </View>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 20, paddingTop: 16, paddingBottom: 8 },
  title: { fontSize: 24, fontWeight: '700', color: '#111' },
  exportBtn: { backgroundColor: '#007AFF20', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 6 },
  exportBtnText: { color: '#007AFF', fontWeight: '600', fontSize: 13 },
  tabs: { flexDirection: 'row', marginHorizontal: 16, marginBottom: 8, borderRadius: 8, backgroundColor: '#F2F2F7', padding: 2 },
  tab: { flex: 1, paddingVertical: 8, alignItems: 'center', borderRadius: 6 },
  tabActive: { backgroundColor: '#fff', shadowColor: '#000', shadowOpacity: 0.1, shadowRadius: 4, elevation: 2 },
  tabText: { color: '#888', fontWeight: '500', fontSize: 13 },
  tabTextActive: { color: '#007AFF', fontWeight: '600', fontSize: 13 },
  periodRow: { flexDirection: 'row', paddingHorizontal: 16, gap: 8, marginBottom: 8 },
  periodBtn: { flex: 1, paddingVertical: 6, alignItems: 'center', borderRadius: 6, backgroundColor: '#F2F2F7' },
  periodBtnActive: { backgroundColor: '#007AFF' },
  periodBtnText: { fontSize: 12, color: '#666', fontWeight: '500' },
  periodBtnTextActive: { color: '#fff', fontWeight: '600' },
  statRow: { flexDirection: 'row', gap: 12, marginBottom: 24 },
  statCard: { flex: 1, backgroundColor: '#F8F8F8', borderRadius: 12, padding: 16 },
  statLabel: { fontSize: 13, color: '#888', marginBottom: 6 },
  statValue: { fontSize: 22, fontWeight: '700', color: '#111' },
  chartTitle: { fontSize: 14, fontWeight: '600', color: '#888', marginBottom: 12, textTransform: 'uppercase', letterSpacing: 0.5 },
  barRow: { flexDirection: 'row', alignItems: 'center', marginBottom: 10 },
  barLabel: { width: 60, fontSize: 12, color: '#555' },
  barTrack: { flex: 1, height: 20, backgroundColor: '#F0F0F0', borderRadius: 4, marginHorizontal: 8, overflow: 'hidden' },
  barFill: { height: '100%', backgroundColor: '#007AFF', borderRadius: 4 },
  barValue: { width: 60, fontSize: 12, color: '#111', textAlign: 'right' },
  statsGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12 },
  gridCard: { width: '47%', backgroundColor: '#F8F8F8', borderRadius: 12, padding: 16, borderLeftWidth: 4 },
  gridValue: { fontSize: 28, fontWeight: '700', marginBottom: 4 },
  gridLabel: { fontSize: 13, color: '#888' },
});
