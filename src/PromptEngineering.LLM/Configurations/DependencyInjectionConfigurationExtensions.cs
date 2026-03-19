using PromptEngineering.LLM.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace PromptEngineering.LLM.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection UseGenAi(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(AiServiceSettings));
        var settings = section.Get<AiServiceSettings>();

        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(settings);

        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.BaseAddress);
        ArgumentNullException.ThrowIfNull(settings.Instances);

        if (!settings.Instances.Any())
        {
            throw new ArgumentException("No instance to use.");
        }

        foreach (var instance in settings.Instances)
        {
            ArgumentNullException.ThrowIfNull(instance.Name);
            ArgumentNullException.ThrowIfNull(instance.ApiKey);
            ArgumentNullException.ThrowIfNull(instance.Deployment);
        }

        // Load settings from appsettings.json file
        services.Configure<AiServiceSettings>(section);

        services.AddScoped<IAiService, AiService>();
        HttpClientSetUp(services,
            services.BuildServiceProvider().GetRequiredService<IOptions<AiServiceSettings>>());

        return services;
    }

    private static void HttpClientSetUp(IServiceCollection services, IOptions<AiServiceSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Value);

        // Cover Transient Errors
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3,
                    // 2, 4, 8 seconds
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    );

        foreach (var instance in options.Value.Instances)
        {
            services
                .AddHttpClient(instance.Name, cfg =>
                {
                    cfg.BaseAddress =
                        new Uri(options.Value.BaseAddress);
                    cfg.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutInSeconds);
                    cfg.DefaultRequestHeaders.Add("Api-Key", instance.ApiKey);
                })
                .SetHandlerLifetime(TimeSpan.FromSeconds(options.Value.HandlerLifetimeInSeconds))
                .AddPolicyHandler(retryPolicy);
        }
    }
}
