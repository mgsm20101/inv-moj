namespace WIMS.Domain.Enums;

/// <summary>نوع الجرد.</summary>
public enum StockCountType : byte
{
    Full = 1,     // جرد كامل لكل أصناف المخزن
    Partial = 2,  // جرد جزئي لأصناف محدّدة
    Cyclic = 3,   // جرد دوري بالعيّنة
}

/// <summary>
/// دورة حياة محضر الجرد.
/// Draft → Frozen (لقطة الرصيد الدفتري + تجميد الحركة) → UnderReview (إدخال الفعلي + فروقات)
/// → Approved (ترحيل التسويات + فكّ التجميد). الإلغاء يفكّ التجميد أيضاً.
/// </summary>
public enum StockCountStatus : byte
{
    Draft = 1,        // مُخطَّط، حُدِّد نطاقه، لم يُجمَّد بعد
    Frozen = 2,       // الرصيد الدفتري مُلتَقَط والحركة مُجمَّدة، بانتظار العدّ الفعلي
    UnderReview = 3,  // أُدخِل العدّ الفعلي وحُسِبت الفروقات، بانتظار الاعتماد
    Approved = 4,     // اعتُمِد ورُحِّلت التسويات وفُكّ التجميد
    Cancelled = 5,    // أُلغِي وفُكّ التجميد
}

/// <summary>نوع الإنذار.</summary>
public enum AlertType : byte
{
    ReorderPoint = 1,  // بلوغ نقطة إعادة الطلب
    MinStock = 2,      // بلوغ/تحت الحد الأدنى
    NearExpiry = 3,    // قرب انتهاء الصلاحية
    Expired = 4,       // منتهي الصلاحية فعلاً
    Stagnant = 5,      // صنف راكد (بلا حركة صرف)
}

/// <summary>درجة خطورة الإنذار.</summary>
public enum AlertSeverity : byte
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

/// <summary>حالة الإنذار.</summary>
public enum AlertStatus : byte
{
    Open = 1,          // مفتوح ونشِط
    Acknowledged = 2,  // اطُّلِع عليه
    Resolved = 3,      // انتهى سببه (تلقائياً أو يدوياً)
}
