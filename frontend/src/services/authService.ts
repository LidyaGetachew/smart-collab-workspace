import api from './api';

export interface User {
  id: string;
  token: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  avatarUrl?: string;
}

export interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginData {
  email: string;
  password: string;
}

export const authService = {
  async register(data: RegisterData): Promise<User> {
    const response = await api.post('/auth/register', data);
    return response.data;
  },

  async login(data: LoginData): Promise<User> {
    const response = await api.post('/auth/login', data);
    return response.data;
  },

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  },

  setUser(user: User): void {
    localStorage.setItem('token', user.token);
    localStorage.setItem('user', JSON.stringify(user));
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await api.post('/auth/change-password', { currentPassword, newPassword, confirmNewPassword: newPassword });
  },

  async getProfile(): Promise<User> {
    const response = await api.get('/auth/profile');
    return response.data;
  }
};