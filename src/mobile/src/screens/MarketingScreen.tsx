/**
 * M5: Marketing campaigns page in mobile.
 * Shows active campaigns, email blast stats, and a quick "Send Campaign" action.
 */
import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { apiClient } from '../api/apiClient';

interface Campaign {
  id: string;
  name: string;
  type: 'email' | 'sms';
  status: 'draft' | 'scheduled' | 'sent' | 'active';
  sentCount: number;
  openRate: number;
  clickRate: number;
  scheduledAt?: string;
  sentAt?: string;
}

export default function MarketingScreen() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadCampaigns = useCallback(async () => {
    try {
      const res = await apiClient.get('/campaigns');
      const body = res.data;
      setCampaigns(Array.isArray(body) ? body : (body?.campaigns ?? body?.data ?? []));
    } catch {
      Alert.alert('Error', 'Failed to load campaigns. Please try again.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { loadCampaigns(); }, [loadCampaigns]);

  const statusColor = (status: Campaign['status']) => ({
    draft: '#6B7280',
    scheduled: '#F59E0B',
    sent: '#3B82F6',
    active: '#10B981',
  }[status] ?? '#6B7280');

  const renderCampaign = ({ item }: { item: Campaign }) => (
    <View style={styles.card}>
      <View style={styles.cardHeader}>
        <Text style={styles.campaignName}>{item.name}</Text>
        <View style={[styles.badge, { backgroundColor: statusColor(item.status) }]}>
          <Text style={styles.badgeText}>{item.status.toUpperCase()}</Text>
        </View>
      </View>

      <Text style={styles.typeText}>{item.type === 'email' ? '📧 Email' : '📱 SMS'}</Text>

      {item.status === 'sent' || item.status === 'active' ? (
        <View style={styles.statsRow}>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{item.sentCount.toLocaleString()}</Text>
            <Text style={styles.statLabel}>Sent</Text>
          </View>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{(item.openRate * 100).toFixed(1)}%</Text>
            <Text style={styles.statLabel}>Open Rate</Text>
          </View>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{(item.clickRate * 100).toFixed(1)}%</Text>
            <Text style={styles.statLabel}>Click Rate</Text>
          </View>
        </View>
      ) : (
        <Text style={styles.scheduledText}>
          {item.scheduledAt ? `Scheduled: ${new Date(item.scheduledAt).toLocaleDateString()}` : 'Draft — not sent yet'}
        </Text>
      )}
    </View>
  );

  if (loading) return <ActivityIndicator style={styles.loader} size="large" color="#3B82F6" />;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Marketing Campaigns</Text>
        <TouchableOpacity
          style={styles.newButton}
          onPress={() => Alert.alert('New Campaign', 'Use the web dashboard to create campaigns with the full editor.')}
        >
          <Text style={styles.newButtonText}>+ New</Text>
        </TouchableOpacity>
      </View>

      {campaigns.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyIcon}>📣</Text>
          <Text style={styles.emptyText}>No campaigns yet</Text>
          <Text style={styles.emptySubtext}>Create your first email or SMS campaign to reach your clients.</Text>
        </View>
      ) : (
        <FlatList
          data={campaigns}
          keyExtractor={item => item.id}
          renderItem={renderCampaign}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); loadCampaigns(); }} />}
          contentContainerStyle={styles.list}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  loader: { flex: 1 },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: 16, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#E5E7EB' },
  title: { fontSize: 20, fontWeight: '700', color: '#111827' },
  newButton: { backgroundColor: '#3B82F6', paddingHorizontal: 14, paddingVertical: 8, borderRadius: 8 },
  newButtonText: { color: '#fff', fontWeight: '600', fontSize: 14 },
  list: { padding: 16, gap: 12 },
  card: { backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  cardHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 },
  campaignName: { fontSize: 16, fontWeight: '600', color: '#111827', flex: 1 },
  badge: { paddingHorizontal: 8, paddingVertical: 3, borderRadius: 6 },
  badgeText: { color: '#fff', fontSize: 10, fontWeight: '700' },
  typeText: { fontSize: 13, color: '#6B7280', marginBottom: 10 },
  statsRow: { flexDirection: 'row', gap: 16 },
  stat: { alignItems: 'center' },
  statValue: { fontSize: 18, fontWeight: '700', color: '#111827' },
  statLabel: { fontSize: 11, color: '#6B7280', marginTop: 2 },
  scheduledText: { fontSize: 13, color: '#6B7280', fontStyle: 'italic' },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 32 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 18, fontWeight: '600', color: '#374151', marginBottom: 8 },
  emptySubtext: { fontSize: 14, color: '#6B7280', textAlign: 'center' },
});
