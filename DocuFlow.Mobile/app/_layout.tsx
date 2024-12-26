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
            headerLargeTitle: true,
          }} 
        />
        <Stack.Screen 
          name="upload" 
          options={{ 
            title: 'Upload Document',
            presentation: 'modal',
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
