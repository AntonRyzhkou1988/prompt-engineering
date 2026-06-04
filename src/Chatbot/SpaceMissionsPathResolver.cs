using PromptEngineering.Mcp;

namespace Chatbot;

public static class SpaceMissionsPathResolver
{
    public const string DatasetPathEnvVar = "SPACE_MISSIONS_DATASET_PATH";

    public static void ApplyAbsolutePaths(SpaceMissionsAgentOptions options, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.McpProjectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatasetPath);

        var repoRoot = !string.IsNullOrWhiteSpace(options.RepoRoot)
            ? Path.GetFullPath(options.RepoRoot)
            : FindRepoRoot(contentRootPath);

        var projectPath = ToAbsolutePath(repoRoot, options.McpProjectPath);
        var datasetPath = ToAbsolutePath(repoRoot, options.DatasetPath);

        var mcp = options.SpaceMissionsMcp;
        mcp.WorkingDirectory = repoRoot;

        if (string.IsNullOrWhiteSpace(mcp.Name))
            mcp.Name = "space-missions-mcp";

        ConfigureLaunchCommand(mcp, projectPath);

        mcp.Environment ??= new Dictionary<string, string>(StringComparer.Ordinal);
        mcp.Environment[DatasetPathEnvVar] = datasetPath;
    }

    public static string FindRepoRoot(string startPath)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(startPath));
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "dataset", "space_missions.csv")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate repository root containing dataset/space_missions.csv.");
    }

    private static void ConfigureLaunchCommand(McpTransportOptions mcp, string projectPath)
    {
        if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            mcp.Command = "dotnet";
            mcp.Arguments =
            [
                "run",
                "--no-launch-profile",
                "--project",
                projectPath
            ];
            return;
        }

        if (projectPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            mcp.Command = "dotnet";
            mcp.Arguments = ["exec", projectPath];
            return;
        }

        mcp.Command = projectPath;
        mcp.Arguments = [];
    }

    private static string ToAbsolutePath(string repoRoot, string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }
}
