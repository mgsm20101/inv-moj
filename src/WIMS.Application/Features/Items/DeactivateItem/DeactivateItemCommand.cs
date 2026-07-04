using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.DeactivateItem;

/// <summary>يعطّل صنفاً (BR-08) — لا حذف، فقط IsActive=false.</summary>
public sealed record DeactivateItemCommand(Guid Id) : ICommand<Result>;

public sealed class DeactivateItemCommandHandler(IAppDbContext db)
    : IRequestHandler<DeactivateItemCommand, Result>
{
    public async Task<Result> Handle(DeactivateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item.NotFound", "الصنف غير موجود."));

        item.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
