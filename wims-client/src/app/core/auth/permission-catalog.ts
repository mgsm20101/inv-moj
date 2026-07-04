// كتالوج الصلاحيات (المرحلة ٦) — نسخة أمامية ثابتة من PermissionKeys.All في الـ backend
// (WIMS.Domain/Authorization/PermissionKeys.cs). بيانات مرجعية ثابتة، لا endpoint لها.
// تُستخدم لعرض مصفوفة صلاحيات المستخدم مجمّعة حسب الوحدة.

export interface PermissionEntry {
  key: string;
  name: string;
  module: string;
}

/** كل الصلاحيات الـ٣٤ بالترتيب والوحدات كما في الخادم. */
export const PERMISSION_CATALOG: PermissionEntry[] = [
  { key: 'Users.View', name: 'عرض المستخدمين', module: 'المستخدمون' },
  { key: 'Users.Manage', name: 'إدارة المستخدمين', module: 'المستخدمون' },
  { key: 'Roles.View', name: 'عرض الأدوار', module: 'الأدوار' },
  { key: 'Roles.Manage', name: 'إدارة الأدوار', module: 'الأدوار' },
  { key: 'Audit.View', name: 'عرض سجل التدقيق', module: 'التدقيق' },
  { key: 'Audit.Edit', name: 'تعديل سجل التدقيق', module: 'التدقيق' },
  { key: 'Items.View', name: 'عرض الأصناف', module: 'الأصناف' },
  { key: 'Items.Manage', name: 'إدارة الأصناف', module: 'الأصناف' },
  { key: 'Warehouses.View', name: 'عرض المخازن', module: 'المخازن' },
  { key: 'Warehouses.Manage', name: 'إدارة المخازن', module: 'المخازن' },
  { key: 'Import.Execute', name: 'استيراد البيانات', module: 'الاستيراد' },
  { key: 'Vouchers.View', name: 'عرض المستندات', module: 'الحركات' },
  { key: 'Vouchers.Create', name: 'إنشاء مستند حركة', module: 'الحركات' },
  { key: 'Vouchers.Submit', name: 'رفع مستند للاعتماد', module: 'الحركات' },
  { key: 'Vouchers.Approve', name: 'اعتماد/رفض المستندات', module: 'الحركات' },
  { key: 'Vouchers.Cancel', name: 'إلغاء المستندات', module: 'الحركات' },
  { key: 'Suppliers.View', name: 'عرض المورّدين', module: 'المورّدون' },
  { key: 'Suppliers.Manage', name: 'إدارة المورّدين', module: 'المورّدون' },
  { key: 'Employees.View', name: 'عرض الموظفين', module: 'الموظفون' },
  { key: 'Employees.Manage', name: 'إدارة الموظفين', module: 'الموظفون' },
  { key: 'Custody.View', name: 'عرض العُهد', module: 'العُهد' },
  { key: 'Custody.Manage', name: 'إدارة العُهد', module: 'العُهد' },
  { key: 'Custody.Transfer', name: 'نقل العُهد', module: 'العُهد' },
  { key: 'Custody.Clear', name: 'براءة ذمة', module: 'العُهد' },
  { key: 'Approvals.View', name: 'عرض الموافقات', module: 'الموافقات' },
  { key: 'Approvals.Act', name: 'اتخاذ إجراء موافقة', module: 'الموافقات' },
  { key: 'Approvals.Configure', name: 'تهيئة مسارات الموافقة', module: 'الموافقات' },
  { key: 'StockCount.View', name: 'عرض الجرد', module: 'الجرد' },
  { key: 'StockCount.Manage', name: 'إدارة الجرد', module: 'الجرد' },
  { key: 'StockCount.Approve', name: 'اعتماد محضر الجرد', module: 'الجرد' },
  { key: 'Alerts.View', name: 'عرض الإنذارات', module: 'الإنذارات' },
  { key: 'Alerts.Manage', name: 'إدارة الإنذارات', module: 'الإنذارات' },
  { key: 'Reports.View', name: 'عرض/تصدير التقارير', module: 'التقارير' },
  { key: 'Dashboard.View', name: 'عرض لوحة المؤشرات', module: 'التقارير' },
];

/** مفاتيح الصلاحيات المستخدمة لتصفية التنقّل الجانبي. */
export const NavPermission = {
  Dashboard: 'Dashboard.View',
  Items: 'Items.View',
  Vouchers: 'Vouchers.View',
  Approvals: 'Approvals.View',
  Alerts: 'Alerts.View',
  Reports: 'Reports.View',
  Users: 'Users.View',
} as const;
