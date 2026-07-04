using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.Posting;

/// <summary>
/// محرّك الترحيل (Posting): يولّد حركات الدفتر ويحدّث الأرصدة لسند معتمد،
/// بتكلفة طبقية بالدُفعة (Batch-Layered) وتخصيص FEFO/FIFO. لا يستدعي SaveChanges (المستدعي يتكفّل بالذرّية).
/// </summary>
public interface IVoucherPostingService
{
    /// <summary>يرحّل سند الاستلام/الصرف/المرتجع/التسوية (وخروج التحويل). يعيد فشلاً عند نقص الرصيد.</summary>
    Task<Result> PostAsync(Voucher voucher, string userName, CancellationToken cancellationToken = default);

    /// <summary>يؤكّد استلام تحويل (In-Transit → Received): يدخل البضاعة للمخزن الهدف بنفس التكلفة.</summary>
    Task<Result> ConfirmTransferReceiptAsync(Voucher voucher, string userName, CancellationToken cancellationToken = default);
}
