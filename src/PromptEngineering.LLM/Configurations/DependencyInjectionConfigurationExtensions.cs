using PromptEngineering.LLM.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace PromptEngineering.LLM.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddGenAi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var aiSection = configuration
            .GetSection(nameof(SystemSettings))
            .GetSection(nameof(AiServiceSettings));
        var settings = aiSection.Get<AiServiceSettings>() ?? throw new ArgumentNullException(nameof(aiSection));

        ArgumentNullException.ThrowIfNull(aiSection);
        ArgumentNullException.ThrowIfNull(settings.BaseAddress);
        ArgumentNullException.ThrowIfNull(settings.Instances);
        ArgumentNullException.ThrowIfNull(settings.Retry);

        if (!settings.Instances.Any())
        {
            throw new ArgumentException("No instance to use.");
        }

        if (settings.Retry.RetryCount <= 0)
        {
            throw new ArgumentException("RetryCount should be greater than zero.");
        }

        if (settings.Retry.BackoffBase <= 1)
        {
            throw new ArgumentException("BackoffBase should be greater than one.");
        }

        foreach (var instance in settings.Instances)
        {
            ArgumentNullException.ThrowIfNull(instance.Name);
            ArgumentNullException.ThrowIfNull(instance.ApiKey);
            ArgumentNullException.ThrowIfNull(instance.Deployment);
        }

        services.Configure<AiServiceSettings>(aiSection);

        services.AddScoped<IAiService, AiService>();
        HttpClientSetUp(services, settings);

        return services;
    }

    private static void HttpClientSetUp(IServiceCollection services, AiServiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Cover Transient Errors
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(settings.Retry.RetryCount,
                    // Exponential backoff, e.g. 2, 4, 8 seconds for base 2.
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(settings.Retry.BackoffBase, retryAttempt))
                    );

        foreach (var instance in settings.Instances)
        {
            services
                .AddHttpClient(instance.Name, cfg =>
                {
                    cfg.BaseAddress =
                        new Uri(settings.BaseAddress);
                    cfg.Timeout = TimeSpan.FromSeconds(settings.TimeoutInSeconds);
                    cfg.DefaultRequestHeaders.Add("api-key", instance.ApiKey);
                })
                .SetHandlerLifetime(TimeSpan.FromSeconds(settings.HandlerLifetimeInSeconds))
                .AddPolicyHandler(retryPolicy);
        }
    }
}
