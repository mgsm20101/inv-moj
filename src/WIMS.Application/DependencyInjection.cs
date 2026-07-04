using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WIMS.Application.Common.Behaviors;
using WIMS.Application.Features.Alerts;
using WIMS.Application.Features.Approvals;
using WIMS.Application.Features.Custody;
using WIMS.Application.Features.Transactions.Posting;
using WIMS.Shared;

namespace WIMS.Application;

/// <summary>تسجيل خدمات طبقة Application: MediatR + Behaviors + FluentValidation + SimpleMapper.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddShared();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // ترتيب مهم: تحقق ← تسجيل ← تدقيق.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IVoucherPostingService, VoucherPostingService>();
        services.AddScoped<ICustodyProvisioningService, CustodyProvisioningService>();
        services.AddScoped<IApprovalRoutingService, ApprovalRoutingService>();
        services.AddScoped<IAlertScanner, AlertScanner>();

        return services;
    }
}
