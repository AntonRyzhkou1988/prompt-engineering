using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;
using PromptEngineering.Mcp;
using LlmRole = PromptEngineering.LLM.Models.Role;

namespace Chatbot.Services;

public sealed class SpaceMissionsAgentService
{
    private const string SystemPrompt =
        """
        You are a concise space launch data analyst for dataset/space_missions.csv.
        For any factual question about launches, companies, rockets, locations, dates, or mission outcomes, call the space missions MCP tools.
        Use get_space_missions_schema when you need column definitions.
        Use filter_space_missions, aggregate_space_missions, or count_space_missions to ground answers in tool results.
        Do not invent counts, percentages, or mission facts. If tool results are partial (row caps), say so.
        """;

    private static readonly MediaTypeHeaderValue JsonMedia = new("application/json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IOptions<SpaceMissionsAgentOptions> _options;
    private readonly IOptions<AiServiceSettings> _aiSettings;
    private readonly IAiService _ai;
    private readonly ISpaceMissionsMcpAgentService _mcpAgent;
    private readonly ILogger<SpaceMissionsAgentService> _logger;

    public SpaceMissionsAgentService(
        IOptions<SpaceMissionsAgentOptions> options,
        IOptions<AiServiceSettings> aiSettings,
        IAiService ai,
        ISpaceMissionsMcpAgentService mcpAgent,
        ILogger<SpaceMissionsAgentService> logger)
    {
        _options = options;
        _aiSettings = aiSettings;
        _ai = ai;
        _mcpAgent = mcpAgent;
        _logger = logger;
    }

    public async Task<SpaceMissionsAgentRunResult> RunAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userQuestion);

        var opts = _options.Value;
        var instanceName = opts.InstanceName.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new InvalidOperationException("SpaceMissionsAgent:InstanceName is not set.");

        var deployment = ResolveDeployment(instanceName);
        var maxIterations = Math.Max(1, opts.MaxFunctionIterations);

        await using var session = await _mcpAgent.ConnectAsync(cancellationToken).ConfigureAwait(false);
        var toolDefinitions = session.ToolDefinitions.ToList();

        _logger.LogInformation(
            "Space missions MCP tools loaded: {ToolCount} tool definitions",
            toolDefinitions.Count);

        var conversation = new List<ChatMessage>
        {
            new() { Role = LlmRole.System, Content = SystemPrompt },
            new() { Role = LlmRole.User, Content = userQuestion }
        };

        var invokedToolNames = new List<string>();

        for (var i = 0; i < maxIterations; i++)
        {
            var request = new ChatRequest
            {
                Model = deployment,
                Temperature = opts.Temperature,
                Tools = toolDefinitions,
            };
            foreach (var m in conversation)
                request.AddMessage(m);

            var completion = await _ai.CompleteChatAsync(instanceName, request, JsonMedia, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (completion?.Choices?.FirstOrDefault()?.Message is not { } assistantMessage)
                throw new InvalidOperationException("Chat API returned no message.");

            conversation.Add(assistantMessage);

            if (assistantMessage.ToolCalls is not { Count: > 0 } calls)
            {
                var text = assistantMessage.Content?.Trim() ?? string.Empty;
                return new SpaceMissionsAgentRunResult(text, invokedToolNames);
            }

            foreach (var call in calls)
            {
                var name = call.Function?.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                invokedToolNames.Add(name);
                var argsJson = call.Function?.Arguments ?? "{}";
                var toolCallId = call.Id ?? Guid.NewGuid().ToString("N");

                var result = await session.CallToolAsync(name, argsJson, cancellationToken).ConfigureAwait(false);
                var content = McpCallToolResultFormatter.ToModelText(result);
                conversation.Add(new ChatMessage
                {
                    Role = LlmRole.Tool,
                    ToolCallId = toolCallId,
                    Content = content
                });
            }
        }

        var last = conversation.LastOrDefault(m => m.Role == LlmRole.Assistant);
        return new SpaceMissionsAgentRunResult(last?.Content?.Trim() ?? string.Empty, invokedToolNames);
    }

    private string ResolveDeployment(string instanceName)
    {
        var inst = _aiSettings.Value.Instances.FirstOrDefault(x =>
            x.Name.Equals(instanceName, StringComparison.Ordinal));
        if (inst is null)
            throw new InvalidOperationException($"No AI instance named '{instanceName}' in SystemSettings:AiServiceSettings:Instances.");
        return inst.Deployment;
    }
}

public sealed record SpaceMissionsAgentRunResult(string AnswerText, IReadOnlyList<string> ToolNamesInvoked);
