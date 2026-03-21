namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering pipeline:
/// dataset loading, prompt construction, LLM completion, and completion persistence.
/// </summary>
public interface IContextService
{
    /// <summary>Runs the full pipeline and returns completion details with output artifact path.</summary>
    Task<ContextPipelineResult> RunAsync(CancellationToken cancellationToken = default);
}
