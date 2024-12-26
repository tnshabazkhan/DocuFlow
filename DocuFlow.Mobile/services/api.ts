import axios from 'axios';
import { Platform } from 'react-native';

const API_BASE_URL = Platform.OS === 'android' ? 'http://10.0.2.2:5009/api' : 'http://localhost:5009/api';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const uploadDocument = async (file: any, category: number) => {
  try {
    const initiateResponse = await api.post('/documents', {
      fileName: file.name || `mobile_upload_${Date.now()}.jpg`,
      category: category
    });
    
    const { documentId, sasUri } = initiateResponse.data;

    const fileContent = await fetch(file.uri).then(res => res.blob());
    
    await axios.put(sasUri, fileContent, {
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.mimeType || 'application/octet-stream',
      }
    });

    await api.post(`/documents/${documentId}/complete`);

    return documentId;
  } catch (error) {
    console.error('Upload flow failed:', error);
    throw error;
  }
};

export const getDocuments = async () => {
  const response = await api.get('/documents');
  return response.data;
};

export const getDocument = async (id: string) => {
  const response = await api.get(`/documents/${id}`);
  return response.data;
};

export const getDocumentContentUrl = async (id: string) => {
    const response = await api.get(`/documents/${id}/content-url`);
    return response.data.url;
};

export default api;
