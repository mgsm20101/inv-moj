import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CategoryDto,
  ImportResult,
  ItemDto,
  ItemsQuery,
  PagedResult,
  UnitDto,
} from '../../core/models/catalog.models';

@Injectable({ providedIn: 'root' })
export class ItemsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getItems(query: ItemsQuery): Observable<PagedResult<ItemDto>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.isActive !== undefined)
      params = params.set('isActive', query.isActive);

    return this.http.get<PagedResult<ItemDto>>(`${this.base}/items`, { params });
  }

  getItem(id: string): Observable<ItemDto> {
    return this.http.get<ItemDto>(`${this.base}/items/${id}`);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/items/${id}/deactivate`, {});
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.base}/categories`);
  }

  getUnits(): Observable<UnitDto[]> {
    return this.http.get<UnitDto[]>(`${this.base}/units`);
  }

  /**
   * استيراد أصناف من Excel.
   * commit=false → معاينة وتحقق فقط (لا حفظ). commit=true → حفظ ذرّي.
   */
  importItems(file: File, commit: boolean): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file);
    const params = new HttpParams().set('commit', commit);
    return this.http.post<ImportResult>(`${this.base}/import/items`, form, {
      params,
    });
  }
}
