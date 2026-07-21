import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from './auth.models';

const TOKEN_KEY = 'reklio.token';
const USER_KEY = 'reklio.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly _currentUser = signal<CurrentUser | null>(this.restoreUser());
  private readonly _token = signal<string | null>(this.restoreToken());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly role = computed(() => this._currentUser()?.role ?? null);

  register(request: RegisterRequest): Observable<AuthResponse> {
    // Registracija ne loguje automatski — korisnik ide na success ekran pa na login.
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._currentUser.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private setSession(response: AuthResponse): void {
    const user: CurrentUser = {
      id: response.id,
      fullName: response.fullName,
      email: response.email,
      role: response.role,
    };
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._token.set(response.token);
    this._currentUser.set(user);
  }

  private restoreToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private restoreUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  }
}