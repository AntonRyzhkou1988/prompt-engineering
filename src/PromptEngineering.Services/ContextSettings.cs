namespace PromptEngineering.Services;

/// <summary>
/// Options for the prompt-engineering pipeline. Prompt text is loaded from <see cref="DefaultPromptsJsonPath"/> into <see cref="ContextPromptsOptions"/>.
/// </summary>
public sealed class ContextSettings
{
    /// <summary>
    /// Path to JSON containing <c>DefaultAssistantRole</c> and <c>DefaultUserPrompt</c> string arrays (relative to app base or repo search, or absolute).
    /// </summary>
    public string DefaultPromptsJsonPath { get; set; } = "prompts/default-prompts.json";

    public float Temperature { get; set; } = 0.3f;

    public string DatasetPath { get; set; } = "dataset/attacks.csv";

    public string OutputDirectory { get; set; } = "output";
}
