namespace PromptEngineering.Services;

/// <summary>
/// Assistant and user prompt segments loaded from one prompt JSON document.
/// </summary>
public sealed class ContextPrompt
{
    public required string[] DefaultAssistantRole { get; set; }

    public required string[] DefaultUserPrompt { get; set; }

    public required string InstanceName { get; set; }

    public required float Temperature { get; set; }
}
