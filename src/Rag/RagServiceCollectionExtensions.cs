using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rag;

public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddRag(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RagSettings>()
            .Bind(configuration.GetSection("Rag"))
            .Validate(settings =>
            {
                settings.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddSingleton<RagOrchestrator>();
        return services;
    }
}
