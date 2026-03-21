using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.Services;
using PromptEngineering.Services.Configurations;

namespace PromptEngineering.Client.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static ServiceProvider BuildPromptEngineeringClientServiceProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddPromptEngineeringClient();
        return services.BuildServiceProvider();
    }

    public static IServiceCollection AddPromptEngineeringClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = BuildConfiguration();

        services.AddSingleton(configuration);
        services.AddPromptEngineeringServices(configuration);
        services.AddGenAi(configuration);
        services.AddSingleton<IContextService, ContextService>();
        services.AddSingleton<IPromptService, PromptService>();

        return services;
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .Build();
    }
}
