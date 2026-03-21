using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PromptEngineering.Services;

namespace PromptEngineering.Services.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddPromptEngineeringServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ContextSettings>(configuration.GetSection(nameof(ContextSettings)));
        services.AddOptions<ContextPromptsOptions>();
        services.AddSingleton<IPostConfigureOptions<ContextPromptsOptions>, ContextPromptsPostConfigure>();

        return services;
    }
}
