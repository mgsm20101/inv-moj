import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AlertDto,
  AlertScanSummary,
  AlertStatus,
  AlertType,
} from '../../core/models/inventory.models';

/** خدمة التنبيهات (المرحلة ٤). القوائم خام غير مصفّحة، مرتّبة بالخطورة على الخادم. */
@Injectable({ providedIn: 'root' })
export class AlertsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** GET /api/alerts?status=&type= — قائمة خام. */
  list(status?: AlertStatus, type?: AlertType): Observable<AlertDto[]> {
    let params = new HttpParams();
    if (status !== undefined) params = params.set('status', status);
    if (type !== undefined) params = params.set('type', type);
    return this.http.get<AlertDto[]>(`${this.base}/alerts`, { params });
  }

  /** POST /api/alerts/{id}/acknowledge — Open → Acknowledged. */
  acknowledge(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/alerts/${id}/acknowledge`, {});
  }

  /** POST /api/alerts/{id}/resolve — → Resolved. */
  resolve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/alerts/${id}/resolve`, {});
  }

  /** POST /api/alerts/scan — فحص يدوي؛ يرجع ملخّص المُنشأ/المُعالَج/الحرِج. */
  scan(): Observable<AlertScanSummary> {
    return this.http.post<AlertScanSummary>(`${this.base}/alerts/scan`, {});
  }
}
