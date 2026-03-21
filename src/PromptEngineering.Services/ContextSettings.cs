namespace PromptEngineering.Services;

public sealed record ContextSettings
{
    public string AiInstanceName { get; init; } = "AIArchitect.PromptEngineering";

    public string DefaultAssistantRole { get; init; } = "You are software developer assistant.";

    public string DefaultUserPrompt { get; init; } = "What is a GC in .NET?";

    public float Temperature { get; init; } = 0.3f;

    public string DatasetPath { get; init; } = "dataset/attacks.csv";

    public string OutputDirectory { get; init; } = "output";
}
