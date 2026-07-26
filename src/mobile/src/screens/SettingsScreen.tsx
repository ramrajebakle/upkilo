import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  Switch,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  SectionList,
  Alert,
  Linking,
  ActivityIndicator,
  Modal,
  TextInput,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { apiClient } from '../api/apiClient';
import { RootStackParamList } from '../../App';

type NavProp = NativeStackNavigationProp<RootStackParamList>;

// Must match PrivacyController.RequestAccountDeletion's expected confirmation string.
const DELETE_PHRASE = 'DELETE MY ACCOUNT';

interface NotifPrefs {
  emailNotifications: boolean;
  smsNotifications: boolean;
  pushNotifications: boolean;
}

export function SettingsScreen() {
  const navigation = useNavigation<NavProp>();
  const [prefs, setPrefs] = useState<NotifPrefs>({
    emailNotifications: true,
    smsNotifications: false,
    pushNotifications: true,
  });
  const [loading, setLoading] = useState(true);
  const [deleteModalVisible, setDeleteModalVisible] = useState(false);
  const [deleteConfirmText, setDeleteConfirmText] = useState('');
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    apiClient.get('/notification-preferences')
      .then(res => setPrefs(res.data as NotifPrefs))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const savePrefs = async (newPrefs: NotifPrefs) => {
    setPrefs(newPrefs);
    try {
      await apiClient.put('/notification-preferences', newPrefs);
    } catch {
      Alert.alert('Error', 'Failed to save preferences');
    }
  };

  const exportData = async () => {
    try {
      await apiClient.post('/privacy/export', {});
      Alert.alert('Success', 'Your data export has been initiated. You will receive an email shortly.');
    } catch {
      Alert.alert('Error', 'Failed to export data');
    }
  };

  // The server requires the user to type DELETE_PHRASE exactly. Collect it here rather than
  // sending a constant — hardcoding it in the client would defeat the confirmation guard.
  const deleteAccount = () => {
    setDeleteConfirmText('');
    setDeleteModalVisible(true);
  };

  const submitAccountDeletion = async () => {
    if (deleteConfirmText.trim() !== DELETE_PHRASE) return;
    setDeleting(true);
    try {
      // GDPR/DPDP deletion runs through the privacy controller, not an auth route.
      await apiClient.post('/privacy/delete-account', { confirmationText: DELETE_PHRASE });
      setDeleteModalVisible(false);
      navigation.reset({ index: 0, routes: [{ name: 'Login' }] });
    } catch {
      Alert.alert('Error', 'Failed to delete account');
    } finally {
      setDeleting(false);
    }
  };

  const sections = [
    {
      title: 'Account',
      data: [
        {
          key: 'profile',
          label: 'Profile',
          onPress: () => navigation.navigate('Profile'),
          showArrow: true,
        },
        {
          key: 'changePassword',
          label: 'Change Password',
          onPress: () => navigation.navigate('Profile'),
          showArrow: true,
        },
      ],
    },
    {
      title: 'Notifications',
      data: [
        {
          key: 'email',
          label: 'Email Notifications',
          isSwitch: true,
          value: prefs.emailNotifications,
          onToggle: (v: boolean) => savePrefs({ ...prefs, emailNotifications: v }),
        },
        {
          key: 'sms',
          label: 'SMS Notifications',
          isSwitch: true,
          value: prefs.smsNotifications,
          onToggle: (v: boolean) => savePrefs({ ...prefs, smsNotifications: v }),
        },
        {
          key: 'push',
          label: 'Push Notifications',
          isSwitch: true,
          value: prefs.pushNotifications,
          onToggle: (v: boolean) => savePrefs({ ...prefs, pushNotifications: v }),
        },
      ],
    },
    {
      title: 'Privacy',
      data: [
        { key: 'export', label: 'Download My Data', onPress: exportData, showArrow: true },
        { key: 'delete', label: 'Delete Account', onPress: deleteAccount, showArrow: true, danger: true },
      ],
    },
    {
      title: 'About',
      data: [
        { key: 'version', label: 'App Version', value: '1.0.0' },
        { key: 'privacy', label: 'Privacy Policy', onPress: () => Linking.openURL('https://upkilo.com/privacy'), showArrow: true },
        { key: 'terms', label: 'Terms of Service', onPress: () => Linking.openURL('https://upkilo.com/terms'), showArrow: true },
      ],
    },
  ];

  type SectionItem = {
    key: string;
    label: string;
    onPress?: () => void;
    showArrow?: boolean;
    danger?: boolean;
    isSwitch?: boolean;
    value?: boolean | string;
    onToggle?: (v: boolean) => void;
  };

  const renderItem = ({ item }: { item: SectionItem }) => (
    <TouchableOpacity
      style={styles.row}
      onPress={item.onPress}
      disabled={!item.onPress && !item.isSwitch}
      activeOpacity={item.onPress ? 0.7 : 1}
    >
      <Text style={[styles.rowLabel, item.danger && styles.dangerText]}>{item.label}</Text>
      {item.isSwitch ? (
        <Switch
          value={item.value as boolean}
          onValueChange={item.onToggle}
          trackColor={{ false: '#ddd', true: '#007AFF' }}
          thumbColor={item.value ? '#fff' : '#f4f3f4'}
        />
      ) : item.value && typeof item.value === 'string' ? (
        <Text style={styles.rowValue}>{item.value}</Text>
      ) : item.showArrow ? (
        <Text style={styles.arrow}>›</Text>
      ) : null}
    </TouchableOpacity>
  );

  if (loading) {
    return <View style={styles.center}><ActivityIndicator size="large" color="#007AFF" /></View>;
  }

  return (
    <SafeAreaView style={styles.container}>
      <SectionList
        sections={sections}
        keyExtractor={item => item.key}
        renderItem={renderItem}
        renderSectionHeader={({ section }) => (
          <Text style={styles.sectionHeader}>{section.title}</Text>
        )}
        contentContainerStyle={styles.list}
      />

      <Modal
        visible={deleteModalVisible}
        transparent
        animationType="fade"
        onRequestClose={() => setDeleteModalVisible(false)}
      >
        <View style={styles.modalBackdrop}>
          <View style={styles.modalCard}>
            <Text style={styles.modalTitle}>Delete Account</Text>
            <Text style={styles.modalBody}>
              This permanently deletes your account and cannot be undone. Type{' '}
              <Text style={styles.modalPhrase}>{DELETE_PHRASE}</Text> to confirm.
            </Text>
            <TextInput
              style={styles.modalInput}
              value={deleteConfirmText}
              onChangeText={setDeleteConfirmText}
              autoCapitalize="characters"
              autoCorrect={false}
              placeholder={DELETE_PHRASE}
              testID="input-delete-confirm"
            />
            <View style={styles.modalActions}>
              <TouchableOpacity
                style={styles.modalCancel}
                onPress={() => setDeleteModalVisible(false)}
                disabled={deleting}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[
                  styles.modalDelete,
                  deleteConfirmText.trim() !== DELETE_PHRASE && styles.modalDeleteDisabled,
                ]}
                onPress={submitAccountDeletion}
                disabled={deleteConfirmText.trim() !== DELETE_PHRASE || deleting}
                testID="btn-delete-confirm"
              >
                {deleting ? (
                  <ActivityIndicator color="#fff" />
                ) : (
                  <Text style={styles.modalDeleteText}>Delete</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F2F2F7' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  list: { paddingBottom: 40 },
  modalBackdrop: { flex: 1, backgroundColor: 'rgba(0,0,0,0.45)', justifyContent: 'center', paddingHorizontal: 24 },
  modalCard: { backgroundColor: '#fff', borderRadius: 14, padding: 20 },
  modalTitle: { fontSize: 18, fontWeight: '700', marginBottom: 8, color: '#111' },
  modalBody: { fontSize: 14, color: '#444', lineHeight: 20, marginBottom: 16 },
  modalPhrase: { fontWeight: '700', color: '#111' },
  modalInput: { borderWidth: 1, borderColor: '#D1D1D6', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 10, fontSize: 15, marginBottom: 16 },
  modalActions: { flexDirection: 'row', justifyContent: 'flex-end', gap: 12 },
  modalCancel: { paddingVertical: 10, paddingHorizontal: 16 },
  modalCancelText: { fontSize: 15, color: '#007AFF', fontWeight: '600' },
  modalDelete: { backgroundColor: '#FF3B30', borderRadius: 8, paddingVertical: 10, paddingHorizontal: 20, minWidth: 88, alignItems: 'center' },
  modalDeleteDisabled: { backgroundColor: '#FFB3AE' },
  modalDeleteText: { color: '#fff', fontSize: 15, fontWeight: '600' },
  sectionHeader: { fontSize: 13, fontWeight: '600', color: '#888', paddingHorizontal: 20, paddingTop: 24, paddingBottom: 8, textTransform: 'uppercase', letterSpacing: 0.5 },
  row: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    backgroundColor: '#fff', paddingHorizontal: 20, paddingVertical: 14,
    borderBottomWidth: 1, borderColor: '#F0F0F0',
  },
  rowLabel: { fontSize: 16, color: '#111' },
  rowValue: { fontSize: 15, color: '#888' },
  arrow: { fontSize: 20, color: '#C7C7CC' },
  dangerText: { color: '#FF3B30' },
});
