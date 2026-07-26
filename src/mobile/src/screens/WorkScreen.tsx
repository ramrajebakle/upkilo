import React, { useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, SafeAreaView, ScrollView,
  RefreshControl, TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { Calendar, Clock, User, ChevronRight, CheckCircle2, AlertCircle } from 'lucide-react-native';
import { useFocusEffect } from '@react-navigation/native';
import { apiClient } from '../api/apiClient';

interface Appointment {
  id: string;
  clientName: string;
  serviceName: string;
  startTime: string;
  endTime: string;
  status: 'confirmed' | 'pending' | 'cancelled' | 'completed' | 'no_show';
  staffName?: string;
  price?: number;
}

const STATUS_CONFIG: Record<string, { label: string; color: string; bg: string }> = {
  confirmed: { label: 'Confirmed', color: '#16A34A', bg: '#DCFCE7' },
  pending:   { label: 'Pending',   color: '#D97706', bg: '#FEF3C7' },
  cancelled: { label: 'Cancelled', color: '#DC2626', bg: '#FEE2E2' },
  completed: { label: 'Done',      color: '#6B7280', bg: '#F3F4F6' },
  no_show:   { label: 'No-show',   color: '#9333EA', bg: '#F3E8FF' },
};

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

function getDayLabel(date: Date): string {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const target = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const diff = Math.round((target.getTime() - today.getTime()) / 86400000);
  if (diff === 0) return 'Today';
  if (diff === 1) return 'Tomorrow';
  if (diff === -1) return 'Yesterday';
  return date.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });
}

