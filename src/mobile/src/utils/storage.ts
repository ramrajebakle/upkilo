import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

// Web platform cannot use expo-secure-store (OS keychain unavailable in browser).
// sessionStorage is used as a fallback: it is cleared when the tab closes, reducing
// the window for XSS token theft compared to localStorage.
const webStore = typeof sessionStorage !== 'undefined' ? sessionStorage : null;

export const setItemAsync = async (key: string, value: string) => {
  if (Platform.OS === 'web') {
    try { webStore?.setItem(key, value); } catch {}
    return;
  }
  return SecureStore.setItemAsync(key, value);
};

export const getItemAsync = async (key: string) => {
  if (Platform.OS === 'web') {
    try { return webStore?.getItem(key) ?? null; } catch { return null; }
  }
  return SecureStore.getItemAsync(key);
};

export const deleteItemAsync = async (key: string) => {
  if (Platform.OS === 'web') {
    try { webStore?.removeItem(key); } catch {}
    return;
  }
  return SecureStore.deleteItemAsync(key);
};
