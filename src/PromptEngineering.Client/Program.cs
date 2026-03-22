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
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Run ReAct sequence 
        var runs = await contextService.RunReActAsync(cancellationToken: cts.Token);

        // Output result
        Console.WriteLine($"Completed prompts count: {runs.Count}");
    }
}
