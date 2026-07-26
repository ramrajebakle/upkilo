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
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient } from '../api/apiClient';
import { RootStackParamList } from '../../App';
import { money, useTenantCurrency } from '../utils/currency';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

interface Invoice {
  // Present on the API response; used so each record renders in the
  // currency it was actually billed in rather than assuming dollars.
  currency?: string;
  id: string;
  invoiceNumber: string;
  amount: number;
  status: 'Paid' | 'Pending' | 'Overdue';
  date: string;
  clientName: string;
}

interface Transaction {
  // Present on the API response; used so each record renders in the
  // currency it was actually billed in rather than assuming dollars.
  currency?: string;
  id: string;
  amount: number;
  method: string;
  date: string;
  bookingReference: string;
}

export function PaymentsScreen() {
  // Aggregate figures belong to the tenant, so they render in the tenant's currency.
  const tenantCurrency = useTenantCurrency();
  const navigation = useNavigation<NavProp>();
  const [tab, setTab] = useState<'Invoices' | 'Transactions'>('Invoices');
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalOutstanding, setTotalOutstanding] = useState(0);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      // allSettled, not all: the transaction-history endpoint does not exist yet, and with
      // Promise.all its rejection blanked the whole screen including the invoices that do load.
      const [invRes, txRes] = await Promise.allSettled([
        apiClient.get('/billing/invoices?page=1&pageSize=20'),
        apiClient.get('/payments/history?limit=20'),
      ]);

      const invBody = invRes.status === 'fulfilled'
        ? (invRes.value.data as { data?: Invoice[]; items?: Invoice[] })
        : {};
      const invData = invBody.data || invBody.items || [];

      const txBody = txRes.status === 'fulfilled'
        ? (txRes.value.data as { data?: Transaction[]; items?: Transaction[] })
        : {};
      const txData = txBody.data || txBody.items || [];

      setInvoices(invData);
      setTransactions(txData);
      const outstanding = invData
        .filter((i: Invoice) => i.status !== 'Paid')
        .reduce((sum: number, i: Invoice) => sum + i.amount, 0);
      setTotalOutstanding(outstanding);
    } catch {
      Alert.alert('Error', 'Failed to load payments data');
    } finally {
      setLoading(false);
    }
  };

  const downloadPdf = async (id: string) => {
    try {
      const res = await apiClient.get(`/export/invoices/${id}/pdf`);
      const url = (res.data as { url?: string }).url;
      Alert.alert('PDF Ready', url ? `Download URL: ${url}` : 'PDF generated successfully');
    } catch {
      Alert.alert('Error', 'Failed to generate PDF');
    }
  };

  const statusColor = (status: string) => {
    if (status === 'Paid') return '#34C759';
    if (status === 'Overdue') return '#FF3B30';
    return '#FF9500';
  };

  const renderInvoice = ({ item }: { item: Invoice }) => (
    <View style={styles.row}>
      <View style={styles.rowLeft}>
        <Text style={styles.rowTitle}>{item.invoiceNumber}</Text>
        <Text style={styles.rowSub}>{item.clientName} · {new Date(item.date).toLocaleDateString()}</Text>
      </View>
      <View style={styles.rowRight}>
        <Text style={styles.amount}>{money(item.amount, item.currency ?? tenantCurrency)}</Text>
        <View style={[styles.badge, { backgroundColor: statusColor(item.status) + '20' }]}>
          <Text style={[styles.badgeText, { color: statusColor(item.status) }]}>{item.status}</Text>
        </View>
        <TouchableOpacity style={styles.pdfBtn} onPress={() => downloadPdf(item.id)}>
          <Text style={styles.pdfBtnText}>PDF</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  const renderTransaction = ({ item }: { item: Transaction }) => (
    <View style={styles.row}>
      <View style={styles.rowLeft}>
        <Text style={styles.rowTitle}>{item.method}</Text>
        <Text style={styles.rowSub}>{item.bookingReference} · {new Date(item.date).toLocaleDateString()}</Text>
      </View>
      <Text style={styles.amount}>{money(item.amount, item.currency ?? tenantCurrency)}</Text>
    </View>
  );

  if (loading) {
    return <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>;
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Payments</Text>
      </View>
      <View style={styles.statBar}>
        <Text style={styles.statLabel}>Total Outstanding</Text>
        <Text style={styles.statValue}>{money(totalOutstanding, tenantCurrency)}</Text>
      </View>
      <View style={styles.tabs}>
        {(['Invoices', 'Transactions'] as const).map(t => (
          <TouchableOpacity
            key={t}
            style={[styles.tab, tab === t && styles.tabActive]}
            onPress={() => setTab(t)}
          >
            <Text style={[styles.tabText, tab === t && styles.tabTextActive]}>{t}</Text>
          </TouchableOpacity>
        ))}
      </View>
      {tab === 'Invoices' ? (
        <FlatList
          data={invoices}
          keyExtractor={item => item.id}
          renderItem={renderInvoice}
          contentContainerStyle={styles.list}
        />
      ) : (
        <FlatList
          data={transactions}
          keyExtractor={item => item.id}
          renderItem={renderTransaction}
          contentContainerStyle={styles.list}
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 8 },
  title: { fontSize: 24, fontWeight: '700', color: '#111' },
  statBar: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    backgroundColor: '#FFF3CD', paddingHorizontal: 20, paddingVertical: 12, marginHorizontal: 16,
    borderRadius: 10, marginBottom: 8,
  },
  statLabel: { fontSize: 14, color: '#856404' },
  statValue: { fontSize: 18, fontWeight: '700', color: '#856404' },
  tabs: { flexDirection: 'row', marginHorizontal: 16, marginBottom: 8, borderRadius: 8, backgroundColor: '#F2F2F7', padding: 2 },
  tab: { flex: 1, paddingVertical: 8, alignItems: 'center', borderRadius: 6 },
  tabActive: { backgroundColor: '#fff', shadowColor: '#000', shadowOpacity: 0.1, shadowRadius: 4, elevation: 2 },
  tabText: { color: '#888', fontWeight: '500' },
  tabTextActive: { color: '#007AFF', fontWeight: '600' },
  list: { paddingHorizontal: 16 },
  row: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingVertical: 14, borderBottomWidth: 1, borderColor: '#F0F0F0',
  },
  rowLeft: { flex: 1 },
  rowRight: { alignItems: 'flex-end' },
  rowTitle: { fontSize: 15, fontWeight: '600', color: '#111' },
  rowSub: { fontSize: 12, color: '#888', marginTop: 2 },
  amount: { fontSize: 15, fontWeight: '700', color: '#111' },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2, marginTop: 4 },
  badgeText: { fontSize: 11, fontWeight: '600' },
  pdfBtn: { marginTop: 4, backgroundColor: '#007AFF20', borderRadius: 6, paddingHorizontal: 10, paddingVertical: 4 },
  pdfBtnText: { color: '#007AFF', fontSize: 12, fontWeight: '600' },
});
