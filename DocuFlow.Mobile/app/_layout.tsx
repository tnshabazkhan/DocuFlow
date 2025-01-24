import { Stack } from 'expo-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const queryClient = new QueryClient();

export default function RootLayout() {
  return (
    <QueryClientProvider client={queryClient}>
      <Stack>
        <Stack.Screen 
          name="index" 
          options={{ 
            title: 'DocuFlow',
          }} 
        />
        <Stack.Screen 
          name="upload" 
          options={{ 
            title: 'Upload Document',
          }} 
        />
        <Stack.Screen 
          name="details/[id]" 
          options={{ 
            title: 'Document Details',
          }} 
        />
      </Stack>
    </QueryClientProvider>
  );
}
