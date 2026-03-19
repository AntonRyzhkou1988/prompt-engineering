using Microsoft.Extensions.DependencyInjection;

namespace PromptEngineering.Services.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddPromptEngineeringServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        

        return services;
    }
}
