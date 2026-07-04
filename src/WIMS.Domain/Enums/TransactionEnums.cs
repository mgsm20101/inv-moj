namespace WIMS.Domain.Enums;

/// <summary>نوع المستند (السند/الإذن).</summary>
public enum VoucherType : byte
{
    Receipt = 1,     // إذن إضافة/استلام (GRN)
    Issue = 2,       // إذن صرف
    Transfer = 3,    // تحويل بين مخزنين
    Return = 4,      // مرتجع
    Adjustment = 5,  // تسوية
    Reversal = 6,    // حركة عكسية (إلغاء أثر سند معتمد — لا حذف)
}

/// <summary>دورة حياة المستند. الرصيد يتغيّر حصراً عند الانتقال UnderReview → Approved (Post).</summary>
public enum VoucherStatus : byte
{
    Draft = 1,        // مسودة — قابل للتعديل، بلا أثر على الرصيد
    UnderReview = 2,  // قيد الاعتماد
    Approved = 3,     // معتمد ومُرحّل (Posted)
    Rejected = 4,     // مرفوض
    Cancelled = 5,    // ملغى (لا يُلغى المعتمد — يُعكَس فقط)
}

/// <summary>نوع التسوية.</summary>
public enum AdjustmentType : byte
{
    IncreaseFound = 1,     // زيادة/عثور
    DecreaseShortage = 2,  // عجز/نقص
    Damage = 3,            // تلف
    Destruction = 4,       // إتلاف (بمحضر لجنة)
    Expiry = 5,            // انتهاء صلاحية
}

/// <summary>حالة التحويل (بضاعة في الطريق حتى تأكيد الاستلام).</summary>
public enum TransferStatus : byte
{
    Draft = 1,
    InTransit = 2,   // خرجت من المصدر، لم تصل الهدف
    Received = 3,    // استلمها الهدف
}

/// <summary>نوع الحركة الدفترية (الأثر على الرصيد).</summary>
public enum StockTxnType : byte
{
    Receipt = 1,          // +1
    Issue = 2,            // -1
    TransferOut = 3,      // -1 (المصدر)
    TransferIn = 4,       // +1 (الهدف)
    ReturnIn = 5,         // +1 (مرتجع من طالب)
    ReturnOut = 6,        // -1 (مرتجع لمورّد)
    AdjustIncrease = 7,   // +1
    AdjustDecrease = 8,   // -1
    Reversal = 9,         // حسب الأصل
}
