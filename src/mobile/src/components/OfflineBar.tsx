import React, { useEffect } from 'react';
import { View, Text, StyleSheet, Animated } from 'react-native';
import { WifiOff } from 'lucide-react-native';
import { useAppStore } from '../store/appStore';

export function OfflineBar() {
  const isOffline = useAppStore((s) => s.isOffline);
  const opacity = React.useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.timing(opacity, {
      toValue: isOffline ? 1 : 0,
      duration: 300,
      useNativeDriver: true,
    }).start();
  }, [isOffline, opacity]);

  if (!isOffline) return null;

  return (
    <Animated.View style={[styles.bar, { opacity }]} accessibilityRole="alert" accessibilityLiveRegion="assertive">
      <WifiOff size={14} color="#fff" />
      <Text style={styles.text}>No internet connection</Text>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  bar: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 9999,
    backgroundColor: '#DC2626',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 8,
    paddingHorizontal: 16,
  },
  text: {
    color: '#fff',
    fontSize: 13,
    fontWeight: '600',
  },
});
