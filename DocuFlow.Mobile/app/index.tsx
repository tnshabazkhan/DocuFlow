import { StyleSheet, Text, View, FlatList, TouchableOpacity, ActivityIndicator, Animated, Alert } from 'react-native';
import { Link, useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useQuery } from '@tanstack/react-query';
import { getDocuments } from '../services/api';
import { Colors } from '../constants/Colors';
import { Ionicons } from '@expo/vector-icons';
import { useEffect, useRef, useState } from 'react';
import * as SecureStore from 'expo-secure-store';
import authService from '../services/authService';

function FadeInItem({ children, index }: { children: React.ReactNode, index: number }) {
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const slideAnim = useRef(new Animated.Value(20)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.spring(fadeAnim, {
        toValue: 1,
        useNativeDriver: true,
        tension: 10,
        friction: 4,
      }),
      Animated.timing(slideAnim, {
        toValue: 0,
        duration: 500,
        delay: index * 100,
        useNativeDriver: true,
      })
    ]).start();
  }, []);

  return (
    <Animated.View style={{ opacity: fadeAnim, transform: [{ translateY: slideAnim }] }}>
      {children}
    </Animated.View>
  );
}

export default function HomeScreen() {
  const router = useRouter();
  const headerFadeAnim = useRef(new Animated.Value(0)).current;
  const headerSlideAnim = useRef(new Animated.Value(-20)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(headerFadeAnim, {
        toValue: 1,
        duration: 800,
        useNativeDriver: true,
      }),
      Animated.timing(headerSlideAnim, {
        toValue: 0,
        duration: 800,
        useNativeDriver: true,
      })
    ]).start();
  }, []);

  const [hasToken, setHasToken] = useState(false);

  useEffect(() => {
    async function checkToken() {
      const token = await SecureStore.getItemAsync('user_token');
      setHasToken(!!token);
    }
    checkToken();
  }, []);

  const { data: documents, isLoading, error, refetch } = useQuery({
    queryKey: ['documents'],
    queryFn: getDocuments,
    enabled: hasToken,
    refetchInterval: 10000,
  });

  const handleLogout = async () => {
    Alert.alert(
      "Logout",
      "Are you sure you want to logout?",
      [
        { text: "Cancel", style: "cancel" },
        { 
          text: "Logout", 
          style: "destructive",
          onPress: async () => {
            await authService.logout();
            // This will trigger the _layout.tsx check and redirect to login
            router.replace('/login');
          }
        }
      ]
    );
  };

  const getStatusInfo = (status: number) => {
    switch (status) {
      case 2: return { text: 'Processed', color: Colors.success, icon: 'checkmark-circle' as const };
      case 1: return { text: 'Analyzing...', color: Colors.warning, icon: 'sync' as const };
      case 3: return { text: 'Failed', color: Colors.error, icon: 'alert-circle' as const };
      default: return { text: 'Uploaded', color: Colors.secondary, icon: 'cloud-upload' as const };
    }
  };

  const renderItem = ({ item, index }: { item: any; index: number }) => {
    const status = getStatusInfo(item.status);
    const isImage = item.fileName?.toLowerCase().match(/\.(jpg|jpeg|png|gif)$/);

    return (
      <FadeInItem index={index}>
        <TouchableOpacity 
          style={styles.card}
          onPress={() => router.push(`/details/${item.id}`)}
          activeOpacity={0.7}
        >
          <View style={styles.iconContainer}>
            <Ionicons 
              name={isImage ? "image-outline" : "document-text-outline"} 
              size={24} 
              color={Colors.primary} 
            />
          </View>
          <View style={styles.contentContainer}>
            <Text style={styles.fileName} numberOfLines={1}>{item.fileName}</Text>
            <View style={styles.metadataRow}>
              <Text style={styles.date}>{new Date(item.uploadDate).toLocaleDateString()}</Text>
              <View style={styles.dot} />
              <View style={styles.statusRow}>
                <Ionicons name={status.icon} size={14} color={status.color} style={{ marginRight: 4 }} />
                <Text style={[styles.statusText, { color: status.color }]}>{status.text}</Text>
              </View>
            </View>
          </View>
          <Ionicons name="chevron-forward" size={20} color={Colors.border} />
        </TouchableOpacity>
      </FadeInItem>
    );
  };

  if (isLoading && !documents) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <StatusBar style="dark" />
      
      <Animated.View style={[styles.welcomeSection, { opacity: headerFadeAnim, transform: [{ translateY: headerSlideAnim }] }]}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <View style={{ flex: 1 }}>
                <Text style={styles.welcomeTitle}>My Documents</Text>
                <Text style={styles.welcomeSub}>Manage and analyze your intelligent documents</Text>
            </View>
            <TouchableOpacity onPress={handleLogout} style={styles.logoutButton}>
                <Ionicons name="log-out-outline" size={24} color={Colors.error} />
            </TouchableOpacity>
        </View>
      </Animated.View>

      <FlatList
        data={documents}
        renderItem={renderItem}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        onRefresh={refetch}
        refreshing={isLoading}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Ionicons name="documents-outline" size={64} color={Colors.border} />
            <Text style={styles.emptyText}>No documents analyzed yet.</Text>
            <TouchableOpacity 
                style={styles.emptyButton}
                onPress={() => router.push('/upload')}
            >
                <Text style={styles.emptyButtonText}>Upload Your First Document</Text>
            </TouchableOpacity>
          </View>
        }
      />

      <Link href="/upload" asChild>
        <TouchableOpacity style={styles.fab} activeOpacity={0.8}>
          <Ionicons name="add" size={30} color="#fff" />
        </TouchableOpacity>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  welcomeSection: {
    paddingHorizontal: 20,
    paddingTop: 10,
    paddingBottom: 20,
    backgroundColor: Colors.surface,
  },
  welcomeTitle: {
    fontSize: 28,
    fontWeight: '800',
    color: Colors.text,
    letterSpacing: -0.5,
  },
  welcomeSub: {
    fontSize: 14,
    color: Colors.textLight,
    marginTop: 4,
  },
  logoutButton: {
    padding: 8,
    marginLeft: 8,
  },
  list: {
    padding: 16,
    paddingBottom: 100,
  },
  card: {
    backgroundColor: Colors.surface,
    padding: 16,
    borderRadius: 16,
    marginBottom: 12,
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.03,
    shadowRadius: 8,
    elevation: 2,
  },
  iconContainer: {
    width: 48,
    height: 48,
    borderRadius: 12,
    backgroundColor: Colors.accent,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 16,
  },
  contentContainer: {
    flex: 1,
  },
  fileName: {
    fontSize: 16,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 4,
  },
  metadataRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  date: {
    fontSize: 13,
    color: Colors.textLight,
  },
  dot: {
    width: 3,
    height: 3,
    borderRadius: 1.5,
    backgroundColor: Colors.border,
    marginHorizontal: 8,
  },
  statusRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusText: {
    fontSize: 13,
    fontWeight: '600',
  },
  emptyContainer: {
    alignItems: 'center',
    marginTop: 80,
    paddingHorizontal: 40,
  },
  emptyText: {
    textAlign: 'center',
    marginTop: 16,
    fontSize: 16,
    color: Colors.textLight,
    marginBottom: 24,
  },
  emptyButton: {
    backgroundColor: Colors.primary,
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 12,
  },
  emptyButtonText: {
    color: '#fff',
    fontWeight: '600',
  },
  fab: {
    position: 'absolute',
    right: 20,
    bottom: 30,
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: Colors.primary,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.3,
    shadowRadius: 12,
    elevation: 8,
  },
});
