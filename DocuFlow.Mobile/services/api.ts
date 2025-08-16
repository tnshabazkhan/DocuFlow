import axios from 'axios';
import { Config } from '../constants/Config';

const API_BASE_URL = Config.API_URL;

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const uploadDocument = async (file: any, category: number) => {
  try {
    // Step 1: Initiate
    console.log(`[DocuFlow] Initiating upload for: ${file.name}`);
    const initiateResponse = await api.post('/documents', {
      fileName: file.name || `mobile_upload_${Date.now()}.jpg`,
      category: category
    });
    
    const { documentId, sasUri } = initiateResponse.data;
    console.log(`[DocuFlow] Received DocumentId: ${documentId}`);

    // Step 2: Direct Upload to Blob Storage
    console.log(`[DocuFlow] Uploading binary content to Azure Blob Storage...`);
    
    // Using fetch for the PUT request is often more reliable for raw binary data in React Native/Expo
    const blobResponse = await fetch(file.uri);
    const blob = await blobResponse.blob();
    
    console.log(`[DocuFlow] Blob size to upload: ${blob.size} bytes`);

    const uploadResponse = await fetch(sasUri, {
      method: 'PUT',
      body: blob,
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.mimeType || 'application/octet-stream',
      },
    });

    if (!uploadResponse.ok) {
        const errorText = await uploadResponse.text();
        console.error(`[DocuFlow] Azure Storage upload failed: ${uploadResponse.status} ${uploadResponse.statusText}`, errorText);
        throw new Error(`Azure Storage upload failed with status ${uploadResponse.status}`);
    }

    console.log(`[DocuFlow] Upload successful.`);

    // Step 3: Complete
    await api.post(`/documents/${documentId}/complete`);
    console.log(`[DocuFlow] Processing notification sent.`);

    return documentId;
  } catch (error) {
    console.error('[DocuFlow] Upload flow failed:', error);
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

export const getSummaryPdfUrl = async (id: string) => {
    const response = await api.get(`/documents/${id}/summary-url`);
    return response.data.url;
};

export default api;
