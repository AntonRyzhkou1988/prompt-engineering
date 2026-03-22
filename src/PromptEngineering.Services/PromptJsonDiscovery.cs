namespace PromptEngineering.Services;

public static class PromptJsonDiscovery
{
    /// <summary>
    /// Returns full paths to every <c>*.json</c> file in the resolved prompts directory, ordered by file name (ordinal, case-insensitive).
    /// </summary>
    public static IReadOnlyList<string> GetOrderedPromptJsonFullPaths(string promptPath)
    {
        return Directory.GetFiles(promptPath, "*.json")
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
