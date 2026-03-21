namespace PromptEngineering.Services;

/// <summary>
/// Assistant and user prompt segments loaded from the JSON file at <see cref="ContextSettings.DefaultPromptsJsonPath"/>.
/// </summary>
public sealed class ContextPromptsOptions
{
    public string[] DefaultAssistantRole { get; set; } = [];

    public string[] DefaultUserPrompt { get; set; } = [];
}
