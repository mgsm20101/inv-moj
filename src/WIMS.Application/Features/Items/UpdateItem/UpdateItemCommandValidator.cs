using FluentValidation;

namespace WIMS.Application.Features.Items.UpdateItem;

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الصنف بالعربي مطلوب.").MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Barcode).MaximumLength(50);

        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .GreaterThanOrEqualTo(x => x.MinStock)
            .WithMessage("نقطة إعادة الطلب يجب ألا تقل عن الحد الأدنى.");
        RuleFor(x => x.MaxStock)
            .GreaterThanOrEqualTo(x => x.ReorderPoint)
            .When(x => x.MaxStock.HasValue)
            .WithMessage("الحد الأقصى يجب ألا يقل عن نقطة إعادة الطلب.");
    }
}
