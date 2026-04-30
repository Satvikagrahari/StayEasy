import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { Router } from '@angular/router';
import {
  AuthResponse,
  JwtPayload,
  LoginRequest,
  PasswordResetSendOtpRequest,
  PasswordResetVerifyOtpRequest,
  ResetPasswordRequest,
  SendOtpRequest,
  SignupRequest,
  UpdateProfileRequest,
  UserProfile,
  VerifyOtpRequest
} from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly BASE_URL = `${environment.gatewayUrl}/gateway/auth`;
  private readonly USERS_URL = `${environment.gatewayUrl}/gateway/users`;
  private currentUserSubject = new BehaviorSubject<JwtPayload | null>(this.decodeStoredToken());

  currentUser$ = this.currentUserSubject.asObservable();
  isAuthenticated$: Observable<boolean> = this.currentUserSubject.pipe(map(user => user !== null));
  isAdmin$: Observable<boolean> = this.currentUserSubject.pipe(map(user => user?.role?.toLowerCase() === 'admin'));

  getCurrentUser(): JwtPayload | null {
    return this.currentUserSubject.getValue();
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  clearAuth(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    this.currentUserSubject.next(null);
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.BASE_URL}/login`, credentials).pipe(
      tap(response => this.setSession(response))
    );
  }

  signup(userData: SignupRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/signup`, userData);
  }

  logout(): void {
    this.http.post(`${this.BASE_URL}/logout`, {}).subscribe({ error: () => undefined });
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  sendOtp(request: SendOtpRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/send-otp`, request);
  }

  verifyOtp(request: VerifyOtpRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/verify-otp`, request);
  }

  sendPasswordResetOtp(request: PasswordResetSendOtpRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/forgot-password/send-otp`, request);
  }

  verifyPasswordResetOtp(request: PasswordResetVerifyOtpRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/forgot-password/verify-otp`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/forgot-password/reset`, request);
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.USERS_URL}/profile`);
  }

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.USERS_URL}/profile`, request).pipe(
      tap(() => this.syncCurrentUser(request))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.BASE_URL}/refresh`, {
      refreshToken: localStorage.getItem('refreshToken')
    }).pipe(tap(response => this.setSession(response)));
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem('token', response.token);
    localStorage.setItem('refreshToken', response.refreshToken);
    this.currentUserSubject.next(this.decodeToken(response.token));
  }

  private decodeToken(token: string): JwtPayload | null {
    try {
      const payload = token.split('.')[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const parsed = JSON.parse(decoded) as Record<string, unknown>;
      const normalized = {
        ...parsed,
        email: parsed['email'] ?? parsed['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        role: parsed['role'] ?? parsed['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
        nameid: parsed['nameid'] ?? parsed['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        username: parsed['unique_name'] ?? parsed['name'] ?? parsed['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
      } as JwtPayload;

      if (normalized.exp && normalized.exp * 1000 < Date.now()) {
        this.clearAuth();
        return null;
      }

      return normalized;
    } catch {
      return null;
    }
  }

  private decodeStoredToken(): JwtPayload | null {
    const token = localStorage.getItem('token');
    return token ? this.decodeToken(token) : null;
  }

  private syncCurrentUser(profile: Pick<UserProfile, 'userName' | 'email'>): void {
    const currentUser = this.currentUserSubject.getValue();
    if (!currentUser) {
      return;
    }

    this.currentUserSubject.next({
      ...currentUser,
      username: profile.userName,
      email: profile.email
    });
  }
}
