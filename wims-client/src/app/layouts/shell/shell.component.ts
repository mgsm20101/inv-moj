import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NavPermission } from '../../core/auth/permission-catalog';
import { ChangePasswordDialogComponent } from '../../features/admin/change-password/change-password-dialog.component';

interface NavItem {
  label: string;
  path: string;
  icon: string; // مفتاح أيقونة (انظر القالب)
  phase: string;
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
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly navOpen = signal(true);
  readonly userMenuOpen = signal(false);

  private readonly changePwDlg =
    viewChild.required<ChangePasswordDialogComponent>('changePwDlg');

  readonly userName = this.auth.userName;
  readonly primaryRole = computed(() => this.auth.roles()[0] ?? 'مستخدم');
  readonly initial = computed(() => (this.auth.userName() ?? '؟').charAt(0));

  private readonly allNav: NavItem[] = [
    { label: 'الرئيسية', path: '/dashboard', icon: 'home', phase: '', perm: [NavPermission.Dashboard] },
    { label: 'الأصناف', path: '/items', icon: 'box', phase: '١', perm: [NavPermission.Items] },
    { label: 'الحركات', path: '/movements', icon: 'transfer', phase: '٢', perm: [NavPermission.Vouchers] },
    { label: 'العُهد والموافقات', path: '/approvals', icon: 'stamp', phase: '٣', perm: [NavPermission.Approvals] },
    { label: 'الجرد والتنبيهات', path: '/inventory', icon: 'clipboard', phase: '٤', perm: [NavPermission.Alerts] },
    { label: 'التقارير', path: '/reports', icon: 'chart', phase: '٥', perm: [NavPermission.Reports] },
    { label: 'الإدارة والصلاحيات', path: '/admin', icon: 'shield', phase: '٦', perm: ['Users.View', 'Roles.View'] },
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
