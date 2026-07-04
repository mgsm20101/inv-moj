using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Reports;

/// <summary>تنسيق موحّد لأرقام وتواريخ التقارير (أرقام لاتينية بفواصل آلاف).</summary>
internal static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Qty(decimal v) => v.ToString("#,##0.###", Inv);
    public static string Money(decimal v) => v.ToString("#,##0.00", Inv);
    public static string Date(DateTime v) => v.ToString("yyyy-MM-dd", Inv);
    public static string Date(DateOnly v) => v.ToString("yyyy-MM-dd", Inv);
    public static string DateTimeMin(DateTime v) => v.ToString("yyyy-MM-dd HH:mm", Inv);
}

// ═══════════════════════════════════════════════════════════════════════
//  1) تقرير رصيد المخزون (FR-RPT-01)
// ═══════════════════════════════════════════════════════════════════════
public sealed record StockBalanceReportQuery(Guid? WarehouseId, bool OnlyInStock = true)
    : IQuery<Result<ReportDocument>>;

public sealed class StockBalanceReportQueryHandler(IAppDbContext db)
    : IRequestHandler<StockBalanceReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(StockBalanceReportQuery request, CancellationToken ct)
    {
        var q = db.StockBalances.AsNoTracking();
        if (request.WarehouseId is { } wid) q = q.Where(b => b.WarehouseId == wid);
        if (request.OnlyInStock) q = q.Where(b => b.QtyOnHand > 0);

        var data = await q
            .OrderBy(b => b.Item.ItemCode).ThenBy(b => b.Warehouse.Code)
            .Select(b => new
            {
                b.Item.ItemCode,
                b.Item.NameAr,
                WhName = b.Warehouse.NameAr,
                Unit = b.Item.BaseUnit.Code,
                b.BatchNo,
                b.QtyOnHand,
                b.AvgCost,
            })
            .ToListAsync(ct);

        var rows = data.Select(d => (IReadOnlyList<string>)new[]
        {
            d.ItemCode, d.NameAr, d.WhName, d.Unit, d.BatchNo ?? "-",
            Fmt.Qty(d.QtyOnHand), Fmt.Money(d.AvgCost), Fmt.Money(d.QtyOnHand * d.AvgCost),
        }).ToList();

        var totalValue = data.Sum(d => d.QtyOnHand * d.AvgCost);

        return new ReportDocument
        {
            Title = "تقرير رصيد المخزون",
            Subtitle = await WarehouseSubtitleAsync(db, request.WarehouseId, ct),
            Meta = [new("عدد الأسطر", data.Count.ToString(CultureInfo.InvariantCulture))],
            Columns =
            [
                new("كود الصنف"), new("اسم الصنف", ReportAlignment.Right, 2.2f), new("المخزن"),
                new("الوحدة", ReportAlignment.Center), new("الدُفعة", ReportAlignment.Center),
                new("الرصيد", ReportAlignment.Center), new("تكلفة الوحدة", ReportAlignment.Center),
                new("القيمة", ReportAlignment.Center),
            ],
            Rows = rows,
            Totals = ["الإجمالي", "", "", "", "", "", "", Fmt.Money(totalValue)],
            GeneratedAt = DateTime.Now,
        };
    }

    internal static async Task<string?> WarehouseSubtitleAsync(IAppDbContext db, Guid? warehouseId, CancellationToken ct)
    {
        if (warehouseId is not { } wid) return "كل المخازن";
        var name = await db.Warehouses.AsNoTracking().Where(w => w.Id == wid)
            .Select(w => w.NameAr).FirstOrDefaultAsync(ct);
        return name is null ? null : $"المخزن: {name}";
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  2) كارت الصنف — دفتر الحركة (FR-RPT-02)
// ═══════════════════════════════════════════════════════════════════════
public sealed record ItemCardReportQuery(Guid ItemId, Guid? WarehouseId, DateTime? From, DateTime? To)
    : IQuery<Result<ReportDocument>>;

