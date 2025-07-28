import * as SecureStore from 'expo-secure-store';
import axios from 'axios';
import Config from '../constants/Config';

export interface AuthResponse {
  token: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
  };
}

class AuthService {
  async register(email: string, password: string, firstName: string, lastName: string): Promise<AuthResponse> {
    const response = await axios.post(`${Config.BASE_URL}/api/identity/register`, {
      email,
      password,
      firstName,
      lastName,
    });
    
    const authData = response.data;
    await this.saveAuthData(authData);
    return authData;
  }

  async login(email: string, password: string): Promise<AuthResponse> {
    const response = await axios.post(`${Config.BASE_URL}/api/identity/login`, {
      email,
      password,
    });
    
    const authData = response.data;
    await this.saveAuthData(authData);
    return authData;
  }

  private async saveAuthData(data: AuthResponse) {
    await SecureStore.setItemAsync('user_token', data.token);
    await SecureStore.setItemAsync('user_data', JSON.stringify(data));
  }

  async getStoredToken(): Promise<string | null> {
    return await SecureStore.getItemAsync('user_token');
  }

  async getStoredUser(): Promise<AuthResponse | null> {
    const userData = await SecureStore.getItemAsync('user_data');
    return userData ? JSON.parse(userData) : null;
  }

  async logout() {
    await SecureStore.deleteItemAsync('user_token');
    await SecureStore.deleteItemAsync('user_data');
  }
}

export const authService = new AuthService();
export default authService;
