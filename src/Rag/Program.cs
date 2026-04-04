using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Configurations;

namespace Rag;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .Build();

        var ragSettings = configuration.GetSection("Rag").Get<RagSettings>()
            ?? throw new InvalidOperationException("Configuration section 'Rag' is missing or invalid.");
        ragSettings.Validate();

        await using var provider = new ServiceCollection()
            .AddGenAi(configuration)
            .AddSingleton(ragSettings)
            .AddSingleton<RagOrchestrator>()
            .BuildServiceProvider();

        var ai = provider.GetRequiredService<IAiService>();
        var orchestrator = provider.GetRequiredService<RagOrchestrator>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Indexing documents...");
        var store = await orchestrator.BuildIndexAsync(cts.Token);
        Console.WriteLine($"Indexed {store.Count} chunk(s). Ask a question (empty line to exit).");

        if (args.Length > 0)
        {
            var line = string.Join(' ', args);
            var answer = await orchestrator.AnswerAsync(store, line, cts.Token);
            Console.WriteLine(answer);
            return;
        }

        while (!cts.IsCancellationRequested)
        {
            Console.Write("> ");
            var line = await Console.In.ReadLineAsync(cts.Token);
            if (line is null || string.IsNullOrWhiteSpace(line))
                break;

            try
            {
                var answer = await orchestrator.AnswerAsync(store, line, cts.Token);
                Console.WriteLine(answer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
