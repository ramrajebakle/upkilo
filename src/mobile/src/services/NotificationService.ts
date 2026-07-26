import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import Constants from 'expo-constants';
import { Platform } from 'react-native';
import { apiClient } from '../api/apiClient';

// EAS injects the project id into the resolved app config at build time. Reading it from there
// keeps push registration working without a separate env var — the previous
// `EXPO_PUBLIC_PROJECT_ID` was never set by any build profile, so this silently fell back to a
// placeholder id and push tokens could not be issued.
function getEasProjectId(): string | undefined {
  return (
    Constants.expoConfig?.extra?.eas?.projectId ??
    (Constants as any).easConfig?.projectId ??
    process.env.EXPO_PUBLIC_PROJECT_ID
  );
}

export async function registerForPushNotificationsAsync() {
  let token;

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('default', {
      name: 'default',
      importance: Notifications.AndroidImportance.MAX,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#FF231F7C',
    });
  }

  if (Device.isDevice) {
    const { status: existingStatus } = await Notifications.getPermissionsAsync();
    let finalStatus = existingStatus;
    if (existingStatus !== 'granted') {
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }
    if (finalStatus !== 'granted') {
      console.log('Failed to get push token for push notification!');
      return;
    }
    const projectId = getEasProjectId();
    if (!projectId) {
      console.warn('[NotificationService] No EAS projectId in app config — push disabled.');
      return;
    }

    token = (await Notifications.getExpoPushTokenAsync({ projectId })).data;
    
    // Removed console.log('Push Token:', token) for production readiness

    // Register with backend. Route is /notifications/push/mobile/register and the DTO field
    // is `deviceToken` — the previous '/notifications/tokens' with `token` 404'd on every launch.
    try {
      await apiClient.post('/notifications/push/mobile/register', {
        deviceToken: token,
        platform: Platform.OS === 'ios' ? 'APNS' : 'FCM',
        deviceModel: Device.modelName,
        osVersion: Device.osVersion
      });
    } catch (error) {
      console.error('Failed to register push token with backend', error);
    }
  } else {
    console.log('Must use physical device for Push Notifications');
  }

  return token;
}

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
});

/**
 * M4: Push notification deeplinks — tap notification → navigate to specific screen.
 *
 * Notification data schema:
 *   { type: 'booking', bookingId: '...' }     → BookingDetailScreen
 *   { type: 'invoice', invoiceId: '...' }      → InvoicesScreen
 *   { type: 'message', conversationId: '...' } → MessagesScreen
 *   { type: 'review', reviewId: '...' }        → ReviewsScreen
 *
 * Usage: call setupDeeplinkHandler(navigation) in App.tsx after the navigation tree is ready.
 */
export function resolveDeeplinkRoute(notificationData: Record<string, string>): { screen: string; params?: Record<string, string> } | null {
  const { type } = notificationData;
  switch (type) {
    case 'booking':
      return notificationData.bookingId
        ? { screen: 'ClientBooking', params: { bookingId: notificationData.bookingId } }
        : { screen: 'Schedule' };
    case 'invoice':
      return notificationData.invoiceId
        ? { screen: 'Invoices', params: { invoiceId: notificationData.invoiceId } }
        : { screen: 'Invoices' };
    case 'message':
      return notificationData.conversationId
        ? { screen: 'Notifications', params: { conversationId: notificationData.conversationId } }
        : { screen: 'Notifications' };
    case 'review':
      // 'Dashboard' is not a registered route — the dashboard is the Home tab inside MainTabs.
      return { screen: 'MainTabs' };
    case 'upsell':
      return { screen: 'Settings', params: { section: 'billing' } };
    default:
      return null;
  }
}

export function setupDeeplinkHandler(navigation: any) {
  const safeNavigate = (nav: any, route: { screen: string; params?: Record<string, string> }) => {
    try {
      nav.navigate(route.screen, route.params);
    } catch (err) {
      console.error('[NotificationService] deeplink navigation failed:', err);
    }
  };

  // Handle notifications that open the app from background/killed state.
  // Unavailable on web (no native notifications module) — swallow rather than
  // surfacing an unhandled rejection at startup.
  Notifications.getLastNotificationResponseAsync()
    .then(response => {
      if (response?.notification?.request?.content?.data) {
        const route = resolveDeeplinkRoute(response.notification.request.content.data as Record<string, string>);
        if (route && navigation) safeNavigate(navigation, route);
      }
    })
    .catch(err => {
      console.warn('[NotificationService] last notification response unavailable:', err);
    });

  // Handle notifications tapped while app is in foreground/background
  const subscription = Notifications.addNotificationResponseReceivedListener(response => {
    const data = response.notification.request.content.data as Record<string, string>;
    const route = resolveDeeplinkRoute(data);
    if (route && navigation) safeNavigate(navigation, route);
  });

  return subscription; // Return for cleanup: subscription.remove() in useEffect
}
