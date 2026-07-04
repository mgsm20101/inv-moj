using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Custody;

/// <summary>
/// يحوّل الأصناف المستديمة المصروفة (RequiresCustody) تلقائياً إلى بنود عهدة باسم المستلِم،
/// ضمن نفس معاملة الترحيل (BR-CUS-04). لا يستدعي SaveChanges.
/// </summary>
public interface ICustodyProvisioningService
{
    Task<Result> ProvisionAsync(Voucher voucher, string userName, CancellationToken cancellationToken = default);
}
