using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.LLM.Models;

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

        var systemSettings = configuration.GetRequiredSection("SystemSettings").Get<SystemSettings>()
     ?? throw new InvalidOperationException("Configuration section 'SystemSettings' is missing or invalid.");

        var ragSettings = configuration.GetRequiredSection("Rag").Get<RagSettings>()
            ?? throw new InvalidOperationException("Configuration section 'Rag' is missing or invalid.");
        ragSettings.Validate();
       

        await using var provider = new ServiceCollection()
            .AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true))
            .AddGenAi(configuration)
            .AddSingleton(ragSettings)
            .AddSingleton<RagOrchestrator>()
            .BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<RagOrchestrator>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Indexing documents...");
        var store = await orchestrator.BuildIndexAsync(cts.Token);
        Console.WriteLine($"Indexed {store.Count} chunk(s).");

        if (args.Length > 0)
        {
            var line = string.Join(' ', args);
            var answer = await orchestrator.AnswerAsync(store, line, cts.Token);
            Console.WriteLine(answer);
            return;
        }

        while (!cts.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("Select prefilled mode or manual mode:");
            Console.WriteLine("  [1] Prefilled — all .md files in the questions folder");
            Console.WriteLine("  [2] Manual — enter questions interactively");
            Console.Write("Choice (1/2): ");

            string? choice;
            try
            {
                choice = await Console.In.ReadLineAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (choice is null)
                break;

            var c = choice.Trim();
            if (c == "1")
            {
                await RunPrefilledAsync(orchestrator, store, ragSettings, cts.Token);
                break;
            }

            if (c == "2")
            {
                await RunManualAsync(orchestrator, store, ragSettings, cts.Token);
                break;
            }

            Console.WriteLine("Invalid choice. Enter 1 or 2.");
        }
    }

    private static string QuestionsRoot(RagSettings settings) =>
        Path.Combine(AppContext.BaseDirectory, settings.QuestionsPath);

    private static string AnswersRoot(RagSettings settings) =>
        Path.Combine(AppContext.BaseDirectory, settings.AnswersPath);

    private static void EnsureAnswersDirectory(RagSettings settings) =>
        Directory.CreateDirectory(AnswersRoot(settings));

    private static string BuildPrefilledAnswerDocument(string sourceFileName, string answerBody)
    {
        var utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return string.Join(
            Environment.NewLine,
            "---",
            $"source: {sourceFileName}",
            $"generated_utc: {utc}",
            "---",
            "",
            answerBody);
    }

    private static string BuildManualAnswerDocument(string question, string answerBody)
    {
        var utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return string.Join(
            Environment.NewLine,
            "---",
            $"generated_utc: {utc}",
            "---",
            "",
            "## Question",
            "",
            question,
            "",
            "## Answer",
            "",
            answerBody);
    }

    private static async Task RunPrefilledAsync(
        RagOrchestrator orchestrator,
        InMemoryVectorStore store,
        RagSettings settings,
        CancellationToken cancellationToken)
    {
        var root = QuestionsRoot(settings);
        if (!Directory.Exists(root))
        {
            Console.WriteLine($"Questions directory not found: {root}");
            return;
        }

        var files = Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine($"No .md question files in '{root}'.");
            return;
        }

        EnsureAnswersDirectory(settings);
        var ok = 0;
        var fail = 0;

        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            var question = await File.ReadAllTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(question))
            {
                Console.Error.WriteLine($"Skip (empty): {name}");
                fail++;
                continue;
            }

            try
            {
                var answer = await orchestrator.AnswerAsync(store, question.Trim(), cancellationToken);
                var outPath = Path.Combine(AnswersRoot(settings), name);
                var body = BuildPrefilledAnswerDocument(name, answer);
                await File.WriteAllTextAsync(outPath, body, cancellationToken);
                ok++;
                Console.WriteLine($"Saved: {outPath}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fail++;
                Console.Error.WriteLine($"{name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Prefilled run finished: {ok} saved, {fail} failed or skipped.");
    }

    private static async Task RunManualAsync(
        RagOrchestrator orchestrator,
        InMemoryVectorStore store,
        RagSettings settings,
        CancellationToken cancellationToken)
    {
        EnsureAnswersDirectory(settings);
        Console.WriteLine();
        Console.WriteLine("Manual mode — one question per line (empty line to exit). Each answer is saved under answers/.");

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("> ");
            string? line;
            try
            {
                line = await Console.In.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line is null || string.IsNullOrWhiteSpace(line))
                break;

            try
            {
                var trimmed = line.Trim();
                var answer = await orchestrator.AnswerAsync(store, trimmed, cancellationToken);
                var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                var fileName = $"manual_{stamp}.md";
                var outPath = Path.Combine(AnswersRoot(settings), fileName);
                var body = BuildManualAnswerDocument(trimmed, answer);
                await File.WriteAllTextAsync(outPath, body, cancellationToken);
                Console.WriteLine(answer);
                Console.WriteLine($"Saved: {outPath}");
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
