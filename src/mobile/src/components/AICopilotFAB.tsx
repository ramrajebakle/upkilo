import React, { useState, useCallback } from 'react';
import { View, TouchableOpacity, StyleSheet, Modal, Text, ActivityIndicator, ScrollView, Alert } from 'react-native';
import { Sparkles, X, Send } from 'lucide-react-native';
import { apiClient } from '../api/apiClient';

interface AIInsight {
  id: string;
  message: string;
  actionLabel?: string;
  actionType?: string;
}

export function AICopilotFAB() {
  const [isOpen, setIsOpen] = useState(false);
  const [insights, setInsights] = useState<AIInsight[]>([]);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const loadInsights = useCallback(async () => {
    setLoading(true);
    try {
      // Base URL includes /api/v1 — path is relative.
      // /aidashboard/recommendations returns { recommendations: string[] } — advisory text with
      // no programmatic action attached, so these render without an action button.
      // A 403 here means the tenant's plan does not include ai_insights.
      const res = await apiClient.get('/aidashboard/recommendations');
      const list = res.data?.recommendations ?? [];
      setInsights(
        (Array.isArray(list) ? list : []).map((text: unknown, i: number) => ({
          id: `rec-${i}`,
          message: typeof text === 'string' ? text : String((text as { message?: string })?.message ?? ''),
        }))
      );
    } catch {
      setInsights([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleOpen = () => {
    setIsOpen(true);
    loadInsights();
  };

  // Dismiss an insight locally. There is no server-side "apply this recommendation" action —
  // recommendations are advisory text — so this does not call the API.
  const handleAction = (insight: AIInsight) => {
    setInsights((prev) => prev.filter((i) => i.id !== insight.id));
  };

  return (
    <>
      <TouchableOpacity
        style={styles.fab}
        onPress={handleOpen}
        activeOpacity={0.8}
        accessibilityLabel="Open AI Copilot"
        accessibilityRole="button"
      >
        <Sparkles size={24} color="#fff" />
      </TouchableOpacity>

      <Modal visible={isOpen} transparent animationType="slide" onRequestClose={() => setIsOpen(false)}>
        <View style={styles.modalOverlay}>
          <View style={styles.sheet}>
            <View style={styles.header}>
              <View style={styles.titleContainer}>
                <Sparkles size={20} color="#7C3AED" />
                <Text style={styles.title}>AI Copilot</Text>
              </View>
              <TouchableOpacity
                onPress={() => setIsOpen(false)}
                accessibilityLabel="Close AI Copilot"
                accessibilityRole="button"
              >
                <X size={24} color="#666" />
              </TouchableOpacity>
            </View>

            <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
              {loading ? (
                <View style={styles.loaderContainer}>
                  <ActivityIndicator size="large" color="#7C3AED" />
                  <Text style={styles.loaderText}>Fetching insights...</Text>
                </View>
              ) : insights.length === 0 ? (
                <View style={styles.emptyContainer}>
                  <Sparkles size={40} color="#D8B4FE" />
                  <Text style={styles.emptyText}>No insights right now</Text>
                  <Text style={styles.emptySubtext}>Check back later — your AI Copilot will surface recommendations as your business activity grows.</Text>
                </View>
              ) : (
                insights.map((insight) => (
                  <View key={insight.id} style={styles.insightCard}>
                    <Text style={styles.insightTitle}>Insight</Text>
                    <Text style={styles.insightDesc}>{insight.message}</Text>
                    {insight.actionLabel && (
                      <TouchableOpacity
                        style={[styles.actionButton, actionLoading === insight.id && styles.actionButtonLoading]}
                        onPress={() => handleAction(insight)}
                        disabled={actionLoading === insight.id}
                        accessibilityLabel={insight.actionLabel}
                        accessibilityRole="button"
                      >
                        {actionLoading === insight.id ? (
                          <ActivityIndicator size="small" color="#fff" />
                        ) : (
                          <View style={styles.actionRow}>
                            <Send size={13} color="#fff" />
                            <Text style={styles.actionText}>{insight.actionLabel}</Text>
                          </View>
                        )}
                      </TouchableOpacity>
                    )}
                  </View>
                ))
              )}
            </ScrollView>
          </View>
        </View>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  fab: {
    position: 'absolute',
    bottom: 24,
    right: 24,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#7C3AED',
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#7C3AED',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 5,
    zIndex: 600,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.4)',
    justifyContent: 'flex-end',
  },
  sheet: {
    backgroundColor: '#fff',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    padding: 24,
    maxHeight: '75%',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -4 },
    shadowOpacity: 0.1,
    shadowRadius: 12,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20,
  },
  titleContainer: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  title: { fontSize: 18, fontWeight: 'bold', color: '#111120' },
  content: { flex: 1 },
  loaderContainer: { alignItems: 'center', paddingVertical: 40, gap: 12 },
  loaderText: { fontSize: 14, color: '#9999B0' },
  emptyContainer: { alignItems: 'center', paddingVertical: 40, gap: 12 },
  emptyText: { fontSize: 16, fontWeight: '600', color: '#333344' },
  emptySubtext: { fontSize: 13, color: '#9999B0', textAlign: 'center', lineHeight: 20 },
  insightCard: {
    padding: 16,
    backgroundColor: '#F3E8FF',
    borderRadius: 12,
    borderLeftWidth: 4,
    borderLeftColor: '#7C3AED',
    marginBottom: 12,
  },
  insightTitle: {
    fontSize: 11,
    fontWeight: 'bold',
    color: '#7C3AED',
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 6,
  },
  insightDesc: { fontSize: 14, color: '#333344', lineHeight: 20, marginBottom: 12 },
  actionButton: {
    backgroundColor: '#7C3AED',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
    alignSelf: 'flex-start',
    minWidth: 120,
    alignItems: 'center',
  },
  actionButtonLoading: { opacity: 0.7 },
  actionRow: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  actionText: { color: '#fff', fontWeight: '600', fontSize: 13 },
});
