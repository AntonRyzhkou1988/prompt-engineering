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

namespace Agent;

public sealed class WeatherNewsAgentService
{
    private const string SystemPrompt =
        """
        You are a concise assistant. For current weather, forecasts, or atmospheric conditions, call the Open-Meteo MCP tools.
        For news headlines, web search, or current events, call the DuckDuckGo MCP tools (e.g. duckduckgo_web_search).
        Use tools for factual data; do not invent temperatures or URLs. If a tool errors, say so briefly.
        """;

    private static readonly MediaTypeHeaderValue JsonMedia = new("application/json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IOptions<AgentOptions> _options;
    private readonly IOptions<AiServiceSettings> _aiSettings;
    private readonly IAiService _ai;
    private readonly IWeatherAgentService _weatherAgent;
    private readonly INewsAgentService _newsAgent;
    private readonly ILogger<WeatherNewsAgentService> _logger;

    public WeatherNewsAgentService(
        IOptions<AgentOptions> options,
        IOptions<AiServiceSettings> aiSettings,
        IAiService ai,
        IWeatherAgentService weatherAgent,
        INewsAgentService newsAgent,
        ILogger<WeatherNewsAgentService> logger)
    {
        _options = options;
        _aiSettings = aiSettings;
        _ai = ai;
        _weatherAgent = weatherAgent;
        _newsAgent = newsAgent;
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userQuestion);

        var opts = _options.Value;
        var instanceName = opts.InstanceName.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new InvalidOperationException("Agent:InstanceName is not set.");

        var deployment = ResolveDeployment(instanceName);
        var maxIterations = Math.Max(1, opts.MaxFunctionIterations);

        await using var weatherSession = await _weatherAgent.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await using var newsSession = await _newsAgent.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var toolDefinitions = new List<ChatToolDefinition>(weatherSession.ToolDefinitions.Count + newsSession.ToolDefinitions.Count);
        toolDefinitions.AddRange(weatherSession.ToolDefinitions);
        toolDefinitions.AddRange(newsSession.ToolDefinitions);

        _logger.LogInformation(
            "MCP tools loaded: Open-Meteo={OpenMeteoCount}, DuckDuckGo={DdgCount}, tool definitions={DefCount}",
            weatherSession.Tools.Count, newsSession.Tools.Count, toolDefinitions.Count);

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
                return new AgentRunResult(text, invokedToolNames);
            }

            foreach (var call in calls)
            {
                var name = call.Function?.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                invokedToolNames.Add(name);
                var argsJson = call.Function?.Arguments ?? "{}";
                var toolCallId = call.Id ?? Guid.NewGuid().ToString("N");

                var result = await InvokeToolAsync(
                        weatherSession,
                        newsSession,
                        name,
                        argsJson,
                        cancellationToken)
                    .ConfigureAwait(false);

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
        return new AgentRunResult(last?.Content?.Trim() ?? string.Empty, invokedToolNames);
    }

    private string ResolveDeployment(string instanceName)
    {
        var inst = _aiSettings.Value.Instances.FirstOrDefault(x =>
            x.Name.Equals(instanceName, StringComparison.Ordinal));
        if (inst is null)
            throw new InvalidOperationException($"No AI instance named '{instanceName}' in SystemSettings:AiServiceSettings:Instances.");
        return inst.Deployment;
    }

    private static async Task<CallToolResult> InvokeToolAsync(
        IMcpBackendSession weatherSession,
        IMcpBackendSession newsSession,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (weatherSession.Tools.Any(t => t.Name == toolName))
            return await weatherSession.CallToolAsync(toolName, argumentsJson, cancellationToken).ConfigureAwait(false);
        if (newsSession.Tools.Any(t => t.Name == toolName))
            return await newsSession.CallToolAsync(toolName, argumentsJson, cancellationToken).ConfigureAwait(false);

        throw new InvalidOperationException($"Unknown tool name '{toolName}'.");
    }
}

public sealed record AgentRunResult(string AnswerText, IReadOnlyList<string> ToolNamesInvoked);
