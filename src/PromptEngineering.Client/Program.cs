using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .Build();

        var services = new ServiceCollection();
        services.UseGenAi(configuration);
        await using var provider = services.BuildServiceProvider();
        var aiService = provider.GetRequiredService<IAiService>();

        var chatRequest = new ChatRequest()
        {
            Temperature = 0.3f
        };
        chatRequest.AddSystemMessage("You are software developer assistant.");
        chatRequest.AddUserMessage("What is a GC in .NET?");

        var completion = await aiService.CompleteChatAsync("AIArchitect",
            chatRequest,
            new MediaTypeHeaderValue("application/json"),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            CancellationToken.None);

        Console.WriteLine("GenAI dependencies are configured.");
    }
}