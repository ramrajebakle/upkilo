import React, { useCallback, useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  SafeAreaView,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { apiClient, unwrapList } from '../api/apiClient';
import { money } from '../utils/currency';

interface Service {
  id: string;
  name: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
}

interface RevenueState {
  monthRevenue: number;
  currency: string;
  totalBookings: number;
  averageValue: number;
  completionRate: number;
  acceptCards: boolean;
  services: Service[];
}


export function RevenueScreen() {
  const [state, setState] = useState<RevenueState | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    // Every panel degrades on its own — a plan-gated or empty endpoint must not blank the screen.
    const [revenueRes, bookingsRes, settingsRes, paymentsRes, servicesRes] =
      await Promise.allSettled([
        apiClient.get('/analytics/revenue?period=month'),
        apiClient.get('/analytics/bookings?period=month'),
        apiClient.get('/settings/business'),
        apiClient.get('/settings/payments'),
        apiClient.get('/services'),
      ]);

    const val = <T,>(r: PromiseSettledResult<{ data: T }>): Partial<T> =>
      r.status === 'fulfilled' ? ((r.value.data ?? {}) as Partial<T>) : {};

    const revenue = val<{ totalRevenue: number }>(revenueRes);
    const bookings = val<{ totalBookings: number; averageValue: number; completionRate: number }>(bookingsRes);
    const settings = val<{ currency: string }>(settingsRes);
    const payments = val<{ acceptCards: boolean }>(paymentsRes);

    setState({
      monthRevenue: Number(revenue.totalRevenue) || 0,
      currency: settings.currency ?? 'USD',
      totalBookings: Number(bookings.totalBookings) || 0,
      averageValue: Number(bookings.averageValue) || 0,
      completionRate: Number(bookings.completionRate) || 0,
      acceptCards: payments.acceptCards ?? false,
      services: servicesRes.status === 'fulfilled' ? unwrapList<Service>(servicesRes.value.data) : [],
    });
    setLoading(false);
    setRefreshing(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <ActivityIndicator style={{ marginTop: 60 }} size="large" color="#D97706" />
      </SafeAreaView>
    );
  }

  const s = state!;
  const activeServices = s.services.filter((x) => x.isActive);

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.banner}>
        <Text style={styles.bannerText}>You are viewing revenue from your customers</Text>
      </View>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />
        }
      >
        <Text style={styles.headerTitle}>YOUR REVENUE</Text>

        {/* Payment acceptance — reflects Settings → Payments */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Payments</Text>
          <View style={styles.providerRow}>
            <View style={styles.providerInfo}>
              <View style={[styles.statusDot, s.acceptCards && styles.dotConnected]} />
              <Text style={styles.providerName}>Card payments</Text>
              <Text style={s.acceptCards ? styles.providerStatus : styles.providerStatusDim}>
                {s.acceptCards ? 'Enabled' : 'Disabled'}
              </Text>
            </View>
          </View>
          <Text style={styles.hint}>Payout currency: {s.currency}</Text>
        </View>

        {/* Revenue this month */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Revenue This Month</Text>
          <Text style={styles.revenueAmount}>{money(s.monthRevenue, s.currency)}</Text>
          <View style={styles.divider} />
          <View style={styles.statsInlineRow}>
            <View>
              <Text style={styles.statLabel}>Bookings</Text>
              <Text style={styles.statValue}>{s.totalBookings}</Text>
            </View>
            <View>
              <Text style={styles.statLabel}>Avg value</Text>
              <Text style={styles.statValue}>{money(s.averageValue, s.currency)}</Text>
            </View>
            <View>
              <Text style={styles.statLabel}>Completed</Text>
              <Text style={styles.statValue}>{Math.round(s.completionRate)}%</Text>
            </View>
          </View>
        </View>

        {/* Services and prices */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Services & Prices</Text>
          {activeServices.length === 0 ? (
            <Text style={styles.hint}>No active services yet.</Text>
          ) : (
            <View style={styles.planBox}>
              {activeServices.slice(0, 8).map((svc) => (
                <Text key={svc.id} style={styles.planText}>
                  {svc.name}
                  {'  '}
                  <Text style={styles.planSub}>
                    {money(svc.price, s.currency)} · {svc.durationMinutes} min
                  </Text>
                </Text>
              ))}
            </View>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFFBEB' }, // Amber tint
  banner: { backgroundColor: '#F59E0B', padding: 12, alignItems: 'center' },
  bannerText: { color: '#fff', fontSize: 13, fontWeight: '600' },
  content: { padding: 24 },
  headerTitle: { fontSize: 24, fontWeight: '700', color: '#78350F', marginBottom: 24, letterSpacing: -0.5 },
  section: { marginBottom: 32 },
  sectionTitle: { fontSize: 16, fontWeight: '600', color: '#B45309', marginBottom: 12 },
  providerRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 },
  providerInfo: { flexDirection: 'row', alignItems: 'center' },
  statusDot: { width: 8, height: 8, borderRadius: 4, borderWidth: 1, borderColor: '#D97706', marginRight: 8, backgroundColor: 'transparent' },
  dotConnected: { backgroundColor: '#F59E0B', borderColor: '#F59E0B' },
  providerName: { fontSize: 15, color: '#78350F', fontWeight: '500', marginRight: 10 },
  providerStatus: { fontSize: 14, color: '#D97706' },
  providerStatusDim: { fontSize: 14, color: '#92400E', opacity: 0.7 },
  hint: { fontSize: 13, color: '#92400E', opacity: 0.8 },
  revenueAmount: { fontSize: 40, fontWeight: '700', color: '#78350F', marginBottom: 12 },
  divider: { height: 1, backgroundColor: '#FDE68A', marginBottom: 16 },
  statsInlineRow: { flexDirection: 'row', justifyContent: 'space-between', paddingRight: 20 },
  statLabel: { fontSize: 13, color: '#92400E', opacity: 0.8, marginBottom: 4 },
  statValue: { fontSize: 16, fontWeight: '600', color: '#78350F' },
  planBox: { borderWidth: 1, borderColor: '#FDE68A', padding: 16, borderRadius: 8 },
  planText: { fontSize: 14, color: '#78350F', marginBottom: 6 },
  planSub: { color: '#92400E' },
});
