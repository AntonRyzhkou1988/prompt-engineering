namespace PromptEngineering.Services;

internal static class ContextSettingsPromptPathResolver
{
    /// <summary>
    /// Resolves a prompts JSON path: absolute paths as-is; otherwise tries <see cref="AppContext.BaseDirectory"/>, then walks parent directories (repo-root dev layout).
    /// </summary>
    public static string? ResolveExistingFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path))
        {
            var full = Path.GetFullPath(path);
            return File.Exists(full) ? full : null;
        }

        var baseDir = AppContext.BaseDirectory;
        var fromBase = Path.GetFullPath(Path.Combine(baseDir, path));
        if (File.Exists(fromBase))
        {
            return fromBase;
        }

        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var candidate = Path.GetFullPath(Path.Combine(dir.FullName, path));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
