namespace PromptEngineering.Services;

/// <summary>
/// Options for the prompt-engineering pipeline.
/// </summary>
public sealed class ContextSettings
{
    /// <summary>
    /// Path to the prompts directory containing versioned JSON files (for example v1.json, v2.json, v3.json).
    /// The runtime discovers all <c>*.json</c> files there and executes them in file-name order.
    /// Absolute and repository-relative paths are supported.
    /// </summary>
    public string PromptPath { get; set; } = string.Empty;

    public float Temperature { get; set; } = 0.3f;

    public string DatasetPath { get; set; } = "dataset/attacks.csv";

    public string OutputDirectory { get; set; } = "output";
}
