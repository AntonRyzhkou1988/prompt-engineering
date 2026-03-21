namespace PromptEngineering.Services;

public sealed record ContextSettings
{
    public string AiInstanceName { get; init; } = "AIArchitect.PromptEngineering";

    public string[] DefaultAssistantRole { get; init; } = ["You are software developer assistant."];

    public string[] DefaultUserPrompt { get; init; } =
    [
        "Analyze shark attack incidents from dataset/attacks.csv.",
        "<data>",
        "- Dataset rows will be injected here at runtime.",
        "</data>",
        "Use only the records above as evidence."
    ];

    public float Temperature { get; init; } = 0.3f;

    public string DatasetPath { get; init; } = "dataset/attacks.csv";

    public string OutputDirectory { get; init; } = "output";
}
