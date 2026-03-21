namespace PromptEngineering.Services;

public static class PromptJsonDiscovery
{
    /// <summary>
    /// Returns full paths to every <c>*.json</c> file in the resolved prompts directory, ordered by file name (ordinal, case-insensitive).
    /// </summary>
    public static IReadOnlyList<string> GetOrderedPromptJsonFullPaths(string promptPath = "prompts")
    {
        var dir = ContextSettingsPromptPathResolver.ResolveExistingDirectoryPath(promptPath)
            ?? throw new InvalidOperationException(
                $"Prompts directory was not found. Tried path '{promptPath}' (resolved from app base and parent directories).");

        return Directory.GetFiles(dir, "*.json")
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
