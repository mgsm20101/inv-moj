import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import {
  PERMISSION_CATALOG,
  PermissionEntry,
} from '../../core/auth/permission-catalog';
import { problemDetail } from '../../core/http/problem-details';
import {
  Permission,
  RoleSummary,
  UserSummary,
} from '../../core/models/admin.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { HasPermissionDirective } from '../../shared/has-permission.directive';
import { AdminService } from './admin.service';
import { RoleEditorComponent } from './roles/role-editor.component';
import { UserEditorComponent } from './users/user-editor.component';

interface PermGroup {
  module: string;
  items: (PermissionEntry & { held: boolean })[];
  heldCount: number;
}

type Tab = 'permissions' | 'users' | 'roles';

/**
 * الإدارة والصلاحيات (المرحلة ٦) — واجهة إدارة RBAC حقيقية بتبويبات:
 *   • صلاحياتي   — مراجعة صلاحيات المستخدم الحالي (للجميع).
 *   • المستخدمون — جدول + CRUD (Users.View / Users.Manage).
 *   • الأدوار    — جدول + CRUD + مصفوفة صلاحيات (Roles.View / Roles.Manage).
 */
@Component({
  selector: 'wims-admin',
  standalone: true,
  imports: [
    HasPermissionDirective,
    UserEditorComponent,
    RoleEditorComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminComponent {
  private readonly auth = inject(AuthService);
  private readonly service = inject(AdminService);

  // ---- التبويبات ----
  readonly canViewUsers = this.auth.hasPermission('Users.View');
  readonly canManageUsers = this.auth.hasPermission('Users.Manage');
  readonly canViewRoles = this.auth.hasPermission('Roles.View');
  readonly canManageRoles = this.auth.hasPermission('Roles.Manage');

  readonly tab = signal<Tab>(
    this.canViewUsers ? 'users' : this.canViewRoles ? 'roles' : 'permissions',
  );

  constructor() {
    // التبويب الافتراضي نشِط منذ التهيئة فلا يمرّ عبر setTab() عند فتح الشاشة —
    // لازم تحميل بياناته صراحةً هنا (setTab يكفي فقط للتبديل اللاحق بين التبويبات).
    const initial = this.tab();
    if (initial === 'users') this.loadUsers();
    else if (initial === 'roles') this.loadRolesAndPerms();
  }

  setTab(t: Tab): void {
    this.tab.set(t);
    if (t === 'users' && this.users() === null) this.loadUsers();
    if (t === 'roles' && this.roles() === null) this.loadRolesAndPerms();
  }

  // ---- صلاحياتي (المراجعة) ----
  readonly userName = this.auth.userName;
  readonly roleNames = this.auth.roles;
  readonly initial = computed(() => (this.auth.userName() ?? '؟').charAt(0));
  readonly totalCount = PERMISSION_CATALOG.length;
  readonly heldCount = computed(() => this.auth.permissions().size);

  readonly groups = computed<PermGroup[]>(() => {
    const held = this.auth.permissions();
    const byModule = new Map<string, (PermissionEntry & { held: boolean })[]>();
    for (const p of PERMISSION_CATALOG) {
      const list = byModule.get(p.module) ?? [];
      list.push({ ...p, held: held.has(p.key) });
      byModule.set(p.module, list);
    }
    return [...byModule.entries()].map(([module, items]) => ({
      module,
      items,
      heldCount: items.filter((i) => i.held).length,
    }));
  });

  // ---- المستخدمون ----
  readonly users = signal<UserSummary[] | null>(null);
  readonly usersLoading = signal(false);
  readonly usersError = signal<string | null>(null);
  readonly editingUserId = signal<string | null | undefined>(undefined); // undefined=مغلق، null=جديد

  loadUsers(): void {
    this.usersLoading.set(true);
    this.usersError.set(null);
    this.service.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        this.usersLoading.set(false);
      },
      error: (e) => {
        this.usersError.set(problemDetail(e, 'تعذّر تحميل المستخدمين.'));
        this.usersLoading.set(false);
      },
    });
  }

  openNewUser(): void {
    this.editingUserId.set(null);
  }
  openEditUser(id: string): void {
    this.editingUserId.set(id);
  }
  closeUserEditor(saved: boolean): void {
    this.editingUserId.set(undefined);
    if (saved) this.loadUsers();
  }

  // ---- الأدوار ----
  readonly roles = signal<RoleSummary[] | null>(null);
  readonly permissions = signal<Permission[]>([]);
  readonly rolesLoading = signal(false);
  readonly rolesError = signal<string | null>(null);
  readonly editingRoleId = signal<string | null | undefined>(undefined);

  loadRolesAndPerms(): void {
    this.rolesLoading.set(true);
    this.rolesError.set(null);
    forkJoin({
      roles: this.service.getRoles(),
      perms: this.service.getPermissions(),
    }).subscribe({
      next: ({ roles, perms }) => {
        this.roles.set(roles);
        this.permissions.set(perms);
        this.rolesLoading.set(false);
      },
      error: (e) => {
        this.rolesError.set(problemDetail(e, 'تعذّر تحميل الأدوار والصلاحيات.'));
        this.rolesLoading.set(false);
      },
    });
  }

  openNewRole(): void {
    this.editingRoleId.set(null);
  }
  openEditRole(id: string): void {
    this.editingRoleId.set(id);
  }
  closeRoleEditor(saved: boolean): void {
    this.editingRoleId.set(undefined);
    if (saved) this.loadRolesAndPerms();
  }

  // ---- حذف دور ----
  private readonly deleteDlg =
    viewChild.required<ConfirmDialogComponent>('deleteDlg');
  private pendingDelete: RoleSummary | null = null;
  readonly deleteName = signal('');

  requestDeleteRole(role: RoleSummary): void {
    this.pendingDelete = role;
    this.deleteName.set(role.name);
    this.deleteDlg().open();
  }

  confirmDeleteRole(): void {
    const role = this.pendingDelete;
    if (!role) return;
    this.pendingDelete = null;
    this.rolesError.set(null);
    this.service.deleteRole(role.id).subscribe({
      next: () => this.loadRolesAndPerms(),
      error: (e) =>
        this.rolesError.set(
          problemDetail(e, 'تعذّر حذف الدور. قد يكون مُسنَداً لمستخدمين.'),
        ),
    });
  }
}
