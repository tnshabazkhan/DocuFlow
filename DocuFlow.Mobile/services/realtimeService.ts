import * as signalR from '@microsoft/signalr';

const BASE_URL = 'http://172.20.10.12:5009'; // Matching your api.ts

class RealtimeService {
  private connection: signalR.HubConnection | null = null;
  private listeners: ((update: { documentId: string; status: string }) => void)[] = null;

  constructor() {
    this.listeners = [];
  }

  async start() {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE_URL}/api/realtime`)
      .withAutomaticReconnect([0, 2000, 10000, 30000]) // Specific retry delays
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Stable intervals for Mobile (4G/5G/Wi-Fi)
    this.connection.serverTimeoutInMilliseconds = 60000; // 60s
    this.connection.keepAliveIntervalInMilliseconds = 15000; // 15s

    this.connection.on('progressUpdate', (update) => {
      console.log(`[SignalR] Progress: ${update.status} for ${update.documentId}`);
      this.listeners.forEach(listener => listener(update));
    });

    try {
      await this.connection.start();
      console.log('[SignalR] Connected successfully');
    } catch (err) {
      console.error('[SignalR] Connection failed: ', err);
      setTimeout(() => this.start(), 5000);
    }
  }

  onUpdate(callback: (update: { documentId: string; status: string }) => void) {
    this.listeners.push(callback);
    return () => {
      this.listeners = this.listeners.filter(l => l !== callback);
    };
  }

  async stop() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const realtimeService = new RealtimeService();
export default realtimeService;
