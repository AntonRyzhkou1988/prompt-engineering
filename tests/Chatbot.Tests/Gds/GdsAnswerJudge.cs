using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;

namespace Chatbot.Tests.Gds;

internal sealed class GdsAnswerJudge(
    IAiService aiService,
    IOptions<GdsJudgeOptions> gdsOptions,
    IOptions<Chatbot.SpaceMissionsAgentOptions> agentOptions)
{
    private const string SystemPrompt =
        """
        You are an evaluator for space-missions Chatbot answers (RAG + MCP hybrid agent).
        Compare the agent answer against authoritative ground-truth facts and verification criteria.
        Score using Answer Correctness Score (ACS):
        - 1 = correct and covers all key criteria
        - 0.5 = partially correct or missing a major point
        - 0 = incorrect, fabricated facts, or off-topic
        Set passed=true when score >= 0.5.
        Ground-truth facts override the agent answer when they conflict.
        Reply with JSON only: {"score": number, "passed": boolean, "reasoning": string}
        """;

    public async Task<GdsJudgeResult> VerifyAsync(
        GdsManifestItem item,
        string agentAnswer,
        GdsGroundTruthDocument groundTruth,
        IReadOnlyList<string> toolsInvoked,
        bool toolRoutingPassed,
        CancellationToken cancellationToken = default)
    {
        var instanceName = gdsOptions.Value.JudgeInstanceName?.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            instanceName = agentOptions.Value.InstanceName.Trim();
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new InvalidOperationException("Gds judge instance name is not configured.");

        var criteria = string.Join(Environment.NewLine, item.VerificationCriteria.Select(c => $"- {c}"));
        var userPrompt = new StringBuilder()
            .AppendLine("## Question")
            .AppendLine(item.Question)
            .AppendLine()
            .AppendLine("## Agent answer")
            .AppendLine(agentAnswer)
            .AppendLine()
            .AppendLine("## Tools invoked by agent")
            .AppendLine(toolsInvoked.Count > 0 ? string.Join(", ", toolsInvoked) : "(none)")
            .AppendLine()
            .AppendLine("## Verification criteria")
            .AppendLine(criteria)
            .AppendLine()
            .AppendLine("## Ground-truth key facts (authoritative)")
            .AppendLine(JsonSerializer.Serialize(groundTruth.KeyFacts, new JsonSerializerOptions { WriteIndented = true }))
            .ToString();

        var request = new ChatRequest();
        request
            .SetTemperature(0f)
            .AddSystemMessage(SystemPrompt)
            .AddUserMessage(userPrompt);

        var completion = await aiService.CompleteChatAsync(
            instanceName,
            request,
            new MediaTypeHeaderValue("application/json"),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            cancellationToken).ConfigureAwait(false);

        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
            ?? throw new InvalidOperationException("Judge LLM returned no content.");

        var parsed = ParseJudgeResponse(content);

        return new GdsJudgeResult
        {
            ItemId = item.ItemId,
            Score = parsed.Score,
            Passed = parsed.Passed,
            Reasoning = parsed.Reasoning,
            ToolsInvoked = toolsInvoked,
            ToolRoutingPassed = toolRoutingPassed,
            GeneratedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        };
    }

    private static GdsJudgeResponse ParseJudgeResponse(string content)
    {
        var json = ExtractJsonObject(content);
        var parsed = JsonSerializer.Deserialize<GdsJudgeResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Judge response JSON could not be parsed.");

        if (parsed.Score is < 0 or > 1)
            throw new InvalidOperationException($"Judge score out of range: {parsed.Score}");

        return parsed;
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Judge response did not contain a JSON object.");

        return content[start..(end + 1)];
    }
}

internal sealed class GdsJudgeOptions
{
    public const string SectionName = "Gds";

    public string? JudgeInstanceName { get; set; }
}
