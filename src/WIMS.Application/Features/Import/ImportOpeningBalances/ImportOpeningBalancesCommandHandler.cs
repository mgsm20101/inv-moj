using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Inventory;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Import.ImportOpeningBalances;

public sealed class ImportOpeningBalancesCommandHandler(IAppDbContext db, IExcelReader reader)
    : IRequestHandler<ImportOpeningBalancesCommand, Result<ImportResult>>
{
    private const string SheetName = "الأرصدة الافتتاحية";

    private static class Col
    {
        public const string ItemCode = "كود الصنف";
        public const string WarehouseCode = "كود المخزن";
        public const string LocationCode = "كود الموقع";
        public const string Batch = "رقم الدفعة";
        public const string Serial = "رقم السيريال";
        public const string Expiry = "تاريخ الصلاحية";
        public const string Qty = "الكمية";
        public const string Cost = "تكلفة الوحدة";
    }

    private static readonly string[] RequiredHeaders =
        [Col.ItemCode, Col.WarehouseCode, Col.Qty, Col.Cost];

    public async Task<Result<ImportResult>> Handle(ImportOpeningBalancesCommand request, CancellationToken cancellationToken)
    {
        var sheet = reader.ReadSheet(request.Content, SheetName);
        if (sheet is null)
            return new ImportResult(0, 0, 0, false,
                [new ImportRowError(0, SheetName, $"التبويب «{SheetName}» غير موجود في الملف.")]);

        var structural = RequiredHeaders
            .Where(h => !sheet.Headers.Contains(h))
            .Select(h => new ImportRowError(1, h, $"عمود إلزامي مفقود: «{h}»."))
            .ToList();
        if (structural.Count > 0)
            return new ImportResult(sheet.Rows.Count, 0, 0, false, structural);

        // ── مراجع ──
        var items = await db.Items
            .Select(i => new { i.Id, i.ItemCode, i.TracksBatch, i.TracksSerial, i.TracksExpiry, i.IsActive })
            .ToListAsync(cancellationToken);
        var itemByCode = items.ToDictionary(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

        var warehouses = await db.Warehouses
            .Select(w => new { w.Id, w.Code, w.UsesLocations, w.IsActive })
            .ToListAsync(cancellationToken);
        var warehouseByCode = warehouses.ToDictionary(w => w.Code, StringComparer.OrdinalIgnoreCase);

        var locations = await db.WarehouseLocations
            .Select(l => new { l.Id, l.WarehouseId, l.Code })
            .ToListAsync(cancellationToken);

        var existingCombos = (await db.StockBalances
            .Select(s => new { s.ItemId, s.WarehouseId, s.LocationId, s.BatchNo, s.SerialNo })
            .ToListAsync(cancellationToken))
            .Select(ComboKey).ToHashSet();

        var errors = new List<ImportRowError>();
        var seenCombos = new HashSet<string>();
        var toInsert = new List<StockBalance>();
        var wacAccumulator = new Dictionary<Guid, (decimal Qty, decimal Value)>();
        var validRows = 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var row in sheet.Rows)
        {
            if (row.Cells.Values.All(string.IsNullOrWhiteSpace)) continue;

            var rowErrors = new List<ImportRowError>();

            var itemCode = row.Get(Col.ItemCode);
            var whCode = row.Get(Col.WarehouseCode);
            var locCode = row.Get(Col.LocationCode);
            var batch = row.Get(Col.Batch);
            var serial = row.Get(Col.Serial);

            itemByCode.TryGetValue(itemCode, out var item);
            if (item is null) rowErrors.Add(new(row.RowNumber, Col.ItemCode, $"الصنف «{itemCode}» غير موجود."));
            else if (!item.IsActive) rowErrors.Add(new(row.RowNumber, Col.ItemCode, "الصنف غير نشط."));

            warehouseByCode.TryGetValue(whCode, out var warehouse);
            if (warehouse is null) rowErrors.Add(new(row.RowNumber, Col.WarehouseCode, $"المخزن «{whCode}» غير موجود."));
            else if (!warehouse.IsActive) rowErrors.Add(new(row.RowNumber, Col.WarehouseCode, "المخزن غير نشط."));

            // الموقع: إلزامي إذا المخزن يستخدم مواقع.
            Guid? locationId = null;
            if (warehouse is not null && warehouse.UsesLocations)
            {
                if (string.IsNullOrWhiteSpace(locCode))
                    rowErrors.Add(new(row.RowNumber, Col.LocationCode, "المخزن يستخدم مواقع؛ كود الموقع مطلوب."));
                else
                {
                    var loc = locations.FirstOrDefault(l => l.WarehouseId == warehouse.Id
                        && l.Code.Equals(locCode, StringComparison.OrdinalIgnoreCase));
                    if (loc is null) rowErrors.Add(new(row.RowNumber, Col.LocationCode, $"الموقع «{locCode}» غير موجود في المخزن."));
                    else locationId = loc.Id;
                }
            }

            // أبعاد التتبّع حسب الصنف (BR-04).
            if (item is not null)
            {
                if (item.TracksBatch && string.IsNullOrWhiteSpace(batch))
                    rowErrors.Add(new(row.RowNumber, Col.Batch, "الصنف يتتبّع الدُفعة؛ رقم الدُفعة مطلوب."));
                if (item.TracksSerial && string.IsNullOrWhiteSpace(serial))
                    rowErrors.Add(new(row.RowNumber, Col.Serial, "الصنف يتتبّع السيريال؛ رقم السيريال مطلوب."));
            }

            // تاريخ الصلاحية.
            DateOnly? expiry = null;
            var expiryText = row.Get(Col.Expiry);
            if (!string.IsNullOrWhiteSpace(expiryText))
            {
                if (TryParseDate(expiryText, out var d)) expiry = d;
                else rowErrors.Add(new(row.RowNumber, Col.Expiry, $"تاريخ الصلاحية «{expiryText}» غير صحيح (المتوقع yyyy-MM-dd)."));
            }
            if (item is { TracksExpiry: true })
            {
                if (expiry is null) rowErrors.Add(new(row.RowNumber, Col.Expiry, "الصنف يتتبّع الصلاحية؛ التاريخ مطلوب."));
                else if (expiry <= today) rowErrors.Add(new(row.RowNumber, Col.Expiry, "تاريخ الصلاحية يجب أن يكون مستقبلياً (R-06)."));
            }

            // الكمية > 0 (R-04) والتكلفة ≥ 0 (R-05).
            var qty = ParseDecimal(row.Get(Col.Qty), row.RowNumber, Col.Qty, rowErrors);
            if (qty is <= 0) rowErrors.Add(new(row.RowNumber, Col.Qty, "الكمية الافتتاحية يجب أن تكون أكبر من صفر."));
            var cost = ParseDecimal(row.Get(Col.Cost), row.RowNumber, Col.Cost, rowErrors);
            if (cost is < 0) rowErrors.Add(new(row.RowNumber, Col.Cost, "تكلفة الوحدة لا يمكن أن تكون سالبة."));

            if (item is not null && item.TracksSerial && qty is > 1)
                rowErrors.Add(new(row.RowNumber, Col.Qty, "الأصناف المتتبَّعة بسيريال: الكمية = 1 لكل سطر."));

            // تفرّد التركيبة (R-01 على مستوى الرصيد).
            if (item is not null && warehouse is not null)
            {
                var key = ComboKey(new { ItemId = item.Id, WarehouseId = warehouse.Id, LocationId = locationId,
                    BatchNo = Nullable(batch), SerialNo = Nullable(serial) });
                if (!seenCombos.Add(key)) rowErrors.Add(new(row.RowNumber, Col.ItemCode, "تركيبة (صنف/مخزن/موقع/دُفعة/سيريال) مكررة داخل الملف."));
                if (existingCombos.Contains(key)) rowErrors.Add(new(row.RowNumber, Col.ItemCode, "يوجد رصيد لهذه التركيبة مسبقاً."));
            }

            if (rowErrors.Count > 0) { errors.AddRange(rowErrors); continue; }

            validRows++;
            toInsert.Add(new StockBalance
            {
                ItemId = item!.Id,
                WarehouseId = warehouse!.Id,
                LocationId = locationId,
                BatchNo = Nullable(batch),
                SerialNo = Nullable(serial),
                ExpiryDate = expiry,
                QtyOnHand = qty!.Value,
                QtyReserved = 0,
                AvgCost = cost!.Value,
            });

            var acc = wacAccumulator.GetValueOrDefault(item.Id);
            wacAccumulator[item.Id] = (acc.Qty + qty.Value, acc.Value + qty.Value * cost.Value);
        }

        var totalDataRows = sheet.Rows.Count(r => !r.Cells.Values.All(string.IsNullOrWhiteSpace));

        var committed = false;
        var importedCount = 0;
        if (request.Commit && errors.Count == 0 && toInsert.Count > 0)
        {
            db.StockBalances.AddRange(toInsert);

            // تأسيس متوسط التكلفة المرجّح لكل صنف (BR-10).
            var affectedItemIds = wacAccumulator.Keys.ToList();
            var itemsToUpdate = await db.Items.Where(i => affectedItemIds.Contains(i.Id)).ToListAsync(cancellationToken);
            foreach (var itm in itemsToUpdate)
            {
                var (q, v) = wacAccumulator[itm.Id];
                if (q > 0) itm.WeightedAvgCost = decimal.Round(v / q, 4);
            }

            await db.SaveChangesAsync(cancellationToken);
            committed = true;
            importedCount = toInsert.Count;
        }

        return new ImportResult(totalDataRows, validRows, importedCount, committed, errors);
    }

    private static string? Nullable(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ComboKey(dynamic c)
        => $"{c.ItemId}|{c.WarehouseId}|{c.LocationId}|{c.BatchNo}|{c.SerialNo}";

    private static decimal? ParseDecimal(string value, int rowNumber, string column, List<ImportRowError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) { errors.Add(new(rowNumber, column, "القيمة مطلوبة.")); return null; }
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
        errors.Add(new(rowNumber, column, $"القيمة «{value}» ليست رقماً صحيحاً."));
        return null;
    }

    private static bool TryParseDate(string value, out DateOnly date)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
           || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
