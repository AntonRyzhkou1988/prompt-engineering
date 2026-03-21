using Microsoft.Extensions.Options;

namespace PromptEngineering.Services;

/// <summary>
/// Loads <see cref="ContextPromptsOptions"/> from the JSON file at <see cref="ContextSettings.DefaultPromptsJsonPath"/> (sole source at runtime).
/// </summary>
internal sealed class ContextPromptsPostConfigure : IPostConfigureOptions<ContextPromptsOptions>
{
    private readonly IOptions<ContextSettings> _contextSettings;

    public ContextPromptsPostConfigure(IOptions<ContextSettings> contextSettings)
    {
        _contextSettings = contextSettings;
    }

    public void PostConfigure(string? name, ContextPromptsOptions options)
    {
        if (name is not null && !string.Equals(name, Options.DefaultName, StringComparison.Ordinal))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(options);

        var relativeOrAbsolute = _contextSettings.Value.DefaultPromptsJsonPath;
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeOrAbsolute);

        var resolvedPath = ContextSettingsPromptPathResolver.ResolveExistingFilePath(relativeOrAbsolute)
            ?? throw new InvalidOperationException(
                $"Context prompts JSON was not found. Tried path '{relativeOrAbsolute}' (resolved from app base and parent directories).");

        var loaded = ContextPromptsJsonLoader.LoadFromResolvedPath(resolvedPath);
        options.DefaultAssistantRole = loaded.DefaultAssistantRole;
        options.DefaultUserPrompt = loaded.DefaultUserPrompt;
    }
}
