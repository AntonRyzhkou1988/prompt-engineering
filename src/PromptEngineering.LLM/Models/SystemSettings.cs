namespace PromptEngineering.LLM.Models;

/// <summary>
/// Top-level system / infrastructure settings (e.g. AI HTTP clients).
/// </summary>
public sealed record SystemSettings
{
    /// <summary>
    /// Upper bound on CSV data rows loaded into the prompt; 0 means no limit.
    /// </summary>
    public int MaximumDatasetRecordCount { get; init; }

    public required AiServiceSettings AiServiceSettings { get; init; }
}
