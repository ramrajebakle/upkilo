/**
 * MK7: Consumer-facing app — Loyalty passport screen.
 * Shows the consumer's points across all Upkilo businesses, tier status, and rewards.
 */
import React, { useEffect, useState } from 'react';
import {
  View, Text, ScrollView, TouchableOpacity, StyleSheet, ActivityIndicator,
} from 'react-native';
import { apiClient } from '../../api/apiClient';

interface LoyaltyPassport {
  totalPoints: number;
  tier: 'Bronze' | 'Silver' | 'Gold' | 'Platinum';
  nextTierPoints: number;
  businesses: Array<{
    name: string;
    points: number;
    visitsCount: number;
    lastVisit: string;
  }>;
  rewards: Array<{
    id: string;
    title: string;
    pointsCost: number;
    description: string;
    available: boolean;
  }>;
}

const TIER_COLORS: Record<string, string> = {
  Bronze: '#CD7F32',
  Silver: '#C0C0C0',
  Gold: '#FFD700',
  Platinum: '#E5E4E2',
};

export default function ConsumerLoyaltyScreen({ route }: { route: any }) {
  const clientEmail = route?.params?.clientEmail ?? '';
  const [passport, setPassport] = useState<LoyaltyPassport | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Base URL already includes /api/v1 — do not repeat the prefix.
    apiClient.get(`/marketplace/loyalty/${encodeURIComponent(clientEmail)}`)
      .then(res => setPassport(res.data?.passport ?? res.data))
      .catch(() => null)
      .finally(() => setLoading(false));
  }, [clientEmail]);

  if (loading) return <ActivityIndicator style={styles.loader} size="large" color="#3B82F6" />;

  const tierColor = TIER_COLORS[passport?.tier ?? 'Bronze'];
  const progressPct = passport
    ? Math.min(100, (passport.totalPoints / (passport.totalPoints + passport.nextTierPoints)) * 100)
    : 0;

  return (
    <ScrollView style={styles.container}>
      {/* Tier card */}
      <View style={[styles.tierCard, { backgroundColor: tierColor + '22' }]}>
        <View style={[styles.tierBadge, { backgroundColor: tierColor }]}>
          <Text style={styles.tierText}>{passport?.tier ?? 'Bronze'}</Text>
        </View>
        <Text style={styles.points}>{passport?.totalPoints?.toLocaleString() ?? 0} pts</Text>
        <Text style={styles.nextTier}>
          {passport ? `${passport.nextTierPoints} pts to next tier` : 'Earn points by booking services'}
        </Text>
        {/* Progress bar */}
        <View style={styles.progressBar}>
          <View style={[styles.progressFill, { width: `${progressPct}%` as any, backgroundColor: tierColor }]} />
        </View>
      </View>

      {/* Businesses */}
      {(passport?.businesses?.length ?? 0) > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Your Points</Text>
          {passport!.businesses.map((b, i) => (
            <View key={i} style={styles.businessRow}>
              <View style={{ flex: 1 }}>
                <Text style={styles.businessName}>{b.name}</Text>
                <Text style={styles.visitInfo}>{b.visitsCount} visits · Last: {new Date(b.lastVisit).toLocaleDateString()}</Text>
              </View>
              <Text style={styles.businessPoints}>{b.points} pts</Text>
            </View>
          ))}
        </View>
      )}

      {/* Rewards */}
      {(passport?.rewards?.length ?? 0) > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Available Rewards</Text>
          {passport!.rewards.map(r => (
            <View key={r.id} style={[styles.rewardCard, !r.available && styles.rewardCardDisabled]}>
              <View style={{ flex: 1 }}>
                <Text style={styles.rewardTitle}>{r.title}</Text>
                <Text style={styles.rewardDesc}>{r.description}</Text>
              </View>
              <TouchableOpacity
                style={[styles.redeemBtn, !r.available && styles.redeemBtnDisabled]}
                disabled={!r.available}
              >
                <Text style={styles.redeemBtnText}>{r.pointsCost} pts</Text>
              </TouchableOpacity>
            </View>
          ))}
        </View>
      )}

      {!passport && (
        <View style={styles.empty}>
          <Text style={styles.emptyIcon}>🏅</Text>
          <Text style={styles.emptyText}>No loyalty data yet</Text>
          <Text style={styles.emptySubtext}>Book services at Upkilo businesses to start earning points.</Text>
        </View>
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  loader: { flex: 1 },
  tierCard: { margin: 16, borderRadius: 16, padding: 20, alignItems: 'center' },
  tierBadge: { paddingHorizontal: 16, paddingVertical: 6, borderRadius: 20, marginBottom: 8 },
  tierText: { color: '#fff', fontWeight: '700', fontSize: 14 },
  points: { fontSize: 32, fontWeight: '800', color: '#111827' },
  nextTier: { fontSize: 13, color: '#6B7280', marginTop: 4 },
  progressBar: { width: '100%', height: 6, backgroundColor: '#E5E7EB', borderRadius: 3, marginTop: 12, overflow: 'hidden' },
  progressFill: { height: 6, borderRadius: 3 },
  section: { margin: 16, marginTop: 0, backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  sectionTitle: { fontSize: 16, fontWeight: '700', color: '#111827', marginBottom: 12 },
  businessRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#F3F4F6' },
  businessName: { fontSize: 14, fontWeight: '600', color: '#111827' },
  visitInfo: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  businessPoints: { fontSize: 15, fontWeight: '700', color: '#3B82F6' },
  rewardCard: { flexDirection: 'row', alignItems: 'center', paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: '#F3F4F6' },
  rewardCardDisabled: { opacity: 0.5 },
  rewardTitle: { fontSize: 14, fontWeight: '600', color: '#111827' },
  rewardDesc: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  redeemBtn: { backgroundColor: '#3B82F6', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 6 },
  redeemBtnDisabled: { backgroundColor: '#D1D5DB' },
  redeemBtnText: { color: '#fff', fontWeight: '600', fontSize: 13 },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 48 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 18, fontWeight: '600', color: '#374151', marginBottom: 8 },
  emptySubtext: { fontSize: 14, color: '#6B7280', textAlign: 'center' },
});
