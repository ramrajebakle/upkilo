/**
 * MK7: Consumer-facing app — Discovery screen.
 * End-consumers can search and browse nearby service businesses.
 */
import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, TextInput, FlatList, TouchableOpacity,
  StyleSheet, Alert, Animated,
} from 'react-native';
import { apiClient } from '../../api/apiClient';
import { useTablet } from '../../hooks/useTablet';

interface Business {
  id: string;
  name: string;
  category: string;
  rating: number;
  reviewCount: number;
  distanceKm?: number;
  nextSlot?: string;
  slug: string;
}

function SkeletonCard() {
  const opacity = React.useRef(new Animated.Value(0.4)).current;
  React.useEffect(() => {
    Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, { toValue: 1, duration: 700, useNativeDriver: true }),
        Animated.timing(opacity, { toValue: 0.4, duration: 700, useNativeDriver: true }),
      ])
    ).start();
  }, [opacity]);
  return (
    <Animated.View style={[styles.card, { opacity }]}>
      <View style={[styles.skeletonLine, { width: '60%', height: 16, marginBottom: 8 }]} />
      <View style={[styles.skeletonLine, { width: '35%', height: 12, marginBottom: 16 }]} />
      <View style={{ flexDirection: 'row', gap: 12 }}>
        <View style={[styles.skeletonLine, { width: 60, height: 12 }]} />
        <View style={[styles.skeletonLine, { width: 50, height: 12 }]} />
      </View>
    </Animated.View>
  );
}

export default function ConsumerDiscoverScreen({ navigation }: { navigation: any }) {
  const [query, setQuery] = useState('');
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [loading, setLoading] = useState(false);
  const isTablet = useTablet();

  const search = useCallback(async (q: string) => {
    setLoading(true);
    try {
      // Base URL already includes /api/v1 — do not repeat the prefix.
      // Endpoint is marketplace/search (paginated via pageSize).
      const res = await apiClient.get(`/marketplace/search?q=${encodeURIComponent(q)}&pageSize=20`);
      const payload = res.data ?? {};
      // Normalise: API returns { items: [], total: n } — fall back to legacy shapes
      setBusinesses(payload.items ?? payload.businesses ?? payload.data ?? []);
    } catch (err) {
      console.error('[ConsumerDiscoverScreen] search failed:', err);
      Alert.alert('Search failed', 'Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { search(''); }, [search]);

  const renderBusiness = ({ item }: { item: Business }) => (
    <TouchableOpacity
      style={styles.card}
      onPress={() => navigation?.navigate?.('ConsumerBook', { slug: item.slug, name: item.name })}
    >
      <View style={styles.cardHeader}>
        <Text style={styles.businessName}>{item.name}</Text>
        <Text style={styles.category}>{item.category}</Text>
      </View>
      <View style={styles.cardFooter}>
        <Text style={styles.rating}>★ {item.rating.toFixed(1)} ({item.reviewCount})</Text>
        {item.distanceKm != null && (
          <Text style={styles.distance}>{item.distanceKm < 1 ? `${(item.distanceKm * 1000).toFixed(0)}m` : `${item.distanceKm.toFixed(1)}km`} away</Text>
        )}
        {item.nextSlot && <Text style={styles.nextSlot}>Next: {item.nextSlot}</Text>}
      </View>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <View style={styles.searchBar}>
        <TextInput
          style={styles.searchInput}
          placeholder="Search salons, spas, gyms..."
          value={query}
          onChangeText={setQuery}
          onSubmitEditing={() => search(query)}
          returnKeyType="search"
        />
        <TouchableOpacity style={styles.searchBtn} onPress={() => search(query)}>
          <Text style={styles.searchBtnText}>Search</Text>
        </TouchableOpacity>
      </View>

      {loading ? (
        <View style={styles.list}>
          {[1, 2, 3, 4, 5].map((i) => <SkeletonCard key={i} />)}
        </View>
      ) : businesses.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyIcon}>🔍</Text>
          <Text style={styles.emptyText}>No businesses found</Text>
          <Text style={styles.emptySubtext}>Try a different search term or category.</Text>
        </View>
      ) : (
        <FlatList
          data={businesses}
          keyExtractor={item => item.id}
          renderItem={renderBusiness}
          numColumns={isTablet ? 2 : 1}
          key={isTablet ? 'tablet' : 'phone'}
          columnWrapperStyle={isTablet ? { gap: 12 } : undefined}
          contentContainerStyle={styles.list}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  skeletonLine: { backgroundColor: '#E5E7EB', borderRadius: 6 },
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  searchBar: { flexDirection: 'row', padding: 12, gap: 8, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#E5E7EB' },
  searchInput: { flex: 1, borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 8, fontSize: 15 },
  searchBtn: { backgroundColor: '#7C3AED', borderRadius: 8, paddingHorizontal: 16, minHeight: 44, justifyContent: 'center' },
  searchBtnText: { color: '#fff', fontWeight: '600', fontSize: 14 },
  loader: { flex: 1 },
  list: { padding: 16, gap: 12 },
  card: { flex: 1, backgroundColor: '#fff', borderRadius: 12, padding: 16, shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 4, elevation: 2 },
  cardHeader: { marginBottom: 8 },
  businessName: { fontSize: 17, fontWeight: '700', color: '#111827' },
  category: { fontSize: 13, color: '#6B7280', marginTop: 2 },
  cardFooter: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  rating: { fontSize: 13, color: '#F59E0B', fontWeight: '600' },
  distance: { fontSize: 12, color: '#6B7280' },
  nextSlot: { fontSize: 12, color: '#10B981', fontWeight: '500', marginLeft: 'auto' },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 48 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 18, fontWeight: '600', color: '#374151', marginBottom: 8 },
  emptySubtext: { fontSize: 14, color: '#6B7280', textAlign: 'center' },
});
