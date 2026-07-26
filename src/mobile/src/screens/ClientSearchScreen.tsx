import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  ActivityIndicator,
  Alert,
  TextInput,
  Modal,
  ScrollView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { apiClient, unwrapList } from '../api/apiClient';
import { RootStackParamList } from '../../App';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

interface Client {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  lastVisitDate?: string;
  totalBookings?: number;
}

const RECENT_SEARCHES_KEY = 'upkilo_recent_searches';

export function ClientSearchScreen() {
  const navigation = useNavigation<NavProp>();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Client[]>([]);
  const [loading, setLoading] = useState(false);
  const [recentSearches, setRecentSearches] = useState<string[]>([]);
  const [selectedClient, setSelectedClient] = useState<Client | null>(null);
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    loadRecentSearches();
  }, []);

  useEffect(() => {
    if (debounceTimer.current) clearTimeout(debounceTimer.current);
    if (!query.trim()) {
      setResults([]);
      setLoading(false);
      return;
    }
    debounceTimer.current = setTimeout(() => {
      search(query);
    }, 300);
    return () => { if (debounceTimer.current) clearTimeout(debounceTimer.current); };
  }, [query]);

  const loadRecentSearches = async () => {
    try {
      const stored = await AsyncStorage.getItem(RECENT_SEARCHES_KEY);
      if (stored) setRecentSearches(JSON.parse(stored));
    } catch {}
  };

  const saveRecentSearch = async (q: string) => {
    try {
      const updated = [q, ...recentSearches.filter(s => s !== q)].slice(0, 5);
      setRecentSearches(updated);
      await AsyncStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(updated));
    } catch {}
  };

  const search = async (q: string) => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/clients/search?q=${encodeURIComponent(q)}&limit=20`);
      const data = unwrapList<Client>(res.data);
      setResults(data);
      await saveRecentSearch(q);
    } catch {
      Alert.alert('Error', 'Search failed');
    } finally {
      setLoading(false);
    }
  };

  const initials = (client: Client) =>
    (client.firstName.charAt(0) + client.lastName.charAt(0)).toUpperCase();

  const renderClient = ({ item }: { item: Client }) => (
    <TouchableOpacity style={styles.row} onPress={() => setSelectedClient(item)}>
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{initials(item)}</Text>
      </View>
      <View style={styles.rowInfo}>
        <Text style={styles.clientName}>{item.firstName} {item.lastName}</Text>
        <Text style={styles.clientEmail}>{item.email}</Text>
        {item.lastVisitDate && (
          <Text style={styles.lastVisit}>Last visit: {new Date(item.lastVisitDate).toLocaleDateString()}</Text>
        )}
      </View>
    </TouchableOpacity>
  );

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TextInput
          style={styles.searchInput}
          value={query}
          onChangeText={setQuery}
          placeholder="Search clients by name or email..."
          placeholderTextColor="#999"
          autoFocus
          returnKeyType="search"
        />
        {query.length > 0 && (
          <TouchableOpacity onPress={() => { setQuery(''); setResults([]); }}>
            <Text style={styles.clearBtn}>✕</Text>
          </TouchableOpacity>
        )}
      </View>

      {loading && <ActivityIndicator style={{ marginTop: 20 }} color="#007AFF" />}

      {!query && recentSearches.length > 0 && (
        <View style={styles.recentSection}>
          <Text style={styles.recentTitle}>Recent Searches</Text>
          {recentSearches.map(s => (
            <TouchableOpacity key={s} style={styles.recentItem} onPress={() => setQuery(s)}>
              <Text style={styles.recentText}>{s}</Text>
            </TouchableOpacity>
          ))}
        </View>
      )}

      {!loading && query && results.length === 0 && (
        <Text style={styles.empty}>No clients found for "{query}"</Text>
      )}

      <FlatList
        data={results}
        keyExtractor={item => item.id}
        renderItem={renderClient}
        contentContainerStyle={styles.list}
      />

      <Modal visible={!!selectedClient} animationType="slide" presentationStyle="pageSheet">
        {selectedClient && (
          <SafeAreaView style={styles.modal}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{selectedClient.firstName} {selectedClient.lastName}</Text>
              <TouchableOpacity onPress={() => setSelectedClient(null)}>
                <Text style={styles.closeBtn}>Close</Text>
              </TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={styles.modalBody}>
              <View style={styles.bigAvatar}>
                <Text style={styles.bigAvatarText}>{initials(selectedClient)}</Text>
              </View>
              {[
                { label: 'Email', value: selectedClient.email },
                { label: 'Phone', value: selectedClient.phone || 'Not provided' },
                { label: 'Total Bookings', value: String(selectedClient.totalBookings ?? 0) },
                { label: 'Last Visit', value: selectedClient.lastVisitDate ? new Date(selectedClient.lastVisitDate).toLocaleDateString() : 'No visits yet' },
              ].map(item => (
                <View key={item.label} style={styles.detailRow}>
                  <Text style={styles.detailLabel}>{item.label}</Text>
                  <Text style={styles.detailValue}>{item.value}</Text>
                </View>
              ))}
              <TouchableOpacity
                style={styles.bookBtn}
                onPress={() => {
                  setSelectedClient(null);
                  navigation.navigate('ClientBooking');
                }}
              >
                <Text style={styles.bookBtnText}>Book for Client</Text>
              </TouchableOpacity>
            </ScrollView>
          </SafeAreaView>
        )}
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 16, paddingTop: 16, paddingBottom: 8 },
  searchInput: { flex: 1, backgroundColor: '#F2F2F7', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 15, color: '#111' },
  clearBtn: { color: '#888', fontSize: 18, marginLeft: 10, padding: 4 },
  recentSection: { padding: 16 },
  recentTitle: { fontSize: 13, fontWeight: '600', color: '#888', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8 },
  recentItem: { paddingVertical: 10, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  recentText: { fontSize: 15, color: '#007AFF' },
  list: { paddingHorizontal: 16 },
  row: { flexDirection: 'row', alignItems: 'center', paddingVertical: 12, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  avatar: { width: 44, height: 44, borderRadius: 22, backgroundColor: '#007AFF', justifyContent: 'center', alignItems: 'center', marginRight: 14 },
  avatarText: { color: '#fff', fontWeight: '700', fontSize: 16 },
  rowInfo: { flex: 1 },
  clientName: { fontSize: 15, fontWeight: '600', color: '#111' },
  clientEmail: { fontSize: 13, color: '#888', marginTop: 2 },
  lastVisit: { fontSize: 12, color: '#aaa', marginTop: 2 },
  empty: { textAlign: 'center', color: '#888', marginTop: 60, fontSize: 15, paddingHorizontal: 20 },
  modal: { flex: 1, backgroundColor: '#fff' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, borderBottomWidth: 1, borderColor: '#eee' },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#111' },
  closeBtn: { color: '#007AFF', fontWeight: '600', fontSize: 16 },
  modalBody: { padding: 20, alignItems: 'center' },
  bigAvatar: { width: 80, height: 80, borderRadius: 40, backgroundColor: '#007AFF', justifyContent: 'center', alignItems: 'center', marginBottom: 20 },
  bigAvatarText: { color: '#fff', fontWeight: '700', fontSize: 28 },
  detailRow: { flexDirection: 'row', justifyContent: 'space-between', width: '100%', paddingVertical: 12, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  detailLabel: { fontSize: 14, color: '#888' },
  detailValue: { fontSize: 14, fontWeight: '600', color: '#111', flex: 1, textAlign: 'right' },
  bookBtn: { backgroundColor: '#007AFF', borderRadius: 12, paddingVertical: 14, width: '100%', alignItems: 'center', marginTop: 24 },
  bookBtnText: { color: '#fff', fontWeight: '700', fontSize: 16 },
});
