import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { authToken } from './auth.interceptor';
import { AuthState, LoginResult } from './auth.models';

const PROFILE_KEY = 'wims.auth';

/**
 * خدمة المصادقة (المرحلة ٦) — تسجيل دخول حقيقي مقابل POST /api/auth/login.
 * تخزّن التوكن (للـ interceptor) وملف الجلسة (اسم/أدوار/صلاحيات)، وتوفّر
 * فحوص الصلاحيات وحالة المصادقة. تُستعاد الجلسة عند إقلاع التطبيق.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  private readonly state = signal<AuthState | null>(this.restore());

  readonly userName = computed(() => this.state()?.userName ?? null);
  readonly roles = computed(() => this.state()?.roles ?? []);
  readonly permissions = computed(() => new Set(this.state()?.permissions ?? []));
  readonly isAuthenticated = computed(() => {
    const s = this.state();
    if (!s?.token) return false;
    return new Date(s.expiresAtUtc).getTime() > Date.now();
  });

  /** POST /api/auth/login — عند النجاح تُحفظ الجلسة وتُضبط الإشارات. */
  login(
    userName: string,
    password: string,
    remember: boolean,
  ): Observable<LoginResult> {
    return this.http
      .post<LoginResult>(`${this.base}/auth/login`, { userName, password })
      .pipe(tap((res) => this.persist(res, remember)));
  }

  logout(): void {
    authToken.clear();
    localStorage.removeItem(PROFILE_KEY);
    sessionStorage.removeItem(PROFILE_KEY);
    this.state.set(null);
  }

  hasPermission(key: string): boolean {
    return this.permissions().has(key);
  }

  hasAnyPermission(keys: string[]): boolean {
    const p = this.permissions();
    return keys.some((k) => p.has(k));
  }

  private persist(res: LoginResult, remember: boolean): void {
    const store = remember ? localStorage : sessionStorage;
    authToken.set(res.token, remember);
    const profile: AuthState = {
      token: res.token,
      expiresAtUtc: res.expiresAtUtc,
      userName: res.userName,
      roles: res.roles,
      permissions: res.permissions,
    };
    store.setItem(PROFILE_KEY, JSON.stringify(profile));
    this.state.set(profile);
  }

  private restore(): AuthState | null {
    const raw =
      localStorage.getItem(PROFILE_KEY) ?? sessionStorage.getItem(PROFILE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthState;
    } catch {
      return null;
    }
  }
}
