export type UserRole = 'Customer' | 'Operator';

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshExpiresAt: string;
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
}