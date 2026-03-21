using Microsoft.Extensions.Options;

namespace PromptEngineering.Services;

/// <summary>
/// Loads <see cref="ContextPromptsOptions"/> from the JSON file at <see cref="ContextSettings.PromptPath"/> (sole source at runtime).
/// </summary>
internal sealed class ContextPromptsPostConfigure(IOptions<ContextSettings> contextSettings)
    : IPostConfigureOptions<ContextPromptsOptions>
{
    public void PostConfigure(string? name, ContextPromptsOptions options)
    {
        if (name is not null && !string.Equals(name, Options.DefaultName, StringComparison.Ordinal))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(options);

        var promptPath = contextSettings.Value.PromptPath;
        ArgumentException.ThrowIfNullOrWhiteSpace(promptPath);

        if (!Path.IsPathRooted(promptPath))
        {
            throw new InvalidOperationException(
                $"ContextSettings.PromptPath must be an absolute path, but '{promptPath}' is relative.");
        }

        if (!Directory.Exists(promptPath))
        {
            throw new InvalidOperationException($"Context prompts directory is not found.");
        }

        var loaded = ContextPromptsJsonLoader.LoadFromResolvedPath(promptPath);
        options.DefaultAssistantRole = loaded.DefaultAssistantRole;
        options.DefaultUserPrompt = loaded.DefaultUserPrompt;
    }
}
