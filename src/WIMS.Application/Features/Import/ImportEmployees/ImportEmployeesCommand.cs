using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Employees;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Import.ImportEmployees;

/// <summary>يستورد الموظفين من تبويب «الموظفون». Commit=false معاينة، true اعتماد ذرّي.</summary>
public sealed record ImportEmployeesCommand(byte[] Content, bool Commit) : ICommand<Result<ImportResult>>;

public sealed class ImportEmployeesCommandHandler(IAppDbContext db, IExcelReader reader)
    : IRequestHandler<ImportEmployeesCommand, Result<ImportResult>>
{
    private const string SheetName = "الموظفون";

    private static class Col
    {
        public const string No = "الرقم الوظيفي";
        public const string NationalId = "الرقم القومي";
        public const string Name = "الاسم";
        public const string Dept = "الإدارة";
        public const string Email = "البريد";
        public const string JobTitle = "المسمى الوظيفي";
        public const string Status = "الحالة";
    }

    private static readonly string[] RequiredHeaders = [Col.No, Col.NationalId, Col.Name, Col.Dept];

    private static readonly Dictionary<string, EmployeeStatus> StatusMap = new()
    {
        ["نشط"] = EmployeeStatus.Active,
        ["موقوف"] = EmployeeStatus.Suspended,
        ["منقول"] = EmployeeStatus.Transferred,
    };

    public async Task<Result<ImportResult>> Handle(ImportEmployeesCommand request, CancellationToken ct)
    {
        var sheet = reader.ReadSheet(request.Content, SheetName);
        if (sheet is null)
            return new ImportResult(0, 0, 0, false,
                [new ImportRowError(0, SheetName, $"التبويب «{SheetName}» غير موجود.")]);

        var structural = RequiredHeaders.Where(h => !sheet.Headers.Contains(h))
            .Select(h => new ImportRowError(1, h, $"عمود إلزامي مفقود: «{h}».")).ToList();
        if (structural.Count > 0)
            return new ImportResult(sheet.Rows.Count, 0, 0, false, structural);

        var existingNos = (await db.Employees.Select(e => e.EmployeeNo).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingNids = (await db.Employees.Select(e => e.NationalId).ToListAsync(ct)).ToHashSet();

        var errors = new List<ImportRowError>();
        var seenNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNid = new HashSet<string>();
        var toInsert = new List<Employee>();
        var valid = 0;

        foreach (var row in sheet.Rows)
        {
            if (row.Cells.Values.All(string.IsNullOrWhiteSpace)) continue;
            var rowErrors = new List<ImportRowError>();

            var no = row.Get(Col.No);
            var nid = row.Get(Col.NationalId);
            var name = row.Get(Col.Name);
            var dept = row.Get(Col.Dept);

            if (string.IsNullOrWhiteSpace(no)) rowErrors.Add(new(row.RowNumber, Col.No, "الرقم الوظيفي مطلوب."));
            else { if (!seenNo.Add(no)) rowErrors.Add(new(row.RowNumber, Col.No, "الرقم الوظيفي مكرر في الملف.")); if (existingNos.Contains(no)) rowErrors.Add(new(row.RowNumber, Col.No, "الرقم الوظيفي موجود مسبقاً.")); }

            if (string.IsNullOrWhiteSpace(nid) || nid.Length != 14 || !nid.All(char.IsDigit))
                rowErrors.Add(new(row.RowNumber, Col.NationalId, "الرقم القومي يجب أن يكون 14 رقمًا."));
            else { if (!seenNid.Add(nid)) rowErrors.Add(new(row.RowNumber, Col.NationalId, "الرقم القومي مكرر في الملف.")); if (existingNids.Contains(nid)) rowErrors.Add(new(row.RowNumber, Col.NationalId, "الرقم القومي موجود مسبقاً.")); }

            if (string.IsNullOrWhiteSpace(name)) rowErrors.Add(new(row.RowNumber, Col.Name, "اسم الموظف مطلوب."));
            if (string.IsNullOrWhiteSpace(dept)) rowErrors.Add(new(row.RowNumber, Col.Dept, "الإدارة مطلوبة."));

            var statusText = row.Get(Col.Status);
            var status = EmployeeStatus.Active;
            if (!string.IsNullOrWhiteSpace(statusText) && !StatusMap.TryGetValue(statusText, out status))
                rowErrors.Add(new(row.RowNumber, Col.Status, $"الحالة «{statusText}» غير صحيحة."));

            if (rowErrors.Count > 0) { errors.AddRange(rowErrors); continue; }

            valid++;
            toInsert.Add(new Employee
            {
                EmployeeNo = no, NationalId = nid, FullNameAr = name, Department = dept,
                Email = string.IsNullOrWhiteSpace(row.Get(Col.Email)) ? null : row.Get(Col.Email),
                JobTitle = string.IsNullOrWhiteSpace(row.Get(Col.JobTitle)) ? null : row.Get(Col.JobTitle),
                Status = status,
            });
        }

        var totalRows = sheet.Rows.Count(r => !r.Cells.Values.All(string.IsNullOrWhiteSpace));
        var committed = false; var imported = 0;
        if (request.Commit && errors.Count == 0 && toInsert.Count > 0)
        {
            db.Employees.AddRange(toInsert);
            await db.SaveChangesAsync(ct);
            committed = true; imported = toInsert.Count;
        }

        return new ImportResult(totalRows, valid, imported, committed, errors);
    }
}
