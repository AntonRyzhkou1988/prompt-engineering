using Chatbot;
using Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class SpaceMissionsMcpCommunicationTests
{
    [Test]
    public async Task ConnectAsync_ListsExpectedMcpTools()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv"
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(
            options,
            Path.Combine(repoRoot, "src", "Chatbot"),
            Path.Combine(repoRoot, "src", "Chatbot", "bin", "Debug", "net8.0"));

        var builtDll = Path.Combine(
            repoRoot,
            "src",
            "SpaceMissions.McpServer",
            "bin",
            "Debug",
            "net8.0",
            SpaceMissionsPathResolver.McpServerAssemblyFileName);

        if (!File.Exists(builtDll)
            && SpaceMissionsPathResolver.GetBundledMcpServerDllPath(
                Path.Combine(repoRoot, "src", "Chatbot", "bin", "Debug", "net8.0")) is null)
        {
            Assert.Ignore("Build Chatbot and SpaceMissions.McpServer to run MCP communication test.");
        }

        var service = new SpaceMissionsMcpAgentService(
            Options.Create(options),
            NullLoggerFactory.Instance,
            NullLogger<SpaceMissionsMcpAgentService>.Instance);

        await using var session = await service.ConnectAsync();

        var toolNames = session.Tools.Select(t => t.Name).ToList();
        Assert.That(toolNames, Does.Contain("get_space_missions_schema"));
        Assert.That(toolNames, Does.Contain("get_space_missions_summary"));
        Assert.That(toolNames, Does.Contain("filter_space_missions"));
        Assert.That(toolNames, Does.Contain("aggregate_space_missions"));
        Assert.That(toolNames, Does.Contain("aggregate_space_missions_by_launch_country"));
        Assert.That(toolNames, Does.Contain("compute_space_mission_success_rate"));
        Assert.That(toolNames, Does.Contain("count_space_missions"));
        Assert.That(toolNames, Does.Contain("list_space_mission_distinct_values"));
        Assert.That(session.ToolDefinitions, Has.Count.EqualTo(8));
    }

    [Test]
    public async Task ConnectAsync_CanInvokeCountTool()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv"
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(options, Path.Combine(repoRoot, "src", "Chatbot"));

        var builtDll = Path.Combine(
            repoRoot,
            "src",
            "SpaceMissions.McpServer",
            "bin",
            "Debug",
            "net8.0",
            SpaceMissionsPathResolver.McpServerAssemblyFileName);

        if (!File.Exists(builtDll))
            Assert.Ignore("Build SpaceMissions.McpServer to run MCP communication test.");

        var service = new SpaceMissionsMcpAgentService(
            Options.Create(options),
            NullLoggerFactory.Instance,
            NullLogger<SpaceMissionsMcpAgentService>.Instance);

        await using var session = await service.ConnectAsync();
        var result = await session.CallToolAsync("count_space_missions", "{}", CancellationToken.None);

        Assert.That(result.IsError, Is.Not.EqualTo(true));
    }
}
