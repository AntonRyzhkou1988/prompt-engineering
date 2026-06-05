using Rag;

namespace Chatbot;

public static class RagPathResolver
{
    public static void ApplyAbsolutePaths(RagSettings options, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        if (string.IsNullOrWhiteSpace(options.DocumentsFolderPath))
            options.DocumentsFolderPath = SpaceMissionsPathResolver.FindRepoRoot(contentRootPath);

        options.EnsureDatasetExists(AppContext.BaseDirectory);
    }
}
