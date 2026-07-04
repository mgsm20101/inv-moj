// نماذج المصادقة (المرحلة ٦) — مطابقة لعقد POST /api/auth/login.

/** استجابة تسجيل الدخول — LoginResult. */
export interface LoginResult {
  token: string;
  expiresAtUtc: string;
  userName: string;
  roles: string[];
  permissions: string[];
}

/** حالة الجلسة المخزّنة محلياً. */
export interface AuthState {
  token: string;
  expiresAtUtc: string;
  userName: string;
  roles: string[];
  permissions: string[];
}
