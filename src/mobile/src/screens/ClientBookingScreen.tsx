import React, { useState, useEffect } from 'react';
import { 
  StyleSheet, 
  View, 
  Text, 
  FlatList, 
  TouchableOpacity, 
  ActivityIndicator, 
  SafeAreaView,
  Alert,
  ScrollView
} from 'react-native';
import { apiClient } from '../api/apiClient';
import { ShoppingBag, Clock, Tag, Calendar, ChevronRight } from 'lucide-react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { format, addDays, startOfDay } from 'date-fns';

export function ClientBookingScreen() {
  const navigation = useNavigation();
  const route = useRoute<any>();
  const clientIdParam: string | undefined = route.params?.clientId;
  const [services, setServices] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  
  // States for the new booking flow
  const [step, setStep] = useState(1); // 1: Service, 2: Date/Time
  const [selectedService, setSelectedService] = useState<any>(null);
  const [selectedDate, setSelectedDate] = useState<Date>(startOfDay(addDays(new Date(), 1)));
  const [slots, setSlots] = useState<any[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [staffList, setStaffList] = useState<any[]>([]);
  const [selectedStaffId, setSelectedStaffId] = useState<string | null>(null);

  useEffect(() => {
    fetchServices();
    fetchStaff();
  }, []);

  useEffect(() => {
    if (step === 2 && selectedService) {
      fetchSlots();
    }
  }, [selectedDate, selectedService, step]);

  const fetchServices = async () => {
    try {
      const response = await apiClient.get('/services');
      setServices(response.data?.data || []);
    } catch (error) {
      console.error('Fetch services error:', error);
    } finally {
      setLoading(false);
    }
  };

  // A booking requires a staff member — CreateBookingRequest.StaffId is required and the
  // service rejects Guid.Empty. Load active staff and default to the first.
  const fetchStaff = async () => {
    try {
      const response = await apiClient.get('/staff?isActive=true&pageSize=100');
      const list = response.data?.data ?? response.data ?? [];
      setStaffList(list);
      if (list.length > 0) setSelectedStaffId(list[0].id);
    } catch (error) {
      console.error('Fetch staff error:', error);
    }
  };

  const fetchSlots = async () => {
    if (!selectedService) return;
    setLoadingSlots(true);
    try {
      // Correct endpoint for public availability
      const dateStr = format(selectedDate, 'yyyy-MM-dd');
      const response = await apiClient.get('/bookings/availability', {
        params: {
          serviceId: selectedService.id,
          date: dateStr
        }
      });
      // The API returns { date: "...", slots: [ { time: "HH:mm", available: true }, ... ] }
      setSlots(response.data?.slots || []);
      setSelectedSlot(null);
    } catch (error) {
      console.error('Fetch slots error:', error);
      setSlots([]);
    } finally {
      setLoadingSlots(false);
    }
  };

  const handleServiceSelect = (service: any) => {
    setSelectedService(service);
    setStep(2);
  };

  const handleConfirmBooking = async () => {
    if (!selectedSlot || !selectedService || !selectedStaffId) return;

    try {
      const response = await apiClient.post('/bookings', {
        serviceId: selectedService.id,
        staffId: selectedStaffId,
        ...(clientIdParam ? { clientId: clientIdParam } : {}),
        startTime: format(selectedDate, 'yyyy-MM-dd') + 'T' + selectedSlot + ':00Z',
        endTime: format(selectedDate, 'yyyy-MM-dd') + 'T' + calculateEndTime(selectedSlot, selectedService.durationMinutes) + ':00Z'
      });

      if (response.status === 201 || response.status === 200) {
        Alert.alert('Success', 'Your appointment has been booked!', [
          { text: 'OK', onPress: () => navigation.navigate('Home' as never) }
        ]);
      }
    } catch (error: any) {
      Alert.alert('Booking Failed', error.response?.data?.error || 'Could not complete booking');
    }
  };

  const calculateEndTime = (startTime: string, duration: number) => {
    const [hours, minutes] = startTime.split(':').map(Number);
    const totalMinutes = hours * 60 + minutes + duration;
    const endHours = Math.floor(totalMinutes / 60);
    const endMinutes = totalMinutes % 60;
    return `${endHours.toString().padStart(2, '0')}:${endMinutes.toString().padStart(2, '0')}`;
  };

  const renderServiceItem = ({ item }: { item: any }) => (
    <TouchableOpacity style={styles.serviceCard} onPress={() => handleServiceSelect(item)}>
      <View style={styles.cardInfo}>
        <Text style={styles.serviceName}>{item.name}</Text>
        <Text style={styles.serviceDescription} numberOfLines={2}>
          {item.description || 'Professional service at Upkilo.'}
        </Text>
        
        <View style={styles.metaRow}>
          <View style={styles.metaItem}>
            <Clock size={14} color="#666" />
            <Text style={styles.metaText}>{item.durationMinutes} min</Text>
          </View>
          <View style={styles.metaItem}>
            <Tag size={14} color="#666" />
            <Text style={styles.metaText}>{item.category || 'General'}</Text>
          </View>
        </View>
      </View>

      <View style={styles.priceContainer}>
        <Text style={styles.priceSymbol}>$</Text>
        <Text style={styles.priceValue}>{item.price}</Text>
        <ChevronRight size={20} color="#ccc" />
      </View>
    </TouchableOpacity>
  );

  const renderSlots = () => {
    if (loadingSlots) return <ActivityIndicator style={{ marginTop: 20 }} />;
    if (slots.length === 0) return <Text style={styles.noSlots}>No available slots for this day.</Text>;

    return (
      <View style={styles.slotsGrid}>
        {slots.map((slot) => (
          <TouchableOpacity 
            key={slot.time}
            style={[
              styles.slotButton, 
              selectedSlot === slot.time && styles.selectedSlot
            ]}
            onPress={() => setSelectedSlot(slot.time)}
          >
            <Text style={[
              styles.slotText,
              selectedSlot === slot.time && styles.selectedSlotText
            ]}>
              {slot.time}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    );
  };

  if (step === 2) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => setStep(1)}>
            <Text style={styles.backLink}>Change Service</Text>
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Select Time</Text>
          <View style={{ width: 80 }} />
        </View>

        <ScrollView contentContainerStyle={styles.scrollContent}>
          <View style={styles.selectedServiceHeader}>
            <Text style={styles.summaryLabel}>Booking</Text>
            <Text style={styles.summaryValue}>{selectedService.name}</Text>
          </View>

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Select Date</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.dateSelector}>
              {[...Array(14)].map((_, i) => {
                const date = addDays(new Date(), i + 1);
                const isSelected = format(date, 'yyyy-MM-dd') === format(selectedDate, 'yyyy-MM-dd');
                return (
                  <TouchableOpacity 
                    key={i} 
                    style={[styles.dateCard, isSelected && styles.selectedDateCard]}
                    onPress={() => setSelectedDate(date)}
                  >
                    <Text style={[styles.dateDay, isSelected && styles.selectedDateText]}>{format(date, 'EEE')}</Text>
                    <Text style={[styles.dateNumber, isSelected && styles.selectedDateText]}>{format(date, 'd')}</Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>

          {staffList.length > 0 && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>Select Staff</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.dateSelector}>
                {staffList.map((st) => {
                  const isSel = selectedStaffId === st.id;
                  const label = st.name ?? (`${st.firstName ?? ''} ${st.lastName ?? ''}`.trim() || 'Staff');
                  return (
                    <TouchableOpacity
                      key={st.id}
                      style={[styles.dateCard, { minWidth: 96, paddingHorizontal: 12 }, isSel && styles.selectedDateCard]}
                      onPress={() => setSelectedStaffId(st.id)}
                    >
                      <Text
                        style={[styles.dateDay, isSel && styles.selectedDateText]}
                        numberOfLines={1}
                      >
                        {label}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </ScrollView>
            </View>
          )}

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Available Times</Text>
            {renderSlots()}
          </View>
        </ScrollView>

        <View style={styles.footer}>
          <TouchableOpacity 
            style={[styles.bookButton, (!selectedSlot || !selectedStaffId) && styles.disabledButton]}
            onPress={handleConfirmBooking}
            disabled={!selectedSlot || !selectedStaffId}
          >
            <Text style={styles.bookButtonText}>Confirm Booking ${selectedService.price}</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Text style={styles.backLink}>Dashboard</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Select Service</Text>
        <View style={{ width: 80 }} />
      </View>

      {loading ? (
        <ActivityIndicator size="large" color="#007AFF" style={{ marginTop: 40 }} />
      ) : (
        <FlatList
          data={services}
          renderItem={renderServiceItem}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <ShoppingBag size={48} color="#eee" />
              <Text style={styles.emptyText}>No services available</Text>
            </View>
          }
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  backLink: {
    color: '#007AFF',
    fontWeight: '600',
    minWidth: 80,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1a1a1a',
  },
  listContent: {
    padding: 16,
  },
  scrollContent: {
    paddingBottom: 100,
  },
  serviceCard: {
    flexDirection: 'row',
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: 16,
    marginBottom: 16,
    borderWidth: 1,
    borderColor: '#f0f0f0',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 8,
    elevation: 2,
  },
  cardInfo: {
    flex: 1,
    paddingRight: 10,
  },
  serviceName: {
    fontSize: 17,
    fontWeight: 'bold',
    color: '#1a1a1a',
    marginBottom: 4,
  },
  serviceDescription: {
    fontSize: 14,
    color: '#666',
    lineHeight: 20,
    marginBottom: 12,
  },
  metaRow: {
    flexDirection: 'row',
    gap: 12,
  },
  metaItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  metaText: {
    fontSize: 12,
    color: '#888',
  },
  priceContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  priceSymbol: {
    fontSize: 12,
    color: '#007AFF',
    fontWeight: 'bold',
  },
  priceValue: {
    fontSize: 20,
    fontWeight: '800',
    color: '#007AFF',
  },
  section: {
    marginTop: 24,
    paddingHorizontal: 20,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1a1a1a',
    marginBottom: 16,
  },
  selectedServiceHeader: {
    backgroundColor: '#f8f9fa',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  summaryLabel: {
    fontSize: 12,
    color: '#888',
    textTransform: 'uppercase',
    letterSpacing: 1,
    marginBottom: 4,
  },
  summaryValue: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#1a1a1a',
  },
  dateSelector: {
    marginHorizontal: -20,
    paddingHorizontal: 20,
  },
  dateCard: {
    width: 60,
    height: 80,
    borderRadius: 12,
    backgroundColor: '#f5f5f5',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  selectedDateCard: {
    backgroundColor: '#007AFF',
  },
  dateDay: {
    fontSize: 12,
    color: '#666',
    marginBottom: 4,
  },
  dateNumber: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1a1a1a',
  },
  selectedDateText: {
    color: '#fff',
  },
  slotsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  slotButton: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    backgroundColor: '#f5f5f5',
    width: '31%',
    alignItems: 'center',
  },
  selectedSlot: {
    backgroundColor: '#007AFF',
  },
  slotText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1a1a1a',
  },
  selectedSlotText: {
    color: '#fff',
  },
  footer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    padding: 20,
    paddingBottom: 40,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#eee',
  },
  bookButton: {
    backgroundColor: '#007AFF',
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
  },
  disabledButton: {
    backgroundColor: '#ccc',
  },
  bookButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  noSlots: {
    textAlign: 'center',
    color: '#999',
    marginTop: 20,
  },
  emptyContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 60,
  },
  emptyText: {
    marginTop: 16,
    color: '#999',
    fontSize: 16,
  },
});

