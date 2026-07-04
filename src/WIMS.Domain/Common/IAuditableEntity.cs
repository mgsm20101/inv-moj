namespace WIMS.Domain.Common;

/// <summary>
/// واجهة تُعلِّم الكيانات التي يجب أن يملأ لها الـ Infrastructure حقول التدقيق آلياً.
/// كل <see cref="BaseEntity"/> يحققها ضمناً.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
}
