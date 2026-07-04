using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Catalog;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Import.ImportItems;

public sealed class ImportItemsCommandHandler(IAppDbContext db, IExcelReader reader)
    : IRequestHandler<ImportItemsCommand, Result<ImportResult>>
{
    private const string SheetName = "الأصناف";

    // أسماء الأعمدة كما في القالب المعتمد (القسم 3.1).
    private static class Col
    {
        public const string Code = "كود الصنف";
        public const string NameAr = "الاسم بالعربي";
        public const string NameEn = "الاسم بالإنجليزي";
        public const string Category = "كود التصنيف";
        public const string Type = "نوع الصنف";
        public const string Unit = "وحدة القياس";
        public const string TracksBatch = "يتتبع دفعة؟";
        public const string TracksExpiry = "يتتبع صلاحية؟";
        public const string TracksSerial = "يتتبع سيريال؟";
        public const string Min = "الحد الأدنى";
        public const string Reorder = "نقطة إعادة الطلب";
        public const string Max = "الحد الأقصى";
        public const string Hazard = "تصنيف الخطورة";
        public const string ShelfLife = "العمر الافتراضي (يوم)";
        public const string Barcode = "باركود";
    }

    private static readonly string[] RequiredHeaders =
        [Col.Code, Col.NameAr, Col.Category, Col.Type, Col.Unit];

    private static readonly Dictionary<string, ItemType> TypeMap = new()
    {
        ["مستهلك"] = ItemType.Consumable,
        ["مستديم"] = ItemType.Durable,
        ["خطر"] = ItemType.Hazardous,
        ["قابل للتلف"] = ItemType.Perishable,
    };

    public async Task<Result<ImportResult>> Handle(ImportItemsCommand request, CancellationToken cancellationToken)
    {
        var sheet = reader.ReadSheet(request.Content, SheetName);
        if (sheet is null)
            return new ImportResult(0, 0, 0, false,
                [new ImportRowError(0, SheetName, $"التبويب «{SheetName}» غير موجود في الملف.")]);

        // R-13: التحقق من بنية القالب.
        var structural = RequiredHeaders
            .Where(h => !sheet.Headers.Contains(h))
            .Select(h => new ImportRowError(1, h, $"عمود إلزامي مفقود: «{h}»."))
            .ToList();
        if (structural.Count > 0)
            return new ImportResult(sheet.Rows.Count, 0, 0, false, structural);

        // ── جلب المراجع من قاعدة البيانات ──
        var categories = await db.ItemCategories
            .Select(c => new { c.Id, c.Code, c.IsActive, IsLeaf = !db.ItemCategories.Any(x => x.ParentId == c.Id) })
            .ToListAsync(cancellationToken);
        var categoryByCode = categories.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var units = await db.UnitsOfMeasure.Select(u => new { u.Id, u.Code, u.IsActive }).ToListAsync(cancellationToken);
        var unitByCode = units.ToDictionary(u => u.Code, StringComparer.OrdinalIgnoreCase);

        var existingCodes = (await db.Items.Select(i => i.ItemCode).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingBarcodes = (await db.Items.Where(i => i.Barcode != null).Select(i => i.Barcode!).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportRowError>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toInsert = new List<Item>();
        var validRows = 0;

        foreach (var row in sheet.Rows)
        {
            if (IsEmptyRow(row)) continue; // R-14

            var rowErrors = new List<ImportRowError>();

            var code = row.Get(Col.Code);
            var nameAr = row.Get(Col.NameAr);
            var categoryCode = row.Get(Col.Category);
            var typeText = row.Get(Col.Type);
            var unitCode = row.Get(Col.Unit);

            if (string.IsNullOrWhiteSpace(code)) rowErrors.Add(new(row.RowNumber, Col.Code, "كود الصنف مطلوب."));
            if (string.IsNullOrWhiteSpace(nameAr)) rowErrors.Add(new(row.RowNumber, Col.NameAr, "اسم الصنف مطلوب."));

            // R-01: تكرار الكود داخل الملف / مقابل النظام.
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (!seenCodes.Add(code)) rowErrors.Add(new(row.RowNumber, Col.Code, $"كود الصنف «{code}» مكرر داخل الملف."));
                if (existingCodes.Contains(code)) rowErrors.Add(new(row.RowNumber, Col.Code, $"كود الصنف «{code}» موجود مسبقاً في النظام."));
            }

            // R-07: التصنيف موجود وعقدة نهائية ونشط.
            categoryByCode.TryGetValue(categoryCode, out var category);
            if (category is null) rowErrors.Add(new(row.RowNumber, Col.Category, $"كود التصنيف «{categoryCode}» غير موجود."));
            else if (!category.IsLeaf) rowErrors.Add(new(row.RowNumber, Col.Category, "التصنيف ليس عقدة نهائية (يحتوي تصنيفات فرعية)."));
            else if (!category.IsActive) rowErrors.Add(new(row.RowNumber, Col.Category, "التصنيف غير نشط."));

            // نوع الصنف.
            TypeMap.TryGetValue(typeText, out var itemType);
            if (!TypeMap.ContainsKey(typeText)) rowErrors.Add(new(row.RowNumber, Col.Type, $"نوع الصنف «{typeText}» غير صحيح."));

            // R-03: وحدة القياس.
            unitByCode.TryGetValue(unitCode, out var unit);
            if (unit is null) rowErrors.Add(new(row.RowNumber, Col.Unit, $"وحدة القياس «{unitCode}» غير معرّفة."));

            // أبعاد التتبّع (افتراضيات حسب النوع + تجاوز يدوي).
            var tracksBatch = ParseBool(row.Get(Col.TracksBatch)) ?? itemType is ItemType.Hazardous or ItemType.Perishable;
            var tracksExpiry = ParseBool(row.Get(Col.TracksExpiry)) ?? itemType == ItemType.Perishable;
            var tracksSerial = ParseBool(row.Get(Col.TracksSerial)) ?? itemType == ItemType.Durable;
            if (itemType == ItemType.Perishable) tracksExpiry = true;

            // الحدود العددية (BR-07).
            var min = ParseDecimal(row.Get(Col.Min), row.RowNumber, Col.Min, rowErrors) ?? 0;
            var reorder = ParseDecimal(row.Get(Col.Reorder), row.RowNumber, Col.Reorder, rowErrors) ?? 0;
            var max = ParseNullableDecimal(row.Get(Col.Max), row.RowNumber, Col.Max, rowErrors);
            if (min < 0 || reorder < 0) rowErrors.Add(new(row.RowNumber, Col.Min, "الحدود لا يمكن أن تكون سالبة."));
            if (reorder < min) rowErrors.Add(new(row.RowNumber, Col.Reorder, "نقطة الطلب أقل من الحد الأدنى."));
            if (max.HasValue && max < reorder) rowErrors.Add(new(row.RowNumber, Col.Max, "الحد الأقصى أقل من نقطة الطلب."));

            // R-08/BR-05: الخطر يتطلب تصنيف خطورة.
            var hazard = row.Get(Col.Hazard);
            if (itemType == ItemType.Hazardous && string.IsNullOrWhiteSpace(hazard))
                rowErrors.Add(new(row.RowNumber, Col.Hazard, "الصنف الخطر يتطلب تصنيف خطورة."));

            // BR-06: القابل للتلف يتطلب عمراً افتراضياً.
            int? shelfLife = null;
            var shelfText = row.Get(Col.ShelfLife);
            if (!string.IsNullOrWhiteSpace(shelfText))
            {
                if (int.TryParse(shelfText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sl) && sl > 0) shelfLife = sl;
                else rowErrors.Add(new(row.RowNumber, Col.ShelfLife, "العمر الافتراضي يجب أن يكون رقماً موجباً."));
            }
            if (itemType == ItemType.Perishable && shelfLife is null)
                rowErrors.Add(new(row.RowNumber, Col.ShelfLife, "الصنف القابل للتلف يتطلب عمراً افتراضياً (بالأيام)."));

            // الباركود فريد.
            var barcode = row.Get(Col.Barcode);
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                if (!seenBarcodes.Add(barcode)) rowErrors.Add(new(row.RowNumber, Col.Barcode, $"الباركود «{barcode}» مكرر داخل الملف."));
                if (existingBarcodes.Contains(barcode)) rowErrors.Add(new(row.RowNumber, Col.Barcode, $"الباركود «{barcode}» موجود مسبقاً."));
            }

            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                continue;
            }

            validRows++;
            toInsert.Add(new Item
            {
                ItemCode = code,
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                NameAr = nameAr,
                NameEn = string.IsNullOrWhiteSpace(row.Get(Col.NameEn)) ? null : row.Get(Col.NameEn),
                CategoryId = category!.Id,
                ItemType = itemType,
                BaseUnitId = unit!.Id,
                TracksBatch = tracksBatch,
                TracksExpiry = tracksExpiry,
                TracksSerial = tracksSerial,
                MinStock = min,
                ReorderPoint = reorder,
                MaxStock = max,
                HazardClass = string.IsNullOrWhiteSpace(hazard) ? null : hazard,
                ShelfLifeDays = shelfLife,
                IsActive = true,
                IsStockItem = true,
            });
        }

        var totalDataRows = sheet.Rows.Count(r => !IsEmptyRow(r));

        // R-09: الاستيراد ذرّي — لا اعتماد مع وجود أي خطأ.
        var committed = false;
        var importedCount = 0;
        if (request.Commit && errors.Count == 0 && toInsert.Count > 0)
        {
            db.Items.AddRange(toInsert);
            await db.SaveChangesAsync(cancellationToken);
            committed = true;
            importedCount = toInsert.Count;
        }

        return new ImportResult(totalDataRows, validRows, importedCount, committed, errors);
    }

    private static bool IsEmptyRow(ExcelRowData row) => row.Cells.Values.All(string.IsNullOrWhiteSpace);

    private static bool? ParseBool(string value) => value.Trim() switch
    {
        "نعم" or "true" or "1" => true,
        "لا" or "false" or "0" => false,
        _ => null,
    };

    private static decimal? ParseDecimal(string value, int rowNumber, string column, List<ImportRowError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
        errors.Add(new(rowNumber, column, $"القيمة «{value}» ليست رقماً صحيحاً."));
        return null;
    }

    private static decimal? ParseNullableDecimal(string value, int rowNumber, string column, List<ImportRowError> errors)
        => ParseDecimal(value, rowNumber, column, errors);
}
