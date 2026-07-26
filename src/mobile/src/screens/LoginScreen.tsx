import React, { useState } from 'react';
import {
  StyleSheet,
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { getLocales } from 'expo-localization';
import { apiClient } from '../api/apiClient';
import * as SecureStore from '../utils/storage';

interface LoginScreenProps {
  onLoginSuccess: (role: string) => void;
}

const STRINGS: Record<string, {
  title: string; subtitle: string; emailPh: string; passwordPh: string; signIn: string;
  error: string; fieldError: string; twoFaTitle: string; twoFaSubtitle: string; codePh: string; verify: string;
}> = {
  hi: {
    title: 'Upkilo स्टाफ',
    subtitle: 'अपना शेड्यूल प्रबंधित करने के लिए साइन इन करें',
    emailPh: 'ईमेल',
    passwordPh: 'पासवर्ड',
    signIn: 'साइन इन करें',
    error: 'लॉगिन विफल',
    fieldError: 'कृपया सभी फ़ील्ड भरें',
    twoFaTitle: 'दो-चरणीय सत्यापन',
    twoFaSubtitle: 'अपने प्रमाणक ऐप से 6-अंकीय कोड दर्ज करें',
    codePh: 'सत्यापन कोड',
    verify: 'सत्यापित करें',
  },
  default: {
    title: 'Upkilo Staff',
    subtitle: 'Sign in to manage your schedule',
    emailPh: 'Email',
    passwordPh: 'Password',
    signIn: 'Sign In',
    error: 'Login Failed',
    fieldError: 'Please fill in all fields',
    twoFaTitle: 'Two-Factor Verification',
    twoFaSubtitle: 'Enter the 6-digit code from your authenticator app',
    codePh: 'Verification code',
    verify: 'Verify',
  },
};

function getStrings() {
  const lang = getLocales()[0]?.languageCode ?? 'default';
  return STRINGS[lang] ?? STRINGS['default'];
}

export function LoginScreen({ onLoginSuccess }: LoginScreenProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [twoFactorRequired, setTwoFactorRequired] = useState(false);
  const [code, setCode] = useState('');
  const s = getStrings();

  // Persist the full session (access token, refresh token, role) and hand control back to App.
  // FIX (M-01): the refresh token MUST be stored — apiClient's 401 interceptor reads it from
  // SecureStore to silently re-auth. Without it, every session died at access-token expiry.
  // FIX (M-02): role comes from the `user` object, not a (non-existent) top-level `role` field.
  const persistAndFinish = async (data: any) => {
    const role: string = data?.user?.role ?? data?.role ?? 'tenant_owner';
    await SecureStore.setItemAsync('auth_token', data.token);
    if (data.refreshToken) await SecureStore.setItemAsync('refresh_token', data.refreshToken);
    await SecureStore.setItemAsync('user_role', role);
    onLoginSuccess(role);
  };

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert(s.error, s.fieldError);
      return;
    }

    setLoading(true);
    try {
      const response = await apiClient.post('/auth/login', { email, password });

      // FIX (M-03): a 2FA-gated account returns { twoFactorRequired: true } with NO token.
      // Switch to the code-entry step instead of treating it as a failed login.
      if (response.data?.twoFactorRequired) {
        setTwoFactorRequired(true);
        return;
      }

      if (response.data?.token) {
        await persistAndFinish(response.data);
      } else {
        throw new Error('No token received');
      }
    } catch (error: any) {
      console.error('Login error:', error?.response?.status ?? error?.message);
      Alert.alert(s.error, error.response?.data?.message || 'Invalid credentials');
    } finally {
      setLoading(false);
    }
  };

  const handleVerify2fa = async () => {
    if (!code) {
      Alert.alert(s.error, s.fieldError);
      return;
    }
    setLoading(true);
    try {
      const response = await apiClient.post('/auth/verify-2fa', { email, code });
      if (response.data?.token) {
        await persistAndFinish(response.data);
      } else {
        throw new Error('No token received');
      }
    } catch (error: any) {
      console.error('2FA verify error:', error?.response?.status ?? error?.message);
      Alert.alert(s.error, error.response?.data?.message || 'Invalid verification code');
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      style={styles.container}
    >
      <View style={styles.formContainer}>
        {twoFactorRequired ? (
          <>
            <Text style={styles.title}>{s.twoFaTitle}</Text>
            <Text style={styles.subtitle}>{s.twoFaSubtitle}</Text>

            <TextInput
              style={styles.input}
              placeholder={s.codePh}
              placeholderTextColor="#999"
              value={code}
              onChangeText={setCode}
              keyboardType="number-pad"
              autoCapitalize="none"
              autoFocus
              accessibilityLabel={s.codePh}
            />

            <TouchableOpacity
              style={styles.button}
              onPress={handleVerify2fa}
              disabled={loading}
              accessibilityLabel={s.verify}
              accessibilityRole="button"
            >
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.buttonText}>{s.verify}</Text>
              )}
            </TouchableOpacity>
          </>
        ) : (
          <>
            <Text style={styles.title}>{s.title}</Text>
            <Text style={styles.subtitle}>{s.subtitle}</Text>

            <TextInput
              style={styles.input}
              placeholder={s.emailPh}
              placeholderTextColor="#999"
              value={email}
              onChangeText={setEmail}
              keyboardType="email-address"
              autoCapitalize="none"
              accessibilityLabel={s.emailPh}
            />

            <TextInput
              style={styles.input}
              placeholder={s.passwordPh}
              placeholderTextColor="#999"
              value={password}
              onChangeText={setPassword}
              secureTextEntry
              accessibilityLabel={s.passwordPh}
            />

            <TouchableOpacity
              style={styles.button}
              onPress={handleLogin}
              disabled={loading}
              accessibilityLabel={s.signIn}
              accessibilityRole="button"
            >
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.buttonText}>{s.signIn}</Text>
              )}
            </TouchableOpacity>
          </>
        )}
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  formContainer: {
    flex: 1,
    justifyContent: 'center',
    padding: 30,
  },
  title: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#000',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
    marginBottom: 40,
  },
  input: {
    height: 56,
    borderWidth: 1,
    borderColor: '#eee',
    borderRadius: 12,
    paddingHorizontal: 16,
    marginBottom: 16,
    fontSize: 16,
    backgroundColor: '#fbfbfb',
  },
  button: {
    height: 56,
    backgroundColor: '#7C3AED',
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 10,
    shadowColor: '#7C3AED',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 3,
  },
  buttonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
});
