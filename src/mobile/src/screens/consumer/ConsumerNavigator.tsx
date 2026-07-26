/**
 * Consumer-facing app navigator.
 * Separate navigation tree from the business-owner app.
 * Mounted when user role is 'consumer' | 'customer' | 'client'.
 */
import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Search, CalendarDays, Star } from 'lucide-react-native';
import ConsumerDiscoverScreen from './ConsumerDiscoverScreen';
import ConsumerHistoryScreen from './ConsumerHistoryScreen';
import ConsumerLoyaltyScreen from './ConsumerLoyaltyScreen';
import ConsumerBookScreen from './ConsumerBookScreen';
import ConsumerConfirmScreen from './ConsumerConfirmScreen';

const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

const ACTIVE_COLOR = '#7C3AED';
const INACTIVE_COLOR = '#9CA3AF';

function DiscoverStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="ConsumerDiscover" component={ConsumerDiscoverScreen} />
      <Stack.Screen name="ConsumerBook" component={ConsumerBookScreen} options={{ headerShown: true, title: 'Book Service' }} />
      <Stack.Screen name="ConsumerConfirm" component={ConsumerConfirmScreen} options={{ headerShown: true, title: 'Confirm Booking' }} />
    </Stack.Navigator>
  );
}

export default function ConsumerNavigator() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: ACTIVE_COLOR,
        tabBarInactiveTintColor: INACTIVE_COLOR,
        tabBarStyle: { borderTopWidth: 1, borderTopColor: '#E4E4EB', backgroundColor: '#fff' },
        tabBarIcon: ({ color, size }) => {
          if (route.name === 'Discover') return <Search size={size} color={color} />;
          if (route.name === 'Bookings') return <CalendarDays size={size} color={color} />;
          if (route.name === 'Loyalty') return <Star size={size} color={color} />;
          return null;
        },
      })}
    >
      <Tab.Screen name="Discover" component={DiscoverStack} />
      <Tab.Screen name="Bookings" component={ConsumerHistoryScreen} />
      <Tab.Screen name="Loyalty" component={ConsumerLoyaltyScreen} />
    </Tab.Navigator>
  );
}
