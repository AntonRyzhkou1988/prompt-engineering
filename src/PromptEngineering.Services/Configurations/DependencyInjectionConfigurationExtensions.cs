using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PromptEngineering.Services.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddPromptEngineeringServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ContextSettings>(configuration.GetSection(nameof(ContextSettings)));

        return services;
    }
}
