using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Import.ImportOpeningBalances;

/// <summary>
/// يستورد الأرصدة الافتتاحية من تبويب «الأرصدة الافتتاحية» → StockBalance + تأسيس متوسط التكلفة (WAC).
/// ذرّي: الكل أو لا شيء (يمنع اختلال الميزان الافتتاحي — R-09).
/// </summary>
public sealed record ImportOpeningBalancesCommand(byte[] Content, bool Commit) : ICommand<Result<ImportResult>>;
