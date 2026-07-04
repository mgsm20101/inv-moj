using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;

namespace WIMS.Application.Features.Admin.Permissions;

public sealed record PermissionDto(string Key, string Name, string Module);

/// <summary>كل الصلاحيات المعرّفة (مرتّبة حسب الوحدة) — تغذّي مصفوفة اختيار صلاحيات الدور.</summary>
public sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;

public sealed class GetPermissionsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken ct)
        => await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Key)
            .Select(p => new PermissionDto(p.Key, p.Name, p.Module))
            .ToListAsync(ct);
}
