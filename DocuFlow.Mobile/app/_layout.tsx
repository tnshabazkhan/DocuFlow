import { Stack } from 'expo-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Colors } from '../constants/Colors';
import { View, StyleSheet, Text, Animated } from 'react-native';
import { useState, useEffect, useRef } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { realtimeService } from '../services/realtimeService';

const queryClient = new QueryClient();

function StartupAnimation({ onFinish }: { onFinish: () => void }) {
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const scaleAnim = useRef(new Animated.Value(0.5)).current;
  const textAnim = useRef(new Animated.Value(20)).current;
  const textFadeAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    // Start SignalR background connection
    realtimeService.start();

    Animated.parallel([
      Animated.spring(scaleAnim, {
        toValue: 1,
        useNativeDriver: true,
        tension: 10,
        friction: 4,
      }),
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 800,
        useNativeDriver: true,
      }),
    ]).start();

    Animated.parallel([
      Animated.timing(textAnim, {
        toValue: 0,
        duration: 800,
        delay: 500,
        useNativeDriver: true,
      }),
      Animated.timing(textFadeAnim, {
        toValue: 1,
        duration: 800,
        delay: 500,
        useNativeDriver: true,
      }),
    ]).start();

    const timer = setTimeout(onFinish, 2500);
    return () => clearTimeout(timer);
  }, []);

  return (
    <View style={styles.splashContainer}>
      <Animated.View style={[styles.logoContainer, { opacity: fadeAnim, transform: [{ scale: scaleAnim }] }]}>
        <View style={styles.iconCircle}>
          <Ionicons name="documents" size={60} color="#fff" />
        </View>
        <Animated.View style={{ opacity: textFadeAnim, transform: [{ translateY: textAnim }] }}>
          <Text style={styles.splashTitle}>DocuFlow</Text>
          <Text style={styles.splashSub}>Intelligent Document Processing</Text>
        </Animated.View>
      </Animated.View>
    </View>
  );
}

export default function RootLayout() {
  const [isAppReady, setIsAppReady] = useState(false);
  const appFadeAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (isAppReady) {
      Animated.timing(appFadeAnim, {
        toValue: 1,
        duration: 600,
        useNativeDriver: true,
      }).start();
    }
  }, [isAppReady]);

  return (
    <QueryClientProvider client={queryClient}>
      {!isAppReady ? (
        <StartupAnimation onFinish={() => setIsAppReady(true)} />
      ) : (
        <Animated.View style={{ flex: 1, opacity: appFadeAnim }}>
          <Stack
            screenOptions={{
              headerStyle: {
                backgroundColor: Colors.surface,
              },
              headerShadowVisible: false,
              headerTitleStyle: {
                fontWeight: '700',
                color: Colors.text,
                fontSize: 18,
              },
              headerTintColor: Colors.primary,
            }}
          >
            <Stack.Screen 
              name="index" 
              options={{ 
                title: 'DocuFlow',
              }} 
            />
            <Stack.Screen 
              name="upload" 
              options={{ 
                title: 'Analyze Document',
                headerBackTitle: 'Back',
              }} 
            />
            <Stack.Screen 
              name="details/[id]" 
              options={{ 
                title: 'Document Insights',
                headerBackTitle: 'Back',
              }} 
            />
          </Stack>
        </Animated.View>
      )}
    </QueryClientProvider>
  );
}

const styles = StyleSheet.create({
  splashContainer: {
    flex: 1,
    backgroundColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  logoContainer: {
    alignItems: 'center',
  },
  iconCircle: {
    width: 120,
    height: 120,
    borderRadius: 60,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 24,
  },
  splashTitle: {
    fontSize: 42,
    fontWeight: '900',
    color: '#fff',
    letterSpacing: -1,
    textAlign: 'center',
  },
  splashSub: {
    fontSize: 16,
    color: 'rgba(255, 255, 255, 0.8)',
    marginTop: 8,
    fontWeight: '600',
    textAlign: 'center',
  },
});
