using System.Text;
using System.Text.Json;

namespace PromptEngineering.Services;

internal static class ContextPromptsJsonLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ContextPrompt LoadFromResolvedPath(string resolvedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedPath);

        var json = File.ReadAllText(resolvedPath, Encoding.UTF8);
        var document = JsonSerializer.Deserialize<ContextPrompt>(json, JsonOptions)
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

        return new ContextPrompt
        {
            DefaultAssistantRole = document.DefaultAssistantRole,
            DefaultUserPrompt = document.DefaultUserPrompt,
            InstanceName = document.InstanceName,
            Temperature = document.Temperature
        };
    }
}
