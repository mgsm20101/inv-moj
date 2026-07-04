using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Catalog;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Categories.CreateCategory;

/// <summary>ينشئ تصنيفاً (جذراً أو فرعاً). يحسب المستوى والمسار آلياً.</summary>
public sealed record CreateCategoryCommand : ICommand<Result<Guid>>
{
    public string Code { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public Guid? ParentId { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("كود التصنيف مطلوب.").MaximumLength(10);
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم التصنيف مطلوب.").MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
    }
}

public sealed class CreateCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (await db.ItemCategories.AnyAsync(c => c.Code == code, cancellationToken))
            return Error.Conflict("Category.Code", $"كود التصنيف '{code}' مستخدم مسبقاً.");

        byte level = 1;
        var path = code;

        if (request.ParentId.HasValue)
        {
            var parent = await db.ItemCategories
                .FirstOrDefaultAsync(c => c.Id == request.ParentId.Value, cancellationToken);
            if (parent is null)
                return Error.NotFound("Category.Parent", "التصنيف الأب غير موجود.");

            level = (byte)(parent.Level + 1);
            path = $"{parent.Path}/{code}";
        }

        var category = new ItemCategory
        {
            Code = code,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn?.Trim(),
            ParentId = request.ParentId,
            Level = level,
            Path = path,
            SortOrder = request.SortOrder,
            IsActive = true,
        };

        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
