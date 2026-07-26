/**
 * MK7: Consumer-facing app — Book a service screen.
 * Shows business services and allows consumers to pick a slot.
 */
import React, { useEffect, useState } from 'react';
import {
  View, Text, ScrollView, TouchableOpacity, StyleSheet,
  ActivityIndicator, Alert,
} from 'react-native';
import { publicClient, unwrapList } from '../../api/apiClient';

interface Service { id: string; name: string; duration: number; price: number; }

export default function ConsumerBookScreen({ route, navigation }: { route: any; navigation: any }) {
  const { slug, name } = route?.params ?? { slug: 'demo', name: 'Business' };
  const [services, setServices] = useState<Service[]>([]);
  const [selected, setSelected] = useState<Service | null>(null);
  const [slots, setSlots] = useState<string[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Public booking widget lives at api/booking/{slug}/... (NOT under /api/v1) → use publicClient.
    publicClient.get(`/api/booking/${slug}/services`)
      .then(res => setServices(
        Array.isArray(res.data?.services) ? res.data.services : unwrapList<Service>(res.data)
      ))
      .catch(() => Alert.alert('Error', 'Could not load services'))
      .finally(() => setLoading(false));
  }, [slug]);

  const loadSlots = async (svc: Service) => {
    setSelected(svc);
    setSlots([]);
    try {
      const today = new Date().toISOString().split('T')[0];
      const res = await publicClient.get(`/api/booking/${slug}/availability?serviceId=${svc.id}&date=${today}`);
      setSlots((res.data?.slots ?? []).slice(0, 8).map((s: any) => s.time ?? s));
    } catch { setSlots([]); }
  };

  const confirmBooking = () => {
    if (!selected || !selectedSlot) return;
    navigation?.navigate?.('ConsumerConfirm', { slug, service: selected, slot: selectedSlot });
  };

  if (loading) return <ActivityIndicator style={styles.loader} size="large" color="#3B82F6" />;

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.heading}>{name}</Text>

      {/* Services */}
      <Text style={styles.sectionTitle}>Choose a Service</Text>
      {services.map(svc => (
        <TouchableOpacity
          key={svc.id}
          style={[styles.serviceCard, selected?.id === svc.id && styles.serviceCardSelected]}
          onPress={() => loadSlots(svc)}
        >
          <Text style={styles.serviceName}>{svc.name}</Text>
          <Text style={styles.serviceDetail}>{svc.duration} min · ${svc.price}</Text>
        </TouchableOpacity>
      ))}

      {/* Time slots */}
      {selected && (
        <>
          <Text style={styles.sectionTitle}>Pick a Time</Text>
          {slots.length === 0 ? (
            <Text style={styles.noSlots}>No available slots today. Try another date.</Text>
          ) : (
            <View style={styles.slotsGrid}>
              {slots.map(slot => (
                <TouchableOpacity
                  key={slot}
                  style={[styles.slotBtn, selectedSlot === slot && styles.slotBtnSelected]}
                  onPress={() => setSelectedSlot(slot)}
                >
                  <Text style={[styles.slotText, selectedSlot === slot && styles.slotTextSelected]}>{slot}</Text>
                </TouchableOpacity>
              ))}
            </View>
          )}
        </>
      )}

      {selected && selectedSlot && (
        <TouchableOpacity style={styles.confirmBtn} onPress={confirmBooking}>
          <Text style={styles.confirmBtnText}>Continue</Text>
        </TouchableOpacity>
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  loader: { flex: 1 },
  heading: { fontSize: 22, fontWeight: '700', color: '#111827', marginBottom: 16 },
  sectionTitle: { fontSize: 16, fontWeight: '700', color: '#374151', marginTop: 16, marginBottom: 8 },
  serviceCard: { backgroundColor: '#fff', borderRadius: 10, padding: 14, marginBottom: 8, borderWidth: 2, borderColor: 'transparent' },
  serviceCardSelected: { borderColor: '#3B82F6' },
  serviceName: { fontSize: 15, fontWeight: '600', color: '#111827' },
  serviceDetail: { fontSize: 13, color: '#6B7280', marginTop: 2 },
  noSlots: { fontSize: 14, color: '#6B7280', fontStyle: 'italic' },
  slotsGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  slotBtn: { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 8, borderWidth: 1, borderColor: '#D1D5DB', backgroundColor: '#fff' },
  slotBtnSelected: { backgroundColor: '#3B82F6', borderColor: '#3B82F6' },
  slotText: { fontSize: 14, color: '#374151', fontWeight: '500' },
  slotTextSelected: { color: '#fff' },
  confirmBtn: { marginTop: 24, backgroundColor: '#3B82F6', borderRadius: 12, padding: 16, alignItems: 'center' },
  confirmBtnText: { color: '#fff', fontSize: 16, fontWeight: '700' },
});
