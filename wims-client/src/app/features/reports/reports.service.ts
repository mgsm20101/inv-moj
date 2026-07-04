import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DashboardDto,
  ReportDocument,
  ReportExportFormat,
} from '../../core/models/reports.models';

/** قيم فلاتر التقرير — تُمرَّر كـ query params (تُتجاهل الفارغة). */
export type ReportParams = Record<string, string | number | boolean | null | undefined>;

/**
 * خدمة التقارير ولوحة المعلومات (المرحلة ٥).
 * كل تقرير له نفس مسار الخادم بثلاث صيغ عبر ?format:
 *  - json  → ReportDocument (للعرض في الواجهة)
 *  - excel → ملف xlsx (تنزيل)
 *  - (افتراضي) pdf → ملف pdf (تنزيل)
 */
@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** GET /api/dashboard — مؤشرات لوحة المعلومات (قيمة خام). */
  dashboard(nearExpiryDays = 30): Observable<DashboardDto> {
    const params = new HttpParams().set('nearExpiryDays', nearExpiryDays);
    return this.http.get<DashboardDto>(`${this.base}/dashboard`, { params });
  }

  private buildParams(p: ReportParams): HttpParams {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(p)) {
      if (v !== null && v !== undefined && v !== '') {
        params = params.set(k, v);
      }
    }
    return params;
  }

  /** جلب مستند التقرير للعرض (format=json). path مثل 'stock-balance' أو 'item-card/{id}'. */
  fetch(path: string, p: ReportParams = {}): Observable<ReportDocument> {
    const params = this.buildParams({ ...p, format: 'json' });
    return this.http.get<ReportDocument>(`${this.base}/reports/${path}`, {
      params,
    });
  }

  /** تنزيل التقرير كملف (pdf/excel). يرجع Blob مع نوع المحتوى من الخادم. */
  download(
    path: string,
    p: ReportParams = {},
    format: ReportExportFormat = 'pdf',
  ): Observable<Blob> {
    const params = this.buildParams({
      ...p,
      ...(format === 'excel' ? { format: 'excel' } : {}),
    });
    return this.http.get(`${this.base}/reports/${path}`, {
      params,
      responseType: 'blob',
    });
  }
}
