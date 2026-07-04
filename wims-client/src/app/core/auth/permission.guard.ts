import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * مصنع حارس صلاحيات — يسمح بالوصول إذا كان المستخدم يملك الصلاحية
 * (أو أيّ صلاحية من القائمة). يُستخدم بعد authGuard في نفس المسار.
 * بلا جلسة → تسجيل الدخول؛ بجلسة دون صلاحية → إعادة للرئيسية.
 */
export function permissionGuard(key: string | string[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const keys = Array.isArray(key) ? key : [key];

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
      });
    }

    if (auth.hasAnyPermission(keys)) return true;

    return router.createUrlTree(['/dashboard']);
  };
}
