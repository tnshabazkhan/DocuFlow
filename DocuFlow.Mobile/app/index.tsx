import { StyleSheet, Text, View, FlatList, TouchableOpacity, ActivityIndicator } from 'react-native';
import { Link, useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useQuery } from '@tanstack/react-query';
import { getDocuments } from '../services/api';

export default function HomeScreen() {
  const router = useRouter();

  const { data: documents, isLoading, error, refetch } = useQuery({
    queryKey: ['documents'],
    queryFn: getDocuments,
    refetchInterval: 10000, // Refresh list every 10 seconds
  });

  const getStatusText = (status: number) => {
    switch (status) {
      case 0: return 'Uploaded';
      case 1: return 'Processing...';
      case 2: return 'Processed';
      case 3: return 'Failed';
      default: return 'Unknown';
    }
  };

  const renderItem = ({ item }: { item: any }) => (
    <TouchableOpacity 
      style={styles.card}
      onPress={() => router.push(`/details/${item.id}`)}
    >
      <View style={{ flex: 1 }}>
        <Text style={styles.fileName} numberOfLines={1}>{item.fileName}</Text>
        <Text style={styles.date}>{new Date(item.uploadDate).toLocaleDateString()}</Text>
      </View>
      <View style={[
        styles.badge, 
        item.status === 2 ? styles.badgeSuccess : item.status === 1 ? styles.badgeWarning : styles.badgeInfo
      ]}>
        <Text style={styles.badgeText}>{getStatusText(item.status)}</Text>
      </View>
    </TouchableOpacity>
  );

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#007bff" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <StatusBar style="auto" />
      
      <FlatList
        data={documents}
        renderItem={renderItem}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        onRefresh={refetch}
        refreshing={isLoading}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No documents yet. Tap "+" to upload.</Text>
        }
      />

      <Link href="/upload" asChild>
        <TouchableOpacity style={styles.fab}>
          <Text style={styles.fabText}>+</Text>
        </TouchableOpacity>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8f9fa',
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  list: {
    padding: 16,
  },
  card: {
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  fileName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1a1a1a',
    marginBottom: 4,
    paddingRight: 8,
  },
  date: {
    fontSize: 14,
    color: '#6c757d',
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    minWidth: 80,
    alignItems: 'center',
  },
  badgeSuccess: {
    backgroundColor: '#d1e7dd',
  },
  badgeWarning: {
    backgroundColor: '#fff3cd',
  },
  badgeInfo: {
    backgroundColor: '#cfe2ff',
  },
  badgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#000',
  },
  emptyText: {
    textAlign: 'center',
    marginTop: 40,
    color: '#6c757d',
  },
  fab: {
    position: 'absolute',
    right: 24,
    bottom: 24,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#007bff',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#007bff',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 6,
    elevation: 5,
  },
  fabText: {
    fontSize: 32,
    color: '#fff',
    lineHeight: 32,
  },
});
