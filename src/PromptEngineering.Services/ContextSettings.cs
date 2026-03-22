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
    public required string PromptPath { get; set; }

    public required string DatasetPath { get; set; }

    public required string OutputDirectory { get; set; }

    public required List<string> ReActSequence { get; set; }
}
