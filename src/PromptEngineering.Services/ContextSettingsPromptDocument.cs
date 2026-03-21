using System.Text.Json.Serialization;

namespace PromptEngineering.Services;

internal sealed class ContextSettingsPromptDocument
{
    [JsonPropertyName("DefaultAssistantRole")]
    public string[]? DefaultAssistantRole { get; set; }

    [JsonPropertyName("DefaultUserPrompt")]
    public string[]? DefaultUserPrompt { get; set; }
}
