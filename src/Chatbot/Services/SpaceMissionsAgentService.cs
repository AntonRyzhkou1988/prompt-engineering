using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;
using PromptEngineering.Mcp;
using Rag;
using LlmRole = PromptEngineering.LLM.Models.Role;

namespace Chatbot.Services;

public sealed class SpaceMissionsAgentService(
    IOptions<SpaceMissionsAgentOptions> options,
    IOptions<AiServiceSettings> aiSettings,
    IAiService ai,
    ISpaceMissionsMcpAgentService mcpAgent,
    RagOrchestrator ragOrchestrator,
    RagIndexStore ragIndexStore,
    ILogger<SpaceMissionsAgentService> logger)
{
    private const string HybridSystemPromptTemplate =
        """
        You are a concise space launch data analyst for dataset/space_missions.csv.
        Retrieved context from the corpus is below. Use it for explanations and cite non-obvious claims with [n].
        For exact counts, filters, aggregates, success rates, distinct values, or paginated row lists, call the space missions MCP tools.
        Use get_space_missions_schema for column definitions and get_space_missions_summary for dataset overview.
        Use list_space_mission_distinct_values to discover filter values (column + optional search) before exact-match filters.
        Use filter_space_missions (with offset for pagination), count_space_missions, and aggregate_space_missions for row-level and grouped analysis.
        Use aggregate_space_missions_by_launch_country for country share questions (last comma segment of Location).
        Use compute_space_mission_success_rate for success-rate questions instead of manual division.
        Do not invent counts, percentages, or mission facts. Prefer MCP tools over guessing from partial context.
        If tool results are partial (row caps or bucket rollups), say so.

        {0}

        ## Retrieved context
        {1}
        """;

    private static readonly MediaTypeHeaderValue JsonMedia = new("application/json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<SpaceMissionsAgentRunResult> RunAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userQuestion);

        var opts = options.Value;
        var instanceName = opts.InstanceName.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new InvalidOperationException("SpaceMissionsAgent:InstanceName is not set.");

        // RAG
        EnsureRagIndexReady();

        var deployment = ResolveDeployment(instanceName);
        var maxIterations = Math.Max(1, opts.MaxFunctionIterations);

        var retrieval = await ragOrchestrator
            .RetrieveContextAsync(ragIndexStore.Index!, userQuestion, cancellationToken)
            .ConfigureAwait(false);

        var filteredChunks = FilterRetrievedChunks(retrieval.RankedChunks, opts.MinRetrievalSimilarity);
        var contextSection = filteredChunks.Count == 0
            ? "(No relevant chunks above similarity threshold; rely on MCP tools for evidence.)"
            : RagContextFormatter.FormatContextBlocks(filteredChunks);

        // MCP
        var toolHints = SpaceMissionToolRoutingHints.BuildHints(userQuestion);
        var routingSection = string.IsNullOrWhiteSpace(toolHints)
            ? string.Empty
            : toolHints + Environment.NewLine;

        var systemPrompt = string.Format(
            HybridSystemPromptTemplate,
            routingSection,
            contextSection);

        await using var session = await mcpAgent.ConnectAsync(cancellationToken).ConfigureAwait(false);
        var toolDefinitions = session.ToolDefinitions.ToList();

        logger.LogInformation(
            "Space missions MCP tools loaded: {ToolCount} tool definitions; retrieved {RetrievedCount} chunk(s), kept {KeptCount} above similarity {MinSimilarity:F2}",
            toolDefinitions.Count,
            retrieval.RankedChunks.Count,
            filteredChunks.Count,
            opts.MinRetrievalSimilarity);

        var conversation = new List<ChatMessage>
        {
            new() { Role = LlmRole.System, Content = systemPrompt },
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

            var completion = await ai.CompleteChatAsync(instanceName, request, JsonMedia, JsonOptions, cancellationToken)
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

    private void EnsureRagIndexReady()
    {
        if (ragIndexStore.IsReady)
            return;

        if (ragIndexStore.IsBuilding)
            throw new RagIndexNotReadyException("The knowledge index is still building. Please try again in a moment.");

        if (ragIndexStore.BuildError is { } buildError)
            throw new RagIndexNotReadyException("The knowledge index failed to build.", buildError);

        throw new RagIndexNotReadyException("The knowledge index is not available.");
    }

    private static IReadOnlyList<(VectorRecord Record, float Similarity)> FilterRetrievedChunks(
        IReadOnlyList<(VectorRecord Record, float Similarity)> rankedChunks,
        float minSimilarity)
    {
        if (minSimilarity <= 0f)
            return rankedChunks;

        return rankedChunks
            .Where(x => x.Similarity >= minSimilarity)
            .ToList();
    }

    private string ResolveDeployment(string instanceName)
    {
        var inst = aiSettings.Value.Instances.FirstOrDefault(x =>
            x.Name.Equals(instanceName, StringComparison.Ordinal));
        if (inst is null)
            throw new InvalidOperationException($"No AI instance named '{instanceName}' in SystemSettings:AiServiceSettings:Instances.");
        return inst.Deployment;
    }
}

public sealed class RagIndexNotReadyException : Exception
{
    public RagIndexNotReadyException(string message) : base(message)
    {
    }

    public RagIndexNotReadyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed record SpaceMissionsAgentRunResult(string AnswerText, IReadOnlyList<string> ToolNamesInvoked);
