import React, { useState, useEffect, useRef, useCallback } from 'react';
import { StatusBar } from 'expo-status-bar';
import { StyleSheet, View, ActivityIndicator } from 'react-native';
import NetInfo from '@react-native-community/netinfo';
import { NavigationContainerRef } from '@react-navigation/native';
import * as Sentry from '@sentry/react-native';
import { OfflineBar } from './src/components/OfflineBar';
import { useAppStore } from './src/store/appStore';
import * as SecureStore from './src/utils/storage';
import * as LocalAuthentication from 'expo-local-authentication';
import { Briefcase, Wallet, Users, Settings2, Home } from 'lucide-react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import {
  registerForPushNotificationsAsync,
  setupDeeplinkHandler,
} from './src/services/NotificationService';
import { AuthContext } from './src/context/AuthContext';

import { LoginScreen } from './src/screens/LoginScreen';
import { DashboardScreen } from './src/screens/DashboardScreen';
import { WorkScreen } from './src/screens/WorkScreen';
import { RevenueScreen } from './src/screens/RevenueScreen';
import { TeamScreen } from './src/screens/TeamScreen';
import { SettingsScreen } from './src/screens/SettingsScreen';
import { ClientBookingScreen } from './src/screens/ClientBookingScreen';
import { QRScannerScreen } from './src/screens/QRScannerScreen';
import { ProfileScreen } from './src/screens/ProfileScreen';
import { PaymentsScreen } from './src/screens/PaymentsScreen';
import { NotificationsScreen } from './src/screens/NotificationsScreen';
import { StaffListScreen } from './src/screens/StaffListScreen';
import { ServiceListScreen } from './src/screens/ServiceListScreen';
import { ReportsScreen } from './src/screens/ReportsScreen';
import { InvoicesScreen } from './src/screens/InvoicesScreen';
import { ScheduleScreen } from './src/screens/ScheduleScreen';
import AnalyticsScreen from './src/screens/AnalyticsScreen';
import { ClientSearchScreen } from './src/screens/ClientSearchScreen';
import MarketingScreen from './src/screens/MarketingScreen';
import ConsumerNavigator from './src/screens/consumer/ConsumerNavigator';
import { AICopilotFAB } from './src/components/AICopilotFAB';

// Warn in ALL environments — a missing DSN silently disables crash reporting in production,
// which is exactly where it matters most.
if (!process.env.EXPO_PUBLIC_SENTRY_DSN) {
  console.warn('[Sentry] EXPO_PUBLIC_SENTRY_DSN not set — crash reporting disabled');
}

Sentry.init({
  dsn: process.env.EXPO_PUBLIC_SENTRY_DSN ?? '',
  debug: false,
});

export type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
  ConsumerTabs: undefined;
  ClientBooking: { serviceId?: string; clientId?: string; bookingId?: string } | undefined;
  QRScanner: undefined;
  Profile: undefined;
  Payments: undefined;
  Notifications: { conversationId?: string } | undefined;
  StaffList: undefined;
  ServiceList: undefined;
  Reports: undefined;
  Invoices: { invoiceId?: string } | undefined;
  Schedule: undefined;
  Analytics: undefined;
  ClientSearch: undefined;
  Marketing: undefined;
};

export type TabParamList = {
  Home: undefined;
  Work: undefined;
  Revenue: undefined;
  Team: undefined;
  Settings: undefined;
};

const Stack = createNativeStackNavigator<RootStackParamList>();
const Tab = createBottomTabNavigator<TabParamList>();

const TAB_ICONS: Record<string, React.ComponentType<{ size: number; color: string }>> = {
  Home: Home,
  Work: Briefcase,
  // Currency-neutral: tenants bill in their own currency (USD, INR, …), so a rupee glyph
  // in the tab bar was wrong for most of them.
  Revenue: Wallet,
  Team: Users,
  Settings: Settings2,
};

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarIcon: ({ color, size }) => {
          const Icon = TAB_ICONS[route.name];
          return Icon ? <Icon size={size} color={color} /> : null;
        },
        tabBarActiveTintColor: '#7C3AED',
        tabBarInactiveTintColor: '#9999B0',
        tabBarStyle: { borderTopWidth: 1, borderTopColor: '#E4E4EB', backgroundColor: '#fff' },
      })}
    >
      <Tab.Screen name="Home" component={DashboardScreen} />
      <Tab.Screen name="Work" component={WorkScreen} />
      <Tab.Screen name="Revenue" component={RevenueScreen} />
      <Tab.Screen name="Team" component={TeamScreen} />
      <Tab.Screen name="Settings" component={SettingsScreen} />
    </Tab.Navigator>
  );
}

const CONSUMER_ROLES = new Set(['consumer', 'customer', 'client']);

