using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.Client.Configurations;
using PromptEngineering.Services;

namespace PromptEngineering.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configuration
        await using var provider = new ServiceCollection().BuildPromptEngineeringClientServiceProvider();
        var contextService = provider.GetRequiredService<IContextService>();

        var promptPaths = PromptJsonDiscovery.GetOrderedPromptJsonFullPaths();
        foreach (var promptPath in promptPaths)
        {
            Console.WriteLine($"Running pipeline for {promptPath}...");

            var pipelineResult = await contextService.RunAsync(promptPath, CancellationToken.None);
            var completion = pipelineResult.Completion;

            if (completion.Choices == null || !completion.Choices.Any())
            {
                Console.WriteLine($"First choice saved to {pipelineResult.OutputPath} (no choices returned).");
                continue;
            }

            var choice = completion.Choices.First();
            var messageContent = choice?.Message?.Content;

            if (!string.IsNullOrWhiteSpace(messageContent))
            {
                Console.WriteLine(messageContent);
            }

            Console.WriteLine($"Saved assistant Markdown: {pipelineResult.OutputPath}");
        }
    }
}