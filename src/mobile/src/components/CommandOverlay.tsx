import React, { useState, useCallback, useRef } from 'react';
import { View, Text, StyleSheet, Modal, TextInput, TouchableOpacity, FlatList, ActivityIndicator } from 'react-native';
import { Search, X, ArrowRight } from 'lucide-react-native';
import { apiClient } from '../api/apiClient';

interface SearchResult {
  id: string;
  title: string;
  subtitle?: string;
  type: 'booking' | 'client' | 'service' | 'action';
  screen?: string;
  params?: Record<string, string>;
}

interface CommandOverlayProps {
  visible: boolean;
  onClose: () => void;
  navigation?: any;
}

export function CommandOverlay({ visible, onClose, navigation }: CommandOverlayProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleQueryChange = useCallback((text: string) => {
    setQuery(text);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (!text.trim()) {
      setResults([]);
      return;
    }
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        // Base URL includes /api/v1
        const res = await apiClient.get(`/search?q=${encodeURIComponent(text.trim())}&limit=10`);
        const data = res.data ?? {};
        setResults(data.items ?? data.results ?? []);
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 300);
  }, []);

  const handleSelect = (item: SearchResult) => {
    if (item.screen && navigation) {
      try {
        navigation.navigate(item.screen, item.params);
      } catch {}
    }
    setQuery('');
    setResults([]);
    onClose();
  };

  const handleClose = () => {
    setQuery('');
    setResults([]);
    onClose();
  };

  const renderItem = ({ item }: { item: SearchResult }) => (
    <TouchableOpacity
      style={styles.item}
      onPress={() => handleSelect(item)}
      accessibilityLabel={item.title}
      accessibilityRole="button"
    >
      <View style={styles.itemContent}>
        <Text style={styles.itemText}>{item.title}</Text>
        {item.subtitle && <Text style={styles.itemSubtext}>{item.subtitle}</Text>}
      </View>
      <ArrowRight size={16} color="#9999B0" />
    </TouchableOpacity>
  );

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={handleClose}>
      <TouchableOpacity style={styles.overlay} activeOpacity={1} onPress={handleClose}>
        <TouchableOpacity style={styles.palette} activeOpacity={1} onPress={() => {}}>
          <View style={styles.searchContainer}>
            <Search size={20} color="#999" />
            <TextInput
              style={styles.input}
              placeholder="Search clients, bookings, services..."
              placeholderTextColor="#999"
              autoFocus
              value={query}
              onChangeText={handleQueryChange}
              returnKeyType="search"
              accessibilityLabel="Search"
            />
            {loading ? (
              <ActivityIndicator size="small" color="#7C3AED" />
            ) : (
              <TouchableOpacity onPress={handleClose} accessibilityLabel="Close search" accessibilityRole="button">
                <X size={20} color="#666" />
              </TouchableOpacity>
            )}
          </View>

          {results.length > 0 && (
            <FlatList
              data={results}
              keyExtractor={(item) => item.id}
              renderItem={renderItem}
              style={styles.resultsList}
              keyboardShouldPersistTaps="always"
            />
          )}

          {!query && results.length === 0 && (
            <View style={styles.hint}>
              <Text style={styles.hintText}>Start typing to search across your business data</Text>
            </View>
          )}

          {query && !loading && results.length === 0 && (
            <View style={styles.hint}>
              <Text style={styles.hintText}>No results for "{query}"</Text>
            </View>
          )}
        </TouchableOpacity>
      </TouchableOpacity>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    paddingTop: 60,
    paddingHorizontal: 16,
  },
  palette: {
    backgroundColor: '#fff',
    borderRadius: 16,
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 24,
    elevation: 10,
    maxHeight: '70%',
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#F0F0F4',
    gap: 12,
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: '#111120',
  },
  resultsList: {
    maxHeight: 360,
  },
  item: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#F8F8FA',
    gap: 12,
  },
  itemContent: { flex: 1 },
  itemText: { fontSize: 15, color: '#333344' },
  itemSubtext: { fontSize: 12, color: '#9999B0', marginTop: 2 },
  hint: { padding: 20, alignItems: 'center' },
  hintText: { fontSize: 14, color: '#9999B0', textAlign: 'center' },
});
