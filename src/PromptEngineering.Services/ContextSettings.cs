namespace PromptEngineering.Services;

public sealed record ContextSettings
{
    public string AssistantRoleKey { get; init; } = "assistant.role";

    public string DefaultAssistantRole { get; init; } = "You are software developer assistant.";

    public string DefaultUserPrompt { get; init; } = "What is a GC in .NET?";

    public float Temperature { get; init; } = 0.3f;
}