export function WorkScreen({ navigation }: { navigation?: any }) {
  const today = new Date();
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchAppointments = useCallback(async () => {
    try {
      const dateStr = today.toISOString().split('T')[0];
      const res = await apiClient.get(`/bookings?date=${dateStr}&pageSize=50`);
      // The API returns { data: [...] }. The previous `res.data?.items ?? res.data` fell through
      // to that wrapper object, so `appointments` became an object and .filter() crashed.
      const body = res.data;
      const list = body?.data ?? body?.items ?? body;
      setAppointments(Array.isArray(list) ? list : []);
      setError(null);
    } catch {
      setError('Could not load today\'s schedule');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { fetchAppointments(); }, [fetchAppointments]));

  const onRefresh = () => { setRefreshing(true); fetchAppointments(); };

  const upcoming = appointments.filter(a => a.status !== 'cancelled' && a.status !== 'no_show');
  const confirmed = upcoming.filter(a => a.status === 'confirmed' || a.status === 'completed');

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#7C3AED" />}
      >
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.dayLabel}>{getDayLabel(today)}</Text>
          <Text style={styles.dateLabel}>
            {today.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}
          </Text>
        </View>

        {/* Summary pills */}
        {!loading && (
          <View style={styles.pillRow}>
            <View style={[styles.pill, { backgroundColor: '#F3E8FF' }]}>
              <Calendar size={14} color="#7C3AED" />
              <Text style={[styles.pillText, { color: '#7C3AED' }]}>{upcoming.length} appointment{upcoming.length !== 1 ? 's' : ''}</Text>
            </View>
            <View style={[styles.pill, { backgroundColor: '#DCFCE7' }]}>
              <CheckCircle2 size={14} color="#16A34A" />
              <Text style={[styles.pillText, { color: '#16A34A' }]}>{confirmed.length} confirmed</Text>
            </View>
          </View>
        )}

        {/* Loading */}
        {loading && (
          <View style={styles.center}>
            <ActivityIndicator size="large" color="#7C3AED" />
            <Text style={styles.loadingText}>Loading schedule…</Text>
          </View>
        )}

        {/* Error */}
        {error && !loading && (
          <View style={styles.errorCard}>
            <AlertCircle size={20} color="#DC2626" />
            <Text style={styles.errorText}>{error}</Text>
          </View>
        )}

        {/* Appointment list */}
        {!loading && appointments.length === 0 && !error && (
          <View style={styles.emptyState}>
            <Calendar size={48} color="#D1D5DB" />
            <Text style={styles.emptyTitle}>No appointments today</Text>
            <Text style={styles.emptyDesc}>Enjoy the free time or add a walk-in booking.</Text>
          </View>
        )}

        {!loading && appointments.map((appt) => {
          const status = STATUS_CONFIG[appt.status] ?? STATUS_CONFIG.pending;
          return (
            <TouchableOpacity
              key={appt.id}
              style={styles.apptCard}
              onPress={() => navigation?.navigate?.('BookingDetail', { id: appt.id })}
              activeOpacity={0.7}
            >
              {/* Time column */}
              <View style={styles.timeCol}>
                <Text style={styles.timeStart}>{formatTime(appt.startTime)}</Text>
                <View style={styles.timeLine} />
                <Text style={styles.timeEnd}>{formatTime(appt.endTime)}</Text>
              </View>

              {/* Detail column */}
              <View style={styles.detailCol}>
                <View style={styles.apptRow}>
                  <Text style={styles.clientName} numberOfLines={1}>{appt.clientName}</Text>
                  <View style={[styles.statusBadge, { backgroundColor: status.bg }]}>
                    <Text style={[styles.statusText, { color: status.color }]}>{status.label}</Text>
                  </View>
                </View>
                <Text style={styles.serviceName} numberOfLines={1}>{appt.serviceName}</Text>
                {appt.staffName && (
                  <View style={styles.staffRow}>
                    <User size={12} color="#9CA3AF" />
                    <Text style={styles.staffText}>{appt.staffName}</Text>
                  </View>
                )}
              </View>

              <ChevronRight size={16} color="#D1D5DB" />
            </TouchableOpacity>
          );
        })}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8F8FA' },
  content: { padding: 20, paddingBottom: 40 },
  header: { marginBottom: 16 },
  dayLabel: { fontSize: 28, fontWeight: '800', color: '#111120', lineHeight: 34 },
  dateLabel: { fontSize: 14, color: '#66667A', marginTop: 2 },
  pillRow: { flexDirection: 'row', gap: 8, marginBottom: 20 },
  pill: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingHorizontal: 12, paddingVertical: 6, borderRadius: 100 },
  pillText: { fontSize: 13, fontWeight: '600' },
  center: { alignItems: 'center', paddingVertical: 48 },
  loadingText: { color: '#9CA3AF', marginTop: 12, fontSize: 14 },
  errorCard: { flexDirection: 'row', alignItems: 'center', gap: 10, backgroundColor: '#FEE2E2', borderRadius: 12, padding: 14, marginBottom: 16 },
  errorText: { color: '#DC2626', fontSize: 14, flex: 1 },
  emptyState: { alignItems: 'center', paddingVertical: 60 },
  emptyTitle: { fontSize: 18, fontWeight: '700', color: '#374151', marginTop: 16, marginBottom: 6 },
  emptyDesc: { fontSize: 14, color: '#9CA3AF', textAlign: 'center' },
  apptCard: {
    flexDirection: 'row', alignItems: 'center', gap: 12,
    backgroundColor: '#fff', borderRadius: 16, padding: 16, marginBottom: 10,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 6, elevation: 2,
  },
  timeCol: { alignItems: 'center', width: 48 },
  timeStart: { fontSize: 12, fontWeight: '700', color: '#7C3AED' },
  timeLine: { width: 1, height: 16, backgroundColor: '#E5E7EB', marginVertical: 3 },
  timeEnd: { fontSize: 12, color: '#9CA3AF' },
  detailCol: { flex: 1, gap: 2 },
  apptRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  clientName: { fontSize: 15, fontWeight: '700', color: '#111120', flex: 1 },
  serviceName: { fontSize: 13, color: '#6B7280' },
  staffRow: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 2 },
  staffText: { fontSize: 12, color: '#9CA3AF' },
  statusBadge: { paddingHorizontal: 8, paddingVertical: 2, borderRadius: 100 },
  statusText: { fontSize: 11, fontWeight: '700' },
});
