import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../core/models/catalog.models';
import {
  CreateVoucherCommand,
  StockBalanceDto,
  SupplierDto,
  VoucherDetailDto,
  VoucherDto,
  VouchersQuery,
  WarehouseDto,
} from '../../core/models/voucher.models';

@Injectable({ providedIn: 'root' })
export class VouchersService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  list(query: VouchersQuery): Observable<PagedResult<VoucherDto>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.type !== undefined) params = params.set('type', query.type);
    if (query.status !== undefined) params = params.set('status', query.status);
    return this.http.get<PagedResult<VoucherDto>>(`${this.base}/vouchers`, {
      params,
    });
  }

  get(id: string): Observable<VoucherDetailDto> {
    return this.http.get<VoucherDetailDto>(`${this.base}/vouchers/${id}`);
  }

  create(cmd: CreateVoucherCommand): Observable<string> {
    return this.http.post<string>(`${this.base}/vouchers`, cmd);
  }

  submit(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/vouchers/${id}/submit`, {});
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/vouchers/${id}/approve`, {});
  }

  reject(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/vouchers/${id}/reject`, { reason });
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/vouchers/${id}/cancel`, {});
  }

  confirmTransfer(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/vouchers/${id}/confirm-transfer`,
      {},
    );
  }

  warehouses(): Observable<WarehouseDto[]> {
    return this.http.get<WarehouseDto[]>(`${this.base}/warehouses`);
  }

  suppliers(): Observable<SupplierDto[]> {
    return this.http.get<SupplierDto[]>(`${this.base}/suppliers`);
  }

  stockBalances(
    warehouseId?: string,
    itemId?: string,
  ): Observable<StockBalanceDto[]> {
    let params = new HttpParams();
    if (warehouseId) params = params.set('warehouseId', warehouseId);
    if (itemId) params = params.set('itemId', itemId);
    return this.http.get<StockBalanceDto[]>(`${this.base}/stock/balances`, {
      params,
    });
  }
}
