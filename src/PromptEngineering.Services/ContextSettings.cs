namespace PromptEngineering.Services;

/// <summary>
/// Options for the prompt-engineering pipeline. Prompt text is loaded from <see cref="PromptPath"/> into <see cref="ContextPromptsOptions"/>.
/// </summary>
public sealed class ContextSettings
{
    /// <summary>
    /// Absolute path to JSON containing <c>DefaultAssistantRole</c> and <c>DefaultUserPrompt</c> string arrays.
    /// </summary>
    public string PromptPath { get; set; } = string.Empty;

    public float Temperature { get; set; } = 0.3f;

    public string DatasetPath { get; set; } = "dataset/attacks.csv";

    public string OutputDirectory { get; set; } = "output";
}