public sealed class ItemCardReportQueryHandler(IAppDbContext db)
    : IRequestHandler<ItemCardReportQuery, Result<ReportDocument>>
{
    private static readonly Dictionary<StockTxnType, string> TxnNames = new()
    {
        [StockTxnType.Receipt] = "استلام",
        [StockTxnType.Issue] = "صرف",
        [StockTxnType.TransferOut] = "تحويل صادر",
        [StockTxnType.TransferIn] = "تحويل وارد",
        [StockTxnType.ReturnIn] = "مرتجع وارد",
        [StockTxnType.ReturnOut] = "مرتجع صادر",
        [StockTxnType.AdjustIncrease] = "تسوية زيادة",
        [StockTxnType.AdjustDecrease] = "تسوية عجز",
        [StockTxnType.Reversal] = "حركة عكسية",
    };

    public async Task<Result<ReportDocument>> Handle(ItemCardReportQuery request, CancellationToken ct)
    {
        var item = await db.Items.AsNoTracking().Where(i => i.Id == request.ItemId)
            .Select(i => new { i.ItemCode, i.NameAr }).FirstOrDefaultAsync(ct);
        if (item is null)
            return Error.NotFound("Report.Item", "الصنف غير موجود.");

        var q = db.StockTransactions.AsNoTracking().Where(t => t.ItemId == request.ItemId);
        if (request.WarehouseId is { } wid) q = q.Where(t => t.WarehouseId == wid);
        if (request.From is { } from) q = q.Where(t => t.PostedAt >= from);
        if (request.To is { } to) q = q.Where(t => t.PostedAt < to.Date.AddDays(1));

        var data = await q
            .OrderBy(t => t.TransactionNo)
            .Select(t => new
            {
                t.PostedAt, t.TransactionNo, t.TxnType, t.Direction,
                VoucherNo = t.Voucher.VoucherNo, WhName = t.Warehouse.NameAr,
                t.Qty, t.QtyOnHandAfter, t.WacAfter,
            })
            .ToListAsync(ct);

        var rows = data.Select(d => (IReadOnlyList<string>)new[]
        {
            Fmt.Date(d.PostedAt),
            d.TransactionNo.ToString(CultureInfo.InvariantCulture),
            TxnNames.GetValueOrDefault(d.TxnType, d.TxnType.ToString()),
            d.VoucherNo,
            d.WhName,
            d.Direction > 0 ? Fmt.Qty(d.Qty) : "",
            d.Direction < 0 ? Fmt.Qty(d.Qty) : "",
            Fmt.Qty(d.QtyOnHandAfter),
            Fmt.Money(d.WacAfter),
        }).ToList();

        return new ReportDocument
        {
            Title = "كارت الصنف",
            Subtitle = $"{item.ItemCode} — {item.NameAr}",
            Meta =
            [
                new("عدد الحركات", data.Count.ToString(CultureInfo.InvariantCulture)),
                new("الفترة", $"{(request.From is { } f ? Fmt.Date(f) : "البداية")} — {(request.To is { } t ? Fmt.Date(t) : "الآن")}"),
            ],
            Columns =
            [
                new("التاريخ", ReportAlignment.Center), new("رقم الحركة", ReportAlignment.Center),
                new("نوع الحركة"), new("المستند"), new("المخزن"),
                new("وارد", ReportAlignment.Center), new("منصرف", ReportAlignment.Center),
                new("الرصيد بعد", ReportAlignment.Center), new("التكلفة بعد", ReportAlignment.Center),
            ],
            Rows = rows,
            GeneratedAt = DateTime.Now,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  3) تقرير الأصناف الراكدة (FR-RPT-03)
// ═══════════════════════════════════════════════════════════════════════
public sealed record StagnantReportQuery(int Days, Guid? WarehouseId) : IQuery<Result<ReportDocument>>;

public sealed class StagnantReportQueryHandler(IAppDbContext db)
    : IRequestHandler<StagnantReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(StagnantReportQuery request, CancellationToken ct)
    {
        var days = request.Days <= 0 ? 90 : request.Days;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var stockQ = db.StockBalances.AsNoTracking().Where(b => b.QtyOnHand > 0);
        if (request.WarehouseId is { } wid) stockQ = stockQ.Where(b => b.WarehouseId == wid);

        var stock = await stockQ
            .GroupBy(b => new { b.ItemId, b.Item.ItemCode, b.Item.NameAr })
            .Select(g => new
            {
                g.Key.ItemId, g.Key.ItemCode, g.Key.NameAr,
                Qty = g.Sum(x => x.QtyOnHand),
                Value = g.Sum(x => x.QtyOnHand * x.AvgCost),
            })
            .ToListAsync(ct);

        if (stock.Count == 0)
            return Empty(days, request.WarehouseId);

        var itemIds = stock.Select(s => s.ItemId).ToList();
        var txnQ = db.StockTransactions.AsNoTracking().Where(t => itemIds.Contains(t.ItemId));
        if (request.WarehouseId is { } wid2) txnQ = txnQ.Where(t => t.WarehouseId == wid2);

        var lastMove = await txnQ
            .GroupBy(t => t.ItemId)
            .Select(g => new { ItemId = g.Key, Last = g.Max(x => x.PostedAt) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Last, ct);

        var now = DateTime.UtcNow;
        var stagnant = stock
            .Select(s => new { s, Last = lastMove.TryGetValue(s.ItemId, out var l) ? (DateTime?)l : null })
            .Where(x => x.Last is null || x.Last < cutoff)
            .OrderByDescending(x => x.Last is null ? int.MaxValue : (int)(now - x.Last.Value).TotalDays)
            .ToList();

        var rows = stagnant.Select(x => (IReadOnlyList<string>)new[]
        {
            x.s.ItemCode, x.s.NameAr,
            Fmt.Qty(x.s.Qty),
            Fmt.Money(x.s.Value),
            x.Last is { } l ? Fmt.Date(l) : "لا حركة",
            x.Last is { } l2 ? ((int)(now - l2).TotalDays).ToString(CultureInfo.InvariantCulture) : "—",
        }).ToList();

        return new ReportDocument
        {
            Title = "تقرير الأصناف الراكدة",
            Subtitle = await StockBalanceReportQueryHandler.WarehouseSubtitleAsync(db, request.WarehouseId, ct),
            Meta =
            [
                new("عتبة الركود", $"{days} يوم"),
                new("عدد الأصناف الراكدة", stagnant.Count.ToString(CultureInfo.InvariantCulture)),
            ],
            Columns =
            [
                new("كود الصنف"), new("اسم الصنف", ReportAlignment.Right, 2.5f),
                new("الرصيد", ReportAlignment.Center), new("القيمة", ReportAlignment.Center),
                new("آخر حركة", ReportAlignment.Center), new("أيام الركود", ReportAlignment.Center),
            ],
            Rows = rows,
            Totals = ["الإجمالي", "", "", Fmt.Money(stagnant.Sum(x => x.s.Value)), "", ""],
            GeneratedAt = DateTime.Now,
        };
    }

    private static ReportDocument Empty(int days, Guid? warehouseId) => new()
    {
        Title = "تقرير الأصناف الراكدة",
        Subtitle = warehouseId is null ? "كل المخازن" : null,
        Meta = [new("عتبة الركود", $"{days} يوم"), new("عدد الأصناف الراكدة", "0")],
        Columns =
        [
            new("كود الصنف"), new("اسم الصنف", ReportAlignment.Right, 2.5f),
            new("الرصيد", ReportAlignment.Center), new("القيمة", ReportAlignment.Center),
            new("آخر حركة", ReportAlignment.Center), new("أيام الركود", ReportAlignment.Center),
        ],
        Rows = [],
        GeneratedAt = DateTime.Now,
    };
}

// ═══════════════════════════════════════════════════════════════════════
//  4) تقرير الأصناف تحت الحد الأدنى (FR-RPT-04 / FR-REO)
// ═══════════════════════════════════════════════════════════════════════
public sealed record BelowMinReportQuery(Guid? WarehouseId) : IQuery<Result<ReportDocument>>;

public sealed class BelowMinReportQueryHandler(IAppDbContext db)
    : IRequestHandler<BelowMinReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(BelowMinReportQuery request, CancellationToken ct)
    {
        // الرصيد الإجمالي لكل صنف (اختيارياً ضمن مخزن محدّد).
        var balQ = db.StockBalances.AsNoTracking();
        if (request.WarehouseId is { } wid) balQ = balQ.Where(b => b.WarehouseId == wid);

        var onHand = await balQ
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.QtyOnHand) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty, ct);

        var items = await db.Items.AsNoTracking()
            .Where(i => i.IsActive && i.IsStockItem && i.MinStock > 0)
            .OrderBy(i => i.ItemCode)
            .Select(i => new { i.Id, i.ItemCode, i.NameAr, i.MinStock, i.ReorderPoint, i.ReorderQty })
            .ToListAsync(ct);

        var rows = new List<IReadOnlyList<string>>();
        foreach (var i in items)
        {
            var qty = onHand.GetValueOrDefault(i.Id);
            if (qty >= i.MinStock) continue;

            var shortage = i.MinStock - qty;
            var suggested = i.ReorderQty is { } rq && rq > 0 ? rq : shortage;
            rows.Add(new[]
            {
                i.ItemCode, i.NameAr,
                Fmt.Qty(qty), Fmt.Qty(i.MinStock), Fmt.Qty(i.ReorderPoint),
                Fmt.Qty(shortage), Fmt.Qty(suggested),
            });
        }

        return new ReportDocument
        {
            Title = "تقرير الأصناف تحت الحد الأدنى",
            Subtitle = await StockBalanceReportQueryHandler.WarehouseSubtitleAsync(db, request.WarehouseId, ct),
            Meta = [new("عدد الأصناف", rows.Count.ToString(CultureInfo.InvariantCulture))],
            Columns =
            [
                new("كود الصنف"), new("اسم الصنف", ReportAlignment.Right, 2.5f),
                new("الرصيد", ReportAlignment.Center), new("الحد الأدنى", ReportAlignment.Center),
                new("نقطة الطلب", ReportAlignment.Center), new("العجز", ReportAlignment.Center),
                new("الكمية المقترحة", ReportAlignment.Center),
            ],
            Rows = rows,
            GeneratedAt = DateTime.Now,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  5) تقرير العُهد (FR-RPT-05)
// ═══════════════════════════════════════════════════════════════════════
public sealed record CustodyReportQuery(Guid? EmployeeId) : IQuery<Result<ReportDocument>>;

public sealed class CustodyReportQueryHandler(IAppDbContext db)
    : IRequestHandler<CustodyReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(CustodyReportQuery request, CancellationToken ct)
    {
        var q = db.CustodyItems.AsNoTracking()
            .Where(ci => ci.Status == CustodyItemStatus.InCustody
                      && ci.Custody.Status == CustodyStatus.Active
                      && ci.Custody.CustodyType == CustodyType.Personal);
        if (request.EmployeeId is { } eid) q = q.Where(ci => ci.Custody.EmployeeId == eid);

        var data = await q
            .OrderBy(ci => ci.Custody.Employee!.EmployeeNo).ThenBy(ci => ci.Item.ItemCode)
            .Select(ci => new
            {
                EmpNo = ci.Custody.Employee!.EmployeeNo,
                EmpName = ci.Custody.Employee!.FullNameAr,
                Dept = ci.Custody.Employee!.Department,
                ci.Item.ItemCode,
                ItemName = ci.Item.NameAr,
                ci.SerialNo,
                ci.Qty,
                ci.UnitCost,
                ci.AssignedAt,
            })
            .ToListAsync(ct);

        var rows = data.Select(d => (IReadOnlyList<string>)new[]
        {
            d.EmpNo, d.EmpName, d.Dept, d.ItemCode, d.ItemName,
            d.SerialNo ?? "-", Fmt.Qty(d.Qty), Fmt.Money(d.UnitCost),
            Fmt.Money(d.Qty * d.UnitCost), Fmt.Date(d.AssignedAt),
        }).ToList();

        string? subtitle = null;
        if (request.EmployeeId is { } id)
        {
            var emp = await db.Employees.AsNoTracking().Where(e => e.Id == id)
                .Select(e => new { e.EmployeeNo, e.FullNameAr }).FirstOrDefaultAsync(ct);
            subtitle = emp is null ? null : $"الموظف: {emp.EmployeeNo} — {emp.FullNameAr}";
        }

        return new ReportDocument
        {
            Title = "تقرير العُهد الشخصية",
            Subtitle = subtitle ?? "كل الموظفين",
            Meta = [new("عدد البنود", data.Count.ToString(CultureInfo.InvariantCulture))],
            Columns =
            [
                new("رقم الموظف"), new("اسم الموظف", ReportAlignment.Right, 2f), new("الإدارة"),
                new("كود الصنف"), new("اسم الصنف", ReportAlignment.Right, 2f),
                new("السيريال", ReportAlignment.Center), new("الكمية", ReportAlignment.Center),
                new("تكلفة الوحدة", ReportAlignment.Center), new("القيمة", ReportAlignment.Center),
                new("تاريخ التسليم", ReportAlignment.Center),
            ],
            Rows = rows,
            Totals = ["الإجمالي", "", "", "", "", "", "", "", Fmt.Money(data.Sum(d => d.Qty * d.UnitCost)), ""],
            GeneratedAt = DateTime.Now,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  6) محضر الجرد (FR-RPT-06)
// ═══════════════════════════════════════════════════════════════════════
public sealed record StockCountMinutesReportQuery(Guid StockCountId) : IQuery<Result<ReportDocument>>;

public sealed class StockCountMinutesReportQueryHandler(IAppDbContext db)
    : IRequestHandler<StockCountMinutesReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(StockCountMinutesReportQuery request, CancellationToken ct)
    {
        var header = await db.StockCounts.AsNoTracking()
            .Where(c => c.Id == request.StockCountId)
            .Select(c => new
            {
                c.CountNo, c.CountType, c.Status, WhName = c.Warehouse.NameAr,
                c.FrozenAt, c.FrozenBy, c.CountedAt, c.ApprovedBy, c.ApprovedAt,
                c.AdjustmentVoucherNos, c.Notes,
            })
            .FirstOrDefaultAsync(ct);
        if (header is null)
            return Error.NotFound("Report.StockCount", "محضر الجرد غير موجود.");

        var lines = await db.StockCountLines.AsNoTracking()
            .Where(l => l.StockCountId == request.StockCountId)
            .OrderBy(l => l.LineNo)
            .Select(l => new
            {
                l.LineNo, l.Item.ItemCode, ItemName = l.Item.NameAr, l.BatchNo,
                l.BookQty, l.PhysicalQty, l.VarianceQty, l.UnitCost, l.VarianceValue,
            })
            .ToListAsync(ct);

        var rows = lines.Select(l => (IReadOnlyList<string>)new[]
        {
            l.LineNo.ToString(CultureInfo.InvariantCulture),
            l.ItemCode, l.ItemName, l.BatchNo ?? "-",
            Fmt.Qty(l.BookQty),
            l.PhysicalQty is { } p ? Fmt.Qty(p) : "—",
            Fmt.Qty(l.VarianceQty),
            Fmt.Money(l.UnitCost),
            Fmt.Money(l.VarianceValue),
        }).ToList();

        var typeName = header.CountType switch
        {
            StockCountType.Full => "جرد كامل",
            StockCountType.Partial => "جرد جزئي",
            StockCountType.Cyclic => "جرد دوري",
            _ => header.CountType.ToString(),
        };

        var meta = new List<ReportMeta>
        {
            new("نوع الجرد", typeName),
            new("المخزن", header.WhName),
            new("التجميد", header.FrozenAt is { } fa ? $"{Fmt.DateTimeMin(fa)} — {header.FrozenBy}" : "—"),
            new("الاعتماد", header.ApprovedAt is { } aa ? $"{Fmt.DateTimeMin(aa)} — {header.ApprovedBy}" : "—"),
        };
        if (!string.IsNullOrWhiteSpace(header.AdjustmentVoucherNos))
            meta.Add(new("سندات التسوية", header.AdjustmentVoucherNos));

        return new ReportDocument
        {
            Title = $"محضر جرد رقم {header.CountNo}",
            Subtitle = header.Notes,
            Meta = meta,
            Columns =
            [
                new("م", ReportAlignment.Center), new("كود الصنف"),
                new("اسم الصنف", ReportAlignment.Right, 2.5f), new("الدُفعة", ReportAlignment.Center),
                new("الرصيد الدفتري", ReportAlignment.Center), new("الفعلي", ReportAlignment.Center),
                new("الفرق", ReportAlignment.Center), new("تكلفة الوحدة", ReportAlignment.Center),
                new("قيمة الفرق", ReportAlignment.Center),
            ],
            Rows = rows,
            Totals = ["", "الإجمالي", "", "", "", "", Fmt.Qty(lines.Sum(l => l.VarianceQty)), "", Fmt.Money(lines.Sum(l => l.VarianceValue))],
            GeneratedAt = DateTime.Now,
        };
    }
}
