using FluentValidation;
using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Items.CreateItem;

/// <summary>تحقق شكلي (Shape) لقواعد الصنف — القواعد المرجعية (DB) تُفحص في الـ Handler.</summary>
public sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الصنف بالعربي مطلوب.").MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ItemCode).MaximumLength(20);
        RuleFor(x => x.Barcode).MaximumLength(50);

        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("التصنيف مطلوب.");
        RuleFor(x => x.BaseUnitId).NotEmpty().WithMessage("وحدة القياس مطلوبة.");
        RuleFor(x => x.ItemType).IsInEnum().WithMessage("نوع الصنف غير صحيح.");

        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStock).GreaterThanOrEqualTo(0).When(x => x.MaxStock.HasValue);

        // BR-07: MinStock ≤ ReorderPoint ≤ MaxStock.
        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(x => x.MinStock)
            .WithMessage("نقطة إعادة الطلب يجب ألا تقل عن الحد الأدنى.");
        RuleFor(x => x.MaxStock)
            .GreaterThanOrEqualTo(x => x.ReorderPoint)
            .When(x => x.MaxStock.HasValue)
            .WithMessage("الحد الأقصى يجب ألا يقل عن نقطة إعادة الطلب.");

        // BR-05: الصنف الخطر يتطلب تصنيف خطورة.
        RuleFor(x => x.HazardClass)
            .NotEmpty()
            .When(x => x.ItemType == ItemType.Hazardous)
            .WithMessage("الصنف الخطر يتطلب تصنيف خطورة (HazardClass).");

        // BR-06: القابل للتلف يتطلب عمراً افتراضياً.
        RuleFor(x => x.ShelfLifeDays)
            .NotNull().GreaterThan(0)
            .When(x => x.ItemType == ItemType.Perishable)
            .WithMessage("الصنف القابل للتلف يتطلب عمراً افتراضياً (بالأيام).");
    }
}
