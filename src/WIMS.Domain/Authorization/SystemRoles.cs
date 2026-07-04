namespace WIMS.Domain.Authorization;

/// <summary>
/// أسماء الأدوار الأساسية المبذورة — مرجع موحّد يمنع اختلاف السلاسل بين الـ Seeding
/// وبوابة اعتماد الموافقات وإعداد المسارات. الدور <see cref="Admin"/> فائق الصلاحية.
/// </summary>
public static class SystemRoles
{
    /// <summary>مدير النظام — يملك كل الصلاحيات ويتجاوز بوابات الدور في الموافقات.</summary>
    public const string Admin = "مدير النظام";

    /// <summary>أمين مخزن — يدير الأصناف والحركات والأرصدة والاستيراد.</summary>
    public const string WarehouseKeeper = "أمين مخزن";

    /// <summary>معتمِد — يعتمد الحركات (الخطوة الأولى في مسار الموافقة).</summary>
    public const string Approver = "معتمِد";

    /// <summary>مدير مالي — الاعتماد المالي (الخطوة الثانية للصرف الكبير).</summary>
    public const string Finance = "مدير مالي";

    /// <summary>مدقّق — عرض القراءة والتقارير وسجل التدقيق.</summary>
    public const string Auditor = "مدقّق";
}
