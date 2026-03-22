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

        // Run 3 chained ReAct iterations: each completion feeds into the next via <prior_run>
        var runs = await contextService.RunIterativeAsync(
            "initial.json", iterations: 3, cancellationToken: CancellationToken.None);

        // Write summarize.txt aggregating all iteration outputs
        await contextService.SummarizeAsync(runs, cancellationToken: CancellationToken.None);
    }
}
