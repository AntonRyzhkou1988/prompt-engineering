using Chatbot.Tests.Gds;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
[Category("Integration")]
public sealed class SpaceMissionsGdsIntegrationTests
{
    [Test]
    [Explicit("Requires LLM API key, MCP server build, RAG index, and network access.")]
    public async Task RunAllItems_AgentAnswersPassToolRoutingAndLlmJudge()
    {
        await using var host = await GdsTestHost.CreateAsync().ConfigureAwait(false);

        if (!host.TryGetApiKey(out _))
        {
            Assert.Ignore(
                "Set a real API key via user secrets or environment variables for " +
                "SpaceMissionsAgent:InstanceName / Gds:JudgeInstanceName before running this test.");
        }

        try
        {
            await using var probeSession = await GdsGroundTruthBuilder.ConnectMcpSessionAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Build SpaceMissions.McpServer", StringComparison.Ordinal))
        {
            Assert.Ignore(ex.Message);
        }

        GdsTestHost.EnsureArtifactDirectories();

        var failures = new List<string>();

        for (var itemIndex = 0; itemIndex < host.Manifest.Items.Count; itemIndex++)
        {
            var item = host.Manifest.Items[itemIndex];
            if (itemIndex > 0 && host.InterItemDelaySeconds > 0)
            {
                TestContext.WriteLine($"Waiting {host.InterItemDelaySeconds}s before {item.ItemId} to reduce token rate limits.");
                await Task.Delay(TimeSpan.FromSeconds(host.InterItemDelaySeconds), CancellationToken.None)
                    .ConfigureAwait(false);
            }

            var groundTruthPath = GdsPaths.ResolveGroundTruthPath(item.GroundTruthRef);
            if (!File.Exists(groundTruthPath))
            {
                await using var session = await GdsGroundTruthBuilder.ConnectMcpSessionAsync().ConfigureAwait(false);
                await GdsGroundTruthBuilder.BuildAllAsync(session, GdsPaths.GroundTruthDirectory).ConfigureAwait(false);
            }

            var groundTruth = GdsGroundTruthDocument.Load(groundTruthPath);
            var result = await host.RunAgentAsync(item.Question, CancellationToken.None).ConfigureAwait(false);

            Assert.That(result.AnswerText, Is.Not.Null.And.Not.Empty, $"{item.ItemId}: empty agent answer.");

            var answerPath = Path.Combine(GdsPaths.AnswersDirectory, $"{item.ItemId}.md");
            await File.WriteAllTextAsync(
                answerPath,
                GdsTestHost.BuildAnswerDocument(item, result),
                CancellationToken.None).ConfigureAwait(false);

            var toolRoutingPassed = GdsTestHost.CheckToolRouting(item, result.ToolNamesInvoked);
            var judgeResult = await host.JudgeAsync(
                item,
                result.AnswerText,
                groundTruth,
                result.ToolNamesInvoked,
                toolRoutingPassed,
                CancellationToken.None).ConfigureAwait(false);

            var judgePath = Path.Combine(GdsPaths.JudgeDirectory, $"{item.ItemId}.json");
            await File.WriteAllTextAsync(judgePath, judgeResult.ToJson(), CancellationToken.None).ConfigureAwait(false);

            TestContext.WriteLine(
                $"{item.ItemId}: tools={string.Join(",", result.ToolNamesInvoked)} " +
                $"routing={(toolRoutingPassed ? "pass" : "fail")} " +
                $"judge={judgeResult.Score} ({(judgeResult.Passed ? "pass" : "fail")})");

            if (!toolRoutingPassed)
            {
                failures.Add(
                    $"{item.ItemId}: expected tools [{string.Join(", ", item.ExpectedTools)}] " +
                    $"but got [{string.Join(", ", result.ToolNamesInvoked)}]");
            }

            if (!judgeResult.Passed)
                failures.Add($"{item.ItemId}: judge score {judgeResult.Score} — {judgeResult.Reasoning}");
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }
}