export default Sentry.wrap(function App() {
  const [isLoading, setIsLoading] = useState(true);
  const [userToken, setUserToken] = useState<string | null>(null);
  const [userRole, setUserRole] = useState<string | null>(null);
  const setOffline = useAppStore((s) => s.setOffline);
  const navigationRef = useRef<NavigationContainerRef<RootStackParamList>>(null);
  // Holds the Expo notification subscription so we can clean it up on unmount
  const notifSubRef = useRef<{ remove: () => void } | null>(null);

  // Network connectivity monitor
  useEffect(() => {
    const unsub = NetInfo.addEventListener((state) => {
      setOffline(!state.isConnected);
    });
    return () => unsub();
  }, [setOffline]);

  // Cleanup notification deeplink subscription when app unmounts
  useEffect(() => {
    return () => {
      notifSubRef.current?.remove();
    };
  }, []);

  // Read stored token + role and run biometric gate — NO network call.
  // Token validity is checked lazily: the first real API call returns 401
  // if expired, which triggers the refresh interceptor in apiClient.ts.
  useEffect(() => {
    const checkToken = async () => {
      try {
        const token = await SecureStore.getItemAsync('auth_token');
        if (!token) return; // no token → show Login

        const role = (await SecureStore.getItemAsync('user_role')) ?? 'tenant_owner';

        // Biometric gate: only enforce when device has hardware AND user is enrolled.
        // Devices without biometrics skip the gate and proceed normally.
        const hasHardware = await LocalAuthentication.hasHardwareAsync();
        const isEnrolled = await LocalAuthentication.isEnrolledAsync();

        if (hasHardware && isEnrolled) {
          const result = await LocalAuthentication.authenticateAsync({
            promptMessage: 'Authenticate to access Upkilo',
            cancelLabel: 'Cancel',
            fallbackLabel: 'Use Passcode',
          });
          if (!result.success) {
            // Biometric failed / cancelled — clear session and show Login
            await SecureStore.deleteItemAsync('auth_token');
            await SecureStore.deleteItemAsync('user_role');
            return;
          }
        }

        setUserToken(token);
        setUserRole(role);
      } catch {
        // SecureStore read failure (hardware error) — show Login without deleting tokens
        setUserToken(null);
        setUserRole(null);
      } finally {
        setIsLoading(false);
      }
    };
    checkToken();
  }, []);

  // Register push notifications on mount (independent of auth state)
  useEffect(() => {
    registerForPushNotificationsAsync().catch(() => null);
  }, []);

  // Called by NavigationContainer once the navigation tree is mounted.
  // We store the subscription in a ref so the cleanup useEffect above can remove it.
  const handleNavigationReady = useCallback(() => {
    notifSubRef.current = setupDeeplinkHandler(navigationRef.current) ?? null;
  }, []);

  const handleLoginSuccess = useCallback(async (role: string) => {
    const token = await SecureStore.getItemAsync('auth_token');
    setUserToken(token);
    setUserRole(role);
  }, []);

  // Exposed via AuthContext so any screen can trigger a clean logout.
  // Clears SecureStore, then updates state — the navigator re-renders and
  // shows Login automatically without any navigation.reset() call.
  const handleLogout = useCallback(async () => {
    await SecureStore.deleteItemAsync('auth_token');
    await SecureStore.deleteItemAsync('refresh_token');
    await SecureStore.deleteItemAsync('user_role');
    await SecureStore.deleteItemAsync('user_data');
    setUserToken(null);
    setUserRole(null);
  }, []);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#7C3AED" />
      </View>
    );
  }

  const isConsumer = userRole !== null && CONSUMER_ROLES.has(userRole.toLowerCase());

  return (
    <AuthContext.Provider value={{ logout: handleLogout }}>
      <View style={{ flex: 1 }}>
        <OfflineBar />
        <NavigationContainer ref={navigationRef} onReady={handleNavigationReady}>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            {userToken ? (
              <>
                {isConsumer ? (
                  <Stack.Screen name="ConsumerTabs" component={ConsumerNavigator} />
                ) : (
                  <>
                    <Stack.Screen name="MainTabs" component={MainTabs} />
                    <Stack.Screen name="ClientBooking" component={ClientBookingScreen} />
                    <Stack.Screen name="QRScanner" component={QRScannerScreen} />
                    <Stack.Screen name="Profile" component={ProfileScreen} />
                    <Stack.Screen name="Payments" component={PaymentsScreen} />
                    <Stack.Screen name="Notifications" component={NotificationsScreen} />
                    <Stack.Screen name="StaffList" component={StaffListScreen} />
                    <Stack.Screen name="ServiceList" component={ServiceListScreen} />
                    <Stack.Screen name="Reports" component={ReportsScreen} />
                    {/* Declared in RootStackParamList and targeted by push deeplinks, but the
                        Stack.Screen entries were missing — invoice/schedule notifications
                        resolved to routes that did not exist and silently did nothing. */}
                    <Stack.Screen name="Invoices" component={InvoicesScreen} />
                    <Stack.Screen name="Schedule" component={ScheduleScreen} />
                    {/* Previously orphaned: the screen files existed and called real endpoints,
                        but had no navigation entry, so users could never reach them. */}
                    <Stack.Screen name="Analytics" component={AnalyticsScreen} />
                    <Stack.Screen name="ClientSearch" component={ClientSearchScreen} />
                    <Stack.Screen name="Marketing" component={MarketingScreen} />
                  </>
                )}
              </>
            ) : (
              <Stack.Screen name="Login">
                {(props) => <LoginScreen {...props} onLoginSuccess={handleLoginSuccess} />}
              </Stack.Screen>
            )}
          </Stack.Navigator>
          <StatusBar style="auto" />
        </NavigationContainer>

        {/* Global AI Copilot — only for business roles, not consumers */}
        {userToken && !isConsumer && <AICopilotFAB />}
      </View>
    </AuthContext.Provider>
  );
});

const styles = StyleSheet.create({
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#fff',
  },
});
