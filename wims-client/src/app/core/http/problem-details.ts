import { HttpErrorResponse } from '@angular/common/http';

/** شكل استجابة ProblemDetails القياسية من الـ backend. */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
}

/**
 * يستخرج رسالة قابلة للعرض من خطأ HTTP.
 * يُفضّل `detail` من ProblemDetails، ثم `title`، ثم رسالة احتياطية.
 */
export function problemDetail(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ProblemDetails | string | null | undefined;
    if (typeof body === 'string' && body.trim()) return body;
    if (body && typeof body === 'object') {
      if (body.detail?.trim()) return body.detail;
      if (body.title?.trim()) return body.title;
    }
    if (err.status === 0) return 'تعذّر الاتصال بالخادم. تحقّق من الشبكة.';
  }
  return fallback;
}
