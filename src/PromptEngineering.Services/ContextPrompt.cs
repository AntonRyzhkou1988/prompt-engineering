namespace PromptEngineering.Services;

/// <summary>
/// Assistant and user prompt segments loaded from one prompt JSON document.
/// </summary>
public sealed class ContextPrompt
{
    public string[] DefaultAssistantRole { get; set; } = [];

    public string[] DefaultUserPrompt { get; set; } = [];
}
