using Chatbot;
using Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class SpaceMissionsPathResolverTests
{
    [Test]
    public void ApplyAbsolutePaths_ResolvesAbsoluteDatasetPath()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv",
            SpaceMissionsMcp = { Name = "space-missions-mcp" }
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(
            options,
            Path.Combine(repoRoot, "src", "Chatbot"),
            AppContext.BaseDirectory);

        var datasetPath = options.SpaceMissionsMcp.Environment[SpaceMissionsPathResolver.DatasetPathEnvVar];
        Assert.That(Path.IsPathRooted(datasetPath), Is.True);
        Assert.That(File.Exists(datasetPath), Is.True);
        Assert.That(options.SpaceMissionsMcp.Command, Is.EqualTo("dotnet"));
        Assert.That(Path.IsPathRooted(options.SpaceMissionsMcp.WorkingDirectory!), Is.True);
    }

    [Test]
    public void ApplyAbsolutePaths_PrefersBuiltMcpServerDllOverCsproj()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var builtDll = Path.Combine(
            repoRoot,
            "src",
            "SpaceMissions.McpServer",
            "bin",
            "Debug",
            "net8.0",
            SpaceMissionsPathResolver.McpServerAssemblyFileName);

        if (!File.Exists(builtDll))
            Assert.Ignore("Build SpaceMissions.McpServer to run this test.");

        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv"
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(options, Path.Combine(repoRoot, "src", "Chatbot"));

        Assert.That(options.SpaceMissionsMcp.Arguments, Is.EqualTo(["exec", builtDll]));
        Assert.That(options.SpaceMissionsMcp.WorkingDirectory, Is.EqualTo(Path.GetDirectoryName(builtDll)));
    }

    [Test]
    public void ApplyAbsolutePaths_RepoRootOverride_UsesConfiguredRoot()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new SpaceMissionsAgentOptions
        {
            RepoRoot = repoRoot,
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv"
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(options, Path.GetTempPath());

        var datasetPath = options.SpaceMissionsMcp.Environment[SpaceMissionsPathResolver.DatasetPathEnvVar];
        Assert.That(datasetPath, Is.EqualTo(Path.Combine(repoRoot, "dataset", "space_missions.csv")));
    }

    [Test]
    public void GetBundledMcpServerDllPath_WhenCopiedToOutput_ReturnsAbsolutePath()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var chatbotOutput = Path.Combine(repoRoot, "src", "Chatbot", "bin", "Debug", "net8.0");
        if (!Directory.Exists(chatbotOutput))
            Assert.Ignore("Build Chatbot to run this test.");

        var bundledDll = SpaceMissionsPathResolver.GetBundledMcpServerDllPath(chatbotOutput);
        if (bundledDll is null)
            Assert.Ignore("Chatbot output does not contain mcp-server folder yet. Run: dotnet build src/Chatbot/Chatbot.csproj");

        Assert.That(Path.IsPathRooted(bundledDll), Is.True);
        Assert.That(File.Exists(bundledDll), Is.True);
    }
}
