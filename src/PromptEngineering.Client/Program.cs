using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.Services;
using PromptEngineering.Services.Configurations;

namespace PromptEngineering.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configure
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .Build();

        await using var provider = new ServiceCollection()
            .AddPromptEngineeringServices(configuration)
            .BuildServiceProvider();

        var contextService = provider.GetRequiredService<IContextService>();

        // Run the three-version ReAct chain: v1 (minimal) → v2 (structured) → v3 (strict ReAct).
        // Each completion feeds into the next prompt's <prior_run> region.
        var runs = await contextService.RunVersionChainAsync(
            ["v1.json", "v2.json", "v3.json"],
            cancellationToken: CancellationToken.None);

        // Write summarize.txt aggregating all iteration outputs
        await contextService.SummarizeAsync(runs, cancellationToken: CancellationToken.None);
    }
}
