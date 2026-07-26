import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, StyleSheet, SafeAreaView, TouchableOpacity, ScrollView, RefreshControl, Alert, ActivityIndicator } from 'react-native';
import { Sparkles, Command, AlertCircle } from 'lucide-react-native';
import { getLocales } from 'expo-localization';
import { useNavigation } from '@react-navigation/native';
import { CommandOverlay } from '../components/CommandOverlay';
import { apiClient } from '../api/apiClient';

interface DashboardStats {
  tasksToday: number;
  weeklyRevenue: number;
  currency: string;
  aiBriefing: string | null;
}

function formatCurrency(amount: number, currency = 'USD'): string {
  const locale = getLocales()[0]?.languageTag ?? 'en-US';
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(amount);
}

export function DashboardScreen() {
  const navigation = useNavigation();
  const [commandVisible, setCommandVisible] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [fetchError, setFetchError] = useState(false);
  const [draftingOutreach, setDraftingOutreach] = useState(false);
  const [stats, setStats] = useState<DashboardStats>({
    tasksToday: 0,
    weeklyRevenue: 0,
    currency: 'USD',
    aiBriefing: null,
  });
  const [userName, setUserName] = useState('');

  const fetchStats = useCallback(async () => {
    try {
      setFetchError(false);
      // Base URL already includes /api/v1 — do not repeat it in the path.
      // /dashboard/stats carries no currency, so read the tenant's configured
      // currency from business settings rather than assuming one.
      const [statsRes, settingsRes] = await Promise.allSettled([
        apiClient.get('/dashboard/stats'),
        apiClient.get('/settings/business'),
      ]);

      if (statsRes.status === 'rejected') {
        setFetchError(true);
        return;
      }

      const d = statsRes.value.data ?? {};
      const currency =
        settingsRes.status === 'fulfilled'
          ? settingsRes.value.data?.currency ?? 'USD'
          : 'USD';

      setStats({
        tasksToday: d.todayBookings ?? d.tasksToday ?? 0,
        weeklyRevenue: d.revenueThisMonth ?? d.weeklyRevenue ?? 0,
        currency,
        aiBriefing: d.aiBriefing ?? null,
      });
      setUserName(d.userName ?? '');
    } catch {
      setFetchError(true);
    }
  }, []);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await fetchStats();
    setRefreshing(false);
  }, [fetchStats]);

  const greeting = (() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  })();

  const dateLabel = new Date().toLocaleDateString(
    getLocales()[0]?.languageTag ?? 'en',
    { weekday: 'long', hour: 'numeric', minute: '2-digit' }
  );

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#7C3AED']} />
        }
      >
        {fetchError && (
          <View style={styles.errorBanner}>
            <AlertCircle size={16} color="#EF4444" />
            <Text style={styles.errorText}>Failed to load dashboard data. Pull down to retry.</Text>
          </View>
        )}
        <View style={styles.header}>
          <View>
            <Text style={styles.greeting}>{greeting}{userName ? `, ${userName}` : ''}.</Text>
            <Text style={styles.date}>{dateLabel}</Text>
          </View>
          <TouchableOpacity style={styles.searchButton} onPress={() => setCommandVisible(true)}>
            <Command size={20} color="#111120" />
          </TouchableOpacity>
        </View>

        <View style={styles.statsRow}>
          <View style={styles.statBox}>
            <Text style={styles.statValue}>{stats.tasksToday}</Text>
            <Text style={styles.statLabel}>Tasks due today</Text>
          </View>
          <View style={styles.statBox}>
            <Text style={styles.statValue}>{formatCurrency(stats.weeklyRevenue, stats.currency)}</Text>
            <Text style={styles.statLabel}>Revenue this week</Text>
          </View>
        </View>

        {stats.aiBriefing && (
          <View style={styles.aiBriefingCard}>
            <View style={styles.aiHeader}>
              <Sparkles size={16} color="#7C3AED" />
              <Text style={styles.aiTitle}>AI BRIEFING</Text>
            </View>
            <Text style={styles.aiMessage}>{`"${stats.aiBriefing}"`}</Text>
            <Text style={styles.aiPrompt}>Want me to draft outreach?</Text>
            <View style={styles.aiActions}>
              <TouchableOpacity
                style={styles.btnPrimary}
                disabled={draftingOutreach}
                onPress={async () => {
                  setDraftingOutreach(true);
                  try {
                    // There is no /ai/draft-outreach route; copy generation goes through
                    // /ai/copywriting, which returns the generated text rather than saving it.
                    const res = await apiClient.post('/ai/copywriting', {
                      type: 'email',
                      businessType: 'service',
                      tone: 'friendly',
                      keyPoints: stats.aiBriefing ? [stats.aiBriefing] : [],
                    });
                    const draft = res.data?.content ?? res.data?.text ?? res.data?.result;
                    Alert.alert(
                      'Draft ready',
                      typeof draft === 'string' && draft.trim()
                        ? draft
                        : 'Your outreach draft has been generated.'
                    );
                  } catch {
                    Alert.alert('Error', 'Could not draft outreach. Please try again.');
                  } finally {
                    setDraftingOutreach(false);
                  }
                }}
                accessibilityLabel="Draft outreach message"
                accessibilityRole="button"
              >
                {draftingOutreach ? (
                  <ActivityIndicator size="small" color="#fff" />
                ) : (
                  <Text style={styles.btnPrimaryText}>Yes, draft it</Text>
                )}
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.btnSecondary}
                onPress={() => setStats(prev => ({ ...prev, aiBriefing: null }))}
                accessibilityLabel="Dismiss AI briefing"
                accessibilityRole="button"
              >
                <Text style={styles.btnSecondaryText}>No</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}
      </ScrollView>

      {/* `navigation` is required: CommandOverlay.handleSelect is a no-op without it, so
          selecting a search result silently did nothing. */}
      <CommandOverlay
        visible={commandVisible}
        onClose={() => setCommandVisible(false)}
        navigation={navigation}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8F8FA' },
  content: { padding: 20 },
  errorBanner: {
    flexDirection: 'row', alignItems: 'center', gap: 8,
    backgroundColor: '#FEF2F2', borderRadius: 10, padding: 12,
    marginBottom: 16, borderWidth: 1, borderColor: '#FCA5A5',
  },
  errorText: { flex: 1, fontSize: 13, color: '#B91C1C' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 },
  greeting: { fontSize: 24, fontWeight: 'bold', color: '#111120' },
  date: { fontSize: 14, color: '#66667A', marginTop: 4 },
  searchButton: {
    width: 40, height: 40, borderRadius: 20, backgroundColor: '#E4E4EB',
    alignItems: 'center', justifyContent: 'center',
  },
  statsRow: { flexDirection: 'row', gap: 16, marginBottom: 24 },
  statBox: {
    flex: 1, backgroundColor: '#fff', padding: 16, borderRadius: 16,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 2,
  },
  statValue: { fontSize: 24, fontWeight: 'bold', color: '#111120' },
  statLabel: { fontSize: 13, color: '#66667A', marginTop: 4 },
  aiBriefingCard: {
    backgroundColor: '#fff', padding: 24, borderRadius: 20,
    borderWidth: 1, borderColor: '#F3E8FF',
    shadowColor: '#7C3AED', shadowOffset: { width: 0, height: 8 }, shadowOpacity: 0.1, shadowRadius: 24, elevation: 8,
  },
  aiHeader: { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 16 },
  aiTitle: { fontSize: 12, fontWeight: 'bold', color: '#7C3AED', letterSpacing: 0.5 },
  aiMessage: { fontSize: 16, color: '#333344', lineHeight: 24, marginBottom: 12 },
  aiPrompt: { fontSize: 15, fontWeight: '600', color: '#111120', marginBottom: 20 },
  aiActions: { flexDirection: 'row', gap: 12 },
  btnPrimary: { backgroundColor: '#7C3AED', paddingVertical: 12, paddingHorizontal: 20, borderRadius: 10, flex: 1, alignItems: 'center' },
  btnPrimaryText: { color: '#fff', fontWeight: '600', fontSize: 15 },
  btnSecondary: { backgroundColor: '#F0F0F4', paddingVertical: 12, paddingHorizontal: 20, borderRadius: 10, flex: 1, alignItems: 'center' },
  btnSecondaryText: { color: '#333344', fontWeight: '600', fontSize: 15 },
});
