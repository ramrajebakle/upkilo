import React from 'react';
import { View, Text, StyleSheet, Modal, TouchableOpacity } from 'react-native';
import { Compass, FileText, CheckCircle } from 'lucide-react-native';

interface OrbitSheetProps {
  visible: boolean;
  onClose: () => void;
}

export function OrbitSheet({ visible, onClose }: OrbitSheetProps) {
  return (
    <Modal visible={visible} transparent animationType="slide">
      <View style={styles.overlay}>
        <TouchableOpacity style={styles.dismissArea} onPress={onClose} />
        <View style={styles.sheet}>
          <View style={styles.handle} />
          <Text style={styles.title}>Your Orbit</Text>
          
          <Text style={styles.sectionTitle}>ACTIVE CONTEXTS</Text>
          
          <TouchableOpacity style={styles.itemActive}>
            <View style={styles.iconContainer}>
              <CheckCircle size={20} color="#007AFF" />
            </View>
            <View>
              <Text style={styles.itemTitle}>Q3 Revenue Planning</Text>
              <Text style={styles.itemSub}>3 tasks pending</Text>
            </View>
          </TouchableOpacity>
          
          <TouchableOpacity style={styles.item}>
            <View style={styles.iconContainer}>
              <FileText size={20} color="#666" />
            </View>
            <View>
              <Text style={styles.itemTitle}>Onboarding Template</Text>
              <Text style={styles.itemSub}>Drafting in progress</Text>
            </View>
          </TouchableOpacity>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.4)',
    justifyContent: 'flex-end',
  },
  dismissArea: {
    flex: 1,
  },
  sheet: {
    backgroundColor: '#F8F8FA',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    minHeight: 300,
    padding: 24,
    paddingTop: 12,
  },
  handle: {
    width: 40,
    height: 4,
    backgroundColor: '#E4E4EB',
    borderRadius: 2,
    alignSelf: 'center',
    marginBottom: 24,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#111120',
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 11,
    fontWeight: 'bold',
    color: '#9999B0',
    letterSpacing: 0.5,
    marginBottom: 12,
  },
  item: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    gap: 16,
    borderRadius: 12,
    marginBottom: 8,
  },
  itemActive: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    gap: 16,
    borderRadius: 12,
    marginBottom: 8,
    backgroundColor: '#fff',
    borderLeftWidth: 3,
    borderLeftColor: '#007AFF',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 8,
    backgroundColor: '#F0F0F4',
    alignItems: 'center',
    justifyContent: 'center',
  },
  itemTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333344',
  },
  itemSub: {
    fontSize: 13,
    color: '#9999B0',
    marginTop: 2,
  }
});
