namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering pipeline:
/// dataset loading, prompt construction, LLM completion, and persisting the first choice assistant message as Markdown.
/// </summary>
public interface IContextService
{
    /// <summary>Runs the full pipeline and returns the completion with the path to the saved assistant Markdown (first choice body only).</summary>
    Task<ContextPipelineResult> RunAsync(CancellationToken cancellationToken = default);
}
