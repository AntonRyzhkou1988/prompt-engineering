using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;
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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WeatherNewsAgentService> _logger;

    public WeatherNewsAgentService(
        IOptions<AgentOptions> options,
        IOptions<AiServiceSettings> aiSettings,
        IAiService ai,
        ILoggerFactory loggerFactory,
        ILogger<WeatherNewsAgentService> logger)
    {
        _options = options;
        _aiSettings = aiSettings;
        _ai = ai;
        _loggerFactory = loggerFactory;
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

        await using var openMeteo = await CreateMcpClientAsync(opts.OpenMeteo, cancellationToken).ConfigureAwait(false);
        await using var duckDuckGo = await CreateMcpClientAsync(opts.DuckDuckGo, cancellationToken).ConfigureAwait(false);

        var openMeteoTools = await openMeteo.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var duckDuckGoTools = await duckDuckGo.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var toolDefinitions = BuildToolDefinitions(openMeteoTools.ToList(), duckDuckGoTools.ToList());

        _logger.LogInformation(
            "MCP tools loaded: Open-Meteo={OpenMeteoCount}, DuckDuckGo={DdgCount}, tool definitions={DefCount}",
            openMeteoTools.Count, duckDuckGoTools.Count, toolDefinitions.Count);

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
                        openMeteo,
                        duckDuckGo,
                        openMeteoTools,
                        duckDuckGoTools,
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

    private static List<ChatToolDefinition> BuildToolDefinitions(
        IList<McpClientTool> openMeteoTools,
        IList<McpClientTool> duckDuckGoTools)
    {
        var list = new List<ChatToolDefinition>();
        foreach (var t in openMeteoTools)
            list.Add(ToChatToolDefinition(t));
        foreach (var t in duckDuckGoTools)
            list.Add(ToChatToolDefinition(t));
        return list;
    }

    private static ChatToolDefinition ToChatToolDefinition(McpClientTool t)
    {
        JsonElement? parameters = t.JsonSchema is { } schema ? schema : JsonDocument.Parse("{}").RootElement;
        return new ChatToolDefinition
        {
            Function = new ChatToolFunctionDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = parameters
            }
        };
    }

    private static async Task<CallToolResult> InvokeToolAsync(
        McpClient openMeteo,
        McpClient duckDuckGo,
        IList<McpClientTool> openMeteoTools,
        IList<McpClientTool> duckDuckGoTools,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var args = ParseToolArguments(argumentsJson);
        if (openMeteoTools.Any(t => t.Name == toolName))
            return await openMeteo.CallToolAsync(toolName, args, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (duckDuckGoTools.Any(t => t.Name == toolName))
            return await duckDuckGo.CallToolAsync(toolName, args, cancellationToken: cancellationToken).ConfigureAwait(false);

        throw new InvalidOperationException($"Unknown tool name '{toolName}'.");
    }

    private static IReadOnlyDictionary<string, object?>? ParseToolArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict is null)
                return null;
            return dict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }
        catch
        {
            return null;
        }
    }

    private async Task<McpClient> CreateMcpClientAsync(McpTransportOptions mcp, CancellationToken cancellationToken)
    {
        var env = MergeEnvironmentFromProcess(mcp.Environment);
        var transportOptions = new StdioClientTransportOptions
        {
            Name = mcp.Name,
            Command = mcp.Command,
            Arguments = mcp.Arguments.ToArray(),
        };

        if (!string.IsNullOrWhiteSpace(mcp.WorkingDirectory))
            transportOptions.WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, mcp.WorkingDirectory));

        if (env.Count > 0)
            transportOptions.EnvironmentVariables = env.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);

        var transport = new StdioClientTransport(transportOptions, _loggerFactory);
        return await McpClient.CreateAsync(transport, null, _loggerFactory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// For each key in <paramref name="configured"/>, if the value is empty, use the process environment variable when set.
    /// </summary>
    private static Dictionary<string, string> MergeEnvironmentFromProcess(Dictionary<string, string> configured)
    {
        var env = new Dictionary<string, string>(configured, StringComparer.Ordinal);
        foreach (var kv in configured.ToList())
        {
            if (string.IsNullOrWhiteSpace(kv.Value))
            {
                var fromOs = Environment.GetEnvironmentVariable(kv.Key);
                if (!string.IsNullOrWhiteSpace(fromOs))
                    env[kv.Key] = fromOs;
            }
        }

        return env;
    }
}

public sealed record AgentRunResult(string AnswerText, IReadOnlyList<string> ToolNamesInvoked);
