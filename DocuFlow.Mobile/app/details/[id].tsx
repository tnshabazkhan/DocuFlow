import { StyleSheet, Text, View, ScrollView, ActivityIndicator, RefreshControl, TouchableOpacity, Alert } from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { getDocument, getDocumentContentUrl } from '../../services/api';
import { useState } from 'react';
import * as Linking from 'expo-linking';

export default function DetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [refreshing, setRefreshing] = useState(false);
  const [isOpeningContent, setIsOpeningContent] = useState(false);

  const { data: document, isLoading, error, refetch } = useQuery({
    queryKey: ['document', id],
    queryFn: () => getDocument(id),
    refetchInterval: (query) => {
      return query.state.data?.status === 1 || query.state.data?.status === 0 ? 3000 : false;
    }
  });

  const onRefresh = async () => {
    setRefreshing(true);
    await refetch();
    setRefreshing(false);
  };

  const handleViewFullContent = async () => {
    setIsOpeningContent(true);
    try {
        const url = await getDocumentContentUrl(id);
        if (url) {
            await Linking.openURL(url);
        } else {
            Alert.alert('Error', 'Full content URL could not be generated.');
        }
    } catch (err) {
        Alert.alert('Error', 'Failed to retrieve full content.');
        console.error(err);
    } finally {
        setIsOpeningContent(false);
    }
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#007bff" />
        <Text style={styles.loadingText}>Fetching AI Insights...</Text>
      </View>
    );
  }

  if (error || !document) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>Could not find document details.</Text>
      </View>
    );
  }

  return (
    <ScrollView 
      style={styles.container}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
    >
      <View style={styles.header}>
        <Text style={styles.fileName}>{document.fileName}</Text>
        <View style={styles.statusRow}>
          <Text style={styles.statusLabel}>Status:</Text>
          <Text style={[
            styles.statusValue, 
            document.status === 2 ? styles.statusSuccess : styles.statusPending
          ]}>
            {document.status === 2 ? 'Processed' : document.status === 1 ? 'Processing...' : 'Uploaded'}
          </Text>
        </View>
      </View>

      {document.summary && (
        <View style={styles.summarySection}>
          <Text style={styles.sectionTitle}>Smart Summary</Text>
          <Text style={styles.summaryText}>{document.summary}</Text>
        </View>
      )}

      {document.status === 2 ? (
        <>
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>AI Metadata</Text>
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Type:</Text>
              <Text style={styles.infoValue}>{document.documentType || 'N/A'}</Text>
            </View>
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>AI Confidence:</Text>
              <Text style={styles.infoValue}>
                {document.confidenceScore ? `${(document.confidenceScore * 100).toFixed(0)}%` : 'N/A'}
              </Text>
            </View>
            {document.extractedTextUri && (
                <View style={styles.contentRow}>
                    <View style={{ flex: 1 }}>
                        <Text style={styles.infoLabel}>Full Content:</Text>
                        <Text style={[styles.infoValue, { color: '#6c757d' }]}>Stored in cloud</Text>
                    </View>
                    <TouchableOpacity 
                        style={styles.viewButton} 
                        onPress={handleViewFullContent}
                        disabled={isOpeningContent}
                    >
                        {isOpeningContent ? (
                            <ActivityIndicator size="small" color="#fff" />
                        ) : (
                            <Text style={styles.viewButtonText}>View Full Text</Text>
                        )}
                    </TouchableOpacity>
                </View>
            )}
          </View>

          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Extracted Fields</Text>
            {document.extractedData && Object.keys(document.extractedData).length > 0 ? (
              Object.entries(document.extractedData).map(([key, value]) => (
                <View key={key} style={styles.dataRow}>
                  <Text style={styles.dataKey}>{key}:</Text>
                  <Text style={styles.dataValue}>{String(value)}</Text>
                </View>
              ))
            ) : (
              <Text style={styles.emptyText}>No specific fields were extracted.</Text>
            )}
          </View>
        </>
      ) : (
        <View style={styles.processingCard}>
          <ActivityIndicator size="small" color="#007bff" style={{ marginBottom: 12 }} />
          <Text style={styles.processingTitle}>Analyzing Document</Text>
          <Text style={styles.processingSub}>Azure AI is extracting structured data and generating insights. This page will update automatically.</Text>
        </View>
      )}
    </ScrollView>
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
    padding: 20,
  },
  header: {
    padding: 24,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  fileName: {
    fontSize: 22,
    fontWeight: '700',
    color: '#1a1a1a',
    marginBottom: 8,
  },
  statusRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusLabel: {
    fontSize: 14,
    color: '#6c757d',
    marginRight: 8,
  },
  statusValue: {
    fontSize: 14,
    fontWeight: '600',
  },
  statusSuccess: {
    color: '#28a745',
  },
  statusPending: {
    color: '#ffc107',
  },
  summarySection: {
    margin: 16,
    padding: 20,
    backgroundColor: '#e7f3ff',
    borderRadius: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#007bff',
  },
  summaryText: {
    fontSize: 15,
    lineHeight: 22,
    color: '#333',
  },
  section: {
    marginTop: 16,
    padding: 24,
    backgroundColor: '#fff',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 16,
    color: '#333',
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  contentRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  infoLabel: {
    color: '#6c757d',
  },
  infoValue: {
    fontWeight: '600',
    color: '#1a1a1a',
  },
  viewButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 8,
    minWidth: 100,
    alignItems: 'center',
  },
  viewButtonText: {
    color: '#fff',
    fontSize: 13,
    fontWeight: '600',
  },
  dataRow: {
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f1f3f5',
  },
  dataKey: {
    fontSize: 12,
    color: '#6c757d',
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 4,
  },
  dataValue: {
    fontSize: 16,
    color: '#1a1a1a',
    fontWeight: '500',
  },
  loadingText: {
    marginTop: 12,
    color: '#6c757d',
  },
  errorText: {
    color: '#dc3545',
    fontSize: 16,
  },
  processingCard: {
    margin: 24,
    padding: 30,
    backgroundColor: '#fff',
    borderRadius: 16,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#e9ecef',
  },
  processingTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#333',
    marginBottom: 8,
  },
  processingSub: {
    fontSize: 14,
    color: '#6c757d',
    textAlign: 'center',
    lineHeight: 20,
  },
  emptyText: {
    color: '#6c757d',
    fontStyle: 'italic',
  }
});
