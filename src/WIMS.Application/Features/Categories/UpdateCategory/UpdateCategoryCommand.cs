using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Categories.UpdateCategory;

/// <summary>يعدّل بيانات تصنيف. الكود والأب ثابتان (تغيير الأب يُعيد حساب المستوى/المسار — خارج النطاق).</summary>
public sealed record UpdateCategoryCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public int SortOrder { get; init; }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم التصنيف مطلوب.").MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
    }
}

public sealed class UpdateCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ItemCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound("Category", "التصنيف غير موجود."));

        category.NameAr = request.NameAr.Trim();
        category.NameEn = request.NameEn?.Trim();
        category.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
