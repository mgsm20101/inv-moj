import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

const TOKEN_KEY = 'wims.token';
const PROFILE_KEY = 'wims.auth';

/**
 * يرفق توكن JWT على كل طلب، ويعالج انتهاء الجلسة (401):
 * يمسح الجلسة ويحوّل لتسجيل الدخول (عدا طلب الدخول نفسه).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = localStorage.getItem(TOKEN_KEY) ?? sessionStorage.getItem(TOKEN_KEY);
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const isLogin = req.url.includes('/auth/login');
      if (err.status === 401 && !isLogin) {
        authToken.clear();
        localStorage.removeItem(PROFILE_KEY);
        sessionStorage.removeItem(PROFILE_KEY);
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      }
      return throwError(() => err);
    }),
  );
};

export const authToken = {
  key: TOKEN_KEY,
  get: (): string | null =>
    localStorage.getItem(TOKEN_KEY) ?? sessionStorage.getItem(TOKEN_KEY),
  set: (token: string, remember: boolean): void => {
    (remember ? localStorage : sessionStorage).setItem(TOKEN_KEY, token);
  },
  clear: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
  },
};
