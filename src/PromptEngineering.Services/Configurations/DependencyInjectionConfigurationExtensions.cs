using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.LLM.Models;
using PromptEngineering.Services;

namespace PromptEngineering.Services.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddPromptEngineeringServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SystemSettings>(configuration.GetSection(nameof(SystemSettings)));
        services.Configure<ContextSettings>(configuration.GetSection(nameof(ContextSettings)));

        services.AddGenAi(configuration);
        services.AddSingleton<IContextService, ContextService>();

        return services;
    }
}
