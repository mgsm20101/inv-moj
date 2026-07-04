import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CountEntry,
  PlanStockCountCommand,
  StockCountDetailDto,
  StockCountDto,
  StockCountStatus,
} from '../../core/models/inventory.models';

/**
 * خدمة الجرد (المرحلة ٤).
 * القائمة خام (Ok مباشر)؛ التفصيل مغلّف في Result (قيمة خام عند النجاح).
 * سير العمل: plan → freeze → count → submit → approve/cancel.
 */
@Injectable({ providedIn: 'root' })
export class StockCountService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** GET /api/stock-counts?status= — قائمة خام مرتّبة بالأحدث. */
  list(status?: StockCountStatus): Observable<StockCountDto[]> {
    let params = new HttpParams();
    if (status !== undefined) params = params.set('status', status);
    return this.http.get<StockCountDto[]>(`${this.base}/stock-counts`, {
      params,
    });
  }

  /** GET /api/stock-counts/{id} — تفصيل مع البنود. */
  get(id: string): Observable<StockCountDetailDto> {
    return this.http.get<StockCountDetailDto>(
      `${this.base}/stock-counts/${id}`,
    );
  }

  /** POST /api/stock-counts — إنشاء محضر (مسودة)؛ يرجع المعرّف. */
  plan(cmd: PlanStockCountCommand): Observable<string> {
    return this.http.post<string>(`${this.base}/stock-counts`, cmd);
  }

  /** POST /api/stock-counts/{id}/freeze — تجميد ولقطة الأرصدة الدفترية. */
  freeze(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-counts/${id}/freeze`, {});
  }

  /** POST /api/stock-counts/{id}/count — إدخال الأعداد الفعلية. */
  enterCount(id: string, entries: CountEntry[]): Observable<void> {
    return this.http.post<void>(
      `${this.base}/stock-counts/${id}/count`,
      entries,
    );
  }

  /** POST /api/stock-counts/{id}/submit — رفع للمراجعة. */
  submit(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-counts/${id}/submit`, {});
  }

  /** POST /api/stock-counts/{id}/approve — اعتماد وترحيل تسويات الفروقات (SoD). */
  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-counts/${id}/approve`, {});
  }

  /** POST /api/stock-counts/{id}/cancel — إلغاء المحضر. */
  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/stock-counts/${id}/cancel`, {});
  }
}
