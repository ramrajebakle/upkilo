import React, { useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, SafeAreaView, ScrollView,
  RefreshControl, TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { User, Phone, Mail, ChevronRight, Users, AlertCircle } from 'lucide-react-native';
import { useFocusEffect } from '@react-navigation/native';
import { apiClient, unwrapList } from '../api/apiClient';

interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  role: string;
  isActive: boolean;
  color?: string;
  servicesCount?: number;
  bookingsToday?: number;
}

const ROLE_LABELS: Record<string, string> = {
  tenant_owner: 'Owner',
  manager: 'Manager',
  staff: 'Staff',
  receptionist: 'Receptionist',
};

const AVATAR_COLORS = ['#7C3AED', '#2563EB', '#16A34A', '#D97706', '#DC2626', '#0891B2', '#9333EA'];

function getAvatarColor(id: string): string {
  let hash = 0;
  for (let i = 0; i < id.length; i++) hash = id.charCodeAt(i) + ((hash << 5) - hash);
  return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
}

function initials(first: string, last: string): string {
  return `${first?.[0] ?? ''}${last?.[0] ?? ''}`.toUpperCase();
}

export function TeamScreen({ navigation }: { navigation?: any }) {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStaff = useCallback(async () => {
    try {
      const res = await apiClient.get('/staff?pageSize=100&isActive=true');
      setStaff(unwrapList(res.data));
      setError(null);
    } catch {
      setError('Could not load team members');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { fetchStaff(); }, [fetchStaff]));

  const onRefresh = () => { setRefreshing(true); fetchStaff(); };

  const activeCount = staff.filter(s => s.isActive).length;

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#7C3AED" />}
      >
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>Team</Text>
          {!loading && (
            <View style={styles.countPill}>
              <Users size={13} color="#7C3AED" />
              <Text style={styles.countText}>{activeCount} active</Text>
            </View>
          )}
        </View>

        {/* Loading */}
        {loading && (
          <View style={styles.center}>
            <ActivityIndicator size="large" color="#7C3AED" />
            <Text style={styles.loadingText}>Loading team…</Text>
          </View>
        )}

        {/* Error */}
        {error && !loading && (
          <View style={styles.errorCard}>
            <AlertCircle size={20} color="#DC2626" />
            <Text style={styles.errorText}>{error}</Text>
          </View>
        )}

        {/* Empty */}
        {!loading && !error && staff.length === 0 && (
          <View style={styles.emptyState}>
            <Users size={48} color="#D1D5DB" />
            <Text style={styles.emptyTitle}>No team members yet</Text>
            <Text style={styles.emptyDesc}>Add staff from the web dashboard to see them here.</Text>
          </View>
        )}

        {/* Staff list */}
        {!loading && staff.map((member) => {
          const color = member.color ?? getAvatarColor(member.id);
          const roleLabel = ROLE_LABELS[member.role] ?? member.role;
          return (
            <TouchableOpacity
              key={member.id}
              style={styles.card}
              onPress={() => navigation?.navigate?.('StaffDetail', { id: member.id })}
              activeOpacity={0.7}
            >
              {/* Avatar */}
              <View style={[styles.avatar, { backgroundColor: color }]}>
                <Text style={styles.avatarText}>{initials(member.firstName, member.lastName)}</Text>
              </View>

              {/* Info */}
              <View style={styles.info}>
                <View style={styles.nameRow}>
                  <Text style={styles.name} numberOfLines={1}>
                    {member.firstName} {member.lastName}
                  </Text>
                  <View style={[styles.roleBadge, !member.isActive && styles.roleBadgeInactive]}>
                    <Text style={[styles.roleText, !member.isActive && styles.roleTextInactive]}>
                      {member.isActive ? roleLabel : 'Inactive'}
                    </Text>
                  </View>
                </View>

                {member.email && (
                  <View style={styles.metaRow}>
                    <Mail size={12} color="#9CA3AF" />
                    <Text style={styles.metaText} numberOfLines={1}>{member.email}</Text>
                  </View>
                )}

                {(member.bookingsToday !== undefined || member.servicesCount !== undefined) && (
                  <View style={styles.statsRow}>
                    {member.bookingsToday !== undefined && (
                      <Text style={styles.statChip}>{member.bookingsToday} today</Text>
                    )}
                    {member.servicesCount !== undefined && (
                      <Text style={styles.statChip}>{member.servicesCount} service{member.servicesCount !== 1 ? 's' : ''}</Text>
                    )}
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
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 },
  title: { fontSize: 28, fontWeight: '800', color: '#111120' },
  countPill: { flexDirection: 'row', alignItems: 'center', gap: 6, backgroundColor: '#F3E8FF', paddingHorizontal: 12, paddingVertical: 6, borderRadius: 100 },
  countText: { fontSize: 13, fontWeight: '600', color: '#7C3AED' },
  center: { alignItems: 'center', paddingVertical: 48 },
  loadingText: { color: '#9CA3AF', marginTop: 12, fontSize: 14 },
  errorCard: { flexDirection: 'row', alignItems: 'center', gap: 10, backgroundColor: '#FEE2E2', borderRadius: 12, padding: 14, marginBottom: 16 },
  errorText: { color: '#DC2626', fontSize: 14, flex: 1 },
  emptyState: { alignItems: 'center', paddingVertical: 60 },
  emptyTitle: { fontSize: 18, fontWeight: '700', color: '#374151', marginTop: 16, marginBottom: 6 },
  emptyDesc: { fontSize: 14, color: '#9CA3AF', textAlign: 'center', maxWidth: 260 },
  card: {
    flexDirection: 'row', alignItems: 'center', gap: 14,
    backgroundColor: '#fff', borderRadius: 16, padding: 16, marginBottom: 10,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 6, elevation: 2,
  },
  avatar: { width: 48, height: 48, borderRadius: 24, alignItems: 'center', justifyContent: 'center', flexShrink: 0 },
  avatarText: { color: '#fff', fontSize: 16, fontWeight: '800' },
  info: { flex: 1, gap: 3, minWidth: 0 },
  nameRow: { flexDirection: 'row', alignItems: 'center', gap: 8, justifyContent: 'space-between' },
  name: { fontSize: 15, fontWeight: '700', color: '#111120', flex: 1 },
  roleBadge: { backgroundColor: '#EDE9FE', paddingHorizontal: 8, paddingVertical: 2, borderRadius: 100, flexShrink: 0 },
  roleBadgeInactive: { backgroundColor: '#F3F4F6' },
  roleText: { fontSize: 11, fontWeight: '700', color: '#7C3AED' },
  roleTextInactive: { color: '#9CA3AF' },
  metaRow: { flexDirection: 'row', alignItems: 'center', gap: 5 },
  metaText: { fontSize: 12, color: '#6B7280', flex: 1 },
  statsRow: { flexDirection: 'row', gap: 6, marginTop: 2 },
  statChip: { fontSize: 11, color: '#6B7280', backgroundColor: '#F3F4F6', paddingHorizontal: 8, paddingVertical: 2, borderRadius: 100 },
});
