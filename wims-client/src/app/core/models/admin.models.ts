// نماذج إدارة المستخدمين والأدوار والصلاحيات (RBAC) — مطابقة لعقود الـ backend.

/** ملخّص مستخدم في الجدول. */
export interface UserSummary {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
}

/** تفاصيل مستخدم للتحرير. */
export interface UserDetail {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roleIds: string[];
  roles: string[];
}

/** طلب إنشاء مستخدم. */
export interface CreateUserRequest {
  userName: string;
  fullName: string;
  email: string;
  password: string;
  roleIds: string[];
}

/** طلب تعديل بيانات مستخدم. */
export interface UpdateUserRequest {
  fullName: string;
  email: string;
}

/** ملخّص دور في الجدول. */
export interface RoleSummary {
  id: string;
  name: string;
  description: string;
  permissionCount: number;
  userCount: number;
  isSystem: boolean;
}

/** تفاصيل دور مع مفاتيح صلاحياته. */
export interface RoleDetail {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  permissionKeys: string[];
}

/** طلب إنشاء دور. */
export interface CreateRoleRequest {
  name: string;
  description: string;
  permissionKeys: string[];
}

/** طلب تعديل بيانات دور. */
export interface UpdateRoleRequest {
  name: string;
  description: string;
}

/** صلاحية كما يعيدها الخادم. */
export interface Permission {
  key: string;
  name: string;
  module: string;
}
