using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Import.ImportItems;

/// <summary>
/// يستورد الأصناف من تبويب «الأصناف» في ملف Excel.
/// <paramref name="Commit"/> = false → معاينة أخطاء فقط؛ true → اعتماد (ذرّي: الكل أو لا شيء).
/// </summary>
public sealed record ImportItemsCommand(byte[] Content, bool Commit) : ICommand<Result<ImportResult>>;
