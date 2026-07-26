import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  ActivityIndicator,
  RefreshControl,
  Alert,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient, unwrapList } from '../api/apiClient';
import { RootStackParamList } from '../../App';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

interface Notification {
  id: string;
  title: string;
  message: string;
  createdAt: string;
  isRead: boolean;
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

export function NotificationsScreen() {
  const navigation = useNavigation<NavProp>();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const res = await apiClient.get('/notifications?limit=30');
      const data = unwrapList<Notification>(res.data);
      setNotifications(data);
    } catch {
      Alert.alert('Error', 'Failed to load notifications');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const onRefresh = () => {
    setRefreshing(true);
    load();
  };

  const markRead = async (id: string) => {
    try {
      await apiClient.patch(`/notifications/${id}/read`, {});
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch {
      Alert.alert('Error', 'Failed to mark notification as read');
    }
  };

  const markAllRead = async () => {
    try {
      await apiClient.post('/notifications/read-all', {});
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch {
      Alert.alert('Error', 'Failed to mark all as read');
    }
  };

  const renderItem = ({ item }: { item: Notification }) => (
    <TouchableOpacity style={styles.row} onPress={() => markRead(item.id)}>
      {!item.isRead && <View style={styles.dot} />}
      <View style={[styles.rowContent, item.isRead && styles.rowContentRead]}>
        <View style={styles.rowHeader}>
          <Text style={styles.rowTitle}>{item.title}</Text>
          <Text style={styles.rowTime}>{timeAgo(item.createdAt)}</Text>
        </View>
        <Text style={styles.rowMessage} numberOfLines={2}>
          {item.message.length > 60 ? item.message.slice(0, 60) + '…' : item.message}
        </Text>
      </View>
    </TouchableOpacity>
  );

  if (loading) {
    return <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>;
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Notifications</Text>
        <TouchableOpacity onPress={markAllRead}>
          <Text style={styles.markAllBtn}>Mark All Read</Text>
        </TouchableOpacity>
      </View>
      <FlatList
        data={notifications}
        keyExtractor={item => item.id}
        renderItem={renderItem}
        contentContainerStyle={styles.list}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        ListEmptyComponent={<Text style={styles.empty}>No notifications</Text>}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 20, paddingTop: 16, paddingBottom: 8,
  },
  title: { fontSize: 24, fontWeight: '700', color: '#111' },
  markAllBtn: { color: '#007AFF', fontSize: 14, fontWeight: '600' },
  list: { paddingHorizontal: 16 },
  row: { flexDirection: 'row', alignItems: 'flex-start', paddingVertical: 14, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#007AFF', marginTop: 6, marginRight: 10 },
  rowContent: { flex: 1 },
  rowContentRead: { opacity: 0.6 },
  rowHeader: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  rowTitle: { fontSize: 15, fontWeight: '600', color: '#111', flex: 1 },
  rowTime: { fontSize: 12, color: '#888', marginLeft: 8 },
  rowMessage: { fontSize: 13, color: '#555' },
  empty: { textAlign: 'center', color: '#888', marginTop: 60, fontSize: 15 },
});
