namespace Chatbot.Tests.Gds;

internal static class GdsPaths
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "gds", "manifest.json")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing gds/manifest.json.");
    }

    public static string GdsRoot => Path.Combine(FindRepoRoot(), "gds");

    public static string ManifestPath => Path.Combine(GdsRoot, "manifest.json");

    public static string GroundTruthDirectory => Path.Combine(GdsRoot, "ground-truth");

    public static string AnswersDirectory => Path.Combine(GdsRoot, "answers");

    public static string JudgeDirectory => Path.Combine(GdsRoot, "judge");

    public static string ResolveGroundTruthPath(string groundTruthRef) =>
        Path.GetFullPath(Path.Combine(GdsRoot, groundTruthRef.Replace('/', Path.DirectorySeparatorChar)));
}
