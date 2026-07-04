import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CustodyStatementDto,
  EmployeeDto,
} from '../../core/models/custody.models';

/**
 * خدمة العُهد (المرحلة ٣).
 * الـ backend يوفّر endpointين فقط للعُهد: كشف عهدة موظف وبراءة ذمته.
 * لا يوجد endpoint لقائمة العُهد — كله مبني على الموظف.
 */
@Injectable({ providedIn: 'root' })
export class CustodyService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** GET /api/employees?search= — قائمة خام غير مصفّحة (بحث بالرقم/الاسم/الهوية). */
  employees(search?: string): Observable<EmployeeDto[]> {
    let params = new HttpParams();
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<EmployeeDto[]>(`${this.base}/employees`, { params });
  }

  /** GET /api/custody/statement/{employeeId}?includeHistory= */
  statement(
    employeeId: string,
    includeHistory = false,
  ): Observable<CustodyStatementDto> {
    const params = new HttpParams().set('includeHistory', includeHistory);
    return this.http.get<CustodyStatementDto>(
      `${this.base}/custody/statement/${employeeId}`,
      { params },
    );
  }

  /** POST /api/custody/clear/{employeeId} — براءة ذمة (تفشل بـ 409 إن بقيت بنود قائمة). */
  clear(employeeId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/custody/clear/${employeeId}`,
      {},
    );
  }

  /** POST /api/custody/items/{custodyItemId}/return — إرجاع بند عهدة للمخزن. */
  returnItem(custodyItemId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/custody/items/${custodyItemId}/return`,
      {},
    );
  }
}
