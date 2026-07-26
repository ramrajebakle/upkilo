/**
 * Consumer-facing booking confirmation screen.
 * Called from ConsumerBookScreen after service + slot selection.
 */
import React, { useState } from 'react';
import {
  View, Text, StyleSheet, SafeAreaView, ScrollView,
  TouchableOpacity, ActivityIndicator, Alert,
} from 'react-native';
import { CheckCircle, Calendar, Clock, Briefcase } from 'lucide-react-native';
import * as Haptics from 'expo-haptics';
import { apiClient, publicClient } from '../../api/apiClient';
import { currencySymbol, money as sharedMoney } from '../../utils/currency';

interface Service {
  id: string;
  name: string;
  duration: number;
  price: number;
  currency?: string;
}

// The business sets its own currency; the screen previously hardcoded a rupee symbol and
// en-IN grouping regardless of who the customer was booking with.
/**
 * Formatting delegates to the shared helper; the only local behaviour is showing a dash for a
 * missing price, which is more honest here than rendering it as zero.
 */
function money(amount: number | undefined, currency: string | undefined) {
  if (amount == null) return '—';
  return sharedMoney(amount, currency);
}

interface RouteParams {
  slug: string;
  name?: string;
  service: Service;
  slot: string;
}

export default function ConsumerConfirmScreen({ route, navigation }: { route: any; navigation: any }) {
  const { slug, name, service, slot } = (route?.params ?? {}) as RouteParams;
  const [loading, setLoading] = useState(false);
  const [confirmed, setConfirmed] = useState<string | null>(null);

  const handleConfirm = async () => {
    setLoading(true);
    try {
      // Fetch the signed-in consumer's contact details for the public booking payload.
      const me = (await apiClient.get('/auth/me')).data ?? {};
      const firstName = me.firstName ?? me.user?.firstName ?? '';
      const lastName = me.lastName ?? me.user?.lastName ?? '';
      const email = me.email ?? me.user?.email ?? '';
      const phone = me.phone ?? me.phoneNumber ?? me.user?.phone ?? '';

      // Slots may be "HH:mm" (for today) or a full ISO datetime. The public booking endpoint
      // takes date + time separately.
      const parsed = new Date(slot);
      const hasFullDate = !isNaN(parsed.getTime()) && /\d{4}-\d{2}-\d{2}/.test(slot);
      const date = hasFullDate ? parsed.toISOString().split('T')[0] : new Date().toISOString().split('T')[0];
      const time = hasFullDate
        ? `${String(parsed.getHours()).padStart(2, '0')}:${String(parsed.getMinutes()).padStart(2, '0')}`
        : slot;

      // Public booking widget lives at api/booking/{slug}/book (NOT under /api/v1) → publicClient.
      // utmSource "marketplace" applies the Upkilo commission via Stripe Connect.
      const res = await publicClient.post(`/api/booking/${slug}/book`, {
        serviceId: service.id,
        date,
        time,
        firstName,
        lastName,
        email,
        phone,
        utmSource: 'marketplace',
      });
      const ref: string =
        res.data?.bookingReference ?? res.data?.reference ?? res.data?.id ?? 'UPK-' + Date.now();
      await Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
      setConfirmed(ref);
    } catch (err: any) {
      await Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
      Alert.alert('Booking failed', err.response?.data?.error ?? err.response?.data?.message ?? 'Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (confirmed) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.successContainer}>
          <View style={styles.successIcon}>
            <CheckCircle size={48} color="#7C3AED" />
          </View>
          <Text style={styles.successTitle}>Booking Confirmed!</Text>
          <Text style={styles.successRef}>Reference: {confirmed}</Text>
          <Text style={styles.successMsg}>
            You'll receive a confirmation reminder before your appointment.
          </Text>
          <TouchableOpacity
            style={styles.btnDone}
            onPress={() => navigation?.navigate?.('Bookings')}
          >
            <Text style={styles.btnDoneText}>View My Bookings</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.btnSecondary}
            onPress={() => navigation?.navigate?.('Discover')}
          >
            <Text style={styles.btnSecondaryText}>Browse More Services</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const slotDate = new Date(slot);
  const dateLabel = isNaN(slotDate.getTime()) ? slot : slotDate.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' });
  const timeLabel = isNaN(slotDate.getTime()) ? '' : slotDate.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.heading}>Confirm Booking</Text>
        {name && <Text style={styles.businessName}>{name}</Text>}

        <View style={styles.summaryCard}>
          <View style={styles.summaryRow}>
            <Briefcase size={18} color="#7C3AED" />
            <View style={styles.summaryText}>
              <Text style={styles.summaryLabel}>Service</Text>
              <Text style={styles.summaryValue}>{service?.name}</Text>
            </View>
          </View>

          <View style={styles.divider} />

          <View style={styles.summaryRow}>
            <Calendar size={18} color="#7C3AED" />
            <View style={styles.summaryText}>
              <Text style={styles.summaryLabel}>Date</Text>
              <Text style={styles.summaryValue}>{dateLabel}</Text>
            </View>
          </View>

          {timeLabel ? (
            <>
              <View style={styles.divider} />
              <View style={styles.summaryRow}>
                <Clock size={18} color="#7C3AED" />
                <View style={styles.summaryText}>
                  <Text style={styles.summaryLabel}>Time</Text>
                  <Text style={styles.summaryValue}>{timeLabel}</Text>
                </View>
              </View>
            </>
          ) : null}

          <View style={styles.divider} />

          <View style={styles.summaryRow}>
            <View style={styles.priceIcon}>
              <Text style={styles.priceIconText}>{currencySymbol(service?.currency)}</Text>
            </View>
            <View style={styles.summaryText}>
              <Text style={styles.summaryLabel}>Price</Text>
              <Text style={styles.summaryValue}>{money(service?.price, service?.currency)}</Text>
            </View>
          </View>
        </View>

        <Text style={styles.note}>
          Payment is collected at the time of service unless prepayment is required.
        </Text>

        <TouchableOpacity
          style={[styles.btnConfirm, loading && styles.btnDisabled]}
          onPress={handleConfirm}
          disabled={loading}
          accessibilityRole="button"
          accessibilityLabel="Confirm and book appointment"
        >
          {loading ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.btnConfirmText}>Confirm Booking</Text>
          )}
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.btnBack}
          onPress={() => navigation?.goBack?.()}
          disabled={loading}
        >
          <Text style={styles.btnBackText}>Back</Text>
        </TouchableOpacity>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8F8FA' },
  content: { padding: 20, paddingBottom: 40 },
  heading: { fontSize: 26, fontWeight: 'bold', color: '#111120', marginBottom: 4 },
  businessName: { fontSize: 16, color: '#66667A', marginBottom: 24 },
  summaryCard: {
    backgroundColor: '#fff', borderRadius: 20, padding: 20,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.06, shadowRadius: 8, elevation: 3,
    marginBottom: 20,
  },
  summaryRow: { flexDirection: 'row', alignItems: 'center', gap: 14, paddingVertical: 4 },
  summaryText: { flex: 1 },
  summaryLabel: { fontSize: 12, color: '#66667A', marginBottom: 2 },
  summaryValue: { fontSize: 16, fontWeight: '600', color: '#111120' },
  priceIcon: { width: 18, height: 18, alignItems: 'center', justifyContent: 'center' },
  priceIconText: { fontSize: 16, color: '#7C3AED', fontWeight: 'bold' },
  divider: { height: 1, backgroundColor: '#F0F0F4', marginVertical: 12 },
  note: { fontSize: 13, color: '#66667A', textAlign: 'center', marginBottom: 24, lineHeight: 20 },
  btnConfirm: {
    backgroundColor: '#7C3AED', height: 56, borderRadius: 16,
    alignItems: 'center', justifyContent: 'center', marginBottom: 12,
    shadowColor: '#7C3AED', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.35, shadowRadius: 12, elevation: 5,
  },
  btnDisabled: { opacity: 0.7 },
  btnConfirmText: { color: '#fff', fontSize: 17, fontWeight: '700' },
  btnBack: {
    height: 48, borderRadius: 16, alignItems: 'center', justifyContent: 'center',
    backgroundColor: '#F0F0F4',
  },
  btnBackText: { color: '#333344', fontSize: 15, fontWeight: '600' },
  successContainer: {
    flex: 1, alignItems: 'center', justifyContent: 'center', padding: 32,
  },
  successIcon: {
    width: 88, height: 88, borderRadius: 44, backgroundColor: '#F3E8FF',
    alignItems: 'center', justifyContent: 'center', marginBottom: 24,
  },
  successTitle: { fontSize: 28, fontWeight: 'bold', color: '#111120', marginBottom: 8, textAlign: 'center' },
  successRef: { fontSize: 14, color: '#7C3AED', fontWeight: '600', marginBottom: 16, textAlign: 'center' },
  successMsg: { fontSize: 15, color: '#66667A', textAlign: 'center', lineHeight: 22, marginBottom: 32 },
  btnDone: {
    backgroundColor: '#7C3AED', height: 56, borderRadius: 16, width: '100%',
    alignItems: 'center', justifyContent: 'center', marginBottom: 12,
    shadowColor: '#7C3AED', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.3, shadowRadius: 10, elevation: 4,
  },
  btnDoneText: { color: '#fff', fontSize: 16, fontWeight: '700' },
  btnSecondary: {
    height: 48, borderRadius: 16, width: '100%',
    alignItems: 'center', justifyContent: 'center', backgroundColor: '#F0F0F4',
  },
  btnSecondaryText: { color: '#333344', fontSize: 15, fontWeight: '600' },
});
