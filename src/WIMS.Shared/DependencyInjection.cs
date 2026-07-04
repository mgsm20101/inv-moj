using Microsoft.Extensions.DependencyInjection;
using WIMS.Shared.Mapping;

namespace WIMS.Shared;

/// <summary>تسجيل خدمات طبقة Shared في حاوية الاعتماديات.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        // تسجيل SimpleMapper بديلاً عن AutoMapper.
        services.AddSingleton<ISimpleMapper, SimpleMapper>();
        return services;
    }
}
