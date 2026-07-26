/**
 * MK7: Consumer-facing app — Booking history screen.
 * Shows past and upcoming bookings for the authenticated consumer.
 */
import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, FlatList, TouchableOpacity, RefreshControl,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { apiClient } from '../../api/apiClient';
import { AlertCircle } from 'lucide-react-native';

interface ConsumerBooking {
  id: string;
  businessName: string;
  serviceName: string;
  startTime: string;
  status: 'confirmed' | 'completed' | 'cancelled';
  price: number;
}

const statusLabel: Record<ConsumerBooking['status'], string> = {
  confirmed: 'Upcoming',
  completed: 'Completed',
  cancelled: 'Cancelled',
};

const statusColor: Record<ConsumerBooking['status'], string> = {
  confirmed: '#10B981',
  completed: '#3B82F6',
  cancelled: '#EF4444',
};

export default function ConsumerHistoryScreen({ navigation }: { navigation: any }) {
  const [bookings, setBookings] = useState<ConsumerBooking[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [fetchError, setFetchError] = useState(false);

  const load = useCallback(async () => {
    try {
      setFetchError(false);
      // Base URL already includes /api/v1 — do not repeat the prefix
      const res = await apiClient.get('/consumer/bookings');
      const payload = res.data ?? {};
      setBookings(payload.items ?? payload.bookings ?? payload.data ?? []);
    } catch (err) {
      console.error('[ConsumerHistoryScreen] failed to load bookings:', err);
      setFetchError(true);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const renderItem = ({ item }: { item: ConsumerBooking }) => (
    <View style={styles.card}>
      <View style={styles.cardRow}>
        <Text style={styles.businessName}>{item.businessName}</Text>
        <View style={[styles.badge, { backgroundColor: statusColor[item.status] }]}>
          <Text style={styles.badgeText}>{statusLabel[item.status]}</Text>
        </View>
      </View>
      <Text style={styles.serviceName}>{item.serviceName}</Text>
      <View style={styles.cardFooter}>
        <Text style={styles.time}>{new Date(item.startTime).toLocaleString()}</Text>
        <Text style={styles.price}>${item.price}</Text>
      </View>
      {item.status === 'completed' && (
        <TouchableOpacity
          style={styles.rebookBtn}
          onPress={() => navigation?.navigate?.('ConsumerBook', { slug: '', name: item.businessName })}
        >
          <Text style={styles.rebookBtnText}>Book Again</Text>
        </TouchableOpacity>
      )}
    </View>
  );

  if (loading) return <ActivityIndicator style={styles.loader} size="large" color="#3B82F6" />;

  return (
    <View style={styles.container}>
      {fetchError && (
        <View style={styles.errorBanner}>
          <AlertCircle size={15} color="#EF4444" />
          <Text style={styles.errorText}>Failed to load bookings. Pull down to retry.</Text>
        </View>
      )}
      {bookings.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyIcon}>📅</Text>
          <Text style={styles.emptyText}>No bookings yet</Text>
          <Text style={styles.emptySubtext}>Discover and book a service to get started.</Text>
          <TouchableOpacity style={styles.discoverBtn} onPress={() => navigation?.navigate?.('ConsumerDiscover')}>
            <Text style={styles.discoverBtnText}>Discover Services</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={bookings}
          keyExtractor={b => b.id}
          renderItem={renderItem}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          contentContainerStyle={styles.list}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  loader: { flex: 1 },
  errorBanner: {
    flexDirection: 'row', alignItems: 'center', gap: 8,
    backgroundColor: '#FEF2F2', padding: 12, borderBottomWidth: 1,
    borderBottomColor: '#FCA5A5',
  },
  errorText: { flex: 1, fontSize: 13, color: '#B91C1C' },
  list: { padding: 16, gap: 12 },
  card: { backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  cardRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 },
  businessName: { fontSize: 16, fontWeight: '700', color: '#111827', flex: 1 },
  badge: { paddingHorizontal: 8, paddingVertical: 3, borderRadius: 6 },
  badgeText: { color: '#fff', fontSize: 10, fontWeight: '700' },
  serviceName: { fontSize: 14, color: '#6B7280', marginBottom: 8 },
  cardFooter: { flexDirection: 'row', justifyContent: 'space-between' },
  time: { fontSize: 13, color: '#374151' },
  price: { fontSize: 14, fontWeight: '600', color: '#111827' },
  rebookBtn: { marginTop: 12, borderWidth: 1, borderColor: '#3B82F6', borderRadius: 8, paddingVertical: 8, alignItems: 'center' },
  rebookBtnText: { color: '#3B82F6', fontWeight: '600', fontSize: 14 },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 48 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 18, fontWeight: '600', color: '#374151', marginBottom: 8 },
  emptySubtext: { fontSize: 14, color: '#6B7280', textAlign: 'center', marginBottom: 20 },
  discoverBtn: { backgroundColor: '#3B82F6', borderRadius: 10, paddingHorizontal: 24, paddingVertical: 12 },
  discoverBtnText: { color: '#fff', fontWeight: '700', fontSize: 15 },
});
