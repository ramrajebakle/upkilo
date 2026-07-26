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
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient, unwrapList } from '../api/apiClient';
import { RootStackParamList } from '../../App';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  role: string;
  todayBookingsCount: number;
  isActive: boolean;
}

interface ScheduleItem {
  id: string;
  startTime: string;
  endTime: string;
  clientName: string;
  serviceName: string;
}

export function StaffListScreen() {
  const navigation = useNavigation<NavProp>();
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [filtered, setFiltered] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [selectedStaff, setSelectedStaff] = useState<StaffMember | null>(null);
  const [schedule, setSchedule] = useState<ScheduleItem[]>([]);
  const [scheduleLoading, setScheduleLoading] = useState(false);

  useEffect(() => {
    loadStaff();
  }, []);

  useEffect(() => {
    const q = search.toLowerCase();
    setFiltered(staff.filter(s =>
      `${s.firstName} ${s.lastName}`.toLowerCase().includes(q)
    ));
  }, [search, staff]);

  const loadStaff = async () => {
    try {
      const res = await apiClient.get('/staff?includeStats=true');
      const data = unwrapList<StaffMember>(res.data);
      setStaff(data);
      setFiltered(data);
    } catch {
      Alert.alert('Error', 'Failed to load staff');
    } finally {
      setLoading(false);
    }
  };

  const openStaffDetail = async (member: StaffMember) => {
    setSelectedStaff(member);
    setScheduleLoading(true);
    try {
      const res = await apiClient.get(`/staff/${member.id}/schedule?date=today`);
      setSchedule(unwrapList<ScheduleItem>(res.data));
    } catch {
      setSchedule([]);
    } finally {
      setScheduleLoading(false);
    }
  };

  const addStaff = () => {
    Alert.prompt('Invite Staff', 'Enter email address:', async (email) => {
      if (!email) return;
      try {
        // Route is /invitation (singular) and `role` is required — the server parses it into
        // the UserRole enum. This screen invites staff members.
        await apiClient.post('/invitation', { email, role: 'Staff' });
        Alert.alert('Success', `Invitation sent to ${email}`);
      } catch {
        Alert.alert('Error', 'Failed to send invitation');
      }
    });
  };

  const initials = (member: StaffMember) =>
    (member.firstName.charAt(0) + member.lastName.charAt(0)).toUpperCase();

  const renderItem = ({ item }: { item: StaffMember }) => (
    <TouchableOpacity style={styles.row} onPress={() => openStaffDetail(item)}>
      <View style={[styles.avatar, { backgroundColor: item.isActive ? '#007AFF' : '#aaa' }]}>
        <Text style={styles.avatarText}>{initials(item)}</Text>
      </View>
      <View style={styles.rowInfo}>
        <Text style={styles.name}>{item.firstName} {item.lastName}</Text>
        <Text style={styles.role}>{item.role}</Text>
      </View>
      <View style={styles.rowRight}>
        <Text style={styles.bookingCount}>{item.todayBookingsCount} today</Text>
        <View style={[styles.statusDot, { backgroundColor: item.isActive ? '#34C759' : '#FF3B30' }]} />
      </View>
    </TouchableOpacity>
  );

  if (loading) {
    return <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>;
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Staff</Text>
      </View>
      <View style={styles.searchBar}>
        <TextInput
          style={styles.searchInput}
          value={search}
          onChangeText={setSearch}
          placeholder="Search staff..."
          placeholderTextColor="#999"
        />
      </View>
      <FlatList
        data={filtered}
        keyExtractor={item => item.id}
        renderItem={renderItem}
        contentContainerStyle={styles.list}
        ListEmptyComponent={<Text style={styles.empty}>No staff found</Text>}
      />
      <TouchableOpacity style={styles.fab} onPress={addStaff}>
        <Text style={styles.fabText}>+ Invite</Text>
      </TouchableOpacity>

      <Modal visible={!!selectedStaff} animationType="slide" presentationStyle="pageSheet">
        {selectedStaff && (
          <SafeAreaView style={styles.modal}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{selectedStaff.firstName} {selectedStaff.lastName}</Text>
              <TouchableOpacity onPress={() => setSelectedStaff(null)}>
                <Text style={styles.closeBtn}>Close</Text>
              </TouchableOpacity>
            </View>
            <Text style={styles.modalSubtitle}>Today's Schedule</Text>
            {scheduleLoading ? (
              <ActivityIndicator color="#007AFF" style={{ marginTop: 20 }} />
            ) : (
              <FlatList
                data={schedule}
                keyExtractor={item => item.id}
                renderItem={({ item }) => (
                  <View style={styles.scheduleItem}>
                    <Text style={styles.scheduleTime}>
                      {new Date(item.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} –{' '}
                      {new Date(item.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </Text>
                    <Text style={styles.scheduleService}>{item.serviceName}</Text>
                    <Text style={styles.scheduleClient}>{item.clientName}</Text>
                  </View>
                )}
                ListEmptyComponent={<Text style={styles.empty}>No appointments today</Text>}
                contentContainerStyle={{ padding: 16 }}
              />
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
  searchBar: { paddingHorizontal: 16, marginBottom: 8 },
  searchInput: { backgroundColor: '#F2F2F7', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 15, color: '#111' },
  list: { paddingHorizontal: 16 },
  row: { flexDirection: 'row', alignItems: 'center', paddingVertical: 14, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  avatar: { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center', marginRight: 14 },
  avatarText: { color: '#fff', fontWeight: '700', fontSize: 16 },
  rowInfo: { flex: 1 },
  name: { fontSize: 15, fontWeight: '600', color: '#111' },
  role: { fontSize: 13, color: '#888', marginTop: 2 },
  rowRight: { alignItems: 'flex-end' },
  bookingCount: { fontSize: 13, color: '#007AFF', fontWeight: '600' },
  statusDot: { width: 8, height: 8, borderRadius: 4, marginTop: 6 },
  fab: { position: 'absolute', bottom: 24, right: 24, backgroundColor: '#007AFF', borderRadius: 24, paddingVertical: 12, paddingHorizontal: 20, shadowColor: '#007AFF', shadowOpacity: 0.4, shadowRadius: 8, elevation: 6 },
  fabText: { color: '#fff', fontWeight: '700', fontSize: 15 },
  empty: { textAlign: 'center', color: '#888', marginTop: 60, fontSize: 15 },
  modal: { flex: 1, backgroundColor: '#fff' },
  modalHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 20, borderBottomWidth: 1, borderColor: '#eee' },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#111' },
  closeBtn: { color: '#007AFF', fontWeight: '600', fontSize: 16 },
  modalSubtitle: { fontSize: 14, fontWeight: '600', color: '#888', paddingHorizontal: 16, paddingTop: 16, textTransform: 'uppercase', letterSpacing: 0.5 },
  scheduleItem: { paddingVertical: 12, borderBottomWidth: 1, borderColor: '#F0F0F0' },
  scheduleTime: { fontSize: 13, color: '#007AFF', fontWeight: '600', marginBottom: 2 },
  scheduleService: { fontSize: 15, fontWeight: '600', color: '#111' },
  scheduleClient: { fontSize: 13, color: '#666', marginTop: 2 },
});
