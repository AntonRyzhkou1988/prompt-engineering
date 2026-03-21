using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PromptEngineering.Services;

/// <summary>
/// Loads <see cref="ContextPromptsOptions"/> from the JSON file at <see cref="ContextSettings.DefaultPromptsJsonPath"/> (sole source at runtime).
/// </summary>
internal sealed class ContextPromptsPostConfigure : IPostConfigureOptions<ContextPromptsOptions>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

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

        var json = File.ReadAllText(resolvedPath, Encoding.UTF8);
        var document = JsonSerializer.Deserialize<ContextSettingsPromptDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize prompts JSON: '{resolvedPath}'.");

        if (document.DefaultAssistantRole == null || document.DefaultAssistantRole.Length == 0)
        {
            throw new InvalidOperationException(
                $"Prompts JSON '{resolvedPath}' must contain a non-empty DefaultAssistantRole array.");
        }

        if (document.DefaultUserPrompt == null || document.DefaultUserPrompt.Length == 0)
        {
            throw new InvalidOperationException(
                $"Prompts JSON '{resolvedPath}' must contain a non-empty DefaultUserPrompt array.");
        }

        options.DefaultAssistantRole = document.DefaultAssistantRole;
        options.DefaultUserPrompt = document.DefaultUserPrompt;
    }
}
