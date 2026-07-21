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
  token: string;
  expiresAt: string;
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