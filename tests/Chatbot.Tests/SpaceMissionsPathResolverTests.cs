using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class SpaceMissionsPathResolverTests
{
    [Test]
    public void ApplyAbsolutePaths_ResolvesWorkingDirectoryProjectAndDataset()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv",
            SpaceMissionsMcp = { Name = "space-missions-mcp" }
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(options, Path.Combine(repoRoot, "src", "Chatbot"));

        Assert.That(Path.IsPathRooted(options.SpaceMissionsMcp.WorkingDirectory), Is.True);
        Assert.That(options.SpaceMissionsMcp.WorkingDirectory, Is.EqualTo(repoRoot));
        Assert.That(options.SpaceMissionsMcp.Command, Is.EqualTo("dotnet"));
        Assert.That(options.SpaceMissionsMcp.Arguments, Is.EqualTo([
            "run",
            "--no-launch-profile",
            "--project",
            Path.GetFullPath(Path.Combine(repoRoot, "src", "SpaceMissions.McpServer", "SpaceMissions.McpServer.csproj"))
        ]));
        Assert.That(Path.IsPathRooted(options.SpaceMissionsMcp.Environment[SpaceMissionsPathResolver.DatasetPathEnvVar]), Is.True);
        Assert.That(File.Exists(options.SpaceMissionsMcp.Environment[SpaceMissionsPathResolver.DatasetPathEnvVar]), Is.True);
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

        Assert.That(options.SpaceMissionsMcp.WorkingDirectory, Is.EqualTo(repoRoot));
    }
}
