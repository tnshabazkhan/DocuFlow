import { useState } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, ActivityIndicator, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import { uploadDocument } from '../services/api';

const CATEGORIES = [
  { label: 'General / Layout', value: 0 },
  { label: 'Invoice', value: 1 },
  { label: 'Receipt', value: 2 },
  { label: 'Identity Card', value: 3 },
  { label: 'Text Extraction (OCR)', value: 4 },
  { label: 'Smart Summary', value: 5 },
];

export default function UploadScreen() {
  const router = useRouter();
  const [selectedCategory, setSelectedCategory] = useState(0);
  const [file, setFile] = useState<any>(null);
  const [isUploading, setIsUploading] = useState(false);

  const pickDocument = async () => {
    const result = await DocumentPicker.getDocumentAsync({
      type: ['application/pdf', 'image/*'],
    });

    if (!result.canceled) {
      setFile(result.assets[0]);
    }
  };

  const takePhoto = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permission needed', 'We need camera access to take photos of documents.');
      return;
    }

    const result = await ImagePicker.launchCameraAsync({
      quality: 0.8,
    });

    if (!result.canceled) {
      setFile(result.assets[0]);
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
      Alert.alert('Success', 'Document uploaded and processing started!', [
        { text: 'View Results', onPress: () => router.replace(`/details/${id}`) },
        { text: 'Close', onPress: () => router.back() }
      ]);
    } catch (error) {
      setIsUploading(false);
      Alert.alert('Upload Failed', 'There was an error connecting to the DocuFlow API. Please check your network and try again.');
      console.error(error);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.label}>1. Select Category</Text>
      <View style={styles.categoryContainer}>
        {CATEGORIES.map((cat) => (
          <TouchableOpacity
            key={cat.value}
            style={[styles.categoryButton, selectedCategory === cat.value && styles.categorySelected]}
            onPress={() => setSelectedCategory(cat.value)}
          >
            <Text style={[styles.categoryText, selectedCategory === cat.value && styles.categoryTextSelected]}>
              {cat.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <Text style={styles.label}>2. Pick File</Text>
      <View style={styles.actionRow}>
        <TouchableOpacity style={styles.actionButton} onPress={pickDocument}>
          <Text style={styles.actionButtonText}>Choose File</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.actionButton} onPress={takePhoto}>
          <Text style={styles.actionButtonText}>Take Photo</Text>
        </TouchableOpacity>
      </View>

      {file && (
        <View style={styles.filePreview}>
          <Text style={styles.fileName}>Selected: {file.name || 'Photo'}</Text>
        </View>
      )}

      <TouchableOpacity 
        style={[styles.uploadButton, !file && styles.uploadButtonDisabled]} 
        onPress={handleUpload}
        disabled={isUploading || !file}
      >
        {isUploading ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={styles.uploadButtonText}>Start Intelligent Processing</Text>
        )}
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#fff',
  },
  label: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 16,
    marginTop: 20,
    color: '#333',
  },
  categoryContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  categoryButton: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#dee2e6',
    backgroundColor: '#f8f9fa',
  },
  categorySelected: {
    backgroundColor: '#007bff',
    borderColor: '#007bff',
  },
  categoryText: {
    fontSize: 14,
    color: '#495057',
  },
  categoryTextSelected: {
    color: '#fff',
    fontWeight: '600',
  },
  actionRow: {
    flexDirection: 'row',
    gap: 12,
  },
  actionButton: {
    flex: 1,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#007bff',
    alignItems: 'center',
  },
  actionButtonText: {
    color: '#007bff',
    fontWeight: '600',
  },
  filePreview: {
    marginTop: 24,
    padding: 12,
    backgroundColor: '#e9ecef',
    borderRadius: 8,
  },
  fileName: {
    color: '#495057',
    fontSize: 14,
  },
  uploadButton: {
    marginTop: 'auto',
    marginBottom: 40,
    backgroundColor: '#007bff',
    padding: 18,
    borderRadius: 14,
    alignItems: 'center',
  },
  uploadButtonDisabled: {
    backgroundColor: '#ccc',
  },
  uploadButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '700',
  },
});
