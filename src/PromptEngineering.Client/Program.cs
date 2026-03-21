using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.Client.Configurations;
using PromptEngineering.Services;

namespace PromptEngineering.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await using var provider = new ServiceCollection().BuildPromptEngineeringClientServiceProvider();
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