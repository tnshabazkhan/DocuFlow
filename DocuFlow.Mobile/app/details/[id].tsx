import { StyleSheet, Text, View, ScrollView, ActivityIndicator, RefreshControl, TouchableOpacity, Alert, Animated } from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { getDocument, getDocumentContentUrl, getSummaryPdfUrl } from '../../services/api';
import { realtimeService } from '../../services/realtimeService';
import { useState, useEffect, useRef } from 'react';
import * as Linking from 'expo-linking';
import { Colors } from '../../constants/Colors';
import { Ionicons } from '@expo/vector-icons';

function StaggeredSection({ children, delay = 0 }: { children: React.ReactNode, delay?: number }) {
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const slideAnim = useRef(new Animated.Value(20)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 600,
        delay,
        useNativeDriver: true,
      }),
      Animated.timing(slideAnim, {
        toValue: 0,
        duration: 600,
        delay,
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

export default function DetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const [refreshing, setRefreshing] = useState(false);
  const [isOpeningContent, setIsOpeningContent] = useState(false);
  const [isOpeningPdf, setIsOpeningPdf] = useState(false);
  const [liveStatus, setLiveStatus] = useState<string | null>(null);

  const headerScaleAnim = useRef(new Animated.Value(0.9)).current;
  const headerFadeAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(headerFadeAnim, {
        toValue: 1,
        duration: 600,
        useNativeDriver: true,
      }),
      Animated.spring(headerScaleAnim, {
        toValue: 1,
        useNativeDriver: true,
        tension: 20,
        friction: 7,
      })
    ]).start();

    // Listen for SignalR updates
    const unsubscribe = realtimeService.onUpdate((update) => {
        if (update.documentId === id) {
            setLiveStatus(update.status);
            // If the status is a final state, refresh the whole document data
            if (update.status === 'Processed' || update.status === 'Failed') {
                refetch();
            }
        }
    });

    return () => unsubscribe();
  }, [id]);

  const { data: document, isLoading, error, refetch } = useQuery({
    queryKey: ['document', id],
    queryFn: () => getDocument(id),
    refetchInterval: (query) => {
      // Keep polling as a backup, but slower if we have SignalR
      return query.state.data?.status === 1 || query.state.data?.status === 0 ? 5000 : false;
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

  const handleDownloadPdf = async () => {
    setIsOpeningPdf(true);
    try {
        const url = await getSummaryPdfUrl(id);
        if (url) {
            await Linking.openURL(url);
        } else {
            Alert.alert('Error', 'PDF Report URL could not be generated.');
        }
    } catch (err) {
        Alert.alert('Error', 'Failed to retrieve PDF report.');
        console.error(err);
    } finally {
        setIsOpeningPdf(false);
    }
  };

  if (isLoading && !document) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={Colors.primary} />
        <Text style={styles.loadingText}>Fetching AI Insights...</Text>
      </View>
    );
  }

  if (error || !document) {
    return (
      <View style={styles.center}>
        <Ionicons name="alert-circle-outline" size={48} color={Colors.error} />
        <Text style={styles.errorText}>Could not find document details.</Text>
      </View>
    );
  }

  const isProcessed = document.status === 2;

  const getStepStatus = (stepName: string) => {
    if (isProcessed) return 'completed';
    if (!liveStatus) return 'pending';

    const status = liveStatus.toLowerCase();
    
    const steps = {
        ocr: status.includes('extracting') || status.includes('analysis') || status.includes('starting'),
        summary: status.includes('summarizing'),
        report: status.includes('generating pdf')
    };

    if (stepName === 'ocr') {
        if (steps.summary || steps.report) return 'completed';
        return steps.ocr ? 'active' : 'pending';
    }
    if (stepName === 'summary') {
        if (steps.report) return 'completed';
        return steps.summary ? 'active' : 'pending';
    }
    if (stepName === 'report') {
        return steps.report ? 'active' : 'pending';
    }
    return 'pending';
  };

  const StepItem = ({ label, status, subtext }: { label: string, status: 'completed' | 'active' | 'pending', subtext?: string }) => {
    const pulseAnim = useRef(new Animated.Value(1)).current;

    useEffect(() => {
        if (status === 'active') {
            const animation = Animated.loop(
                Animated.sequence([
                    Animated.timing(pulseAnim, { toValue: 1.2, duration: 800, useNativeDriver: true }),
                    Animated.timing(pulseAnim, { toValue: 1, duration: 800, useNativeDriver: true }),
                ])
            );
            animation.start();
            return () => animation.stop();
        } else {
            pulseAnim.setValue(1);
        }
    }, [status]);

    return (
        <View style={styles.stepItem}>
            <View style={styles.stepLeft}>
                <Animated.View style={[
                    styles.stepIconContainer,
                    status === 'active' && { transform: [{ scale: pulseAnim }], backgroundColor: Colors.accent },
                    status === 'completed' && { backgroundColor: Colors.success }
                ]}>
                    {status === 'completed' ? (
                        <Ionicons name="checkmark" size={14} color="#fff" />
                    ) : (
                        <View style={[styles.stepDot, status === 'active' && { backgroundColor: Colors.primary }]} />
                    )}
                </Animated.View>
                <View style={[styles.stepLine, status === 'completed' && { backgroundColor: Colors.success }]} />
            </View>
            <View style={styles.stepRight}>
                <Text style={[
                    styles.stepLabel, 
                    status === 'active' && { color: Colors.primary, fontWeight: '800' },
                    status === 'pending' && { color: Colors.textLight }
                ]}>{label}</Text>
                {status === 'active' && subtext && (
                    <Text style={styles.stepSubtext}>{subtext}</Text>
                )}
            </View>
        </View>
    );
  };

  return (
    <ScrollView 
      style={styles.container}
      contentContainerStyle={styles.scrollContent}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={Colors.primary} />}
    >
      <Animated.View style={{ opacity: headerFadeAnim, transform: [{ scale: headerScaleAnim }] }}>
        <View style={styles.headerCard}>
            <View style={styles.headerMain}>
                <View style={styles.headerIcon}>
                    <Ionicons name="document-text" size={30} color={Colors.primary} />
                </View>
                <View style={{ flex: 1 }}>
                    <Text style={styles.fileName}>{document.fileName}</Text>
                    <View style={styles.statusBadgeRow}>
                        <View style={[
                            styles.badge, 
                            isProcessed ? styles.badgeSuccess : styles.badgeWarning
                        ]}>
                            <Text style={[
                                styles.badgeText, 
                                { color: isProcessed ? Colors.success : Colors.warning }
                            ]}>
                                {liveStatus || (isProcessed ? 'Processed' : 'Analyzing')}
                            </Text>
                        </View>
                        <Text style={styles.dateText}>
                            {new Date(document.uploadDate).toLocaleDateString()}
                        </Text>
                    </View>
                </View>
            </View>
        </View>
      </Animated.View>

      {!isProcessed ? (
        <StaggeredSection delay={300}>
            <View style={styles.timelineCard}>
                <Text style={styles.timelineTitle}>AI Processing Sequence</Text>
                
                <StepItem 
                    label="Extracting Knowledge" 
                    status={getStepStatus('ocr')} 
                    subtext="Reading text and identifying structures..."
                />
                <StepItem 
                    label="Intelligent Summarization" 
                    status={getStepStatus('summary')} 
                    subtext={liveStatus?.includes('Summarizing') ? liveStatus : "Distilling key insights..."}
                />
                <StepItem 
                    label="Finalizing PDF Insights" 
                    status={getStepStatus('report')} 
                    subtext="Generating your downloadable report..."
                />

                <View style={styles.timelineFooter}>
                    <ActivityIndicator size="small" color={Colors.primary} style={{ marginRight: 10 }} />
                    <Text style={styles.footerText}>Securely processing on Azure AI...</Text>
                </View>
            </View>
        </StaggeredSection>
      ) : (
        <>
          {document.summary && (
            <StaggeredSection delay={200}>
                <View style={styles.summarySection}>
                <View style={styles.sectionHeader}>
                    <View style={styles.titleWithIcon}>
                        <Ionicons name="sparkles" size={18} color={Colors.primary} style={{ marginRight: 8 }} />
                        <Text style={styles.sectionTitle}>Smart Summary</Text>
                    </View>
                    {document.summaryPdfUri && (
                        <TouchableOpacity 
                            style={styles.pdfAction} 
                            onPress={handleDownloadPdf}
                            disabled={isOpeningPdf}
                        >
                            {isOpeningPdf ? (
                                <ActivityIndicator size="small" color={Colors.primary} />
                            ) : (
                                <Ionicons name="download-outline" size={20} color={Colors.primary} />
                            )}
                        </TouchableOpacity>
                    )}
                </View>
                <Text style={styles.summaryText}>{document.summary}</Text>
                </View>
            </StaggeredSection>
          )}

          <StaggeredSection delay={400}>
            <View style={styles.detailsSection}>
                <Text style={styles.sectionTitle}>AI Metadata</Text>
                <View style={styles.metaGrid}>
                    <View style={styles.metaItem}>
                        <Text style={styles.metaLabel}>Confidence</Text>
                        <Text style={styles.metaValue}>
                            {document.confidenceScore ? `${(document.confidenceScore * 100).toFixed(0)}%` : 'N/A'}
                        </Text>
                    </View>
                    <View style={styles.metaItem}>
                        <Text style={styles.metaLabel}>Document Type</Text>
                        <Text style={styles.metaValue}>{document.documentType || 'General'}</Text>
                    </View>
                </View>

                {document.extractedTextUri && (
                    <TouchableOpacity 
                        style={styles.fullTextButton} 
                        onPress={handleViewFullContent}
                        disabled={isOpeningContent}
                    >
                        <Ionicons name="eye-outline" size={20} color={Colors.primary} style={{ marginRight: 8 }} />
                        <Text style={styles.fullTextButtonText}>
                            {isOpeningContent ? 'Loading...' : 'View Extracted Full Text'}
                        </Text>
                    </TouchableOpacity>
                )}
            </View>
          </StaggeredSection>

          <StaggeredSection delay={600}>
            <View style={styles.dataSection}>
                <Text style={styles.sectionTitle}>Extracted Fields</Text>
                {document.extractedData && Object.keys(document.extractedData).length > 0 ? (
                <View style={styles.dataList}>
                    {Object.entries(document.extractedData).map(([key, value]) => (
                        <View key={key} style={styles.dataRow}>
                            <Text style={styles.dataKey}>{key.replace(/([A-Z])/g, ' $1').trim()}</Text>
                            <Text style={styles.dataValue}>{String(value)}</Text>
                        </View>
                    ))}
                </View>
                ) : (
                <View style={styles.emptyData}>
                    <Text style={styles.emptyText}>No specific structured fields identified.</Text>
                </View>
                )}
            </View>
          </StaggeredSection>
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  scrollContent: {
    paddingBottom: 40,
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
    backgroundColor: Colors.background,
  },
  headerCard: {
    backgroundColor: Colors.surface,
    padding: 24,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  headerMain: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  headerIcon: {
    width: 60,
    height: 60,
    borderRadius: 16,
    backgroundColor: Colors.accent,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 20,
  },
  fileName: {
    fontSize: 20,
    fontWeight: '800',
    color: Colors.text,
    marginBottom: 6,
  },
  statusBadgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    borderWidth: 1,
  },
  badgeSuccess: {
    backgroundColor: '#ECFDF5',
    borderColor: '#A7F3D0',
  },
  badgeWarning: {
    backgroundColor: '#FFFBEB',
    borderColor: '#FDE68A',
  },
  badgeText: {
    fontSize: 12,
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  dateText: {
    fontSize: 13,
    color: Colors.textLight,
  },
  summarySection: {
    margin: 16,
    padding: 20,
    backgroundColor: Colors.surface,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: Colors.border,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.02,
    shadowRadius: 10,
    elevation: 2,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  titleWithIcon: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: Colors.text,
  },
  pdfAction: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: Colors.accent,
    justifyContent: 'center',
    alignItems: 'center',
  },
  summaryText: {
    fontSize: 15,
    lineHeight: 24,
    color: Colors.text,
  },
  detailsSection: {
    margin: 16,
    marginTop: 0,
    padding: 20,
    backgroundColor: Colors.surface,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  metaGrid: {
    flexDirection: 'row',
    marginTop: 16,
    gap: 16,
  },
  metaItem: {
    flex: 1,
    padding: 12,
    backgroundColor: Colors.background,
    borderRadius: 12,
  },
  metaLabel: {
    fontSize: 11,
    color: Colors.textLight,
    textTransform: 'uppercase',
    fontWeight: '600',
    marginBottom: 4,
  },
  metaValue: {
    fontSize: 15,
    fontWeight: '700',
    color: Colors.text,
  },
  fullTextButton: {
    marginTop: 20,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 14,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Colors.primary,
  },
  fullTextButtonText: {
    color: Colors.primary,
    fontWeight: '700',
    fontSize: 14,
  },
  dataSection: {
    margin: 16,
    marginTop: 0,
  },
  dataList: {
    marginTop: 12,
    backgroundColor: Colors.surface,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: Colors.border,
    overflow: 'hidden',
  },
  dataRow: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  dataKey: {
    fontSize: 11,
    color: Colors.textLight,
    textTransform: 'uppercase',
    fontWeight: '700',
    marginBottom: 4,
  },
  dataValue: {
    fontSize: 15,
    color: Colors.text,
    fontWeight: '500',
  },
  loadingText: {
    marginTop: 16,
    color: Colors.textLight,
    fontWeight: '600',
  },
  errorText: {
    marginTop: 12,
    color: Colors.error,
    fontSize: 16,
    fontWeight: '600',
  },
  emptyData: {
    marginTop: 12,
    padding: 24,
    backgroundColor: Colors.surface,
    borderRadius: 20,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    borderStyle: 'dashed',
  },
  emptyText: {
    color: Colors.textLight,
    fontSize: 14,
  },
  timelineCard: {
    margin: 16,
    padding: 24,
    backgroundColor: Colors.surface,
    borderRadius: 24,
    borderWidth: 1,
    borderColor: Colors.border,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.05,
    shadowRadius: 10,
    elevation: 2,
  },
  timelineTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: Colors.text,
    marginBottom: 24,
  },
  stepItem: {
    flexDirection: 'row',
    minHeight: 60,
  },
  stepLeft: {
    width: 30,
    alignItems: 'center',
  },
  stepIconContainer: {
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: Colors.border,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1,
  },
  stepDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: Colors.textLight,
  },
  stepLine: {
    width: 2,
    flex: 1,
    backgroundColor: Colors.border,
    marginVertical: 4,
  },
  stepRight: {
    flex: 1,
    paddingLeft: 16,
    paddingBottom: 20,
  },
  stepLabel: {
    fontSize: 15,
    fontWeight: '600',
    color: Colors.text,
  },
  stepSubtext: {
    fontSize: 13,
    color: Colors.textLight,
    marginTop: 4,
    lineHeight: 18,
  },
  timelineFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 8,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
  },
  footerText: {
    fontSize: 12,
    color: Colors.textLight,
    fontStyle: 'italic',
  }
});
