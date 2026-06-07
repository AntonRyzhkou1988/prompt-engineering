using System.Globalization;
using Chatbot;
using Chatbot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.LLM.Models;
using Rag;

namespace Chatbot.Tests.Gds;

internal sealed class GdsTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private readonly GdsJudgeOptions _gdsOptions;

    private GdsTestHost(ServiceProvider provider, GdsJudgeOptions gdsOptions)
    {
        _provider = provider;
        _gdsOptions = gdsOptions;
    }

    public RagIndexStore RagIndexStore { get; private init; } = null!;

    public GdsManifest Manifest { get; private init; } = null!;

    public int InterItemDelaySeconds => Math.Max(0, _gdsOptions.InterItemDelaySeconds);

    public int RateLimitMaxAttempts => Math.Max(1, _gdsOptions.RateLimitMaxAttempts);

    public static async Task<GdsTestHost> CreateAsync(CancellationToken cancellationToken = default)
    {
        var repoRoot = GdsPaths.FindRepoRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(repoRoot, "src", "Chatbot"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<SpaceMissionsGdsIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddGenAi(configuration);
        services.AddRag(configuration);
        var gdsSection = configuration.GetSection(GdsJudgeOptions.SectionName);
        services.Configure<GdsJudgeOptions>(gdsSection);
        services.PostConfigure<RagSettings>(options =>
        {
            RagPathResolver.ApplyAbsolutePaths(options, Path.Combine(repoRoot, "src", "Chatbot"));
            var ragInstance = gdsSection["RagInstanceName"];
            if (!string.IsNullOrWhiteSpace(ragInstance))
                options.InstanceName = ragInstance;
        });
        services
            .AddOptions<SpaceMissionsAgentOptions>()
            .Bind(configuration.GetSection(SpaceMissionsAgentOptions.SectionName))
            .PostConfigure(options =>
            {
                SpaceMissionsPathResolver.ApplyAbsolutePaths(
                    options,
                    Path.Combine(repoRoot, "src", "Chatbot"),
                    AppContext.BaseDirectory);
                var agentInstance = gdsSection["AgentInstanceName"];
                if (!string.IsNullOrWhiteSpace(agentInstance))
                    options.InstanceName = agentInstance;
            });
        services.AddSingleton<RagIndexStore>();
        services.AddSingleton<ISpaceMissionsMcpAgentService, SpaceMissionsMcpAgentService>();
        services.AddScoped<SpaceMissionsAgentService>();
        services.AddSingleton<GdsAnswerJudge>();

        var provider = services.BuildServiceProvider();
        var ragStore = provider.GetRequiredService<RagIndexStore>();
        var orchestrator = provider.GetRequiredService<RagOrchestrator>();

        ragStore.IsBuilding = true;
        try
        {
            ragStore.Index = await orchestrator.BuildIndexAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ragStore.IsBuilding = false;
        }

        var manifest = GdsManifest.Load(GdsPaths.ManifestPath);
        var gdsOptions = provider.GetRequiredService<IOptions<GdsJudgeOptions>>().Value;

        return new GdsTestHost(provider, gdsOptions)
        {
            RagIndexStore = ragStore,
            Manifest = manifest,
        };
    }

    public async Task<SpaceMissionsAgentRunResult> RunAgentAsync(string question, CancellationToken cancellationToken)
    {
        return await GdsLlmRetry.ExecuteAsync(
            async ct =>
            {
                await using var scope = _provider.CreateAsyncScope();
                var agent = scope.ServiceProvider.GetRequiredService<SpaceMissionsAgentService>();
                return await agent.RunAsync(question, ct).ConfigureAwait(false);
            },
            RateLimitMaxAttempts,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<GdsJudgeResult> JudgeAsync(
        GdsManifestItem item,
        string agentAnswer,
        GdsGroundTruthDocument groundTruth,
        IReadOnlyList<string> toolsInvoked,
        bool toolRoutingPassed,
        CancellationToken cancellationToken)
    {
        var judge = _provider.GetRequiredService<GdsAnswerJudge>();
        return await judge.VerifyAsync(item, agentAnswer, groundTruth, toolsInvoked, toolRoutingPassed, cancellationToken)
            .ConfigureAwait(false);
    }

    public string? ResolveJudgeInstanceName()
    {
        var gdsOptions = _provider.GetRequiredService<IOptions<GdsJudgeOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(gdsOptions.JudgeInstanceName))
            return gdsOptions.JudgeInstanceName;

        return _provider.GetRequiredService<IOptions<SpaceMissionsAgentOptions>>().Value.InstanceName;
    }

    public bool TryGetApiKey(out string? apiKey)
    {
        var instanceName = ResolveJudgeInstanceName();
        var aiSettings = _provider.GetRequiredService<IOptions<AiServiceSettings>>().Value;
        var instance = aiSettings.Instances.FirstOrDefault(x =>
            x.Name.Equals(instanceName, StringComparison.Ordinal));

        apiKey = instance?.ApiKey;
        return !string.IsNullOrWhiteSpace(apiKey)
            && !apiKey.Equals("DialApiKey", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CheckToolRouting(GdsManifestItem item, IReadOnlyList<string> toolsInvoked)
    {
        if (item.ExpectedTools.Count == 0)
            return true;

        bool Matches(string expected) =>
            toolsInvoked.Any(invoked => invoked.Equals(expected, StringComparison.OrdinalIgnoreCase));

        return item.ExpectedToolsMode.Equals("any", StringComparison.OrdinalIgnoreCase)
            ? item.ExpectedTools.Any(Matches)
            : item.ExpectedTools.All(Matches);
    }

    public static string BuildAnswerDocument(
        GdsManifestItem item,
        SpaceMissionsAgentRunResult result) =>
        string.Join(
            Environment.NewLine,
            "---",
            $"item_id: {item.ItemId}",
            $"source_question_number: {item.SourceQuestionNumber}",
            $"question: {item.Question.Replace("\"", "\\\"", StringComparison.Ordinal)}",
            $"tools_invoked: {string.Join(", ", result.ToolNamesInvoked)}",
            $"generated_utc: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}",
            "---",
            "",
            result.AnswerText);

    public static void EnsureArtifactDirectories()
    {
        Directory.CreateDirectory(GdsPaths.AnswersDirectory);
        Directory.CreateDirectory(GdsPaths.JudgeDirectory);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync().ConfigureAwait(false);
}
