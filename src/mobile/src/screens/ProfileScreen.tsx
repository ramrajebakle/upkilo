import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  ActivityIndicator,
  ScrollView,
  Alert,
  Modal,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import * as SecureStore from '../utils/storage';
import { apiClient } from '../api/apiClient';
import { useAuth } from '../context/AuthContext';

interface Profile {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
}

export function ProfileScreen() {
  const { logout: appLogout } = useAuth();
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');

  // Change-password modal state (replaces Alert.prompt which is iOS-only)
  const [pwdModalVisible, setPwdModalVisible] = useState(false);
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [changingPwd, setChangingPwd] = useState(false);

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    try {
      const res = await apiClient.get('/auth/me');
      const data = res.data as Profile;
      setProfile(data);
      setFirstName(data.firstName);
      setLastName(data.lastName);
      setPhone(data.phone || '');
    } catch {
      Alert.alert('Error', 'Failed to load profile');
    } finally {
      setLoading(false);
    }
  };

  const saveProfile = async () => {
    setSaving(true);
    try {
      await apiClient.put('/profile', { firstName, lastName, phone });
      Alert.alert('Success', 'Profile updated');
    } catch {
      Alert.alert('Error', 'Failed to save profile');
    } finally {
      setSaving(false);
    }
  };

  const changePassword = () => {
    setOldPassword('');
    setNewPassword('');
    setConfirmPassword('');
    setPwdModalVisible(true);
  };

  const submitChangePassword = async () => {
    if (newPassword !== confirmPassword) {
      Alert.alert('Error', 'Passwords do not match');
      return;
    }
    if (newPassword.length < 8) {
      Alert.alert('Error', 'New password must be at least 8 characters');
      return;
    }
    setChangingPwd(true);
    try {
      // POST /profile/change-password; the DTO field is `currentPassword`.
      await apiClient.post('/profile/change-password', { currentPassword: oldPassword, newPassword });
      setPwdModalVisible(false);
      Alert.alert('Success', 'Password changed successfully');
    } catch {
      Alert.alert('Error', 'Failed to change password. Check your current password.');
    } finally {
      setChangingPwd(false);
    }
  };

  const logout = async () => {
    // Revoke refresh token on backend first, then clear local state via AuthContext.
    // AuthContext.logout() sets userToken=null in App.tsx, which causes the navigator
    // to re-render and show the Login screen — no navigation.reset() needed.
    try {
      const refreshToken = await SecureStore.getItemAsync('refresh_token');
      if (refreshToken) {
        await apiClient.post('/auth/logout', { refreshToken }).catch(() => null);
      }
    } finally {
      await appLogout();
    }
  };

  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#007AFF" />
      </View>
    );
  }

  const initials =
    (firstName.charAt(0) || '') + (lastName.charAt(0) || '');

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.scroll}>
        <View style={styles.avatarCircle}>
          <Text style={styles.avatarText}>{initials.toUpperCase()}</Text>
        </View>
        <Text style={styles.roleText}>{profile?.role}</Text>
        <Text style={styles.emailText}>{profile?.email}</Text>

        <View style={styles.section}>
          <Text style={styles.label}>First Name</Text>
          <TextInput
            style={styles.input}
            value={firstName}
            onChangeText={setFirstName}
            placeholder="First Name"
          />
          <Text style={styles.label}>Last Name</Text>
          <TextInput
            style={styles.input}
            value={lastName}
            onChangeText={setLastName}
            placeholder="Last Name"
          />
          <Text style={styles.label}>Phone</Text>
          <TextInput
            style={styles.input}
            value={phone}
            onChangeText={setPhone}
            placeholder="Phone"
            keyboardType="phone-pad"
          />
        </View>

        <TouchableOpacity style={styles.primaryBtn} onPress={saveProfile} disabled={saving}>
          <Text style={styles.primaryBtnText}>{saving ? 'Saving...' : 'Save Changes'}</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.secondaryBtn} onPress={changePassword}>
          <Text style={styles.secondaryBtnText}>Change Password</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.logoutBtn} onPress={logout}>
          <Text style={styles.logoutBtnText}>Logout</Text>
        </TouchableOpacity>
      </ScrollView>

      {/* Cross-platform change-password modal — replaces Alert.prompt (iOS-only) */}
      <Modal
        visible={pwdModalVisible}
        transparent
        animationType="slide"
        onRequestClose={() => setPwdModalVisible(false)}
      >
        <KeyboardAvoidingView
          style={styles.modalOverlay}
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        >
          <View style={styles.modalCard}>
            <Text style={styles.modalTitle}>Change Password</Text>
            <TextInput
              style={styles.input}
              placeholder="Current Password"
              secureTextEntry
              value={oldPassword}
              onChangeText={setOldPassword}
              autoCapitalize="none"
            />
            <TextInput
              style={[styles.input, { marginTop: 12 }]}
              placeholder="New Password (min 8 chars)"
              secureTextEntry
              value={newPassword}
              onChangeText={setNewPassword}
              autoCapitalize="none"
            />
            <TextInput
              style={[styles.input, { marginTop: 12 }]}
              placeholder="Confirm New Password"
              secureTextEntry
              value={confirmPassword}
              onChangeText={setConfirmPassword}
              autoCapitalize="none"
            />
            <View style={styles.modalBtns}>
              <TouchableOpacity style={styles.modalCancel} onPress={() => setPwdModalVisible(false)}>
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.modalConfirm} onPress={submitChangePassword} disabled={changingPwd}>
                <Text style={styles.modalConfirmText}>{changingPwd ? 'Saving...' : 'Update'}</Text>
              </TouchableOpacity>
            </View>
          </View>
        </KeyboardAvoidingView>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  scroll: { padding: 24, alignItems: 'center' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  avatarCircle: {
    width: 80, height: 80, borderRadius: 40, backgroundColor: '#007AFF',
    justifyContent: 'center', alignItems: 'center', marginBottom: 8,
  },
  avatarText: { color: '#fff', fontSize: 28, fontWeight: '700' },
  roleText: { fontSize: 14, color: '#888', marginBottom: 4 },
  emailText: { fontSize: 14, color: '#555', marginBottom: 24 },
  section: { width: '100%', marginBottom: 16 },
  label: { fontSize: 13, color: '#555', marginBottom: 4, marginTop: 12 },
  input: {
    borderWidth: 1, borderColor: '#ddd', borderRadius: 8,
    paddingHorizontal: 12, paddingVertical: 10, fontSize: 15, color: '#111',
  },
  primaryBtn: {
    backgroundColor: '#007AFF', borderRadius: 10, paddingVertical: 14,
    width: '100%', alignItems: 'center', marginTop: 8,
  },
  primaryBtnText: { color: '#fff', fontWeight: '600', fontSize: 16 },
  secondaryBtn: {
    borderWidth: 1, borderColor: '#007AFF', borderRadius: 10, paddingVertical: 14,
    width: '100%', alignItems: 'center', marginTop: 12,
  },
  secondaryBtnText: { color: '#007AFF', fontWeight: '600', fontSize: 16 },
  logoutBtn: {
    marginTop: 24, paddingVertical: 14, width: '100%', alignItems: 'center',
  },
  logoutBtnText: { color: '#FF3B30', fontWeight: '600', fontSize: 16 },
  modalOverlay: {
    flex: 1, backgroundColor: 'rgba(0,0,0,0.4)', justifyContent: 'flex-end',
  },
  modalCard: {
    backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20,
    padding: 24, paddingBottom: 40,
  },
  modalTitle: { fontSize: 18, fontWeight: '700', color: '#111', marginBottom: 20 },
  modalBtns: { flexDirection: 'row', gap: 12, marginTop: 20 },
  modalCancel: {
    flex: 1, paddingVertical: 14, borderRadius: 10, borderWidth: 1,
    borderColor: '#ddd', alignItems: 'center',
  },
  modalCancelText: { color: '#555', fontWeight: '600' },
  modalConfirm: {
    flex: 1, paddingVertical: 14, borderRadius: 10,
    backgroundColor: '#007AFF', alignItems: 'center',
  },
  modalConfirmText: { color: '#fff', fontWeight: '600' },
});
