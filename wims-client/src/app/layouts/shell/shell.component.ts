import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NavPermission } from '../../core/auth/permission-catalog';
import { environment } from '../../../environments/environment';
import { ChangePasswordDialogComponent } from '../../features/admin/change-password/change-password-dialog.component';
import pkg from '../../../../package.json';

interface NavItem {
  label: string;
  path: string;
  icon: string; // مفتاح أيقونة (انظر القالب)
  perm: string[]; // صلاحيات العرض المطلوبة (أيّ منها يكفي)
}

/**
 * الهيكل العام (App Shell) — تنقّل جانبي RTL + شريط علوي.
 * عناصر التنقّل تعكس مراحل الخطة؛ الروابط غير المفعّلة بعد
 * تشير لمسارات ستُبنى في مراحلها.
 */
@Component({
  selector: 'wims-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ChangePasswordDialogComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  readonly navOpen = signal(true);
  readonly userMenuOpen = signal(false);
  readonly avatarUrl = signal<string | null>(null);
  readonly photoError = signal<string | null>(null);
  /** شعار وزارة العدل المرفوع؛ يسقط للختم المرسوم إن غاب الملف. */
  readonly logoOk = signal(true);

  ngOnInit(): void {
    this.loadAvatar();
  }

  /** صورة المستخدم الحالي (إن وُجدت) عبر Blob لتمرير التوكن. */
  private loadAvatar(): void {
    this.http
      .get(`${environment.apiBaseUrl}/me/photo`, { responseType: 'blob' })
      .subscribe({
        next: (blob) => this.avatarUrl.set(URL.createObjectURL(blob)),
        error: () => this.avatarUrl.set(null),
      });
  }

  /** خدمة ذاتية: يغيّر المستخدم صورته الخاصة (POST /api/me/photo). */
  onChangePhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // يسمح باختيار الملف نفسه مجددًا
    if (!file) return;

    this.photoError.set(null);
    const data = new FormData();
    data.append('file', file);
    this.http.post(`${environment.apiBaseUrl}/me/photo`, data).subscribe({
      next: () => {
        this.loadAvatar();
        this.closeUserMenu();
      },
      error: (e: { error?: { detail?: string; message?: string } }) =>
        this.photoError.set(
          e?.error?.detail ?? e?.error?.message ?? 'تعذّر تغيير الصورة.',
        ),
    });
  }

  private readonly changePwDlg =
    viewChild.required<ChangePasswordDialogComponent>('changePwDlg');

  readonly userName = this.auth.userName;
  readonly primaryRole = computed(() => this.auth.roles()[0] ?? 'مستخدم');
  readonly initial = computed(() => (this.auth.userName() ?? '؟').charAt(0));

  /** رقم الإصدار من package.json (المصدر الموحّد المُحدَّث في سير عمل الإصدارات). */
  readonly version = pkg.version;
  readonly year = new Date().getFullYear();

  private readonly allNav: NavItem[] = [
    { label: 'الرئيسية', path: '/dashboard', icon: 'home', perm: [NavPermission.Dashboard] },
    { label: 'الأصناف', path: '/items', icon: 'box', perm: [NavPermission.Items] },
    { label: 'الحركات', path: '/movements', icon: 'transfer', perm: [NavPermission.Vouchers] },
    { label: 'العُهد والموافقات', path: '/approvals', icon: 'stamp', perm: [NavPermission.Approvals] },
    { label: 'الجرد والتنبيهات', path: '/inventory', icon: 'clipboard', perm: [NavPermission.Alerts] },
    { label: 'التقارير', path: '/reports', icon: 'chart', perm: [NavPermission.Reports] },
    { label: 'البيانات الأساسية', path: '/master-data', icon: 'database', perm: ['Warehouses.View', 'Items.View', 'Employees.View', 'Suppliers.View'] },
    { label: 'الإدارة والصلاحيات', path: '/admin', icon: 'shield', perm: ['Users.View', 'Roles.View'] },
  ];

  /** التنقّل المرئي — مُصفّى حسب صلاحيات المستخدم (RBAC؛ أيّ صلاحية من القائمة تكفي). */
  readonly nav = computed(() =>
    this.allNav.filter((i) => this.auth.hasAnyPermission(i.perm)),
  );

  toggleNav(): void {
    this.navOpen.update((v) => !v);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((v) => !v);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  openChangePassword(): void {
    this.closeUserMenu();
    this.changePwDlg().open();
  }

  logout(): void {
    this.closeUserMenu();
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
