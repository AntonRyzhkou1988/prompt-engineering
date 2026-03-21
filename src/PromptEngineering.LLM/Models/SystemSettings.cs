namespace PromptEngineering.LLM.Models;

/// <summary>
/// Top-level system / infrastructure settings (e.g. AI HTTP clients).
/// </summary>
public sealed record SystemSettings
{
    public required AiServiceSettings AiServiceSettings { get; init; }
}
