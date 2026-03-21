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
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .Build();

        await using var provider = new ServiceCollection()
            .AddPromptEngineeringServices(configuration)
            .BuildServiceProvider();

        var contextService = provider.GetRequiredService<IContextService>();

        var runs = await contextService.RunReActAsync(cancellationToken: CancellationToken.None);

        var separator = $"{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}{Environment.NewLine}";
        var resultsText = string.Join(
            separator,
            runs.Select(run =>
            {
                var content = run.Completion.Choices?.FirstOrDefault()?.Message?.Content;
                return string.IsNullOrWhiteSpace(content)
                    ? $"(no assistant content) — saved: {run.OutputPath}"
                    : content;
            }));

        await File.WriteAllTextAsync("results.txt", resultsText);
    }
}
