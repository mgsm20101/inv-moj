namespace WIMS.Application.Common.Interfaces;

/// <summary>
/// يولّد كود الصنف الموحّد "GG-CCCC-SSSS": مقطعا التصنيف من مسار الفئة + تسلسل تلقائي (القسم 4).
/// </summary>
public interface IItemCodeGenerator
{
    /// <summary>يولّد الكود التالي للتصنيف المحدّد بناءً على أعلى تسلسل حالي داخله.</summary>
    Task<string> GenerateAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
