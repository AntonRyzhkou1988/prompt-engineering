using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.Client.Configurations;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Exceptions;
using PromptEngineering.Services;

namespace PromptEngineering.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configuration
        await using var provider = new ServiceCollection().BuildPromptEngineeringClientServiceProvider();
        var aiService = provider.GetRequiredService<IAiService>();
        var promptService = provider.GetRequiredService<IPromptService>();

        // Prompt generation
        var chatRequest = await promptService.BuildAsync(CancellationToken.None);

        // Completion
        var completion = await aiService.CompleteChatAsync("AIArchitect.PromptEngineering",
            chatRequest,
            new MediaTypeHeaderValue("application/json"),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            CancellationToken.None);
        if (completion == null) throw new OpenAiException("Completion is null.");

        if (completion.Usage == null) throw new OpenAiException("Completion usage is null.");

        if (completion.Choices == null) throw new OpenAiException("Completion choices is null.");

        // Output
        var choice = completion.Choices.FirstOrDefault();
        if (choice == null) throw new OpenAiException("Completion first choice is null");

        if (choice.Message == null) throw new OpenAiException("Completion first choice message is null");

        Console.WriteLine($"{choice.Message.Content}");
    }
}