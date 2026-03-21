namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering pipeline:
/// dataset loading, prompt construction, LLM completion, and persisting the first choice assistant message as Markdown.
/// </summary>
public interface IContextService
{
    /// <summary>Runs the full pipeline and returns the completion with the path to the saved assistant Markdown (first choice body only).</summary>
    Task<ContextPipelineResult> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the full pipeline using prompts loaded from the given JSON path (relative to app base / repo walk, or absolute).
    /// The saved Markdown file name includes the prompt file stem (e.g. v2) for easier identification.
    /// </summary>
    Task<ContextPipelineResult> RunAsync(string promptsJsonRelativeOrAbsolutePath, CancellationToken cancellationToken = default);
}
