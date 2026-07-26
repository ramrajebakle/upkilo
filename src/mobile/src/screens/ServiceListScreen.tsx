import React, { useState, useEffect } from 'react';
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
  Switch,
  ScrollView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient, unwrapList } from '../api/apiClient';
import { RootStackParamList } from '../../App';
import { money, useTenantCurrency } from '../utils/currency';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

interface Service {
  // Present on the API response; used so each record renders in the
  // currency it was actually billed in rather than assuming dollars.
  currency?: string;
  id: string;
  name: string;
  duration: number;
  price: number;
  category: string;
  description: string;
  isActive: boolean;
}

const CATEGORY_COLORS: Record<string, string> = {
  Hair: '#FF6B6B',
  Nails: '#4ECDC4',
  Spa: '#45B7D1',
  Beauty: '#96CEB4',
  Fitness: '#FFEAA7',
  Wellness: '#DDA0DD',
};

export function ServiceListScreen() {
  // Aggregate figures belong to the tenant, so they render in the tenant's currency.
  const tenantCurrency = useTenantCurrency();
  const navigation = useNavigation<NavProp>();
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalVisible, setModalVisible] = useState(false);
  const [editing, setEditing] = useState<Service | null>(null);
  const [form, setForm] = useState({ name: '', duration: '', price: '', description: '' });

  useEffect(() => {
    loadServices();
  }, []);

  const loadServices = async () => {
    try {
      const res = await apiClient.get('/services');
      setServices(unwrapList<Service>(res.data));
    } catch {
      Alert.alert('Error', 'Failed to load services');
    } finally {
      setLoading(false);
    }
  };

  const openEdit = (service: Service) => {
    setEditing(service);
    setForm({
      name: service.name,
      duration: String(service.duration),
      price: String(service.price),
      description: service.description,
    });
    setModalVisible(true);
  };

  const openAdd = () => {
    setEditing(null);
    setForm({ name: '', duration: '', price: '', description: '' });
    setModalVisible(true);
  };

  const saveService = async () => {
    const payload = {
      name: form.name,
      duration: parseInt(form.duration),
      price: parseFloat(form.price),
      description: form.description,
    };
    try {
      if (editing) {
        await apiClient.put(`/services/${editing.id}`, payload);
        setServices(prev => prev.map(s => s.id === editing.id ? { ...s, ...payload } : s));
      } else {
        const res = await apiClient.post('/services', payload);
        setServices(prev => [...prev, res.data as Service]);
      }
      setModalVisible(false);
    } catch {
      Alert.alert('Error', 'Failed to save service');
    }
  };

  const toggleActive = async (service: Service) => {
    try {
      await apiClient.patch(`/services/${service.id}`, { isActive: !service.isActive });
      setServices(prev => prev.map(s => s.id === service.id ? { ...s, isActive: !s.isActive } : s));
    } catch {
      Alert.alert('Error', 'Failed to update service');
    }
  };

  const getCategoryColor = (category: string) => CATEGORY_COLORS[category] || '#007AFF';

  const renderItem = ({ item }: { item: Service }) => (
    <TouchableOpacity style={styles.card} onPress={() => openEdit(item)}>
      <View style={[styles.categoryBar, { backgroundColor: getCategoryColor(item.category) }]} />
      <View style={styles.cardBody}>
        <View style={styles.cardHeader}>
          <Text style={styles.serviceName}>{item.name}</Text>
          <Switch
            value={item.isActive}
            onValueChange={() => toggleActive(item)}
            trackColor={{ false: '#ddd', true: '#007AFF' }}
          />
        </View>
        <View style={styles.cardDetails}>
          <Text style={styles.detail}>{item.duration} min</Text>
          <Text style={styles.detail}>·</Text>
          <Text style={styles.detail}>{money(item.price, item.currency ?? tenantCurrency)}</Text>
          <Text style={styles.detail}>·</Text>
          <Text style={[styles.category, { color: getCategoryColor(item.category) }]}>{item.category}</Text>
        </View>
      </View>
    </TouchableOpacity>
  );

  if (loading) {
    return <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>;
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Services</Text>
      </View>
      <FlatList
        data={services}
        keyExtractor={item => item.id}
        renderItem={renderItem}
        contentContainerStyle={styles.list}
        ListEmptyComponent={<Text style={styles.empty}>No services found</Text>}
      />
      <TouchableOpacity style={styles.fab} onPress={openAdd}>
        <Text style={styles.fabText}>+ Add</Text>
      </TouchableOpacity>

      <Modal visible={modalVisible} animationType="slide" presentationStyle="pageSheet">
        <SafeAreaView style={styles.modal}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>{editing ? 'Edit Service' : 'Add Service'}</Text>
            <TouchableOpacity onPress={() => setModalVisible(false)}>
              <Text style={styles.closeBtn}>Cancel</Text>
            </TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={styles.modalBody}>
            {([
              { label: 'Name', key: 'name', placeholder: 'Service name' },
              { label: 'Duration (minutes)', key: 'duration', placeholder: '60', numeric: true },
              { label: 'Price ($)', key: 'price', placeholder: '0.00', numeric: true },
              { label: 'Description', key: 'description', placeholder: 'Describe the service' },
            ] as Array<{ label: string; key: keyof typeof form; placeholder: string; numeric?: boolean }>).map(field => (
              <View key={field.key}>
                <Text style={styles.label}>{field.label}</Text>
                <TextInput
                  style={styles.input}
                  value={form[field.key]}
                  onChangeText={v => setForm(f => ({ ...f, [field.key]: v }))}
                  placeholder={field.placeholder}
                  keyboardType={field.numeric ? 'numeric' : 'default'}
                />
              </View>
            ))}
            <TouchableOpacity style={styles.saveBtn} onPress={saveService}>
              <Text style={styles.saveBtnText}>Save Service</Text>
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 8 },
  title: { fontSize: 24, fontWeight: '700', color: '#111' },
  list: { paddingHorizontal: 16 },
  card: { flexDirection: 'row', backgroundColor: '#F8F8F8', borderRadius: 12, marginBottom: 12, overflow: 'hidden' },
  categoryBar: { width: 6 },
  cardBody: { flex: 1, padding: 14 },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  serviceName: { fontSize: 16, fontWeight: '600', color: '#111', flex: 1 },
  cardDetails: { flexDirection: 'row', alignItems: 'center', marginTop: 6, gap: 6 },
  detail: { fontSize: 13, color: '#666' },
  category: { fontSize: 13, fontWeight: '600' },
  fab: { position: 'absolute', bottom: 24, right: 24, backgroundColor: '#007AFF', borderRadius: 24, paddingVertical: 12, paddingHorizontal: 20, shadowColor: '#007AFF', shadowOpacity: 0.4, shadowRadius: 8, elevation: 6 },
  fabText: { color: '#fff', fontWeight: '700', fontSize: 15 },
  empty: { textAlign: 'center', color: '#888', marginTop: 60, fontSize: 15 },
  modal: { flex: 1, backgroundColor: '#fff' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, borderBottomWidth: 1, borderColor: '#eee' },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#111' },
  closeBtn: { color: '#007AFF', fontWeight: '600', fontSize: 16 },
  modalBody: { padding: 20 },
  label: { fontSize: 13, color: '#555', marginBottom: 4, marginTop: 12 },
  input: { borderWidth: 1, borderColor: '#ddd', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 10, fontSize: 15, color: '#111' },
  saveBtn: { backgroundColor: '#007AFF', borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 24 },
  saveBtnText: { color: '#fff', fontWeight: '600', fontSize: 16 },
});
