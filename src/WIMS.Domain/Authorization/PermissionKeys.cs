namespace WIMS.Domain.Authorization;

/// <summary>
/// المصدر الموحّد لمفاتيح الصلاحيات — تُستخدم في الـ Seeding وفي [Authorize(Policy = ...)].
/// إضافة أي صلاحية جديدة تتم هنا حصراً لتبقى المرجع الوحيد (Single Source of Truth).
/// </summary>
public static class PermissionKeys
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Manage = "Users.Manage";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Manage = "Roles.Manage";
    }

    public static class Audit
    {
        public const string View = "Audit.View";
        /// <summary>صلاحية تعديل سجل التدقيق (FR-AUD-01) — تعديل موثّق لا استبدال صامت.</summary>
        public const string Edit = "Audit.Edit";
    }

    public static class Items
    {
        public const string View = "Items.View";
        public const string Manage = "Items.Manage";
    }

    public static class Warehouses
    {
        public const string View = "Warehouses.View";
        public const string Manage = "Warehouses.Manage";
    }

    public static class Import
    {
        public const string Execute = "Import.Execute";
    }

    /// <summary>حركات المخزون (المرحلة 2). فصل الواجبات: من ينشئ لا يعتمد.</summary>
    public static class Vouchers
    {
        public const string View = "Vouchers.View";
        public const string Create = "Vouchers.Create";
        public const string Submit = "Vouchers.Submit";
        public const string Approve = "Vouchers.Approve";
        public const string Cancel = "Vouchers.Cancel";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Manage = "Suppliers.Manage";
    }

    /// <summary>الموظفون والعُهد (المرحلة 3).</summary>
    public static class Employees
    {
        public const string View = "Employees.View";
        public const string Manage = "Employees.Manage";
    }

    public static class Custody
    {
        public const string View = "Custody.View";
        public const string Manage = "Custody.Manage";
        public const string Transfer = "Custody.Transfer";
        public const string Clear = "Custody.Clear";
    }

    public static class Approvals
    {
        public const string View = "Approvals.View";
        public const string Act = "Approvals.Act";
        public const string Configure = "Approvals.Configure";
    }

    /// <summary>الجرد والإنذارات (المرحلة 4).</summary>
    public static class StockCount
    {
        public const string View = "StockCount.View";
        public const string Manage = "StockCount.Manage";
        public const string Approve = "StockCount.Approve";
    }

    public static class Alerts
    {
        public const string View = "Alerts.View";
        public const string Manage = "Alerts.Manage";
    }

    /// <summary>التقارير ولوحة المؤشرات (المرحلة 5).</summary>
    public static class Reports
    {
        public const string View = "Reports.View";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    /// <summary>كل المفاتيح المعرّفة — يُستخدم في الـ Seeding.</summary>
    public static IReadOnlyList<(string Key, string Name, string Module)> All { get; } = new List<(string, string, string)>
    {
        (Users.View,        "عرض المستخدمين",   "المستخدمون"),
        (Users.Manage,      "إدارة المستخدمين", "المستخدمون"),
        (Roles.View,        "عرض الأدوار",      "الأدوار"),
        (Roles.Manage,      "إدارة الأدوار",    "الأدوار"),
        (Audit.View,        "عرض سجل التدقيق",  "التدقيق"),
        (Audit.Edit,        "تعديل سجل التدقيق", "التدقيق"),
        (Items.View,        "عرض الأصناف",      "الأصناف"),
        (Items.Manage,      "إدارة الأصناف",    "الأصناف"),
        (Warehouses.View,   "عرض المخازن",      "المخازن"),
        (Warehouses.Manage, "إدارة المخازن",    "المخازن"),
        (Import.Execute,    "استيراد البيانات", "الاستيراد"),
        (Vouchers.View,     "عرض المستندات",    "الحركات"),
        (Vouchers.Create,   "إنشاء مستند حركة", "الحركات"),
        (Vouchers.Submit,   "رفع مستند للاعتماد", "الحركات"),
        (Vouchers.Approve,  "اعتماد/رفض المستندات", "الحركات"),
        (Vouchers.Cancel,   "إلغاء المستندات",  "الحركات"),
        (Suppliers.View,    "عرض المورّدين",    "المورّدون"),
        (Suppliers.Manage,  "إدارة المورّدين",  "المورّدون"),
        (Employees.View,    "عرض الموظفين",     "الموظفون"),
        (Employees.Manage,  "إدارة الموظفين",   "الموظفون"),
        (Custody.View,      "عرض العُهد",       "العُهد"),
        (Custody.Manage,    "إدارة العُهد",     "العُهد"),
        (Custody.Transfer,  "نقل العُهد",       "العُهد"),
        (Custody.Clear,     "براءة ذمة",        "العُهد"),
        (Approvals.View,    "عرض الموافقات",    "الموافقات"),
        (Approvals.Act,     "اتخاذ إجراء موافقة", "الموافقات"),
        (Approvals.Configure, "تهيئة مسارات الموافقة", "الموافقات"),
        (StockCount.View,   "عرض الجرد",        "الجرد"),
        (StockCount.Manage, "إدارة الجرد",      "الجرد"),
        (StockCount.Approve,"اعتماد محضر الجرد", "الجرد"),
        (Alerts.View,       "عرض الإنذارات",    "الإنذارات"),
        (Alerts.Manage,     "إدارة الإنذارات",  "الإنذارات"),
        (Reports.View,      "عرض/تصدير التقارير", "التقارير"),
        (Dashboard.View,    "عرض لوحة المؤشرات", "التقارير"),
    };
}
