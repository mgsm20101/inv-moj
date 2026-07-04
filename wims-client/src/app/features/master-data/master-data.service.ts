import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserSummary } from '../../core/models/admin.models';
import { CategoryDto, UnitDto } from '../../core/models/catalog.models';
import { EmployeeDto } from '../../core/models/custody.models';
import { WarehouseDto } from '../../core/models/voucher.models';
import {
  CreateCategoryRequest,
  CreateEmployeeRequest,
  CreateSupplierRequest,
  CreateUnitRequest,
  CreateWarehouseRequest,
  EmployeeDetailDto,
  SupplierDetailDto,
  SupplierDto,
  UpdateCategoryRequest,
  UpdateEmployeeRequest,
  UpdateSupplierRequest,
  UpdateUnitRequest,
  UpdateWarehouseRequest,
  WarehouseDetailDto,
} from './master-data.models';

/**
 * خدمة «البيانات الأساسية» — تجمّع CRUD الكيانات المرجعية الأربعة
 * (المخازن، الموظفون، التصنيفات، وحدات القياس) في مكان واحد.
 * أخطاء الخادم بصيغة ProblemDetails (تُقرأ عبر problemDetail).
 */
@Injectable({ providedIn: 'root' })
export class MasterDataService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // ---- المخازن ----
  getWarehouses(): Observable<WarehouseDto[]> {
    return this.http.get<WarehouseDto[]>(`${this.base}/warehouses`);
  }
  getWarehouse(id: string): Observable<WarehouseDetailDto> {
    return this.http.get<WarehouseDetailDto>(`${this.base}/warehouses/${id}`);
  }
  createWarehouse(body: CreateWarehouseRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/warehouses`, body);
  }
  updateWarehouse(id: string, body: UpdateWarehouseRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/warehouses/${id}`, body);
  }

  // ---- الموظفون ----
  getEmployees(search?: string): Observable<EmployeeDto[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<EmployeeDto[]>(`${this.base}/employees`, { params });
  }
  getEmployee(id: string): Observable<EmployeeDetailDto> {
    return this.http.get<EmployeeDetailDto>(`${this.base}/employees/${id}`);
  }
  createEmployee(body: CreateEmployeeRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/employees`, body);
  }
  updateEmployee(id: string, body: UpdateEmployeeRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/employees/${id}`, body);
  }

  // ---- التصنيفات ----
  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.base}/categories`);
  }
  createCategory(body: CreateCategoryRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/categories`, body);
  }
  updateCategory(id: string, body: UpdateCategoryRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/categories/${id}`, body);
  }

  // ---- وحدات القياس ----
  getUnits(): Observable<UnitDto[]> {
    return this.http.get<UnitDto[]>(`${this.base}/units`);
  }
  createUnit(body: CreateUnitRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/units`, body);
  }
  updateUnit(id: string, body: UpdateUnitRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/units/${id}`, body);
  }

  // ---- الموردون ----
  getSuppliers(): Observable<SupplierDto[]> {
    return this.http.get<SupplierDto[]>(`${this.base}/suppliers`);
  }
  getSupplier(id: string): Observable<SupplierDetailDto> {
    return this.http.get<SupplierDetailDto>(`${this.base}/suppliers/${id}`);
  }
  createSupplier(body: CreateSupplierRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/suppliers`, body);
  }
  updateSupplier(id: string, body: UpdateSupplierRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/suppliers/${id}`, body);
  }

  // ---- مساعد: قائمة المستخدمين (لاختيار أمين المخزن / ربط الموظف) ----
  getUsers(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${this.base}/users`);
  }
}
