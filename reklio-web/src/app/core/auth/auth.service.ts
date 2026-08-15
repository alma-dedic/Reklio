import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from './auth.models';

const ACCESS_KEY = 'reklio.token';
const REFRESH_KEY = 'reklio.refresh';
const USER_KEY = 'reklio.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly _currentUser = signal<CurrentUser | null>(this.restoreUser());
  private readonly _token = signal<string | null>(localStorage.getItem(ACCESS_KEY));

  // Dijeljeni refresh u letu — više paralelnih 401 dijeli jedan poziv.
  private refreshInFlight$: Observable<AuthResponse> | null = null;

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

  refresh(): Observable<AuthResponse> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }
    const refreshToken = this.getRefreshToken();
    this.refreshInFlight$ = this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken })
      .pipe(
        tap((response) => this.setSession(response)),
        finalize(() => (this.refreshInFlight$ = null)),
        shareReplay(1),
      );
    return this.refreshInFlight$;
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      // Poništi server-side (best-effort).
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe({
        next: () => {},
        error: () => {},
      });
    }
    this.clearSession();
  }

  getToken(): string | null {
    return this._token();
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  private setSession(response: AuthResponse): void {
    const user: CurrentUser = {
      id: response.id,
      fullName: response.fullName,
      email: response.email,
      role: response.role,
    };
    localStorage.setItem(ACCESS_KEY, response.accessToken);
    localStorage.setItem(REFRESH_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._token.set(response.accessToken);
    this._currentUser.set(user);
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._currentUser.set(null);
  }

  private restoreUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  }
}
