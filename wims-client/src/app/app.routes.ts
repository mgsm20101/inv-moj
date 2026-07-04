import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
    title: 'تسجيل الدخول · WIMS',
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layouts/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
        title: 'الرئيسية · WIMS',
      },
      {
        path: 'items',
        loadComponent: () =>
          import('./features/items/catalog/items-catalog.component').then(
            (m) => m.ItemsCatalogComponent,
          ),
        title: 'كتالوج الأصناف · WIMS',
      },
      {
        path: 'items/import',
        canActivate: [permissionGuard('Import.Execute')],
        loadComponent: () =>
          import('./features/items/import/items-import.component').then(
            (m) => m.ItemsImportComponent,
          ),
        title: 'رفع أصناف · WIMS',
      },
      {
        path: 'items/new',
        canActivate: [permissionGuard('Items.Manage')],
        loadComponent: () =>
          import('./features/items/form/item-form.component').then(
            (m) => m.ItemFormComponent,
          ),
        title: 'صنف جديد · WIMS',
      },
      {
        path: 'items/:id/edit',
        canActivate: [permissionGuard('Items.Manage')],
        loadComponent: () =>
          import('./features/items/form/item-form.component').then(
            (m) => m.ItemFormComponent,
          ),
        title: 'تعديل صنف · WIMS',
      },
      {
        path: 'movements',
        loadComponent: () =>
          import('./features/movements/list/movements-list.component').then(
            (m) => m.MovementsListComponent,
          ),
        title: 'الحركات · WIMS',
      },
      {
        path: 'movements/new',
        canActivate: [permissionGuard('Vouchers.Create')],
        loadComponent: () =>
          import('./features/movements/create/voucher-create.component').then(
            (m) => m.VoucherCreateComponent,
          ),
        title: 'إذن جديد · WIMS',
      },
      {
        path: 'movements/:id',
        loadComponent: () =>
          import('./features/movements/detail/voucher-detail.component').then(
            (m) => m.VoucherDetailComponent,
          ),
        title: 'مستند حركة · WIMS',
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./features/approvals/inbox/approvals-inbox.component').then(
            (m) => m.ApprovalsInboxComponent,
          ),
        title: 'صندوق الموافقات · WIMS',
      },
      {
        path: 'custody',
        loadComponent: () =>
          import('./features/custody/statement/custody-statement.component').then(
            (m) => m.CustodyStatementComponent,
          ),
        title: 'كشف العُهد · WIMS',
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('./features/inventory/alerts/alerts.component').then(
            (m) => m.AlertsComponent,
          ),
        title: 'التنبيهات · WIMS',
      },
      {
        path: 'inventory/counts',
        loadComponent: () =>
          import('./features/inventory/counts-list/counts-list.component').then(
            (m) => m.CountsListComponent,
          ),
        title: 'محاضر الجرد · WIMS',
      },
      {
        path: 'inventory/counts/new',
        loadComponent: () =>
          import('./features/inventory/count-plan/count-plan.component').then(
            (m) => m.CountPlanComponent,
          ),
        title: 'جرد جديد · WIMS',
      },
      {
        path: 'inventory/counts/:id',
        loadComponent: () =>
          import('./features/inventory/count-detail/count-detail.component').then(
            (m) => m.CountDetailComponent,
          ),
        title: 'محضر جرد · WIMS',
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then(
            (m) => m.ReportsComponent,
          ),
        title: 'التقارير · WIMS',
      },
      {
        path: 'master-data',
        canActivate: [
          permissionGuard(['Warehouses.View', 'Items.View', 'Employees.View']),
        ],
        loadComponent: () =>
          import('./features/master-data/master-data.component').then(
            (m) => m.MasterDataComponent,
          ),
        title: 'البيانات الأساسية · WIMS',
      },
      {
        path: 'admin',
        canActivate: [permissionGuard(['Users.View', 'Roles.View'])],
        loadComponent: () =>
          import('./features/admin/admin.component').then(
            (m) => m.AdminComponent,
          ),
        title: 'الإدارة والصلاحيات · WIMS',
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
