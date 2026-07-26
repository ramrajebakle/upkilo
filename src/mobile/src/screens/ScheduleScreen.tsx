import React, { useState, useEffect, useCallback } from 'react';
import { 
  StyleSheet, 
  View, 
  Text, 
  SectionList, 
  TouchableOpacity, 
  ActivityIndicator, 
  SafeAreaView,
  RefreshControl,
  StatusBar,
  Modal,
  ScrollView
} from 'react-native';
import { apiClient } from '../api/apiClient';
import { ChevronLeft, Clock, MapPin, Calendar, CheckCircle, XCircle, User, X } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import { format, parseISO } from 'date-fns';

export function ScheduleScreen() {
  const navigation = useNavigation();
  const [sections, setSections] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState<any>(null);

  useEffect(() => {
    fetchSchedule();
  }, []);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    fetchSchedule();
  }, []);

  const fetchSchedule = async () => {
    try {
      const today = new Date();
      const nextWeek = new Date();
      nextWeek.setDate(today.getDate() + 7);

      const response = await apiClient.get('/bookings', {
        params: {
          startDate: format(today, 'yyyy-MM-dd'),
          endDate: format(nextWeek, 'yyyy-MM-dd')
        }
      });

      const data = response.data?.data || [];
      
      // Group by date
      const grouped = data.reduce((acc: any, booking: any) => {
        const dateKey = format(parseISO(booking.startTime), 'yyyy-MM-dd');
        const displayDate = format(parseISO(booking.startTime), 'EEEE, MMM d');
        
        if (!acc[dateKey]) acc[dateKey] = { title: displayDate, data: [] };
        acc[dateKey].data.push(booking);
        return acc;
      }, {});

      const sectionData = Object.keys(grouped)
        .sort()
        .map(key => grouped[key]);

      setSections(sectionData);
    } catch (error) {
      console.error('Fetch schedule error:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'confirmed': return '#34C759';
      case 'pending': return '#FF9500';
      case 'cancelled': return '#FF3B30';
      case 'completed': return '#8E8E93';
      default: return '#D1D1D6';
    }
  };

  const renderItem = ({ item }: { item: any }) => (
    <TouchableOpacity 
      style={styles.bookingCard} 
      activeOpacity={0.7}
      onPress={() => {
        setSelectedBooking(item);
        setModalVisible(true);
      }}
    >
      <View style={styles.timeContainer}>
        <Text style={styles.startTime}>
          {format(parseISO(item.startTime), 'HH:mm')}
        </Text>
        <View style={styles.durationBadge}>
          <Text style={styles.durationText}>{item.durationMinutes}m</Text>
        </View>
      </View>
      
      <View style={styles.contentContainer}>
        <View style={styles.row}>
          <Text style={styles.serviceName} numberOfLines={1}>{item.serviceName}</Text>
          <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) + '15' }]}>
            <Text style={[styles.statusBadgeText, { color: getStatusColor(item.status) }]}>{item.status}</Text>
          </View>
        </View>
        
        <Text style={styles.clientName}>{item.clientName}</Text>
        
        {item.locationName && (
          <View style={styles.locationRow}>
            <MapPin size={12} color="#8E8E93" />
            <Text style={styles.locationText}>{item.locationName}</Text>
          </View>
        )}
      </View>
    </TouchableOpacity>
  );

  const renderSectionHeader = ({ section: { title } }: { section: { title: string } }) => (
    <View style={styles.sectionHeader}>
      <Text style={styles.sectionHeaderText}>{title}</Text>
    </View>
  );

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="dark-content" />
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.roundButton}>
          <ChevronLeft size={24} color="#1a1a1a" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Schedule</Text>
        <View style={{ width: 44 }} />
      </View>

      {loading && !refreshing ? (
        <View style={styles.loadingCenter}>
          <ActivityIndicator size="large" color="#007AFF" />
          <Text style={styles.loadingText}>Loading your schedule...</Text>
        </View>
      ) : (
        <SectionList
          sections={sections}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          renderSectionHeader={renderSectionHeader}
          contentContainerStyle={styles.listContent}
          stickySectionHeadersEnabled={true}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#007AFF" />
          }
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Calendar size={64} color="#F2F2F7" />
              <Text style={styles.emptyTitle}>Empty Schedule</Text>
              <Text style={styles.emptySubtitle}>You have no appointments scheduled for this week.</Text>
            </View>
          }
        />
      )}

      <Modal
        animationType="slide"
        transparent={true}
        visible={modalVisible}
        onRequestClose={() => setModalVisible(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Booking Details</Text>
              <TouchableOpacity onPress={() => setModalVisible(false)}>
                <X size={24} color="#666" />
              </TouchableOpacity>
            </View>

            {selectedBooking && (
              <ScrollView>
                <View style={styles.detailSection}>
                  <Text style={styles.detailLabel}>Client</Text>
                  <View style={styles.detailValueRow}>
                    <User size={18} color="#007AFF" />
                    <Text style={styles.detailValueText}>{selectedBooking.clientName}</Text>
                  </View>
                </View>

                <View style={styles.detailSection}>
                  <Text style={styles.detailLabel}>Service</Text>
                  <Text style={styles.detailValueTextMain}>{selectedBooking.serviceName}</Text>
                </View>

                <View style={styles.detailSection}>
                  <Text style={styles.detailLabel}>Time</Text>
                  <View style={styles.detailValueRow}>
                    <Clock size={18} color="#666" />
                    <Text style={styles.detailValueText}>
                      {format(parseISO(selectedBooking.startTime), 'EEEE, MMM d, HH:mm')}
                    </Text>
                  </View>
                </View>
              </ScrollView>
            )}
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F8F9FA',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  roundButton: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f5f5f5',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1a1a1a',
  },
  listContent: {
    paddingBottom: 40,
  },
  loadingCenter: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 12,
    color: '#8E8E93',
    fontSize: 14,
  },
  sectionHeader: {
    backgroundColor: '#F8F9FA',
    paddingHorizontal: 20,
    paddingVertical: 12,
  },
  sectionHeaderText: {
    fontSize: 13,
    fontWeight: '700',
    color: '#8E8E93',
    textTransform: 'uppercase',
    letterSpacing: 1,
  },
  bookingCard: {
    flexDirection: 'row',
    backgroundColor: '#fff',
    marginHorizontal: 16,
    marginVertical: 6,
    padding: 16,
    borderRadius: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 5,
    elevation: 2,
  },
  timeContainer: {
    width: 60,
    alignItems: 'center',
    justifyContent: 'center',
    borderRightWidth: 1,
    borderRightColor: '#F2F2F7',
    paddingRight: 10,
  },
  startTime: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1a1a1a',
  },
  durationBadge: {
    marginTop: 6,
    paddingHorizontal: 6,
    paddingVertical: 2,
    backgroundColor: '#F2F2F7',
    borderRadius: 4,
  },
  durationText: {
    fontSize: 10,
    color: '#8E8E93',
    fontWeight: '600',
  },
  contentContainer: {
    flex: 1,
    paddingLeft: 16,
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  serviceName: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1a1a1a',
    flex: 1,
    marginRight: 8,
  },
  clientName: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  locationRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  locationText: {
    fontSize: 12,
    color: '#8E8E93',
  },
  statusBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
  },
  statusBadgeText: {
    fontSize: 10,
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  emptyContainer: {
    padding: 60,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1a1a1a',
    marginTop: 20,
  },
  emptySubtitle: {
    color: '#8E8E93',
    fontSize: 14,
    textAlign: 'center',
    marginTop: 8,
    lineHeight: 20,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#fff',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    minHeight: '60%',
    padding: 24,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 24,
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: 'bold',
  },
  detailSection: {
    marginBottom: 20,
  },
  detailLabel: {
    fontSize: 13,
    color: '#8E8E93',
    marginBottom: 6,
    textTransform: 'uppercase',
  },
  detailValueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  detailValueText: {
    fontSize: 16,
    color: '#1a1a1a',
  },
  detailValueTextMain: {
    fontSize: 18,
    fontWeight: '600',
    color: '#1a1a1a',
  },
});

