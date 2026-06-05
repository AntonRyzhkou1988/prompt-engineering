using PromptEngineering.Mcp;

namespace Chatbot;

public static class SpaceMissionsPathResolver
{
    public const string DatasetPathEnvVar = "SPACE_MISSIONS_DATASET_PATH";
    public const string BundledMcpServerFolderName = "mcp-server";
    public const string McpServerAssemblyFileName = "SpaceMissions.McpServer.dll";

    public static void ApplyAbsolutePaths(
        SpaceMissionsAgentOptions options,
        string contentRootPath,
        string? applicationBasePath = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.McpProjectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatasetPath);

        applicationBasePath ??= AppContext.BaseDirectory;

        var repoRoot = !string.IsNullOrWhiteSpace(options.RepoRoot)
            ? Path.GetFullPath(options.RepoRoot)
            : FindRepoRoot(contentRootPath);

        var launchPath = ResolveMcpServerLaunchPath(repoRoot, applicationBasePath, options.McpProjectPath);
        var datasetPath = ToAbsolutePath(repoRoot, options.DatasetPath);

        var mcp = options.SpaceMissionsMcp;

        if (string.IsNullOrWhiteSpace(mcp.Name))
            mcp.Name = "space-missions-mcp";

        ConfigureLaunchCommand(mcp, launchPath);

        mcp.Environment ??= new Dictionary<string, string>(StringComparer.Ordinal);
        mcp.Environment[DatasetPathEnvVar] = datasetPath;
    }

    public static string? GetBundledMcpServerDllPath(string applicationBasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationBasePath);

        var bundledDll = Path.Combine(
            applicationBasePath,
            BundledMcpServerFolderName,
            McpServerAssemblyFileName);

        return File.Exists(bundledDll) ? Path.GetFullPath(bundledDll) : null;
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

    public static string ResolveMcpServerLaunchPath(
        string repoRoot,
        string applicationBasePath,
        string configuredProjectPath)
    {
        var bundledDll = GetBundledMcpServerDllPath(applicationBasePath);
        if (bundledDll is not null)
            return bundledDll;

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var builtDll = Path.Combine(
                repoRoot,
                "src",
                "SpaceMissions.McpServer",
                "bin",
                configuration,
                "net8.0",
                McpServerAssemblyFileName);

            if (File.Exists(builtDll))
                return Path.GetFullPath(builtDll);
        }

        return ToAbsolutePath(repoRoot, configuredProjectPath);
    }

    private static void ConfigureLaunchCommand(McpTransportOptions mcp, string launchPath)
    {
        if (launchPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            mcp.Command = "dotnet";
            mcp.Arguments = ["exec", launchPath];
            mcp.WorkingDirectory = Path.GetDirectoryName(launchPath)!;
            return;
        }

        if (launchPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            mcp.Command = "dotnet";
            mcp.Arguments =
            [
                "run",
                "--no-launch-profile",
                "--project",
                launchPath
            ];
            mcp.WorkingDirectory = Path.GetDirectoryName(launchPath)!;
            return;
        }

        mcp.Command = launchPath;
        mcp.Arguments = [];
        mcp.WorkingDirectory = Path.GetDirectoryName(launchPath) ?? AppContext.BaseDirectory;
    }

    private static string ToAbsolutePath(string repoRoot, string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }
}
