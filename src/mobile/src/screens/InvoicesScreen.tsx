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
  Modal,
  ScrollView,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient, unwrapList } from '../api/apiClient';
import { RootStackParamList } from '../../App';
import { money, useTenantCurrency } from '../utils/currency';

type NavProp = NativeStackNavigationProp<RootStackParamList>;
type Filter = 'All' | 'Paid' | 'Pending' | 'Overdue';

interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

interface Invoice {
  // Present on the API response; used so each record renders in the
  // currency it was actually billed in rather than assuming dollars.
  currency?: string;
  id: string;
  invoiceNumber: string;
  clientName: string;
  amount: number;
  dueDate: string;
  status: Filter;
  notes?: string;
  lineItems?: InvoiceLineItem[];
}

export function InvoicesScreen() {
  // Aggregate figures belong to the tenant, so they render in the tenant's currency.
  const tenantCurrency = useTenantCurrency();
  const navigation = useNavigation<NavProp>();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<Filter>('All');
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [totalOutstanding, setTotalOutstanding] = useState(0);

  useEffect(() => {
    loadInvoices();
  }, []);

  const loadInvoices = async () => {
    try {
      // Tenant invoices live under /billing/invoices and paginate with page/pageSize.
      const res = await apiClient.get('/billing/invoices?page=1&pageSize=50');
      const data = unwrapList<Invoice>(res.data);
      setInvoices(data);
      const outstanding = data
        .filter(i => i.status !== 'Paid')
        .reduce((sum, i) => sum + i.amount, 0);
      setTotalOutstanding(outstanding);
    } catch {
      Alert.alert('Error', 'Failed to load invoices');
    } finally {
      setLoading(false);
    }
  };

  const openDetail = async (invoice: Invoice) => {
    setSelectedInvoice(invoice);
    setDetailLoading(true);
    try {
      const res = await apiClient.get(`/billing/invoices/${invoice.id}`);
      setSelectedInvoice(res.data as Invoice);
    } catch {
      setSelectedInvoice(invoice);
    } finally {
      setDetailLoading(false);
    }
  };

  const sendReminder = async (id: string) => {
    try {
      await apiClient.post(`/billing/invoices/${id}/send-reminder`, {});
      Alert.alert('Success', 'Reminder sent to client');
    } catch {
      Alert.alert('Error', 'Failed to send reminder');
    }
  };

  const markPaid = async (id: string) => {
    try {
      await apiClient.post(`/billing/invoices/${id}/mark-paid`, {});
      setInvoices(prev => prev.map(i => i.id === id ? { ...i, status: 'Paid' as Filter } : i));
      setSelectedInvoice(prev => prev ? { ...prev, status: 'Paid' as Filter } : prev);
      const updated = invoices.map(i => i.id === id ? { ...i, status: 'Paid' as Filter } : i);
      setTotalOutstanding(updated.filter(i => i.status !== 'Paid').reduce((s, i) => s + i.amount, 0));
    } catch {
      Alert.alert('Error', 'Failed to update invoice');
    }
  };

  const statusColor = (status: string) => {
    if (status === 'Paid') return '#34C759';
    if (status === 'Overdue') return '#FF3B30';
    return '#FF9500';
  };

  const filtered = filter === 'All' ? invoices : invoices.filter(i => i.status === filter);

  const renderItem = ({ item }: { item: Invoice }) => (
    <TouchableOpacity style={styles.row} onPress={() => openDetail(item)}>
      <View style={styles.rowLeft}>
        <Text style={styles.clientName}>{item.clientName}</Text>
        <Text style={styles.invoiceNum}>{item.invoiceNumber}</Text>
        <Text style={styles.dueDate}>Due: {new Date(item.dueDate).toLocaleDateString()}</Text>
      </View>
      <View style={styles.rowRight}>
        <Text style={styles.amount}>{money(item.amount, item.currency ?? tenantCurrency)}</Text>
        <View style={[styles.badge, { backgroundColor: statusColor(item.status) + '20' }]}>
          <Text style={[styles.badgeText, { color: statusColor(item.status) }]}>{item.status}</Text>
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
        <Text style={styles.title}>Invoices</Text>
      </View>
      <View style={styles.statBar}>
        <Text style={styles.statLabel}>Total Outstanding</Text>
        <Text style={styles.statValue}>{money(totalOutstanding, tenantCurrency)}</Text>
      </View>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.filterRow} contentContainerStyle={{ paddingHorizontal: 16, gap: 8 }}>
        {(['All', 'Paid', 'Pending', 'Overdue'] as Filter[]).map(f => (
          <TouchableOpacity key={f} style={[styles.filterBtn, filter === f && styles.filterBtnActive]} onPress={() => setFilter(f)}>
            <Text style={[styles.filterText, filter === f && styles.filterTextActive]}>{f}</Text>
          </TouchableOpacity>
        ))}
      </ScrollView>
      <FlatList
        data={filtered}
        keyExtractor={item => item.id}
        renderItem={renderItem}
        contentContainerStyle={styles.list}
        ListEmptyComponent={<Text style={styles.empty}>No invoices found</Text>}
      />

      <Modal visible={!!selectedInvoice} animationType="slide" presentationStyle="pageSheet">
        {selectedInvoice && (
          <SafeAreaView style={styles.modal}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{selectedInvoice.invoiceNumber}</Text>
              <TouchableOpacity onPress={() => setSelectedInvoice(null)}>
                <Text style={styles.closeBtn}>Close</Text>
              </TouchableOpacity>
            </View>
            {detailLoading ? (
              <ActivityIndicator color="#007AFF" style={{ marginTop: 20 }} />
            ) : (
              <ScrollView contentContainerStyle={styles.modalBody}>
                <View style={styles.detailRow}>
                  <Text style={styles.detailLabel}>Client</Text>
                  <Text style={styles.detailValue}>{selectedInvoice.clientName}</Text>
                </View>
                <View style={styles.detailRow}>
                  <Text style={styles.detailLabel}>Due Date</Text>
                  <Text style={styles.detailValue}>{new Date(selectedInvoice.dueDate).toLocaleDateString()}</Text>
                </View>
                <View style={styles.detailRow}>
                  <Text style={styles.detailLabel}>Status</Text>
                  <Text style={[styles.detailValue, { color: statusColor(selectedInvoice.status) }]}>{selectedInvoice.status}</Text>
                </View>
                {selectedInvoice.lineItems && selectedInvoice.lineItems.length > 0 && (
                  <>
                    <Text style={styles.sectionTitle}>Line Items</Text>
                    {selectedInvoice.lineItems.map(item => (
                      <View key={item.id} style={styles.lineItem}>
                        <Text style={styles.lineDesc}>{item.description}</Text>
                        <Text style={styles.lineTotal}>{item.quantity} × ${item.unitPrice} = ${item.total}</Text>
                      </View>
                    ))}
                  </>
                )}
                <View style={styles.totalRow}>
                  <Text style={styles.totalLabel}>Total</Text>
                  <Text style={styles.totalValue}>{money(selectedInvoice.amount, selectedInvoice.currency ?? tenantCurrency)}</Text>
                </View>
                {selectedInvoice.notes && (
                  <Text style={styles.notes}>Notes: {selectedInvoice.notes}</Text>
                )}
                <View style={styles.actionRow}>
                  {selectedInvoice.status === 'Overdue' && (
                    <TouchableOpacity style={styles.reminderBtn} onPress={() => sendReminder(selectedInvoice.id)}>
                      <Text style={styles.reminderBtnText}>Send Reminder</Text>
                    </TouchableOpacity>
                  )}
                  {selectedInvoice.status !== 'Paid' && (
                    <TouchableOpacity style={styles.paidBtn} onPress={() => markPaid(selectedInvoice.id)}>
                      <Text style={styles.paidBtnText}>Mark as Paid</Text>
                    </TouchableOpacity>
                  )}
                </View>
              </ScrollView>
            )}
          </SafeAreaView>
        )}
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 8 },
  title: { fontSize: 24, fontWeight: '700', color: '#111' },
  statBar: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', backgroundColor: '#FFEBE8', paddingHorizontal: 20, paddingVertical: 12, marginHorizontal: 16, borderRadius: 10, marginBottom: 8 },
  statLabel: { fontSize: 14, color: '#FF3B30' },
  statValue: { fontSize: 18, fontWeight: '700', color: '#FF3B30' },
  filterRow: { flexGrow: 0, marginBottom: 8 },
  filterBtn: { paddingHorizontal: 16, paddingVertical: 8, borderRadius: 20, backgroundColor: '#F2F2F7' },
  filterBtnActive: { backgroundColor: '#007AFF' },
  filterText: { fontSize: 13, color: '#666', fontWeight: '500' },
  filterTextActive: { color: '#fff', fontWeight: '600' },
  list: { paddingHorizontal: 16 },
  row: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 14, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  rowLeft: { flex: 1 },
  clientName: { fontSize: 15, fontWeight: '600', color: '#111' },
  invoiceNum: { fontSize: 12, color: '#888', marginTop: 2 },
  dueDate: { fontSize: 12, color: '#888', marginTop: 2 },
  rowRight: { alignItems: 'flex-end' },
  amount: { fontSize: 16, fontWeight: '700', color: '#111' },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2, marginTop: 4 },
  badgeText: { fontSize: 11, fontWeight: '600' },
  empty: { textAlign: 'center', color: '#888', marginTop: 60, fontSize: 15 },
  modal: { flex: 1, backgroundColor: '#fff' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, borderBottomWidth: 1, borderColor: '#eee' },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#111' },
  closeBtn: { color: '#007AFF', fontWeight: '600', fontSize: 16 },
  modalBody: { padding: 20 },
  detailRow: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 10, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  detailLabel: { fontSize: 14, color: '#888' },
  detailValue: { fontSize: 14, fontWeight: '600', color: '#111' },
  sectionTitle: { fontSize: 14, fontWeight: '600', color: '#888', marginTop: 20, marginBottom: 8, textTransform: 'uppercase', letterSpacing: 0.5 },
  lineItem: { paddingVertical: 8, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  lineDesc: { fontSize: 14, color: '#111' },
  lineTotal: { fontSize: 13, color: '#666', marginTop: 2 },
  totalRow: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 14, borderTopWidth: 2, borderColor: '#111', marginTop: 8 },
  totalLabel: { fontSize: 16, fontWeight: '700', color: '#111' },
  totalValue: { fontSize: 18, fontWeight: '700', color: '#111' },
  notes: { fontSize: 13, color: '#666', marginTop: 12, fontStyle: 'italic' },
  actionRow: { flexDirection: 'row', gap: 12, marginTop: 24 },
  reminderBtn: { flex: 1, backgroundColor: '#FF950020', borderRadius: 10, paddingVertical: 14, alignItems: 'center' },
  reminderBtnText: { color: '#FF9500', fontWeight: '600', fontSize: 15 },
  paidBtn: { flex: 1, backgroundColor: '#34C759', borderRadius: 10, paddingVertical: 14, alignItems: 'center' },
  paidBtnText: { color: '#fff', fontWeight: '600', fontSize: 15 },
});
