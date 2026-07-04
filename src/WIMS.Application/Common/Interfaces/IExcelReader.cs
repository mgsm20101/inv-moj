namespace WIMS.Application.Common.Interfaces;

/// <summary>يقرأ تبويباً من ملف Excel ويعيد صفوفه مفهرسة بأسماء الأعمدة (مطبَّعة).</summary>
public interface IExcelReader
{
    /// <summary>يقرأ التبويب المحدّد؛ يعيد null إذا لم يوجد التبويب في الملف.</summary>
    ExcelSheetData? ReadSheet(byte[] content, string sheetName);
}

/// <summary>صف بيانات من Excel — رقم الصف الفعلي + الخلايا بحسب اسم العمود.</summary>
public sealed record ExcelRowData(int RowNumber, IReadOnlyDictionary<string, string> Cells)
{
    /// <summary>يعيد قيمة الخلية مطبَّعة (Trim) أو سلسلة فارغة.</summary>
    public string Get(string column) => Cells.TryGetValue(column, out var v) ? v : string.Empty;
}

/// <summary>بيانات تبويب كامل: العناوين + الصفوف.</summary>
public sealed record ExcelSheetData(string SheetName, IReadOnlyList<string> Headers, IReadOnlyList<ExcelRowData> Rows);
