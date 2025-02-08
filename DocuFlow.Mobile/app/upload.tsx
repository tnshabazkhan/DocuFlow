import { useState } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, ActivityIndicator, Alert, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import { uploadDocument } from '../services/api';
import { Colors } from '../constants/Colors';
import { Ionicons } from '@expo/vector-icons';

const CATEGORIES = [
  { label: 'Smart Summary', value: 5, icon: 'sparkles-outline', desc: 'AI-generated executive summary' },
  { label: 'Invoice / Receipt', value: 1, icon: 'receipt-outline', desc: 'Extract amounts and dates' },
  { label: 'Identity Card', value: 3, icon: 'id-card-outline', desc: 'Verify personal information' },
  { label: 'Plain Text', value: 4, icon: 'text-outline', desc: 'Convert image to digital text' },
];

const UploadScreen = () => {
  const router = useRouter();
  const [selectedCategory, setSelectedCategory] = useState(5);
  const [file, setFile] = useState<any>(null);
  const [isUploading, setIsUploading] = useState(false);

  const [permissionResponse, requestPermission] = ImagePicker.useCameraPermissions();

  const pickDocument = async () => {
    try {
        const result = await DocumentPicker.getDocumentAsync({
          type: ['application/pdf', 'image/*'],
        });

        if (!result.canceled) {
          setFile(result.assets[0]);
        }
    } catch (err) {
        console.error("Document picking error:", err);
    }
  };

  const takePhoto = async () => {
    try {
        if (!permissionResponse?.granted) {
            const permission = await requestPermission();
            if (!permission.granted) {
                Alert.alert('Permission needed', 'We need camera access to take photos.');
                return;
            }
        }

        const result = await ImagePicker.launchCameraAsync({
          quality: 0.8,
        });

        if (!result.canceled) {
          setFile(result.assets[0]);
        }
    } catch (err) {
        console.error("Camera error:", err);
    }
  };

  const handleUpload = async () => {
    if (!file) {
      Alert.alert('Error', 'Please select a file first.');
      return;
    }

    setIsUploading(true);
    try {
      const id = await uploadDocument(file, selectedCategory);
      setIsUploading(false);
      Alert.alert('Analysis Started', 'Your document is being processed by Azure AI.', [
        { text: 'View Live Results', onPress: () => router.replace(`/details/${id}`) },
        { text: 'Done', onPress: () => router.back() }
      ]);
    } catch (error) {
      setIsUploading(false);
      Alert.alert('Upload Failed', 'Please check your connection and try again.');
    }
  };

  return (
    <View style={styles.container}>
      <ScrollView contentContainerStyle={styles.scrollContent}>
        <Text style={styles.sectionTitle}>1. Analysis Mode</Text>
        <View style={styles.categoryGrid}>
            {CATEGORIES.map((cat) => (
            <TouchableOpacity
                key={cat.value}
                style={[
                    styles.categoryCard, 
                    selectedCategory === cat.value && styles.categoryCardSelected
                ]}
                onPress={() => setSelectedCategory(cat.value)}
                activeOpacity={0.7}
            >
                <View style={[
                    styles.categoryIcon,
                    selectedCategory === cat.value && styles.categoryIconSelected
                ]}>
                    <Ionicons 
                        name={cat.icon as any} 
                        size={24} 
                        color={selectedCategory === cat.value ? '#fff' : Colors.primary} 
                    />
                </View>
                <Text style={[
                    styles.categoryLabel,
                    selectedCategory === cat.value && styles.categoryLabelSelected
                ]}>{cat.label}</Text>
                <Text style={styles.categoryDesc}>{cat.desc}</Text>
            </TouchableOpacity>
            ))}
        </View>

        <Text style={styles.sectionTitle}>2. Source</Text>
        <View style={styles.sourceRow}>
            <TouchableOpacity style={styles.sourceButton} onPress={pickDocument}>
                <Ionicons name="document-attach-outline" size={24} color={Colors.primary} />
                <Text style={styles.sourceButtonText}>Files</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.sourceButton} onPress={takePhoto}>
                <Ionicons name="camera-outline" size={24} color={Colors.primary} />
                <Text style={styles.sourceButtonText}>Camera</Text>
            </TouchableOpacity>
        </View>

        {file && (
            <View style={styles.filePreview}>
                <Ionicons name="checkmark-circle" size={20} color={Colors.success} style={{ marginRight: 8 }} />
                <Text style={styles.fileName} numberOfLines={1}>
                    Ready: {file.name || 'Captured Photo'}
                </Text>
                <TouchableOpacity onPress={() => setFile(null)}>
                    <Ionicons name="close-circle" size={20} color={Colors.textLight} />
                </TouchableOpacity>
            </View>
        )}
      </ScrollView>

      <View style={styles.footer}>
        <TouchableOpacity 
            style={[styles.uploadButton, !file && styles.uploadButtonDisabled]} 
            onPress={handleUpload}
            disabled={isUploading || !file}
        >
            {isUploading ? (
            <ActivityIndicator color="#fff" />
            ) : (
            <>
                <Text style={styles.uploadButtonText}>Run Intelligent Analysis</Text>
                <Ionicons name="arrow-forward" size={20} color="#fff" style={{ marginLeft: 8 }} />
            </>
            )}
        </TouchableOpacity>
      </View>
    </View>
  );
};

export default UploadScreen;

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  scrollContent: {
    padding: 20,
    paddingBottom: 100,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '800',
    marginBottom: 16,
    marginTop: 10,
    color: Colors.text,
  },
  categoryGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginBottom: 24,
  },
  categoryCard: {
    width: '48%',
    backgroundColor: Colors.surface,
    padding: 16,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  categoryCardSelected: {
    borderColor: Colors.primary,
    backgroundColor: Colors.accent,
  },
  categoryIcon: {
    width: 40,
    height: 40,
    borderRadius: 10,
    backgroundColor: Colors.accent,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 12,
  },
  categoryIconSelected: {
    backgroundColor: Colors.primary,
  },
  categoryLabel: {
    fontSize: 14,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 4,
  },
  categoryLabelSelected: {
    color: Colors.primary,
  },
  categoryDesc: {
    fontSize: 11,
    color: Colors.textLight,
    lineHeight: 14,
  },
  sourceRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 24,
  },
  sourceButton: {
    flex: 1,
    backgroundColor: Colors.surface,
    padding: 20,
    borderRadius: 16,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    borderStyle: 'dashed',
  },
  sourceButtonText: {
    color: Colors.text,
    fontWeight: '600',
    marginTop: 8,
  },
  filePreview: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    backgroundColor: Colors.surface,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Colors.success,
  },
  fileName: {
    flex: 1,
    color: Colors.text,
    fontSize: 14,
    fontWeight: '500',
  },
  footer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    padding: 20,
    paddingBottom: 40,
    backgroundColor: Colors.surface,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
  },
  uploadButton: {
    backgroundColor: Colors.primary,
    padding: 18,
    borderRadius: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: Colors.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 4,
  },
  uploadButtonDisabled: {
    backgroundColor: Colors.border,
    shadowOpacity: 0,
    elevation: 0,
  },
  uploadButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '700',
  },
});
