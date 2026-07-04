import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateRoleRequest,
  CreateUserRequest,
  Permission,
  RoleDetail,
  RoleSummary,
  UpdateRoleRequest,
  UpdateUserRequest,
  UserDetail,
  UserSummary,
} from '../../core/models/admin.models';

/**
 * خدمة الإدارة (RBAC) — تغطّي نقاط تحكّم المستخدمين والأدوار والصلاحيات
 * وتغيير كلمة المرور الذاتية. أخطاء الخادم بصيغة ProblemDetails.
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // ---- المستخدمون ----
  getUsers(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${this.base}/users`);
  }

  getUser(id: string): Observable<UserDetail> {
    return this.http.get<UserDetail>(`${this.base}/users/${id}`);
  }

  createUser(body: CreateUserRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/users`, body);
  }

  updateUser(id: string, body: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}`, body);
  }

  setUserActive(id: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}/active`, { isActive });
  }

  resetPassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/${id}/password`, {
      newPassword,
    });
  }

  assignRoles(id: string, roleIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}/roles`, { roleIds });
  }

  // ---- الأدوار ----
  getRoles(): Observable<RoleSummary[]> {
    return this.http.get<RoleSummary[]>(`${this.base}/roles`);
  }

  getRole(id: string): Observable<RoleDetail> {
    return this.http.get<RoleDetail>(`${this.base}/roles/${id}`);
  }

  createRole(body: CreateRoleRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/roles`, body);
  }

  updateRole(id: string, body: UpdateRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/roles/${id}`, body);
  }

  setRolePermissions(id: string, permissionKeys: string[]): Observable<void> {
    return this.http.put<void>(`${this.base}/roles/${id}/permissions`, {
      permissionKeys,
    });
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/roles/${id}`);
  }

  // ---- الصلاحيات ----
  getPermissions(): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${this.base}/permissions`);
  }

  // ---- خدمة ذاتية ----
  changePassword(
    currentPassword: string,
    newPassword: string,
  ): Observable<void> {
    return this.http.post<void>(`${this.base}/me/change-password`, {
      currentPassword,
      newPassword,
    });
  }
}
